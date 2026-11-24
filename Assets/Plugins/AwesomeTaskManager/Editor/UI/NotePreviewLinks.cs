using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    [InitializeOnLoad]
    internal static class NotePreviewLinks
    {
        private static readonly Regex ScriptMentionRegex = new Regex(
            @"(?<![\w/\\.-])(?<path>(?:[\w.-]+[\\/])*[\w.-]+\.cs)(?![\w.-])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly string[] TitleOpeningMarkers = { "[[", "\"", "**" };
        private static readonly Dictionary<string, string> ScriptGuidCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> CardIdByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> CardIdByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<KeyValuePair<string, string>> CardTitlesByLength = new List<KeyValuePair<string, string>>();
        private static readonly List<KeyValuePair<string, string>> UnusualCardCodes = new List<KeyValuePair<string, string>>();

        private static SaveData _cardIndexData;
        private static double _cardIndexBuiltAt;
        private static int _minCardCodeLength;
        private static int _maxCardCodeLength;
        private static bool _allCardCodesContainDigit;
        private static bool _previewActive;

        static NotePreviewLinks()
        {
            EditorGUI.hyperLinkClicked -= OnHyperLinkClicked;
            EditorGUI.hyperLinkClicked += OnHyperLinkClicked;
            EditorApplication.projectChanged -= ClearScriptCache;
            EditorApplication.projectChanged += ClearScriptCache;
        }

        public static void BeginPreview(SaveData data)
        {
            _previewActive = true;
            RefreshCardIndex(data);
        }

        public static void EndPreview()
        {
            _previewActive = false;
        }

        public static string WrapInlineCode(string rawCode, string formattedCode)
        {
            if (!_previewActive || string.IsNullOrWhiteSpace(rawCode)) return formattedCode;

            string scriptGuid = ResolveScriptGuid(rawCode.Trim());
            if (!string.IsNullOrEmpty(scriptGuid))
                return CreateLinkMarkup("atmscript", scriptGuid, rawCode, GetLinkColor());

            string cardId = ResolveCardId(rawCode.Trim());
            return !string.IsNullOrEmpty(cardId)
                ? CreateLinkMarkup("atmcard", cardId, rawCode, GetLinkColor())
                : formattedCode;
        }

        public static string ProtectReferences(string text, List<string> tokens, string linkColor)
        {
            if (!_previewActive || string.IsNullOrEmpty(text) || tokens == null) return text;

            text = ProtectMarkedCardTitles(text, tokens, linkColor);

            if (text.IndexOf(".cs", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text = ScriptMentionRegex.Replace(text, match =>
                {
                    string guid = ResolveScriptGuid(match.Groups["path"].Value);
                    return string.IsNullOrEmpty(guid)
                        ? match.Value
                        : AddToken(tokens, CreateLinkMarkup("atmscript", guid, match.Value, linkColor));
                });
            }

            return ProtectTaskCodeMentions(text, tokens, linkColor);
        }

        private static string ProtectMarkedCardTitles(string text, List<string> tokens, string linkColor)
        {
            if (CardTitlesByLength.Count == 0 || !ContainsTitleMarker(text)) return text;

            StringBuilder builder = null;
            int copiedThrough = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!TryGetTitleMarker(text, i, out string openingMarker, out string closingMarker)) continue;

                int titleStart = i + openingMarker.Length;
                foreach (var titleEntry in CardTitlesByLength)
                {
                    string title = titleEntry.Key;
                    if (titleStart + title.Length > text.Length ||
                        string.Compare(text, titleStart, title, 0, title.Length, StringComparison.OrdinalIgnoreCase) != 0)
                        continue;

                    int titleEnd = titleStart + title.Length;
                    if (!MatchesAt(text, titleEnd, closingMarker, StringComparison.Ordinal)) continue;

                    if (builder == null) builder = new StringBuilder(text.Length + 32);
                    builder.Append(text, copiedThrough, titleStart - copiedThrough);
                    string matchedTitle = text.Substring(titleStart, title.Length);
                    builder.Append(AddToken(tokens, CreateLinkMarkup("atmcard", titleEntry.Value, matchedTitle, linkColor)));
                    copiedThrough = titleEnd;
                    i = titleEnd - 1;
                    break;
                }
            }

            if (builder == null) return text;
            builder.Append(text, copiedThrough, text.Length - copiedThrough);
            return builder.ToString();
        }

        private static bool ContainsTitleMarker(string text)
        {
            return text.IndexOf("[[", StringComparison.Ordinal) >= 0 ||
                   text.IndexOf('"') >= 0 ||
                   text.IndexOf("**", StringComparison.Ordinal) >= 0;
        }

        private static bool TryGetTitleMarker(string text, int index, out string openingMarker, out string closingMarker)
        {
            foreach (string marker in TitleOpeningMarkers)
            {
                if (!MatchesAt(text, index, marker, StringComparison.Ordinal)) continue;
                openingMarker = marker;
                closingMarker = marker == "[[" ? "]]" : marker;
                return true;
            }

            openingMarker = null;
            closingMarker = null;
            return false;
        }

        private static bool MatchesAt(string text, int index, string value, StringComparison comparison)
        {
            return index + value.Length <= text.Length &&
                   string.Compare(text, index, value, 0, value.Length, comparison) == 0;
        }

        private static string ProtectTaskCodeMentions(string text, List<string> tokens, string linkColor)
        {
            if (CardIdByCode.Count == 0) return text;

            StringBuilder builder = null;
            int copiedThrough = 0;
            for (int i = 0; i < text.Length;)
            {
                if (text[i] == '\u0001')
                {
                    int tokenEnd = text.IndexOf('\u0002', i + 1);
                    i = tokenEnd >= 0 ? tokenEnd + 1 : i + 1;
                    continue;
                }

                if (!IsAsciiCardCodeChar(text[i]))
                {
                    i++;
                    continue;
                }

                int start = i;
                bool containsDigit = false;
                while (i < text.Length && IsAsciiCardCodeChar(text[i]))
                {
                    if (char.IsDigit(text[i])) containsDigit = true;
                    i++;
                }

                int length = i - start;
                if (length < _minCardCodeLength || length > _maxCardCodeLength ||
                    (_allCardCodesContainDigit && !containsDigit) ||
                    !HasCodeBoundaryBefore(text, start) || !HasCodeBoundaryAfter(text, i))
                    continue;

                string candidate = text.Substring(start, length);
                if (!CardIdByCode.TryGetValue(candidate, out string cardId)) continue;

                if (builder == null) builder = new StringBuilder(text.Length + 32);
                builder.Append(text, copiedThrough, start - copiedThrough);
                builder.Append(AddToken(tokens, CreateLinkMarkup("atmcard", cardId, candidate, linkColor)));
                copiedThrough = i;
            }

            if (builder != null)
            {
                builder.Append(text, copiedThrough, text.Length - copiedThrough);
                text = builder.ToString();
            }

            return ProtectUnusualCardCodes(text, tokens, linkColor);
        }

        private static string ProtectUnusualCardCodes(string text, List<string> tokens, string linkColor)
        {
            foreach (var codeEntry in UnusualCardCodes)
            {
                string code = codeEntry.Key;
                int searchFrom = 0;
                int matchIndex;
                StringBuilder builder = null;
                int copiedThrough = 0;
                while ((matchIndex = text.IndexOf(code, searchFrom, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    int matchEnd = matchIndex + code.Length;
                    bool inProtectedToken = matchIndex > 0 && text[matchIndex - 1] == '\u0001';
                    if (inProtectedToken || !HasCodeBoundaryBefore(text, matchIndex) || !HasCodeBoundaryAfter(text, matchEnd))
                    {
                        searchFrom = matchEnd;
                        continue;
                    }

                    if (builder == null) builder = new StringBuilder(text.Length + 32);
                    builder.Append(text, copiedThrough, matchIndex - copiedThrough);
                    builder.Append(AddToken(tokens, CreateLinkMarkup("atmcard", codeEntry.Value, code, linkColor)));
                    copiedThrough = matchEnd;
                    searchFrom = matchEnd;
                }

                if (builder != null)
                {
                    builder.Append(text, copiedThrough, text.Length - copiedThrough);
                    text = builder.ToString();
                }
            }

            return text;
        }

        private static bool IsAsciiCardCodeChar(char character)
        {
            return character >= 'A' && character <= 'Z' ||
                   character >= 'a' && character <= 'z' ||
                   character >= '0' && character <= '9' ||
                   character == '_' || character == '-';
        }

        private static bool HasCodeBoundaryBefore(string text, int index)
        {
            return index == 0 || !IsRegexWordChar(text[index - 1]) && text[index - 1] != '-';
        }

        private static bool HasCodeBoundaryAfter(string text, int index)
        {
            return index >= text.Length || !IsRegexWordChar(text[index]) && text[index] != '-';
        }

        private static bool IsRegexWordChar(char character)
        {
            switch (char.GetUnicodeCategory(character))
            {
                case System.Globalization.UnicodeCategory.UppercaseLetter:
                case System.Globalization.UnicodeCategory.LowercaseLetter:
                case System.Globalization.UnicodeCategory.TitlecaseLetter:
                case System.Globalization.UnicodeCategory.ModifierLetter:
                case System.Globalization.UnicodeCategory.OtherLetter:
                case System.Globalization.UnicodeCategory.NonSpacingMark:
                case System.Globalization.UnicodeCategory.DecimalDigitNumber:
                case System.Globalization.UnicodeCategory.ConnectorPunctuation:
                    return true;
                default:
                    return false;
            }
        }

        private static string AddToken(List<string> tokens, string value)
        {
            int index = tokens.Count;
            tokens.Add(value);
            return $"\u0001TOKEN_{index}\u0002";
        }

        private static string CreateLinkMarkup(string attribute, string value, string label, string linkColor)
        {
            string safeValue = (value ?? string.Empty).Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
            string safeLabel = (label ?? string.Empty).Replace("<", "‹").Replace(">", "›");
            return $"<a name=\"atm-link\" {attribute}=\"{safeValue}\"><color={linkColor}><u>{safeLabel}</u></color></a>";
        }

        private static string GetLinkColor()
        {
            return TBStyles.LinkColorHex;
        }

        private static string ResolveScriptGuid(string mention)
        {
            if (string.IsNullOrWhiteSpace(mention)) return null;
            mention = mention.Trim().Replace('\\', '/');
            if (!mention.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return null;
            if (ScriptGuidCache.TryGetValue(mention, out string cachedGuid))
                return cachedGuid;

            string fileName = GetFileNameSafe(mention);
            if (fileName.Length <= 3 ||
                !fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            {
                ScriptGuidCache[mention] = null;
                return null;
            }

            string searchName = fileName.Substring(0, fileName.Length - 3);
            string[] guids = AssetDatabase.FindAssets(searchName + " t:MonoScript");
            var matches = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(GetFileNameSafe(path), fileName, StringComparison.OrdinalIgnoreCase))
                    matches.Add(path);
            }

            string resolvedGuid = matches
                .OrderBy(path => PathMatchesMention(path, mention) ? 0 : 1)
                .ThenBy(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(path => path.Length)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(AssetDatabase.AssetPathToGUID)
                .FirstOrDefault();

            ScriptGuidCache[mention] = resolvedGuid;
            return resolvedGuid;
        }

        private static string GetFileNameSafe(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            int separatorIndex = path.LastIndexOfAny(new[] { '/', '\\' });
            return separatorIndex >= 0 ? path.Substring(separatorIndex + 1) : path;
        }

        private static bool PathMatchesMention(string assetPath, string mention)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(mention)) return false;
            string normalizedMention = mention.TrimStart('/');
            return string.Equals(assetPath, normalizedMention, StringComparison.OrdinalIgnoreCase) ||
                   assetPath.EndsWith("/" + normalizedMention, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveCardId(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;
            if (CardIdByCode.TryGetValue(reference, out string cardId)) return cardId;
            return CardIdByTitle.TryGetValue(reference, out cardId) ? cardId : null;
        }

        private static void RefreshCardIndex(SaveData data)
        {
            if (ReferenceEquals(_cardIndexData, data) && EditorApplication.timeSinceStartup - _cardIndexBuiltAt < 1d)
                return;

            _cardIndexData = data;
            _cardIndexBuiltAt = EditorApplication.timeSinceStartup;
            CardIdByCode.Clear();
            CardIdByTitle.Clear();
            CardTitlesByLength.Clear();
            UnusualCardCodes.Clear();
            _minCardCodeLength = int.MaxValue;
            _maxCardCodeLength = 0;
            _allCardCodesContainDigit = true;

            if (data != null)
            {
                foreach (var card in data.AllCards())
                {
                    if (card == null || card.archived) continue;
                    if (!string.IsNullOrWhiteSpace(card.taskCode) && !CardIdByCode.ContainsKey(card.taskCode.Trim()))
                        CardIdByCode.Add(card.taskCode.Trim(), card.id);
                    if (!string.IsNullOrWhiteSpace(card.title) && !CardIdByTitle.ContainsKey(card.title.Trim()))
                        CardIdByTitle.Add(card.title.Trim(), card.id);
                }
            }

            foreach (var codeEntry in CardIdByCode)
            {
                string code = codeEntry.Key;
                if (code.All(IsAsciiCardCodeChar))
                {
                    _minCardCodeLength = Math.Min(_minCardCodeLength, code.Length);
                    _maxCardCodeLength = Math.Max(_maxCardCodeLength, code.Length);
                    if (!code.Any(char.IsDigit)) _allCardCodesContainDigit = false;
                }
                else
                {
                    UnusualCardCodes.Add(codeEntry);
                }
            }

            if (_minCardCodeLength == int.MaxValue) _minCardCodeLength = 0;
            UnusualCardCodes.Sort((left, right) => right.Key.Length.CompareTo(left.Key.Length));
            CardTitlesByLength.AddRange(CardIdByTitle.OrderByDescending(entry => entry.Key.Length));
        }

        private static void OnHyperLinkClicked(EditorWindow sourceWindow, HyperLinkClickedEventArgs args)
        {
            if (args?.hyperLinkData == null) return;

            if (args.hyperLinkData.TryGetValue("atmscript", out string scriptGuid))
            {
                EditorApplication.delayCall += () => SelectScript(scriptGuid);
            }
            else if (args.hyperLinkData.TryGetValue("atmcard", out string cardId))
            {
                EditorApplication.delayCall += () => TaskBoardWindow.OpenCardDetails(cardId);
            }
        }

        private static void SelectScript(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null)
            {
                var window = TaskBoardWindow.Instance;
                if (window != null) window.ShowNotification(new GUIContent("The linked script file could not be found"));
                else Debug.LogWarning("[AwesomeTaskManager] The linked script file could not be found.");
                return;
            }

            Selection.activeObject = script;
            EditorGUIUtility.PingObject(script);
        }

        private static void ClearScriptCache()
        {
            ScriptGuidCache.Clear();
        }
    }
}