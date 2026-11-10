using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AwesomeTaskManager.Editor
{
    /// <summary>
    /// Utility class to render Markdown content in the Unity Editor.
    /// Extracted from TaskBoardWindow to be reusable in NotePopupWindow.
    /// </summary>
    [InitializeOnLoad]
    public static class MarkdownRenderer
    {
        static MarkdownRenderer()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= NoteVideoPlayer.DisposeAll;
            AssemblyReloadEvents.beforeAssemblyReload += NoteVideoPlayer.DisposeAll;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            ClearCache();
        }

        /// <summary>
        /// Clears all cached preview textures and GIF decoders.
        /// </summary>
        public static void ClearCache()
        {
            foreach (var kvp in _imageCache)
            {
                if (kvp.Value != null && (kvp.Value.hideFlags & HideFlags.DontSave) != 0)
                {
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
                }
            }
            _imageCache.Clear();

            foreach (var kvp in _gifCache)
            {
                if (kvp.Value != null && kvp.Value.Frames != null)
                {
                    foreach (var frame in kvp.Value.Frames)
                    {
                        if (frame.texture != null)
                        {
                            UnityEngine.Object.DestroyImmediate(frame.texture);
                        }
                    }
                }
            }
            _gifCache.Clear();
            _failedGifPaths.Clear();

            foreach (var state in _remoteImageCache.Values)
            {
                if (state.request != null)
                {
                    state.request.Abort();
                }
            }
            _remoteImageCache.Clear();

            foreach (var state in _youtubeTitleCache.Values)
            {
                if (state.request != null)
                {
                    state.request.Abort();
                    state.request.Dispose();
                }
            }
            _youtubeTitleCache.Clear();
            NoteVideoPlayer.DisposeAll();
        }

        private static readonly Regex _imageEmbedRegex = new Regex(@"!\[\[([^\]]+)\]\]|!\[([^\]]*)\]\(([^\)]+)\)", RegexOptions.Compiled);
        private static readonly Regex _urlRegex = new Regex(@"(https?://[^\s\n\r\)]+)", RegexOptions.Compiled);
        private static readonly Regex _htmlImageSrcRegex = new Regex(@"<img\b[^>]*?\bsrc\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga", ".psd", ".tiff", ".tif" };
        public static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov", ".m4v", ".ogv" };

        /// <summary>
        /// Filters for EditorUtility.OpenFilePanelWithFilters when attaching media to a note.
        /// </summary>
        public static string[] NoteAttachmentFileFilters
        {
            get
            {
                string images = string.Join(",", Array.ConvertAll(ImageExtensions, e => e.TrimStart('.')));
                string videos = string.Join(",", Array.ConvertAll(VideoExtensions, e => e.TrimStart('.')));
                return new[]
                {
                    "Images & Videos", images + "," + videos,
                    "Images", images,
                    "Videos", videos,
                    "All files", "*"
                };
            }
        }

        private static readonly Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GifDecoder> _gifCache = new Dictionary<string, GifDecoder>();
        private static readonly HashSet<string> _failedGifPaths = new HashSet<string>();
        private static readonly Dictionary<string, RemoteImageState> _remoteImageCache = new Dictionary<string, RemoteImageState>();
        private static readonly Dictionary<string, YouTubeTitleState> _youtubeTitleCache = new Dictionary<string, YouTubeTitleState>();

        private sealed class RemoteImageState
        {
            public string assetPath;
            public string error;
            public bool loading;
            public UnityWebRequest request;
        }

        private sealed class YouTubeTitleState
        {
            public string title;
            public bool loading;
            public UnityWebRequest request;
        }

        [Serializable]
        private sealed class YouTubeOEmbedData
        {
            public string title;
        }

        public static string GetYouTubeVideoId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            string host = uri.Host.ToLowerInvariant();
            bool isShortHost = host == "youtu.be" || host == "www.youtu.be";
            bool isYouTubeHost = host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal) ||
                                 host == "youtube-nocookie.com" || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal);
            if (isShortHost)
            {
                string[] segments = uri.AbsolutePath.Trim('/').Split('/');
                return segments.Length > 0 ? NormalizeYouTubeVideoId(segments[0]) : null;
            }
            if (!isYouTubeHost) return null;

            foreach (string pair in uri.Query.TrimStart('?').Split('&'))
            {
                string[] parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), "v", StringComparison.OrdinalIgnoreCase))
                    return NormalizeYouTubeVideoId(Uri.UnescapeDataString(parts[1]));
            }

            string[] pathSegments = uri.AbsolutePath.Trim('/').Split('/');
            if (pathSegments.Length >= 2 &&
                (string.Equals(pathSegments[0], "shorts", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(pathSegments[0], "embed", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(pathSegments[0], "live", StringComparison.OrdinalIgnoreCase)))
                return NormalizeYouTubeVideoId(pathSegments[1]);

            return null;
        }

        public static bool IsYouTubeUrl(string url)
        {
            return GetYouTubeVideoId(url) != null;
        }

        public static string TryGetYouTubeTitle(string url)
        {
            string videoId = GetYouTubeVideoId(url);
            return videoId != null ? GetYouTubeTitleState(videoId).title : null;
        }

        public static string GetLinkDisplayTitle(string displayName, string url)
        {
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
            return TryGetYouTubeTitle(url) ?? url;
        }

        public static string BuildOpenUrlMessage(string url, string title = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return $"Open this link in your browser?\n\n{url}";

            return $"Open this link in your browser?\n\n{title.Trim()}\n{url}";
        }

        private static string NormalizeYouTubeVideoId(string videoId)
        {
            if (string.IsNullOrEmpty(videoId) || videoId.Length != 11) return null;
            foreach (char character in videoId)
            {
                if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') &&
                    !(character >= '0' && character <= '9') && character != '_' && character != '-')
                    return null;
            }
            return videoId;
        }

        public static bool IsVideoPath(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return false;
            string extension;
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                extension = Path.GetExtension(uri.AbsolutePath);
            else if (pathOrUrl.Contains("://"))
                return false;
            else
                extension = Path.GetExtension(pathOrUrl);

            return Array.Exists(VideoExtensions, item => string.Equals(item, extension, StringComparison.OrdinalIgnoreCase));
        }

        public static string ExtractOEmbedTitle(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<YouTubeOEmbedData>(json)?.title;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsGifUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
            return string.Equals(Path.GetExtension(uri.AbsolutePath), ".gif", StringComparison.OrdinalIgnoreCase);
        }

        public static string ExtractImageSrcFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            Match match = _htmlImageSrcRegex.Match(html);
            if (!match.Success) return null;

            string source = match.Groups[1].Success ? match.Groups[1].Value
                : match.Groups[2].Success ? match.Groups[2].Value
                : match.Groups[3].Value;
            return WebUtility.HtmlDecode(source.Trim());
        }

        public static string DetectImageExtension(byte[] bytes)
        {
            if (bytes == null) return null;
            if (bytes.Length >= 6 &&
                ((bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == '8' && bytes[5] == 'a') &&
                 (bytes[4] == '7' || bytes[4] == '9')))
                return ".gif";
            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47 &&
                bytes[4] == 0x0d && bytes[5] == 0x0a && bytes[6] == 0x1a && bytes[7] == 0x0a)
                return ".png";
            if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
                return ".jpg";
            return null;
        }

        public static string SaveImageBytesToProject(byte[] bytes, string baseName)
        {
            string extension = DetectImageExtension(bytes);
            if (extension == null) return null;

            string directory = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string name = Path.GetFileNameWithoutExtension(baseName ?? "pasted_image");
            if (string.IsNullOrWhiteSpace(name)) name = "pasted_image";
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '_');

            string fullPath = Path.Combine(directory, name + extension);
            if (!File.Exists(fullPath)) File.WriteAllBytes(fullPath, bytes);

            string dataPath = Application.dataPath.Replace('\\', '/');
            string assetPath = "Assets" + fullPath.Replace('\\', '/').Substring(dataPath.Length);
            AssetDatabase.ImportAsset(assetPath);
            return assetPath;
        }

        public static string DownloadImageToProject(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            const string gifDataPrefix = "data:image/gif;base64,";
            if (url.StartsWith(gifDataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    byte[] data = Convert.FromBase64String(url.Substring(gifDataPrefix.Length));
                    return SaveImageBytesToProject(data, "pasted_gif_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[TaskBoard] Invalid GIF data URI: " + e.Message);
                    return null;
                }
            }

            if (!IsHttpUrl(url)) return null;

            try
            {
                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = 20;
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    while (!operation.isDone && stopwatch.Elapsed.TotalSeconds < 20)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Downloading image", url, Mathf.Clamp01(request.downloadProgress)))
                        {
                            request.Abort();
                            return null;
                        }
                        Thread.Sleep(10);
                    }

                    if (!operation.isDone || request.result != UnityWebRequest.Result.Success)
                        return null;

                    byte[] data = request.downloadHandler?.data;
                    if (data == null || data.LongLength > GifDecoder.MaxFileSize)
                        return null;
                    return SaveImageBytesToProject(data, "pasted_image_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TaskBoard] Image download failed: " + e.Message);
                return null;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool IsHttpUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static string _lastCopiedCode = null;
        private static double _lastCopiedTime = 0;
        private static string _tableSelectionNoteId = "";
        private static string _tableSelectionKey = "";
        private static int _tableSelectionAnchorRow = -1;
        private static int _tableSelectionAnchorColumn = -1;
        private static int _tableSelectionFocusRow = -1;
        private static int _tableSelectionFocusColumn = -1;
        private static bool _tableSelectionActive;
        private static bool _tableSelectionDragging;
        private static Rect _tableSelectionRect;
        private static string _tableSelectionText = "";

        /// <summary>
        /// Converts Markdown inline syntax (**bold**, *italic*, `code`, ~~strikethrough~~, [links](url))
        /// into Unity IMGUI Rich Text tags (&lt;b&gt;, &lt;i&gt;, &lt;color&gt;, &lt;a&gt;).
        /// </summary>
        public static string FormatInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";

            string codeColor = EditorGUIUtility.isProSkin ? "#E5C07B" : "#8A3B00";
            string linkColor = EditorGUIUtility.isProSkin ? "#569CD6" : "#0066CC";

            var tokens = new List<string>();

            // 1. Protect and format inline code `...`
            text = Regex.Replace(text, @"`([^`\n\r]+)`", m =>
            {
                string rawCode = m.Groups[1].Value;
                // Sanitize angle brackets inside code to avoid Unity IMGUI invalid tag warnings
                rawCode = rawCode.Replace("<", "‹").Replace(">", "›");
                string formattedCode = $"<color={codeColor}><b>{rawCode}</b></color>";
                int idx = tokens.Count;
                tokens.Add(formattedCode);
                return $"\u0001TOKEN_{idx}\u0002";
            });

            // 2. Protect and format markdown links [label](url)
            text = Regex.Replace(text, @"\[([^\]]+)\]\((https?://[^\s\)]+)\)", m =>
            {
                string label = m.Groups[1].Value.Replace("<", "‹").Replace(">", "›");
                string url = EscapeLinkUrl(m.Groups[2].Value);
                string formattedLink = $"<a href=\"{url}\"><color={linkColor}><u>{label}</u></color></a>";
                int idx = tokens.Count;
                tokens.Add(formattedLink);
                return $"\u0001TOKEN_{idx}\u0002";
            });

            // 3. Protect and format raw URLs https://... or http://...
            text = Regex.Replace(text, @"https?://[^\s\n\r\)]+", m =>
            {
                string url = m.Value;
                int trimLength = url.Length;
                while (trimLength > 0 && ".,;:!?".IndexOf(url[trimLength - 1]) >= 0)
                    trimLength--;

                string linkText = url.Substring(0, trimLength);
                string punctuation = url.Substring(trimLength);
                if (linkText.Length == 0) return url;

                string safeUrl = EscapeLinkUrl(linkText);
                string formattedUrl = $"<a href=\"{safeUrl}\"><color={linkColor}><u>{linkText}</u></color></a>{punctuation}";
                int idx = tokens.Count;
                tokens.Add(formattedUrl);
                return $"\u0001TOKEN_{idx}\u0002";
            });

            // 4. Sanitize raw angle brackets in user text so Unity IMGUI doesn't treat e.g. <GameObject> as invalid tags
            text = text.Replace("<", "‹").Replace(">", "›");

            // 5. Bold + Italic: ***text*** or ___text___
            text = Regex.Replace(text, @"\*\*\*([^\*\n]+?)\*\*\*", "<b><i>$1</i></b>");
            text = Regex.Replace(text, @"___([^_\n]+?)___", "<b><i>$1</i></b>");

            // 6. Bold: **text** or __text__
            text = Regex.Replace(text, @"\*\*([^\*\n]+?)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"__([^_\n]+?)__", "<b>$1</b>");

            // 7. Italic: *text*
            text = Regex.Replace(text, @"(?<!\*)\*([^\*\n]+?)\*(?!\*)", "<i>$1</i>");

            // 8. Italic: _text_ (require word boundary so snake_case_vars are unaffected)
            text = Regex.Replace(text, @"(?<=^|[\s\p{P}])_([^_\n]+?)_(?=[\s\p{P}]|$)", "<i>$1</i>");

            // 9. Strikethrough: ~~text~~
            text = Regex.Replace(text, @"~~([^~\n]+?)~~", "<color=#888888><s>$1</s></color>");

            // 10. Restore protected tokens
            for (int i = 0; i < tokens.Count; i++)
            {
                text = text.Replace($"\u0001TOKEN_{i}\u0002", tokens[i]);
            }

            return text;
        }

        private static string EscapeLinkUrl(string url)
        {
            return (url ?? "").Replace("\"", "%22").Replace("<", "%3C").Replace(">", "%3E");
        }

        /// <summary>
        /// Applies lightweight syntax highlighting to code blocks (C#, JSON, Shader, etc.) for Unity IMGUI rich text display.
        /// </summary>
        public static string HighlightSyntax(string code, string language)
        {
            if (string.IsNullOrEmpty(code)) return code;

            language = (language ?? "").ToLowerInvariant().Trim();
            bool isDark = EditorGUIUtility.isProSkin;

            string keywordColor = isDark ? "#569CD6" : "#0000FF";
            string typeColor = isDark ? "#4EC9B0" : "#267F99";
            string stringColor = isDark ? "#D69D85" : "#A31515";
            string commentColor = isDark ? "#6A9955" : "#008000";
            string numberColor = isDark ? "#B5CEA8" : "#098658";
            string defaultColor = isDark ? "#D4D4D4" : "#222222";

            // Sanitize angle brackets first
            string text = code.Replace("&", "&amp;").Replace("<", "‹").Replace(">", "›");

            if (language == "csharp" || language == "cs" || language == "unity" || language == "c#" || language == "c++" || language == "cpp" || language == "shader" || language == "hlsl")
            {
                // 1. Comments
                var commentTokens = new List<string>();
                text = Regex.Replace(text, @"(//[^\n\r]*)|(/\*[\s\S]*?\*/)", m =>
                {
                    string token = $"<color={commentColor}><i>{m.Value}</i></color>";
                    int idx = commentTokens.Count;
                    commentTokens.Add(token);
                    return $"\u0001COMM_{idx}\u0002";
                });

                // 2. Strings & Character literals
                var stringTokens = new List<string>();
                text = Regex.Replace(text, @"(@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'[^']*')", m =>
                {
                    string token = $"<color={stringColor}>{m.Value}</color>";
                    int idx = stringTokens.Count;
                    stringTokens.Add(token);
                    return $"\u0001STR_{idx}\u0002";
                });

                // 3. Keywords
                string[] keywords = {
                    "public", "private", "protected", "internal", "static", "readonly", "const", "volatile",
                    "class", "struct", "interface", "enum", "record", "namespace", "using", "delegate", "event",
                    "void", "int", "float", "double", "bool", "string", "char", "byte", "sbyte", "short", "ushort", "uint", "ulong", "long", "decimal", "object", "var",
                    "if", "else", "switch", "case", "default", "for", "foreach", "while", "do",
                    "break", "continue", "return", "throw", "try", "catch", "finally", "yield",
                    "new", "override", "virtual", "abstract", "sealed", "async", "await",
                    "get", "set", "init", "add", "remove", "value",
                    "true", "false", "null", "this", "base", "typeof", "sizeof", "nameof",
                    "is", "as", "in", "out", "ref", "params",
                    "Shader", "Properties", "SubShader", "Pass", "Tags", "CGPROGRAM", "ENDCG", "HLSLPROGRAM", "ENDHLSL"
                };
                string kwPattern = @"\b(" + string.Join("|", keywords) + @")\b";
                text = Regex.Replace(text, kwPattern, $"<color={keywordColor}><b>$1</b></color>");

                // 4. Common Types
                string[] types = {
                    "MonoBehaviour", "ScriptableObject", "GameObject", "Transform", "Component",
                    "Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int", "Quaternion", "Matrix4x4", "Color", "Color32", "Rect", "Bounds",
                    "Mathf", "Debug", "Time", "Input", "Camera", "AudioSource", "AudioClip",
                    "Rigidbody", "Rigidbody2D", "Collider", "Collider2D", "MeshRenderer", "SkinnedMeshRenderer",
                    "TrailRenderer", "ParticleSystem", "Light", "Canvas", "RectTransform", "Image", "Text", "Button",
                    "List", "Dictionary", "HashSet", "Queue", "Stack", "IEnumerable", "IEnumerator",
                    "Action", "Func", "Predicate", "Task", "Array", "Exception", "Type", "Guid", "DateTime", "StringBuilder"
                };
                string typePattern = @"\b(" + string.Join("|", types) + @")\b";
                text = Regex.Replace(text, typePattern, $"<color={typeColor}>$1</color>");

                // 5. Restore strings and comments
                for (int i = 0; i < stringTokens.Count; i++)
                    text = text.Replace($"\u0001STR_{i}\u0002", stringTokens[i]);

                for (int i = 0; i < commentTokens.Count; i++)
                    text = text.Replace($"\u0001COMM_{i}\u0002", commentTokens[i]);
            }
            else if (language == "json")
            {
                text = Regex.Replace(text, @"""([^""]+)""\s*:", $"<color={typeColor}>\"$1\"</color>:");
                text = Regex.Replace(text, @":\s*""([^""]*)""", $": <color={stringColor}>\"$1\"</color>");
                text = Regex.Replace(text, @"\b(true|false|null)\b", $"<color={keywordColor}><b>$1</b></color>");
                text = Regex.Replace(text, @"\b(\d+(\.\d+)?)\b", $"<color={numberColor}>$1</color>");
            }
            else
            {
                text = $"<color={defaultColor}>{text}</color>";
            }

            return text;
        }

        private static float _lastMeasuredContainerWidth = 0f;
        private static readonly Dictionary<string, float> _containerWidths = new Dictionary<string, float>();

        /// <summary>
        /// Clears the cached preview container widths (useful when switching layouts or in unit tests).
        /// </summary>
        public static void ClearWidthCache()
        {
            _containerWidths.Clear();
            _lastMeasuredContainerWidth = 0f;
        }

        /// <summary>
        /// Returns the available preview content width for wrapping and height calculation.
        /// </summary>
        public static float GetAvailablePreviewWidth(QuickNote note, float widthOffset = 30f)
        {
            float width = 0f;
            if (_lastMeasuredContainerWidth > 50f)
            {
                width = _lastMeasuredContainerWidth;
            }
            else if (note != null && !string.IsNullOrEmpty(note.id) && _containerWidths.TryGetValue(note.id, out float cachedWidth) && cachedWidth > 50f)
            {
                width = cachedWidth;
            }
            else
            {
                try
                {
                    width = EditorGUIUtility.currentViewWidth;
                }
                catch { }
                if (width < 50f) width = 400f;
            }
            return Mathf.Max(60f, width - widthOffset);
        }

        /// <summary>
        /// Renders a selectable, copyable label with automatic word-wrap height calculation.
        /// </summary>
        public static void DrawSelectableLabel(QuickNote note, string text, GUIStyle style, float widthOffset = 30f, params GUILayoutOption[] options)
        {
            if (text == null) text = "";
            if (Event.current == null) return;

            float availWidth = GetAvailablePreviewWidth(note, widthOffset);
            GUIStyle safeStyle = style ?? EditorStyles.label;
            float height = 18f;
            try
            {
                height = Mathf.Max(safeStyle.lineHeight, safeStyle.CalcHeight(new GUIContent(text), availWidth) + 4f);
            }
            catch
            {
                height = 18f;
            }

            // Intercept ContextClick so Unity SelectableLabel does NOT spawn its default unstyled GenericMenu
            if (Event.current.type == EventType.ContextClick)
            {
                Event.current.Use();
            }

            try
            {
                if (options != null && options.Length > 0)
                {
                    EditorGUILayout.SelectableLabel(text, safeStyle, options);
                }
                else
                {
                    EditorGUILayout.SelectableLabel(text, safeStyle, GUILayout.Height(height), GUILayout.ExpandWidth(true));
                }
            }
            catch { }
        }

        /// <summary>
        /// Renders note content with inline images, headers, bold, italic, inline code,
        /// code blocks, blockquotes, tables, bullet points, checkboxes, and horizontal rules.
        /// Supports multi-line and whole-document text selection and copying throughout all rendered elements.
        /// Returns true while an animated or loading media preview needs repainting.
        /// </summary>
        public static bool DrawMarkdownPreview(QuickNote note, Action<QuickNote> markModified, Action<QuickNote> recordUndo = null)
        {
            if (note == null || string.IsNullOrEmpty(note.content))
            {
                if (Event.current != null)
                {
                    try
                    {
                        EditorGUILayout.LabelField("(empty note — switch to Edit mode to add content)", EditorStyles.centeredGreyMiniLabel);
                    }
                    catch { }
                }
                return false;
            }

            if (Event.current == null)
            {
                return false;
            }

            HandleTableSelectionOutsideEvents(note);

            try
            {
                Rect probeRect = EditorGUILayout.GetControlRect(false, 0, GUILayout.ExpandWidth(true));
                if (probeRect.width > 50f)
                {
                    _lastMeasuredContainerWidth = probeRect.width;
                    if (note != null && !string.IsNullOrEmpty(note.id))
                    {
                        _containerWidths[note.id] = probeRect.width;
                    }
                }
            }
            catch { }

            bool hasAnimatedGif = false;
            string[] lines = note.content.Split('\n');
            var textBuffer = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                string trimmed = line.Trim();

                // ── Code Blocks (```lang ... ```) ──
                if (trimmed.StartsWith("```"))
                {
                    FlushTextBlock(note, textBuffer, markModified);
                    string lang = trimmed.Substring(3).Trim();
                    var codeLines = new List<string>();
                    i++;
                    while (i < lines.Length)
                    {
                        string codeLine = lines[i].TrimEnd('\r');
                        if (codeLine.Trim().StartsWith("```"))
                        {
                            break;
                        }
                        codeLines.Add(codeLine);
                        i++;
                    }
                    string codeBlock = string.Join("\n", codeLines);
                    DrawCodeBlock(note, codeBlock, lang);
                    continue;
                }

                if (TryGetStandaloneYouTubeUrl(trimmed, out string youtubeUrl))
                {
                    FlushTextBlock(note, textBuffer, markModified);
                    if (DrawYouTubeCard(note, youtubeUrl)) hasAnimatedGif = true;
                    continue;
                }

                // ── Markdown Tables (| Col 1 | Col 2 |) ──
                if (IsTableRow(line) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
                {
                    FlushTextBlock(note, textBuffer, markModified);
                    var tableLines = new List<string>();
                    while (i < lines.Length)
                    {
                        string tLine = lines[i].TrimEnd('\r');
                        if (!IsTableRow(tLine))
                        {
                            i--;
                            break;
                        }
                        tableLines.Add(tLine);
                        i++;
                    }
                    DrawTable(note, tableLines);
                    continue;
                }

                // ── Check for image embeds ![[filename.png]] or ![alt](filename.png) ──
                if (_imageEmbedRegex.IsMatch(line))
                {
                    FlushTextBlock(note, textBuffer, markModified);
                    var matches = _imageEmbedRegex.Matches(line);

                    int lastEnd = 0;
                    foreach (Match m in matches)
                    {
                        if (m.Index > lastEnd)
                        {
                            string before = line.Substring(lastEnd, m.Index - lastEnd).Trim();
                            if (!string.IsNullOrEmpty(before))
                            {
                                textBuffer.AppendLine(FormatInlineMarkdown(before));
                                FlushTextBlock(note, textBuffer, markModified);
                            }
                        }

                        string fileName = m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value)
                            ? m.Groups[1].Value.Trim()
                            : m.Groups[3].Value.Trim();

                        if (DrawInlineImage(note, fileName)) hasAnimatedGif = true;
                        lastEnd = m.Index + m.Length;
                    }

                    if (lastEnd < line.Length)
                    {
                        string after = line.Substring(lastEnd).Trim();
                        if (!string.IsNullOrEmpty(after))
                        {
                            textBuffer.AppendLine(FormatInlineMarkdown(after));
                        }
                    }
                    continue;
                }

                // ── Horizontal rule (---, ***, ___) ──
                if (trimmed.Length >= 3 &&
                    (trimmed.Replace("-", "").Trim() == "" ||
                     trimmed.Replace("*", "").Trim() == "" ||
                     trimmed.Replace("_", "").Trim() == ""))
                {
                    textBuffer.AppendLine("<color=#555555>────────────────────────────────────────────</color>");
                    continue;
                }

                // ── Headers (# ...) ──
                if (trimmed.StartsWith("#"))
                {
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    string headerText = trimmed.Substring(level).Trim();
                    int fontSize = level <= 1 ? 17 : level == 2 ? 15 : level == 3 ? 13 : 12;
                    string formattedHeader = FormatInlineMarkdown(headerText);

                    if (textBuffer.Length > 0)
                    {
                        textBuffer.AppendLine();
                    }
                    textBuffer.Append($"<size={fontSize}><b>{formattedHeader}</b></size>");
                    if (level <= 2)
                    {
                        textBuffer.Append("\n<color=#555555>────────────────────────────────────────────</color>");
                    }
                    continue;
                }

                // ── Checkboxes (- [ ] or - [x]) ──
                if (trimmed.StartsWith("- [ ]") || trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]"))
                {
                    FlushTextBlock(note, textBuffer, markModified);
                    
                    bool isChecked = trimmed[3] != ' ';
                    string itemText = trimmed.Substring(5).Trim();
                    string formattedItem = FormatInlineMarkdown(itemText);
                    int indentSpaces = line.Length - line.TrimStart().Length;
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(indentSpaces + 8);
                        if (GUILayout.Button(isChecked ? "☑" : "☐", TBStyles.ChecklistButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            int capturedIndex = i;
                            recordUndo?.Invoke(note);
                            ToggleChecklistLine(note, capturedIndex, markModified);
                            GUIUtility.ExitGUI();
                        }
                        
                        string labelText = isChecked ? $"<color=#888888><i>{formattedItem}</i></color>" : formattedItem;
                        DrawSelectableLabel(note, labelText, TBStyles.PreviewTextStyle, widthOffset: 60f + indentSpaces);
                    }
                    continue;
                }

                // ── Bullet points: - item, * item, + item ──
                if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ")) && trimmed.Length > 2)
                {
                    string bulletText = trimmed.Substring(2).Trim();
                    int indentSpaces = line.Length - line.TrimStart().Length;
                    string indent = indentSpaces > 2 ? "      ◦  " : "  •  ";
                    string formattedBullet = FormatInlineMarkdown(bulletText);
                    
                    if (textBuffer.Length > 0 && !textBuffer.ToString().EndsWith("\n")) textBuffer.AppendLine();
                    textBuffer.Append($"{indent}{formattedBullet}");
                    continue;
                }

                // ── Numbered lists: 1. item ──
                if (trimmed.Length >= 3 && char.IsDigit(trimmed[0]))
                {
                    int dotIdx = trimmed.IndexOf(". ");
                    if (dotIdx > 0 && dotIdx <= 4)
                    {
                        string numPrefix = trimmed.Substring(0, dotIdx + 1);
                        string numText = trimmed.Substring(dotIdx + 2).Trim();
                        string formattedNum = FormatInlineMarkdown(numText);
                        
                        if (textBuffer.Length > 0 && !textBuffer.ToString().EndsWith("\n")) textBuffer.AppendLine();
                        textBuffer.Append($"  <b><color=#569CD6>{numPrefix}</color></b>  {formattedNum}");
                        continue;
                    }
                }

                // ── Blockquotes (> ...) ──
                if (trimmed.StartsWith(">"))
                {
                    string stripped = trimmed.Substring(1).Trim();
                    string formattedQuote = FormatInlineMarkdown(stripped);
                    
                    if (textBuffer.Length > 0 && !textBuffer.ToString().EndsWith("\n")) textBuffer.AppendLine();
                    textBuffer.Append($"  <color=#4EC9B0>│</color> <i><color=#A0A0A0>{formattedQuote}</color></i>");
                    continue;
                }

                // ── Plain text line / paragraph ──
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (textBuffer.Length > 0 && !textBuffer.ToString().EndsWith("\n\n"))
                    {
                        if (textBuffer.ToString().EndsWith("\n")) textBuffer.AppendLine();
                        else textBuffer.Append("\n\n");
                    }
                }
                else
                {
                    if (textBuffer.Length > 0 && !textBuffer.ToString().EndsWith("\n")) textBuffer.Append(" ");
                    textBuffer.Append(FormatInlineMarkdown(line));
                }
            }

            FlushTextBlock(note, textBuffer);
            return hasAnimatedGif;
        }

        private static void DrawCodeBlock(QuickNote note, string rawCode, string language)
        {
            if (string.IsNullOrEmpty(rawCode) || Event.current == null) return;

            string displayLang = string.IsNullOrEmpty(language) ? "CODE" : language.ToUpperInvariant();
            string highlighted = HighlightSyntax(rawCode, language);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // Toolbar Header with language and Copy button
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    string langIcon = displayLang.Contains("CS") || displayLang.Contains("SHARP") ? "💻 C#" :
                                      displayLang.Contains("JSON") ? "📄 JSON" :
                                      displayLang.Contains("SHADER") ? "✨ SHADER" :
                                      $"💻 {displayLang}";

                    EditorGUILayout.LabelField(langIcon, EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));

                    bool isRecentlyCopied = _lastCopiedCode == rawCode && (EditorApplication.timeSinceStartup - _lastCopiedTime < 2.0);
                    string copyBtnText = isRecentlyCopied ? "✓ Copied!" : "📋 Copy";
                    var btnStyle = isRecentlyCopied ? TBStyles.ClipboardSuccessButton : TBStyles.CopyButton;

                    if (GUILayout.Button(copyBtnText, btnStyle, GUILayout.Width(72), GUILayout.Height(18)))
                    {
                        EditorGUIUtility.systemCopyBuffer = rawCode;
                        _lastCopiedCode = rawCode;
                        _lastCopiedTime = EditorApplication.timeSinceStartup;
                    }
                }

                // Code content body - selectable for granular copying
                var codeStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 6, 6)
                };

                DrawSelectableLabel(note, highlighted, codeStyle, widthOffset: 40f);
            }
            GUILayout.Space(4);
        }

        private static void DrawBlockquote(QuickNote note, List<string> quoteLines)
        {
            if (quoteLines == null || quoteLines.Count == 0 || Event.current == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    var quoteStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12,
                        padding = new RectOffset(6, 6, 2, 2)
                    };

                    foreach (var line in quoteLines)
                    {
                        string formatted = FormatInlineMarkdown(line);
                        DrawSelectableLabel(note, formatted, quoteStyle, widthOffset: 48f);
                    }
                }
            }
            GUILayout.Space(2);
        }

        private static bool IsTableRow(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string trimmed = line.Trim();
            return trimmed.StartsWith("|") && trimmed.EndsWith("|") && trimmed.Length >= 2;
        }

        public static bool IsTableSeparator(string line)
        {
            if (!IsTableRow(line)) return false;
            string[] cols = ParseTableRow(line);
            if (cols.Length == 0) return false;
            foreach (string column in cols)
            {
                string trimmed = column.Trim();
                if (!Regex.IsMatch(trimmed, @"^:?-+:?$"))
                    return false;
            }
            return true;
        }

        public static bool TryGetTableSelectionText(QuickNote note, out string selectedText)
        {
            selectedText = "";
            if (!_tableSelectionActive || note == null || note.id != _tableSelectionNoteId || string.IsNullOrEmpty(_tableSelectionText))
                return false;

            selectedText = _tableSelectionText;
            return true;
        }

        public static void ClearTableSelection()
        {
            _tableSelectionNoteId = "";
            _tableSelectionKey = "";
            _tableSelectionAnchorRow = -1;
            _tableSelectionAnchorColumn = -1;
            _tableSelectionFocusRow = -1;
            _tableSelectionFocusColumn = -1;
            _tableSelectionActive = false;
            _tableSelectionDragging = false;
            _tableSelectionRect = default;
            _tableSelectionText = "";
        }

        public static string BuildTableSelectionText(IList<string[]> rows, int anchorRow, int anchorColumn, int focusRow, int focusColumn)
        {
            if (rows == null || rows.Count == 0) return "";

            int rowStart = Mathf.Clamp(Math.Min(anchorRow, focusRow), 0, rows.Count - 1);
            int rowEnd = Mathf.Clamp(Math.Max(anchorRow, focusRow), 0, rows.Count - 1);
            int columnStart = Mathf.Max(0, Math.Min(anchorColumn, focusColumn));
            int columnEnd = Mathf.Max(columnStart, Math.Max(anchorColumn, focusColumn));
            var text = new StringBuilder();
            for (int row = rowStart; row <= rowEnd; row++)
            {
                if (row > rowStart) text.Append('\n');
                for (int column = columnStart; column <= columnEnd; column++)
                {
                    if (column > columnStart) text.Append('\t');
                    if (rows[row] != null && column < rows[row].Length)
                        text.Append(rows[row][column]);
                }
            }
            return text.ToString();
        }

        private static void HandleTableSelectionOutsideEvents(QuickNote note)
        {
            if (!_tableSelectionActive && !_tableSelectionDragging) return;

            Event current = Event.current;
            if (current == null) return;

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                ClearTableSelection();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0 &&
                (note == null || note.id != _tableSelectionNoteId || !_tableSelectionRect.Contains(current.mousePosition)))
            {
                ClearTableSelection();
            }
        }

        private static void UpdateTableSelectionInput(QuickNote note, string tableKey, Rect tableRect, float[] columnWidths,
            float[] rowHeights, List<string[]> rows)
        {
            Event current = Event.current;
            if (current == null) return;

            string noteId = note?.id ?? "";
            bool sameTable = noteId == _tableSelectionNoteId && tableKey == _tableSelectionKey;
            if (current.type == EventType.MouseDown && current.button == 0 && tableRect.Contains(current.mousePosition) &&
                TryGetTableCell(current.mousePosition, tableRect, columnWidths, rowHeights, false, out int row, out int column))
            {
                if (current.shift && sameTable && _tableSelectionActive)
                {
                    _tableSelectionFocusRow = row;
                    _tableSelectionFocusColumn = column;
                    _tableSelectionDragging = false;
                    _tableSelectionText = BuildFormattedTableSelectionText(rows, _tableSelectionAnchorRow, _tableSelectionAnchorColumn, row, column);
                    current.Use();
                }
                else
                {
                    _tableSelectionNoteId = noteId;
                    _tableSelectionKey = tableKey;
                    _tableSelectionAnchorRow = row;
                    _tableSelectionAnchorColumn = column;
                    _tableSelectionFocusRow = row;
                    _tableSelectionFocusColumn = column;
                    _tableSelectionActive = false;
                    _tableSelectionDragging = true;
                    _tableSelectionText = "";
                }
                _tableSelectionRect = tableRect;
                return;
            }

            if (current.type == EventType.MouseDrag && _tableSelectionDragging && sameTable)
            {
                if (TryGetTableCell(current.mousePosition, tableRect, columnWidths, rowHeights, true, out int dragRow, out int dragColumn))
                {
                    _tableSelectionFocusRow = dragRow;
                    _tableSelectionFocusColumn = dragColumn;
                    _tableSelectionActive = dragRow != _tableSelectionAnchorRow || dragColumn != _tableSelectionAnchorColumn;
                    _tableSelectionText = _tableSelectionActive
                        ? BuildFormattedTableSelectionText(rows, _tableSelectionAnchorRow, _tableSelectionAnchorColumn, dragRow, dragColumn)
                        : "";
                    _tableSelectionRect = tableRect;
                    GUIUtility.keyboardControl = 0;
                    GUI.changed = true;
                    EditorWindow.focusedWindow?.Repaint();
                    current.Use();
                }
                return;
            }

            if (current.rawType == EventType.MouseUp && _tableSelectionDragging && sameTable)
                _tableSelectionDragging = false;
        }

        private static bool TryGetTableCell(Vector2 point, Rect tableRect, float[] columnWidths, float[] rowHeights,
            bool clampToTable, out int row, out int column)
        {
            row = -1;
            column = -1;
            if (columnWidths == null || rowHeights == null || columnWidths.Length == 0 || rowHeights.Length == 0)
                return false;

            if (!tableRect.Contains(point) && !clampToTable) return false;
            float x = Mathf.Clamp(point.x, tableRect.x, tableRect.xMax - 0.001f);
            float y = Mathf.Clamp(point.y, tableRect.y, tableRect.yMax - 0.001f);
            float cellY = tableRect.y;
            for (int rowIndex = 0; rowIndex < rowHeights.Length; rowIndex++)
            {
                if (y < cellY + rowHeights[rowIndex])
                {
                    row = rowIndex;
                    break;
                }
                cellY += rowHeights[rowIndex];
            }

            float cellX = tableRect.x;
            for (int columnIndex = 0; columnIndex < columnWidths.Length; columnIndex++)
            {
                if (x < cellX + columnWidths[columnIndex])
                {
                    column = columnIndex;
                    break;
                }
                cellX += columnWidths[columnIndex];
            }
            return row >= 0 && column >= 0;
        }

        private static string BuildFormattedTableSelectionText(List<string[]> rows, int anchorRow, int anchorColumn, int focusRow, int focusColumn)
        {
            var plainRows = new List<string[]>(rows.Count);
            foreach (string[] row in rows)
            {
                var plainRow = new string[row.Length];
                for (int column = 0; column < row.Length; column++)
                    plainRow[column] = StripRichText(FormatInlineMarkdown(row[column]));
                plainRows.Add(plainRow);
            }
            return BuildTableSelectionText(plainRows, anchorRow, anchorColumn, focusRow, focusColumn);
        }

        public static string[] ParseTableRow(string line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            string trimmed = line.Trim();
            if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|") && !trimmed.EndsWith("\\|", StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - 1);

            var cells = new List<string>();
            var cell = new StringBuilder();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char character = trimmed[i];
                if (character == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
                {
                    cell.Append('|');
                    i++;
                }
                else if (character == '|')
                {
                    cells.Add(cell.ToString().Trim());
                    cell.Length = 0;
                }
                else
                {
                    cell.Append(character);
                }
            }
            cells.Add(cell.ToString().Trim());
            return cells.ToArray();
        }

        public static int[] ParseTableAlignments(string separatorRow)
        {
            string[] cells = ParseTableRow(separatorRow);
            var alignments = new int[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                string alignment = cells[i].Trim();
                bool left = alignment.StartsWith(":", StringComparison.Ordinal);
                bool right = alignment.EndsWith(":", StringComparison.Ordinal);
                alignments[i] = left && right ? 1 : right ? 2 : 0;
            }
            return alignments;
        }

        public static float[] FitTableColumnWidths(float[] naturalWidths, float availableWidth, float minimumWidth)
        {
            if (naturalWidths == null || naturalWidths.Length == 0) return Array.Empty<float>();

            int count = naturalWidths.Length;
            float[] widths = new float[count];
            float naturalTotal = 0f;
            for (int i = 0; i < count; i++)
            {
                widths[i] = Mathf.Max(0f, naturalWidths[i]);
                naturalTotal += widths[i];
            }

            availableWidth = Mathf.Max(0f, availableWidth);
            minimumWidth = Mathf.Max(0f, minimumWidth);
            if (naturalTotal <= availableWidth || naturalTotal <= 0f) return widths;
            if (availableWidth <= 0f) return new float[count];

            float minimumTotal = minimumWidth * count;
            if (minimumTotal > availableWidth)
            {
                float scale = availableWidth / naturalTotal;
                for (int i = 0; i < count; i++) widths[i] *= scale;
                ClampTableWidthsToAvailable(widths, availableWidth);
                return widths;
            }

            float flexibleTotal = 0f;
            for (int i = 0; i < count; i++)
                flexibleTotal += Mathf.Max(0f, widths[i] - minimumWidth);

            float flexibleAvailable = availableWidth - minimumTotal;
            if (flexibleTotal <= 0f)
            {
                float equalWidth = availableWidth / count;
                for (int i = 0; i < count; i++) widths[i] = equalWidth;
                ClampTableWidthsToAvailable(widths, availableWidth);
                return widths;
            }

            for (int i = 0; i < count; i++)
            {
                float flexibleWidth = Mathf.Max(0f, widths[i] - minimumWidth);
                widths[i] = minimumWidth + flexibleAvailable * flexibleWidth / flexibleTotal;
            }
            ClampTableWidthsToAvailable(widths, availableWidth);
            return widths;
        }

        private static void ClampTableWidthsToAvailable(float[] widths, float availableWidth)
        {
            float totalWidth = 0f;
            for (int i = 0; i < widths.Length; i++) totalWidth += widths[i];
            if (totalWidth > availableWidth && widths.Length > 0)
                widths[widths.Length - 1] = Mathf.Max(0f, widths[widths.Length - 1] - (totalWidth - availableWidth) - 0.00001f);
        }

        private static void DrawTable(QuickNote note, List<string> tableLines)
        {
            if (tableLines == null || tableLines.Count == 0 || Event.current == null) return;

            var rows = new List<string[]>();
            int maxCols = 0;
            int[] alignments = tableLines.Count > 1 ? ParseTableAlignments(tableLines[1]) : Array.Empty<int>();
            for (int lineIndex = 0; lineIndex < tableLines.Count; lineIndex++)
            {
                if (lineIndex == 1 && IsTableSeparator(tableLines[lineIndex])) continue;
                string[] cells = ParseTableRow(tableLines[lineIndex]);
                if (cells.Length == 0) continue;

                maxCols = Mathf.Max(maxCols, cells.Length);
                rows.Add(cells);
            }

            if (rows.Count == 0 || maxCols == 0) return;

            float availableWidth = GetAvailablePreviewWidth(note, 30f);
            float minimumColumnWidth = Mathf.Min(72f, availableWidth / maxCols);
            var baseStyle = new GUIStyle(TBStyles.PreviewTextStyle)
            {
                wordWrap = true,
                richText = true,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 5, 5)
            };
            ClearTableStyleBackgrounds(baseStyle);

            float[] naturalWidths = new float[maxCols];
            for (int column = 0; column < maxCols; column++)
            {
                naturalWidths[column] = minimumColumnWidth;
                foreach (string[] row in rows)
                {
                    string text = column < row.Length ? FormatInlineMarkdown(row[column]) : "";
                    if (row == rows[0]) text = $"<b>{text}</b>";
                    naturalWidths[column] = Mathf.Max(naturalWidths[column], baseStyle.CalcSize(new GUIContent(text)).x);
                }
            }

            float[] columnWidths = FitTableColumnWidths(naturalWidths, availableWidth, minimumColumnWidth);
            float tableWidth = 0f;
            for (int i = 0; i < columnWidths.Length; i++) tableWidth += columnWidths[i];

            float[] rowHeights = new float[rows.Count];
            float totalHeight = 0f;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                float rowHeight = 0f;
                for (int column = 0; column < maxCols; column++)
                {
                    string text = column < rows[rowIndex].Length ? FormatInlineMarkdown(rows[rowIndex][column]) : "";
                    if (rowIndex == 0) text = $"<b>{text}</b>";
                    var cellStyle = new GUIStyle(baseStyle)
                    {
                        alignment = GetTableTextAnchor(column < alignments.Length ? alignments[column] : 0)
                    };
                    rowHeight = Mathf.Max(rowHeight, cellStyle.CalcHeight(new GUIContent(text), Mathf.Max(1f, columnWidths[column])));
                }
                rowHeights[rowIndex] = Mathf.Max(baseStyle.lineHeight + baseStyle.padding.vertical, rowHeight);
                totalHeight += rowHeights[rowIndex];
            }

            Rect tableRect = GUILayoutUtility.GetRect(tableWidth, totalHeight, GUILayout.Width(tableWidth));
            tableRect.width = tableWidth;
            string tableKey = string.Join("\n", tableLines);
            UpdateTableSelectionInput(note, tableKey, tableRect, columnWidths, rowHeights, rows);
            if (Event.current.type == EventType.Repaint)
            {
                Color background = TBStyles.NoteCardBg;
                Color headerBackground = Color.Lerp(background, TBStyles.NoteSelectedAccentColor, 0.22f);
                Color stripeBackground = TBStyles.NoteCardHoverBg;
                Color gridColor = Color.Lerp(background, TBStyles.CardDetailsTextColor, 0.2f);
                Color accentColor = TBStyles.NoteSelectedAccentColor;
                EditorGUI.DrawRect(tableRect, background);
                float y = tableRect.y;
                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    Rect rowRect = new Rect(tableRect.x, y, tableRect.width, rowHeights[rowIndex]);
                    if (rowIndex == 0) EditorGUI.DrawRect(rowRect, headerBackground);
                    else if (rowIndex % 2 == 0) EditorGUI.DrawRect(rowRect, stripeBackground);
                    y += rowHeights[rowIndex];
                }

                if (_tableSelectionActive && note?.id == _tableSelectionNoteId && tableKey == _tableSelectionKey)
                {
                    _tableSelectionRect = tableRect;
                    int rowStart = Math.Min(_tableSelectionAnchorRow, _tableSelectionFocusRow);
                    int rowEnd = Math.Max(_tableSelectionAnchorRow, _tableSelectionFocusRow);
                    int columnStart = Math.Min(_tableSelectionAnchorColumn, _tableSelectionFocusColumn);
                    int columnEnd = Math.Max(_tableSelectionAnchorColumn, _tableSelectionFocusColumn);
                    float selectionY = tableRect.y;
                    for (int rowIndex = 0; rowIndex < rowStart; rowIndex++) selectionY += rowHeights[rowIndex];
                    float selectionX = tableRect.x;
                    for (int column = 0; column < columnStart; column++) selectionX += columnWidths[column];
                    float selectionWidth = 0f;
                    for (int column = columnStart; column <= columnEnd; column++) selectionWidth += columnWidths[column];
                    float selectionHeight = 0f;
                    for (int rowIndex = rowStart; rowIndex <= rowEnd; rowIndex++) selectionHeight += rowHeights[rowIndex];

                    Color selectionFill = accentColor;
                    selectionFill.a = 0.24f;
                    EditorGUI.DrawRect(new Rect(selectionX, selectionY, selectionWidth, selectionHeight), selectionFill);
                    EditorGUI.DrawRect(new Rect(selectionX, selectionY, selectionWidth, 2f), accentColor);
                    EditorGUI.DrawRect(new Rect(selectionX, selectionY + selectionHeight - 2f, selectionWidth, 2f), accentColor);
                    EditorGUI.DrawRect(new Rect(selectionX, selectionY, 2f, selectionHeight), accentColor);
                    EditorGUI.DrawRect(new Rect(selectionX + selectionWidth - 2f, selectionY, 2f, selectionHeight), accentColor);
                }

                y = tableRect.y;
                for (int rowIndex = 0; rowIndex <= rows.Count; rowIndex++)
                {
                    EditorGUI.DrawRect(new Rect(tableRect.x, y - 0.5f, tableRect.width, 1f), gridColor);
                    if (rowIndex < rows.Count) y += rowHeights[rowIndex];
                }

                float x = tableRect.x;
                EditorGUI.DrawRect(new Rect(x - 0.5f, tableRect.y, 1f, tableRect.height), gridColor);
                for (int column = 0; column < maxCols; column++)
                {
                    x += columnWidths[column];
                    EditorGUI.DrawRect(new Rect(x - 0.5f, tableRect.y, 1f, tableRect.height), gridColor);
                }

                EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y + 0.5f, tableRect.width, 1f), gridColor);
                EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.yMax - 1f, tableRect.width, 1f), gridColor);
                EditorGUI.DrawRect(new Rect(tableRect.x + 0.5f, tableRect.y, 1f, tableRect.height), gridColor);
                EditorGUI.DrawRect(new Rect(tableRect.xMax - 1f, tableRect.y, 1f, tableRect.height), gridColor);

                if (rows.Count > 1)
                {
                    float headerBottom = tableRect.y + rowHeights[0];
                    EditorGUI.DrawRect(new Rect(tableRect.x, headerBottom - 1f, tableRect.width, 2f), accentColor);
                }
            }

            float cellY = tableRect.y;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                float cellX = tableRect.x;
                for (int column = 0; column < maxCols; column++)
                {
                    Rect cellRect = new Rect(cellX, cellY, columnWidths[column], rowHeights[rowIndex]);
                    string text = column < rows[rowIndex].Length ? FormatInlineMarkdown(rows[rowIndex][column]) : "";
                    if (rowIndex == 0) text = $"<b>{text}</b>";
                    var cellStyle = new GUIStyle(baseStyle)
                    {
                        alignment = GetTableTextAnchor(column < alignments.Length ? alignments[column] : 0)
                    };

                    GUISettings settings = GUI.skin != null ? GUI.skin.settings : null;
                    if (settings != null)
                    {
                        Color previousSelectionColor = settings.selectionColor;
                        try
                        {
                            Color selectionColor = TBStyles.NoteSelectedAccentColor;
                            selectionColor.a = 0.65f;
                            settings.selectionColor = selectionColor;
                            EditorGUI.SelectableLabel(cellRect, text, cellStyle);
                        }
                        finally
                        {
                            settings.selectionColor = previousSelectionColor;
                        }
                    }
                    else
                    {
                        EditorGUI.SelectableLabel(cellRect, text, cellStyle);
                    }
                    cellX += columnWidths[column];
                }
                cellY += rowHeights[rowIndex];
            }

            GUILayout.Space(4);
        }

        private static TextAnchor GetTableTextAnchor(int alignment)
        {
            return alignment == 1 ? TextAnchor.MiddleCenter : alignment == 2 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }

        private static void ClearTableStyleBackgrounds(GUIStyle style)
        {
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.onNormal.background = null;
            style.onHover.background = null;
            style.onActive.background = null;
            style.onFocused.background = null;
        }

        private static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        private static void FlushTextBlock(QuickNote note, StringBuilder buffer, Action<QuickNote> onModified = null)
        {
            if (buffer == null || buffer.Length == 0) return;
            string text = buffer.ToString().TrimEnd('\r', '\n', ' ');
            buffer.Clear();
            if (string.IsNullOrWhiteSpace(text) || Event.current == null) return;

            DrawSelectableLabel(note, text, TBStyles.PreviewTextStyle, widthOffset: 30f);
        }

        private static void ToggleChecklistLine(QuickNote note, int lineIndex, Action<QuickNote> onModified)
        {
            if (note == null || string.IsNullOrEmpty(note.content)) return;
            
            string[] lines = note.content.Split('\n');
            if (lineIndex < 0 || lineIndex >= lines.Length) return;

            string line = lines[lineIndex];
            string trimmed = line.TrimStart();
            int indentLen = line.Length - trimmed.Length;
            string indent = line.Substring(0, indentLen);

            if (trimmed.StartsWith("- [ ]"))
            {
                lines[lineIndex] = indent + "- [x]" + trimmed.Substring(5);
            }
            else if (trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]"))
            {
                lines[lineIndex] = indent + "- [ ]" + trimmed.Substring(5);
            }

            note.content = string.Join("\n", lines);
            onModified?.Invoke(note);
        }

        private static void OpenURLWithConfirmation(string url, string title = null)
        {
            if (ThemedDialog.Show("Open URL", BuildOpenUrlMessage(url, title), "Open", "Cancel"))
            {
                Application.OpenURL(url);
            }
        }

        private static bool TryGetStandaloneYouTubeUrl(string line, out string url)
        {
            url = line?.Trim();
            if (string.IsNullOrEmpty(url)) return false;
            if (url.StartsWith("<", StringComparison.Ordinal) && url.EndsWith(">", StringComparison.Ordinal))
                url = url.Substring(1, url.Length - 2).Trim();
            return IsYouTubeUrl(url);
        }

        private static bool DrawInlineImage(QuickNote note, string fileName)
        {
            if (Event.current == null) return false;
            if (IsYouTubeUrl(fileName))
                return DrawYouTubeCard(note, fileName);

            if (IsVideoPath(fileName))
            {
                string videoSource = fileName;
                if (!IsHttpUrl(videoSource))
                {
                    string resolvedVideoPath = null;
                    if (note?.imagePaths != null)
                    {
                        foreach (string path in note.imagePaths)
                        {
                            if (Path.GetFileName(path) == fileName || path == fileName)
                            {
                                resolvedVideoPath = ResolveImageFullPath(path);
                                break;
                            }
                        }
                    }

                    if (resolvedVideoPath == null)
                    {
                        string videoFullPath = ResolveImageFullPath(fileName);
                        if (File.Exists(videoFullPath))
                            resolvedVideoPath = videoFullPath;
                    }

                    if (resolvedVideoPath == null)
                    {
                        string videoGuessPath = $"Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/{Path.GetFileName(fileName)}";
                        string videoFullGuess = ResolveImageFullPath(videoGuessPath);
                        if (File.Exists(videoFullGuess)) resolvedVideoPath = videoFullGuess;
                    }

                    if (resolvedVideoPath == null)
                    {
                        EditorGUILayout.LabelField($"⚠ Missing video: {fileName}", EditorStyles.miniLabel);
                        return false;
                    }
                    videoSource = resolvedVideoPath;
                }

                GUILayout.Space(4);
                bool needsRepaint = NoteVideoPlayer.Draw(note?.id, videoSource);
                GUILayout.Space(4);
                return needsRepaint;
            }

            if (IsHttpUrl(fileName))
                return DrawRemoteImage(fileName);

            string resolvedPath = null;
            if (note.imagePaths != null)
            {
                foreach (var p in note.imagePaths)
                {
                    if (Path.GetFileName(p) == fileName || p == fileName)
                    {
                        resolvedPath = p;
                        break;
                    }
                }
            }

            if (resolvedPath == null)
            {
                string guessPath = $"Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/{fileName}";
                string fullGuess = Path.Combine(Application.dataPath, "..", guessPath);
                if (File.Exists(fullGuess))
                    resolvedPath = guessPath;
            }

            if (resolvedPath == null)
            {
                EditorGUILayout.LabelField($"⚠ Missing image: {fileName}", EditorStyles.miniLabel);
                return false;
            }

            GUILayout.Space(4);
            bool hasGif = DrawImageThumbnail(resolvedPath, 200f);
            GUILayout.Space(4);
            return hasGif;
        }

        private static bool DrawRemoteImage(string url)
        {
            RemoteImageState state = GetRemoteImageState(url);

            if (!string.IsNullOrEmpty(state.assetPath))
            {
                GUILayout.Space(4);
                bool hasGif = DrawImageThumbnail(state.assetPath, 200f);
                GUILayout.Space(4);
                return hasGif;
            }

            if (state.loading)
            {
                EditorGUILayout.LabelField("Loading image…", EditorStyles.miniLabel);
                return true;
            }

            EditorGUILayout.LabelField($"⚠ Could not load image: {state.error ?? url}", EditorStyles.miniLabel);
            return false;
        }

        private static RemoteImageState GetRemoteImageState(string url)
        {
            if (_remoteImageCache.TryGetValue(url, out var state)) return state;

            string hash = GetUrlHash(url);
            state = new RemoteImageState { assetPath = FindExistingRemoteImage(hash) };
            _remoteImageCache[url] = state;
            if (string.IsNullOrEmpty(state.assetPath))
                StartRemoteImageDownload(url, hash, state);
            return state;
        }

        private static Texture2D GetRemoteImageTexture(RemoteImageState state)
        {
            if (state == null || string.IsNullOrEmpty(state.assetPath)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(state.assetPath);
        }

        private static bool DrawYouTubeCard(QuickNote note, string url)
        {
            string videoId = GetYouTubeVideoId(url);
            if (videoId == null) return false;

            string thumbnailUrl = "https://img.youtube.com/vi/" + videoId + "/hqdefault.jpg";
            RemoteImageState thumbnailState = GetRemoteImageState(thumbnailUrl);
            YouTubeTitleState titleState = GetYouTubeTitleState(videoId);
            Texture2D thumbnail = GetRemoteImageTexture(thumbnailState);

            float width = Mathf.Min(GetAvailablePreviewWidth(note, 40f), 480f);
            width = Mathf.Max(120f, width);
            float height = width * 9f / 16f;
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));
            if (thumbnail != null)
                GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleAndCrop);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, thumbnail != null ? 0.35f : 0.72f));

            Rect playRect = new Rect(rect.center.x - 26f, rect.center.y - 26f, 52f, 52f);
            EditorGUI.DrawRect(playRect, new Color(0.8f, 0.05f, 0.05f, 0.95f));
            var playStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(playRect, "▶", playStyle);

            Rect titlePanel = new Rect(rect.x, rect.yMax - 48f, rect.width, 48f);
            EditorGUI.DrawRect(titlePanel, new Color(0f, 0f, 0f, 0.76f));
            string title = titleState.title ?? (titleState.loading ? "Loading YouTube preview…" : "YouTube video");
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(titlePanel.x + 10f, titlePanel.y + 3f, titlePanel.width - 20f, 26f), title, titleStyle);
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(titlePanel.x + 10f, titlePanel.y + 28f, titlePanel.width - 20f, 16f), "▶  YouTube · Click to watch in browser", subtitleStyle);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (GUI.Button(rect, new GUIContent(string.Empty, "Open YouTube video"), GUIStyle.none))
                OpenURLWithConfirmation(url, titleState.title);

            GUILayout.Space(4);
            return thumbnailState.loading || titleState.loading;
        }

        private static YouTubeTitleState GetYouTubeTitleState(string videoId)
        {
            if (_youtubeTitleCache.TryGetValue(videoId, out var state)) return state;
            state = new YouTubeTitleState { loading = true };
            _youtubeTitleCache[videoId] = state;

            UnityWebRequest request = null;
            try
            {
                string videoUrl = "https://www.youtube.com/watch?v=" + videoId;
                string oEmbedUrl = "https://www.youtube.com/oembed?url=" + Uri.EscapeDataString(videoUrl) + "&format=json";
                request = UnityWebRequest.Get(oEmbedUrl);
                request.timeout = 10;
                state.request = request;
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += _ =>
                {
                    try
                    {
                        if (request.result == UnityWebRequest.Result.Success)
                            state.title = ExtractOEmbedTitle(request.downloadHandler?.text);
                    }
                    finally
                    {
                        state.loading = false;
                        state.request = null;
                        request.Dispose();
                        EditorApplication.delayCall += () => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
                };
            }
            catch
            {
                state.loading = false;
                state.request = null;
                request?.Dispose();
            }
            return state;
        }

        private static void StartRemoteImageDownload(string url, string hash, RemoteImageState state)
        {
            UnityWebRequest request = null;
            try
            {
                request = UnityWebRequest.Get(url);
                request.timeout = 20;
                state.request = request;
                state.loading = true;
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += _ =>
                {
                    try
                    {
                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            state.error = request.error ?? "download failed";
                            return;
                        }

                        byte[] bytes = request.downloadHandler?.data;
                        if (bytes == null || bytes.LongLength > GifDecoder.MaxFileSize)
                        {
                            state.error = "image is empty or too large";
                            return;
                        }

                        string extension = DetectImageExtension(bytes);
                        if (extension == null)
                        {
                            state.error = "unsupported image data";
                            return;
                        }

                        state.assetPath = SaveImageBytesToProject(bytes, "remote_" + hash);
                        if (string.IsNullOrEmpty(state.assetPath))
                            state.error = "could not save image";
                    }
                    catch (Exception e)
                    {
                        state.error = e.Message;
                    }
                    finally
                    {
                        state.loading = false;
                        state.request = null;
                        request.Dispose();
                        EditorApplication.delayCall += () => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
                };
            }
            catch (Exception e)
            {
                state.loading = false;
                state.error = e.Message;
                state.request = null;
                request?.Dispose();
            }
        }

        private static string FindExistingRemoteImage(string hash)
        {
            string directory = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            foreach (string extension in new[] { ".gif", ".png", ".jpg" })
            {
                string fileName = "remote_" + hash + extension;
                string fullPath = Path.Combine(directory, fileName);
                if (File.Exists(fullPath))
                    return "Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/" + fileName;
            }
            return null;
        }

        private static string GetUrlHash(string url)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool DrawImageThumbnail(string imagePath, float maxHeight)
        {
            if (string.IsNullOrEmpty(imagePath)) return false;

            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (ext == ".gif")
            {
                return DrawAnimatedGif(imagePath, maxHeight);
            }

            Texture2D tex = null;
            if (_imageCache.TryGetValue(imagePath, out var cached) && cached != null)
            {
                tex = cached;
            }
            else
            {
                string assetPath = imagePath.Replace('\\', '/');
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (assetPath.StartsWith(dataPath))
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);

                if (assetPath.StartsWith("Assets"))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }

                if (tex == null && File.Exists(imagePath))
                {
                    try
                    {
                        if (ext == ".psd" || ext == ".tiff" || ext == ".tif" || ext == ".bmp")
                        {
                            string newAssetPath = CopyImageToProject(imagePath);
                            if (!string.IsNullOrEmpty(newAssetPath) && newAssetPath.StartsWith("Assets"))
                                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath);
                        }
                        else
                        {
                            byte[] data = File.ReadAllBytes(imagePath);
                            tex = new Texture2D(2, 2);
                            if (!tex.LoadImage(data))
                            {
                                UnityEngine.Object.DestroyImmediate(tex);
                                tex = null;
                            }
                            else
                            {
                                tex.hideFlags = HideFlags.DontSave;
                            }
                        }
                    }
                    catch { tex = null; }
                }

                if (tex != null)
                    _imageCache[imagePath] = tex;
            }

            if (tex != null)
            {
                DrawTexture(tex, maxHeight, imagePath);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠ Image not found: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
            }
            return false;
        }

        private static bool DrawAnimatedGif(string imagePath, float maxHeight)
        {
            if (_failedGifPaths.Contains(imagePath))
            {
                EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                return false;
            }

            string assetPath = imagePath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets"))
            {
                string fullPath = ResolveImageFullPath(imagePath);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    try { assetPath = CopyImageToProject(fullPath); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[TaskBoard] Failed to copy GIF: {e.Message}");
                        _failedGifPaths.Add(imagePath);
                        EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                        return false;
                    }
                }
            }

            if (!_gifCache.TryGetValue(assetPath, out var gif))
            {
                try
                {
                    string fullPath = assetPath.StartsWith("Assets")
                        ? Path.Combine(Application.dataPath, "..", assetPath)
                        : assetPath;

                    gif = GifDecoder.Load(fullPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TaskBoard] Failed to decode GIF: {e.Message}");
                    gif = null;
                }
                _gifCache[assetPath] = gif; // cache even if null to avoid retrying
            }

            if (gif != null && gif.FrameCount > 0)
            {
                int frameIdx = gif.GetFrameIndex(EditorApplication.timeSinceStartup);
                var frame = gif.Frames[frameIdx];
                if (frame.texture != null)
                {
                    DrawTexture(frame.texture, maxHeight, imagePath);
                    return gif.FrameCount > 1; // Indicates we need repaint for next frame if animated
                }
            }

            return false;
        }

        private static void DrawTexture(Texture2D tex, float maxHeight, string imagePath = null)
        {
            float aspect = (float)tex.width / tex.height;
            float targetHeight = Mathf.Min(tex.height, maxHeight);
            float targetWidth = targetHeight * aspect;

            float availableWidth = GetAvailablePreviewWidth(null, 40f);
            if (targetWidth > availableWidth)
            {
                targetWidth = availableWidth;
                targetHeight = targetWidth / aspect;
            }

            var rect = GUILayoutUtility.GetRect(targetWidth, targetHeight, GUILayout.Width(targetWidth), GUILayout.Height(targetHeight));
            
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (!string.IsNullOrEmpty(imagePath))
                    ImageLargePreviewWindow.ShowWindow(imagePath);
                else
                    ImageLargePreviewWindow.ShowWindow(tex);
                
                Event.current.Use();
            }

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        }

        private static string ResolveImageFullPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            if (imagePath.StartsWith("Assets"))
                return Path.Combine(Application.dataPath, "..", imagePath).Replace('/', Path.DirectorySeparatorChar);
            return imagePath;
        }

        /// <summary>
        /// Copies an external image into Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/
        /// so Unity's asset importer handles it (supports GIF, PSD, etc).
        /// Returns the Assets/... path, or the original path if already inside Assets.
        /// </summary>
        public static string CopyImageToProject(string externalPath)
        {
            if (string.IsNullOrEmpty(externalPath) || !File.Exists(externalPath)) return externalPath;

            // Already inside Assets?
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedInput = externalPath.Replace('\\', '/');
            if (normalizedInput.StartsWith(dataPath))
                return "Assets" + normalizedInput.Substring(dataPath.Length);

            // Copy into project
            string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            string fileName = Path.GetFileName(externalPath);
            string destPath = Path.Combine(destDir, fileName);

            // Avoid overwriting — add unique suffix if needed
            if (File.Exists(destPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                destPath = Path.Combine(destDir, $"{nameNoExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            }

            File.Copy(externalPath, destPath, false);

            // Import immediately to get a valid asset path and GUID
            string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPath.Length);
            AssetDatabase.ImportAsset(assetPath);

            return assetPath;
        }

        private static void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        #region Clipboard & Drag-and-Drop Support

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterClipboardFormat(string formatName);
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint format);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        private const uint CF_DIB = 8;
        private const uint CF_HDROP = 15;
#endif

        public static void PasteImageFromClipboard(QuickNote note, Action<QuickNote> markModified, Action repaint)
        {
            if (TryPasteImageFromClipboard(note, markModified, repaint))
                return;
            ThemedDialog.Show("No Media", "No image or video file found on the clipboard.\n\nTip: Copy an image or video file first, then paste.", "OK");
        }

        /// <summary>
        /// Smart paste helper that inspects system clipboard:
        /// - If clipboard has an image or video file, copies/saves it to AttachedImages and appends/inserts an embed
        /// - Else if clipboard has text, appends/inserts the text at insertIndex (or end if negative)
        /// Returns true if something was pasted.
        /// </summary>
        public static bool SmartPasteToNote(QuickNote note, Action<QuickNote> markModified, Action repaint, out string feedback, int insertIndex = -1, int replaceLength = 0)
        {
            feedback = "";
            if (note == null) return false;

            // 1. Try pasting image
            string assetPath = TryPasteImageToProject(true);
            if (!string.IsNullOrEmpty(assetPath))
            {
                note.imagePaths ??= new List<string>();
                if (!note.imagePaths.Contains(assetPath))
                    note.imagePaths.Add(assetPath);

                string fn = Path.GetFileName(assetPath);
                string embedTag = $"![[{fn}]]";
                string curContent = (note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");

                if (insertIndex >= 0 && insertIndex <= curContent.Length)
                {
                    if (replaceLength < 0 || insertIndex + replaceLength > curContent.Length)
                        replaceLength = 0;
                    note.content = curContent.Substring(0, insertIndex) + embedTag + curContent.Substring(insertIndex + replaceLength);
                }
                else if (string.IsNullOrEmpty(curContent))
                {
                    note.content = embedTag;
                }
                else
                {
                    note.content = curContent.TrimEnd() + "\n\n" + embedTag;
                }
                markModified?.Invoke(note);
                repaint?.Invoke();
                feedback = "Media pasted";
                return true;
            }

            // 2. Try pasting text
            string clipText = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipText))
                clipText = EditorGUIUtility.systemCopyBuffer;

            if (!string.IsNullOrEmpty(clipText))
            {
                clipText = clipText.Replace("\r\n", "\n").Replace("\r", "\n");
                string curContent = (note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
                if (insertIndex >= 0 && insertIndex <= curContent.Length)
                {
                    if (replaceLength < 0 || insertIndex + replaceLength > curContent.Length)
                        replaceLength = 0;
                    note.content = curContent.Substring(0, insertIndex) + clipText + curContent.Substring(insertIndex + replaceLength);
                }
                else if (string.IsNullOrEmpty(curContent))
                {
                    note.content = clipText;
                }
                else
                {
                    note.content = curContent.TrimEnd() + "\n" + clipText;
                }
                markModified?.Invoke(note);
                repaint?.Invoke();
                feedback = "Text pasted";
                return true;
            }

            feedback = "Clipboard empty";
            return false;
        }

        public static bool SmartPasteToNote(QuickNote note, Action<QuickNote> markModified, Action repaint, int insertIndex = -1, int replaceLength = 0)
        {
            return SmartPasteToNote(note, markModified, repaint, out _, insertIndex, replaceLength);
        }

        public static bool TryPasteImageFromClipboard(QuickNote note, Action<QuickNote> markModified, Action repaint, int insertIndex = -1, int replaceLength = 0)
        {
            string assetPath = TryPasteImageToProject(true);
            if (string.IsNullOrEmpty(assetPath)) return false;

            note.imagePaths ??= new List<string>();
            if (!note.imagePaths.Contains(assetPath))
                note.imagePaths.Add(assetPath);

            string fn = Path.GetFileName(assetPath);
            string embedTag = $"![[{fn}]]";
            string curContent = (note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");

            if (insertIndex >= 0 && insertIndex <= curContent.Length)
            {
                if (replaceLength < 0 || insertIndex + replaceLength > curContent.Length)
                    replaceLength = 0;
                note.content = curContent.Substring(0, insertIndex) + embedTag + curContent.Substring(insertIndex + replaceLength);
            }
            else if (string.IsNullOrEmpty(curContent))
            {
                note.content = embedTag;
            }
            else
            {
                note.content = curContent.TrimEnd() + $"\n![[{fn}]]";
            }
            markModified?.Invoke(note);
            repaint?.Invoke();
            return true;
        }

        public static string TryPasteImageToProject()
        {
            return TryPasteImageToProject(false);
        }

        public static string TryPasteImageToProject(bool includeVideo)
        {
#if UNITY_EDITOR_WIN
            string htmlImageSource = null;
            try
            {
                uint htmlFormat = RegisterClipboardFormat("HTML Format");
                if (htmlFormat != 0 && OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        if (IsClipboardFormatAvailable(htmlFormat))
                        {
                            string html = ReadClipboardText(htmlFormat);
                            htmlImageSource = ExtractImageSrcFromHtml(html);
                        }
                    }
                    finally { CloseClipboard(); }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TaskBoard] HTML clipboard read failed: " + e.Message);
            }

            if (!string.IsNullOrEmpty(htmlImageSource) &&
                (IsGifUrl(htmlImageSource) || htmlImageSource.StartsWith("data:image/gif;base64,", StringComparison.OrdinalIgnoreCase)))
            {
                string pastedGifPath = DownloadImageToProject(htmlImageSource);
                if (!string.IsNullOrEmpty(pastedGifPath)) return pastedGifPath;
            }

            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        // Check for file drop (user copied a file in Explorer)
                        if (IsClipboardFormatAvailable(CF_HDROP))
                        {
                            var hDrop = GetClipboardData(CF_HDROP);
                            if (hDrop != IntPtr.Zero)
                            {
                                uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                                for (uint i = 0; i < fileCount; i++)
                                {
                                    var sb = new StringBuilder(260);
                                    DragQueryFile(hDrop, i, sb, 260);
                                    string filePath = sb.ToString();
                                    if (IsNoteMediaFile(filePath, includeVideo))
                                    {
                                        return CopyImageToProject(filePath);
                                    }
                                }
                            }
                        }

                        // Check for bitmap data (user did PrintScreen or copied from image editor)
                        if (IsClipboardFormatAvailable(CF_DIB))
                        {
                            var hMem = GetClipboardData(CF_DIB);
                            if (hMem != IntPtr.Zero)
                            {
                                int size = GlobalSize(hMem);
                                var ptr = GlobalLock(hMem);
                                if (ptr != IntPtr.Zero && size > 40)
                                {
                                    try
                                    {
                                        byte[] dibData = new byte[size];
                                        Marshal.Copy(ptr, dibData, 0, size);

                                        // Parse DIB header directly into a Texture2D
                                        Texture2D tex = CreateTextureFromDib(dibData);
                                        if (tex != null)
                                        {
                                            byte[] pngBytes = tex.EncodeToPNG();
                                            UnityEngine.Object.DestroyImmediate(tex);

                                            string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
                                            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                                            string fileName = $"pasted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                                            string destPath = Path.Combine(destDir, fileName);
                                            File.WriteAllBytes(destPath, pngBytes);

                                            string dataPathNorm = Application.dataPath.Replace('\\', '/');
                                            string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPathNorm.Length);
                                            AssetDatabase.ImportAsset(assetPath);

                                            return assetPath;
                                        }
                                    }
                                    finally { GlobalUnlock(hMem); }
                                }
                            }
                        }
                    }
                    finally { CloseClipboard(); }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TaskBoard] Clipboard paste failed: " + e.Message);
            }
