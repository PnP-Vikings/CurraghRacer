using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AwesomeTaskManager.Editor;

namespace AwesomeTaskManager.Data
{
    public static class ExternalJsonImportExport
    {
        public static string DetectFormat(string json)
        {
            try
            {
                return DetectFormat(new JsonReader(json).Read());
            }
            catch
            {
                return null;
            }
        }

        public static bool TryImport(string json, out ExportBoardData data, out string format, List<string> warnings)
        {
            data = null;
            object root;
            try
            {
                root = new JsonReader(json).Read();
            }
            catch (Exception exception)
            {
                warnings?.Add("Invalid JSON: " + exception.Message);
                format = null;
                return false;
            }

            format = DetectFormat(root);
            try
            {
                switch (format)
                {
                    case "Trello": data = ImportTrello(AsObject(root), warnings); break;
                    case "ClickUp": data = ImportTasks(AsArray(GetValue(AsObject(root), "tasks")), GetText(AsObject(root), "name", "team_name"), true, warnings); break;
                    case "Jira": data = ImportJira(AsObject(root), warnings); break;
                    case "Generic": data = ImportGeneric(root, warnings); break;
                    case "Native":
                        warnings?.Add("This is a native Awesome Task Manager board file.");
                        return false;
                    default:
                        warnings?.Add("The JSON format was not recognized. Supported formats are Trello, ClickUp, Jira, and generic task arrays.");
                        return false;
                }
                return data?.board != null;
            }
            catch (Exception exception)
            {
                data = null;
                warnings?.Add("Could not import " + (format ?? "JSON") + " data: " + exception.Message);
                return false;
            }
        }

        public static string BuildTrelloJson(TaskBoard board, IEnumerable<Assignee> assignees, IEnumerable<CategoryColorEntry> categoryColors)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var membersById = (assignees ?? Enumerable.Empty<Assignee>())
                .Where(member => member != null && !string.IsNullOrWhiteSpace(member.id))
                .GroupBy(member => member.id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var colorsByCategory = (categoryColors ?? Enumerable.Empty<CategoryColorEntry>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.category))
                .GroupBy(entry => entry.category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().colorIndex, StringComparer.OrdinalIgnoreCase);
            var labelsByCategory = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            var lists = new List<object>();
            var cards = new List<object>();
            var checklists = new List<object>();
            int cardIndex = 0;
            int columnIndex = 0;

            foreach (TaskColumn column in board.columns ?? new List<TaskColumn>())
            {
                if (column == null) continue;
                string listId = NonEmpty(column.id, "list-" + columnIndex);
                lists.Add(new Dictionary<string, object>
                {
                    ["id"] = listId,
                    ["idBoard"] = NonEmpty(board.id, "board"),
                    ["name"] = column.title ?? string.Empty,
                    ["closed"] = false,
                    ["pos"] = columnIndex++ * 16384
                });

                foreach (TaskCard card in column.cards ?? new List<TaskCard>())
                {
                    if (card == null) continue;
                    string cardId = NonEmpty(card.id, "card-" + cardIndex);
                    var cardLabels = new List<object>();
                    if (!string.IsNullOrWhiteSpace(card.category))
                    {
                        int labelColor = colorsByCategory.TryGetValue(card.category, out int categoryColor) && categoryColor > 0
                            ? categoryColor
                            : card.colorLabel;
                        string labelId = "label-" + card.category;
                        var label = new Dictionary<string, object>
                        {
                            ["id"] = labelId,
                            ["name"] = card.category,
                            ["color"] = BoardImportExport.GetTrelloColorName(labelColor)
                        };
                        cardLabels.Add(label);
                        labelsByCategory[card.category] = label;
                    }

                    var memberIds = new List<object>();
                    foreach (string assigneeId in card.assigneeIds ?? new List<string>())
                    {
                        if (string.IsNullOrWhiteSpace(assigneeId)) continue;
                        memberIds.Add(assigneeId);
                        if (!membersById.ContainsKey(assigneeId))
                            membersById[assigneeId] = new Assignee { id = assigneeId, name = assigneeId };
                    }

                    cards.Add(new Dictionary<string, object>
                    {
                        ["id"] = cardId,
                        ["idBoard"] = NonEmpty(board.id, "board"),
                        ["idList"] = listId,
                        ["name"] = card.title ?? string.Empty,
                        ["desc"] = card.description ?? string.Empty,
                        ["pos"] = cardIndex++ * 16384,
                        ["closed"] = card.archived,
                        ["due"] = string.IsNullOrWhiteSpace(card.dueDate) ? null : card.dueDate,
                        ["dueComplete"] = card.completed,
                        ["idMembers"] = memberIds,
                        ["labels"] = cardLabels,
                        ["attachments"] = GetUrlAttachments(card),
                        ["atmPriority"] = card.priority,
                        ["atmTaskCode"] = card.taskCode ?? string.Empty,
                        ["atmColor"] = card.colorLabel,
                        ["atmCompleted"] = card.completed,
                        ["atmArchived"] = card.archived,
                        ["atmLinks"] = BoardImportExport.FormatLinks(card),
                        ["atmChecklistLinks"] = card.checklistLinkedCardIds ?? new List<string>()
                    });

                    if (card.checklistItems != null && card.checklistItems.Count > 0)
                    {
                        var items = new List<object>();
                        for (int i = 0; i < card.checklistItems.Count; i++)
                        {
                            bool complete = card.checklistStates != null && i < card.checklistStates.Count && card.checklistStates[i];
                            items.Add(new Dictionary<string, object>
                            {
                                ["id"] = cardId + "-check-" + i,
                                ["name"] = card.checklistItems[i] ?? string.Empty,
                                ["state"] = complete ? "complete" : "incomplete",
                                ["pos"] = i * 16384
                            });
                        }
                        checklists.Add(new Dictionary<string, object>
                        {
                            ["id"] = cardId + "-checklist",
                            ["idBoard"] = NonEmpty(board.id, "board"),
                            ["idCard"] = cardId,
                            ["name"] = "Checklist",
                            ["pos"] = 0,
                            ["checkItems"] = items
                        });
                    }
                }
            }

