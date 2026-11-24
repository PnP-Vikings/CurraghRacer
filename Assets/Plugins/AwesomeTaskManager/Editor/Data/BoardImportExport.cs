using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AwesomeTaskManager.Editor;

namespace AwesomeTaskManager.Data
{
    public static class BoardImportExport
    {
        private const string SpreadsheetNamespace = "urn:schemas-microsoft-com:office:spreadsheet";

        public static readonly string[] ExportHeaders =
        {
            "Task ID", "Task Link", "Task Type", "Task Custom ID", "Task Name", "Task Content",
            "Status", "Date created", "Date created Text", "Due date", "Due date Text",
            "Start date", "Start date Text", "Parent ID", "Subtask IDs", "Attachments",
            "Assignees", "Tags", "Priority", "List Name", "Space Name", "Time Estimated",
            "Time Estimated Text", "Checklists", "Comments", "Assigned Comments",
            "Time Spent", "Time Spent Text", "Rolled Up Time", "Rolled Up Time Text"
        };

        public static readonly string[] ExtraExportHeaders =
        {
            "ATM Completed", "ATM Archived", "ATM Color", "ATM Links", "ATM Checklist Links"
        };

        public static readonly string[] FullExportHeaders = ExportHeaders.Concat(ExtraExportHeaders).ToArray();

        public sealed class ImportReport
        {
            public int created;
            public int updated;
            public int placeholders;
            public int newColumns;
            public int newAssignees;
            public int newCategories;
            public readonly List<string> skippedRows = new List<string>();
            public readonly List<string> warnings = new List<string>();

            public string Summary()
            {
                return "Created: " + created + ", updated: " + updated + ", placeholders: " + placeholders +
                       ", skipped: " + skippedRows.Count + ", new columns: " + newColumns +
                       ", new assignees: " + newAssignees + ", new categories: " + newCategories;
            }
        }

        public sealed class ImportPreview
        {
            public int totalCards;
            public int wouldUpdate;
            public int wouldCreate;
            public readonly List<string> rows = new List<string>();
        }

        public enum ImportConflictMode
        {
            Merge,
            CreateNew
        }

        public enum ImportItemKind
        {
            Board,
            Column,
            Card
        }

        public sealed class ImportPlan
        {
            public ImportItemKind kind;
            public string sourceName;
            public string targetName;
            public string suggestedCopyName;
            public string conflictText;
            public bool hasConflict;
            public int totalCards;
            public int matchingCards;
            public int matchingColumns;
            public int newColumns;
            public readonly List<ImportPlanColumn> columns = new List<ImportPlanColumn>();
        }

        public sealed class ImportPlanColumn
        {
            public string name;
            public bool exists;
            public readonly List<ImportPlanCard> cards = new List<ImportPlanCard>();
        }

        public sealed class ImportPlanCard
        {
            public string title;
            public string details;
            public bool exists;
        }

        public sealed class ChecklistEntry
        {
            public string text;
            public bool resolved;
        }

        public sealed class ImportedRowInfo
        {
            public string sourceId;
            public string taskCode;
            public string parentId;
            public List<string> subtaskIds = new List<string>();
            public TaskCard card;
        }

        public static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return rows;
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            bool fieldStarted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                if (c == '"' && !fieldStarted)
                {
                    inQuotes = true;
                    fieldStarted = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                }
                else if (c == '\r' || c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row.Clear();
                    fieldStarted = false;
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                }
                else
                {
                    field.Append(c);
                    fieldStarted = true;
                }
            }