#endif
            // Fallback: check if clipboard text is a file path to an image
            string clipText = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipText))
            {
                clipText = clipText.Trim().Trim('"');
                if (IsGifUrl(clipText) || clipText.StartsWith("data:image/gif;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    string pastedGifPath = DownloadImageToProject(clipText);
                    if (!string.IsNullOrEmpty(pastedGifPath)) return pastedGifPath;
                }

                if (File.Exists(clipText))
                {
                    if (IsNoteMediaFile(clipText, includeVideo))
                    {
                        return CopyImageToProject(clipText);
                    }
                }
            }
            return null;
        }

        private static bool IsNoteMediaFile(string path, bool includeVideo)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return Array.Exists(ImageExtensions, e => e == extension) ||
                   (includeVideo && Array.Exists(VideoExtensions, e => e == extension));
        }

#if UNITY_EDITOR_WIN
        private static string ReadClipboardText(uint format)
        {
            IntPtr hMem = GetClipboardData(format);
            if (hMem == IntPtr.Zero) return null;

            int size = GlobalSize(hMem);
            if (size <= 0 || size > 10 * 1024 * 1024) return null;

            IntPtr ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero) return null;
            try
            {
                byte[] data = new byte[size];
                Marshal.Copy(ptr, data, 0, size);
                return Encoding.UTF8.GetString(data).TrimEnd('\0');
            }
            finally { GlobalUnlock(hMem); }
        }