            var labels = labelsByCategory.Values.Cast<object>().ToList();
            var members = membersById.Values.Select(member => (object)new Dictionary<string, object>
            {
                ["id"] = member.id,
                ["fullName"] = member.name ?? string.Empty,
                ["username"] = member.name ?? string.Empty
            }).ToList();
            var root = new Dictionary<string, object>
            {
                ["id"] = NonEmpty(board.id, "board"),
                ["name"] = board.name ?? string.Empty,
                ["desc"] = string.Empty,
                ["closed"] = false,
                ["lists"] = lists,
                ["cards"] = cards,
                ["checklists"] = checklists,
                ["labels"] = labels,
                ["members"] = members,
            };
            var output = new StringBuilder();
            WriteJsonValue(output, root);
            return output.ToString();
        }

        private static ExportBoardData ImportTrello(Dictionary<string, object> source, List<string> warnings)
        {
            string boardName = GetText(source, "name");
            var board = new TaskBoard(string.IsNullOrWhiteSpace(boardName) ? "Trello Import" : boardName)
            {
                id = GetText(source, "id")
            };
            board.columns.Clear();
            var data = new ExportBoardData { board = board };
            var columnsById = new Dictionary<string, TaskColumn>(StringComparer.OrdinalIgnoreCase);
            var closedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object listValue in SortByPosition(AsArray(GetValue(source, "lists"))))
            {
                Dictionary<string, object> list = AsObject(listValue);
                if (list == null) continue;
                string listId = GetText(list, "id");
                string title = GetText(list, "name");
                var column = new TaskColumn(string.IsNullOrWhiteSpace(title) ? "To Do" : title) { id = NonEmpty(listId, Guid.NewGuid().ToString()) };
                board.columns.Add(column);
                if (!string.IsNullOrWhiteSpace(listId)) columnsById[listId] = column;
                if (ToBool(GetValue(list, "closed")) && !string.IsNullOrWhiteSpace(listId)) closedColumns.Add(listId);
            }

            var assigneesById = new Dictionary<string, Assignee>(StringComparer.OrdinalIgnoreCase);
            foreach (object memberValue in AsArray(GetValue(source, "members")))
            {
                Dictionary<string, object> member = AsObject(memberValue);
                if (member == null) continue;
                AddAssignee(data, assigneesById, GetText(member, "id"), GetText(member, "fullName", "username", "name"));
            }

            var checklistsByCard = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            foreach (object checklistValue in AsArray(GetValue(source, "checklists")))
            {
                Dictionary<string, object> checklist = AsObject(checklistValue);
                string cardId = GetText(checklist, "idCard");
                if (string.IsNullOrWhiteSpace(cardId)) continue;
                if (!checklistsByCard.TryGetValue(cardId, out List<Dictionary<string, object>> cardChecklists))
                    checklistsByCard[cardId] = cardChecklists = new List<Dictionary<string, object>>();
                cardChecklists.Add(checklist);
            }