            if (field.Length > 0 || fieldStarted || row.Count > 0 || text[text.Length - 1] == ',')
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }
            return rows;
        }

        public static string EscapeCsvField(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string BuildCsv(IEnumerable<string[]> rows)
        {
            var output = new StringBuilder();
            if (rows == null) return output.ToString();
            foreach (string[] row in rows)
            {
                if (row != null)
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        if (i > 0) output.Append(',');
                        output.Append(EscapeCsvField(row[i]));
                    }
                }
                output.Append("\r\n");
            }
            return output.ToString();
        }

        public static string[] BuildCardRow(TaskCard card, string status, string boardName, IEnumerable<Assignee> assignees, string parentId = null)
        {
            var row = new string[FullExportHeaders.Length];
            if (card == null) return row;

            var cardAssigneeIds = card.assigneeIds ?? new List<string>();
            string assigneeNames = FormatList((assignees ?? Enumerable.Empty<Assignee>())
                .Where(a => a != null && cardAssigneeIds.Contains(a.id))
                .Select(a => a.name ?? string.Empty));
            string tags = FormatList(string.IsNullOrEmpty(card.category) ? Enumerable.Empty<string>() : new[] { card.category });
            string priority = card.priority <= 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
            var checklists = new List<string>();
            var checklistItems = card.checklistItems ?? new List<string>();
            var checklistStates = card.checklistStates ?? new List<bool>();
            for (int i = 0; i < checklistItems.Count; i++)
            {
                string itemStatus = i < checklistStates.Count && checklistStates[i] ? "RESOLVED" : "UNRESOLVED";
                checklists.Add((checklistItems[i] ?? string.Empty) + " (" + itemStatus + ")");
            }

            row[0] = card.id;
            row[2] = "task";
            row[3] = card.taskCode;
            row[4] = card.title;
            row[5] = card.description;
            row[6] = status;
            row[7] = card.createdDate;
            row[8] = card.createdDate;
            row[9] = card.dueDate;
            row[10] = card.dueDate;
            row[13] = parentId ?? string.Empty;
            row[14] = FormatList(card.checklistLinkedCardIds);
            row[15] = "[]";
            row[16] = assigneeNames;
            row[17] = tags;
            row[18] = priority;
            row[19] = boardName;
            row[20] = "AwesomeTaskManager";
            row[23] = string.Join("\n", checklists);
            row[25] = "0";
            row[30] = card.completed ? "TRUE" : "FALSE";
            row[31] = card.archived ? "TRUE" : "FALSE";
            row[32] = card.colorLabel.ToString(CultureInfo.InvariantCulture);
            row[33] = FormatLinks(card);
            row[34] = string.Join("\n", card.checklistLinkedCardIds ?? new List<string>());
            return row;
        }

        public static string[] BuildPlaceholderRow(string status, string boardName)
        {
            var row = new string[FullExportHeaders.Length];
            row[2] = "status_placeholder";
            row[6] = status;
            row[15] = "[]";
            row[16] = "[]";
            row[17] = "[]";
            row[19] = boardName;
            row[20] = "AwesomeTaskManager";
            row[25] = "0";
            return row;
        }

        public static string FormatList(IEnumerable<string> values)
        {
            return "[" + string.Join(",", (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrEmpty(value))
                .Select(value => value.IndexOfAny(new[] { ',', '"', ';', '|', '\r', '\n' }) < 0
                    ? value
                    : "\"" + value.Replace("\"", "\"\"") + "\"")) + "]";
        }

        public static List<string> ParseList(string value)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(value)) return result;

            string text = value.Trim();
            if (text.Length >= 2 && text[0] == '[' && text[text.Length - 1] == ']')
                text = text.Substring(1, text.Length - 2);

            var item = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        item.Append('"');
                        i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (!inQuotes && (character == ',' || character == ';' || character == '|' || character == '\r' || character == '\n'))
                {
                    AddListItem(result, item);
                }
                else item.Append(character);
            }
            AddListItem(result, item);
            return result;
        }

        public static int ParsePriority(string value)
        {
            return ParsePriority(value, false);
        }

        public static int ParsePriority(string value, bool numbersDescending)
        {
            string text = (value ?? string.Empty).Trim();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric >= 0 && numeric <= 4)
                return numbersDescending && numeric > 0 ? 5 - numeric : numeric;

            switch (text.ToLowerInvariant())
            {
                case "none": case "no priority": case "not urgent": case "not high": return 0;
                case "low": return 1;
                case "medium": case "normal": return 2;
                case "high": return 3;
                case "urgent": return 4;
            }

            if (Regex.IsMatch(text, @"\bnot\s+(urgent|high)\b", RegexOptions.IgnoreCase)) return 0;
            if (Regex.IsMatch(text, @"\burgent\b", RegexOptions.IgnoreCase)) return 4;
            if (Regex.IsMatch(text, @"\bhigh\b", RegexOptions.IgnoreCase)) return 3;
            if (Regex.IsMatch(text, @"\b(medium|normal)\b", RegexOptions.IgnoreCase)) return 2;
            if (Regex.IsMatch(text, @"\blow\b", RegexOptions.IgnoreCase)) return 1;
            return 0;
        }

        public static string NormalizeDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string text = value.Trim();

            text = Regex.Replace(text, @"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),?\s+", string.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+(UTC|GMT|[A-Z]{2,5}|[+-]\d{2}:?\d{2})$", string.Empty, RegexOptions.IgnoreCase).Trim();

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric))
            {
                try
                {
                    DateTime date = numeric >= 100000000000d
                        ? DateTimeOffset.FromUnixTimeMilliseconds((long)numeric).UtcDateTime
                        : DateTime.FromOADate(numeric);
                    return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (ArgumentException) { }
                catch (OverflowException) { }
            }

            string[] formats =
            {
                "yyyy-MM-dd", "yyyy-M-d", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", "M/d/yyyy", "MM/dd/yyyy",
                "M/d/yy", "MM/dd/yy", "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy", "dd.MM.yyyy",
                "MMM d, yyyy", "MMMM d, yyyy"
            };
            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out DateTime parsed) ||
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out parsed))
                return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return text;
        }

        public static List<ChecklistEntry> ParseChecklist(string value)
        {
            var entries = new List<ChecklistEntry>();
            if (string.IsNullOrWhiteSpace(value)) return entries;

            if (value.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                MatchCollection arrays = Regex.Matches(value, @"\[(?<items>[^\]]*)\]", RegexOptions.Singleline);
                if (arrays.Count > 0)
                {
                    foreach (Match array in arrays)
                    {
                        MatchCollection items = Regex.Matches(array.Groups["items"].Value, @"""((?:\\.|[^""\\])*)""");
                        foreach (Match item in items)
                        {
                            string text = item.Groups[1].Value
                                .Replace("\\n", "\n")
                                .Replace("\\r", "\r")
                                .Replace("\\\"", "\"")
                                .Replace("\\\\", "\\");
                            AddChecklistEntry(entries, text);
                        }
                    }
                    return entries;
                }
            }

            string[] lines = value.Contains("\n") || value.Contains("\r")
                ? value.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                : value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
                AddChecklistEntry(entries, line);
            return entries;
        }

        private static void AddChecklistEntry(List<ChecklistEntry> entries, string value)
        {
            string text = (value ?? string.Empty).Trim();
            if (text.Length == 0) return;
            bool resolved = false;
            Match marker = Regex.Match(text, @"\s*\((RESOLVED|UNRESOLVED|\[x\]|\[ \])\)\s*$", RegexOptions.IgnoreCase);
            if (marker.Success)
            {
                resolved = marker.Groups[1].Value.Equals("RESOLVED", StringComparison.OrdinalIgnoreCase) ||
                           marker.Groups[1].Value.Equals("[x]", StringComparison.OrdinalIgnoreCase);
                text = text.Substring(0, marker.Index).TrimEnd();
            }
            else if (text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
            {
                resolved = true;
                text = text.Substring(3).TrimStart();
            }
            else if (text.StartsWith("[ ]", StringComparison.Ordinal)) text = text.Substring(3).TrimStart();
            if (text.Length > 0) entries.Add(new ChecklistEntry { text = text, resolved = resolved });
        }

        public static string StripLabelColor(string label, out int colorIndex)
        {
            colorIndex = 0;
            string text = (label ?? string.Empty).Trim();
            Match match = Regex.Match(text, @"\s*\(([^()]*)\)\s*$");
            if (!match.Success || !TryGetLabelColorIndex(match.Groups[1].Value, out colorIndex))
            {
                colorIndex = 0;
                return text;
            }
            return text.Substring(0, match.Index).TrimEnd();
        }

        public static bool TryGetLabelColorIndex(string value, out int colorIndex)
        {
            string color = Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[_\s-]*(dark|light)$", string.Empty);
            switch (color)
            {
                case "green": colorIndex = 1; return true;
                case "blue": colorIndex = 2; return true;
                case "yellow": colorIndex = 3; return true;
                case "orange": colorIndex = 4; return true;
                case "red": colorIndex = 5; return true;
                case "purple": colorIndex = 6; return true;
                case "teal": colorIndex = 7; return true;
                case "pink": colorIndex = 8; return true;
                case "lime": colorIndex = 9; return true;
                case "sky": case "cyan": colorIndex = 11; return true;
                case "black": case "grey": case "gray": colorIndex = 15; return true;
                case "amber": colorIndex = 12; return true;
                case "brown": colorIndex = 16; return true;
                default: colorIndex = 0; return false;
            }
        }

        public static string GetTrelloColorName(int colorIndex)
        {
            switch (colorIndex)
            {
                case 1: return "green";
                case 2: return "blue";
                case 3: return "yellow";
                case 4: return "orange";
                case 5: return "red";
                case 6: return "purple";
                case 7: return "teal";
                case 8: return "pink";
                case 9: return "lime";
                case 11: return "sky";
                case 12: return "amber";
                case 15: return "black";
                case 16: return "brown";
                default: return null;
            }
        }

        public static bool ParseBool(string value)
        {
            string text = (value ?? string.Empty).Trim();
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("1", StringComparison.OrdinalIgnoreCase) || text.Equals("done", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("completed", StringComparison.OrdinalIgnoreCase) || text.Equals("archived", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool IsDoneStatus(string status, string customNames = null)
        {
            string[] names = { "done", "closed", "complete", "completed", "finished", "resolved", "finalizado", "finalizada", "concluído", "concluída", "completado", "completada", "terminé", "terminado", "fertig", "erledigt", "completato", "completata", "hecho", "hecha" };
            return MatchesStatus(status, names, customNames);
        }

        public static bool IsArchivedStatus(string status, string customNames = null)
        {
            string[] names = { "archived", "archive", "archivado", "archivada", "archivé", "archivée", "archiviert", "archiviato", "archiviata" };
            return MatchesStatus(status, names, customNames);
        }

        public static string FormatLinks(TaskCard card)
        {
            if (card == null) return string.Empty;
            var lines = new List<string>();
            foreach (LinkedItem link in card.linkedItems ?? new List<LinkedItem>())
            {
                if (link == null) continue;
                string type = link.isNote ? "note" : link.isUrl ? "url" : "asset";
                string target = link.isUrl ? link.url : link.guid;
                if (!string.IsNullOrWhiteSpace(target))
                    lines.Add(type + ":" + Uri.EscapeDataString(target) + "|" + Uri.EscapeDataString(link.displayName ?? string.Empty));
            }
            foreach (string guid in card.linkedAssetGuids ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(guid) && !(card.linkedItems ?? new List<LinkedItem>()).Any(link => link != null && !link.isNote && !link.isUrl && link.guid == guid))
                    lines.Add("asset:" + Uri.EscapeDataString(guid) + "|");
            return string.Join("\n", lines);
        }

        public static List<LinkedItem> ParseLinks(string value)
        {
            var links = new List<LinkedItem>();
            if (string.IsNullOrWhiteSpace(value)) return links;
            foreach (string line in value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = line.IndexOf(':');
                int separator = line.IndexOf('|', colon + 1);
                if (colon <= 0) continue;
                string type = line.Substring(0, colon).Trim().ToLowerInvariant();
                string target = Uri.UnescapeDataString(line.Substring(colon + 1, (separator < 0 ? line.Length : separator) - colon - 1));
                string displayName = separator < 0 ? string.Empty : Uri.UnescapeDataString(line.Substring(separator + 1));
                if (type == "url") links.Add(LinkedItem.CreateUrl(target, displayName));
                else if (type == "note")
                {
                    LinkedItem note = LinkedItem.CreateNote(target);
                    note.displayName = displayName;
                    links.Add(note);
                }
                else if (type == "asset") links.Add(new LinkedItem(target) { displayName = displayName });
            }
            return links;
        }

        public static void MapRowToCard(TaskCard card, string[] values, string status, ImportFieldMapping mapping, SaveData data, ImportReport report)
        {
            if (card == null || mapping == null) return;
            values = values ?? Array.Empty<string>();
            card.checklistItems = card.checklistItems ?? new List<string>();
            card.checklistStates = card.checklistStates ?? new List<bool>();
            card.checklistLinkedCardIds = card.checklistLinkedCardIds ?? new List<string>();
            card.assigneeIds = card.assigneeIds ?? new List<string>();
            card.linkedAssetGuids = card.linkedAssetGuids ?? new List<string>();
            card.linkedItems = card.linkedItems ?? new List<LinkedItem>();
            data ??= new SaveData();
            data.categories ??= new List<string>();
            data.assignees ??= new List<Assignee>();

            if (mapping.nameIndex >= 0) card.title = GetValue(values, mapping.nameIndex);
            if (mapping.descriptionIndex >= 0) card.description = GetValue(values, mapping.descriptionIndex);
            if (mapping.dueDateIndex >= 0) card.dueDate = NormalizeDate(GetValue(values, mapping.dueDateIndex));
            if (mapping.priorityIndex >= 0) card.priority = ParsePriority(GetValue(values, mapping.priorityIndex), mapping.priorityNumbersDescending);
            if (mapping.statusIndex >= 0)
            {
                card.completed = IsDoneStatus(status, mapping.doneStatusNames);
                card.archived = IsArchivedStatus(status, mapping.archivedStatusNames);
            }
            if (mapping.completedIndex >= 0) card.completed = ParseBool(GetValue(values, mapping.completedIndex));
            if (mapping.archivedIndex >= 0) card.archived = ParseBool(GetValue(values, mapping.archivedIndex));
            if (mapping.colorIndex >= 0 && int.TryParse(GetValue(values, mapping.colorIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out int color))
                card.colorLabel = color;
            if (mapping.taskCodeIndex >= 0) card.taskCode = GetValue(values, mapping.taskCodeIndex);

            if (mapping.tagsIndex >= 0)
            {
                List<string> tags = ParseList(GetValue(values, mapping.tagsIndex));
                string firstTag = tags.FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag))?.Trim() ?? string.Empty;
                int importedLabelColor = 0;
                string category = mapping.importLabelColors ? StripLabelColor(firstTag, out importedLabelColor) : firstTag;
                card.category = category;
                if (!string.IsNullOrEmpty(category) && !data.categories.Any(existing => string.Equals(existing, category, StringComparison.OrdinalIgnoreCase)))
                {
                    data.categories.Add(category);
                    if (report != null) report.newCategories++;
                }
                if (!string.IsNullOrEmpty(category) && importedLabelColor > 0)
                {
                    data.SetCategoryColor(category, importedLabelColor);
                    if (mapping.colorIndex < 0) card.colorLabel = importedLabelColor;
                }
            }

            if (mapping.assigneeIndex >= 0)
            {
                var ids = new List<string>();
                foreach (string name in ParseList(GetValue(values, mapping.assigneeIndex)).Where(name => !string.IsNullOrWhiteSpace(name)))
                {
                    Assignee assignee = data.assignees.FirstOrDefault(existing => existing != null && string.Equals(existing.name, name.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (assignee == null)
                    {
                        assignee = new Assignee { name = name.Trim() };
                        data.assignees.Add(assignee);
                        if (report != null) report.newAssignees++;
                    }
                    if (!ids.Contains(assignee.id)) ids.Add(assignee.id);
                }
                card.assigneeIds = ids;
            }

            if (mapping.checklistIndex >= 0)
            {
                List<ChecklistEntry> checklist = ParseChecklist(GetValue(values, mapping.checklistIndex));
                card.checklistItems = checklist.Select(entry => entry.text).ToList();
                card.checklistStates = checklist.Select(entry => entry.resolved).ToList();
            }
            if (mapping.linksIndex >= 0)
            {
                card.linkedItems = ParseLinks(GetValue(values, mapping.linksIndex));
                card.linkedAssetGuids = card.linkedItems.Where(link => link != null && !link.isNote && !link.isUrl && !string.IsNullOrEmpty(link.guid)).Select(link => link.guid).Distinct().ToList();
            }
            if (mapping.checklistLinksIndex >= 0)
                card.checklistLinkedCardIds = ParseList(GetValue(values, mapping.checklistLinksIndex));
            else if (mapping.subtaskIdsIndex >= 0)
                card.checklistLinkedCardIds = ParseList(GetValue(values, mapping.subtaskIdsIndex));

            if (mapping.customFieldsIndex >= 0 && mapping.preset == ImportMappingPreset.Trello)
            {
                string customText = GetValue(values, mapping.customFieldsIndex);
                if (card.priority == 0) card.priority = ParsePriority(customText);
                if (!string.IsNullOrWhiteSpace(customText))
                    card.description = (card.description ?? string.Empty) + (string.IsNullOrWhiteSpace(card.description) ? string.Empty : "\n\n") + "Custom Fields: " + customText.Trim();
            }
        }

        public static TaskCard FindCardByKey(IEnumerable<TaskCard> cards, string id, string taskCode)
        {
            foreach (TaskCard card in cards ?? Enumerable.Empty<TaskCard>())
            {
                if (card == null) continue;
                if ((!string.IsNullOrWhiteSpace(id) && string.Equals(card.id, id.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(taskCode) && string.Equals(card.taskCode, taskCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return card;
            }
            return null;
        }

        public static TaskCard GetOrCreateCard(IEnumerable<TaskCard> cards, string id, string taskCode, bool updateExisting, string title, out bool updated)
        {
            TaskCard existing = updateExisting ? FindCardByKey(cards, id, taskCode) : null;
            if (existing != null)
            {
                updated = true;
                return existing;
            }
            updated = false;
            return new TaskCard(title);
        }

        public static HashSet<string> CollectCardKeys(IEnumerable<TaskCard> cards)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (TaskCard card in cards ?? Enumerable.Empty<TaskCard>())
            {
                if (card == null) continue;
                if (!string.IsNullOrWhiteSpace(card.id)) keys.Add(card.id.Trim());
                if (!string.IsNullOrWhiteSpace(card.taskCode)) keys.Add(card.taskCode.Trim());
            }
            return keys;
        }

        public static ImportPreview BuildPreview(IList<string[]> rows, ImportFieldMapping mapping, ISet<string> existingKeys, int visibleRows = 5)
        {
            var preview = new ImportPreview();
            if (rows == null || mapping == null) return preview;
            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i] ?? Array.Empty<string>();
                string name = GetValue(values, mapping.nameIndex);
                if (string.IsNullOrWhiteSpace(name)) continue;
                preview.totalCards++;
                string id = GetValue(values, mapping.taskIdIndex);
                string code = GetValue(values, mapping.taskCodeIndex);
                bool update = mapping.updateExisting && existingKeys != null && ((!string.IsNullOrWhiteSpace(id) && existingKeys.Contains(id)) ||
                                                                                 (!string.IsNullOrWhiteSpace(code) && existingKeys.Contains(code)));
                if (update) preview.wouldUpdate++;
                if (preview.rows.Count < visibleRows)
                    preview.rows.Add((update ? "Update: " : "Create: ") + name);
            }
            preview.wouldCreate = preview.totalCards - preview.wouldUpdate;
            return preview;
        }

        public static string GetUniqueName(string baseName, IEnumerable<string> existingNames)
        {
            string name = string.IsNullOrWhiteSpace(baseName) ? "Imported" : baseName.Trim();
            var names = new HashSet<string>((existingNames ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.OrdinalIgnoreCase);
            if (!names.Contains(name)) return name;

            int suffix = 2;
            string candidate;
            do candidate = name + " (" + suffix++ + ")";
            while (names.Contains(candidate));
            return candidate;
        }

        public static TaskBoard FindExistingBoard(string id, string name, IEnumerable<TaskBoard> boards)
        {
            return (boards ?? Enumerable.Empty<TaskBoard>()).FirstOrDefault(board => board != null &&
                ((!string.IsNullOrWhiteSpace(id) && string.Equals(board.id, id, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(name) && string.Equals(board.name, name, StringComparison.OrdinalIgnoreCase))));
        }

        public static TaskColumn FindExistingColumn(string id, string title, IEnumerable<TaskColumn> columns)
        {
            return (columns ?? Enumerable.Empty<TaskColumn>()).FirstOrDefault(column => column != null &&
                ((!string.IsNullOrWhiteSpace(id) && string.Equals(column.id, id, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(title) && string.Equals(column.title, title, StringComparison.OrdinalIgnoreCase))));
        }

        public static TaskColumn ResolveCardImportDestination(TaskBoard board, TaskColumn selectedColumn, string status, bool createCopy)
        {
            if (createCopy) return selectedColumn;
            return (board?.columns ?? new List<TaskColumn>()).FirstOrDefault(column => column != null &&
                string.Equals(column.title, status, StringComparison.OrdinalIgnoreCase)) ?? selectedColumn;
        }

        public static ImportPlan BuildBoardPlan(TaskBoard imported, IList<TaskBoard> existingBoards, IEnumerable<Assignee> assignees)
        {
            string sourceName = imported?.name ?? string.Empty;
            TaskBoard existing = FindExistingBoard(imported?.id, sourceName, existingBoards);
            string copyName = GetUniqueName(sourceName, (existingBoards ?? new List<TaskBoard>()).Where(board => board != null).Select(board => board.name));
            var plan = NewPlan(ImportItemKind.Board, sourceName, existing?.name ?? sourceName, copyName);
            plan.hasConflict = existing != null;
            if (plan.hasConflict) plan.conflictText = "A board with this name or ID already exists.";

            var assigneeNames = BuildAssigneeNameMap(assignees);
            foreach (TaskColumn sourceColumn in imported?.columns ?? new List<TaskColumn>())
            {
                if (sourceColumn == null) continue;
                TaskColumn existingColumn = existing == null ? null : FindExistingColumn(sourceColumn.id, sourceColumn.title, existing.columns);
                var columnPlan = new ImportPlanColumn { name = sourceColumn.title ?? string.Empty, exists = existingColumn != null };
                if (columnPlan.exists) plan.matchingColumns++;
                else plan.newColumns++;
                AddCardsToPlan(plan, columnPlan, sourceColumn.cards, existing == null ? null : GetCardsFromBoard(existing), assigneeNames);
                plan.columns.Add(columnPlan);
            }
            return plan;
        }

        public static ImportPlan BuildColumnPlan(TaskColumn imported, TaskBoard target, IEnumerable<Assignee> assignees)
        {
            TaskColumn existing = target == null ? null : FindExistingColumn(imported?.id, imported?.title, target.columns);
            string copyName = GetUniqueName(imported?.title, target?.columns?.Where(column => column != null).Select(column => column.title));
            var plan = NewPlan(ImportItemKind.Column, imported?.title, existing?.title ?? imported?.title, copyName);
            plan.hasConflict = existing != null;
            if (plan.hasConflict) plan.conflictText = "A column with this name or ID already exists on the target board.";

            var assigneeNames = BuildAssigneeNameMap(assignees);
            var columnPlan = new ImportPlanColumn { name = imported?.title ?? string.Empty, exists = existing != null };
            if (columnPlan.exists) plan.matchingColumns++;
            else plan.newColumns++;
            AddCardsToPlan(plan, columnPlan, imported?.cards, target == null ? null : GetCardsFromBoard(target), assigneeNames);
            plan.columns.Add(columnPlan);
            return plan;
        }

        public static ImportPlan BuildCardPlan(TaskCard imported, TaskBoard target, IEnumerable<Assignee> assignees, string targetColumn = null)
        {
            TaskCard existing = target == null ? null : FindCardByKey(GetCardsFromBoard(target), imported?.id, imported?.taskCode);
            string sourceName = imported?.title ?? string.Empty;
            string copyName = GetUniqueName(sourceName, GetCardsFromBoard(target).Select(card => card?.title));
            var plan = NewPlan(ImportItemKind.Card, sourceName, existing?.title ?? sourceName, copyName);
            plan.hasConflict = existing != null;
            if (plan.hasConflict) plan.conflictText = "A task with this ID or task code already exists on the target board.";

            var assigneeNames = BuildAssigneeNameMap(assignees);
            var columnPlan = new ImportPlanColumn { name = targetColumn ?? string.Empty };
            AddCardToPlan(plan, columnPlan, imported, existing, assigneeNames);
            plan.columns.Add(columnPlan);
            return plan;
        }

        public static ImportPlan BuildRowsPlan(IList<string[]> rows, ImportFieldMapping mapping, ImportMappingScope scope, TaskBoard targetBoard, string defaultStatus, string sourceName)
        {
            ImportItemKind kind = scope == ImportMappingScope.Board ? ImportItemKind.Board : scope == ImportMappingScope.Column ? ImportItemKind.Column : ImportItemKind.Card;
            string resolvedName = string.IsNullOrWhiteSpace(sourceName) ? "Imported" : sourceName.Trim();
            IEnumerable<string> existingNames = kind == ImportItemKind.Board
                ? (targetBoard == null ? Enumerable.Empty<string>() : new[] { targetBoard.name })
                : kind == ImportItemKind.Column
                    ? (targetBoard?.columns ?? new List<TaskColumn>()).Where(column => column != null).Select(column => column.title)
                    : GetCardsFromBoard(targetBoard).Select(card => card?.title);
            string copyName = GetUniqueName(resolvedName, existingNames);
            var plan = NewPlan(kind, resolvedName, targetBoard?.name ?? resolvedName, copyName);
            if (mapping == null || rows == null) return plan;

            var existingCards = targetBoard == null ? Enumerable.Empty<TaskCard>() : GetCardsFromBoard(targetBoard);
            var existingColumns = targetBoard?.columns ?? new List<TaskColumn>();
            var columnsByName = new Dictionary<string, ImportPlanColumn>(StringComparer.OrdinalIgnoreCase);
            var sourceRows = new List<(string[] values, string name, string status)>();
            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i] ?? Array.Empty<string>();
                string name = GetValue(values, mapping.nameIndex);
                if (string.IsNullOrWhiteSpace(name)) continue;
                string status = GetValue(values, mapping.statusIndex);
                if (string.IsNullOrWhiteSpace(status)) status = string.IsNullOrWhiteSpace(defaultStatus) ? "To Do" : defaultStatus;
                sourceRows.Add((values, name, status));
                if (!columnsByName.ContainsKey(status))
                {
                    TaskColumn existingColumn = FindExistingColumn(null, status, existingColumns);
                    columnsByName[status] = new ImportPlanColumn { name = status, exists = existingColumn != null };
                    if (kind != ImportItemKind.Card)
                    {
                        if (existingColumn != null) plan.matchingColumns++;
                        else plan.newColumns++;
                    }
                }
            }

            foreach (var row in sourceRows)
            {
                TaskCard existing = FindCardByKey(existingCards, GetValue(row.values, mapping.taskIdIndex), GetValue(row.values, mapping.taskCodeIndex));
                string details = BuildRowDetails(row.values, mapping);
                var cardPlan = new ImportPlanCard { title = row.name, details = details, exists = existing != null };
                columnsByName[row.status].cards.Add(cardPlan);
                plan.totalCards++;
                if (cardPlan.exists) plan.matchingCards++;
            }

            foreach (ImportPlanColumn column in columnsByName.Values)
                plan.columns.Add(column);

            bool matchingBoard = kind == ImportItemKind.Board && targetBoard != null;
            plan.hasConflict = matchingBoard || plan.matchingColumns > 0 || plan.matchingCards > 0;
            if (plan.hasConflict)
                plan.conflictText = kind == ImportItemKind.Board && matchingBoard
                    ? "A board with this name already exists."
                    : plan.matchingCards > 0
                        ? "Some tasks match existing IDs or task codes."
                        : "One or more columns already exist on the target board.";
            return plan;
        }

        public static void CopyCardContent(TaskCard source, TaskCard target)
        {
            if (source == null || target == null) return;
            TaskCard copy = source.Clone(false);
            string id = target.id;
            int taskNumber = target.taskNumber;
            string taskCode = target.taskCode;
            string createdDate = target.createdDate;
            string createdBy = target.createdBy;
            target.title = copy.title;
            target.description = copy.description;
            target.category = copy.category;
            target.colorLabel = copy.colorLabel;
            target.priority = copy.priority;
            target.dueDate = copy.dueDate;
            target.archived = copy.archived;
            target.checklistItems = copy.checklistItems ?? new List<string>();
            target.checklistStates = copy.checklistStates ?? new List<bool>();
            target.checklistLinkedCardIds = copy.checklistLinkedCardIds ?? new List<string>();
            target.linkedAssetGuids = copy.linkedAssetGuids ?? new List<string>();
            target.linkedSceneObjects = copy.linkedSceneObjects ?? new List<SceneObjectReference>();
            target.linkedItems = copy.linkedItems ?? new List<LinkedItem>();
            target.imagePath = copy.imagePath;
            target.showChecklist = copy.showChecklist;
            target.completed = copy.completed;
            target.assigneeIds = copy.assigneeIds ?? new List<string>();
            target.id = id;
            target.taskNumber = taskNumber;
            target.taskCode = taskCode;
            target.createdDate = createdDate;
            target.createdBy = createdBy;
            target.Touch();
        }

        public static void MergeCardsIntoColumn(TaskBoard board, TaskColumn column, IEnumerable<TaskCard> importedCards, IDictionary<string, string> assigneeMap, ImportReport report)
        {
            if (column == null) return;
            column.cards ??= new List<TaskCard>();
            var destinations = (importedCards ?? Enumerable.Empty<TaskCard>()).Where(card => card != null)
                .Select(card => new ImportedCardDestination { card = card, column = column }).ToList();
            MergeCardsToDestinations(board, destinations, assigneeMap, report);
        }

        public static TaskColumn MergeImportedColumn(TaskBoard board, TaskColumn existingColumn, TaskColumn importedColumn, IDictionary<string, string> assigneeMap, ImportReport report)
        {
            if (board == null || importedColumn == null) return existingColumn;
            board.columns ??= new List<TaskColumn>();
            TaskColumn target = existingColumn ?? FindExistingColumn(importedColumn.id, importedColumn.title, board.columns);
            if (target == null)
            {
                target = new TaskColumn(importedColumn.title);
                board.columns.Add(target);
            }
            else if (!string.IsNullOrWhiteSpace(importedColumn.title)) target.title = importedColumn.title;
            MergeCardsIntoColumn(board, target, importedColumn.cards, assigneeMap, report);
            return target;
        }

        public static TaskBoard MergeImportedBoard(TaskBoard existingBoard, TaskBoard importedBoard, IDictionary<string, string> assigneeMap, ImportReport report)
        {
            if (existingBoard == null || importedBoard == null) return existingBoard;
            existingBoard.columns ??= new List<TaskColumn>();
            importedBoard.columns ??= new List<TaskColumn>();
            var destinations = new List<ImportedCardDestination>();
            foreach (TaskColumn importedColumn in importedBoard.columns.Where(column => column != null))
            {
                TaskColumn targetColumn = FindExistingColumn(importedColumn.id, importedColumn.title, existingBoard.columns);
                if (targetColumn == null)
                {
                    targetColumn = new TaskColumn(importedColumn.title);
                    existingBoard.columns.Add(targetColumn);
                    if (report != null) report.newColumns++;
                }
                else if (!string.IsNullOrWhiteSpace(importedColumn.title)) targetColumn.title = importedColumn.title;

                targetColumn.cards ??= new List<TaskCard>();
                destinations.AddRange((importedColumn.cards ?? new List<TaskCard>()).Where(card => card != null)
                    .Select(card => new ImportedCardDestination { card = card, column = targetColumn }));
            }
            MergeCardsToDestinations(existingBoard, destinations, assigneeMap, report);
            return existingBoard;
        }

        private sealed class ImportedCardDestination
        {
            public TaskCard card;
            public TaskColumn column;
            public TaskCard existing;
            public string oldId;
            public string finalId;
        }

        private static void MergeCardsToDestinations(TaskBoard board, IList<ImportedCardDestination> destinations, IDictionary<string, string> assigneeMap, ImportReport report)
        {
            var existingCards = board == null ? new List<TaskCard>() : GetCardsFromBoard(board).Where(card => card != null).ToList();
            var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ImportedCardDestination destination in destinations)
            {
                destination.oldId = destination.card.id;
                destination.existing = FindCardByKey(existingCards, destination.card.id, destination.card.taskCode);
                destination.finalId = destination.existing == null ? Guid.NewGuid().ToString() : destination.existing.id;
                if (!string.IsNullOrWhiteSpace(destination.oldId) && !idMap.ContainsKey(destination.oldId))
                    idMap.Add(destination.oldId, destination.finalId);
            }

            foreach (ImportedCardDestination destination in destinations)
            {
                TaskCard source = destination.card;
                source.id = destination.finalId;
                source.checklistLinkedCardIds ??= new List<string>();
                for (int i = 0; i < source.checklistLinkedCardIds.Count; i++)
                {
                    string linkedId = source.checklistLinkedCardIds[i];
                    if (!string.IsNullOrWhiteSpace(linkedId) && idMap.TryGetValue(linkedId, out string remappedId))
                        source.checklistLinkedCardIds[i] = remappedId;
                }
                source.assigneeIds = (source.assigneeIds ?? new List<string>()).Select(id =>
                    !string.IsNullOrEmpty(id) && assigneeMap != null && assigneeMap.TryGetValue(id, out string mappedId) ? mappedId : id)
                    .Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                destination.column.cards ??= new List<TaskCard>();
                if (destination.existing != null)
                {
                    if (board?.columns != null)
                        foreach (TaskColumn oldColumn in board.columns) oldColumn?.cards?.Remove(destination.existing);
                    CopyCardContent(source, destination.existing);
                    destination.column.cards.Add(destination.existing);
                    if (report != null) report.updated++;
                }
                else
                {
                    source.taskNumber = 0;
                    source.taskCode = string.Empty;
                    destination.column.cards.Add(source);
                    existingCards.Add(source);
                    if (report != null) report.created++;
                }
            }
        }

        private static ImportPlan NewPlan(ImportItemKind kind, string sourceName, string targetName, string suggestedCopyName)
        {
            return new ImportPlan
            {
                kind = kind,
                sourceName = sourceName ?? string.Empty,
                targetName = targetName ?? string.Empty,
                suggestedCopyName = suggestedCopyName ?? string.Empty
            };
        }

        private static void AddCardsToPlan(ImportPlan plan, ImportPlanColumn columnPlan, IEnumerable<TaskCard> importedCards, IEnumerable<TaskCard> existingCards, IDictionary<string, string> assigneeNames)
        {
            foreach (TaskCard card in importedCards ?? Enumerable.Empty<TaskCard>())
            {
                if (card == null) continue;
                TaskCard existing = FindCardByKey(existingCards, card.id, card.taskCode);
                AddCardToPlan(plan, columnPlan, card, existing, assigneeNames);
            }
        }

        private static void AddCardToPlan(ImportPlan plan, ImportPlanColumn columnPlan, TaskCard card, TaskCard existing, IDictionary<string, string> assigneeNames)
        {
            if (card == null) return;
            string assignees = string.Join(", ", (card.assigneeIds ?? new List<string>()).Select(id =>
                id != null && assigneeNames != null && assigneeNames.TryGetValue(id, out string name) ? name : string.Empty).Where(name => !string.IsNullOrEmpty(name)));
            List<string> checklistItems = card.checklistItems ?? new List<string>();
            List<bool> checklistStates = card.checklistStates ?? new List<bool>();
            int completedChecklistItems = checklistItems.Select((item, index) => index < checklistStates.Count && checklistStates[index]).Count(done => done);
            string checklist = "Checklist: " + completedChecklistItems + "/" + checklistItems.Count + " complete";
            var details = new List<string>();
            if (card.priority > 0) details.Add("Priority " + card.priority);
            if (!string.IsNullOrWhiteSpace(assignees)) details.Add("Assignees: " + assignees);
            if (!string.IsNullOrWhiteSpace(card.dueDate)) details.Add("Due: " + card.dueDate);
            details.Add(checklist);
            columnPlan.cards.Add(new ImportPlanCard { title = card.title ?? "Untitled", details = string.Join(" · ", details), exists = existing != null });
            plan.totalCards++;
            if (existing != null) plan.matchingCards++;
        }

        private static string BuildRowDetails(string[] values, ImportFieldMapping mapping)
        {
            var details = new List<string>();
            string priority = GetValue(values, mapping.priorityIndex);
            if (!string.IsNullOrWhiteSpace(priority)) details.Add("Priority: " + priority);
            string assignees = GetValue(values, mapping.assigneeIndex);
            if (!string.IsNullOrWhiteSpace(assignees)) details.Add("Assignees: " + assignees);
            string dueDate = GetValue(values, mapping.dueDateIndex);
            if (!string.IsNullOrWhiteSpace(dueDate)) details.Add("Due: " + dueDate);
            List<ChecklistEntry> checklist = ParseChecklist(GetValue(values, mapping.checklistIndex));
            if (mapping.checklistIndex >= 0) details.Add("Checklist: " + checklist.Count(entry => entry.resolved) + "/" + checklist.Count + " complete");
            string description = GetValue(values, mapping.descriptionIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                description = description.Replace("\r", " ").Replace("\n", " ");
                if (description.Length > 90) description = description.Substring(0, 87) + "...";
                details.Add(description);
            }
            return string.Join(" · ", details);
        }

        private static Dictionary<string, string> BuildAssigneeNameMap(IEnumerable<Assignee> assignees)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assignee assignee in assignees ?? Enumerable.Empty<Assignee>())
            {
                if (assignee != null && !string.IsNullOrEmpty(assignee.id) && !result.ContainsKey(assignee.id))
                    result.Add(assignee.id, assignee.name ?? string.Empty);
            }
            return result;
        }

        private static IEnumerable<TaskCard> GetCardsFromBoard(TaskBoard board)
        {
            return board?.columns == null ? Enumerable.Empty<TaskCard>() : board.columns.Where(column => column?.cards != null).SelectMany(column => column.cards);
        }

        public static void ResolveSubtaskLinks(IDictionary<string, TaskCard> importedBySourceId, IEnumerable<ImportedRowInfo> rowsInfo)
        {
            if (importedBySourceId == null || rowsInfo == null) return;
            var rows = rowsInfo.Where(row => row?.card != null).ToList();
            foreach (ImportedRowInfo row in rows)
            {
                row.card.checklistLinkedCardIds = row.card.checklistLinkedCardIds ?? new List<string>();
                var refs = (row.subtaskIds ?? new List<string>())
                    .Concat(row.card.checklistLinkedCardIds ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (string id in refs)
                {
                    if (TryFindImportedCard(importedBySourceId, id, out TaskCard child))
                    {
                        row.card.checklistLinkedCardIds.RemoveAll(linkedId => string.Equals(linkedId, id, StringComparison.OrdinalIgnoreCase));
                        AddLink(row.card, child.id);
                    }
                }
                if (TryFindImportedCard(importedBySourceId, row.parentId, out TaskCard parent))
                    AddLink(parent, row.card.id);
            }
        }

        public static byte[] BuildXlsx(string sheetName, IEnumerable<string[]> rows)
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    WriteZipEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                    WriteZipEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    WriteZipEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"" + EscapeXml(SanitizeWorksheetName(sheetName)) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                    WriteZipEntry(archive, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs></styleSheet>");

                    var worksheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                    int rowNumber = 0;
                    foreach (string[] row in rows ?? Enumerable.Empty<string[]>())
                    {
                        rowNumber++;
                        worksheet.Append("<row r=\"").Append(rowNumber).Append("\">");
                        if (row != null)
                        {
                            for (int column = 0; column < row.Length; column++)
                            {
                                worksheet.Append("<c r=\"").Append(ColumnName(column)).Append(rowNumber).Append("\" t=\"inlineStr\"");
                                if (rowNumber == 1) worksheet.Append(" s=\"1\"");
                                worksheet.Append("><is><t xml:space=\"preserve\">").Append(EscapeXml(row[column])).Append("</t></is></c>");
                            }
                        }
                        worksheet.Append("</row>");
                    }
                    worksheet.Append("</sheetData></worksheet>");
                    WriteZipEntry(archive, "xl/worksheets/sheet1.xml", worksheet.ToString());
                }
                return stream.ToArray();
            }
        }

        public static string BuildSpreadsheetXml(string sheetName, IEnumerable<string[]> rows)
        {
            var output = new StringBuilder();
            output.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            output.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            output.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            output.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            output.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            output.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            output.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            output.AppendLine(" <Styles><Style ss:ID=\"sHeader\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/></Style></Styles>");
            output.Append(" <Worksheet ss:Name=\"").Append(EscapeXml(SanitizeWorksheetName(sheetName))).AppendLine("\">");
            output.AppendLine("  <Table>");

            bool isHeader = true;
            if (rows != null)
            {
                foreach (string[] row in rows)
                {
                    output.Append(isHeader ? "   <Row ss:StyleID=\"sHeader\">" : "   <Row>").AppendLine();
                    if (row != null)
                    {
                        foreach (string value in row)
                        {
                            output.Append("    <Cell><Data ss:Type=\"String\" xml:space=\"preserve\">")
                                .Append(EscapeXml(value).Replace("\r\n", "&#10;").Replace("\r", "&#10;").Replace("\n", "&#10;"))
                                .AppendLine("</Data></Cell>");
                        }
                    }
                    output.AppendLine("   </Row>");
                    isHeader = false;
                }
            }

            output.AppendLine("  </Table>");
            output.AppendLine(" </Worksheet>");
            output.AppendLine("</Workbook>");
            return output.ToString();
        }

        public static string SanitizeWorksheetName(string name)
        {
            var sanitized = new string((name ?? string.Empty).Where(c => "[]:*?/\\".IndexOf(c) < 0).ToArray()).Trim('\'', ' ');
            if (sanitized.Length > 31) sanitized = sanitized.Substring(0, 31);
            return string.IsNullOrWhiteSpace(sanitized) ? "Sheet1" : sanitized;
        }

        public static List<string[]> ParseXlsx(string path)
        {
            var rows = new List<string[]>();
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                var sharedStrings = new List<string>();
                ZipArchiveEntry sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (sharedEntry != null)
                {
                    XmlDocument sharedXml = LoadXml(sharedEntry);
                    foreach (XmlNode stringItem in sharedXml.SelectNodes("//*[local-name()='si']"))
                    {
                        var value = new StringBuilder();
                        foreach (XmlNode textNode in stringItem.SelectNodes(".//*[local-name()='t' and not(ancestor::*[local-name()='rPh'])]"))
                            value.Append(textNode.InnerText);
                        sharedStrings.Add(value.ToString());
                    }
                }

                ZipArchiveEntry sheetEntry = FindFirstWorksheet(archive);
                if (sheetEntry == null) return rows;
                XmlDocument sheet = LoadXml(sheetEntry);
                foreach (XmlNode rowNode in sheet.SelectNodes("//*[local-name()='sheetData']/*[local-name()='row']"))
                {
                    var values = new List<string>();
                    int lastColumn = -1;
                    foreach (XmlNode cell in rowNode.SelectNodes("./*[local-name()='c']"))
                    {
                        string reference = cell.Attributes?["r"]?.Value;
                        int column = string.IsNullOrEmpty(reference) ? lastColumn + 1 : GetColumnIndex(reference);
                        if (column < 0) column = lastColumn + 1;
                        while (values.Count < column) values.Add(string.Empty);

                        string type = cell.Attributes?["t"]?.Value;
                        string value = string.Empty;
                        if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
                        {
                            var text = new StringBuilder();
                            foreach (XmlNode textNode in cell.SelectNodes("./*[local-name()='is']//*[local-name()='t']"))
                                text.Append(textNode.InnerText);
                            value = text.ToString();
                        }
                        else
                        {
                            XmlNode valueNode = cell.SelectSingleNode("./*[local-name()='v']");
                            string raw = valueNode?.InnerText ?? string.Empty;
                            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(raw, out int sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                                    value = sharedStrings[sharedIndex];
                            }
                            else if (string.Equals(type, "b", StringComparison.OrdinalIgnoreCase))
                            {
                                value = raw == "1" ? "TRUE" : raw == "0" ? "FALSE" : raw;
                            }
                            else
                            {
                                value = raw;
                            }
                        }

                        values.Add(value);
                        lastColumn = column;
                    }
                    rows.Add(values.ToArray());
                }
            }
            return rows;
        }

        public static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference)) return -1;
            int column = 0;
            bool foundLetter = false;
            foreach (char character in cellReference)
            {
                if (!char.IsLetter(character)) break;
                char upper = char.ToUpperInvariant(character);
                if (upper < 'A' || upper > 'Z') return -1;
                column = column * 26 + upper - 'A' + 1;
                foundLetter = true;
            }
            return foundLetter ? column - 1 : -1;
        }

        public static List<string[]> ParseXmlSpreadsheet(string path)
        {
            var rows = new List<string[]>();
            var xml = new XmlDocument();
            xml.Load(path);
            XmlNode worksheet = xml.SelectSingleNode("//*[local-name()='Worksheet']");
            if (worksheet == null) return rows;
            XmlNodeList rowNodes = worksheet.SelectNodes(".//*[local-name()='Table']/*[local-name()='Row']");
            foreach (XmlNode rowNode in rowNodes)
            {
                var values = new List<string>();
                foreach (XmlNode cell in rowNode.SelectNodes("./*[local-name()='Cell']"))
                {
                    XmlAttribute indexAttribute = cell.Attributes?["Index", SpreadsheetNamespace];
                    if (indexAttribute != null && int.TryParse(indexAttribute.Value, out int oneBasedIndex) && oneBasedIndex > 0)
                    {
                        while (values.Count < oneBasedIndex - 1) values.Add(string.Empty);
                    }
                    XmlNode data = cell.SelectSingleNode("./*[local-name()='Data']");
                    values.Add(data?.InnerText ?? string.Empty);
                }
                rows.Add(values.ToArray());
            }
            return rows;
        }

        public static Dictionary<string, string> MergeAssignees(List<Assignee> existing, IEnumerable<Assignee> imported)
        {
            existing = existing ?? new List<Assignee>();
            var idMap = new Dictionary<string, string>();
            if (imported == null) return idMap;

            foreach (Assignee assignee in imported)
            {
                if (assignee == null) continue;
                string oldId = assignee.id;
                Assignee match = existing.FirstOrDefault(a => a != null &&
                    ((!string.IsNullOrEmpty(oldId) && a.id == oldId) ||
                     (!string.IsNullOrWhiteSpace(assignee.name) && string.Equals(a.name, assignee.name, StringComparison.OrdinalIgnoreCase))));
                if (match == null)
                {
                    if (string.IsNullOrEmpty(assignee.id)) assignee.id = Guid.NewGuid().ToString();
                    existing.Add(assignee);
                    match = assignee;
                }
                if (!string.IsNullOrEmpty(oldId)) idMap[oldId] = match.id;
            }
            return idMap;
        }

        public static void PrepareImportedCards(IEnumerable<TaskCard> cards, IDictionary<string, string> assigneeMap)
        {
            if (cards == null) return;
            var cardList = cards.Where(card => card != null).ToList();
            var cardIdMap = new Dictionary<string, string>();
            foreach (TaskCard card in cardList)
            {
                string oldId = card.id;
                string newId = Guid.NewGuid().ToString();
                if (!string.IsNullOrEmpty(oldId) && !cardIdMap.ContainsKey(oldId)) cardIdMap.Add(oldId, newId);
                card.id = newId;
                card.taskNumber = 0;
                card.taskCode = string.Empty;
                card.checklistItems = card.checklistItems ?? new List<string>();
                card.checklistStates = card.checklistStates ?? new List<bool>();
                card.checklistLinkedCardIds = card.checklistLinkedCardIds ?? new List<string>();
                card.assigneeIds = card.assigneeIds ?? new List<string>();
                card.linkedAssetGuids = card.linkedAssetGuids ?? new List<string>();
                card.linkedSceneObjects = card.linkedSceneObjects ?? new List<SceneObjectReference>();
                card.linkedItems = card.linkedItems ?? new List<LinkedItem>();
            }

            foreach (TaskCard card in cardList)
            {
                for (int i = 0; i < card.checklistLinkedCardIds.Count; i++)
                {
                    string linkedId = card.checklistLinkedCardIds[i];
                    if (!string.IsNullOrEmpty(linkedId) && cardIdMap.TryGetValue(linkedId, out string newLinkedId))
                        card.checklistLinkedCardIds[i] = newLinkedId;
                }

                var remappedAssignees = new List<string>();
                foreach (string assigneeId in card.assigneeIds)
                {
                    if (!string.IsNullOrEmpty(assigneeId) && assigneeMap != null && assigneeMap.TryGetValue(assigneeId, out string newAssigneeId) &&
                        !string.IsNullOrEmpty(newAssigneeId) && !remappedAssignees.Contains(newAssigneeId))
                        remappedAssignees.Add(newAssigneeId);
                }
                card.assigneeIds = remappedAssignees;
            }
        }

        public static TaskColumn PrepareImportedColumn(TaskColumn column, IDictionary<string, string> assigneeMap)
        {
            if (column == null) return null;
            column.id = Guid.NewGuid().ToString();
            column.cards = column.cards ?? new List<TaskCard>();
            PrepareImportedCards(column.cards, assigneeMap);
            return column;
        }

        public static TaskBoard PrepareImportedBoard(TaskBoard board, IDictionary<string, string> assigneeMap)
        {
            if (board == null) return null;
            board.id = Guid.NewGuid().ToString();
            board.columns = board.columns ?? new List<TaskColumn>();
            var cards = new List<TaskCard>();
            foreach (TaskColumn column in board.columns)
            {
                if (column == null) continue;
                column.id = Guid.NewGuid().ToString();
                column.cards = column.cards ?? new List<TaskCard>();
                cards.AddRange(column.cards.Where(card => card != null));
            }
            PrepareImportedCards(cards, assigneeMap);
            return board;
        }

        public static string GetUniqueFilePath(string directory, string name, string extension)
        {
            string safeName = string.IsNullOrWhiteSpace(name) ? "Export" : name;
            string normalizedExtension = string.IsNullOrEmpty(extension) ? string.Empty : extension.StartsWith(".") ? extension : "." + extension;
            string path = Path.Combine(directory, safeName + normalizedExtension);
            int suffix = 2;
            while (File.Exists(path))
            {
                path = Path.Combine(directory, safeName + " (" + suffix + ")" + normalizedExtension);
                suffix++;
            }
            return path;
        }

        private static void AddListItem(List<string> result, StringBuilder item)
        {
            string value = item.ToString().Trim();
            if (value.Length > 0) result.Add(value);
            item.Clear();
        }

        private static bool MatchesStatus(string status, IEnumerable<string> builtInNames, string customNames)
        {
            string value = (status ?? string.Empty).Trim();
            if (value.Length == 0) return false;
            return (builtInNames ?? Enumerable.Empty<string>()).Concat(ParseList(customNames))
                .Any(name => string.Equals(value, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string GetValue(string[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? (values[index] ?? string.Empty).Trim() : string.Empty;
        }

        private static void AddLink(TaskCard card, string linkedId)
        {
            if (card == null || string.IsNullOrWhiteSpace(linkedId)) return;
            card.checklistLinkedCardIds = card.checklistLinkedCardIds ?? new List<string>();
            if (!card.checklistLinkedCardIds.Contains(linkedId)) card.checklistLinkedCardIds.Add(linkedId);
        }

        private static bool TryFindImportedCard(IDictionary<string, TaskCard> importedBySourceId, string sourceKey, out TaskCard card)
        {
            card = null;
            if (string.IsNullOrWhiteSpace(sourceKey)) return false;
            string key = sourceKey.Trim();
            return importedBySourceId.TryGetValue(key, out card) || importedBySourceId.TryGetValue(key.ToLowerInvariant(), out card);
        }

        private static void WriteZipEntry(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(content);
        }

        private static string ColumnName(int zeroBasedColumn)
        {
            int value = zeroBasedColumn + 1;
            var name = new StringBuilder();
            while (value > 0)
            {
                value--;
                name.Insert(0, (char)('A' + value % 26));
                value /= 26;
            }
            return name.ToString();
        }

        private static ZipArchiveEntry FindFirstWorksheet(ZipArchive archive)
        {
            ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
            ZipArchiveEntry relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbookEntry != null && relationshipsEntry != null)
            {
                XmlDocument workbook = LoadXml(workbookEntry);
                XmlNode firstSheet = workbook.SelectSingleNode("//*[local-name()='sheets']/*[local-name()='sheet']");
                string relationshipId = firstSheet?.Attributes?["id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"]?.Value;
                if (!string.IsNullOrEmpty(relationshipId))
                {
                    XmlDocument relationships = LoadXml(relationshipsEntry);
                    XmlNode relationship = null;
                    foreach (XmlNode candidate in relationships.SelectNodes("//*[local-name()='Relationship']"))
                    {
                        if (candidate.Attributes?["Id"]?.Value == relationshipId)
                        {
                            relationship = candidate;
                            break;
                        }
                    }
                    string target = relationship?.Attributes?["Target"]?.Value;
                    if (!string.IsNullOrEmpty(target))
                    {
                        string entryName = ResolveZipPath("xl", target);
                        ZipArchiveEntry resolvedEntry = archive.GetEntry(entryName);
                        if (resolvedEntry != null) return resolvedEntry;
                    }
                }
            }

            return archive.Entries
                .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) &&
                                entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        private static string ResolveZipPath(string basePath, string target)
        {
            string combined = target.StartsWith("/", StringComparison.Ordinal) ? target.Substring(1) : basePath + "/" + target;
            var parts = new List<string>();
            foreach (string part in combined.Replace('\\', '/').Split('/'))
            {
                if (string.IsNullOrEmpty(part) || part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
                }
                else
                {
                    parts.Add(part);
                }
            }
            return string.Join("/", parts);
        }

        private static XmlDocument LoadXml(ZipArchiveEntry entry)
        {
            var document = new XmlDocument();
            using (Stream stream = entry.Open()) document.Load(stream);
            return document;
        }

        private static string EscapeXml(string value)
        {
            var safe = new StringBuilder();
            value = value ?? string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsHighSurrogate(character))
                {
                    if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                        safe.Append(character).Append(value[++i]);
                    continue;
                }
                if (char.IsLowSurrogate(character)) continue;
                if (character == '\t' || character == '\n' || character == '\r' ||
                    (character >= 0x20 && character <= 0xD7FF) ||
                    (character >= 0xE000 && character <= 0xFFFD))
                    safe.Append(character);
            }
            return SecurityElement.Escape(safe.ToString()) ?? string.Empty;
        }
    }
}