#endif

        /// <summary>Handle Unity editor drag-and-drop of image and video files onto note editor area.</summary>
        public static void HandleNoteDragDropImages(QuickNote note, Action<QuickNote> markModified, Action repaint)
        {
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasImage = false;
                if (DragAndDrop.paths != null)
                {
                    foreach (var p in DragAndDrop.paths)
                    {
                        if (IsNoteMediaFile(p, true))
                        { hasImage = true; break; }
                    }
                }

                if (hasImage)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        note.imagePaths ??= new List<string>();
                        foreach (var p in DragAndDrop.paths)
                        {
                            if (IsNoteMediaFile(p, true))
                            {
                                string assetPath = CopyImageToProject(p);
                                if (!note.imagePaths.Contains(assetPath))
                                    note.imagePaths.Add(assetPath);
                                string fn = Path.GetFileName(assetPath);
                                note.content = (note.content ?? "") + $"\n![[{fn}]]";
                            }
                        }
                        markModified?.Invoke(note);
                        repaint?.Invoke();
                    }
                    evt.Use();
                }
            }
        }

#if UNITY_EDITOR_WIN
        private static Texture2D CreateTextureFromDib(byte[] dibData)
        {
            if (dibData == null || dibData.Length < 40) return null;

            int headerSize = BitConverter.ToInt32(dibData, 0);
            int width = BitConverter.ToInt32(dibData, 4);
            int rawHeight = BitConverter.ToInt32(dibData, 8);
            int bitCount = BitConverter.ToInt16(dibData, 14);
            int compression = BitConverter.ToInt32(dibData, 16);

            if (width <= 0 || rawHeight == 0) return null;
            if (bitCount != 24 && bitCount != 32) return null;
            if (compression != 0 && compression != 3) return null;

            bool bottomUp = rawHeight > 0;
            int absHeight = Math.Abs(rawHeight);

            int extraOffset = 0;
            if (compression == 3) extraOffset = 12;

            int pixelDataStart = headerSize + extraOffset;
            int bytesPerPixel = bitCount / 8;
            int rowStride = ((width * bytesPerPixel + 3) / 4) * 4;

            if (pixelDataStart + rowStride * absHeight > dibData.Length)
            {
                pixelDataStart = headerSize;
                if (pixelDataStart + rowStride * absHeight > dibData.Length) return null;
            }

            var tex = new Texture2D(width, absHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * absHeight];

            for (int y = 0; y < absHeight; y++)
            {
                int srcRow = bottomUp ? y : (absHeight - 1 - y);
                int srcRowStart = pixelDataStart + srcRow * rowStride;

                for (int x = 0; x < width; x++)
                {
                    int srcIdx = srcRowStart + x * bytesPerPixel;
                    if (srcIdx + bytesPerPixel > dibData.Length) break;

                    byte b = dibData[srcIdx];
                    byte g = dibData[srcIdx + 1];
                    byte r = dibData[srcIdx + 2];
                    byte a = bytesPerPixel == 4 ? dibData[srcIdx + 3] : (byte)255;
                    if (bytesPerPixel == 4 && a == 0) a = 255;

                    pixels[y * width + x] = new Color32(r, g, b, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }
#endif

        #endregion
    }
}