            foreach (object cardValue in SortByPosition(AsArray(GetValue(source, "cards"))))
            {
                Dictionary<string, object> cardSource = AsObject(cardValue);
                if (cardSource == null) continue;
                string listId = GetText(cardSource, "idList");
                if (!columnsById.TryGetValue(listId, out TaskColumn column))
                {
                    if (board.columns.Count == 0)
                    {
                        column = new TaskColumn("To Do");
                        board.columns.Add(column);
                    }
                    else column = board.columns[0];
                }

                TaskCard card = CreateCard(cardSource, true);
                card.archived = ToBool(GetValue(cardSource, "closed")) || closedColumns.Contains(listId);
                card.completed = ToBool(GetValue(cardSource, "dueComplete"));
                if (HasValue(cardSource, "atmArchived")) card.archived = ToBool(GetValue(cardSource, "atmArchived"));
                if (HasValue(cardSource, "atmCompleted")) card.completed = ToBool(GetValue(cardSource, "atmCompleted"));
                if (HasValue(cardSource, "atmPriority")) card.priority = ToInt(GetValue(cardSource, "atmPriority"), card.priority);
                if (HasValue(cardSource, "atmTaskCode")) card.taskCode = GetText(cardSource, "atmTaskCode");
                if (HasValue(cardSource, "atmColor")) card.colorLabel = ToInt(GetValue(cardSource, "atmColor"), card.colorLabel);

                foreach (object labelValue in AsArray(GetValue(cardSource, "labels")))
                {
                    Dictionary<string, object> label = AsObject(labelValue);
                    string labelName = label == null ? AsText(labelValue) : GetText(label, "name");
                    if (string.IsNullOrWhiteSpace(labelName)) continue;
                    if (string.IsNullOrWhiteSpace(card.category)) card.category = labelName;
                    string colorName = GetText(label, "color");
                    if (BoardImportExport.TryGetLabelColorIndex(colorName, out int colorIndex))
                    {
                        if (card.colorLabel == 0) card.colorLabel = colorIndex;
                        SetCategoryColor(data, labelName, colorIndex);
                    }
                }

                foreach (object memberIdValue in AsArray(GetValue(cardSource, "idMembers")))
                {
                    string memberId = AsText(memberIdValue);
                    if (string.IsNullOrWhiteSpace(memberId)) continue;
                    if (!assigneesById.TryGetValue(memberId, out Assignee assignee))
                    {
                        assignee = AddAssignee(data, assigneesById, memberId, memberId);
                        warnings?.Add("Trello member " + memberId + " was not included in the JSON; its ID is used as the name.");
                    }
                    card.assigneeIds.Add(assignee.id);
                }

                if (checklistsByCard.TryGetValue(card.id, out List<Dictionary<string, object>> sourceChecklists))
                    foreach (Dictionary<string, object> checklist in sourceChecklists)
                        AppendTrelloChecklist(card, checklist);
                foreach (object attachmentValue in AsArray(GetValue(cardSource, "attachments")))
                {
                    Dictionary<string, object> attachment = AsObject(attachmentValue);
                    string url = GetText(attachment, "url");
                    if (!string.IsNullOrWhiteSpace(url)) AddLinkIfMissing(card, LinkedItem.CreateUrl(url, GetText(attachment, "name")));
                }
                string shortUrl = GetText(cardSource, "shortUrl");
                if (!string.IsNullOrWhiteSpace(shortUrl)) AddLinkIfMissing(card, LinkedItem.CreateUrl(shortUrl, "Trello card"));
                if (HasValue(cardSource, "atmLinks"))
                    foreach (LinkedItem link in BoardImportExport.ParseLinks(GetText(cardSource, "atmLinks")))
                        AddLinkIfMissing(card, link);
                foreach (object linkedCardId in AsArray(GetValue(cardSource, "atmChecklistLinks")))
                    if (!string.IsNullOrWhiteSpace(AsText(linkedCardId))) card.checklistLinkedCardIds.Add(AsText(linkedCardId));
                column.cards.Add(card);
            }

