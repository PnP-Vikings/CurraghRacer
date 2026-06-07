using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AwesomeTaskManager.Data
{
    //Data Structure for Task Board
    [Serializable]
    public class SceneObjectReference
    {
        public string scenePath;
        public string globalObjectId;
        public string name;

        public SceneObjectReference() { }
        public SceneObjectReference(string scenePath, string globalObjectId, string name)
        {
            this.scenePath = scenePath;
            this.globalObjectId = globalObjectId;
            this.name = name;
        }
    }

    [Serializable]
    public class LinkedItem
    {
        public bool isSceneObject;
        public bool isNote;
        public bool isUrl;
        public string guid; // for assets or note ID
        public SceneObjectReference sceneObject; // for scene objects
        public string url; // for URLs
        public string displayName; // for URLs or Notes if we want to cache it

        public LinkedItem() { }
        public LinkedItem(string guid) { this.guid = guid; isSceneObject = false; isNote = false; isUrl = false; }
        public LinkedItem(SceneObjectReference sref) { this.sceneObject = sref; isSceneObject = true; isNote = false; isUrl = false; }

        public static LinkedItem CreateNote(string noteId)
        {
            return new LinkedItem { guid = noteId, isNote = true, isSceneObject = false, isUrl = false };
        }

        public static LinkedItem CreateUrl(string url, string name = "")
        {
            return new LinkedItem { url = url, displayName = name, isUrl = true, isSceneObject = false, isNote = false };
        }
    }

    [Serializable]
    public class TaskCard
    {
        public string id;
        public string title;
        public string description;
        public string category;
        public int colorLabel;
        public int priority;
        public string createdDate;
        public string dueDate;
        public bool archived;
        public List<string> checklistItems  = new List<string>();
        public List<bool>   checklistStates = new List<bool>();
        public List<string> checklistLinkedCardIds = new List<string>();
        public List<string> linkedAssetGuids = new List<string>();
        public List<SceneObjectReference> linkedSceneObjects = new List<SceneObjectReference>();
        public List<LinkedItem> linkedItems = new List<LinkedItem>();
        public string imagePath; // relative or absolute path to attached image/gif
        public bool showChecklist = true; // per-card toggle for showing checklist on board
        public bool completed; // manually mark card as completed (overrides overdue styling)
        public List<string> assigneeIds = new List<string>();
        public bool isArchived; // for explicit archival logic

        public TaskCard() { id = Guid.NewGuid().ToString(); createdDate = Now(); category = ""; }
        public TaskCard(string title) : this() { this.title = title; description = ""; }
        static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        public TaskCard Clone(bool resetId = true)
        {
            string json = JsonUtility.ToJson(this);
            TaskCard clone = JsonUtility.FromJson<TaskCard>(json);
            if (resetId)
            {
                clone.id = Guid.NewGuid().ToString();
                clone.createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }
            return clone;
        }
    }

    [Serializable]
    public class TaskColumn
    {
        public string id;
        public string title;
        public List<TaskCard> cards = new List<TaskCard>();
        public TaskColumn() { id = Guid.NewGuid().ToString(); }
        public TaskColumn(string title) : this() { this.title = title; }
    }

    [Serializable]
    public class TaskBoard
    {
        public string id;
        public string name;
        public List<TaskColumn> columns = new List<TaskColumn>();
        public string createdDate;

        public TaskBoard() { id = Guid.NewGuid().ToString(); createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); }
        public TaskBoard(string name) : this()
        {
            this.name = name;
            columns.Add(new TaskColumn("📋 To Do"));
            columns.Add(new TaskColumn("🔨 In Progress"));
            columns.Add(new TaskColumn("✅ Done"));
        }

        public TaskBoard Clone(bool resetIds = true, bool includeCards = true)
        {
            string json = JsonUtility.ToJson(this);
            TaskBoard clone = JsonUtility.FromJson<TaskBoard>(json);

            if (!includeCards)
            {
                foreach (var col in clone.columns)
                {
                    col.cards.Clear();
                }
            }

            if (resetIds)
            {
                clone.id = Guid.NewGuid().ToString();
                clone.createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                foreach (var col in clone.columns)
                {
                    col.id = Guid.NewGuid().ToString();
                    foreach (var card in col.cards)
                    {
                        card.id = Guid.NewGuid().ToString();
                        card.createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    }
                }
            }
            return clone;
        }
    }

    [Serializable]
    public class Assignee
    {
        public string id;
        public string name;
        public string profileImageGuid;
        public int colorIndex;
        public int borderColorIndex;

        public Assignee()
        {
            id = Guid.NewGuid().ToString();
            name = "User";
            colorIndex = 1; // Default to Green
            borderColorIndex = 0; // Default to None/Grey
        }
    }

    // Maps a category name to a default color index
    [Serializable]
    public class CategoryColorEntry
    {
        public string category;
        public int colorIndex; // index into TBStyles.LabelColors

        public CategoryColorEntry() { }
        public CategoryColorEntry(string cat, int color) { category = cat; colorIndex = color; }
    }

    // Note folder
    [Serializable]
    public class NoteFolder
    {
        public string id;
        public string name;
        public bool expanded = true;

        public NoteFolder() { id = Guid.NewGuid().ToString(); }
        public NoteFolder(string name) : this() { this.name = name; }
    }

    [Serializable]
    public class QuickNote
    {
        public string id;
        public string title;
        public string content;
        public string createdDate;
        public string modifiedDate;
        public int colorIndex;
        public bool pinned;
        public string folderId; // "" or null = unfiled (root)
        public List<string> tags = new List<string>();
        public string imagePath; // DEPRECATED — kept for backward compat migration
        public List<string> imagePaths = new List<string>(); // multiple attached images

        public QuickNote()
        {
            id = Guid.NewGuid().ToString();
            createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            modifiedDate = createdDate;
            title = "New Note";
            content = "";
            folderId = "";
        }

        public int WordCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(content)) return 0;
                return content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        public int CharCount => string.IsNullOrEmpty(content) ? 0 : content.Length;
    }

    [Serializable]
    public class SaveData
    {
        public List<TaskBoard> boards = new List<TaskBoard>();
        public List<TaskBoard> templates = new List<TaskBoard>();
        public List<QuickNote> notes  = new List<QuickNote>();
        public List<NoteFolder> noteFolders = new List<NoteFolder>();
        public List<string> categories = new List<string> { "Audio", "Art", "Code", "Design", "UI", "Bug", "Feature" };
        public List<CategoryColorEntry> categoryColors = new List<CategoryColorEntry>();
        public List<Assignee> assignees = new List<Assignee>();
        public int lastBoardIndex;

        public int GetCategoryColor(string category)
        {
            if (string.IsNullOrEmpty(category)) return 0;
            foreach (var e in categoryColors)
                if (e.category == category) return e.colorIndex;
            return 0;
        }

        public void SetCategoryColor(string category, int colorIndex)
        {
            for (int i = 0; i < categoryColors.Count; i++)
            {
                if (categoryColors[i].category == category)
                {
                    categoryColors[i].colorIndex = colorIndex;
                    return;
                }
            }
            categoryColors.Add(new CategoryColorEntry(category, colorIndex));
        }

        public bool RenameCategory(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
            oldName = oldName.Trim();
            newName = newName.Trim();
            if (oldName == newName) return true;
            if (categories.Contains(newName)) return false;

            int idx = categories.IndexOf(oldName);
            if (idx < 0) return false;
            categories[idx] = newName;

            foreach (var board in boards)
            foreach (var column in board.columns)
            foreach (var card in column.cards)
            {
                if (card.category == oldName)
                    card.category = newName;
            }

            for (int i = 0; i < categoryColors.Count; i++)
            {
                if (categoryColors[i].category == oldName)
                    categoryColors[i].category = newName;
            }

            return true;
        }

        public void DeleteCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            category = category.Trim();
            categories.Remove(category);
            categoryColors.RemoveAll(x => x.category == category);

            foreach (var board in boards)
            foreach (var column in board.columns)
            foreach (var card in column.cards)
            {
                if (card.category == category)
                    card.category = string.Empty;
            }
        }

        public void SyncLinkedChecklistItems(string subtaskCardId, bool isCompleted)
        {
            if (string.IsNullOrEmpty(subtaskCardId)) return;
            
            foreach (var card in AllCards())
            {
                if (card.checklistLinkedCardIds == null) continue;
                for (int i = 0; i < card.checklistLinkedCardIds.Count; i++)
                {
                    if (card.checklistLinkedCardIds[i] == subtaskCardId)
                    {
                        if (i < card.checklistStates.Count)
                        {
                            card.checklistStates[i] = isCompleted;
                        }
                    }
                }
            }
        }

        public void CleanupReferencesToCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            foreach (var card in AllCards())
            {
                if (card.checklistLinkedCardIds == null) continue;
                for (int i = 0; i < card.checklistLinkedCardIds.Count; i++)
                {
                    if (card.checklistLinkedCardIds[i] == cardId)
                    {
                        card.checklistLinkedCardIds[i] = string.Empty;
                    }
                }
            }
        }

        public IEnumerable<TaskCard> AllCards()
        {
            if (boards == null) yield break;
            foreach (var board in boards)
            {
                if (board.columns == null) continue;
                foreach (var column in board.columns)
                {
                    if (column.cards == null) continue;
                    foreach (var card in column.cards)
                    {
                        yield return card;
                    }
                }
            }
        }

        public void Normalize()
        {
            categories ??= new List<string>();
            categoryColors ??= new List<CategoryColorEntry>();
            boards ??= new List<TaskBoard>();
            templates ??= new List<TaskBoard>();
            notes ??= new List<QuickNote>();
            noteFolders ??= new List<NoteFolder>();
            assignees ??= new List<Assignee>();

            if (templates.Count == 0) AddDefaultTemplates();

            categories = categories
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            foreach (var board in boards)
            {
                board.columns ??= new List<TaskColumn>();

                foreach (var column in board.columns)
                {
                    column.cards ??= new List<TaskCard>();
                    column.cards.RemoveAll(IsStrayBlankCard);
                    foreach (var card in column.cards)
                    {
                        card.checklistItems ??= new List<string>();
                        card.checklistStates ??= new List<bool>();
                        card.checklistLinkedCardIds ??= new List<string>();

                        while (card.checklistStates.Count < card.checklistItems.Count) card.checklistStates.Add(false);
                        while (card.checklistLinkedCardIds.Count < card.checklistItems.Count) card.checklistLinkedCardIds.Add(string.Empty);

                        if (card.checklistStates.Count > card.checklistItems.Count) card.checklistStates.RemoveRange(card.checklistItems.Count, card.checklistStates.Count - card.checklistItems.Count);
                        if (card.checklistLinkedCardIds.Count > card.checklistItems.Count) card.checklistLinkedCardIds.RemoveRange(card.checklistItems.Count, card.checklistLinkedCardIds.Count - card.checklistItems.Count);

                        card.linkedAssetGuids ??= new List<string>();
                        card.linkedSceneObjects ??= new List<SceneObjectReference>();
                        card.linkedItems ??= new List<LinkedItem>();
                        card.assigneeIds ??= new List<string>();

                        // Migrate old separate lists to unified linkedItems
                        if (card.linkedAssetGuids.Count > 0)
                        {
                            foreach (var guid in card.linkedAssetGuids)
                            {
                                if (!card.linkedItems.Any(li => !li.isSceneObject && li.guid == guid))
                                    card.linkedItems.Add(new LinkedItem(guid));
                            }
                            card.linkedAssetGuids.Clear();
                        }

                        if (card.linkedSceneObjects.Count > 0)
                        {
                            foreach (var sref in card.linkedSceneObjects)
                            {
                                if (!card.linkedItems.Any(li => li.isSceneObject && li.sceneObject != null && li.sceneObject.globalObjectId == sref.globalObjectId))
                                    card.linkedItems.Add(new LinkedItem(sref));
                            }
                            card.linkedSceneObjects.Clear();
                        }
                    }
                }
            }

            noteFolders = noteFolders
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.name))
                .GroupBy(x => x.id)
                .Select(x => x.First())
                .ToList();

            notes = notes.Where(x => x != null).ToList();
            foreach (var note in notes)
            {
                note.folderId ??= string.Empty;
                note.tags ??= new List<string>();
                note.imagePaths ??= new List<string>();
                note.title ??= "New Note";
                note.content ??= string.Empty;

                // Migrate old single imagePath to imagePaths list
                if (!string.IsNullOrEmpty(note.imagePath))
                {
                    if (!note.imagePaths.Contains(note.imagePath))
                        note.imagePaths.Insert(0, note.imagePath);
                    note.imagePath = null;
                }
            }

            categoryColors = categoryColors
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.category) && categories.Contains(x.category))
                .GroupBy(x => x.category)
                .Select(x => x.Last())
                .ToList();
        }

        private void AddDefaultTemplates()
        {
            // Software Development
            var agile = new TaskBoard("Software Development (Agile)");
            agile.columns.Clear();
            agile.columns.Add(new TaskColumn("📋 Backlog"));
            agile.columns.Add(new TaskColumn("⏳ To Do"));
            agile.columns.Add(new TaskColumn("🔨 In Progress"));
            agile.columns.Add(new TaskColumn("🔍 QA / Review"));
            agile.columns.Add(new TaskColumn("✅ Done"));
            templates.Add(agile);

            // Game Design
            var gameDesign = new TaskBoard("Game Design");
            gameDesign.columns.Clear();
            gameDesign.columns.Add(new TaskColumn("💡 Concepts"));
            gameDesign.columns.Add(new TaskColumn("⚙️ Mechanics"));
            gameDesign.columns.Add(new TaskColumn("🗺️ Level Design"));
            gameDesign.columns.Add(new TaskColumn("🎨 Art / Assets"));
            gameDesign.columns.Add(new TaskColumn("✨ Polish"));
            gameDesign.columns.Add(new TaskColumn("✅ Completed"));
            templates.Add(gameDesign);

            // Bug Tracker
            var bugs = new TaskBoard("Bug Tracker");
            bugs.columns.Clear();
            bugs.columns.Add(new TaskColumn("🔴 Critical"));
            bugs.columns.Add(new TaskColumn("🟠 Major"));
            bugs.columns.Add(new TaskColumn("🟡 Minor"));
            bugs.columns.Add(new TaskColumn("🟢 Fixed"));
            templates.Add(bugs);

            // Simple To-Do
            var todo = new TaskBoard("Simple To-Do");
            todo.columns.Clear();
            todo.columns.Add(new TaskColumn("📝 To Do"));
            todo.columns.Add(new TaskColumn("✅ Done"));
            templates.Add(todo);
        }

        private static bool IsStrayBlankCard(TaskCard card)
        {
            if (card == null) return true;

            bool emptyTitle = string.IsNullOrWhiteSpace(card.title);
            bool emptyDescription = string.IsNullOrWhiteSpace(card.description);
            bool emptyCategory = string.IsNullOrWhiteSpace(card.category);
            bool emptyDueDate = string.IsNullOrWhiteSpace(card.dueDate);
            bool noChecklist = card.checklistItems == null || card.checklistItems.Count == 0;
            bool defaultVisuals = card.colorLabel == 0 && card.priority == 0 && !card.archived;

            return emptyTitle && emptyDescription && emptyCategory && emptyDueDate && noChecklist && defaultVisuals;
        }
    }
}
