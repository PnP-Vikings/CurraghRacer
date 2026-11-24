using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeTaskManager.Data;
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
        Trello,
        Jira,
        Asana,
        Monday,
        Notion,
        GitHubProjects,
        Linear,
        Wrike,
        MicrosoftPlanner,
        AzureDevOps,
        YouTrack,
        Basecamp,
        Airtable
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
        public int taskIdIndex = -1;
        public int taskCodeIndex = -1;
        public int parentIdIndex = -1;
        public int subtaskIdsIndex = -1;
        public int completedIndex = -1;
        public int archivedIndex = -1;
        public int colorIndex = -1;
        public int linksIndex = -1;
        public int checklistLinksIndex = -1;
        public bool updateExisting;
        public bool priorityNumbersDescending;
        public bool importLabelColors = true;
        public string doneStatusNames = string.Empty;
        public string archivedStatusNames = string.Empty;
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
            taskIdIndex = Mathf.Max(-1, taskIdIndex);
            taskCodeIndex = Mathf.Max(-1, taskCodeIndex);
            parentIdIndex = Mathf.Max(-1, parentIdIndex);
            subtaskIdsIndex = Mathf.Max(-1, subtaskIdsIndex);
            completedIndex = Mathf.Max(-1, completedIndex);
            archivedIndex = Mathf.Max(-1, archivedIndex);
            colorIndex = Mathf.Max(-1, colorIndex);
            linksIndex = Mathf.Max(-1, linksIndex);
            checklistLinksIndex = Mathf.Max(-1, checklistLinksIndex);

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
        private static readonly string[] AtmClickUpHeaders = BoardImportExport.FullExportHeaders;

        private static readonly ImportMappingPreset[] SupportedPresets =
        {
            ImportMappingPreset.Generic, ImportMappingPreset.ClickUp, ImportMappingPreset.Trello,
            ImportMappingPreset.Jira, ImportMappingPreset.Asana, ImportMappingPreset.Monday,
            ImportMappingPreset.Notion, ImportMappingPreset.GitHubProjects, ImportMappingPreset.Linear,
            ImportMappingPreset.Wrike, ImportMappingPreset.MicrosoftPlanner, ImportMappingPreset.AzureDevOps,
            ImportMappingPreset.YouTrack, ImportMappingPreset.Basecamp, ImportMappingPreset.Airtable
        };

        public static string[] GetPresetNames()
        {
            return SupportedPresets.Select(GetPresetDisplayName).ToArray();
        }

        public static string GetPresetDisplayName(ImportMappingPreset preset)
        {
            switch (preset)
            {
                case ImportMappingPreset.ClickUp: return "ClickUp";
                case ImportMappingPreset.Trello: return "Trello";
                case ImportMappingPreset.Jira: return "Jira";
                case ImportMappingPreset.Asana: return "Asana";
                case ImportMappingPreset.Monday: return "Monday.com";
                case ImportMappingPreset.Notion: return "Notion";
                case ImportMappingPreset.GitHubProjects: return "GitHub Projects";
                case ImportMappingPreset.Linear: return "Linear";
                case ImportMappingPreset.Wrike: return "Wrike";
                case ImportMappingPreset.MicrosoftPlanner: return "Microsoft Planner";
                case ImportMappingPreset.AzureDevOps: return "Azure DevOps";
                case ImportMappingPreset.YouTrack: return "YouTrack";
                case ImportMappingPreset.Basecamp: return "Basecamp";
                case ImportMappingPreset.Airtable: return "Airtable";
                default: return "Generic";
            }
        }

        public static string GetBuiltInProfileId(ImportMappingPreset preset, string[] headers)
        {
            switch (preset)
            {
                case ImportMappingPreset.Generic:
                    return "builtin-generic-table";
                case ImportMappingPreset.ClickUp:
                    return headers != null && headers.Any(header =>
                               string.Equals(header, "ATM Completed", StringComparison.OrdinalIgnoreCase))
                        ? "builtin-atm-clickup"
                        : "builtin-clickup";
                case ImportMappingPreset.Trello:
                    return "builtin-trello";
                default:
                    return "builtin-" + Regex.Replace(preset.ToString(), "[^A-Za-z0-9]+", "").ToLowerInvariant();
            }
        }

        public static ImportFieldMapping AutoDetect(string[] headers)
        {
            ImportMappingPreset bestPreset = ImportMappingPreset.Generic;
            int bestScore = 0;
            foreach (ImportMappingPreset preset in SupportedPresets.Where(item => item != ImportMappingPreset.Generic))
            {
                ImportFieldMapping candidate = CreateMapping(headers, preset);
                int score = CountMappedFields(candidate) + GetPresetSignatureScore(headers, preset);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPreset = preset;
                }
            }

            return bestScore >= 5 ? CreateMapping(headers, bestPreset) : Generic(headers);
        }

        public static ImportFieldMapping Generic(string[] headers)
        {
            return CreateMapping(headers, ImportMappingPreset.Generic);
        }

        public static ImportFieldMapping ClickUp(string[] headers)
        {
            return CreateMapping(headers, ImportMappingPreset.ClickUp);
        }

        public static ImportFieldMapping Trello(string[] headers)
        {
            return CreateMapping(headers, ImportMappingPreset.Trello);
        }

        public static ImportFieldMapping ForPreset(string[] headers, ImportMappingPreset preset)
        {
            return CreateMapping(headers, preset);
        }

        public static string[] GetSampleHeaders(ImportMappingPreset preset)
        {
            switch (preset)
            {
                case ImportMappingPreset.ClickUp: return new[] { "Task ID", "Task Custom ID", "Task Name", "Task Content", "Status", "Due date", "Parent ID", "Subtask IDs", "Assignees", "Tags", "Priority", "List Name", "Checklists", "Custom Fields" };
                case ImportMappingPreset.Trello: return new[] { "Card ID", "Card Name", "Card Description", "List Name", "Members", "Labels", "Due Date", "Checklist Items", "Board Name", "Card Archived", "Due Complete", "Custom Fields" };
                case ImportMappingPreset.Jira: return new[] { "Issue key", "Summary", "Issue Type", "Status", "Priority", "Assignee", "Labels", "Due date", "Description", "Project" };
                case ImportMappingPreset.Asana: return new[] { "Task ID", "Task Name", "Notes", "Section", "Priority", "Assignee", "Tags", "Due Date", "Project", "Custom Fields" };
                case ImportMappingPreset.Monday: return new[] { "Item", "Status", "Owner", "Priority", "Due Date", "Tags", "Board", "Updates", "Description" };
                case ImportMappingPreset.Notion: return new[] { "Name", "Status", "Assignee", "Due", "Tags", "Description", "Priority", "Parent item" };
                case ImportMappingPreset.GitHubProjects: return new[] { "Title", "Body", "Status", "Assignees", "Labels", "Milestone", "Repository", "Number", "URL" };
                case ImportMappingPreset.Linear: return new[] { "Title", "Description", "Status", "Priority", "Assignee", "Labels", "Due Date", "Identifier", "Project", "Parent" };
                case ImportMappingPreset.Wrike: return new[] { "Task Name", "Description", "Status", "Importance", "Assigned To", "Tags", "Due Date", "Folder", "Task ID", "Checklist" };
                case ImportMappingPreset.MicrosoftPlanner: return new[] { "Task Name", "Bucket Name", "Progress", "Priority", "Assigned To", "Due Date", "Description", "Labels", "Checklist", "Task ID" };
                case ImportMappingPreset.AzureDevOps: return new[] { "ID", "Work Item Type", "Title", "State", "Assigned To", "Tags", "Description", "Priority", "Iteration Path", "Due Date", "Parent" };
                case ImportMappingPreset.YouTrack: return new[] { "ID", "Summary", "Type", "State", "Priority", "Assignee", "Tags", "Due Date", "Description", "Project" };
                case ImportMappingPreset.Basecamp: return new[] { "To-do", "Description", "Completed", "Assigned to", "Due Date", "To-do list", "Project" };
                case ImportMappingPreset.Airtable: return new[] { "Name", "Description", "Status", "Priority", "Assignee", "Tags", "Due Date", "Checklist", "Group", "ID" };
                default: return new[] { "Name", "Description", "Status", "Priority", "Assignees", "Tags", "Due Date", "Checklist", "List Name", "Custom Fields" };
            }
        }

        private static ImportFieldMapping CreateMapping(string[] headers, ImportMappingPreset preset)
        {
            headers ??= Array.Empty<string>();
            return new ImportFieldMapping
            {
                preset = preset,
                nameIndex = FindPresetHeaderIndex(headers, preset, "name", "Task Name", "Card Name", "Item", "Summary", "Issue Title", "Title", "To-do", "Task", "Name"),
                descriptionIndex = FindPresetHeaderIndex(headers, preset, "description", "Task Content", "Card Description", "Description", "Notes", "Body", "Details", "Updates"),
                statusIndex = FindPresetHeaderIndex(headers, preset, "status", "Status", "List Name", "List", "Section", "State", "Progress", "Bucket Name", "Bucket", "Column", "Workflow State", "To-do list"),
                priorityIndex = FindPresetHeaderIndex(headers, preset, "priority", "Priority", "Importance"),
                assigneeIndex = FindPresetHeaderIndex(headers, preset, "assignee", "Assignees", "Assignee", "Members", "Assigned To", "Assigned to", "Owner", "Person"),
                tagsIndex = FindPresetHeaderIndex(headers, preset, "tags", "Tags", "Labels (Name (color))", "Labels", "Label"),
                dueDateIndex = FindPresetHeaderIndex(headers, preset, "dueDate", "Due date", "Due Date", "Due", "Deadline"),
                checklistIndex = FindPresetHeaderIndex(headers, preset, "checklist", "Checklists", "Checklist", "Checklist Items", "Sub-items", "Subtasks"),
                listNameIndex = FindPresetHeaderIndex(headers, preset, "listName", "List Name", "Board Name", "Board", "Project", "Repository", "Folder", "To-do list", "Group", "Iteration Path"),
                customFieldsIndex = FindPresetHeaderIndex(headers, preset, "customFields", "Custom Fields", "Custom Field", "Issue Type", "Work Item Type", "Type", "Milestone"),
                taskIdIndex = FindPresetHeaderIndex(headers, preset, "taskId", "Task ID", "Card ID", "Issue ID", "Work Item ID", "Number", "ID"),
                taskCodeIndex = FindPresetHeaderIndex(headers, preset, "taskCode", "Task Custom ID", "Task Code", "Issue key", "Identifier"),
                parentIdIndex = FindPresetHeaderIndex(headers, preset, "parentId", "Parent ID", "Parent", "Parent item"),
                subtaskIdsIndex = FindPresetHeaderIndex(headers, preset, "subtaskIds", "Subtask IDs", "Subtasks"),
                completedIndex = FindPresetHeaderIndex(headers, preset, "completed", "ATM Completed", "Due Complete", "Completed", "Is Complete"),
                archivedIndex = FindPresetHeaderIndex(headers, preset, "archived", "ATM Archived", "Card Archived", "Archived"),
                colorIndex = FindPresetHeaderIndex(headers, preset, "color", "ATM Color", "Color Label"),
                linksIndex = FindPresetHeaderIndex(headers, preset, "links", "ATM Links", "Linked Items", "URL"),
                checklistLinksIndex = FindPresetHeaderIndex(headers, preset, "checklistLinks", "ATM Checklist Links", "Checklist Links"),
                priorityNumbersDescending = preset == ImportMappingPreset.ClickUp,
                importLabelColors = true
            };
        }

        private static int FindPresetHeaderIndex(string[] headers, ImportMappingPreset preset, string field, params string[] candidates)
        {
            if (preset == ImportMappingPreset.Generic)
                return FindFirstHeaderIndex(headers, candidates);

            var supportedHeaders = new HashSet<string>(GetSampleHeaders(preset), StringComparer.OrdinalIgnoreCase);
            string[] presetCandidates = candidates
                .Where(candidate => candidate.StartsWith("ATM ", StringComparison.OrdinalIgnoreCase) || supportedHeaders.Contains(candidate))
                .ToArray();

            if (field == "status" && preset == ImportMappingPreset.Trello)
                presetCandidates = new[] { "List Name" };
            if (field == "listName" && preset == ImportMappingPreset.Trello)
                presetCandidates = new[] { "Board Name", "Board" };

            return FindFirstHeaderIndex(headers, presetCandidates);
        }

        private static int CountMappedFields(ImportFieldMapping mapping)
        {
            return new[] { mapping.nameIndex, mapping.descriptionIndex, mapping.statusIndex, mapping.priorityIndex,
                mapping.assigneeIndex, mapping.tagsIndex, mapping.dueDateIndex, mapping.checklistIndex,
                mapping.listNameIndex, mapping.taskIdIndex, mapping.taskCodeIndex }.Count(index => index >= 0);
        }

        private static int GetPresetSignatureScore(string[] headers, ImportMappingPreset preset)
        {
            int score = 0;
            switch (preset)
            {
                case ImportMappingPreset.ClickUp:
                    if (HasHeader(headers, "Task Custom ID")) score += 5;
                    if (HasHeader(headers, "Task Content")) score += 3;
                    if (HasHeader(headers, "Checklists")) score += 2;
                    break;
                case ImportMappingPreset.Trello:
                    if (HasHeader(headers, "Card Name")) score += 4;
                    if (HasHeader(headers, "Card ID")) score += 4;
                    if (HasHeader(headers, "List Name") && HasHeader(headers, "Board Name")) score += 2;
                    break;
                case ImportMappingPreset.Jira:
                    if (HasHeader(headers, "Issue key")) score += 5;
                    if (HasHeader(headers, "Summary")) score += 2;
                    break;
                case ImportMappingPreset.Asana:
                    if (HasHeader(headers, "Section")) score += 4;
                    if (HasHeader(headers, "Task Name") && HasHeader(headers, "Project")) score += 2;
                    break;
                case ImportMappingPreset.Monday:
                    if (HasHeader(headers, "Item")) score += 5;
                    if (HasHeader(headers, "Owner")) score += 2;
                    break;
                case ImportMappingPreset.Notion:
                    if (HasHeader(headers, "Parent item")) score += 4;
                    if (HasHeader(headers, "Name") && HasHeader(headers, "Status")) score += 1;
                    break;
                case ImportMappingPreset.GitHubProjects:
                    if (HasHeader(headers, "Repository")) score += 4;
                    if (HasHeader(headers, "Milestone")) score += 2;
                    break;
                case ImportMappingPreset.Linear:
                    if (HasHeader(headers, "Identifier")) score += 5;
                    if (HasHeader(headers, "Project") && HasHeader(headers, "Labels")) score += 1;
                    break;
                case ImportMappingPreset.Wrike:
                    if (HasHeader(headers, "Importance")) score += 3;
                    if (HasHeader(headers, "Folder")) score += 3;
                    break;
                case ImportMappingPreset.MicrosoftPlanner:
                    if (HasHeader(headers, "Bucket Name")) score += 5;
                    if (HasHeader(headers, "Progress")) score += 2;
                    break;
                case ImportMappingPreset.AzureDevOps:
                    if (HasHeader(headers, "Work Item Type")) score += 5;
                    if (HasHeader(headers, "Iteration Path")) score += 2;
                    break;
                case ImportMappingPreset.YouTrack:
                    if (HasHeader(headers, "Summary") && HasHeader(headers, "Project") && HasHeader(headers, "Type")) score += 4;
                    break;
                case ImportMappingPreset.Basecamp:
                    if (HasHeader(headers, "To-do")) score += 5;
                    if (HasHeader(headers, "To-do list")) score += 2;
                    break;
                case ImportMappingPreset.Airtable:
                    if (HasHeader(headers, "Group")) score += 4;
                    if (HasHeader(headers, "Name") && HasHeader(headers, "ID")) score += 2;
                    break;
            }
            return score;
        }

        private static bool HasHeader(string[] headers, string candidate)
        {
            return FindFirstHeaderIndex(headers, candidate) >= 0;
        }

        public static int FindFirstHeaderIndex(string[] headers, params string[] candidates)
        {
            if (headers == null || headers.Length == 0 || candidates == null || candidates.Length == 0) return -1;

            foreach (string candidate in candidates)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    string h = (headers[i] ?? string.Empty).Trim();
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
                    GetBuiltInProfileId(ImportMappingPreset.ClickUp, AtmClickUpHeaders),
                    "AwesomeTaskManager CSV",
                    ImportMappingScope.Any,
                    FromPreset(ClickUp(AtmClickUpHeaders)),
                    AtmClickUpHeaders),
                CreateBuiltInProfile(
                    GetBuiltInProfileId(ImportMappingPreset.ClickUp, GetSampleHeaders(ImportMappingPreset.ClickUp)),
                    "ClickUp Export",
                    ImportMappingScope.Any,
                    FromPreset(ClickUp(GetSampleHeaders(ImportMappingPreset.ClickUp))),
                    GetSampleHeaders(ImportMappingPreset.ClickUp)),
                CreateBuiltInProfile(
                    GetBuiltInProfileId(ImportMappingPreset.Trello, GetSampleHeaders(ImportMappingPreset.Trello)),
                    "Trello Export",
                    ImportMappingScope.Any,
                    FromPreset(Trello(GetSampleHeaders(ImportMappingPreset.Trello))),
                    GetSampleHeaders(ImportMappingPreset.Trello)),
                CreateBuiltInProfile(
                    GetBuiltInProfileId(ImportMappingPreset.Generic, GetSampleHeaders(ImportMappingPreset.Generic)),
                    "Generic Spreadsheet",
                    ImportMappingScope.Any,
                    Generic(GetSampleHeaders(ImportMappingPreset.Generic)),
                    GetSampleHeaders(ImportMappingPreset.Generic))
            };

            foreach (ImportMappingPreset preset in SupportedPresets.Where(item => item != ImportMappingPreset.Generic && item != ImportMappingPreset.ClickUp && item != ImportMappingPreset.Trello))
            {
                string[] headers = GetSampleHeaders(preset);
                profiles.Add(CreateBuiltInProfile(GetBuiltInProfileId(preset, headers), GetPresetDisplayName(preset) + " Export", ImportMappingScope.Any,
                    FromPreset(ForPreset(headers, preset)), headers));
            }

            return profiles.Where(p => p.MatchesScope(scope)).ToList();
        }
    }

    public class ImportFieldMappingWindow : EditorWindow
    {
        private static readonly string[] PresetNames = ImportFieldMappingPresets.GetPresetNames();

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
        private IList<string[]> _rows;
        private ISet<string> _existingKeys;
        private string _presetStatusMessage;

        public static void Open(string title, ImportMappingScope scope, string sourcePath, string[] headers, ImportFieldMapping suggestedMapping, IEnumerable<ImportFieldMappingProfile> profiles, string suggestedProfileId, Action<ImportFieldMappingWindowResult> onConfirm, IList<string[]> rows = null, ISet<string> existingKeys = null)
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
            window._rows = rows;
            window._existingKeys = existingKeys;
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
            using var _scrollbarTheme = ThemedScrollbars.Begin();
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

                EditorGUILayout.HelpBox("Choose a source format, review the detected columns, and preview the tasks before importing. Only Name is required.", MessageType.Info);

                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scope.scrollPosition;
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawSectionHeader("Source and profile");
                        DrawLabeledDropdown("Preset", (int)_mapping.preset, PresetNames, index =>
                        {
                            ApplyPreset((ImportMappingPreset)index);
                            Repaint();
                        });
                        if (!string.IsNullOrWhiteSpace(_presetStatusMessage))
                            EditorGUILayout.LabelField(_presetStatusMessage, EditorStyles.wordWrappedMiniLabel);
                        DrawLabeledDropdown("Saved Profile", _selectedProfileIndex, _profileOptions, SelectProfile);

                        _saveProfile = EditorGUILayout.ToggleLeft("Save / update profile when importing", _saveProfile);
                        using (new EditorGUI.DisabledGroupScope(!_saveProfile))
                            _profileName = DrawTextField("Profile Name", _profileName);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _rememberLastMappingForPattern = EditorGUILayout.ToggleLeft("Remember mapping for matching files", _rememberLastMappingForPattern);
                            var selectedProfile = _selectedProfileIndex > 0 ? _profiles[_selectedProfileIndex - 1] : null;
                            EditorGUI.BeginDisabledGroup(_selectedProfileIndex <= 0 || (selectedProfile != null && selectedProfile.isBuiltIn));
                            if (GUILayout.Button("Delete Profile", TBStyles.DeleteButton, GUILayout.Height(22), GUILayout.Width(110)))
                            {
                                if (selectedProfile != null && ThemedDialog.Show("Delete Import Profile", "Delete profile '" + selectedProfile.profileName + "'?", "Delete", "Cancel"))
                                {
                                    _onConfirm?.Invoke(new ImportFieldMappingWindowResult { scope = _scope, deleteProfileId = selectedProfile.id });
                                    Close();
                                    GUIUtility.ExitGUI();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        using (new EditorGUI.DisabledGroupScope(!_rememberLastMappingForPattern))
                        {
                            _sourcePattern = DrawTextField("File pattern", _sourcePattern);
                            EditorGUILayout.LabelField("Examples: trello-export*.csv, sprint_report_*.xlsx", EditorStyles.miniLabel);
                        }
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawSectionHeader("Task fields", "Map the columns used by most kanban exports.");
                        DrawMapField("Name *", nameof(_mapping.nameIndex));
                        DrawMapField("Description", nameof(_mapping.descriptionIndex));
                        DrawMapField("Status / Column", nameof(_mapping.statusIndex));
                        DrawMapField("Priority", nameof(_mapping.priorityIndex));
                        DrawMapField("Assignees / Members", nameof(_mapping.assigneeIndex));
                        DrawMapField("Tags / Labels", nameof(_mapping.tagsIndex));
                        DrawMapField("Due Date", nameof(_mapping.dueDateIndex));
                        DrawMapField("Checklist", nameof(_mapping.checklistIndex));
                        DrawMapField("Board / List Name", nameof(_mapping.listNameIndex));
                        DrawMapField("Custom Fields", nameof(_mapping.customFieldsIndex));
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawSectionHeader("Styling and task links");
                        _mapping.importLabelColors = EditorGUILayout.ToggleLeft("Import label colors as task/category styling", _mapping.importLabelColors);
                        if (_mapping.preset == ImportMappingPreset.Trello)
                            EditorGUILayout.LabelField("Trello label suffixes such as ‘(green)’ set matching category and task colors.", EditorStyles.miniLabel);
                        DrawMapField("Color Label", nameof(_mapping.colorIndex));
                        DrawMapField("Linked Notes / URLs / Assets", nameof(_mapping.linksIndex));
                        DrawMapField("Checklist Card Links", nameof(_mapping.checklistLinksIndex));
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawSectionHeader("IDs and status rules");
                        DrawMapField("Task ID", nameof(_mapping.taskIdIndex));
                        DrawMapField("Task Code", nameof(_mapping.taskCodeIndex));
                        DrawMapField("Parent ID", nameof(_mapping.parentIdIndex));
                        DrawMapField("Subtask IDs", nameof(_mapping.subtaskIdsIndex));
                        DrawMapField("Completed", nameof(_mapping.completedIndex));
                        DrawMapField("Archived", nameof(_mapping.archivedIndex));
                        _mapping.doneStatusNames = DrawTextField("Done status names", _mapping.doneStatusNames);
                        _mapping.archivedStatusNames = DrawTextField("Archived status names", _mapping.archivedStatusNames);
                    }

                    if (_rows != null)
                    {
                        ImportFieldMapping matchingMapping = _mapping.Clone();
                        matchingMapping.updateExisting = true;
                        BoardImportExport.ImportPreview preview = BoardImportExport.BuildPreview(_rows, matchingMapping, _existingKeys);
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            DrawSectionHeader("Matching rows");
                            EditorGUILayout.HelpBox(preview.totalCards + " tasks · " + preview.wouldCreate + " new · " + preview.wouldUpdate + " matching existing", MessageType.None);
                            foreach (string previewRow in preview.rows)
                                EditorGUILayout.LabelField(previewRow, EditorStyles.miniLabel);
                        }
                    }
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
                    if (GUILayout.Button("Preview Import...", TBStyles.AddCardButton, GUILayout.Height(26)))
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

                        Action<ImportFieldMappingWindowResult> onConfirm = _onConfirm;
                        Close();
                        try
                        {
                            onConfirm?.Invoke(result);
                        }
                        finally
                        {
                            GUIUtility.ExitGUI();
                        }
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

        private void DrawSectionHeader(string title, string subtitle = null)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(subtitle))
                EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
        }

        private void DrawLabeledDropdown(string label, int selectedIndex, string[] options, Action<int> onSelect)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(170));
                TBStyles.DrawThemedDropdown(selectedIndex, options, onSelect, TBStyles.StandardDropdown, GUILayout.ExpandWidth(true));
            }
        }

        private void DrawMapField(string label, string fieldName)
        {
            int mappedIndex = GetMappedIndex(fieldName);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(170));
                TBStyles.DrawThemedDropdown(mappedIndex + 1, _options, selectedIndex =>
                {
                    SetMappedIndex(fieldName, selectedIndex - 1);
                    Repaint();
                }, TBStyles.StandardDropdown, GUILayout.ExpandWidth(true));
                string sample = GetSampleValue(mappedIndex);
                if (!string.IsNullOrWhiteSpace(sample))
                    EditorGUILayout.LabelField(sample, EditorStyles.miniLabel, GUILayout.Width(110));
            }
        }

        private int GetMappedIndex(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(_mapping.nameIndex): return _mapping.nameIndex;
                case nameof(_mapping.descriptionIndex): return _mapping.descriptionIndex;
                case nameof(_mapping.statusIndex): return _mapping.statusIndex;
                case nameof(_mapping.priorityIndex): return _mapping.priorityIndex;
                case nameof(_mapping.assigneeIndex): return _mapping.assigneeIndex;
                case nameof(_mapping.tagsIndex): return _mapping.tagsIndex;
                case nameof(_mapping.dueDateIndex): return _mapping.dueDateIndex;
                case nameof(_mapping.checklistIndex): return _mapping.checklistIndex;
                case nameof(_mapping.listNameIndex): return _mapping.listNameIndex;
                case nameof(_mapping.customFieldsIndex): return _mapping.customFieldsIndex;
                case nameof(_mapping.taskIdIndex): return _mapping.taskIdIndex;
                case nameof(_mapping.taskCodeIndex): return _mapping.taskCodeIndex;
                case nameof(_mapping.parentIdIndex): return _mapping.parentIdIndex;
                case nameof(_mapping.subtaskIdsIndex): return _mapping.subtaskIdsIndex;
                case nameof(_mapping.completedIndex): return _mapping.completedIndex;
                case nameof(_mapping.archivedIndex): return _mapping.archivedIndex;
                case nameof(_mapping.colorIndex): return _mapping.colorIndex;
                case nameof(_mapping.linksIndex): return _mapping.linksIndex;
                case nameof(_mapping.checklistLinksIndex): return _mapping.checklistLinksIndex;
                default: return -1;
            }
        }

        private void SetMappedIndex(string fieldName, int index)
        {
            switch (fieldName)
            {
                case nameof(_mapping.nameIndex): _mapping.nameIndex = index; break;
                case nameof(_mapping.descriptionIndex): _mapping.descriptionIndex = index; break;
                case nameof(_mapping.statusIndex): _mapping.statusIndex = index; break;
                case nameof(_mapping.priorityIndex): _mapping.priorityIndex = index; break;
                case nameof(_mapping.assigneeIndex): _mapping.assigneeIndex = index; break;
                case nameof(_mapping.tagsIndex): _mapping.tagsIndex = index; break;
                case nameof(_mapping.dueDateIndex): _mapping.dueDateIndex = index; break;
                case nameof(_mapping.checklistIndex): _mapping.checklistIndex = index; break;
                case nameof(_mapping.listNameIndex): _mapping.listNameIndex = index; break;
                case nameof(_mapping.customFieldsIndex): _mapping.customFieldsIndex = index; break;
                case nameof(_mapping.taskIdIndex): _mapping.taskIdIndex = index; break;
                case nameof(_mapping.taskCodeIndex): _mapping.taskCodeIndex = index; break;
                case nameof(_mapping.parentIdIndex): _mapping.parentIdIndex = index; break;
                case nameof(_mapping.subtaskIdsIndex): _mapping.subtaskIdsIndex = index; break;
                case nameof(_mapping.completedIndex): _mapping.completedIndex = index; break;
                case nameof(_mapping.archivedIndex): _mapping.archivedIndex = index; break;
                case nameof(_mapping.colorIndex): _mapping.colorIndex = index; break;
                case nameof(_mapping.linksIndex): _mapping.linksIndex = index; break;
                case nameof(_mapping.checklistLinksIndex): _mapping.checklistLinksIndex = index; break;
            }
        }

        private string GetSampleValue(int index)
        {
            if (_rows == null || _rows.Count < 2 || _rows[1] == null || index < 0 || index >= _rows[1].Length) return string.Empty;
            string value = _rows[1][index] ?? string.Empty;
            return value.Length > 24 ? value.Substring(0, 21) + "..." : value;
        }

        private void SelectProfile(int newProfileIndex)
        {
            if (newProfileIndex == _selectedProfileIndex) return;
            _selectedProfileIndex = newProfileIndex;
            if (_selectedProfileIndex <= 0)
                RestoreSuggestedState();
            else
                ApplyProfile(_profiles[_selectedProfileIndex - 1]);
            Repaint();
        }

        private string DrawTextField(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(190));
                return TBStyles.DrawThemedTextField(value ?? string.Empty, GUILayout.Height(20));
            }
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
                ? scopeName + " " + ImportFieldMappingPresets.GetPresetDisplayName(_mapping.preset) + " Mapping"
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
            _presetStatusMessage = string.Empty;
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
            _mapping = profile.isBuiltIn
                ? ImportFieldMappingPresets.ForPreset(_headers, profile.mapping.preset)
                : profile.mapping.Clone();
            _mapping.Normalize();
            _presetStatusMessage = string.Empty;
            _profileName = profile.profileName;
            _saveProfile = !profile.isBuiltIn;
            _rememberLastMappingForPattern = !profile.isBuiltIn && profile.rememberLastMappingForPattern;
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
            _mapping = ImportFieldMappingPresets.ForPreset(_headers, preset);
            _selectedProfileIndex = 0;
            _saveProfile = false;

            ImportFieldMappingProfile matchingProfile = _profiles
                .Where(profile => !profile.isBuiltIn
                                 && profile.MatchesHeaderSignature(_headerSignature)
                                 && profile.mapping != null
                                 && profile.mapping.preset == preset)
                .OrderByDescending(profile => profile.lastUsedUtcTicks)
                .FirstOrDefault();

            if (matchingProfile != null)
            {
                _selectedProfileIndex = _profiles.FindIndex(profile => profile.id == matchingProfile.id) + 1;
                ApplyProfile(matchingProfile);
            }
            else
            {
                string builtInProfileId = ImportFieldMappingPresets.GetBuiltInProfileId(preset, _headers);
                int builtInProfileIndex = _profiles.FindIndex(profile => profile.id == builtInProfileId);
                if (builtInProfileIndex >= 0)
                {
                    _selectedProfileIndex = builtInProfileIndex + 1;
                    ApplyProfile(_profiles[builtInProfileIndex]);
                }
            }

            _presetStatusMessage = BuildPresetStatus(preset, _mapping);
        }

        private static string BuildPresetStatus(ImportMappingPreset preset, ImportFieldMapping mapping)
        {

            var mappedFields = new (string label, int index)[]
            {
                ("Name", mapping.nameIndex),
                ("Description", mapping.descriptionIndex),
                ("Status", mapping.statusIndex),
                ("Priority", mapping.priorityIndex),
                ("Assignees", mapping.assigneeIndex),
                ("Tags", mapping.tagsIndex),
                ("Due Date", mapping.dueDateIndex),
                ("Checklist", mapping.checklistIndex),
                ("Board / List Name", mapping.listNameIndex),
                ("Custom Fields", mapping.customFieldsIndex)
            };
            string[] unmapped = mappedFields.Where(field => field.index < 0)
                .Select(field => field.label + ": <None>")
                .ToArray();
            string details = unmapped.Length > 0 ? " Unmapped: " + string.Join(", ", unmapped) + "." : string.Empty;
            return ImportFieldMappingPresets.GetPresetDisplayName(preset) + " preset matched " +
                   (mappedFields.Length - unmapped.Length) + " of " + mappedFields.Length + " fields." + details;
        }
    }
}