            if (board.columns.Count == 0) board.columns.Add(new TaskColumn("To Do"));
            return data;
        }

        private static ExportBoardData ImportJira(Dictionary<string, object> source, List<string> warnings)
        {
            string projectName = GetText(source, "name");
            if (string.IsNullOrWhiteSpace(projectName))
                projectName = GetText(AsObject(GetValue(source, "project")), "name");
            if (string.IsNullOrWhiteSpace(projectName)) projectName = "Jira Import";
            return ImportTasks(AsArray(GetValue(source, "issues")), projectName, false, warnings, true);
        }

        private static ExportBoardData ImportGeneric(object root, List<string> warnings)
        {
            Dictionary<string, object> rootObject = AsObject(root);
            IList<object> items = root is IList<object> directItems
                ? directItems
                : AsArray(GetValue(rootObject, "cards", "tasks", "items", "issues"));
            string name = GetText(rootObject, "name", "boardName", "title");
            if (string.IsNullOrWhiteSpace(name)) name = "Imported JSON";
            return ImportTasks(items, name, false, warnings);
        }

        private static ExportBoardData ImportTasks(IList<object> items, string boardName, bool clickUp, List<string> warnings, bool jira = false)
        {
            if (string.IsNullOrWhiteSpace(boardName)) boardName = clickUp ? "ClickUp Import" : jira ? "Jira Import" : "Imported JSON";
            var board = new TaskBoard(boardName);
            board.columns.Clear();
            var data = new ExportBoardData { board = board };
            var columnsByName = new Dictionary<string, TaskColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (object itemValue in items ?? new List<object>())
            {
                Dictionary<string, object> item = AsObject(itemValue);
                if (item == null)
                {
                    warnings?.Add("A JSON task entry was not an object and was skipped.");
                    continue;
                }
                Dictionary<string, object> fields = jira ? AsObject(GetValue(item, "fields")) ?? item : item;
                string status = clickUp
                    ? GetText(AsObject(GetValue(fields, "status")), "status", "name")
                    : GetText(AsObject(GetValue(fields, "status")), "name", "status");
                if (string.IsNullOrWhiteSpace(status)) status = GetText(fields, "status", "list", "column", "bucket");
                if (string.IsNullOrWhiteSpace(status)) status = "To Do";
                if (!columnsByName.TryGetValue(status, out TaskColumn column))
                {
                    column = new TaskColumn(status);
                    board.columns.Add(column);
                    columnsByName[status] = column;
                }

                TaskCard card = CreateCard(fields, clickUp);
                if (jira) card.taskCode = GetText(item, "key");
                ApplyTaskFields(card, fields, data, clickUp, jira);
                column.cards.Add(card);
            }

            if (board.columns.Count == 0) board.columns.Add(new TaskColumn("To Do"));
            return data;
        }

        private static TaskCard CreateCard(Dictionary<string, object> source, bool clickUp)
        {
            string id = GetText(source, "id", "taskId", "key");
            string title = GetText(source, "name", "title", "summary");
            string description = GetText(source, "desc", "description", "content", "text");
            var card = new TaskCard(string.IsNullOrWhiteSpace(title) ? "Untitled" : title)
            {
                id = NonEmpty(id, Guid.NewGuid().ToString()),
                description = description,
                dueDate = BoardImportExport.NormalizeDate(GetText(source, "due", "due_date", "dueDate", "duedate")),
                completed = ToBool(GetValue(source, "completed", "dueComplete", "due_complete")),
                archived = ToBool(GetValue(source, "archived", "closed")),
                priority = BoardImportExport.ParsePriority(GetText(source, "priority"), clickUp),
                taskCode = GetText(source, "taskCode", "task_code", "custom_id")
            };
            object atmPriority = GetValue(source, "atmPriority");
            if (atmPriority != null) card.priority = ToInt(atmPriority, card.priority);
            object atmColor = GetValue(source, "atmColor", "colorLabel", "color_label");
            if (atmColor != null) card.colorLabel = ToInt(atmColor, 0);
            return card;
        }

        private static void ApplyTaskFields(TaskCard card, Dictionary<string, object> source, ExportBoardData data, bool clickUp, bool jira)
        {
            object priorityValue = GetValue(source, "priority");
            if (priorityValue != null)
            {
                string priorityText = priorityValue is IDictionary<string, object> priorityObject
                    ? GetText(priorityObject, "priority", "name", "value")
                    : AsText(priorityValue);
                card.priority = BoardImportExport.ParsePriority(priorityText, clickUp);
            }

            object labelsValue = GetValue(source, "labels", "tags", "categories");
            List<string> labels = ReadNames(labelsValue, "name", "label", "value");
            if (labels.Count == 0)
            {
                string category = GetText(source, "category", "tag");
                if (!string.IsNullOrWhiteSpace(category)) labels.Add(category);
            }
            if (labels.Count > 0)
            {
                card.category = labels[0];
                object firstLabel = AsArray(labelsValue).FirstOrDefault();
                string colorName = GetText(AsObject(firstLabel), "color");
                if (BoardImportExport.TryGetLabelColorIndex(colorName, out int categoryColor))
                {
                    card.colorLabel = card.colorLabel == 0 ? categoryColor : card.colorLabel;
                    SetCategoryColor(data, card.category, categoryColor);
                }
            }

            object assigneeValue = jira ? GetValue(source, "assignee") : GetValue(source, "assignees", "assignee", "members");
            if (assigneeValue != null)
            {
                foreach (object assigneeValueItem in assigneeValue is IList<object> values ? values : new List<object> { assigneeValue })
                {
                    Dictionary<string, object> assigneeObject = AsObject(assigneeValueItem);
                    string id = GetText(assigneeObject, "id", "accountId", "username", "email");
                    string name = GetText(assigneeObject, "fullName", "displayName", "name", "username", "email");
                    if (assigneeObject == null) name = AsText(assigneeValueItem);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    Assignee assignee = AddAssignee(data, null, id, name);
                    if (!card.assigneeIds.Contains(assignee.id)) card.assigneeIds.Add(assignee.id);
                }
            }

            object checklistValue = GetValue(source, "checklists", "checklist", "check_items");
            AppendGenericChecklist(card, checklistValue);
            foreach (object linkValue in AsArray(GetValue(source, "attachments", "links")))
            {
                Dictionary<string, object> link = AsObject(linkValue);
                string url = link == null ? AsText(linkValue) : GetText(link, "url", "href", "link");
                if (!string.IsNullOrWhiteSpace(url)) card.linkedItems.Add(LinkedItem.CreateUrl(url, GetText(link, "name", "title")));
            }
            if (HasValue(source, "atmLinks"))
                foreach (LinkedItem link in BoardImportExport.ParseLinks(GetText(source, "atmLinks")))
                    AddLinkIfMissing(card, link);
            foreach (object linkedId in AsArray(GetValue(source, "atmChecklistLinks", "checklistLinkedCardIds")))
                if (!string.IsNullOrWhiteSpace(AsText(linkedId))) card.checklistLinkedCardIds.Add(AsText(linkedId));
            if (HasValue(source, "atmCompleted")) card.completed = ToBool(GetValue(source, "atmCompleted"));
            if (HasValue(source, "atmArchived")) card.archived = ToBool(GetValue(source, "atmArchived"));
            if (HasValue(source, "atmTaskCode")) card.taskCode = GetText(source, "atmTaskCode");
            if (HasValue(source, "atmColor")) card.colorLabel = ToInt(GetValue(source, "atmColor"), card.colorLabel);

            string status = GetText(AsObject(GetValue(source, "status")), "status", "name");
            if (string.IsNullOrWhiteSpace(status)) status = GetText(source, "status", "state");
            if (!card.completed && BoardImportExport.IsDoneStatus(status)) card.completed = true;
            if (BoardImportExport.IsArchivedStatus(status)) card.archived = true;
        }

        private static void AppendTrelloChecklist(TaskCard card, Dictionary<string, object> checklist)
        {
            foreach (object itemValue in SortByPosition(AsArray(GetValue(checklist, "checkItems"))))
            {
                Dictionary<string, object> item = AsObject(itemValue);
                string text = GetText(item, "name", "text");
                if (string.IsNullOrWhiteSpace(text)) continue;
                card.checklistItems.Add(text);
                card.checklistStates.Add(GetText(item, "state").Equals("complete", StringComparison.OrdinalIgnoreCase) || ToBool(GetValue(item, "checked")));
            }
        }

        private static void AppendGenericChecklist(TaskCard card, object source)
        {
            if (source is IDictionary<string, object> checklistObject)
            {
                source = GetValue(checklistObject, "items", "checklist", "checkItems", "check_items");
            }
            foreach (object itemValue in AsArray(source))
            {
                Dictionary<string, object> item = AsObject(itemValue);
                object nestedItems = GetValue(item, "items", "checklist", "checkItems", "check_items");
                if (nestedItems != null)
                {
                    AppendGenericChecklist(card, nestedItems);
                    continue;
                }
                string text = item == null ? AsText(itemValue) : GetText(item, "name", "text", "title", "label");
                if (string.IsNullOrWhiteSpace(text)) continue;
                card.checklistItems.Add(text);
                card.checklistStates.Add(ToBool(GetValue(item, "resolved", "complete", "completed", "checked")) ||
                                         GetText(item, "state").Equals("complete", StringComparison.OrdinalIgnoreCase));
            }
        }

        private static List<string> ReadNames(object source, params string[] keys)
        {
            var result = new List<string>();
            if (source is IList<object> list)
            {
                foreach (object value in list)
                {
                    string name = value is IDictionary<string, object> dictionary ? GetText(dictionary, keys) : AsText(value);
                    if (!string.IsNullOrWhiteSpace(name)) result.Add(name);
                }
            }
            else if (source != null)
            {
                string name = source is IDictionary<string, object> dictionary ? GetText(dictionary, keys) : AsText(source);
                if (!string.IsNullOrWhiteSpace(name)) result.Add(name);
            }
            return result;
        }

        private static Assignee AddAssignee(ExportBoardData data, IDictionary<string, Assignee> byId, string id, string name)
        {
            data.assignees ??= new List<Assignee>();
            id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
            name = string.IsNullOrWhiteSpace(name) ? id : name.Trim();
            if (byId != null && byId.TryGetValue(id, out Assignee existingById)) return existingById;
            Assignee existing = data.assignees.FirstOrDefault(assignee => assignee != null &&
                (string.Equals(assignee.id, id, StringComparison.OrdinalIgnoreCase) || string.Equals(assignee.name, name, StringComparison.OrdinalIgnoreCase)));
            if (existing == null)
            {
                existing = new Assignee { id = id, name = name };
                data.assignees.Add(existing);
            }
            if (byId != null) byId[id] = existing;
            return existing;
        }

        private static void SetCategoryColor(ExportBoardData data, string category, int colorIndex)
        {
            if (string.IsNullOrWhiteSpace(category) || colorIndex <= 0) return;
            data.categoryColors ??= new List<CategoryColorEntry>();
            CategoryColorEntry entry = data.categoryColors.FirstOrDefault(item => item != null && string.Equals(item.category, category, StringComparison.OrdinalIgnoreCase));
            if (entry == null) data.categoryColors.Add(new CategoryColorEntry(category, colorIndex));
            else entry.colorIndex = colorIndex;
        }

        private static List<object> GetUrlAttachments(TaskCard card)
        {
            var result = new List<object>();
            foreach (LinkedItem link in card.linkedItems ?? new List<LinkedItem>())
                if (link != null && link.isUrl && !string.IsNullOrWhiteSpace(link.url))
                    result.Add(new Dictionary<string, object> { ["name"] = link.displayName ?? string.Empty, ["url"] = link.url });
            return result;
        }

        private static void AddLinkIfMissing(TaskCard card, LinkedItem link)
        {
            if (card == null || link == null) return;
            string target = link.isUrl ? link.url : link.guid;
            if ((card.linkedItems ?? new List<LinkedItem>()).Any(existing => existing != null &&
                    existing.isUrl == link.isUrl && existing.isNote == link.isNote &&
                    string.Equals(existing.isUrl ? existing.url : existing.guid, target, StringComparison.OrdinalIgnoreCase))) return;
            card.linkedItems ??= new List<LinkedItem>();
            card.linkedItems.Add(link);
        }

        private static IEnumerable<object> SortByPosition(IEnumerable<object> values)
        {
            return (values ?? Enumerable.Empty<object>()).Select((value, index) => new
                {
                    value,
                    index,
                    position = GetNumber(AsObject(value), "pos", "position")
                })
                .OrderBy(item => item.position.HasValue ? 0 : 1)
                .ThenBy(item => item.position ?? item.index)
                .ThenBy(item => item.index)
                .Select(item => item.value);
        }

        private static string DetectFormat(object root)
        {
            Dictionary<string, object> value = AsObject(root);
            if (value == null) return root is IList<object> ? "Generic" : null;
            if (GetValue(value, "board") is IDictionary<string, object>) return "Native";
            if (GetValue(value, "lists") is IList<object> && GetValue(value, "cards") is IList<object>) return "Trello";
            if (GetValue(value, "tasks") is IList<object> tasks)
                return LooksLikeClickUp(value, tasks) ? "ClickUp" : "Generic";
            if (GetValue(value, "issues") is IList<object>) return "Jira";
            if (GetValue(value, "cards") is IList<object> || GetValue(value, "items") is IList<object>) return "Generic";
            return null;
        }

        private static bool LooksLikeClickUp(Dictionary<string, object> root, IList<object> tasks)
        {
            if (HasValue(root, "team_id") || HasValue(root, "team")) return true;
            foreach (Dictionary<string, object> task in tasks.Select(AsObject).Where(task => task != null))
            {
                if (HasValue(task, "date_created") || HasValue(task, "date_updated") || HasValue(task, "custom_id") || HasValue(task, "orderindex") || HasValue(task, "due_date"))
                    return true;
                if (GetValue(task, "status") is IDictionary<string, object> status && status.ContainsKey("status")) return true;
                if (GetValue(task, "priority") is IDictionary<string, object> priority && priority.ContainsKey("priority")) return true;
            }
            return false;
        }

        private static object GetValue(IDictionary<string, object> source, params string[] keys)
        {
            if (source == null) return null;
            foreach (string key in keys ?? Array.Empty<string>())
                if (source.TryGetValue(key, out object value)) return value;
            return null;
        }

        private static bool HasValue(IDictionary<string, object> source, string key)
        {
            return source != null && source.ContainsKey(key) && source[key] != null;
        }

        private static Dictionary<string, object> AsObject(object value) => value as Dictionary<string, object>;
        private static IList<object> AsArray(object value) => value as IList<object> ?? new List<object>();

        private static string GetText(IDictionary<string, object> source, params string[] keys)
        {
            return AsText(GetValue(source, keys));
        }

        private static string AsText(object value)
        {
            if (value == null) return string.Empty;
            if (value is string text) return text;
            if (value is bool boolean) return boolean ? "true" : "false";
            if (value is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
            if (value is IDictionary<string, object> dictionary) return FlattenText(dictionary);
            if (value is IList<object> list) return string.Join(" ", list.Select(AsText).Where(item => !string.IsNullOrWhiteSpace(item)));
            return value.ToString();
        }

        private static string FlattenText(object value)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                if (dictionary.TryGetValue("text", out object text) && text is string stringText) return stringText;
                return string.Join(" ", dictionary.Values.Select(FlattenText).Where(item => !string.IsNullOrWhiteSpace(item)));
            }
            if (value is IList<object> list) return string.Join(" ", list.Select(FlattenText).Where(item => !string.IsNullOrWhiteSpace(item)));
            return value is string stringValue ? stringValue : string.Empty;
        }

        private static string NonEmpty(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static int ToInt(object value, int fallback = 0)
        {
            if (value == null) return fallback;
            return int.TryParse(AsText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
        }

        private static bool ToBool(object value)
        {
            if (value is bool boolean) return boolean;
            return BoardImportExport.ParseBool(AsText(value));
        }

        private static double? GetNumber(IDictionary<string, object> source, params string[] keys)
        {
            object value = GetValue(source, keys);
            if (value is double number) return number;
            return double.TryParse(AsText(value), NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : (double?)null;
        }

        private static void WriteJsonValue(StringBuilder output, object value)
        {
            if (value == null) { output.Append("null"); return; }
            if (value is string text) { WriteJsonString(output, text); return; }
            if (value is bool boolean) { output.Append(boolean ? "true" : "false"); return; }
            if (value is IDictionary<string, object> dictionary)
            {
                output.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    if (!first) output.Append(',');
                    first = false;
                    WriteJsonString(output, pair.Key);
                    output.Append(':');
                    WriteJsonValue(output, pair.Value);
                }
                output.Append('}');
                return;
            }
            if (value is IEnumerable enumerable)
            {
                output.Append('[');
                bool first = true;
                foreach (object item in enumerable)
                {
                    if (!first) output.Append(',');
                    first = false;
                    WriteJsonValue(output, item);
                }
                output.Append(']');
                return;
            }
            if (value is IFormattable formattable)
                output.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
            else WriteJsonString(output, value.ToString());
        }

        private static void WriteJsonString(StringBuilder output, string value)
        {
            output.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (character < 0x20) output.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else output.Append(character);
                        break;
                }
            }
            output.Append('"');
        }

        private sealed class JsonReader
        {
            private readonly string _text;
            private int _position;

            public JsonReader(string text)
            {
                _text = (text ?? string.Empty).TrimStart('\uFEFF');
            }

            public object Read()
            {
                SkipWhitespace();
                object value = ReadValue();
                SkipWhitespace();
                if (_position != _text.Length) throw Error("Unexpected text after the JSON value.");
                return value;
            }

            private object ReadValue()
            {
                SkipWhitespace();
                if (_position >= _text.Length) throw Error("Unexpected end of input.");
                char character = _text[_position];
                if (character == '{') return ReadObject();
                if (character == '[') return ReadArray();
                if (character == '"') return ReadString();
                if (character == 't') { ReadLiteral("true"); return true; }
                if (character == 'f') { ReadLiteral("false"); return false; }
                if (character == 'n') { ReadLiteral("null"); return null; }
                if (character == '-' || char.IsDigit(character)) return ReadNumber();
                throw Error("Unexpected character.");
            }

            private Dictionary<string, object> ReadObject()
            {
                _position++;
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                SkipWhitespace();
                if (Consume('}')) return result;
                while (true)
                {
                    SkipWhitespace();
                    if (_position >= _text.Length || _text[_position] != '"') throw Error("Expected an object key.");
                    string key = ReadString();
                    SkipWhitespace();
                    if (!Consume(':')) throw Error("Expected ':' after an object key.");
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (Consume('}')) return result;
                    if (!Consume(',')) throw Error("Expected ',' or '}' in an object.");
                }
            }

            private List<object> ReadArray()
            {
                _position++;
                var result = new List<object>();
                SkipWhitespace();
                if (Consume(']')) return result;
                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (Consume(']')) return result;
                    if (!Consume(',')) throw Error("Expected ',' or ']' in an array.");
                }
            }

            private string ReadString()
            {
                _position++;
                var result = new StringBuilder();
                while (_position < _text.Length)
                {
                    char character = _text[_position++];
                    if (character == '"') return result.ToString();
                    if (character < 0x20) throw Error("Unescaped control character in a string.");
                    if (character != '\\') { result.Append(character); continue; }
                    if (_position >= _text.Length) throw Error("Incomplete escape sequence.");
                    char escape = _text[_position++];
                    switch (escape)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ReadUnicodeEscape()); break;
                        default: throw Error("Invalid escape sequence.");
                    }
                }
                throw Error("Unterminated string.");
            }

            private char ReadUnicodeEscape()
            {
                if (_position + 4 > _text.Length) throw Error("Incomplete unicode escape.");
                string digits = _text.Substring(_position, 4);
                if (!ushort.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                    throw Error("Invalid unicode escape.");
                _position += 4;
                return (char)code;
            }

            private double ReadNumber()
            {
                int start = _position;
                if (Consume('-') && _position >= _text.Length) throw Error("Invalid number.");
                if (Consume('0'))
                {
                    if (_position < _text.Length && char.IsDigit(_text[_position])) throw Error("Leading zero in number.");
                }
                else
                {
                    if (_position >= _text.Length || !char.IsDigit(_text[_position])) throw Error("Invalid number.");
                    while (_position < _text.Length && char.IsDigit(_text[_position])) _position++;
                }
                if (Consume('.'))
                {
                    if (_position >= _text.Length || !char.IsDigit(_text[_position])) throw Error("Invalid fraction.");
                    while (_position < _text.Length && char.IsDigit(_text[_position])) _position++;
                }
                if (Consume('e') || Consume('E'))
                {
                    if (!Consume('+')) Consume('-');
                    if (_position >= _text.Length || !char.IsDigit(_text[_position])) throw Error("Invalid exponent.");
                    while (_position < _text.Length && char.IsDigit(_text[_position])) _position++;
                }
                if (!double.TryParse(_text.Substring(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw Error("Invalid number.");
                return value;
            }

            private void ReadLiteral(string literal)
            {
                if (_position + literal.Length > _text.Length || string.Compare(_text, _position, literal, 0, literal.Length, StringComparison.Ordinal) != 0)
                    throw Error("Invalid JSON value.");
                _position += literal.Length;
            }

            private bool Consume(char character)
            {
                if (_position >= _text.Length || _text[_position] != character) return false;
                _position++;
                return true;
            }

            private void SkipWhitespace()
            {
                while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++;
            }

            private FormatException Error(string message) => new FormatException(message + " (at character " + _position + ")");
        }
    }
}