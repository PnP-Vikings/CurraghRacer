using System;
using System.Collections.Generic;
using System.Linq;

namespace AwesomeTaskManager.Data
{
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
        public string imagePath; // relative or absolute path to attached image/gif

        public TaskCard() { id = Guid.NewGuid().ToString(); createdDate = Now(); category = ""; }
        public TaskCard(string title) : this() { this.title = title; description = ""; }
        static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");
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
        public List<QuickNote> notes  = new List<QuickNote>();
        public List<NoteFolder> noteFolders = new List<NoteFolder>();
        public List<string> categories = new List<string> { "Audio", "Art", "Code", "Design", "UI", "Bug", "Feature" };
        public List<CategoryColorEntry> categoryColors = new List<CategoryColorEntry>();
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

        public void Normalize()
        {
            categories ??= new List<string>();
            categoryColors ??= new List<CategoryColorEntry>();
            boards ??= new List<TaskBoard>();
            notes ??= new List<QuickNote>();
            noteFolders ??= new List<NoteFolder>();

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
