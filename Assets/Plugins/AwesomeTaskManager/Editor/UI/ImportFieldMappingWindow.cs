using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    public enum ImportMappingScope
    {
        Any,
        Board,
        Column,
        Card
    }

    public enum ImportMappingPreset
    {
        Generic,
        ClickUp,
        Trello
    }

    [Serializable]
    public class ImportFieldMapping
    {
        public int nameIndex = -1;
        public int descriptionIndex = -1;
        public int statusIndex = -1;
        public int priorityIndex = -1;
        public int assigneeIndex = -1;
        public int tagsIndex = -1;
        public int dueDateIndex = -1;
        public int checklistIndex = -1;
        public int listNameIndex = -1;
        public int customFieldsIndex = -1;
        public ImportMappingPreset preset = ImportMappingPreset.Generic;

        public ImportFieldMapping Clone()
        {
            return (ImportFieldMapping)MemberwiseClone();
        }

        public void Normalize()
        {
            nameIndex = Mathf.Max(-1, nameIndex);
            descriptionIndex = Mathf.Max(-1, descriptionIndex);
            statusIndex = Mathf.Max(-1, statusIndex);
            priorityIndex = Mathf.Max(-1, priorityIndex);
            assigneeIndex = Mathf.Max(-1, assigneeIndex);
            tagsIndex = Mathf.Max(-1, tagsIndex);
            dueDateIndex = Mathf.Max(-1, dueDateIndex);
            checklistIndex = Mathf.Max(-1, checklistIndex);
            listNameIndex = Mathf.Max(-1, listNameIndex);
            customFieldsIndex = Mathf.Max(-1, customFieldsIndex);

            if (!Enum.IsDefined(typeof(ImportMappingPreset), preset))
                preset = ImportMappingPreset.Generic;
        }
    }

    [Serializable]
    public class ImportFieldMappingProfile
    {
        public string id = Guid.NewGuid().ToString();
        public bool isBuiltIn;
        public ImportMappingScope scope = ImportMappingScope.Any;
        public string profileName = "Import Mapping";
        public string sourceFilePattern = string.Empty;
        public string headerSignature = string.Empty;
        public bool rememberLastMappingForPattern;
        public long lastUsedUtcTicks;
        public ImportFieldMapping mapping = new ImportFieldMapping();

        public ImportFieldMappingProfile Clone()
        {
            return new ImportFieldMappingProfile
            {
                id = id,
                isBuiltIn = isBuiltIn,
                scope = scope,
                profileName = profileName,
                sourceFilePattern = sourceFilePattern,
                headerSignature = headerSignature,
                rememberLastMappingForPattern = rememberLastMappingForPattern,
                lastUsedUtcTicks = lastUsedUtcTicks,
                mapping = mapping?.Clone() ?? new ImportFieldMapping()
            };
        }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = Guid.NewGuid().ToString();

            if (isBuiltIn && string.IsNullOrWhiteSpace(id))
                id = profileName;

            if (!Enum.IsDefined(typeof(ImportMappingScope), scope))
                scope = ImportMappingScope.Any;

            profileName = string.IsNullOrWhiteSpace(profileName)
                ? "Import Mapping"
                : profileName.Trim();

            sourceFilePattern = ImportFieldMappingPresets.NormalizePattern(sourceFilePattern);
            headerSignature = ImportFieldMappingPresets.NormalizeHeaderSignature(headerSignature);
            mapping ??= new ImportFieldMapping();
            mapping.Normalize();
        }

        public bool MatchesScope(ImportMappingScope requestedScope)
        {
            return scope == ImportMappingScope.Any || scope == requestedScope;
        }

        public bool MatchesSourcePath(string sourcePath)
        {
            if (!rememberLastMappingForPattern || string.IsNullOrWhiteSpace(sourceFilePattern)) return false;

            string fullPath = sourcePath ?? string.Empty;
            string fileName = Path.GetFileName(fullPath);
            return ImportFieldMappingPresets.WildcardMatches(fileName, sourceFilePattern)
                || ImportFieldMappingPresets.WildcardMatches(fullPath, sourceFilePattern);
        }

        public bool MatchesHeaderSignature(string sourceHeaderSignature)
        {
            string normalized = ImportFieldMappingPresets.NormalizeHeaderSignature(sourceHeaderSignature);
            return !string.IsNullOrWhiteSpace(headerSignature)
                && !string.IsNullOrWhiteSpace(normalized)
                && string.Equals(headerSignature, normalized, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ImportFieldMappingWindowResult
    {
        public ImportMappingScope scope;
        public ImportFieldMapping mapping;
        public string selectedProfileId;
        public string profileName;
        public bool saveProfile;
        public bool rememberLastMappingForPattern;
        public string sourceFilePattern;
        public string headerSignature;
        public string deleteProfileId;
    }

    public static class ImportFieldMappingPresets
    {
        private static readonly string[] AtmClickUpHeaders =
        {
            "Task ID", "Task Link", "Task Type", "Task Custom ID", "Task Name", "Task Content",
            "Status", "Date created", "Date created Text", "Due date", "Due date Text", "Start date",
            "Start date Text", "Parent ID", "Subtask IDs", "Attachments", "Assignees", "Tags",
            "Priority", "List Name", "Space Name", "Time Estimated", "Time Estimated Text",
            "Checklists", "Comments", "Assigned Comments", "Time Spent", "Time Spent Text",
            "Rolled Up Time", "Rolled Up Time Text"
        };

        private static readonly string[] TrelloHeaders =
        {
            "Card Name", "Description", "List Name", "Members", "Labels", "Due", "Checklist",
            "Priority", "Board Name", "Custom Fields"
        };

        private static readonly string[] JiraHeaders =
        {
            "Issue key", "Summary", "Issue Type", "Status", "Priority", "Assignee", "Labels",
            "Due date", "Description", "Project"
        };

        private static readonly string[] AsanaHeaders =
        {
            "Task ID", "Task Name", "Notes", "Section", "Priority", "Assignee", "Tags",
            "Due Date", "Project", "Custom Fields"
        };

        private static readonly string[] MondayHeaders =
        {
            "Item", "Status", "Owner", "Priority", "Due Date", "Tags", "Board", "Updates", "Description"
        };

        public static ImportFieldMapping AutoDetect(string[] headers)
        {
            var trello = Trello(headers);
            var clickUp = ClickUp(headers);

            if (trello.nameIndex >= 0 && trello.statusIndex >= 0)
                return trello;
            if (clickUp.nameIndex >= 0 && clickUp.statusIndex >= 0)
                return clickUp;

            var generic = Generic(headers);
            if (generic.nameIndex < 0)
                generic.nameIndex = FindFirstHeaderIndex(headers, "Task Name", "Card Name", "Name", "Title");
            return generic;
        }

        public static ImportFieldMapping Generic(string[] headers)
        {
            return new ImportFieldMapping
            {
                preset = ImportMappingPreset.Generic,
                nameIndex = FindFirstHeaderIndex(headers, "Name", "Task Name", "Card Name", "Title"),
                descriptionIndex = FindFirstHeaderIndex(headers, "Description", "Task Content"),
                statusIndex = FindFirstHeaderIndex(headers, "Status", "List", "List Name"),
                priorityIndex = FindFirstHeaderIndex(headers, "Priority"),
                assigneeIndex = FindFirstHeaderIndex(headers, "Assignees", "Members"),
                tagsIndex = FindFirstHeaderIndex(headers, "Tags", "Labels"),
                dueDateIndex = FindFirstHeaderIndex(headers, "Due date", "Due Date", "Due"),
                checklistIndex = FindFirstHeaderIndex(headers, "Checklists", "Checklist"),
                listNameIndex = FindFirstHeaderIndex(headers, "List Name", "Board Name", "Board"),
                customFieldsIndex = FindFirstHeaderIndex(headers, "Custom Fields", "Custom Field")
            };
        }

        public static ImportFieldMapping ClickUp(string[] headers)
        {
            return new ImportFieldMapping
            {
                preset = ImportMappingPreset.ClickUp,
                nameIndex = FindFirstHeaderIndex(headers, "Task Name", "Name"),
                descriptionIndex = FindFirstHeaderIndex(headers, "Task Content", "Description"),
                statusIndex = FindFirstHeaderIndex(headers, "Status"),
                priorityIndex = FindFirstHeaderIndex(headers, "Priority"),
                assigneeIndex = FindFirstHeaderIndex(headers, "Assignees"),
                tagsIndex = FindFirstHeaderIndex(headers, "Tags"),
                dueDateIndex = FindFirstHeaderIndex(headers, "Due date", "Due Date"),
                checklistIndex = FindFirstHeaderIndex(headers, "Checklists"),
                listNameIndex = FindFirstHeaderIndex(headers, "List Name"),
                customFieldsIndex = FindFirstHeaderIndex(headers, "Custom Fields")
            };
        }

        public static ImportFieldMapping Trello(string[] headers)
        {
            return new ImportFieldMapping
            {
                preset = ImportMappingPreset.Trello,
                nameIndex = FindFirstHeaderIndex(headers, "Card Name", "Name"),
                descriptionIndex = FindFirstHeaderIndex(headers, "Description", "Card Description"),
                statusIndex = FindFirstHeaderIndex(headers, "List Name", "List"),
                priorityIndex = FindFirstHeaderIndex(headers, "Priority"),
                assigneeIndex = FindFirstHeaderIndex(headers, "Members", "Assignees"),
                tagsIndex = FindFirstHeaderIndex(headers, "Labels", "Tags"),
                dueDateIndex = FindFirstHeaderIndex(headers, "Due", "Due Date"),
                checklistIndex = FindFirstHeaderIndex(headers, "Checklist", "Checklists"),
                listNameIndex = FindFirstHeaderIndex(headers, "Board", "Board Name"),
                customFieldsIndex = FindFirstHeaderIndex(headers, "Custom Fields", "Custom Field")
            };
        }

        public static int FindFirstHeaderIndex(string[] headers, params string[] candidates)
        {
            if (headers == null || headers.Length == 0 || candidates == null || candidates.Length == 0) return -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string h = (headers[i] ?? string.Empty).Trim();
                foreach (var candidate in candidates)
                {
                    if (string.Equals(h, candidate, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }

        public static string NormalizePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return string.Empty;

            string normalized = pattern.Trim();
            normalized = Regex.Replace(normalized, @"\*{2,}", "*");
            return normalized;
        }

        public static string BuildSuggestedPattern(string sourcePath)
        {
            string fileName = Path.GetFileName(sourcePath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName)) return "*.*";

            string extension = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(name)) return "*" + extension;

            name = Regex.Replace(name, @"\d+", "*");
            name = Regex.Replace(name, @"([\s._-]*\*)+", "*");
            name = Regex.Replace(name, @"\*{2,}", "*").Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "*";

            return NormalizePattern(name + extension);
        }

        public static string NormalizeHeaderSignature(string headerSignature)
        {
            if (string.IsNullOrWhiteSpace(headerSignature)) return string.Empty;

            string normalized = headerSignature.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"\s+", " ");
            normalized = Regex.Replace(normalized, @"\s*\|\s*", "|");
            return normalized;
        }

        public static string BuildHeaderSignature(string[] headers)
        {
            if (headers == null || headers.Length == 0) return string.Empty;

            var normalized = headers
                .Select(NormalizeHeaderToken)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            if (normalized.Length == 0) return string.Empty;
            return NormalizeHeaderSignature(normalized.Length + ":" + string.Join("|", normalized));
        }

        public static string NormalizeHeaderToken(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return string.Empty;

            string normalized = header.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized;
        }

        public static bool WildcardMatches(string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return false;
            input ??= string.Empty;

            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".") + "$";

            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }

        private static ImportFieldMappingProfile CreateBuiltInProfile(string id, string profileName, ImportMappingScope scope, ImportFieldMapping mapping, string[] headers)
        {
            return CreateBuiltInProfile(id, profileName, scope, mapping, headers, false, string.Empty);
        }

        private static ImportFieldMappingProfile CreateBuiltInProfile(string id, string profileName, ImportMappingScope scope, ImportFieldMapping mapping, string[] headers, bool rememberPattern, string pattern)
        {
            mapping ??= new ImportFieldMapping();
            mapping.Normalize();

            return new ImportFieldMappingProfile
            {
                id = id,
                isBuiltIn = true,
                scope = scope,
                profileName = profileName,
                sourceFilePattern = pattern,
                headerSignature = BuildHeaderSignature(headers),
                rememberLastMappingForPattern = rememberPattern,
                mapping = mapping
            };
        }

        private static ImportFieldMapping FromPreset(ImportFieldMapping preset)
        {
            var clone = preset?.Clone() ?? new ImportFieldMapping();
            clone.Normalize();
            return clone;
        }

        public static List<ImportFieldMappingProfile> GetBuiltInProfiles(ImportMappingScope scope)
        {
            var profiles = new List<ImportFieldMappingProfile>
            {
                CreateBuiltInProfile(
                    "builtin-atm-clickup",
                    "AwesomeTaskManager CSV",
                    ImportMappingScope.Any,
                    FromPreset(ClickUp(AtmClickUpHeaders)),
                    AtmClickUpHeaders),
                CreateBuiltInProfile(
                    "builtin-clickup",
                    "ClickUp Export",
                    ImportMappingScope.Any,
                    FromPreset(ClickUp(AtmClickUpHeaders)),
                    AtmClickUpHeaders),
                CreateBuiltInProfile(
                    "builtin-trello",
                    "Trello Export",
                    ImportMappingScope.Any,
                    FromPreset(Trello(TrelloHeaders)),
                    TrelloHeaders),
                CreateBuiltInProfile(
                    "builtin-jira",
                    "Jira Export",
                    ImportMappingScope.Any,
                    new ImportFieldMapping
                    {
                        preset = ImportMappingPreset.Generic,
                        nameIndex = 1,
                        descriptionIndex = 8,
                        statusIndex = 3,
                        priorityIndex = 4,
                        assigneeIndex = 5,
                        tagsIndex = 6,
                        dueDateIndex = 7,
                        checklistIndex = -1,
                        listNameIndex = 9,
                        customFieldsIndex = -1
                    },
                    JiraHeaders),
                CreateBuiltInProfile(
                    "builtin-asana",
                    "Asana Export",
                    ImportMappingScope.Any,
                    new ImportFieldMapping
                    {
                        preset = ImportMappingPreset.Generic,
                        nameIndex = 1,
                        descriptionIndex = 2,
                        statusIndex = 3,
                        priorityIndex = 4,
                        assigneeIndex = 5,
                        tagsIndex = 6,
                        dueDateIndex = 7,
                        checklistIndex = -1,
                        listNameIndex = 8,
                        customFieldsIndex = 9
                    },
                    AsanaHeaders),
                CreateBuiltInProfile(
                    "builtin-monday",
                    "Monday.com Export",
                    ImportMappingScope.Any,
                    new ImportFieldMapping
                    {
                        preset = ImportMappingPreset.Generic,
                        nameIndex = 0,
                        descriptionIndex = 8,
                        statusIndex = 1,
                        priorityIndex = 3,
                        assigneeIndex = 2,
                        tagsIndex = 5,
                        dueDateIndex = 4,
                        checklistIndex = -1,
                        listNameIndex = 6,
                        customFieldsIndex = -1
                    },
                    MondayHeaders),
                CreateBuiltInProfile(
                    "builtin-generic-table",
                    "Generic Spreadsheet",
                    ImportMappingScope.Any,
                    Generic(new[] { "Name", "Description", "Status", "Priority", "Assignees", "Tags", "Due Date", "Checklist", "List Name", "Custom Fields" }),
                    new[] { "Name", "Description", "Status", "Priority", "Assignees", "Tags", "Due Date", "Checklist", "List Name", "Custom Fields" })
            };

            return profiles.Where(p => p.MatchesScope(scope)).ToList();
        }
    }

    public class ImportFieldMappingWindow : EditorWindow
    {
        private static readonly string[] PresetNames = { "Generic", "ClickUp", "Trello" };

        private ImportMappingScope _scope;
        private string _sourcePath;
        private string _headerSignature;
        private string[] _headers;
        private string[] _options;
        private string[] _profileOptions;
        private List<ImportFieldMappingProfile> _profiles;
        private ImportFieldMapping _suggestedMapping;
        private ImportFieldMapping _mapping;
        private Action<ImportFieldMappingWindowResult> _onConfirm;
        private string _profileName;
        private string _sourcePattern;
        private bool _saveProfile;
        private bool _rememberLastMappingForPattern;
        private int _selectedProfileIndex;
        private Vector2 _scroll;

        public static void Open(string title, ImportMappingScope scope, string sourcePath, string[] headers, ImportFieldMapping suggestedMapping, IEnumerable<ImportFieldMappingProfile> profiles, string suggestedProfileId, Action<ImportFieldMappingWindowResult> onConfirm)
        {
            var window = CreateInstance<ImportFieldMappingWindow>();
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(540, 560);
            window._scope = scope;
            window._sourcePath = sourcePath ?? string.Empty;
            window._headers = headers ?? Array.Empty<string>();
            window._headerSignature = ImportFieldMappingPresets.BuildHeaderSignature(window._headers);
            window._options = new[] { "<None>" }.Concat(window._headers).ToArray();
            var builtIns = ImportFieldMappingPresets.GetBuiltInProfiles(scope);
            window._profiles = builtIns
                .Concat((profiles ?? Enumerable.Empty<ImportFieldMappingProfile>())
                .Where(p => p != null)
                .Where(p => p.MatchesScope(scope))
                .Select(p =>
                {
                    var clone = p.Clone();
                    clone.Normalize();
                    return clone;
                }))
                .GroupBy(p => p.id)
                .Select(g => g.First())
                .OrderBy(p => p.profileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            window._profileOptions = new[] { "<Suggested / Current>" }
                .Concat(window._profiles.Select(window.GetProfileOptionLabel))
                .ToArray();
            window._suggestedMapping = (suggestedMapping ?? ImportFieldMappingPresets.Generic(window._headers)).Clone();
            window._suggestedMapping.Normalize();
            window._mapping = window._suggestedMapping.Clone();
            window._onConfirm = onConfirm;
            window._profileName = window.BuildDefaultProfileName();
            window._sourcePattern = ImportFieldMappingPresets.BuildSuggestedPattern(window._sourcePath);
            window._selectedProfileIndex = 0;

            if (!string.IsNullOrWhiteSpace(suggestedProfileId))
            {
                int profileIndex = window._profiles.FindIndex(p => p.id == suggestedProfileId);
                if (profileIndex >= 0)
                {
                    window._selectedProfileIndex = profileIndex + 1;
                    window.ApplyProfile(window._profiles[profileIndex]);
                }
            }

            if (window._selectedProfileIndex <= 0)
            {
                var signatureMatch = window._profiles
                    .Where(p => p.MatchesHeaderSignature(window._headerSignature))
                    .OrderByDescending(p => p.lastUsedUtcTicks)
                    .FirstOrDefault();
                if (signatureMatch != null)
                {
                    window._selectedProfileIndex = window._profiles.FindIndex(p => p.id == signatureMatch.id) + 1;
                    window.ApplyProfile(signatureMatch);
                }
            }

            window.ShowUtility();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            try
            {
                if (_mapping == null)
                {
                    Close();
                    return;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.PopupBg, true);
                }

                EditorGUILayout.HelpBox("Map incoming columns to Task Manager fields. Only Name is required.", MessageType.Info);

                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scope.scrollPosition;

                    int presetIndex = (int)_mapping.preset;
                    int newPresetIndex = EditorGUILayout.Popup("Preset", presetIndex, PresetNames);
                    if (newPresetIndex != presetIndex)
                    {
                        _mapping.preset = (ImportMappingPreset)newPresetIndex;
                        ApplyPreset(_mapping.preset);
                    }

                    int newProfileIndex = EditorGUILayout.Popup("Saved Profile", _selectedProfileIndex, _profileOptions);
                    if (newProfileIndex != _selectedProfileIndex)
                    {
                        _selectedProfileIndex = newProfileIndex;
                        if (_selectedProfileIndex <= 0)
                            RestoreSuggestedState();
                        else
                            ApplyProfile(_profiles[_selectedProfileIndex - 1]);
                    }

                    EditorGUILayout.Space(6);
                    _saveProfile = EditorGUILayout.ToggleLeft("Save / update profile when importing", _saveProfile);
                    using (new EditorGUI.DisabledGroupScope(!_saveProfile))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Profile Name", GUILayout.Width(130));
                            _profileName = TBStyles.DrawThemedTextField(_profileName, GUILayout.Height(20));
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        var selectedProfile = _selectedProfileIndex > 0 ? _profiles[_selectedProfileIndex - 1] : null;
                        EditorGUI.BeginDisabledGroup(_selectedProfileIndex <= 0 || (selectedProfile != null && selectedProfile.isBuiltIn));
                        if (GUILayout.Button("Delete Selected Profile", TBStyles.DeleteButton, GUILayout.Height(22), GUILayout.Width(170)))
                        {
                            if (selectedProfile != null && ThemedDialog.Show("Delete Import Profile", "Delete profile '" + selectedProfile.profileName + "'?", "Delete", "Cancel"))
                            {
                                _onConfirm?.Invoke(new ImportFieldMappingWindowResult
                                {
                                    scope = _scope,
                                    deleteProfileId = selectedProfile.id
                                });
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                    }

                    _rememberLastMappingForPattern = EditorGUILayout.ToggleLeft("Remember last mapping for matching files", _rememberLastMappingForPattern);
                    using (new EditorGUI.DisabledGroupScope(!_rememberLastMappingForPattern))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Source File Pattern", GUILayout.Width(130));
                            _sourcePattern = TBStyles.DrawThemedTextField(_sourcePattern, GUILayout.Height(20));
                        }
                        EditorGUILayout.HelpBox("Example patterns: trello-export*.csv, sprint_report_*.xlsx, *.xml", MessageType.None);
                    }

                    EditorGUILayout.Space(6);

                    DrawMapField("Name *", ref _mapping.nameIndex);
                    DrawMapField("Description", ref _mapping.descriptionIndex);
                    DrawMapField("Status / Column", ref _mapping.statusIndex);
                    DrawMapField("Priority", ref _mapping.priorityIndex);
                    DrawMapField("Assignees / Members", ref _mapping.assigneeIndex);
                    DrawMapField("Tags / Labels", ref _mapping.tagsIndex);
                    DrawMapField("Due Date", ref _mapping.dueDateIndex);
                    DrawMapField("Checklist", ref _mapping.checklistIndex);
                    DrawMapField("Board/List Name", ref _mapping.listNameIndex);
                    DrawMapField("Custom Fields", ref _mapping.customFieldsIndex);
                }

                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cancel", TBStyles.StandardButton, GUILayout.Height(26)))
                    {
                        Close();
                        GUIUtility.ExitGUI();
                    }

                    EditorGUI.BeginDisabledGroup(_mapping.nameIndex < 0);
                    if (GUILayout.Button("Import", TBStyles.AddCardButton, GUILayout.Height(26)))
                    {
                        var result = new ImportFieldMappingWindowResult
                        {
                            scope = _scope,
                            mapping = _mapping.Clone(),
                            selectedProfileId = _selectedProfileIndex > 0 ? _profiles[_selectedProfileIndex - 1].id : null,
                            saveProfile = _saveProfile,
                            profileName = GetResolvedProfileName(),
                            rememberLastMappingForPattern = _rememberLastMappingForPattern,
                            headerSignature = _headerSignature,
                            sourceFilePattern = _rememberLastMappingForPattern
                                ? ImportFieldMappingPresets.NormalizePattern(string.IsNullOrWhiteSpace(_sourcePattern)
                                    ? ImportFieldMappingPresets.BuildSuggestedPattern(_sourcePath)
                                    : _sourcePattern)
                                : string.Empty
                        };

                        _onConfirm?.Invoke(result);
                        Close();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(this);
            }
        }

        private void DrawMapField(string label, ref int mappedIndex)
        {
            mappedIndex = EditorGUILayout.Popup(label, mappedIndex + 1, _options) - 1;
        }

        private string BuildDefaultProfileName()
        {
            string fileName = Path.GetFileNameWithoutExtension(_sourcePath);
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName.Trim() + " Mapping";

            string scopeName = _scope == ImportMappingScope.Board
                ? "Board"
                : _scope == ImportMappingScope.Column
                    ? "Column"
                    : _scope == ImportMappingScope.Card
                        ? "Card"
                        : "Import";

            return _mapping != null && _mapping.preset != ImportMappingPreset.Generic
                ? scopeName + " " + _mapping.preset + " Mapping"
                : scopeName + " Mapping";
        }

        private string GetProfileOptionLabel(ImportFieldMappingProfile profile)
        {
            if (profile == null) return "<Missing Profile>";
            string label = profile.profileName;
            if (profile.isBuiltIn)
                label += "  [built-in]";
            if (profile.rememberLastMappingForPattern && !string.IsNullOrWhiteSpace(profile.sourceFilePattern))
                return label + "  [" + profile.sourceFilePattern + "]";
            if (!string.IsNullOrWhiteSpace(profile.headerSignature))
                return label + "  [headers]";
            return label;
        }

        private void RestoreSuggestedState()
        {
            _mapping = _suggestedMapping.Clone();
            _mapping.Normalize();
            _saveProfile = false;
            _rememberLastMappingForPattern = false;
            _profileName = BuildDefaultProfileName();
            _sourcePattern = ImportFieldMappingPresets.BuildSuggestedPattern(_sourcePath);
        }

        private void ApplyProfile(ImportFieldMappingProfile profile)
        {
            if (profile == null)
            {
                RestoreSuggestedState();
                return;
            }

            profile.Normalize();
            _mapping = profile.mapping.Clone();
            _mapping.Normalize();
            _profileName = profile.profileName;
            _saveProfile = true;
            _rememberLastMappingForPattern = profile.rememberLastMappingForPattern;
            _sourcePattern = !string.IsNullOrWhiteSpace(profile.sourceFilePattern)
                ? profile.sourceFilePattern
                : ImportFieldMappingPresets.BuildSuggestedPattern(_sourcePath);
        }

        private string GetResolvedProfileName()
        {
            string resolvedName = string.IsNullOrWhiteSpace(_profileName) ? string.Empty : _profileName.Trim();
            if (!string.IsNullOrWhiteSpace(resolvedName)) return resolvedName;

            if (_selectedProfileIndex > 0)
                return _profiles[_selectedProfileIndex - 1].profileName;

            if (_rememberLastMappingForPattern)
                return "Remembered: " + ImportFieldMappingPresets.NormalizePattern(string.IsNullOrWhiteSpace(_sourcePattern)
                    ? ImportFieldMappingPresets.BuildSuggestedPattern(_sourcePath)
                    : _sourcePattern);

            return BuildDefaultProfileName();
        }

        private void ApplyPreset(ImportMappingPreset preset)
        {
            switch (preset)
            {
                case ImportMappingPreset.ClickUp:
                    _mapping = ImportFieldMappingPresets.ClickUp(_headers);
                    break;
                case ImportMappingPreset.Trello:
                    _mapping = ImportFieldMappingPresets.Trello(_headers);
                    break;
                default:
                    _mapping = ImportFieldMappingPresets.Generic(_headers);
                    break;
            }
        }
    }
}

