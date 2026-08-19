using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeTaskManager.Editor;
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

    public enum ChecklistTickStyle
    {
        Vector = 0,       // Crisp Modern Vector Line Check
        Classic = 1,      // Classic Checkmark (✓)
        Heavy = 2,        // Bold Heavy Checkmark (✔)
        Square = 3,       // Minimal Inset Square (■)
        Dot = 4,          // Minimal Dot / Circle (●)
        Cross = 5,        // Clean Cross Mark (✕)
        UnityNative = 6,  // Unity Editor Native Icon
        Custom = 7        // Custom Emoji / Text Symbol
    }

    [Serializable]
    public class ThemeData
    {
        public string name = "Default";
        public List<Color> labelColors = new List<Color>();
        public List<string> priorityIcons = new List<string>();

        // Tab & section icons
        public string boardTabIcon = "📋";
        public string notesTabIcon = "📝";
        public string styleTabIcon = "🎨";
        public string boardHeaderIcon = "🎯";
        public string notesHeaderIcon = "📝";
        public string categoryIcon = "🏷";
        public string assigneeIcon = "👥";
        public string priorityFilterIcon = "🚩";
        public string parentLinkIcon = "🌳";
        public string childLinkIcon = "🌿";
        public string pinnedNoteIcon = "📌";

        // Status & action icons
        public string completedIcon = "✅";
        public string overdueIcon = "🔴";
        public string dueTodayIcon = "🟠";
        public string dueSoonIcon = "🟡";
        public string dueDateIcon = "📅";
        public string archiveIcon = "📦";
        public string unarchiveIcon = "🗃️";

        // Card details, actions & component icons
        public string cardDetailIcon = "📝";
        public string newCardIcon = "✨";
        public string checklistIcon = "☑";
        public string attachmentIcon = "📎";
        public string urlIcon = "🔗";
        public string deleteIcon = "🗑";
        public string saveIcon = "💾";
        public string cancelIcon = "✕";
        public string moveUpIcon = "▲";
        public string moveDownIcon = "▼";

        // Colors
        public Color pro_BoardHeader = Color.white;
        public Color personal_BoardHeader = Color.black;
        public Color pro_ColumnHeader = new Color(0.90f, 0.90f, 0.90f);
        public Color personal_ColumnHeader = Color.black;
        public Color pro_CardTitle = Color.white;
        public Color personal_CardTitle = Color.black;
        public Color pro_CardText = new Color(0.85f, 0.85f, 0.85f);
        public Color personal_CardText = new Color(0.20f, 0.20f, 0.20f);
        public Color pro_SectionLabel = Color.white;
        public Color personal_SectionLabel = Color.black;

        public Color pro_ColumnBg = new Color(0.22f, 0.22f, 0.22f);
        public Color pro_ColumnBgAlt = new Color(0.25f, 0.25f, 0.25f);
        public Color personal_ColumnBg = new Color(0.88f, 0.90f, 0.92f);
        public Color personal_ColumnBgAlt = new Color(0.92f, 0.93f, 0.95f);

        public Color pro_CardBg = new Color(0.24f, 0.24f, 0.24f);
        public Color personal_CardBg = new Color(0.96f, 0.96f, 0.96f);

        public Color pro_CardHighlighted = new Color(0.15f, 0.32f, 0.55f);
        public Color personal_CardHighlighted = new Color(0.55f, 0.72f, 0.95f);

        public Color pro_BoardBg = new Color(0.18f, 0.18f, 0.18f);
        public Color personal_BoardBg = new Color(0.80f, 0.82f, 0.84f);

        public Color pro_TopBarBg = new Color(0.15f, 0.15f, 0.15f);
        public Color personal_TopBarBg = new Color(0.85f, 0.85f, 0.85f);

        // Status Bar / Bottom Footer styling
        public Color pro_StatusBarBg = new Color(0.15f, 0.15f, 0.15f);
        public Color personal_StatusBarBg = new Color(0.85f, 0.85f, 0.85f);
        public Color pro_StatusBarText = new Color(0.75f, 0.75f, 0.78f);
        public Color personal_StatusBarText = new Color(0.35f, 0.35f, 0.38f);

        // Notes view & popout backgrounds
        public Color pro_NoteSidebarBg = new Color(0.20f, 0.20f, 0.20f);
        public Color personal_NoteSidebarBg = new Color(0.88f, 0.90f, 0.92f);
        public Color pro_NoteEditorBg = new Color(0.18f, 0.18f, 0.18f);
        public Color personal_NoteEditorBg = new Color(0.84f, 0.86f, 0.88f);
        public Color pro_NotePopoutBg = new Color(0.18f, 0.18f, 0.18f);
        public Color personal_NotePopoutBg = new Color(0.85f, 0.85f, 0.85f);
        public Color pro_NoteInputBg = new Color(0.14f, 0.14f, 0.14f);
        public Color personal_NoteInputBg = new Color(0.96f, 0.96f, 0.96f);
        public Color pro_NoteInputText = Color.white;
        public Color personal_NoteInputText = Color.black;
        public Color pro_NoteTitle = Color.white;
        public Color personal_NoteTitle = Color.black;

        // Card Details window background
        public Color pro_CardDetailBg = new Color(0.18f, 0.18f, 0.18f);
        public Color personal_CardDetailBg = new Color(0.85f, 0.85f, 0.85f);

        // Button styling
        public Color pro_ButtonBg = new Color(0.26f, 0.26f, 0.26f);
        public Color personal_ButtonBg = new Color(0.92f, 0.92f, 0.92f);
        public Color pro_ButtonText = Color.white;
        public Color personal_ButtonText = Color.black;
        public Color pro_ButtonHoverBg = new Color(0.35f, 0.35f, 0.35f);
        public Color personal_ButtonHoverBg = new Color(0.98f, 0.98f, 0.98f);
        public Color pro_ButtonHoverText = Color.white;
        public Color personal_ButtonHoverText = Color.black;

        // Dropdown styling
        public Color pro_DropdownBg = new Color(0.22f, 0.22f, 0.22f);
        public Color personal_DropdownBg = new Color(0.94f, 0.94f, 0.94f);
        public Color pro_DropdownText = Color.white;
        public Color personal_DropdownText = Color.black;
        public Color pro_DropdownHoverBg = new Color(0.30f, 0.30f, 0.30f);
        public Color personal_DropdownHoverBg = new Color(0.98f, 0.98f, 0.98f);
        public Color pro_DropdownHoverText = Color.white;
        public Color personal_DropdownHoverText = Color.black;

        // Dropdown menu options popup styling
        public Color pro_DropdownMenuBg = new Color(0.16f, 0.16f, 0.16f);
        public Color personal_DropdownMenuBg = new Color(0.93f, 0.93f, 0.93f);
        public Color pro_DropdownMenuText = Color.white;
        public Color personal_DropdownMenuText = Color.black;
        public Color pro_DropdownMenuHoverBg = new Color(0.24f, 0.24f, 0.24f);
        public Color personal_DropdownMenuHoverBg = new Color(0.86f, 0.86f, 0.86f);
        public Color pro_DropdownMenuHoverText = Color.white;
        public Color personal_DropdownMenuHoverText = Color.black;

        // Popup / Dialog window background
        public Color pro_PopupBg = new Color(0.16f, 0.16f, 0.16f);
        public Color personal_PopupBg = new Color(0.92f, 0.92f, 0.92f);

        // Delete button styling
        public Color pro_DeleteBtnBg = new Color(0.48f, 0.16f, 0.16f);
        public Color personal_DeleteBtnBg = new Color(0.88f, 0.33f, 0.33f);
        public Color pro_DeleteBtnText = Color.white;
        public Color personal_DeleteBtnText = Color.white;
        public Color pro_DeleteBtnHoverBg = new Color(0.60f, 0.20f, 0.20f);
        public Color personal_DeleteBtnHoverBg = new Color(0.94f, 0.40f, 0.40f);

        // Header / Tab Buttons (Board, Notes, Style)
        public Color pro_HeaderTabActiveBg = new Color(0.2f, 0.5f, 0.85f);
        public Color personal_HeaderTabActiveBg = new Color(0.25f, 0.55f, 0.90f);
        public Color pro_HeaderTabActiveText = Color.white;
        public Color personal_HeaderTabActiveText = Color.white;
        public Color pro_HeaderTabInactiveBg = new Color(0.22f, 0.22f, 0.22f);
        public Color personal_HeaderTabInactiveBg = new Color(0.88f, 0.88f, 0.88f);
        public Color pro_HeaderTabInactiveText = new Color(0.85f, 0.85f, 0.85f);
        public Color personal_HeaderTabInactiveText = new Color(0.2f, 0.2f, 0.2f);
        public Color pro_HeaderTabHoverBg = new Color(0.32f, 0.32f, 0.32f);
        public Color personal_HeaderTabHoverBg = new Color(0.95f, 0.95f, 0.95f);

        // Add Card Button styling
        public Color pro_AddCardBg = new Color(0.20f, 0.38f, 0.28f);
        public Color personal_AddCardBg = new Color(0.82f, 0.92f, 0.85f);
        public Color pro_AddCardText = new Color(0.85f, 1f, 0.9f);
        public Color personal_AddCardText = new Color(0.1f, 0.35f, 0.15f);
        public Color pro_AddCardHoverBg = new Color(0.25f, 0.48f, 0.35f);
        public Color personal_AddCardHoverBg = new Color(0.88f, 0.97f, 0.90f);

        // Note item cards in list
        public Color pro_NoteCardBg = new Color(0.22f, 0.22f, 0.22f);
        public Color personal_NoteCardBg = new Color(0.92f, 0.94f, 0.96f);
        public Color pro_NoteCardSelectedBg = new Color(0.15f, 0.32f, 0.55f);
        public Color personal_NoteCardSelectedBg = new Color(0.55f, 0.72f, 0.95f);
        public Color pro_NoteCardHoverBg = new Color(0.26f, 0.26f, 0.26f);
        public Color personal_NoteCardHoverBg = new Color(0.96f, 0.97f, 0.98f);

        // Note Action Buttons (Paste Image, Browse Image)
        public Color pro_NoteActionBg = new Color(0.20f, 0.40f, 0.60f);
        public Color personal_NoteActionBg = new Color(0.80f, 0.88f, 0.96f);
        public Color pro_NoteActionText = Color.white;
        public Color personal_NoteActionText = new Color(0.12f, 0.22f, 0.35f);
        public Color pro_NoteActionHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public Color personal_NoteActionHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public Color pro_NoteActionHoverText = Color.white;
        public Color personal_NoteActionHoverText = new Color(0.12f, 0.22f, 0.35f);

        // + Note Button styling
        public Color pro_AddNoteBg = new Color(0.20f, 0.40f, 0.60f);
        public Color personal_AddNoteBg = new Color(0.80f, 0.88f, 0.96f);
        public Color pro_AddNoteText = Color.white;
        public Color personal_AddNoteText = new Color(0.12f, 0.22f, 0.35f);
        public Color pro_AddNoteHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public Color personal_AddNoteHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public Color pro_AddNoteHoverText = Color.white;
        public Color personal_AddNoteHoverText = new Color(0.12f, 0.22f, 0.35f);

        // Import Note Button styling
        public Color pro_ImportNoteBg = new Color(0.20f, 0.40f, 0.60f);
        public Color personal_ImportNoteBg = new Color(0.80f, 0.88f, 0.96f);
        public Color pro_ImportNoteText = Color.white;
        public Color personal_ImportNoteText = new Color(0.12f, 0.22f, 0.35f);
        public Color pro_ImportNoteHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public Color personal_ImportNoteHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public Color pro_ImportNoteHoverText = Color.white;
        public Color personal_ImportNoteHoverText = new Color(0.12f, 0.22f, 0.35f);

        // Note Folders Text styling
        public Color pro_NoteFolderText = new Color(0.85f, 0.85f, 0.85f);
        public Color personal_NoteFolderText = new Color(0.20f, 0.20f, 0.20f);

        // Card Details & Tasks on Board screen
        public Color pro_CardDetailsText = new Color(0.75f, 0.75f, 0.78f);
        public Color personal_CardDetailsText = new Color(0.35f, 0.35f, 0.38f);
        public Color pro_CardTasksText = new Color(0.70f, 0.70f, 0.70f);
        public Color personal_CardTasksText = new Color(0.30f, 0.30f, 0.30f);

        // Card Category Tag on Board screen
        public Color pro_CardCategoryTag = new Color(0.75f, 0.75f, 0.78f);
        public Color personal_CardCategoryTag = new Color(0.35f, 0.35f, 0.38f);

        // Assignee Picture / Avatar Background styling
        public Color pro_AssigneeAvatarBg = new Color(0.20f, 0.20f, 0.22f, 1.0f);
        public Color personal_AssigneeAvatarBg = new Color(0.92f, 0.92f, 0.94f, 1.0f);

        // Checklist Tick Box styling
        public Color pro_ChecklistTickBg = new Color(0.18f, 0.18f, 0.20f);
        public Color personal_ChecklistTickBg = new Color(0.92f, 0.94f, 0.96f);
        public Color pro_ChecklistTickCheckedBg = Color.clear;
        public Color personal_ChecklistTickCheckedBg = Color.clear;
        public Color pro_ChecklistTickBorder = new Color(0.40f, 0.40f, 0.45f);
        public Color personal_ChecklistTickBorder = new Color(0.65f, 0.70f, 0.75f);
        public Color pro_ChecklistTickColor = Color.white;
        public Color personal_ChecklistTickColor = new Color(0.15f, 0.15f, 0.15f);
        public ChecklistTickStyle checklistTickStyle = ChecklistTickStyle.Vector;
        public string customChecklistTickChar = "";

        // Card Definite States (Overdue, Due Today, Due Soon, Completed)
        public Color pro_StatusOverdue = new Color(1f, 0.35f, 0.3f);
        public Color personal_StatusOverdue = new Color(0.85f, 0.2f, 0.15f);

        public Color pro_StatusDueToday = new Color(1f, 0.65f, 0.15f);
        public Color personal_StatusDueToday = new Color(0.9f, 0.5f, 0.05f);

        public Color pro_StatusDueSoon = new Color(0.95f, 0.85f, 0.2f);
        public Color personal_StatusDueSoon = new Color(0.8f, 0.7f, 0.1f);

        public Color pro_StatusCompleted = new Color(0.4f, 0.88f, 0.45f);
        public Color personal_StatusCompleted = new Color(0.15f, 0.65f, 0.25f);

        // Number of tasks completed
        public Color pro_TasksCompletedCount = new Color(0.4f, 0.88f, 0.45f);
        public Color personal_TasksCompletedCount = new Color(0.15f, 0.65f, 0.25f);

        // Tooltip & Truncated Text Hover Popup styling
        public Color pro_TooltipBg = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        public Color personal_TooltipBg = new Color(0.96f, 0.96f, 0.98f, 0.96f);
        public Color pro_TooltipText = Color.white;
        public Color personal_TooltipText = new Color(0.10f, 0.10f, 0.12f);
        public Color pro_TooltipBorder = new Color(0.32f, 0.32f, 0.38f, 0.8f);
        public Color personal_TooltipBorder = new Color(0.72f, 0.72f, 0.78f, 0.8f);

        public Color tabActive = new Color(0.2f, 0.5f, 0.85f);
        public Color noteSelectedAccent = new Color(0.2f, 0.6f, 1f);
        public Color linkColor = new Color(0.2f, 0.55f, 0.95f);

        public static readonly Color[] DefaultLabelColors = new Color[]
        {
            new Color(0.55f, 0.55f, 0.55f),  // 0 Grey (none)
            new Color(0.30f, 0.69f, 0.31f),  // 1 Green
            new Color(0.13f, 0.59f, 0.95f),  // 2 Blue
            new Color(1.00f, 0.92f, 0.15f),  // 3 Yellow
            new Color(1.00f, 0.60f, 0.00f),  // 4 Orange
            new Color(0.96f, 0.26f, 0.21f),  // 5 Red
            new Color(0.61f, 0.15f, 0.69f),  // 6 Purple
            new Color(0.00f, 0.59f, 0.53f),  // 7 Teal
            new Color(0.91f, 0.12f, 0.39f),  // 8 Pink
            new Color(0.80f, 0.86f, 0.22f),  // 9 Lime
            new Color(0.25f, 0.32f, 0.71f),  // 10 Indigo
            new Color(0.00f, 0.74f, 0.83f),  // 11 Cyan
            new Color(1.00f, 0.76f, 0.03f),  // 12 Amber
            new Color(1.00f, 0.34f, 0.13f),  // 13 Deep Orange
            new Color(0.40f, 0.23f, 0.72f),  // 14 Deep Purple
            new Color(0.38f, 0.49f, 0.55f),  // 15 Blue Grey
            new Color(0.47f, 0.33f, 0.28f),  // 16 Brown
        };

        public static readonly string[] DefaultPriorityIcons = { "", "🔵", "🟡", "🟠", "🔴" };

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Theme";

            labelColors ??= new List<Color>();
            if (labelColors.Count < DefaultLabelColors.Length)
            {
                for (int i = labelColors.Count; i < DefaultLabelColors.Length; i++)
                    labelColors.Add(DefaultLabelColors[i]);
            }

            priorityIcons ??= new List<string>();
            if (priorityIcons.Count < DefaultPriorityIcons.Length)
            {
                for (int i = priorityIcons.Count; i < DefaultPriorityIcons.Length; i++)
                    priorityIcons.Add(DefaultPriorityIcons[i]);
            }

            if (string.IsNullOrEmpty(boardTabIcon)) boardTabIcon = "📋";
            if (string.IsNullOrEmpty(notesTabIcon)) notesTabIcon = "📝";
            if (string.IsNullOrEmpty(styleTabIcon)) styleTabIcon = "🎨";
            if (string.IsNullOrEmpty(boardHeaderIcon)) boardHeaderIcon = "🎯";
            if (string.IsNullOrEmpty(notesHeaderIcon)) notesHeaderIcon = "📝";
            if (string.IsNullOrEmpty(categoryIcon)) categoryIcon = "🏷";
            if (string.IsNullOrEmpty(assigneeIcon)) assigneeIcon = "👥";
            if (string.IsNullOrEmpty(priorityFilterIcon)) priorityFilterIcon = "🚩";
            if (string.IsNullOrEmpty(parentLinkIcon)) parentLinkIcon = "🌳";
            if (string.IsNullOrEmpty(childLinkIcon)) childLinkIcon = "🌿";
            if (string.IsNullOrEmpty(pinnedNoteIcon)) pinnedNoteIcon = "📌";
            if (string.IsNullOrEmpty(completedIcon)) completedIcon = "✅";
            if (string.IsNullOrEmpty(overdueIcon)) overdueIcon = "🔴";
            if (string.IsNullOrEmpty(dueTodayIcon)) dueTodayIcon = "🟠";
            if (string.IsNullOrEmpty(dueSoonIcon)) dueSoonIcon = "🟡";
            if (string.IsNullOrEmpty(dueDateIcon)) dueDateIcon = "📅";
            if (string.IsNullOrEmpty(archiveIcon)) archiveIcon = "📦";
            if (string.IsNullOrEmpty(unarchiveIcon)) unarchiveIcon = "🗃️";

            if (string.IsNullOrEmpty(cardDetailIcon)) cardDetailIcon = "📝";
            if (string.IsNullOrEmpty(newCardIcon)) newCardIcon = "✨";
            if (string.IsNullOrEmpty(checklistIcon)) checklistIcon = "☑";
            if (string.IsNullOrEmpty(attachmentIcon)) attachmentIcon = "📎";
            if (string.IsNullOrEmpty(urlIcon)) urlIcon = "🔗";
            if (string.IsNullOrEmpty(deleteIcon)) deleteIcon = "🗑";
            if (string.IsNullOrEmpty(saveIcon)) saveIcon = "💾";
            if (string.IsNullOrEmpty(cancelIcon)) cancelIcon = "✕";
            if (string.IsNullOrEmpty(moveUpIcon)) moveUpIcon = "▲";
            if (string.IsNullOrEmpty(moveDownIcon)) moveDownIcon = "▼";

            if (IsUninitialized(pro_BoardHeader)) pro_BoardHeader = Color.white;
            if (IsUninitialized(personal_BoardHeader)) personal_BoardHeader = Color.black;
            if (IsUninitialized(pro_ColumnHeader)) pro_ColumnHeader = new Color(0.90f, 0.90f, 0.90f);
            if (IsUninitialized(personal_ColumnHeader)) personal_ColumnHeader = Color.black;
            if (IsUninitialized(pro_CardTitle)) pro_CardTitle = Color.white;
            if (IsUninitialized(personal_CardTitle)) personal_CardTitle = Color.black;
            if (IsUninitialized(pro_CardText)) pro_CardText = new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(personal_CardText)) personal_CardText = new Color(0.20f, 0.20f, 0.20f);
            if (IsUninitialized(pro_SectionLabel)) pro_SectionLabel = Color.white;
            if (IsUninitialized(personal_SectionLabel)) personal_SectionLabel = Color.black;

            if (IsUninitialized(pro_ColumnBg)) pro_ColumnBg = new Color(0.22f, 0.22f, 0.22f);
            if (IsUninitialized(pro_ColumnBgAlt)) pro_ColumnBgAlt = new Color(0.25f, 0.25f, 0.25f);
            if (IsUninitialized(personal_ColumnBg)) personal_ColumnBg = new Color(0.88f, 0.90f, 0.92f);
            if (IsUninitialized(personal_ColumnBgAlt)) personal_ColumnBgAlt = new Color(0.92f, 0.93f, 0.95f);

            if (IsUninitialized(pro_CardBg)) pro_CardBg = new Color(0.24f, 0.24f, 0.24f);
            if (IsUninitialized(personal_CardBg)) personal_CardBg = new Color(0.96f, 0.96f, 0.96f);

            if (IsUninitialized(pro_CardHighlighted)) pro_CardHighlighted = new Color(0.15f, 0.32f, 0.55f);
            if (IsUninitialized(personal_CardHighlighted)) personal_CardHighlighted = new Color(0.55f, 0.72f, 0.95f);

            if (IsUninitialized(pro_BoardBg)) pro_BoardBg = new Color(0.18f, 0.18f, 0.18f);
            if (IsUninitialized(personal_BoardBg)) personal_BoardBg = new Color(0.80f, 0.82f, 0.84f);
            if (IsUninitialized(pro_TopBarBg)) pro_TopBarBg = new Color(0.15f, 0.15f, 0.15f);
            if (IsUninitialized(personal_TopBarBg)) personal_TopBarBg = new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(pro_StatusBarBg)) pro_StatusBarBg = !IsUninitialized(pro_TopBarBg) ? pro_TopBarBg : new Color(0.15f, 0.15f, 0.15f);
            if (IsUninitialized(personal_StatusBarBg)) personal_StatusBarBg = !IsUninitialized(personal_TopBarBg) ? personal_TopBarBg : new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(pro_StatusBarText)) pro_StatusBarText = !IsUninitialized(pro_CardDetailsText) ? pro_CardDetailsText : new Color(0.75f, 0.75f, 0.78f);
            if (IsUninitialized(personal_StatusBarText)) personal_StatusBarText = !IsUninitialized(personal_CardDetailsText) ? personal_CardDetailsText : new Color(0.35f, 0.35f, 0.38f);

            if (IsUninitialized(pro_NoteSidebarBg)) pro_NoteSidebarBg = new Color(0.20f, 0.20f, 0.20f);
            if (IsUninitialized(personal_NoteSidebarBg)) personal_NoteSidebarBg = new Color(0.88f, 0.90f, 0.92f);
            if (IsUninitialized(pro_NoteEditorBg)) pro_NoteEditorBg = new Color(0.18f, 0.18f, 0.18f);
            if (IsUninitialized(personal_NoteEditorBg)) personal_NoteEditorBg = new Color(0.84f, 0.86f, 0.88f);
            if (IsUninitialized(pro_NotePopoutBg)) pro_NotePopoutBg = new Color(0.18f, 0.18f, 0.18f);
            if (IsUninitialized(personal_NotePopoutBg)) personal_NotePopoutBg = new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(pro_NoteInputBg)) pro_NoteInputBg = new Color(0.14f, 0.14f, 0.14f);
            if (IsUninitialized(personal_NoteInputBg)) personal_NoteInputBg = new Color(0.96f, 0.96f, 0.96f);
            if (IsUninitialized(pro_NoteInputText)) pro_NoteInputText = Color.white;
            if (IsUninitialized(personal_NoteInputText)) personal_NoteInputText = Color.black;
            if (IsUninitialized(pro_NoteTitle)) pro_NoteTitle = !IsUninitialized(pro_CardTitle) ? pro_CardTitle : Color.white;
            if (IsUninitialized(personal_NoteTitle)) personal_NoteTitle = !IsUninitialized(personal_CardTitle) ? personal_CardTitle : Color.black;

            if (IsUninitialized(pro_CardDetailBg)) pro_CardDetailBg = new Color(0.18f, 0.18f, 0.18f);
            if (IsUninitialized(personal_CardDetailBg)) personal_CardDetailBg = new Color(0.85f, 0.85f, 0.85f);

            if (IsUninitialized(pro_ButtonBg)) pro_ButtonBg = new Color(0.26f, 0.26f, 0.26f);
            if (IsUninitialized(personal_ButtonBg)) personal_ButtonBg = new Color(0.92f, 0.92f, 0.92f);
            if (IsUninitialized(pro_ButtonText)) pro_ButtonText = Color.white;
            if (IsUninitialized(personal_ButtonText)) personal_ButtonText = Color.black;
            if (IsUninitialized(pro_ButtonHoverBg)) pro_ButtonHoverBg = new Color(0.35f, 0.35f, 0.35f);
            if (IsUninitialized(personal_ButtonHoverBg)) personal_ButtonHoverBg = new Color(0.98f, 0.98f, 0.98f);
            if (IsUninitialized(pro_ButtonHoverText)) pro_ButtonHoverText = pro_ButtonText;
            if (IsUninitialized(personal_ButtonHoverText)) personal_ButtonHoverText = personal_ButtonText;

            if (IsUninitialized(pro_DropdownBg)) pro_DropdownBg = new Color(0.22f, 0.22f, 0.22f);
            if (IsUninitialized(personal_DropdownBg)) personal_DropdownBg = new Color(0.94f, 0.94f, 0.94f);
            if (IsUninitialized(pro_DropdownText)) pro_DropdownText = Color.white;
            if (IsUninitialized(personal_DropdownText)) personal_DropdownText = Color.black;
            if (IsUninitialized(pro_DropdownHoverBg)) pro_DropdownHoverBg = new Color(0.30f, 0.30f, 0.30f);
            if (IsUninitialized(personal_DropdownHoverBg)) personal_DropdownHoverBg = new Color(0.98f, 0.98f, 0.98f);
            if (IsUninitialized(pro_DropdownHoverText)) pro_DropdownHoverText = pro_DropdownText;
            if (IsUninitialized(personal_DropdownHoverText)) personal_DropdownHoverText = personal_DropdownText;

            if (IsUninitialized(pro_DropdownMenuBg)) pro_DropdownMenuBg = new Color(0.16f, 0.16f, 0.16f);
            if (IsUninitialized(personal_DropdownMenuBg)) personal_DropdownMenuBg = new Color(0.93f, 0.93f, 0.93f);
            if (IsUninitialized(pro_DropdownMenuText)) pro_DropdownMenuText = Color.white;
            if (IsUninitialized(personal_DropdownMenuText)) personal_DropdownMenuText = Color.black;
            if (IsUninitialized(pro_DropdownMenuHoverBg)) pro_DropdownMenuHoverBg = new Color(0.24f, 0.24f, 0.24f);
            if (IsUninitialized(personal_DropdownMenuHoverBg)) personal_DropdownMenuHoverBg = new Color(0.86f, 0.86f, 0.86f);
            if (IsUninitialized(pro_DropdownMenuHoverText)) pro_DropdownMenuHoverText = Color.white;
            if (IsUninitialized(personal_DropdownMenuHoverText)) personal_DropdownMenuHoverText = Color.black;

            if (IsUninitialized(pro_PopupBg)) pro_PopupBg = new Color(0.16f, 0.16f, 0.16f);
            if (IsUninitialized(personal_PopupBg)) personal_PopupBg = new Color(0.92f, 0.92f, 0.92f);

            if (IsUninitialized(pro_DeleteBtnBg)) pro_DeleteBtnBg = new Color(0.48f, 0.16f, 0.16f);
            if (IsUninitialized(personal_DeleteBtnBg)) personal_DeleteBtnBg = new Color(0.88f, 0.33f, 0.33f);
            if (IsUninitialized(pro_DeleteBtnText)) pro_DeleteBtnText = Color.white;
            if (IsUninitialized(personal_DeleteBtnText)) personal_DeleteBtnText = Color.white;
            if (IsUninitialized(pro_DeleteBtnHoverBg)) pro_DeleteBtnHoverBg = new Color(0.60f, 0.20f, 0.20f);
            if (IsUninitialized(personal_DeleteBtnHoverBg)) personal_DeleteBtnHoverBg = new Color(0.94f, 0.40f, 0.40f);

            if (IsUninitialized(pro_HeaderTabActiveBg)) pro_HeaderTabActiveBg = !IsUninitialized(tabActive) ? tabActive : new Color(0.2f, 0.5f, 0.85f);
            if (IsUninitialized(personal_HeaderTabActiveBg)) personal_HeaderTabActiveBg = new Color(0.25f, 0.55f, 0.90f);
            if (IsUninitialized(pro_HeaderTabActiveText)) pro_HeaderTabActiveText = Color.white;
            if (IsUninitialized(personal_HeaderTabActiveText)) personal_HeaderTabActiveText = Color.white;
            if (IsUninitialized(pro_HeaderTabInactiveBg)) pro_HeaderTabInactiveBg = new Color(0.22f, 0.22f, 0.22f);
            if (IsUninitialized(personal_HeaderTabInactiveBg)) personal_HeaderTabInactiveBg = new Color(0.88f, 0.88f, 0.88f);
            if (IsUninitialized(pro_HeaderTabInactiveText)) pro_HeaderTabInactiveText = new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(personal_HeaderTabInactiveText)) personal_HeaderTabInactiveText = new Color(0.2f, 0.2f, 0.2f);
            if (IsUninitialized(pro_HeaderTabHoverBg)) pro_HeaderTabHoverBg = new Color(0.32f, 0.32f, 0.32f);
            if (IsUninitialized(personal_HeaderTabHoverBg)) personal_HeaderTabHoverBg = new Color(0.95f, 0.95f, 0.95f);

            if (IsUninitialized(pro_AddCardBg)) pro_AddCardBg = new Color(0.20f, 0.38f, 0.28f);
            if (IsUninitialized(personal_AddCardBg)) personal_AddCardBg = new Color(0.82f, 0.92f, 0.85f);
            if (IsUninitialized(pro_AddCardText)) pro_AddCardText = new Color(0.85f, 1f, 0.9f);
            if (IsUninitialized(personal_AddCardText)) personal_AddCardText = new Color(0.1f, 0.35f, 0.15f);
            if (IsUninitialized(pro_AddCardHoverBg)) pro_AddCardHoverBg = new Color(0.25f, 0.48f, 0.35f);
            if (IsUninitialized(personal_AddCardHoverBg)) personal_AddCardHoverBg = new Color(0.88f, 0.97f, 0.90f);

            if (IsUninitialized(pro_NoteCardBg)) pro_NoteCardBg = new Color(0.22f, 0.22f, 0.22f);
            if (IsUninitialized(personal_NoteCardBg)) personal_NoteCardBg = new Color(0.92f, 0.94f, 0.96f);
            if (IsUninitialized(pro_NoteCardSelectedBg)) pro_NoteCardSelectedBg = new Color(0.15f, 0.32f, 0.55f);
            if (IsUninitialized(personal_NoteCardSelectedBg)) personal_NoteCardSelectedBg = new Color(0.55f, 0.72f, 0.95f);
            if (IsUninitialized(pro_NoteCardHoverBg)) pro_NoteCardHoverBg = new Color(0.26f, 0.26f, 0.26f);
            if (IsUninitialized(personal_NoteCardHoverBg)) personal_NoteCardHoverBg = new Color(0.96f, 0.97f, 0.98f);

            if (IsUninitialized(pro_NoteActionBg)) pro_NoteActionBg = new Color(0.20f, 0.40f, 0.60f);
            if (IsUninitialized(personal_NoteActionBg)) personal_NoteActionBg = new Color(0.80f, 0.88f, 0.96f);
            if (IsUninitialized(pro_NoteActionText)) pro_NoteActionText = Color.white;
            if (IsUninitialized(personal_NoteActionText)) personal_NoteActionText = new Color(0.12f, 0.22f, 0.35f);
            if (IsUninitialized(pro_NoteActionHoverBg)) pro_NoteActionHoverBg = new Color(0.26f, 0.48f, 0.70f);
            if (IsUninitialized(personal_NoteActionHoverBg)) personal_NoteActionHoverBg = new Color(0.86f, 0.93f, 0.99f);
            if (IsUninitialized(pro_NoteActionHoverText)) pro_NoteActionHoverText = pro_NoteActionText;
            if (IsUninitialized(personal_NoteActionHoverText)) personal_NoteActionHoverText = personal_NoteActionText;

            if (IsUninitialized(pro_AddNoteBg)) pro_AddNoteBg = !IsUninitialized(pro_NoteActionBg) ? pro_NoteActionBg : new Color(0.20f, 0.40f, 0.60f);
            if (IsUninitialized(personal_AddNoteBg)) personal_AddNoteBg = !IsUninitialized(personal_NoteActionBg) ? personal_NoteActionBg : new Color(0.80f, 0.88f, 0.96f);
            if (IsUninitialized(pro_AddNoteText)) pro_AddNoteText = !IsUninitialized(pro_NoteActionText) ? pro_NoteActionText : Color.white;
            if (IsUninitialized(personal_AddNoteText)) personal_AddNoteText = !IsUninitialized(personal_NoteActionText) ? personal_NoteActionText : new Color(0.12f, 0.22f, 0.35f);
            if (IsUninitialized(pro_AddNoteHoverBg)) pro_AddNoteHoverBg = !IsUninitialized(pro_NoteActionHoverBg) ? pro_NoteActionHoverBg : new Color(0.26f, 0.48f, 0.70f);
            if (IsUninitialized(personal_AddNoteHoverBg)) personal_AddNoteHoverBg = !IsUninitialized(personal_NoteActionHoverBg) ? personal_NoteActionHoverBg : new Color(0.86f, 0.93f, 0.99f);
            if (IsUninitialized(pro_AddNoteHoverText)) pro_AddNoteHoverText = pro_AddNoteText;
            if (IsUninitialized(personal_AddNoteHoverText)) personal_AddNoteHoverText = personal_AddNoteText;

            if (IsUninitialized(pro_ImportNoteBg)) pro_ImportNoteBg = !IsUninitialized(pro_NoteActionBg) ? pro_NoteActionBg : new Color(0.20f, 0.40f, 0.60f);
            if (IsUninitialized(personal_ImportNoteBg)) personal_ImportNoteBg = !IsUninitialized(personal_NoteActionBg) ? personal_NoteActionBg : new Color(0.80f, 0.88f, 0.96f);
            if (IsUninitialized(pro_ImportNoteText)) pro_ImportNoteText = !IsUninitialized(pro_NoteActionText) ? pro_NoteActionText : Color.white;
            if (IsUninitialized(personal_ImportNoteText)) personal_ImportNoteText = !IsUninitialized(personal_NoteActionText) ? personal_NoteActionText : new Color(0.12f, 0.22f, 0.35f);
            if (IsUninitialized(pro_ImportNoteHoverBg)) pro_ImportNoteHoverBg = !IsUninitialized(pro_NoteActionHoverBg) ? pro_NoteActionHoverBg : new Color(0.26f, 0.48f, 0.70f);
            if (IsUninitialized(personal_ImportNoteHoverBg)) personal_ImportNoteHoverBg = !IsUninitialized(personal_NoteActionHoverBg) ? personal_NoteActionHoverBg : new Color(0.86f, 0.93f, 0.99f);
            if (IsUninitialized(pro_ImportNoteHoverText)) pro_ImportNoteHoverText = pro_ImportNoteText;
            if (IsUninitialized(personal_ImportNoteHoverText)) personal_ImportNoteHoverText = personal_ImportNoteText;

            if (IsUninitialized(pro_NoteFolderText)) pro_NoteFolderText = new Color(0.85f, 0.85f, 0.85f);
            if (IsUninitialized(personal_NoteFolderText)) personal_NoteFolderText = new Color(0.20f, 0.20f, 0.20f);

            if (IsUninitialized(pro_CardDetailsText)) pro_CardDetailsText = new Color(0.75f, 0.75f, 0.78f);
            if (IsUninitialized(personal_CardDetailsText)) personal_CardDetailsText = new Color(0.35f, 0.35f, 0.38f);

            if (IsUninitialized(pro_CardTasksText)) pro_CardTasksText = new Color(0.70f, 0.70f, 0.70f);
            if (IsUninitialized(personal_CardTasksText)) personal_CardTasksText = new Color(0.30f, 0.30f, 0.30f);

            if (IsUninitialized(pro_CardCategoryTag)) pro_CardCategoryTag = !IsUninitialized(pro_CardDetailsText) ? pro_CardDetailsText : new Color(0.75f, 0.75f, 0.78f);
            if (IsUninitialized(personal_CardCategoryTag)) personal_CardCategoryTag = !IsUninitialized(personal_CardDetailsText) ? personal_CardDetailsText : new Color(0.35f, 0.35f, 0.38f);

            if (IsUninitialized(pro_AssigneeAvatarBg)) pro_AssigneeAvatarBg = !IsUninitialized(pro_CardBg) ? pro_CardBg : new Color(0.20f, 0.20f, 0.22f, 1.0f);
            if (IsUninitialized(personal_AssigneeAvatarBg)) personal_AssigneeAvatarBg = !IsUninitialized(personal_CardBg) ? personal_CardBg : new Color(0.92f, 0.92f, 0.94f, 1.0f);

            if (IsUninitialized(pro_ChecklistTickBg)) pro_ChecklistTickBg = !IsUninitialized(pro_NoteInputBg) ? pro_NoteInputBg : new Color(0.18f, 0.18f, 0.20f);
            if (IsUninitialized(personal_ChecklistTickBg)) personal_ChecklistTickBg = !IsUninitialized(personal_NoteInputBg) ? personal_NoteInputBg : new Color(0.92f, 0.94f, 0.96f);
            if (IsUninitialized(pro_ChecklistTickBorder)) pro_ChecklistTickBorder = !IsUninitialized(pro_TooltipBorder) ? pro_TooltipBorder : new Color(0.40f, 0.40f, 0.45f);
            if (IsUninitialized(personal_ChecklistTickBorder)) personal_ChecklistTickBorder = !IsUninitialized(personal_TooltipBorder) ? personal_TooltipBorder : new Color(0.65f, 0.70f, 0.75f);
            if (IsUninitialized(pro_ChecklistTickColor)) pro_ChecklistTickColor = Color.white;
            if (IsUninitialized(personal_ChecklistTickColor)) personal_ChecklistTickColor = new Color(0.15f, 0.15f, 0.15f);
            if (customChecklistTickChar == null) customChecklistTickChar = "";

            if (IsUninitialized(pro_StatusOverdue)) pro_StatusOverdue = new Color(1f, 0.35f, 0.3f);
            if (IsUninitialized(personal_StatusOverdue)) personal_StatusOverdue = new Color(0.85f, 0.2f, 0.15f);

            if (IsUninitialized(pro_StatusDueToday)) pro_StatusDueToday = new Color(1f, 0.65f, 0.15f);
            if (IsUninitialized(personal_StatusDueToday)) personal_StatusDueToday = new Color(0.9f, 0.5f, 0.05f);

            if (IsUninitialized(pro_StatusDueSoon)) pro_StatusDueSoon = new Color(0.95f, 0.85f, 0.2f);
            if (IsUninitialized(personal_StatusDueSoon)) personal_StatusDueSoon = new Color(0.8f, 0.7f, 0.1f);

            if (IsUninitialized(pro_StatusCompleted)) pro_StatusCompleted = new Color(0.4f, 0.88f, 0.45f);
            if (IsUninitialized(personal_StatusCompleted)) personal_StatusCompleted = new Color(0.15f, 0.65f, 0.25f);

            if (IsUninitialized(pro_TasksCompletedCount)) pro_TasksCompletedCount = !IsUninitialized(pro_StatusCompleted) ? pro_StatusCompleted : new Color(0.4f, 0.88f, 0.45f);
            if (IsUninitialized(personal_TasksCompletedCount)) personal_TasksCompletedCount = !IsUninitialized(personal_StatusCompleted) ? personal_StatusCompleted : new Color(0.15f, 0.65f, 0.25f);

            if (IsUninitialized(pro_TooltipBg)) pro_TooltipBg = new Color(0.12f, 0.12f, 0.14f, 0.96f);
            if (IsUninitialized(personal_TooltipBg)) personal_TooltipBg = new Color(0.96f, 0.96f, 0.98f, 0.96f);
            if (IsUninitialized(pro_TooltipText)) pro_TooltipText = Color.white;
            if (IsUninitialized(personal_TooltipText)) personal_TooltipText = new Color(0.10f, 0.10f, 0.12f);
            if (IsUninitialized(pro_TooltipBorder)) pro_TooltipBorder = new Color(0.32f, 0.32f, 0.38f, 0.8f);
            if (IsUninitialized(personal_TooltipBorder)) personal_TooltipBorder = new Color(0.72f, 0.72f, 0.78f, 0.8f);

            if (IsUninitialized(tabActive)) tabActive = new Color(0.2f, 0.5f, 0.85f);
            if (IsUninitialized(noteSelectedAccent)) noteSelectedAccent = new Color(0.2f, 0.6f, 1f);
            if (IsUninitialized(linkColor)) linkColor = new Color(0.2f, 0.55f, 0.95f);
        }

        private static bool IsUninitialized(Color c)
        {
            return c.r == 0f && c.g == 0f && c.b == 0f && c.a == 0f;
        }

        public ThemeData Clone()
        {
            var clone = JsonUtility.FromJson<ThemeData>(JsonUtility.ToJson(this));
            clone.Normalize();
            return clone;
        }

        public static ThemeData CreateDefault()
        {
            var t = new ThemeData
            {
                name = "Default",
                labelColors = new List<Color>(DefaultLabelColors),
                priorityIcons = new List<string>(DefaultPriorityIcons),
                boardTabIcon = "📋",
                notesTabIcon = "📝",
                styleTabIcon = "🎨",
                boardHeaderIcon = "🎯",
                notesHeaderIcon = "📝",
                categoryIcon = "🏷",
                assigneeIcon = "👥",
                priorityFilterIcon = "🚩",
                parentLinkIcon = "🌳",
                childLinkIcon = "🌿",
                pinnedNoteIcon = "📌",
                completedIcon = "✅",
                overdueIcon = "🔴",
                dueTodayIcon = "🟠",
                dueSoonIcon = "🟡",
                dueDateIcon = "📅",
                archiveIcon = "📦",
                unarchiveIcon = "🗃️",
                cardDetailIcon = "📝",
                newCardIcon = "✨",
                checklistIcon = "☑",
                attachmentIcon = "📎",
                urlIcon = "🔗",
                deleteIcon = "🗑",
                saveIcon = "💾",
                cancelIcon = "✕",
                moveUpIcon = "▲",
                moveDownIcon = "▼",
                pro_BoardHeader = Color.white,
                personal_BoardHeader = Color.black,
                pro_ColumnHeader = new Color(0.90f, 0.90f, 0.90f),
                personal_ColumnHeader = Color.black,
                pro_CardTitle = Color.white,
                personal_CardTitle = Color.black,
                pro_CardText = new Color(0.85f, 0.85f, 0.85f),
                personal_CardText = new Color(0.20f, 0.20f, 0.20f),
                pro_SectionLabel = Color.white,
                personal_SectionLabel = Color.black,
                pro_ColumnBg = new Color(0.22f, 0.22f, 0.22f),
                pro_ColumnBgAlt = new Color(0.25f, 0.25f, 0.25f),
                personal_ColumnBg = new Color(0.88f, 0.90f, 0.92f),
                personal_ColumnBgAlt = new Color(0.92f, 0.93f, 0.95f),
                pro_CardBg = new Color(0.24f, 0.24f, 0.24f),
                personal_CardBg = new Color(0.96f, 0.96f, 0.96f),
                pro_CardHighlighted = new Color(0.15f, 0.32f, 0.55f),
                personal_CardHighlighted = new Color(0.55f, 0.72f, 0.95f),
                pro_BoardBg = new Color(0.18f, 0.18f, 0.18f),
                personal_BoardBg = new Color(0.80f, 0.82f, 0.84f),
                pro_TopBarBg = new Color(0.15f, 0.15f, 0.15f),
                personal_TopBarBg = new Color(0.85f, 0.85f, 0.85f),
                pro_StatusBarBg = new Color(0.15f, 0.15f, 0.15f),
                personal_StatusBarBg = new Color(0.85f, 0.85f, 0.85f),
                pro_StatusBarText = new Color(0.75f, 0.75f, 0.78f),
                personal_StatusBarText = new Color(0.35f, 0.35f, 0.38f),
                pro_NoteSidebarBg = new Color(0.20f, 0.20f, 0.20f),
                personal_NoteSidebarBg = new Color(0.88f, 0.90f, 0.92f),
                pro_NoteEditorBg = new Color(0.18f, 0.18f, 0.18f),
                personal_NoteEditorBg = new Color(0.84f, 0.86f, 0.88f),
                pro_NotePopoutBg = new Color(0.18f, 0.18f, 0.18f),
                personal_NotePopoutBg = new Color(0.85f, 0.85f, 0.85f),
                pro_NoteInputBg = new Color(0.14f, 0.14f, 0.14f),
                personal_NoteInputBg = new Color(0.96f, 0.96f, 0.96f),
                pro_NoteInputText = Color.white,
                personal_NoteInputText = Color.black,
                pro_NoteTitle = Color.white,
                personal_NoteTitle = Color.black,
                pro_CardDetailBg = new Color(0.18f, 0.18f, 0.18f),
                personal_CardDetailBg = new Color(0.85f, 0.85f, 0.85f),
                pro_ButtonBg = new Color(0.26f, 0.26f, 0.26f),
                personal_ButtonBg = new Color(0.92f, 0.92f, 0.92f),
                pro_ButtonText = Color.white,
                personal_ButtonText = Color.black,
                pro_ButtonHoverBg = new Color(0.35f, 0.35f, 0.35f),
                personal_ButtonHoverBg = new Color(0.98f, 0.98f, 0.98f),
                pro_ButtonHoverText = Color.white,
                personal_ButtonHoverText = Color.black,
                pro_DropdownBg = new Color(0.22f, 0.22f, 0.22f),
                personal_DropdownBg = new Color(0.94f, 0.94f, 0.94f),
                pro_DropdownText = Color.white,
                personal_DropdownText = Color.black,
                pro_DropdownHoverBg = new Color(0.30f, 0.30f, 0.30f),
                personal_DropdownHoverBg = new Color(0.98f, 0.98f, 0.98f),
                pro_DropdownHoverText = Color.white,
                personal_DropdownHoverText = Color.black,
                pro_HeaderTabActiveBg = new Color(0.2f, 0.5f, 0.85f),
                personal_HeaderTabActiveBg = new Color(0.25f, 0.55f, 0.90f),
                pro_HeaderTabActiveText = Color.white,
                personal_HeaderTabActiveText = Color.white,
                pro_HeaderTabInactiveBg = new Color(0.22f, 0.22f, 0.22f),
                personal_HeaderTabInactiveBg = new Color(0.88f, 0.88f, 0.88f),
                pro_HeaderTabInactiveText = new Color(0.85f, 0.85f, 0.85f),
                personal_HeaderTabInactiveText = new Color(0.2f, 0.2f, 0.2f),
                pro_HeaderTabHoverBg = new Color(0.32f, 0.32f, 0.32f),
                personal_HeaderTabHoverBg = new Color(0.95f, 0.95f, 0.95f),
                pro_AddCardBg = new Color(0.20f, 0.38f, 0.28f),
                personal_AddCardBg = new Color(0.82f, 0.92f, 0.85f),
                pro_AddCardText = new Color(0.85f, 1f, 0.9f),
                personal_AddCardText = new Color(0.1f, 0.35f, 0.15f),
                pro_AddCardHoverBg = new Color(0.25f, 0.48f, 0.35f),
                personal_AddCardHoverBg = new Color(0.88f, 0.97f, 0.90f),
                pro_NoteCardBg = new Color(0.22f, 0.22f, 0.22f),
                personal_NoteCardBg = new Color(0.92f, 0.94f, 0.96f),
                pro_NoteCardSelectedBg = new Color(0.15f, 0.32f, 0.55f),
                personal_NoteCardSelectedBg = new Color(0.55f, 0.72f, 0.95f),
                pro_NoteCardHoverBg = new Color(0.26f, 0.26f, 0.26f),
                personal_NoteCardHoverBg = new Color(0.96f, 0.97f, 0.98f),
                pro_NoteActionBg = new Color(0.20f, 0.40f, 0.60f),
                personal_NoteActionBg = new Color(0.80f, 0.88f, 0.96f),
                pro_NoteActionText = Color.white,
                personal_NoteActionText = new Color(0.12f, 0.22f, 0.35f),
                pro_NoteActionHoverBg = new Color(0.26f, 0.48f, 0.70f),
                personal_NoteActionHoverBg = new Color(0.86f, 0.93f, 0.99f),
                pro_NoteActionHoverText = Color.white,
                personal_NoteActionHoverText = new Color(0.12f, 0.22f, 0.35f),
                pro_AddNoteBg = new Color(0.20f, 0.40f, 0.60f),
                personal_AddNoteBg = new Color(0.80f, 0.88f, 0.96f),
                pro_AddNoteText = Color.white,
                personal_AddNoteText = new Color(0.12f, 0.22f, 0.35f),
                pro_AddNoteHoverBg = new Color(0.26f, 0.48f, 0.70f),
                personal_AddNoteHoverBg = new Color(0.86f, 0.93f, 0.99f),
                pro_AddNoteHoverText = Color.white,
                personal_AddNoteHoverText = new Color(0.12f, 0.22f, 0.35f),
                pro_ImportNoteBg = new Color(0.20f, 0.40f, 0.60f),
                personal_ImportNoteBg = new Color(0.80f, 0.88f, 0.96f),
                pro_ImportNoteText = Color.white,
                personal_ImportNoteText = new Color(0.12f, 0.22f, 0.35f),
                pro_ImportNoteHoverBg = new Color(0.26f, 0.48f, 0.70f),
                personal_ImportNoteHoverBg = new Color(0.86f, 0.93f, 0.99f),
                pro_ImportNoteHoverText = Color.white,
                personal_ImportNoteHoverText = new Color(0.12f, 0.22f, 0.35f),
                pro_NoteFolderText = new Color(0.85f, 0.85f, 0.85f),
                personal_NoteFolderText = new Color(0.20f, 0.20f, 0.20f),
                pro_CardDetailsText = new Color(0.75f, 0.75f, 0.78f),
                personal_CardDetailsText = new Color(0.35f, 0.35f, 0.38f),
                pro_CardTasksText = new Color(0.70f, 0.70f, 0.70f),
                personal_CardTasksText = new Color(0.30f, 0.30f, 0.30f),
                pro_CardCategoryTag = new Color(0.75f, 0.75f, 0.78f),
                personal_CardCategoryTag = new Color(0.35f, 0.35f, 0.38f),
                pro_AssigneeAvatarBg = new Color(0.20f, 0.20f, 0.22f, 1.0f),
                personal_AssigneeAvatarBg = new Color(0.92f, 0.92f, 0.94f, 1.0f),
                pro_ChecklistTickBg = new Color(0.18f, 0.18f, 0.20f),
                personal_ChecklistTickBg = new Color(0.92f, 0.94f, 0.96f),
                pro_ChecklistTickCheckedBg = Color.clear,
                personal_ChecklistTickCheckedBg = Color.clear,
                pro_ChecklistTickBorder = new Color(0.38f, 0.38f, 0.44f),
                personal_ChecklistTickBorder = new Color(0.65f, 0.70f, 0.75f),
                pro_ChecklistTickColor = Color.white,
                personal_ChecklistTickColor = new Color(0.15f, 0.15f, 0.15f),
                checklistTickStyle = ChecklistTickStyle.Vector,
                customChecklistTickChar = "",
                pro_StatusOverdue = new Color(1f, 0.35f, 0.3f),
                personal_StatusOverdue = new Color(0.85f, 0.2f, 0.15f),
                pro_StatusDueToday = new Color(1f, 0.65f, 0.15f),
                personal_StatusDueToday = new Color(0.9f, 0.5f, 0.05f),
                pro_StatusDueSoon = new Color(0.95f, 0.85f, 0.2f),
                personal_StatusDueSoon = new Color(0.8f, 0.7f, 0.1f),
                pro_StatusCompleted = new Color(0.4f, 0.88f, 0.45f),
                personal_StatusCompleted = new Color(0.15f, 0.65f, 0.25f),
                pro_TasksCompletedCount = new Color(0.4f, 0.88f, 0.45f),
                personal_TasksCompletedCount = new Color(0.15f, 0.65f, 0.25f),
                pro_TooltipBg = new Color(0.12f, 0.12f, 0.14f, 0.96f),
                personal_TooltipBg = new Color(0.96f, 0.96f, 0.98f, 0.96f),
                pro_TooltipText = Color.white,
                personal_TooltipText = new Color(0.10f, 0.10f, 0.12f),
                pro_TooltipBorder = new Color(0.32f, 0.32f, 0.38f, 0.8f),
                personal_TooltipBorder = new Color(0.72f, 0.72f, 0.78f, 0.8f),
                tabActive = new Color(0.2f, 0.5f, 0.85f),
                noteSelectedAccent = new Color(0.2f, 0.6f, 1f),
                linkColor = new Color(0.2f, 0.55f, 0.95f)
            };
            return t;
        }

        public static ThemeData CreateDarkSlate()
        {
            var t = CreateDefault();
            t.name = "Dark Slate";
            t.priorityIcons = new List<string> { "", "🔹", "🔸", "🔶", "🔺" };
            t.boardTabIcon = "📁";
            t.notesTabIcon = "📄";
            t.styleTabIcon = "✨";
            t.boardHeaderIcon = "⚡";
            t.notesHeaderIcon = "📄";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👤";
            t.priorityFilterIcon = "🔺";
            t.parentLinkIcon = "🔷";
            t.childLinkIcon = "🔹";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "✔";
            t.overdueIcon = "🔺";
            t.dueTodayIcon = "🔸";
            t.dueSoonIcon = "🔹";
            t.dueDateIcon = "📆";
            t.archiveIcon = "📁";
            t.unarchiveIcon = "📂";
            t.cardDetailIcon = "📄";
            t.newCardIcon = "⚡";
            t.checklistIcon = "✔";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = new Color(0.3f, 0.85f, 1f);
            t.personal_BoardHeader = new Color(0.1f, 0.3f, 0.5f);
            t.pro_ColumnHeader = new Color(0.90f, 0.95f, 1f);
            t.personal_ColumnHeader = new Color(0.1f, 0.2f, 0.3f);
            t.pro_CardTitle = new Color(0.95f, 0.98f, 1f);
            t.personal_CardTitle = new Color(0.1f, 0.15f, 0.25f);
            t.pro_CardText = new Color(0.80f, 0.88f, 0.95f);
            t.personal_CardText = new Color(0.2f, 0.25f, 0.35f);
            t.pro_SectionLabel = new Color(0.3f, 0.85f, 1f);
            t.personal_SectionLabel = new Color(0.1f, 0.3f, 0.5f);
            t.pro_ColumnBg = new Color(0.14f, 0.16f, 0.19f);
            t.pro_ColumnBgAlt = new Color(0.17f, 0.20f, 0.23f);
            t.personal_ColumnBg = new Color(0.84f, 0.88f, 0.92f);
            t.personal_ColumnBgAlt = new Color(0.89f, 0.92f, 0.95f);
            t.pro_CardBg = new Color(0.18f, 0.22f, 0.28f);
            t.personal_CardBg = new Color(0.92f, 0.94f, 0.98f);
            t.pro_CardHighlighted = new Color(0.10f, 0.35f, 0.45f);
            t.personal_CardHighlighted = new Color(0.50f, 0.75f, 0.85f);
            t.pro_BoardBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_BoardBg = new Color(0.80f, 0.84f, 0.88f);
            t.pro_TopBarBg = new Color(0.08f, 0.10f, 0.12f);
            t.personal_TopBarBg = new Color(0.75f, 0.80f, 0.85f);
            t.pro_StatusBarBg = new Color(0.08f, 0.10f, 0.12f);
            t.personal_StatusBarBg = new Color(0.75f, 0.80f, 0.85f);
            t.pro_StatusBarText = new Color(0.70f, 0.80f, 0.88f);
            t.personal_StatusBarText = new Color(0.30f, 0.38f, 0.45f);
            t.pro_NoteSidebarBg = new Color(0.12f, 0.14f, 0.17f);
            t.personal_NoteSidebarBg = new Color(0.85f, 0.88f, 0.92f);
            t.pro_NoteEditorBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_NoteEditorBg = new Color(0.82f, 0.85f, 0.89f);
            t.pro_NotePopoutBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_NotePopoutBg = new Color(0.82f, 0.85f, 0.89f);
            t.pro_NoteInputBg = new Color(0.08f, 0.10f, 0.12f);
            t.personal_NoteInputBg = new Color(0.94f, 0.96f, 0.98f);
            t.pro_NoteInputText = new Color(0.9f, 0.95f, 1f);
            t.personal_NoteInputText = new Color(0.1f, 0.15f, 0.2f);
            t.pro_NoteTitle = new Color(0.95f, 0.98f, 1f);
            t.personal_NoteTitle = new Color(0.1f, 0.15f, 0.25f);
            t.pro_CardDetailBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_CardDetailBg = new Color(0.82f, 0.85f, 0.89f);
            t.pro_ButtonBg = new Color(0.18f, 0.22f, 0.27f);
            t.personal_ButtonBg = new Color(0.88f, 0.91f, 0.94f);
            t.pro_ButtonText = new Color(0.85f, 0.95f, 1f);
            t.personal_ButtonText = new Color(0.1f, 0.2f, 0.3f);
            t.pro_ButtonHoverBg = new Color(0.24f, 0.29f, 0.36f);
            t.personal_ButtonHoverBg = new Color(0.95f, 0.97f, 0.99f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.15f, 0.18f, 0.22f);
            t.personal_DropdownBg = new Color(0.90f, 0.93f, 0.96f);
            t.pro_DropdownText = new Color(0.85f, 0.95f, 1f);
            t.personal_DropdownText = new Color(0.1f, 0.2f, 0.3f);
            t.pro_DropdownHoverBg = new Color(0.20f, 0.25f, 0.30f);
            t.personal_DropdownHoverBg = new Color(0.96f, 0.98f, 1.0f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_DropdownMenuBg = new Color(0.88f, 0.90f, 0.94f);
            t.pro_DropdownMenuText = new Color(0.90f, 0.95f, 1.0f);
            t.personal_DropdownMenuText = new Color(0.10f, 0.15f, 0.20f);
            t.pro_DropdownMenuHoverBg = new Color(0.20f, 0.28f, 0.38f);
            t.personal_DropdownMenuHoverBg = new Color(0.75f, 0.82f, 0.92f);
            t.pro_DropdownMenuHoverText = new Color(0.40f, 0.80f, 1.0f);
            t.personal_DropdownMenuHoverText = new Color(0.05f, 0.10f, 0.18f);
            t.pro_PopupBg = new Color(0.10f, 0.12f, 0.15f);
            t.personal_PopupBg = new Color(0.88f, 0.90f, 0.94f);
            t.pro_DeleteBtnBg = new Color(0.45f, 0.15f, 0.18f);
            t.personal_DeleteBtnBg = new Color(0.85f, 0.30f, 0.35f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.60f, 0.20f, 0.24f);
            t.personal_DeleteBtnHoverBg = new Color(0.95f, 0.40f, 0.45f);
            t.pro_HeaderTabActiveBg = new Color(0.0f, 0.55f, 0.75f);
            t.personal_HeaderTabActiveBg = new Color(0.15f, 0.60f, 0.80f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.15f, 0.18f, 0.22f);
            t.personal_HeaderTabInactiveBg = new Color(0.86f, 0.90f, 0.94f);
            t.pro_HeaderTabInactiveText = new Color(0.85f, 0.95f, 1f);
            t.personal_HeaderTabInactiveText = new Color(0.1f, 0.2f, 0.3f);
            t.pro_HeaderTabHoverBg = new Color(0.22f, 0.28f, 0.35f);
            t.personal_HeaderTabHoverBg = new Color(0.94f, 0.96f, 0.99f);
            t.pro_AddCardBg = new Color(0.15f, 0.30f, 0.38f);
            t.personal_AddCardBg = new Color(0.80f, 0.90f, 0.96f);
            t.pro_AddCardText = new Color(0.8f, 0.95f, 1f);
            t.personal_AddCardText = new Color(0.05f, 0.25f, 0.35f);
            t.pro_AddCardHoverBg = new Color(0.20f, 0.40f, 0.50f);
            t.personal_AddCardHoverBg = new Color(0.88f, 0.96f, 1f);
            t.pro_NoteCardBg = new Color(0.15f, 0.18f, 0.22f);
            t.personal_NoteCardBg = new Color(0.88f, 0.92f, 0.96f);
            t.pro_NoteCardSelectedBg = new Color(0.10f, 0.35f, 0.45f);
            t.personal_NoteCardSelectedBg = new Color(0.50f, 0.75f, 0.85f);
            t.pro_NoteCardHoverBg = new Color(0.19f, 0.23f, 0.28f);
            t.personal_NoteCardHoverBg = new Color(0.93f, 0.95f, 0.98f);
            t.pro_NoteActionBg = new Color(0.20f, 0.35f, 0.50f);
            t.personal_NoteActionBg = new Color(0.78f, 0.85f, 0.92f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.10f, 0.20f, 0.30f);
            t.pro_NoteActionHoverBg = new Color(0.26f, 0.42f, 0.58f);
            t.personal_NoteActionHoverBg = new Color(0.84f, 0.90f, 0.96f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.10f, 0.20f, 0.30f);
            t.pro_TooltipBg = new Color(0.10f, 0.13f, 0.17f, 0.96f);
            t.personal_TooltipBg = new Color(0.92f, 0.95f, 0.98f, 0.96f);
            t.pro_TooltipText = new Color(0.85f, 0.95f, 1f);
            t.personal_TooltipText = new Color(0.10f, 0.15f, 0.25f);
            t.pro_TooltipBorder = new Color(0.20f, 0.45f, 0.65f, 0.8f);
            t.personal_TooltipBorder = new Color(0.50f, 0.70f, 0.85f, 0.8f);

            t.pro_AddNoteBg = new Color(0.15f, 0.40f, 0.60f);
            t.personal_AddNoteBg = new Color(0.78f, 0.88f, 0.96f);
            t.pro_AddNoteText = Color.white;
            t.personal_AddNoteText = new Color(0.10f, 0.22f, 0.35f);
            t.pro_AddNoteHoverBg = new Color(0.20f, 0.50f, 0.72f);
            t.personal_AddNoteHoverBg = new Color(0.85f, 0.93f, 0.99f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.10f, 0.22f, 0.35f);

            t.pro_ImportNoteBg = new Color(0.14f, 0.32f, 0.48f);
            t.personal_ImportNoteBg = new Color(0.82f, 0.90f, 0.96f);
            t.pro_ImportNoteText = new Color(0.85f, 0.95f, 1f);
            t.personal_ImportNoteText = new Color(0.12f, 0.25f, 0.38f);
            t.pro_ImportNoteHoverBg = new Color(0.18f, 0.42f, 0.60f);
            t.personal_ImportNoteHoverBg = new Color(0.88f, 0.94f, 0.99f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.08f, 0.20f, 0.32f);

            t.pro_NoteFolderText = new Color(0.80f, 0.90f, 0.98f);
            t.personal_NoteFolderText = new Color(0.15f, 0.25f, 0.35f);

            t.pro_CardDetailsText = new Color(0.70f, 0.80f, 0.88f);
            t.personal_CardDetailsText = new Color(0.30f, 0.38f, 0.45f);
            t.pro_CardTasksText = new Color(0.65f, 0.78f, 0.85f);
            t.personal_CardTasksText = new Color(0.28f, 0.35f, 0.42f);
            t.pro_CardCategoryTag = new Color(0.70f, 0.82f, 0.90f);
            t.personal_CardCategoryTag = new Color(0.25f, 0.35f, 0.45f);

            t.pro_AssigneeAvatarBg = new Color(0.18f, 0.20f, 0.24f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.90f, 0.92f, 0.95f, 1.00f);

            t.pro_StatusOverdue = new Color(1f, 0.38f, 0.35f);
            t.personal_StatusOverdue = new Color(0.85f, 0.22f, 0.18f);
            t.pro_StatusDueToday = new Color(1f, 0.68f, 0.22f);
            t.personal_StatusDueToday = new Color(0.88f, 0.50f, 0.08f);
            t.pro_StatusDueSoon = new Color(0.95f, 0.85f, 0.25f);
            t.personal_StatusDueSoon = new Color(0.78f, 0.68f, 0.10f);
            t.pro_StatusCompleted = new Color(0.25f, 0.85f, 0.70f);
            t.personal_StatusCompleted = new Color(0.10f, 0.60f, 0.48f);
            t.pro_TasksCompletedCount = new Color(0.25f, 0.85f, 0.70f);
            t.personal_TasksCompletedCount = new Color(0.10f, 0.60f, 0.48f);

            t.pro_ChecklistTickBg = new Color(0.12f, 0.15f, 0.20f);
            t.personal_ChecklistTickBg = new Color(0.92f, 0.95f, 0.98f);
            t.pro_ChecklistTickCheckedBg = new Color(0.00f, 0.55f, 0.75f);
            t.personal_ChecklistTickCheckedBg = new Color(0.15f, 0.60f, 0.80f);
            t.pro_ChecklistTickBorder = new Color(0.25f, 0.45f, 0.65f, 0.80f);
            t.personal_ChecklistTickBorder = new Color(0.50f, 0.70f, 0.85f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Classic;

            t.tabActive = new Color(0.0f, 0.65f, 0.85f);
            t.noteSelectedAccent = new Color(0.0f, 0.82f, 0.82f);
            t.linkColor = new Color(0.0f, 0.75f, 0.95f);
            return t;
        }

        public static ThemeData CreateCyberpunk()
        {
            var t = CreateDefault();
            t.name = "Cyberpunk Neon";
            t.priorityIcons = new List<string> { "", "⚡", "💎", "🔮", "🚨" };
            t.boardTabIcon = "💾";
            t.notesTabIcon = "📡";
            t.styleTabIcon = "🔮";
            t.boardHeaderIcon = "⚡";
            t.notesHeaderIcon = "📡";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👾";
            t.priorityFilterIcon = "🚨";
            t.parentLinkIcon = "🔗";
            t.childLinkIcon = "⚡";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "⚡";
            t.overdueIcon = "🚨";
            t.dueTodayIcon = "🔥";
            t.dueSoonIcon = "⚡";
            t.dueDateIcon = "⏱️";
            t.archiveIcon = "💾";
            t.unarchiveIcon = "💿";
            t.cardDetailIcon = "📡";
            t.newCardIcon = "🔮";
            t.checklistIcon = "⚡";
            t.attachmentIcon = "💾";
            t.urlIcon = "🌐";
            t.deleteIcon = "💀";
            t.saveIcon = "💿";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = new Color(1.0f, 0.2f, 0.6f);
            t.personal_BoardHeader = new Color(0.6f, 0.05f, 0.4f);
            t.pro_ColumnHeader = new Color(1.00f, 0.25f, 0.70f);
            t.personal_ColumnHeader = new Color(0.40f, 0.05f, 0.35f);
            t.pro_CardTitle = new Color(0.00f, 0.95f, 1.00f);
            t.personal_CardTitle = new Color(0.30f, 0.00f, 0.40f);
            t.pro_CardText = new Color(0.90f, 0.70f, 1.00f);
            t.personal_CardText = new Color(0.35f, 0.15f, 0.40f);
            t.pro_SectionLabel = new Color(1.00f, 0.25f, 0.70f);
            t.personal_SectionLabel = new Color(0.6f, 0.05f, 0.4f);
            t.pro_ColumnBg = new Color(0.13f, 0.11f, 0.19f);
            t.pro_ColumnBgAlt = new Color(0.17f, 0.14f, 0.24f);
            t.personal_ColumnBg = new Color(0.90f, 0.86f, 0.94f);
            t.personal_ColumnBgAlt = new Color(0.94f, 0.90f, 0.97f);
            t.pro_CardBg = new Color(0.12f, 0.08f, 0.20f);
            t.personal_CardBg = new Color(0.95f, 0.90f, 1.00f);
            t.pro_CardHighlighted = new Color(0.35f, 0.12f, 0.45f);
            t.personal_CardHighlighted = new Color(0.80f, 0.55f, 0.85f);
            t.pro_BoardBg = new Color(0.09f, 0.07f, 0.14f);
            t.personal_BoardBg = new Color(0.85f, 0.80f, 0.90f);
            t.pro_TopBarBg = new Color(0.16f, 0.08f, 0.22f);
            t.personal_TopBarBg = new Color(0.82f, 0.72f, 0.92f);
            t.pro_StatusBarBg = new Color(0.14f, 0.07f, 0.20f);
            t.personal_StatusBarBg = new Color(0.82f, 0.72f, 0.92f);
            t.pro_StatusBarText = new Color(0.80f, 0.65f, 0.92f);
            t.personal_StatusBarText = new Color(0.38f, 0.18f, 0.42f);
            t.pro_NoteSidebarBg = new Color(0.11f, 0.09f, 0.17f);
            t.personal_NoteSidebarBg = new Color(0.88f, 0.83f, 0.92f);
            t.pro_NoteEditorBg = new Color(0.09f, 0.07f, 0.14f);
            t.personal_NoteEditorBg = new Color(0.85f, 0.80f, 0.90f);
            t.pro_NotePopoutBg = new Color(0.09f, 0.07f, 0.14f);
            t.personal_NotePopoutBg = new Color(0.85f, 0.80f, 0.90f);
            t.pro_NoteInputBg = new Color(0.07f, 0.05f, 0.11f);
            t.personal_NoteInputBg = new Color(0.95f, 0.90f, 0.98f);
            t.pro_NoteInputText = new Color(0.95f, 0.85f, 1f);
            t.personal_NoteInputText = new Color(0.2f, 0.05f, 0.25f);
            t.pro_NoteTitle = new Color(0.00f, 0.95f, 1.00f);
            t.personal_NoteTitle = new Color(0.30f, 0.00f, 0.40f);
            t.pro_CardDetailBg = new Color(0.09f, 0.07f, 0.14f);
            t.personal_CardDetailBg = new Color(0.85f, 0.80f, 0.90f);
            t.pro_ButtonBg = new Color(0.24f, 0.12f, 0.32f);
            t.personal_ButtonBg = new Color(0.92f, 0.85f, 0.96f);
            t.pro_ButtonText = new Color(1.0f, 0.3f, 0.7f);
            t.personal_ButtonText = new Color(0.4f, 0.05f, 0.3f);
            t.pro_ButtonHoverBg = new Color(0.35f, 0.16f, 0.46f);
            t.personal_ButtonHoverBg = new Color(0.97f, 0.90f, 1.0f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.20f, 0.10f, 0.28f);
            t.personal_DropdownBg = new Color(0.94f, 0.88f, 0.97f);
            t.pro_DropdownText = new Color(0.2f, 0.95f, 0.85f);
            t.personal_DropdownText = new Color(0.3f, 0.05f, 0.25f);
            t.pro_DropdownHoverBg = new Color(0.30f, 0.14f, 0.40f);
            t.personal_DropdownHoverBg = new Color(0.98f, 0.93f, 1.0f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.08f, 0.05f, 0.12f);
            t.personal_DropdownMenuBg = new Color(0.92f, 0.88f, 0.95f);
            t.pro_DropdownMenuText = new Color(0.95f, 0.85f, 1.0f);
            t.personal_DropdownMenuText = new Color(0.20f, 0.05f, 0.25f);
            t.pro_DropdownMenuHoverBg = new Color(0.35f, 0.10f, 0.45f);
            t.personal_DropdownMenuHoverBg = new Color(0.82f, 0.70f, 0.90f);
            t.pro_DropdownMenuHoverText = new Color(1.0f, 0.40f, 0.80f);
            t.personal_DropdownMenuHoverText = new Color(0.15f, 0.02f, 0.20f);
            t.pro_PopupBg = new Color(0.09f, 0.07f, 0.14f);
            t.personal_PopupBg = new Color(0.90f, 0.85f, 0.95f);
            t.pro_DeleteBtnBg = new Color(0.60f, 0.10f, 0.25f);
            t.personal_DeleteBtnBg = new Color(0.85f, 0.25f, 0.40f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.80f, 0.15f, 0.35f);
            t.personal_DeleteBtnHoverBg = new Color(0.95f, 0.35f, 0.50f);
            t.pro_HeaderTabActiveBg = new Color(0.85f, 0.15f, 0.55f);
            t.personal_HeaderTabActiveBg = new Color(0.75f, 0.10f, 0.50f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.20f, 0.10f, 0.28f);
            t.personal_HeaderTabInactiveBg = new Color(0.90f, 0.82f, 0.95f);
            t.pro_HeaderTabInactiveText = new Color(0.95f, 0.65f, 0.90f);
            t.personal_HeaderTabInactiveText = new Color(0.40f, 0.05f, 0.35f);
            t.pro_HeaderTabHoverBg = new Color(0.32f, 0.15f, 0.42f);
            t.personal_HeaderTabHoverBg = new Color(0.96f, 0.90f, 0.99f);
            t.pro_AddCardBg = new Color(0.40f, 0.10f, 0.35f);
            t.personal_AddCardBg = new Color(0.92f, 0.78f, 0.90f);
            t.pro_AddCardText = new Color(1.0f, 0.45f, 0.85f);
            t.personal_AddCardText = new Color(0.50f, 0.05f, 0.40f);
            t.pro_AddCardHoverBg = new Color(0.55f, 0.15f, 0.48f);
            t.personal_AddCardHoverBg = new Color(0.97f, 0.86f, 0.96f);
            t.pro_NoteCardBg = new Color(0.18f, 0.12f, 0.25f);
            t.personal_NoteCardBg = new Color(0.92f, 0.86f, 0.95f);
            t.pro_NoteCardSelectedBg = new Color(0.35f, 0.12f, 0.45f);
            t.personal_NoteCardSelectedBg = new Color(0.80f, 0.55f, 0.85f);
            t.pro_NoteCardHoverBg = new Color(0.24f, 0.15f, 0.32f);
            t.personal_NoteCardHoverBg = new Color(0.96f, 0.91f, 0.98f);
            t.pro_NoteActionBg = new Color(0.00f, 0.60f, 0.60f, 0.85f);
            t.personal_NoteActionBg = new Color(0.00f, 0.70f, 0.70f, 0.30f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = Color.black;
            t.pro_NoteActionHoverBg = new Color(0.00f, 0.80f, 0.80f, 0.95f);
            t.personal_NoteActionHoverBg = new Color(0.00f, 0.80f, 0.80f, 0.45f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = Color.black;
            t.pro_TooltipBg = new Color(0.08f, 0.04f, 0.14f, 0.96f);
            t.personal_TooltipBg = new Color(0.96f, 0.92f, 0.98f, 0.96f);
            t.pro_TooltipText = new Color(0.00f, 1.00f, 0.90f);
            t.personal_TooltipText = new Color(0.40f, 0.00f, 0.50f);
            t.pro_TooltipBorder = new Color(1.00f, 0.10f, 0.60f, 0.9f);
            t.personal_TooltipBorder = new Color(0.80f, 0.10f, 0.50f, 0.8f);

            t.pro_AddNoteBg = new Color(0.50f, 0.10f, 0.45f);
            t.personal_AddNoteBg = new Color(0.88f, 0.75f, 0.92f);
            t.pro_AddNoteText = new Color(1.00f, 0.60f, 0.95f);
            t.personal_AddNoteText = new Color(0.40f, 0.05f, 0.35f);
            t.pro_AddNoteHoverBg = new Color(0.65f, 0.15f, 0.58f);
            t.personal_AddNoteHoverBg = new Color(0.94f, 0.82f, 0.97f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.30f, 0.00f, 0.30f);

            t.pro_ImportNoteBg = new Color(0.08f, 0.45f, 0.55f);
            t.personal_ImportNoteBg = new Color(0.75f, 0.90f, 0.94f);
            t.pro_ImportNoteText = new Color(0.40f, 1.00f, 0.95f);
            t.personal_ImportNoteText = new Color(0.05f, 0.30f, 0.35f);
            t.pro_ImportNoteHoverBg = new Color(0.12f, 0.58f, 0.70f);
            t.personal_ImportNoteHoverBg = new Color(0.84f, 0.95f, 0.98f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.02f, 0.22f, 0.28f);

            t.pro_NoteFolderText = new Color(0.85f, 0.75f, 0.95f);
            t.personal_NoteFolderText = new Color(0.35f, 0.15f, 0.40f);

            t.pro_CardDetailsText = new Color(0.80f, 0.65f, 0.92f);
            t.personal_CardDetailsText = new Color(0.38f, 0.18f, 0.42f);
            t.pro_CardTasksText = new Color(0.70f, 0.85f, 0.95f);
            t.personal_CardTasksText = new Color(0.25f, 0.25f, 0.38f);
            t.pro_CardCategoryTag = new Color(0.95f, 0.50f, 0.85f);
            t.personal_CardCategoryTag = new Color(0.50f, 0.10f, 0.45f);

            t.pro_AssigneeAvatarBg = new Color(0.12f, 0.10f, 0.20f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.96f, 0.92f, 0.98f, 1.00f);

            t.pro_StatusOverdue = new Color(1.00f, 0.20f, 0.40f);
            t.personal_StatusOverdue = new Color(0.88f, 0.15f, 0.30f);
            t.pro_StatusDueToday = new Color(1.00f, 0.60f, 0.10f);
            t.personal_StatusDueToday = new Color(0.88f, 0.45f, 0.05f);
            t.pro_StatusDueSoon = new Color(1.00f, 0.90f, 0.20f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.70f, 0.05f);
            t.pro_StatusCompleted = new Color(0.00f, 0.95f, 0.65f);
            t.personal_StatusCompleted = new Color(0.05f, 0.65f, 0.40f);
            t.pro_TasksCompletedCount = new Color(0.00f, 0.95f, 0.65f);
            t.personal_TasksCompletedCount = new Color(0.05f, 0.65f, 0.40f);

            t.pro_ChecklistTickBg = new Color(0.08f, 0.06f, 0.16f);
            t.personal_ChecklistTickBg = new Color(0.95f, 0.90f, 0.98f);
            t.pro_ChecklistTickCheckedBg = new Color(0.00f, 0.85f, 0.65f);
            t.personal_ChecklistTickCheckedBg = new Color(0.90f, 0.10f, 0.55f);
            t.pro_ChecklistTickBorder = new Color(0.00f, 0.95f, 1.00f, 0.85f);
            t.personal_ChecklistTickBorder = new Color(0.85f, 0.10f, 0.50f, 0.75f);
            t.pro_ChecklistTickColor = new Color(0.05f, 0.05f, 0.12f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Cross;

            t.tabActive = new Color(0.85f, 0.15f, 0.55f);
            t.noteSelectedAccent = new Color(0.0f, 0.95f, 0.85f);
            t.linkColor = new Color(0.95f, 0.2f, 0.7f);
            return t;
        }

        public static ThemeData CreateForest()
        {
            var t = CreateDefault();
            t.name = "Forest Emerald";
            t.priorityIcons = new List<string> { "", "🌱", "🌿", "🌲", "🔥" };
            t.boardTabIcon = "📋";
            t.notesTabIcon = "📜";
            t.styleTabIcon = "🍃";
            t.boardHeaderIcon = "🌲";
            t.notesHeaderIcon = "📜";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "🧑‍🌾";
            t.priorityFilterIcon = "🚩";
            t.parentLinkIcon = "🌳";
            t.childLinkIcon = "🌿";
            t.pinnedNoteIcon = "📌";
            t.completedIcon = "🌿";
            t.overdueIcon = "🍂";
            t.dueTodayIcon = "🌻";
            t.dueSoonIcon = "🌱";
            t.dueDateIcon = "🍃";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "🪵";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "🌿";
            t.checklistIcon = "🌿";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🍂";
            t.saveIcon = "📦";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = new Color(0.4f, 0.88f, 0.5f);
            t.personal_BoardHeader = new Color(0.1f, 0.4f, 0.2f);
            t.pro_ColumnHeader = new Color(0.85f, 0.98f, 0.88f);
            t.personal_ColumnHeader = new Color(0.1f, 0.35f, 0.18f);
            t.pro_CardTitle = new Color(0.90f, 1.00f, 0.92f);
            t.personal_CardTitle = new Color(0.1f, 0.25f, 0.15f);
            t.pro_CardText = new Color(0.75f, 0.90f, 0.78f);
            t.personal_CardText = new Color(0.18f, 0.30f, 0.20f);
            t.pro_SectionLabel = new Color(0.4f, 0.88f, 0.5f);
            t.personal_SectionLabel = new Color(0.1f, 0.4f, 0.2f);
            t.pro_ColumnBg = new Color(0.12f, 0.18f, 0.14f);
            t.pro_ColumnBgAlt = new Color(0.15f, 0.22f, 0.17f);
            t.personal_ColumnBg = new Color(0.86f, 0.91f, 0.87f);
            t.personal_ColumnBgAlt = new Color(0.90f, 0.94f, 0.91f);
            t.pro_CardBg = new Color(0.12f, 0.18f, 0.14f);
            t.personal_CardBg = new Color(0.90f, 0.95f, 0.92f);
            t.pro_CardHighlighted = new Color(0.15f, 0.38f, 0.22f);
            t.personal_CardHighlighted = new Color(0.60f, 0.82f, 0.65f);
            t.pro_BoardBg = new Color(0.08f, 0.13f, 0.09f);
            t.personal_BoardBg = new Color(0.82f, 0.88f, 0.83f);
            t.pro_TopBarBg = new Color(0.06f, 0.11f, 0.08f);
            t.personal_TopBarBg = new Color(0.76f, 0.84f, 0.78f);
            t.pro_StatusBarBg = new Color(0.06f, 0.11f, 0.08f);
            t.personal_StatusBarBg = new Color(0.76f, 0.84f, 0.78f);
            t.pro_StatusBarText = new Color(0.72f, 0.88f, 0.75f);
            t.personal_StatusBarText = new Color(0.25f, 0.38f, 0.28f);
            t.pro_NoteSidebarBg = new Color(0.10f, 0.15f, 0.11f);
            t.personal_NoteSidebarBg = new Color(0.86f, 0.90f, 0.87f);
            t.pro_NoteEditorBg = new Color(0.08f, 0.13f, 0.09f);
            t.personal_NoteEditorBg = new Color(0.82f, 0.88f, 0.83f);
            t.pro_NotePopoutBg = new Color(0.08f, 0.13f, 0.09f);
            t.personal_NotePopoutBg = new Color(0.82f, 0.88f, 0.83f);
            t.pro_NoteInputBg = new Color(0.06f, 0.10f, 0.07f);
            t.personal_NoteInputBg = new Color(0.93f, 0.96f, 0.94f);
            t.pro_NoteInputText = new Color(0.85f, 0.98f, 0.88f);
            t.personal_NoteInputText = new Color(0.08f, 0.25f, 0.12f);
            t.pro_NoteTitle = new Color(0.90f, 1.00f, 0.92f);
            t.personal_NoteTitle = new Color(0.1f, 0.25f, 0.15f);
            t.pro_CardDetailBg = new Color(0.08f, 0.13f, 0.09f);
            t.personal_CardDetailBg = new Color(0.82f, 0.88f, 0.83f);
            t.pro_ButtonBg = new Color(0.15f, 0.25f, 0.18f);
            t.personal_ButtonBg = new Color(0.88f, 0.94f, 0.90f);
            t.pro_ButtonText = new Color(0.7f, 0.95f, 0.75f);
            t.personal_ButtonText = new Color(0.1f, 0.35f, 0.15f);
            t.pro_ButtonHoverBg = new Color(0.22f, 0.35f, 0.25f);
            t.personal_ButtonHoverBg = new Color(0.94f, 0.98f, 0.95f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.12f, 0.21f, 0.15f);
            t.personal_DropdownBg = new Color(0.90f, 0.95f, 0.91f);
            t.pro_DropdownText = new Color(0.7f, 0.95f, 0.75f);
            t.personal_DropdownText = new Color(0.1f, 0.35f, 0.15f);
            t.pro_DropdownHoverBg = new Color(0.18f, 0.28f, 0.22f);
            t.personal_DropdownHoverBg = new Color(0.96f, 0.99f, 0.97f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.07f, 0.11f, 0.08f);
            t.personal_DropdownMenuBg = new Color(0.88f, 0.92f, 0.89f);
            t.pro_DropdownMenuText = new Color(0.85f, 0.98f, 0.88f);
            t.personal_DropdownMenuText = new Color(0.08f, 0.25f, 0.12f);
            t.pro_DropdownMenuHoverBg = new Color(0.18f, 0.32f, 0.22f);
            t.personal_DropdownMenuHoverBg = new Color(0.74f, 0.84f, 0.76f);
            t.pro_DropdownMenuHoverText = new Color(0.40f, 0.95f, 0.55f);
            t.personal_DropdownMenuHoverText = new Color(0.05f, 0.18f, 0.08f);
            t.pro_PopupBg = new Color(0.08f, 0.13f, 0.09f);
            t.personal_PopupBg = new Color(0.86f, 0.90f, 0.87f);
            t.pro_DeleteBtnBg = new Color(0.50f, 0.18f, 0.15f);
            t.personal_DeleteBtnBg = new Color(0.80f, 0.30f, 0.25f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.65f, 0.22f, 0.18f);
            t.personal_DeleteBtnHoverBg = new Color(0.90f, 0.40f, 0.35f);
            t.pro_HeaderTabActiveBg = new Color(0.18f, 0.65f, 0.35f);
            t.personal_HeaderTabActiveBg = new Color(0.15f, 0.60f, 0.30f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.12f, 0.21f, 0.15f);
            t.personal_HeaderTabInactiveBg = new Color(0.86f, 0.92f, 0.88f);
            t.pro_HeaderTabInactiveText = new Color(0.70f, 0.95f, 0.75f);
            t.personal_HeaderTabInactiveText = new Color(0.10f, 0.35f, 0.15f);
            t.pro_HeaderTabHoverBg = new Color(0.19f, 0.30f, 0.23f);
            t.personal_HeaderTabHoverBg = new Color(0.93f, 0.97f, 0.94f);
            t.pro_AddCardBg = new Color(0.18f, 0.38f, 0.24f);
            t.personal_AddCardBg = new Color(0.82f, 0.93f, 0.86f);
            t.pro_AddCardText = new Color(0.8f, 1f, 0.85f);
            t.personal_AddCardText = new Color(0.08f, 0.35f, 0.12f);
            t.pro_AddCardHoverBg = new Color(0.24f, 0.48f, 0.32f);
            t.personal_AddCardHoverBg = new Color(0.88f, 0.97f, 0.91f);
            t.pro_NoteCardBg = new Color(0.14f, 0.22f, 0.17f);
            t.personal_NoteCardBg = new Color(0.88f, 0.94f, 0.90f);
            t.pro_NoteCardSelectedBg = new Color(0.15f, 0.38f, 0.22f);
            t.personal_NoteCardSelectedBg = new Color(0.60f, 0.82f, 0.65f);
            t.pro_NoteCardHoverBg = new Color(0.18f, 0.28f, 0.22f);
            t.personal_NoteCardHoverBg = new Color(0.94f, 0.97f, 0.95f);
            t.pro_NoteActionBg = new Color(0.18f, 0.45f, 0.30f);
            t.personal_NoteActionBg = new Color(0.78f, 0.90f, 0.82f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.08f, 0.28f, 0.15f);
            t.pro_NoteActionHoverBg = new Color(0.24f, 0.52f, 0.36f);
            t.personal_NoteActionHoverBg = new Color(0.84f, 0.94f, 0.88f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.08f, 0.28f, 0.15f);
            t.pro_TooltipBg = new Color(0.07f, 0.13f, 0.09f, 0.96f);
            t.personal_TooltipBg = new Color(0.92f, 0.97f, 0.94f, 0.96f);
            t.pro_TooltipText = new Color(0.80f, 1.00f, 0.85f);
            t.personal_TooltipText = new Color(0.08f, 0.25f, 0.12f);
            t.pro_TooltipBorder = new Color(0.25f, 0.65f, 0.35f, 0.85f);
            t.personal_TooltipBorder = new Color(0.45f, 0.75f, 0.52f, 0.85f);

            t.pro_AddNoteBg = new Color(0.18f, 0.38f, 0.24f);
            t.personal_AddNoteBg = new Color(0.80f, 0.90f, 0.84f);
            t.pro_AddNoteText = new Color(0.80f, 1.00f, 0.85f);
            t.personal_AddNoteText = new Color(0.08f, 0.28f, 0.14f);
            t.pro_AddNoteHoverBg = new Color(0.24f, 0.48f, 0.30f);
            t.personal_AddNoteHoverBg = new Color(0.88f, 0.95f, 0.90f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.05f, 0.22f, 0.10f);

            t.pro_ImportNoteBg = new Color(0.22f, 0.32f, 0.20f);
            t.personal_ImportNoteBg = new Color(0.84f, 0.91f, 0.82f);
            t.pro_ImportNoteText = new Color(0.85f, 0.95f, 0.80f);
            t.personal_ImportNoteText = new Color(0.15f, 0.30f, 0.12f);
            t.pro_ImportNoteHoverBg = new Color(0.28f, 0.40f, 0.25f);
            t.personal_ImportNoteHoverBg = new Color(0.90f, 0.96f, 0.88f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.10f, 0.24f, 0.08f);

            t.pro_NoteFolderText = new Color(0.80f, 0.95f, 0.82f);
            t.personal_NoteFolderText = new Color(0.12f, 0.28f, 0.15f);

            t.pro_CardDetailsText = new Color(0.72f, 0.88f, 0.75f);
            t.personal_CardDetailsText = new Color(0.25f, 0.38f, 0.28f);
            t.pro_CardTasksText = new Color(0.68f, 0.82f, 0.70f);
            t.personal_CardTasksText = new Color(0.22f, 0.35f, 0.25f);
            t.pro_CardCategoryTag = new Color(0.75f, 0.92f, 0.78f);
            t.personal_CardCategoryTag = new Color(0.18f, 0.36f, 0.22f);

            t.pro_AssigneeAvatarBg = new Color(0.12f, 0.18f, 0.14f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.90f, 0.94f, 0.91f, 1.00f);

            t.pro_StatusOverdue = new Color(0.95f, 0.40f, 0.30f);
            t.personal_StatusOverdue = new Color(0.82f, 0.25f, 0.18f);
            t.pro_StatusDueToday = new Color(0.95f, 0.70f, 0.20f);
            t.personal_StatusDueToday = new Color(0.85f, 0.52f, 0.08f);
            t.pro_StatusDueSoon = new Color(0.88f, 0.85f, 0.25f);
            t.personal_StatusDueSoon = new Color(0.72f, 0.68f, 0.12f);
            t.pro_StatusCompleted = new Color(0.35f, 0.90f, 0.48f);
            t.personal_StatusCompleted = new Color(0.12f, 0.65f, 0.25f);
            t.pro_TasksCompletedCount = new Color(0.35f, 0.90f, 0.48f);
            t.personal_TasksCompletedCount = new Color(0.12f, 0.65f, 0.25f);

            t.pro_ChecklistTickBg = new Color(0.08f, 0.14f, 0.10f);
            t.personal_ChecklistTickBg = new Color(0.90f, 0.96f, 0.92f);
            t.pro_ChecklistTickCheckedBg = new Color(0.20f, 0.62f, 0.35f);
            t.personal_ChecklistTickCheckedBg = new Color(0.18f, 0.60f, 0.30f);
            t.pro_ChecklistTickBorder = new Color(0.25f, 0.55f, 0.35f, 0.85f);
            t.personal_ChecklistTickBorder = new Color(0.40f, 0.68f, 0.50f);
            t.pro_ChecklistTickColor = new Color(0.92f, 1.00f, 0.94f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Classic;

            t.tabActive = new Color(0.18f, 0.65f, 0.35f);
            t.noteSelectedAccent = new Color(0.3f, 0.85f, 0.45f);
            t.linkColor = new Color(0.2f, 0.75f, 0.4f);
            return t;
        }

        public static ThemeData CreatePastel()
        {
            var t = CreateDefault();
            t.name = "Pastel Dream";
            t.priorityIcons = new List<string> { "", "🌸", "🌷", "🌼", "🌺" };
            t.boardTabIcon = "📋";
            t.notesTabIcon = "📝";
            t.styleTabIcon = "🌸";
            t.boardHeaderIcon = "✨";
            t.notesHeaderIcon = "📝";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "🐰";
            t.priorityFilterIcon = "🌸";
            t.parentLinkIcon = "🌸";
            t.childLinkIcon = "🌷";
            t.pinnedNoteIcon = "🎀";
            t.completedIcon = "💖";
            t.overdueIcon = "💔";
            t.dueTodayIcon = "🌸";
            t.dueSoonIcon = "🌼";
            t.dueDateIcon = "🎀";
            t.archiveIcon = "🎁";
            t.unarchiveIcon = "🎀";
            t.cardDetailIcon = "💌";
            t.newCardIcon = "✨";
            t.checklistIcon = "💖";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "🎁";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = new Color(0.85f, 0.75f, 0.95f);
            t.personal_BoardHeader = new Color(0.45f, 0.35f, 0.6f);
            t.pro_ColumnHeader = new Color(0.92f, 0.85f, 0.98f);
            t.personal_ColumnHeader = new Color(0.35f, 0.25f, 0.45f);
            t.pro_CardTitle = new Color(0.95f, 0.90f, 1f);
            t.personal_CardTitle = new Color(0.25f, 0.15f, 0.35f);
            t.pro_CardText = new Color(0.85f, 0.80f, 0.92f);
            t.personal_CardText = new Color(0.35f, 0.25f, 0.45f);
            t.pro_SectionLabel = new Color(0.85f, 0.75f, 0.95f);
            t.personal_SectionLabel = new Color(0.45f, 0.35f, 0.6f);
            t.pro_ColumnBg = new Color(0.18f, 0.16f, 0.22f);
            t.pro_ColumnBgAlt = new Color(0.22f, 0.19f, 0.26f);
            t.personal_ColumnBg = new Color(0.93f, 0.90f, 0.96f);
            t.personal_ColumnBgAlt = new Color(0.96f, 0.93f, 0.98f);
            t.pro_CardBg = new Color(0.20f, 0.16f, 0.24f);
            t.personal_CardBg = new Color(0.96f, 0.92f, 0.98f);
            t.pro_CardHighlighted = new Color(0.35f, 0.25f, 0.45f);
            t.personal_CardHighlighted = new Color(0.85f, 0.75f, 0.95f);
            t.pro_BoardBg = new Color(0.15f, 0.12f, 0.18f);
            t.personal_BoardBg = new Color(0.95f, 0.88f, 0.96f);
            t.pro_TopBarBg = new Color(0.24f, 0.16f, 0.28f);
            t.personal_TopBarBg = new Color(0.92f, 0.80f, 0.95f);
            t.pro_StatusBarBg = new Color(0.22f, 0.15f, 0.26f);
            t.personal_StatusBarBg = new Color(0.90f, 0.80f, 0.94f);
            t.pro_StatusBarText = new Color(0.82f, 0.75f, 0.90f);
            t.personal_StatusBarText = new Color(0.38f, 0.28f, 0.48f);
            t.pro_NoteSidebarBg = new Color(0.17f, 0.14f, 0.20f);
            t.personal_NoteSidebarBg = new Color(0.96f, 0.90f, 0.97f);
            t.pro_NoteEditorBg = new Color(0.15f, 0.12f, 0.18f);
            t.personal_NoteEditorBg = new Color(0.95f, 0.88f, 0.96f);
            t.pro_NotePopoutBg = new Color(0.15f, 0.12f, 0.18f);
            t.personal_NotePopoutBg = new Color(0.95f, 0.88f, 0.96f);
            t.pro_NoteInputBg = new Color(0.12f, 0.10f, 0.15f);
            t.personal_NoteInputBg = new Color(0.98f, 0.95f, 0.99f);
            t.pro_NoteInputText = new Color(0.95f, 0.9f, 1f);
            t.personal_NoteInputText = new Color(0.3f, 0.15f, 0.4f);
            t.pro_NoteTitle = new Color(0.95f, 0.90f, 1f);
            t.personal_NoteTitle = new Color(0.25f, 0.15f, 0.35f);
            t.pro_CardDetailBg = new Color(0.15f, 0.12f, 0.18f);
            t.personal_CardDetailBg = new Color(0.95f, 0.88f, 0.96f);
            t.pro_ButtonBg = new Color(0.28f, 0.20f, 0.32f);
            t.personal_ButtonBg = new Color(0.96f, 0.88f, 0.97f);
            t.pro_ButtonText = new Color(0.95f, 0.85f, 0.98f);
            t.personal_ButtonText = new Color(0.35f, 0.2f, 0.45f);
            t.pro_ButtonHoverBg = new Color(0.38f, 0.28f, 0.44f);
            t.personal_ButtonHoverBg = new Color(0.99f, 0.94f, 1.0f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.24f, 0.18f, 0.28f);
            t.personal_DropdownBg = new Color(0.97f, 0.91f, 0.98f);
            t.pro_DropdownText = new Color(0.95f, 0.85f, 0.98f);
            t.personal_DropdownText = new Color(0.35f, 0.2f, 0.45f);
            t.pro_DropdownHoverBg = new Color(0.32f, 0.24f, 0.38f);
            t.personal_DropdownHoverBg = new Color(1.0f, 0.96f, 1.0f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.14f, 0.11f, 0.16f);
            t.personal_DropdownMenuBg = new Color(0.96f, 0.90f, 0.97f);
            t.pro_DropdownMenuText = new Color(0.95f, 0.90f, 1.0f);
            t.personal_DropdownMenuText = new Color(0.30f, 0.15f, 0.40f);
            t.pro_DropdownMenuHoverBg = new Color(0.32f, 0.24f, 0.38f);
            t.personal_DropdownMenuHoverBg = new Color(0.88f, 0.78f, 0.92f);
            t.pro_DropdownMenuHoverText = new Color(1.0f, 0.80f, 0.95f);
            t.personal_DropdownMenuHoverText = new Color(0.20f, 0.08f, 0.30f);
            t.pro_PopupBg = new Color(0.15f, 0.12f, 0.18f);
            t.personal_PopupBg = new Color(0.94f, 0.88f, 0.95f);
            t.pro_DeleteBtnBg = new Color(0.55f, 0.25f, 0.30f);
            t.personal_DeleteBtnBg = new Color(0.90f, 0.45f, 0.50f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.70f, 0.30f, 0.38f);
            t.personal_DeleteBtnHoverBg = new Color(0.98f, 0.55f, 0.60f);
            t.pro_HeaderTabActiveBg = new Color(0.65f, 0.55f, 0.88f);
            t.personal_HeaderTabActiveBg = new Color(0.60f, 0.45f, 0.82f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.24f, 0.18f, 0.28f);
            t.personal_HeaderTabInactiveBg = new Color(0.94f, 0.88f, 0.96f);
            t.pro_HeaderTabInactiveText = new Color(0.90f, 0.80f, 0.95f);
            t.personal_HeaderTabInactiveText = new Color(0.40f, 0.25f, 0.50f);
            t.pro_HeaderTabHoverBg = new Color(0.34f, 0.26f, 0.40f);
            t.personal_HeaderTabHoverBg = new Color(0.98f, 0.93f, 0.99f);
            t.pro_AddCardBg = new Color(0.35f, 0.22f, 0.38f);
            t.personal_AddCardBg = new Color(0.94f, 0.84f, 0.95f);
            t.pro_AddCardText = new Color(0.98f, 0.85f, 0.99f);
            t.personal_AddCardText = new Color(0.40f, 0.15f, 0.45f);
            t.pro_AddCardHoverBg = new Color(0.46f, 0.30f, 0.50f);
            t.personal_AddCardHoverBg = new Color(0.98f, 0.90f, 0.99f);
            t.pro_NoteCardBg = new Color(0.22f, 0.18f, 0.26f);
            t.personal_NoteCardBg = new Color(0.94f, 0.90f, 0.97f);
            t.pro_NoteCardSelectedBg = new Color(0.35f, 0.25f, 0.45f);
            t.personal_NoteCardSelectedBg = new Color(0.85f, 0.75f, 0.95f);
            t.pro_NoteCardHoverBg = new Color(0.28f, 0.23f, 0.33f);
            t.personal_NoteCardHoverBg = new Color(0.97f, 0.94f, 0.99f);
            t.pro_NoteActionBg = new Color(0.40f, 0.35f, 0.55f);
            t.personal_NoteActionBg = new Color(0.88f, 0.84f, 0.96f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.25f, 0.18f, 0.38f);
            t.pro_NoteActionHoverBg = new Color(0.48f, 0.42f, 0.64f);
            t.personal_NoteActionHoverBg = new Color(0.93f, 0.90f, 0.98f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.25f, 0.18f, 0.38f);
            t.pro_TooltipBg = new Color(0.16f, 0.12f, 0.18f, 0.96f);
            t.personal_TooltipBg = new Color(0.98f, 0.94f, 0.97f, 0.96f);
            t.pro_TooltipText = new Color(0.98f, 0.88f, 0.95f);
            t.personal_TooltipText = new Color(0.35f, 0.15f, 0.30f);
            t.pro_TooltipBorder = new Color(0.75f, 0.50f, 0.70f, 0.8f);
            t.personal_TooltipBorder = new Color(0.88f, 0.65f, 0.82f, 0.8f);

            t.pro_AddNoteBg = new Color(0.48f, 0.32f, 0.52f);
            t.personal_AddNoteBg = new Color(0.90f, 0.80f, 0.95f);
            t.pro_AddNoteText = new Color(0.98f, 0.90f, 1.00f);
            t.personal_AddNoteText = new Color(0.35f, 0.18f, 0.45f);
            t.pro_AddNoteHoverBg = new Color(0.56f, 0.38f, 0.60f);
            t.personal_AddNoteHoverBg = new Color(0.95f, 0.88f, 0.98f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.28f, 0.10f, 0.38f);

            t.pro_ImportNoteBg = new Color(0.38f, 0.30f, 0.48f);
            t.personal_ImportNoteBg = new Color(0.86f, 0.82f, 0.94f);
            t.pro_ImportNoteText = new Color(0.95f, 0.88f, 0.98f);
            t.personal_ImportNoteText = new Color(0.30f, 0.18f, 0.42f);
            t.pro_ImportNoteHoverBg = new Color(0.46f, 0.36f, 0.56f);
            t.personal_ImportNoteHoverBg = new Color(0.92f, 0.88f, 0.97f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.22f, 0.12f, 0.34f);

            t.pro_NoteFolderText = new Color(0.90f, 0.82f, 0.96f);
            t.personal_NoteFolderText = new Color(0.35f, 0.22f, 0.45f);

            t.pro_CardDetailsText = new Color(0.82f, 0.75f, 0.90f);
            t.personal_CardDetailsText = new Color(0.38f, 0.28f, 0.48f);
            t.pro_CardTasksText = new Color(0.78f, 0.70f, 0.86f);
            t.personal_CardTasksText = new Color(0.32f, 0.24f, 0.42f);
            t.pro_CardCategoryTag = new Color(0.88f, 0.75f, 0.94f);
            t.personal_CardCategoryTag = new Color(0.42f, 0.25f, 0.52f);

            t.pro_AssigneeAvatarBg = new Color(0.22f, 0.20f, 0.26f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.96f, 0.94f, 0.98f, 1.00f);

            t.pro_StatusOverdue = new Color(0.95f, 0.45f, 0.55f);
            t.personal_StatusOverdue = new Color(0.85f, 0.30f, 0.40f);
            t.pro_StatusDueToday = new Color(0.98f, 0.65f, 0.50f);
            t.personal_StatusDueToday = new Color(0.88f, 0.48f, 0.30f);
            t.pro_StatusDueSoon = new Color(0.95f, 0.82f, 0.40f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.65f, 0.15f);
            t.pro_StatusCompleted = new Color(0.55f, 0.88f, 0.65f);
            t.personal_StatusCompleted = new Color(0.25f, 0.68f, 0.40f);
            t.pro_TasksCompletedCount = new Color(0.55f, 0.88f, 0.65f);
            t.personal_TasksCompletedCount = new Color(0.25f, 0.68f, 0.40f);

            t.pro_ChecklistTickBg = new Color(0.18f, 0.14f, 0.24f);
            t.personal_ChecklistTickBg = new Color(0.96f, 0.92f, 0.98f);
            t.pro_ChecklistTickCheckedBg = new Color(0.65f, 0.45f, 0.85f);
            t.personal_ChecklistTickCheckedBg = new Color(0.68f, 0.48f, 0.86f);
            t.pro_ChecklistTickBorder = new Color(0.60f, 0.45f, 0.75f, 0.85f);
            t.personal_ChecklistTickBorder = new Color(0.75f, 0.60f, 0.88f);
            t.pro_ChecklistTickColor = new Color(0.98f, 0.96f, 1.00f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Dot;

            t.tabActive = new Color(0.65f, 0.55f, 0.88f);
            t.noteSelectedAccent = new Color(0.95f, 0.55f, 0.75f);
            t.linkColor = new Color(0.7f, 0.5f, 0.9f);
            return t;
        }

        public static ThemeData CreateSunset()
        {
            var t = CreateDefault();
            t.name = "Sunset Warm";
            t.priorityIcons = new List<string> { "", "🕯️", "🪵", "🌇", "🔥" };
            t.boardTabIcon = "📋";
            t.notesTabIcon = "📝";
            t.styleTabIcon = "🌅";
            t.boardHeaderIcon = "☀️";
            t.notesHeaderIcon = "📝";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "🦊";
            t.priorityFilterIcon = "🔥";
            t.parentLinkIcon = "🪵";
            t.childLinkIcon = "🍂";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "✨";
            t.overdueIcon = "🔥";
            t.dueTodayIcon = "☀️";
            t.dueSoonIcon = "🌤️";
            t.dueDateIcon = "🌅";
            t.archiveIcon = "💼";
            t.unarchiveIcon = "🗂️";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "✨";
            t.checklistIcon = "✨";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💼";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = new Color(1.0f, 0.7f, 0.4f);
            t.personal_BoardHeader = new Color(0.6f, 0.25f, 0.1f);
            t.pro_ColumnHeader = new Color(1.00f, 0.85f, 0.60f);
            t.personal_ColumnHeader = new Color(0.45f, 0.20f, 0.08f);
            t.pro_CardTitle = new Color(1.00f, 0.92f, 0.80f);
            t.personal_CardTitle = new Color(0.35f, 0.15f, 0.05f);
            t.pro_CardText = new Color(1.00f, 0.80f, 0.70f);
            t.personal_CardText = new Color(0.40f, 0.22f, 0.12f);
            t.pro_SectionLabel = new Color(1.0f, 0.7f, 0.4f);
            t.personal_SectionLabel = new Color(0.6f, 0.25f, 0.1f);
            t.pro_ColumnBg = new Color(0.20f, 0.15f, 0.13f);
            t.pro_ColumnBgAlt = new Color(0.25f, 0.18f, 0.15f);
            t.personal_ColumnBg = new Color(0.95f, 0.90f, 0.86f);
            t.personal_ColumnBgAlt = new Color(0.97f, 0.93f, 0.90f);
            t.pro_CardBg = new Color(0.22f, 0.15f, 0.12f);
            t.personal_CardBg = new Color(0.98f, 0.92f, 0.88f);
            t.pro_CardHighlighted = new Color(0.45f, 0.25f, 0.15f);
            t.personal_CardHighlighted = new Color(0.95f, 0.75f, 0.60f);
            t.pro_BoardBg = new Color(0.16f, 0.11f, 0.09f);
            t.personal_BoardBg = new Color(0.92f, 0.86f, 0.82f);
            t.pro_TopBarBg = new Color(0.24f, 0.14f, 0.10f);
            t.personal_TopBarBg = new Color(0.90f, 0.78f, 0.72f);
            t.pro_StatusBarBg = new Color(0.22f, 0.12f, 0.08f);
            t.personal_StatusBarBg = new Color(0.90f, 0.78f, 0.72f);
            t.pro_StatusBarText = new Color(0.92f, 0.78f, 0.68f);
            t.personal_StatusBarText = new Color(0.42f, 0.25f, 0.15f);
            t.pro_NoteSidebarBg = new Color(0.18f, 0.13f, 0.11f);
            t.personal_NoteSidebarBg = new Color(0.94f, 0.89f, 0.85f);
            t.pro_NoteEditorBg = new Color(0.16f, 0.11f, 0.09f);
            t.personal_NoteEditorBg = new Color(0.92f, 0.86f, 0.82f);
            t.pro_NotePopoutBg = new Color(0.16f, 0.11f, 0.09f);
            t.personal_NotePopoutBg = new Color(0.92f, 0.86f, 0.82f);
            t.pro_NoteInputBg = new Color(0.13f, 0.09f, 0.07f);
            t.personal_NoteInputBg = new Color(0.98f, 0.95f, 0.92f);
            t.pro_NoteInputText = new Color(1.0f, 0.9f, 0.8f);
            t.personal_NoteInputText = new Color(0.35f, 0.15f, 0.05f);
            t.pro_NoteTitle = new Color(1.00f, 0.92f, 0.80f);
            t.personal_NoteTitle = new Color(0.35f, 0.15f, 0.05f);
            t.pro_CardDetailBg = new Color(0.16f, 0.11f, 0.09f);
            t.personal_CardDetailBg = new Color(0.92f, 0.86f, 0.82f);
            t.pro_ButtonBg = new Color(0.30f, 0.18f, 0.14f);
            t.personal_ButtonBg = new Color(0.96f, 0.90f, 0.86f);
            t.pro_ButtonText = new Color(1.0f, 0.8f, 0.65f);
            t.personal_ButtonText = new Color(0.45f, 0.18f, 0.05f);
            t.pro_ButtonHoverBg = new Color(0.40f, 0.25f, 0.18f);
            t.personal_ButtonHoverBg = new Color(0.99f, 0.95f, 0.92f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.25f, 0.15f, 0.11f);
            t.personal_DropdownBg = new Color(0.97f, 0.92f, 0.88f);
            t.pro_DropdownText = new Color(1.0f, 0.8f, 0.65f);
            t.personal_DropdownText = new Color(0.45f, 0.18f, 0.05f);
            t.pro_DropdownHoverBg = new Color(0.35f, 0.22f, 0.16f);
            t.personal_DropdownHoverBg = new Color(1.0f, 0.96f, 0.93f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.15f, 0.10f, 0.08f);
            t.personal_DropdownMenuBg = new Color(0.95f, 0.90f, 0.86f);
            t.pro_DropdownMenuText = new Color(1.0f, 0.90f, 0.80f);
            t.personal_DropdownMenuText = new Color(0.35f, 0.15f, 0.05f);
            t.pro_DropdownMenuHoverBg = new Color(0.38f, 0.22f, 0.14f);
            t.personal_DropdownMenuHoverBg = new Color(0.90f, 0.80f, 0.72f);
            t.pro_DropdownMenuHoverText = new Color(1.0f, 0.75f, 0.45f);
            t.personal_DropdownMenuHoverText = new Color(0.25f, 0.10f, 0.02f);
            t.pro_PopupBg = new Color(0.16f, 0.11f, 0.09f);
            t.personal_PopupBg = new Color(0.93f, 0.87f, 0.83f);
            t.pro_DeleteBtnBg = new Color(0.58f, 0.18f, 0.12f);
            t.personal_DeleteBtnBg = new Color(0.88f, 0.35f, 0.25f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.75f, 0.22f, 0.16f);
            t.personal_DeleteBtnHoverBg = new Color(0.98f, 0.45f, 0.35f);
            t.pro_HeaderTabActiveBg = new Color(0.9f, 0.45f, 0.15f);
            t.personal_HeaderTabActiveBg = new Color(0.85f, 0.40f, 0.10f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.25f, 0.15f, 0.11f);
            t.personal_HeaderTabInactiveBg = new Color(0.94f, 0.88f, 0.84f);
            t.pro_HeaderTabInactiveText = new Color(1.0f, 0.8f, 0.65f);
            t.personal_HeaderTabInactiveText = new Color(0.45f, 0.20f, 0.08f);
            t.pro_HeaderTabHoverBg = new Color(0.36f, 0.22f, 0.16f);
            t.personal_HeaderTabHoverBg = new Color(0.98f, 0.93f, 0.90f);
            t.pro_AddCardBg = new Color(0.42f, 0.22f, 0.12f);
            t.personal_AddCardBg = new Color(0.96f, 0.85f, 0.78f);
            t.pro_AddCardText = new Color(1.0f, 0.85f, 0.70f);
            t.personal_AddCardText = new Color(0.50f, 0.15f, 0.05f);
            t.pro_AddCardHoverBg = new Color(0.54f, 0.28f, 0.16f);
            t.personal_AddCardHoverBg = new Color(0.99f, 0.90f, 0.84f);
            t.pro_NoteCardBg = new Color(0.24f, 0.17f, 0.14f);
            t.personal_NoteCardBg = new Color(0.95f, 0.90f, 0.86f);
            t.pro_NoteCardSelectedBg = new Color(0.45f, 0.25f, 0.15f);
            t.personal_NoteCardSelectedBg = new Color(0.95f, 0.75f, 0.60f);
            t.pro_NoteCardHoverBg = new Color(0.30f, 0.22f, 0.18f);
            t.personal_NoteCardHoverBg = new Color(0.98f, 0.94f, 0.91f);
            t.pro_NoteActionBg = new Color(0.60f, 0.30f, 0.20f);
            t.personal_NoteActionBg = new Color(0.96f, 0.85f, 0.80f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.40f, 0.15f, 0.08f);
            t.pro_NoteActionHoverBg = new Color(0.70f, 0.38f, 0.26f);
            t.personal_NoteActionHoverBg = new Color(0.98f, 0.90f, 0.86f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.40f, 0.15f, 0.08f);
            t.pro_TooltipBg = new Color(0.16f, 0.09f, 0.08f, 0.96f);
            t.personal_TooltipBg = new Color(0.99f, 0.94f, 0.92f, 0.96f);
            t.pro_TooltipText = new Color(1.00f, 0.90f, 0.82f);
            t.personal_TooltipText = new Color(0.40f, 0.15f, 0.08f);
            t.pro_TooltipBorder = new Color(0.85f, 0.45f, 0.25f, 0.85f);
            t.personal_TooltipBorder = new Color(0.92f, 0.60f, 0.40f, 0.85f);

            t.pro_AddNoteBg = new Color(0.65f, 0.32f, 0.18f);
            t.personal_AddNoteBg = new Color(0.96f, 0.85f, 0.78f);
            t.pro_AddNoteText = new Color(1.00f, 0.92f, 0.85f);
            t.personal_AddNoteText = new Color(0.45f, 0.15f, 0.05f);
            t.pro_AddNoteHoverBg = new Color(0.75f, 0.40f, 0.24f);
            t.personal_AddNoteHoverBg = new Color(0.98f, 0.90f, 0.84f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.35f, 0.10f, 0.02f);

            t.pro_ImportNoteBg = new Color(0.50f, 0.28f, 0.18f);
            t.personal_ImportNoteBg = new Color(0.92f, 0.84f, 0.78f);
            t.pro_ImportNoteText = new Color(1.00f, 0.88f, 0.80f);
            t.personal_ImportNoteText = new Color(0.40f, 0.18f, 0.08f);
            t.pro_ImportNoteHoverBg = new Color(0.60f, 0.35f, 0.22f);
            t.personal_ImportNoteHoverBg = new Color(0.96f, 0.89f, 0.84f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.30f, 0.12f, 0.04f);

            t.pro_NoteFolderText = new Color(1.00f, 0.85f, 0.72f);
            t.personal_NoteFolderText = new Color(0.40f, 0.20f, 0.10f);

            t.pro_CardDetailsText = new Color(0.92f, 0.78f, 0.68f);
            t.personal_CardDetailsText = new Color(0.42f, 0.25f, 0.15f);
            t.pro_CardTasksText = new Color(0.85f, 0.72f, 0.62f);
            t.personal_CardTasksText = new Color(0.38f, 0.22f, 0.12f);
            t.pro_CardCategoryTag = new Color(0.95f, 0.75f, 0.58f);
            t.personal_CardCategoryTag = new Color(0.50f, 0.22f, 0.08f);

            t.pro_AssigneeAvatarBg = new Color(0.22f, 0.14f, 0.16f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.98f, 0.92f, 0.90f, 1.00f);

            t.pro_StatusOverdue = new Color(1.00f, 0.35f, 0.25f);
            t.personal_StatusOverdue = new Color(0.88f, 0.22f, 0.15f);
            t.pro_StatusDueToday = new Color(1.00f, 0.60f, 0.15f);
            t.personal_StatusDueToday = new Color(0.90f, 0.45f, 0.05f);
            t.pro_StatusDueSoon = new Color(0.98f, 0.82f, 0.25f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.65f, 0.10f);
            t.pro_StatusCompleted = new Color(0.45f, 0.88f, 0.45f);
            t.personal_StatusCompleted = new Color(0.18f, 0.65f, 0.25f);
            t.pro_TasksCompletedCount = new Color(0.45f, 0.88f, 0.45f);
            t.personal_TasksCompletedCount = new Color(0.18f, 0.65f, 0.25f);

            t.pro_ChecklistTickBg = new Color(0.16f, 0.10f, 0.08f);
            t.personal_ChecklistTickBg = new Color(0.98f, 0.93f, 0.88f);
            t.pro_ChecklistTickCheckedBg = new Color(0.90f, 0.45f, 0.15f);
            t.personal_ChecklistTickCheckedBg = new Color(0.90f, 0.45f, 0.15f);
            t.pro_ChecklistTickBorder = new Color(0.75f, 0.38f, 0.20f, 0.85f);
            t.personal_ChecklistTickBorder = new Color(0.85f, 0.55f, 0.35f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Heavy;

            t.tabActive = new Color(0.9f, 0.45f, 0.15f);
            t.noteSelectedAccent = new Color(0.95f, 0.6f, 0.15f);
            t.linkColor = new Color(0.95f, 0.45f, 0.2f);
            return t;
        }

        public static ThemeData CreateMonochrome()
        {
            var t = CreateDefault();
            t.name = "Monochrome Minimal";
            t.priorityIcons = new List<string> { "", "▫️", "◽", "◻️", "⬛" };
            t.boardTabIcon = "▤";
            t.notesTabIcon = "▥";
            t.styleTabIcon = "▦";
            t.boardHeaderIcon = "◼";
            t.notesHeaderIcon = "▥";
            t.categoryIcon = "▫";
            t.assigneeIcon = "▪";
            t.priorityFilterIcon = "◼";
            t.parentLinkIcon = "◻";
            t.childLinkIcon = "▫";
            t.pinnedNoteIcon = "▪";
            t.completedIcon = "▪";
            t.overdueIcon = "▫";
            t.dueTodayIcon = "▪";
            t.dueSoonIcon = "▫";
            t.dueDateIcon = "▫";
            t.archiveIcon = "▤";
            t.unarchiveIcon = "▥";
            t.cardDetailIcon = "▪";
            t.newCardIcon = "▫";
            t.checklistIcon = "▪";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "✕";
            t.saveIcon = "▤";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";
            t.pro_BoardHeader = Color.white;
            t.personal_BoardHeader = Color.black;
            t.pro_ColumnHeader = Color.white;
            t.personal_ColumnHeader = Color.black;
            t.pro_CardTitle = Color.white;
            t.personal_CardTitle = Color.black;
            t.pro_CardText = new Color(0.85f, 0.85f, 0.85f);
            t.personal_CardText = new Color(0.20f, 0.20f, 0.20f);
            t.pro_SectionLabel = Color.white;
            t.personal_SectionLabel = Color.black;
            t.pro_ColumnBg = new Color(0.18f, 0.18f, 0.18f);
            t.pro_ColumnBgAlt = new Color(0.22f, 0.22f, 0.22f);
            t.personal_ColumnBg = new Color(0.88f, 0.88f, 0.88f);
            t.personal_ColumnBgAlt = new Color(0.92f, 0.92f, 0.92f);
            t.pro_CardBg = new Color(0.18f, 0.18f, 0.18f);
            t.personal_CardBg = new Color(0.94f, 0.94f, 0.94f);
            t.pro_CardHighlighted = new Color(0.32f, 0.32f, 0.32f);
            t.personal_CardHighlighted = new Color(0.72f, 0.72f, 0.72f);
            t.pro_BoardBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_BoardBg = new Color(0.84f, 0.84f, 0.84f);
            t.pro_TopBarBg = new Color(0.09f, 0.09f, 0.09f);
            t.personal_TopBarBg = new Color(0.78f, 0.78f, 0.78f);
            t.pro_StatusBarBg = new Color(0.09f, 0.09f, 0.09f);
            t.personal_StatusBarBg = new Color(0.78f, 0.78f, 0.78f);
            t.pro_StatusBarText = new Color(0.75f, 0.75f, 0.75f);
            t.personal_StatusBarText = new Color(0.35f, 0.35f, 0.35f);
            t.pro_NoteSidebarBg = new Color(0.14f, 0.14f, 0.14f);
            t.personal_NoteSidebarBg = new Color(0.86f, 0.86f, 0.86f);
            t.pro_NoteEditorBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_NoteEditorBg = new Color(0.84f, 0.84f, 0.84f);
            t.pro_NotePopoutBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_NotePopoutBg = new Color(0.84f, 0.84f, 0.84f);
            t.pro_NoteInputBg = new Color(0.09f, 0.09f, 0.09f);
            t.personal_NoteInputBg = new Color(0.96f, 0.96f, 0.96f);
            t.pro_NoteInputText = Color.white;
            t.personal_NoteInputText = Color.black;
            t.pro_NoteTitle = Color.white;
            t.personal_NoteTitle = Color.black;
            t.pro_CardDetailBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_CardDetailBg = new Color(0.84f, 0.84f, 0.84f);
            t.pro_ButtonBg = new Color(0.22f, 0.22f, 0.22f);
            t.personal_ButtonBg = new Color(0.90f, 0.90f, 0.90f);
            t.pro_ButtonText = Color.white;
            t.personal_ButtonText = Color.black;
            t.pro_ButtonHoverBg = new Color(0.30f, 0.30f, 0.30f);
            t.personal_ButtonHoverBg = new Color(0.96f, 0.96f, 0.96f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.black;
            t.pro_DropdownBg = new Color(0.18f, 0.18f, 0.18f);
            t.personal_DropdownBg = new Color(0.92f, 0.92f, 0.92f);
            t.pro_DropdownText = Color.white;
            t.personal_DropdownText = Color.black;
            t.pro_DropdownHoverBg = new Color(0.26f, 0.26f, 0.26f);
            t.personal_DropdownHoverBg = new Color(0.97f, 0.97f, 0.97f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_DropdownMenuBg = new Color(0.10f, 0.10f, 0.10f);
            t.personal_DropdownMenuBg = new Color(0.92f, 0.92f, 0.92f);
            t.pro_DropdownMenuText = Color.white;
            t.personal_DropdownMenuText = Color.black;
            t.pro_DropdownMenuHoverBg = new Color(0.25f, 0.25f, 0.25f);
            t.personal_DropdownMenuHoverBg = new Color(0.80f, 0.80f, 0.80f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownHoverText = Color.black;
            t.pro_PopupBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_PopupBg = new Color(0.88f, 0.88f, 0.88f);
            t.pro_DeleteBtnBg = new Color(0.40f, 0.15f, 0.15f);
            t.personal_DeleteBtnBg = new Color(0.80f, 0.25f, 0.25f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.55f, 0.20f, 0.20f);
            t.personal_DeleteBtnHoverBg = new Color(0.90f, 0.35f, 0.35f);
            t.pro_HeaderTabActiveBg = new Color(0.42f, 0.42f, 0.42f);
            t.personal_HeaderTabActiveBg = new Color(0.35f, 0.35f, 0.35f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.18f, 0.18f, 0.18f);
            t.personal_HeaderTabInactiveBg = new Color(0.88f, 0.88f, 0.88f);
            t.pro_HeaderTabInactiveText = new Color(0.80f, 0.80f, 0.80f);
            t.personal_HeaderTabInactiveText = new Color(0.20f, 0.20f, 0.20f);
            t.pro_HeaderTabHoverBg = new Color(0.26f, 0.26f, 0.26f);
            t.personal_HeaderTabHoverBg = new Color(0.94f, 0.94f, 0.94f);
            t.pro_AddCardBg = new Color(0.25f, 0.25f, 0.25f);
            t.personal_AddCardBg = new Color(0.86f, 0.86f, 0.86f);
            t.pro_AddCardText = Color.white;
            t.personal_AddCardText = Color.black;
            t.pro_AddCardHoverBg = new Color(0.35f, 0.35f, 0.35f);
            t.personal_AddCardHoverBg = new Color(0.94f, 0.94f, 0.94f);
            t.pro_NoteCardBg = new Color(0.18f, 0.18f, 0.18f);
            t.personal_NoteCardBg = new Color(0.90f, 0.90f, 0.90f);
            t.pro_NoteCardSelectedBg = new Color(0.32f, 0.32f, 0.32f);
            t.personal_NoteCardSelectedBg = new Color(0.72f, 0.72f, 0.72f);
            t.pro_NoteCardHoverBg = new Color(0.24f, 0.24f, 0.24f);
            t.personal_NoteCardHoverBg = new Color(0.95f, 0.95f, 0.95f);
            t.pro_NoteActionBg = new Color(0.32f, 0.32f, 0.32f);
            t.personal_NoteActionBg = new Color(0.85f, 0.85f, 0.85f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = Color.black;
            t.pro_NoteActionHoverBg = new Color(0.42f, 0.42f, 0.42f);
            t.personal_NoteActionHoverBg = new Color(0.92f, 0.92f, 0.92f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = Color.black;
            t.pro_TooltipBg = new Color(0.10f, 0.10f, 0.10f, 0.96f);
            t.personal_TooltipBg = new Color(0.95f, 0.95f, 0.95f, 0.96f);
            t.pro_TooltipText = Color.white;
            t.personal_TooltipText = Color.black;
            t.pro_TooltipBorder = new Color(0.40f, 0.40f, 0.40f, 0.8f);
            t.personal_TooltipBorder = new Color(0.70f, 0.70f, 0.70f, 0.8f);

            t.pro_AddNoteBg = new Color(0.32f, 0.32f, 0.32f);
            t.personal_AddNoteBg = new Color(0.85f, 0.85f, 0.85f);
            t.pro_AddNoteText = Color.white;
            t.personal_AddNoteText = Color.black;
            t.pro_AddNoteHoverBg = new Color(0.42f, 0.42f, 0.42f);
            t.personal_AddNoteHoverBg = new Color(0.92f, 0.92f, 0.92f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = Color.black;

            t.pro_ImportNoteBg = new Color(0.26f, 0.26f, 0.26f);
            t.personal_ImportNoteBg = new Color(0.88f, 0.88f, 0.88f);
            t.pro_ImportNoteText = new Color(0.90f, 0.90f, 0.90f);
            t.personal_ImportNoteText = new Color(0.15f, 0.15f, 0.15f);
            t.pro_ImportNoteHoverBg = new Color(0.35f, 0.35f, 0.35f);
            t.personal_ImportNoteHoverBg = new Color(0.94f, 0.94f, 0.94f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = Color.black;

            t.pro_NoteFolderText = new Color(0.85f, 0.85f, 0.85f);
            t.personal_NoteFolderText = new Color(0.20f, 0.20f, 0.20f);

            t.pro_CardDetailsText = new Color(0.75f, 0.75f, 0.75f);
            t.personal_CardDetailsText = new Color(0.35f, 0.35f, 0.35f);
            t.pro_CardTasksText = new Color(0.70f, 0.70f, 0.70f);
            t.personal_CardTasksText = new Color(0.30f, 0.30f, 0.30f);
            t.pro_CardCategoryTag = new Color(0.80f, 0.80f, 0.80f);
            t.personal_CardCategoryTag = new Color(0.25f, 0.25f, 0.25f);

            t.pro_AssigneeAvatarBg = new Color(0.18f, 0.18f, 0.18f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.94f, 0.94f, 0.94f, 1.00f);

            t.pro_StatusOverdue = new Color(1.00f, 0.40f, 0.40f);
            t.personal_StatusOverdue = new Color(0.80f, 0.20f, 0.20f);
            t.pro_StatusDueToday = new Color(1.00f, 0.70f, 0.20f);
            t.personal_StatusDueToday = new Color(0.85f, 0.50f, 0.10f);
            t.pro_StatusDueSoon = new Color(0.90f, 0.90f, 0.30f);
            t.personal_StatusDueSoon = new Color(0.70f, 0.70f, 0.15f);
            t.pro_StatusCompleted = new Color(0.50f, 0.90f, 0.50f);
            t.personal_StatusCompleted = new Color(0.20f, 0.65f, 0.25f);
            t.pro_TasksCompletedCount = new Color(0.50f, 0.90f, 0.50f);
            t.personal_TasksCompletedCount = new Color(0.20f, 0.65f, 0.25f);

            t.pro_ChecklistTickBg = new Color(0.12f, 0.12f, 0.12f);
            t.personal_ChecklistTickBg = new Color(0.96f, 0.96f, 0.96f);
            t.pro_ChecklistTickCheckedBg = new Color(0.90f, 0.90f, 0.90f);
            t.personal_ChecklistTickCheckedBg = new Color(0.15f, 0.15f, 0.15f);
            t.pro_ChecklistTickBorder = new Color(0.45f, 0.45f, 0.45f);
            t.personal_ChecklistTickBorder = new Color(0.35f, 0.35f, 0.35f);
            t.pro_ChecklistTickColor = new Color(0.10f, 0.10f, 0.10f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Square;

            t.tabActive = new Color(0.42f, 0.42f, 0.42f);
            t.noteSelectedAccent = new Color(0.8f, 0.8f, 0.8f);
            t.linkColor = new Color(0.7f, 0.7f, 0.7f);
            return t;
        }

        public static ThemeData CreateRetro()
        {
            var t = CreateDefault();
            t.name = "Retro Synthwave";
            t.priorityIcons = new List<string> { "", "🔹", "🔸", "⚡", "🔥" };
            t.boardTabIcon = "🕹️";
            t.notesTabIcon = "💾";
            t.styleTabIcon = "📼";
            t.boardHeaderIcon = "👾";
            t.notesHeaderIcon = "📟";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👥";
            t.priorityFilterIcon = "🚩";
            t.parentLinkIcon = "🌴";
            t.childLinkIcon = "🌵";
            t.pinnedNoteIcon = "📌";
            t.completedIcon = "⭐";
            t.overdueIcon = "💀";
            t.dueTodayIcon = "⚡";
            t.dueSoonIcon = "⌛";
            t.dueDateIcon = "📅";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "💾";
            t.cardDetailIcon = "🕹️";
            t.newCardIcon = "✨";
            t.checklistIcon = "👾";
            t.attachmentIcon = "📎";
            t.urlIcon = "🌐";
            t.deleteIcon = "💥";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.labelColors = new List<Color>
            {
                new Color(0.50f, 0.45f, 0.60f, 1.0f),  // 0 Grey
                new Color(0.00f, 0.95f, 0.55f, 1.0f),  // 1 Neon Green
                new Color(0.00f, 0.85f, 1.00f, 1.0f),  // 2 Electric Cyan
                new Color(1.00f, 0.90f, 0.10f, 1.0f),  // 3 Arcade Yellow
                new Color(1.00f, 0.50f, 0.10f, 1.0f),  // 4 Neon Orange
                new Color(1.00f, 0.15f, 0.45f, 1.0f),  // 5 Laser Magenta
                new Color(0.75f, 0.20f, 0.95f, 1.0f),  // 6 Neon Violet
                new Color(0.00f, 0.90f, 0.80f, 1.0f),  // 7 Miami Teal
                new Color(1.00f, 0.30f, 0.75f, 1.0f),  // 8 Synth Pink
                new Color(0.70f, 0.95f, 0.15f, 1.0f),  // 9 Acid Lime
                new Color(0.40f, 0.30f, 0.95f, 1.0f),  // 10 Deep Indigo
                new Color(0.10f, 0.95f, 0.95f, 1.0f),  // 11 Bright Cyan
                new Color(1.00f, 0.75f, 0.05f, 1.0f),  // 12 Sunset Amber
                new Color(1.00f, 0.35f, 0.20f, 1.0f),  // 13 Sunset Coral
                new Color(0.60f, 0.15f, 0.90f, 1.0f),  // 14 Cyber Violet
                new Color(0.35f, 0.45f, 0.65f, 1.0f),  // 15 Retro Denim
                new Color(0.60f, 0.35f, 0.25f, 1.0f)   // 16 Sepia Rust
            };

            // Pro Skin (Dark Mode) - 80s Synthwave / Outrun Arcade
            t.pro_BoardHeader = new Color(1.00f, 0.30f, 0.80f, 1.00f);
            t.personal_BoardHeader = new Color(0.60f, 0.10f, 0.40f, 1.00f);
            t.pro_ColumnHeader = new Color(0.20f, 0.90f, 1.00f, 1.00f);
            t.personal_ColumnHeader = new Color(0.10f, 0.40f, 0.50f, 1.00f);
            t.pro_CardTitle = new Color(1.00f, 0.95f, 0.98f, 1.00f);
            t.personal_CardTitle = new Color(0.20f, 0.15f, 0.25f, 1.00f);
            t.pro_CardText = new Color(0.85f, 0.80f, 0.95f, 1.00f);
            t.personal_CardText = new Color(0.35f, 0.30f, 0.40f, 1.00f);
            t.pro_SectionLabel = new Color(1.00f, 0.40f, 0.85f, 1.00f);
            t.personal_SectionLabel = new Color(0.55f, 0.15f, 0.45f, 1.00f);

            t.pro_ColumnBg = new Color(0.16f, 0.09f, 0.28f, 0.85f);
            t.pro_ColumnBgAlt = new Color(0.19f, 0.11f, 0.32f, 0.85f);
            t.personal_ColumnBg = new Color(0.90f, 0.86f, 0.80f, 0.90f);
            t.personal_ColumnBgAlt = new Color(0.93f, 0.89f, 0.84f, 0.90f);

            t.pro_CardBg = new Color(0.22f, 0.13f, 0.36f, 0.90f);
            t.personal_CardBg = new Color(0.98f, 0.96f, 0.92f, 0.95f);

            t.pro_CardHighlighted = new Color(0.85f, 0.15f, 0.65f, 0.40f);
            t.personal_CardHighlighted = new Color(1.00f, 0.70f, 0.85f, 0.60f);

            t.pro_BoardBg = new Color(0.10f, 0.05f, 0.18f, 0.95f);
            t.personal_BoardBg = new Color(0.96f, 0.93f, 0.88f, 1.00f);

            t.pro_TopBarBg = new Color(0.08f, 0.03f, 0.14f, 0.95f);
            t.personal_TopBarBg = new Color(0.92f, 0.88f, 0.82f, 1.00f);
            t.pro_StatusBarBg = new Color(0.08f, 0.03f, 0.14f, 0.95f);
            t.personal_StatusBarBg = new Color(0.92f, 0.88f, 0.82f, 1.00f);
            t.pro_StatusBarText = new Color(0.80f, 0.75f, 0.92f, 1.00f);
            t.personal_StatusBarText = new Color(0.38f, 0.30f, 0.45f, 1.00f);

            t.pro_NoteSidebarBg = new Color(0.12f, 0.06f, 0.22f, 0.90f);
            t.personal_NoteSidebarBg = new Color(0.88f, 0.84f, 0.78f, 0.90f);
            t.pro_NoteEditorBg = new Color(0.15f, 0.08f, 0.26f, 0.90f);
            t.personal_NoteEditorBg = new Color(0.92f, 0.88f, 0.83f, 0.90f);
            t.pro_NotePopoutBg = new Color(0.12f, 0.06f, 0.22f, 0.98f);
            t.personal_NotePopoutBg = new Color(0.95f, 0.92f, 0.87f, 0.98f);
            t.pro_NoteInputBg = new Color(0.10f, 0.05f, 0.18f, 0.90f);
            t.personal_NoteInputBg = new Color(0.98f, 0.96f, 0.92f, 0.95f);
            t.pro_NoteInputText = new Color(0.95f, 0.92f, 1.00f, 1.00f);
            t.personal_NoteInputText = new Color(0.20f, 0.15f, 0.25f, 1.00f);
            t.pro_NoteTitle = new Color(1.00f, 0.40f, 0.85f, 1.00f);
            t.personal_NoteTitle = new Color(0.60f, 0.10f, 0.40f, 1.00f);
            t.pro_NoteCardBg = new Color(0.18f, 0.10f, 0.30f, 0.80f);
            t.personal_NoteCardBg = new Color(0.92f, 0.88f, 0.84f, 0.80f);
            t.pro_NoteCardSelectedBg = new Color(0.75f, 0.15f, 0.55f, 0.60f);
            t.personal_NoteCardSelectedBg = new Color(0.95f, 0.50f, 0.70f, 0.50f);
            t.pro_NoteCardHoverBg = new Color(0.26f, 0.14f, 0.42f, 0.85f);
            t.personal_NoteCardHoverBg = new Color(0.95f, 0.91f, 0.87f, 0.85f);
            t.pro_NoteActionBg = new Color(0.55f, 0.12f, 0.65f, 0.90f);
            t.personal_NoteActionBg = new Color(0.85f, 0.40f, 0.65f, 0.90f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = Color.white;
            t.pro_NoteActionHoverBg = new Color(0.70f, 0.18f, 0.80f, 1.00f);
            t.personal_NoteActionHoverBg = new Color(0.92f, 0.50f, 0.75f, 1.00f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = Color.white;
            t.pro_CardDetailBg = new Color(0.14f, 0.07f, 0.24f, 0.98f);
            t.personal_CardDetailBg = new Color(0.95f, 0.92f, 0.87f, 0.98f);

            t.pro_ButtonBg = new Color(0.25f, 0.14f, 0.40f, 0.90f);
            t.personal_ButtonBg = new Color(0.88f, 0.84f, 0.78f, 0.95f);
            t.pro_ButtonText = new Color(0.95f, 0.90f, 1.00f, 1.00f);
            t.personal_ButtonText = new Color(0.25f, 0.20f, 0.30f, 1.00f);
            t.pro_ButtonHoverBg = new Color(0.38f, 0.20f, 0.58f, 1.00f);
            t.personal_ButtonHoverBg = new Color(0.94f, 0.90f, 0.86f, 1.00f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = new Color(0.15f, 0.10f, 0.20f, 1.00f);

            t.pro_DropdownBg = new Color(0.20f, 0.10f, 0.34f, 0.90f);
            t.personal_DropdownBg = new Color(0.88f, 0.84f, 0.78f, 0.95f);
            t.pro_DropdownText = new Color(0.95f, 0.90f, 1.00f, 1.00f);
            t.personal_DropdownText = new Color(0.25f, 0.20f, 0.30f, 1.00f);
            t.pro_DropdownHoverBg = new Color(0.32f, 0.16f, 0.50f, 1.00f);
            t.personal_DropdownHoverBg = new Color(0.94f, 0.90f, 0.86f, 1.00f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.15f, 0.10f, 0.20f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.12f, 0.06f, 0.22f, 0.98f);
            t.personal_DropdownMenuBg = new Color(0.95f, 0.92f, 0.87f, 0.98f);
            t.pro_DropdownMenuText = new Color(0.92f, 0.88f, 1.00f, 1.00f);
            t.personal_DropdownMenuText = new Color(0.20f, 0.15f, 0.25f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.70f, 0.15f, 0.55f, 0.90f);
            t.personal_DropdownMenuHoverBg = new Color(0.90f, 0.45f, 0.65f, 0.85f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = Color.white;

            t.pro_PopupBg = new Color(0.12f, 0.06f, 0.22f, 0.98f);
            t.personal_PopupBg = new Color(0.95f, 0.92f, 0.87f, 0.98f);

            t.pro_DeleteBtnBg = new Color(0.65f, 0.08f, 0.25f, 0.90f);
            t.personal_DeleteBtnBg = new Color(0.85f, 0.25f, 0.35f, 0.90f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.85f, 0.12f, 0.35f, 1.00f);
            t.personal_DeleteBtnHoverBg = new Color(0.95f, 0.35f, 0.45f, 1.00f);

            t.pro_HeaderTabActiveBg = new Color(0.90f, 0.15f, 0.60f, 1.00f);
            t.personal_HeaderTabActiveBg = new Color(0.95f, 0.30f, 0.55f, 1.00f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.16f, 0.08f, 0.26f, 0.85f);
            t.personal_HeaderTabInactiveBg = new Color(0.88f, 0.84f, 0.78f, 0.90f);
            t.pro_HeaderTabInactiveText = new Color(0.75f, 0.70f, 0.90f, 1.00f);
            t.personal_HeaderTabInactiveText = new Color(0.35f, 0.30f, 0.40f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(0.28f, 0.15f, 0.45f, 0.90f);
            t.personal_HeaderTabHoverBg = new Color(0.92f, 0.88f, 0.84f, 0.95f);

            t.pro_AddCardBg = new Color(0.08f, 0.55f, 0.45f, 0.90f);
            t.personal_AddCardBg = new Color(0.65f, 0.88f, 0.75f, 0.90f);
            t.pro_AddCardText = new Color(0.40f, 1.00f, 0.85f, 1.00f);
            t.personal_AddCardText = new Color(0.08f, 0.35f, 0.20f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.12f, 0.70f, 0.58f, 1.00f);
            t.personal_AddCardHoverBg = new Color(0.75f, 0.94f, 0.82f, 1.00f);

            t.pro_TooltipBg = new Color(0.12f, 0.05f, 0.18f, 0.96f);
            t.personal_TooltipBg = new Color(0.97f, 0.91f, 0.98f, 0.96f);
            t.pro_TooltipText = new Color(1.00f, 0.92f, 0.20f);
            t.personal_TooltipText = new Color(0.35f, 0.05f, 0.40f);
            t.pro_TooltipBorder = new Color(1.00f, 0.15f, 0.65f, 0.9f);
            t.personal_TooltipBorder = new Color(0.85f, 0.15f, 0.55f, 0.85f);

            t.pro_AddNoteBg = new Color(0.65f, 0.10f, 0.50f, 0.90f);
            t.personal_AddNoteBg = new Color(0.90f, 0.45f, 0.68f, 0.90f);
            t.pro_AddNoteText = Color.white;
            t.personal_AddNoteText = Color.white;
            t.pro_AddNoteHoverBg = new Color(0.80f, 0.15f, 0.65f, 1.00f);
            t.personal_AddNoteHoverBg = new Color(0.95f, 0.55f, 0.75f, 1.00f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = Color.white;

            t.pro_ImportNoteBg = new Color(0.10f, 0.50f, 0.65f, 0.90f);
            t.personal_ImportNoteBg = new Color(0.60f, 0.82f, 0.88f, 0.90f);
            t.pro_ImportNoteText = new Color(0.60f, 1.00f, 1.00f, 1.00f);
            t.personal_ImportNoteText = new Color(0.10f, 0.30f, 0.38f, 1.00f);
            t.pro_ImportNoteHoverBg = new Color(0.15f, 0.65f, 0.80f, 1.00f);
            t.personal_ImportNoteHoverBg = new Color(0.72f, 0.90f, 0.94f, 1.00f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.05f, 0.20f, 0.28f, 1.00f);

            t.pro_NoteFolderText = new Color(0.85f, 0.80f, 0.95f, 1.00f);
            t.personal_NoteFolderText = new Color(0.35f, 0.25f, 0.40f, 1.00f);

            t.pro_CardDetailsText = new Color(0.80f, 0.75f, 0.92f, 1.00f);
            t.personal_CardDetailsText = new Color(0.38f, 0.30f, 0.45f, 1.00f);
            t.pro_CardTasksText = new Color(0.20f, 0.90f, 1.00f, 0.90f);
            t.personal_CardTasksText = new Color(0.15f, 0.45f, 0.55f, 1.00f);
            t.pro_CardCategoryTag = new Color(1.00f, 0.40f, 0.85f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.60f, 0.15f, 0.45f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.20f, 0.16f, 0.24f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.97f, 0.92f, 0.96f, 1.00f);

            t.pro_StatusOverdue = new Color(1.00f, 0.15f, 0.45f, 1.00f);
            t.personal_StatusOverdue = new Color(0.88f, 0.15f, 0.35f, 1.00f);
            t.pro_StatusDueToday = new Color(1.00f, 0.55f, 0.10f, 1.00f);
            t.personal_StatusDueToday = new Color(0.90f, 0.45f, 0.08f, 1.00f);
            t.pro_StatusDueSoon = new Color(1.00f, 0.90f, 0.10f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.70f, 0.05f, 1.00f);
            t.pro_StatusCompleted = new Color(0.00f, 0.95f, 0.55f, 1.00f);
            t.personal_StatusCompleted = new Color(0.05f, 0.70f, 0.40f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.00f, 0.95f, 0.55f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.05f, 0.70f, 0.40f, 1.00f);

            t.pro_ChecklistTickBg = new Color(0.12f, 0.06f, 0.22f);
            t.personal_ChecklistTickBg = new Color(0.95f, 0.90f, 0.96f);
            t.pro_ChecklistTickCheckedBg = new Color(0.90f, 0.15f, 0.60f);
            t.personal_ChecklistTickCheckedBg = new Color(0.90f, 0.20f, 0.60f);
            t.pro_ChecklistTickBorder = new Color(0.00f, 0.85f, 1.00f, 0.80f);
            t.personal_ChecklistTickBorder = new Color(0.80f, 0.20f, 0.60f, 0.75f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Cross;

            t.tabActive = new Color(0.90f, 0.15f, 0.60f, 1.00f);
            t.noteSelectedAccent = new Color(0.00f, 0.85f, 1.00f, 1.00f);
            t.linkColor = new Color(0.00f, 0.85f, 1.00f, 1.00f);

            return t;
        }

        public static ThemeData CreateVintage8Bit()
        {
            var t = CreateDefault();
            t.name = "Vintage 8-Bit";
            t.priorityIcons = new List<string> { "", "🪙", "⭐", "⚡", "👑" };
            t.boardTabIcon = "🎮";
            t.notesTabIcon = "📜";
            t.styleTabIcon = "🎨";
            t.boardHeaderIcon = "🎮";
            t.notesHeaderIcon = "📜";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👥";
            t.priorityFilterIcon = "🚩";
            t.parentLinkIcon = "🌴";
            t.childLinkIcon = "🌵";
            t.pinnedNoteIcon = "📌";
            t.completedIcon = "🏁";
            t.overdueIcon = "💀";
            t.dueTodayIcon = "⏰";
            t.dueSoonIcon = "⌛";
            t.dueDateIcon = "📅";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "💾";
            t.cardDetailIcon = "🔍";
            t.newCardIcon = "➕";
            t.checklistIcon = "☑";
            t.attachmentIcon = "📎";
            t.urlIcon = "🌐";
            t.deleteIcon = "💥";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.labelColors = new List<Color>
            {
                new Color(0.804f, 0.796f, 0.784f, 1.0f),  // 0 Main Grey (CDCBC8)
                new Color(0.00f, 0.65f, 0.15f, 1.0f),  // 1 8-Bit Pipe Green
                new Color(0.20f, 0.60f, 1.00f, 1.0f),  // 2 8-Bit Sky Blue
                new Color(0.98f, 0.75f, 0.05f, 1.0f),  // 3 8-Bit Coin Yellow
                new Color(0.95f, 0.45f, 0.10f, 1.0f),  // 4 8-Bit Fire Orange
                new Color(0.776f, 0.259f, 0.267f, 1.0f),  // 5 Main Red (C64244)
                new Color(0.55f, 0.25f, 0.90f, 1.0f),  // 6 8-Bit Magic Violet
                new Color(0.00f, 0.70f, 0.70f, 1.0f),  // 7 8-Bit Sea Teal
                new Color(0.95f, 0.45f, 0.75f, 1.0f),  // 8 8-Bit Princess Pink
                new Color(0.70f, 0.85f, 0.10f, 1.0f),  // 9 8-Bit Lime
                new Color(0.10f, 0.35f, 0.85f, 1.0f),  // 10 8-Bit Cobalt Blue
                new Color(0.20f, 0.85f, 0.85f, 1.0f),  // 11 8-Bit Mint Cyan
                new Color(0.85f, 0.65f, 0.20f, 1.0f),  // 12 8-Bit Gold Cartridge
                new Color(0.80f, 0.30f, 0.10f, 1.0f),  // 13 8-Bit Brick Rust
                new Color(0.40f, 0.20f, 0.70f, 1.0f),  // 14 8-Bit Dark Purple
                new Color(0.451f, 0.439f, 0.443f, 1.0f),  // 15 Second Grey (737071)
                new Color(0.318f, 0.318f, 0.314f, 1.0f)   // 16 Darker Grey (515150)
            };

            // Palette codes:
            // Main Grey: #CDCBC8 (0.804, 0.796, 0.784)
            // Second Grey: #737071 (0.451, 0.439, 0.443)
            // Darker Grey: #515150 (0.318, 0.318, 0.314)
            // Main Red: #C64244 (0.776, 0.259, 0.267)
            // Highlight Red: #CF0207 (0.812, 0.008, 0.027)
            // Black: #0D0E0E (0.051, 0.055, 0.055)

            // Pro Skin (Dark Mode) - NES Chassis, Slate & Hardware Red
            t.pro_BoardHeader = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.personal_BoardHeader = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.pro_ColumnHeader = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_ColumnHeader = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_CardTitle = new Color(0.95f, 0.95f, 0.95f, 1.00f);
            t.personal_CardTitle = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_CardText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_CardText = new Color(0.318f, 0.318f, 0.314f, 1.00f);
            t.pro_SectionLabel = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.personal_SectionLabel = new Color(0.776f, 0.259f, 0.267f, 1.00f);

            t.pro_ColumnBg = new Color(0.318f, 0.318f, 0.314f, 0.94f);
            t.pro_ColumnBgAlt = new Color(0.355f, 0.355f, 0.350f, 0.94f);
            t.personal_ColumnBg = new Color(0.880f, 0.880f, 0.870f, 0.92f);
            t.personal_ColumnBgAlt = new Color(0.915f, 0.915f, 0.910f, 0.92f);

            t.pro_CardBg = new Color(0.051f, 0.055f, 0.055f, 0.92f);
            t.personal_CardBg = new Color(0.970f, 0.970f, 0.960f, 0.98f);

            t.pro_CardHighlighted = new Color(0.812f, 0.008f, 0.027f, 0.35f);
            t.personal_CardHighlighted = new Color(0.812f, 0.008f, 0.027f, 0.20f);

            t.pro_BoardBg = new Color(0.451f, 0.439f, 0.443f, 1.00f);
            t.personal_BoardBg = new Color(0.804f, 0.796f, 0.784f, 1.00f);

            t.pro_TopBarBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_TopBarBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.pro_StatusBarBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_StatusBarBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.pro_StatusBarText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_StatusBarText = new Color(0.804f, 0.796f, 0.784f, 1.00f);

            t.pro_NoteSidebarBg = new Color(0.318f, 0.318f, 0.314f, 0.92f);
            t.personal_NoteSidebarBg = new Color(0.860f, 0.860f, 0.850f, 0.90f);
            t.pro_NoteEditorBg = new Color(0.451f, 0.439f, 0.443f, 0.92f);
            t.personal_NoteEditorBg = new Color(0.930f, 0.930f, 0.920f, 0.90f);
            t.pro_NotePopoutBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_NotePopoutBg = new Color(0.950f, 0.950f, 0.940f, 0.98f);
            t.pro_NoteInputBg = new Color(0.051f, 0.055f, 0.055f, 0.95f);
            t.personal_NoteInputBg = new Color(0.980f, 0.980f, 0.980f, 0.95f);
            t.pro_NoteInputText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_NoteInputText = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_NoteTitle = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.personal_NoteTitle = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.pro_NoteCardBg = new Color(0.318f, 0.318f, 0.314f, 0.85f);
            t.personal_NoteCardBg = new Color(0.890f, 0.890f, 0.880f, 0.85f);
            t.pro_NoteCardSelectedBg = new Color(0.812f, 0.008f, 0.027f, 0.50f);
            t.personal_NoteCardSelectedBg = new Color(0.812f, 0.008f, 0.027f, 0.30f);
            t.pro_NoteCardHoverBg = new Color(0.451f, 0.439f, 0.443f, 0.85f);
            t.personal_NoteCardHoverBg = new Color(0.930f, 0.930f, 0.920f, 0.90f);
            t.pro_NoteActionBg = new Color(0.776f, 0.259f, 0.267f, 0.85f);
            t.personal_NoteActionBg = new Color(0.776f, 0.259f, 0.267f, 0.85f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = Color.white;
            t.pro_NoteActionHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.personal_NoteActionHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = Color.white;
            t.pro_CardDetailBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_CardDetailBg = new Color(0.940f, 0.940f, 0.930f, 0.98f);

            t.pro_ButtonBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.personal_ButtonBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.pro_ButtonText = Color.white;
            t.personal_ButtonText = Color.white;
            t.pro_ButtonHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.personal_ButtonHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = Color.white;

            t.pro_DropdownBg = new Color(0.318f, 0.318f, 0.314f, 0.90f);
            t.personal_DropdownBg = new Color(0.890f, 0.890f, 0.880f, 0.90f);
            t.pro_DropdownText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_DropdownText = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_DropdownHoverBg = new Color(0.451f, 0.439f, 0.443f, 1.00f);
            t.personal_DropdownHoverBg = new Color(0.940f, 0.940f, 0.930f, 1.00f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.051f, 0.055f, 0.055f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_DropdownMenuBg = new Color(0.960f, 0.960f, 0.950f, 0.98f);
            t.pro_DropdownMenuText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_DropdownMenuText = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.776f, 0.259f, 0.267f, 0.60f);
            t.personal_DropdownMenuHoverBg = new Color(0.776f, 0.259f, 0.267f, 0.15f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = new Color(0.776f, 0.259f, 0.267f, 1.00f);

            t.pro_PopupBg = new Color(0.318f, 0.318f, 0.314f, 0.98f);
            t.personal_PopupBg = new Color(0.950f, 0.950f, 0.940f, 0.98f);

            t.pro_DeleteBtnBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.personal_DeleteBtnBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = Color.white;
            t.pro_DeleteBtnHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.personal_DeleteBtnHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);

            t.pro_HeaderTabActiveBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.personal_HeaderTabActiveBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.318f, 0.318f, 0.314f, 0.75f);
            t.personal_HeaderTabInactiveBg = new Color(0.830f, 0.830f, 0.820f, 0.75f);
            t.pro_HeaderTabInactiveText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_HeaderTabInactiveText = new Color(0.318f, 0.318f, 0.314f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(0.451f, 0.439f, 0.443f, 0.85f);
            t.personal_HeaderTabHoverBg = new Color(0.890f, 0.890f, 0.880f, 0.85f);

            t.pro_AddCardBg = new Color(0.82f, 0.01f, 0.04f, 1f);
            t.personal_AddCardBg = new Color(0.776f, 0.259f, 0.267f, 0.15f);
            t.pro_AddCardText = new Color(0.8f, 0.8f, 0.78f);
            t.personal_AddCardText = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.6f, 0.03f, 0.05f, 1f);
            t.personal_AddCardHoverBg = new Color(0.776f, 0.259f, 0.267f, 0.25f);

            t.pro_TooltipBg = new Color(0.051f, 0.055f, 0.055f, 0.98f);
            t.personal_TooltipBg = new Color(0.950f, 0.950f, 0.940f, 0.98f);
            t.pro_TooltipText = new Color(0.804f, 0.796f, 0.784f);
            t.personal_TooltipText = new Color(0.051f, 0.055f, 0.055f);
            t.pro_TooltipBorder = new Color(0.812f, 0.008f, 0.027f, 0.95f);
            t.personal_TooltipBorder = new Color(0.812f, 0.008f, 0.027f, 0.95f);

            t.pro_AddNoteBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.personal_AddNoteBg = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.pro_AddNoteText = Color.white;
            t.personal_AddNoteText = Color.white;
            t.pro_AddNoteHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.personal_AddNoteHoverBg = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = Color.white;

            t.pro_ImportNoteBg = new Color(0.318f, 0.318f, 0.314f, 0.90f);
            t.personal_ImportNoteBg = new Color(0.318f, 0.318f, 0.314f, 0.85f);
            t.pro_ImportNoteText = Color.white;
            t.personal_ImportNoteText = Color.white;
            t.pro_ImportNoteHoverBg = new Color(0.451f, 0.439f, 0.443f, 1.00f);
            t.personal_ImportNoteHoverBg = new Color(0.451f, 0.439f, 0.443f, 0.95f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = Color.white;

            t.pro_NoteFolderText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_NoteFolderText = new Color(0.051f, 0.055f, 0.055f, 1.00f);

            t.pro_CardDetailsText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_CardDetailsText = new Color(0.318f, 0.318f, 0.314f, 1.00f);
            t.pro_CardTasksText = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_CardTasksText = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.pro_CardCategoryTag = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.776f, 0.259f, 0.267f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.051f, 0.055f, 0.055f, 1.00f);
            t.personal_AssigneeAvatarBg = new Color(0.804f, 0.796f, 0.784f, 1.00f);

            t.pro_StatusOverdue = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.personal_StatusOverdue = new Color(0.812f, 0.008f, 0.027f, 1.00f);
            t.pro_StatusDueToday = new Color(0.95f, 0.55f, 0.10f, 1.00f);
            t.personal_StatusDueToday = new Color(0.85f, 0.45f, 0.08f, 1.00f);
            t.pro_StatusDueSoon = new Color(0.98f, 0.80f, 0.10f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.65f, 0.08f, 1.00f);
            t.pro_StatusCompleted = new Color(0.00f, 0.78f, 0.28f, 1.00f);
            t.personal_StatusCompleted = new Color(0.00f, 0.65f, 0.20f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.00f, 0.78f, 0.28f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.00f, 0.65f, 0.20f, 1.00f);

            t.pro_ChecklistTickBg = new Color(0.318f, 0.318f, 0.314f, 1.00f);
            t.personal_ChecklistTickBg = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.pro_ChecklistTickCheckedBg = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.personal_ChecklistTickCheckedBg = new Color(0.776f, 0.259f, 0.267f, 1.00f);
            t.pro_ChecklistTickBorder = new Color(0.451f, 0.439f, 0.443f, 1.00f);
            t.personal_ChecklistTickBorder = new Color(0.451f, 0.439f, 0.443f, 1.00f);
            t.pro_ChecklistTickColor = new Color(0.804f, 0.796f, 0.784f, 1.00f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Square;

            t.tabActive = new Color(0.776f, 0.259f, 0.267f, 0.90f);
            t.noteSelectedAccent = new Color(0.776f, 0.259f, 0.267f, 0.95f);
            t.linkColor = new Color(0.776f, 0.259f, 0.267f, 1.00f);

            return t;
        }

        public static ThemeData CreateFruitCompanyGlassmorphism()
        {
            var t = CreateDefault();
            t.name = "Fruit Company Glassmorphism";
            t.priorityIcons = new List<string> { "", "⚪", "🟢", "🟡", "🔴" };
            t.boardTabIcon = "🪟";
            t.notesTabIcon = "📝";
            t.styleTabIcon = "✨";
            t.boardHeaderIcon = "🎯";
            t.notesHeaderIcon = "📝";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👤";
            t.priorityFilterIcon = "🚩";
            t.parentLinkIcon = "🌳";
            t.childLinkIcon = "🌿";
            t.pinnedNoteIcon = "📌";
            t.completedIcon = "✅";
            t.overdueIcon = "🔴";
            t.dueTodayIcon = "🟠";
            t.dueSoonIcon = "🟡";
            t.dueDateIcon = "📅";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "🗃️";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "✨";
            t.checklistIcon = "☑";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.labelColors = new List<Color>
            {
                new Color(0.55f, 0.55f, 0.58f, 0.85f),
                new Color(1.00f, 0.23f, 0.19f, 0.85f),
                new Color(1.00f, 0.58f, 0.00f, 0.85f),
                new Color(1.00f, 0.80f, 0.00f, 0.85f),
                new Color(0.20f, 0.78f, 0.35f, 0.85f),
                new Color(0.00f, 0.78f, 0.75f, 0.85f),
                new Color(0.19f, 0.69f, 0.78f, 0.85f),
                new Color(0.20f, 0.68f, 0.90f, 0.85f),
                new Color(0.00f, 0.48f, 1.00f, 0.85f),
                new Color(0.35f, 0.34f, 0.84f, 0.85f),
                new Color(0.69f, 0.32f, 0.87f, 0.85f),
                new Color(1.00f, 0.18f, 0.57f, 0.85f),
                new Color(0.64f, 0.52f, 0.37f, 0.85f),
                new Color(1.00f, 0.43f, 0.38f, 0.85f),
                new Color(0.75f, 0.60f, 0.90f, 0.85f),
                new Color(0.60f, 0.85f, 0.25f, 0.85f),
                new Color(0.95f, 0.40f, 0.60f, 0.85f)
            };

            t.pro_BoardHeader = Color.white;
            t.personal_BoardHeader = new Color(0.10f, 0.10f, 0.12f, 1.00f);
            t.pro_ColumnHeader = new Color(0.95f, 0.95f, 0.98f, 1.00f);
            t.personal_ColumnHeader = new Color(0.12f, 0.12f, 0.16f, 1.00f);
            t.pro_CardTitle = Color.white;
            t.personal_CardTitle = new Color(0.08f, 0.08f, 0.10f, 1.00f);
            t.pro_CardText = new Color(0.82f, 0.86f, 0.92f, 1.00f);
            t.personal_CardText = new Color(0.25f, 0.28f, 0.35f, 1.00f);
            t.pro_SectionLabel = Color.white;
            t.personal_SectionLabel = new Color(0.10f, 0.10f, 0.12f, 1.00f);

            t.pro_BoardBg = new Color(0.07f, 0.08f, 0.11f, 1.00f);
            t.personal_BoardBg = new Color(0.92f, 0.94f, 0.97f, 1.00f);
            t.pro_TopBarBg = new Color(0.12f, 0.14f, 0.18f, 0.75f);
            t.personal_TopBarBg = new Color(1.00f, 1.00f, 1.00f, 0.80f);
            t.pro_StatusBarBg = new Color(0.12f, 0.14f, 0.18f, 0.75f);
            t.personal_StatusBarBg = new Color(1.00f, 1.00f, 1.00f, 0.80f);
            t.pro_StatusBarText = new Color(0.78f, 0.82f, 0.90f, 1.00f);
            t.personal_StatusBarText = new Color(0.35f, 0.38f, 0.45f, 1.00f);

            t.pro_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.pro_ColumnBgAlt = new Color(1.00f, 1.00f, 1.00f, 0.03f);
            t.personal_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.55f);
            t.personal_ColumnBgAlt = new Color(1.00f, 1.00f, 1.00f, 0.38f);

            t.pro_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.10f);
            t.personal_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_CardHighlighted = new Color(0.00f, 0.48f, 1.00f, 0.35f);
            t.personal_CardHighlighted = new Color(0.00f, 0.48f, 1.00f, 0.20f);

            t.pro_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.04f);
            t.personal_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.45f);
            t.pro_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.personal_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.pro_NotePopoutBg = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            t.personal_NotePopoutBg = new Color(0.95f, 0.96f, 0.98f, 0.96f);
            t.pro_NoteInputBg = new Color(0.00f, 0.00f, 0.00f, 0.30f);
            t.personal_NoteInputBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_NoteInputText = Color.white;
            t.personal_NoteInputText = new Color(0.10f, 0.10f, 0.12f, 1.00f);
            t.pro_NoteTitle = Color.white;
            t.personal_NoteTitle = new Color(0.08f, 0.08f, 0.10f, 1.00f);
            t.pro_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.07f);
            t.personal_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.55f);
            t.pro_NoteCardSelectedBg = new Color(0.00f, 0.48f, 1.00f, 0.40f);
            t.personal_NoteCardSelectedBg = new Color(0.00f, 0.48f, 1.00f, 0.20f);
            t.pro_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.14f);
            t.personal_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.80f);

            t.pro_CardDetailBg = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            t.personal_CardDetailBg = new Color(0.94f, 0.95f, 0.98f, 0.96f);
            t.pro_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.10f);
            t.personal_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_ButtonText = Color.white;
            t.personal_ButtonText = new Color(0.10f, 0.10f, 0.12f, 1.00f);
            t.pro_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.20f);
            t.personal_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = new Color(0.05f, 0.05f, 0.08f, 1.00f);

            t.pro_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.pro_DropdownText = Color.white;
            t.personal_DropdownText = new Color(0.10f, 0.10f, 0.12f, 1.00f);
            t.pro_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.18f);
            t.personal_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.05f, 0.05f, 0.08f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.12f, 0.14f, 0.18f, 0.94f);
            t.personal_DropdownMenuBg = new Color(0.96f, 0.97f, 0.99f, 0.95f);
            t.pro_DropdownMenuText = Color.white;
            t.personal_DropdownMenuText = new Color(0.10f, 0.10f, 0.12f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.65f);
            t.personal_DropdownMenuHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.15f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = new Color(0.00f, 0.35f, 0.85f, 1.00f);

            t.pro_PopupBg = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            t.personal_PopupBg = new Color(0.96f, 0.97f, 0.99f, 0.96f);
            t.pro_DeleteBtnBg = new Color(1.00f, 0.27f, 0.23f, 0.40f);
            t.personal_DeleteBtnBg = new Color(1.00f, 0.27f, 0.23f, 0.18f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = new Color(0.85f, 0.15f, 0.12f, 1.00f);
            t.pro_DeleteBtnHoverBg = new Color(1.00f, 0.27f, 0.23f, 0.70f);
            t.personal_DeleteBtnHoverBg = new Color(1.00f, 0.27f, 0.23f, 0.35f);

            t.pro_HeaderTabActiveBg = new Color(0.00f, 0.48f, 1.00f, 0.85f);
            t.personal_HeaderTabActiveBg = new Color(0.00f, 0.48f, 1.00f, 0.85f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.personal_HeaderTabInactiveBg = new Color(0.00f, 0.00f, 0.00f, 0.05f);
            t.pro_HeaderTabInactiveText = new Color(0.85f, 0.88f, 0.92f, 0.90f);
            t.personal_HeaderTabInactiveText = new Color(0.25f, 0.25f, 0.30f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.14f);
            t.personal_HeaderTabHoverBg = new Color(0.00f, 0.00f, 0.00f, 0.10f);

            t.pro_AddCardBg = new Color(0.00f, 0.48f, 1.00f, 0.25f);
            t.personal_AddCardBg = new Color(0.00f, 0.48f, 1.00f, 0.12f);
            t.pro_AddCardText = new Color(0.60f, 0.85f, 1.00f, 1.00f);
            t.personal_AddCardText = new Color(0.00f, 0.40f, 0.90f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.50f);
            t.personal_AddCardHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.25f);

            t.pro_NoteActionBg = new Color(0.00f, 0.48f, 1.00f, 0.35f);
            t.personal_NoteActionBg = new Color(0.00f, 0.48f, 1.00f, 0.20f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.00f, 0.35f, 0.80f, 1.00f);
            t.pro_NoteActionHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.55f);
            t.personal_NoteActionHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.35f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.00f, 0.35f, 0.80f, 1.00f);

            t.pro_TooltipBg = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            t.personal_TooltipBg = new Color(0.96f, 0.97f, 0.99f, 0.85f);
            t.pro_TooltipText = Color.white;
            t.personal_TooltipText = new Color(0.10f, 0.12f, 0.15f);
            t.pro_TooltipBorder = new Color(1.00f, 1.00f, 1.00f, 0.25f);
            t.personal_TooltipBorder = new Color(0.00f, 0.00f, 0.00f, 0.15f);

            t.pro_AddNoteBg = new Color(0.00f, 0.48f, 1.00f, 0.40f);
            t.personal_AddNoteBg = new Color(0.00f, 0.48f, 1.00f, 0.22f);
            t.pro_AddNoteText = Color.white;
            t.personal_AddNoteText = new Color(0.00f, 0.40f, 0.90f, 1.00f);
            t.pro_AddNoteHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.60f);
            t.personal_AddNoteHoverBg = new Color(0.00f, 0.48f, 1.00f, 0.35f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.00f, 0.30f, 0.80f, 1.00f);

            t.pro_ImportNoteBg = new Color(1.00f, 1.00f, 1.00f, 0.10f);
            t.personal_ImportNoteBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_ImportNoteText = new Color(0.90f, 0.94f, 1.00f, 1.00f);
            t.personal_ImportNoteText = new Color(0.12f, 0.16f, 0.22f, 1.00f);
            t.pro_ImportNoteHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.20f);
            t.personal_ImportNoteHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.05f, 0.10f, 0.18f, 1.00f);

            t.pro_NoteFolderText = new Color(0.85f, 0.88f, 0.95f, 1.00f);
            t.personal_NoteFolderText = new Color(0.20f, 0.25f, 0.32f, 1.00f);

            t.pro_CardDetailsText = new Color(0.78f, 0.82f, 0.90f, 1.00f);
            t.personal_CardDetailsText = new Color(0.35f, 0.38f, 0.45f, 1.00f);
            t.pro_CardTasksText = new Color(0.70f, 0.75f, 0.85f, 1.00f);
            t.personal_CardTasksText = new Color(0.30f, 0.35f, 0.42f, 1.00f);
            t.pro_CardCategoryTag = new Color(0.40f, 0.70f, 1.00f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.00f, 0.45f, 0.85f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.16f, 0.16f, 0.20f, 0.90f);
            t.personal_AssigneeAvatarBg = new Color(0.92f, 0.93f, 0.96f, 0.90f);

            t.pro_StatusOverdue = new Color(1.00f, 0.27f, 0.23f, 1.00f);
            t.personal_StatusOverdue = new Color(0.88f, 0.18f, 0.15f, 1.00f);
            t.pro_StatusDueToday = new Color(1.00f, 0.60f, 0.00f, 1.00f);
            t.personal_StatusDueToday = new Color(0.90f, 0.48f, 0.00f, 1.00f);
            t.pro_StatusDueSoon = new Color(1.00f, 0.80f, 0.00f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.85f, 0.65f, 0.00f, 1.00f);
            t.pro_StatusCompleted = new Color(0.20f, 0.80f, 0.35f, 1.00f);
            t.personal_StatusCompleted = new Color(0.12f, 0.65f, 0.25f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.20f, 0.80f, 0.35f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.12f, 0.65f, 0.25f, 1.00f);

            t.pro_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.75f);
            t.pro_ChecklistTickCheckedBg = new Color(0.00f, 0.48f, 1.00f, 0.95f);
            t.personal_ChecklistTickCheckedBg = new Color(0.00f, 0.48f, 1.00f, 1.00f);
            t.pro_ChecklistTickBorder = new Color(1.00f, 1.00f, 1.00f, 0.22f);
            t.personal_ChecklistTickBorder = new Color(0.00f, 0.00f, 0.00f, 0.18f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Vector;

            t.tabActive = new Color(0.00f, 0.48f, 1.00f, 0.90f);
            t.noteSelectedAccent = new Color(0.00f, 0.48f, 1.00f, 0.95f);
            t.linkColor = new Color(0.18f, 0.60f, 1.00f, 1.00f);

            return t;
        }

        public static ThemeData CreateAuroraGlowGlass()
        {
            var t = CreateDefault();
            t.name = "Aurora Glow Glass";
            t.priorityIcons = new List<string> { "", "✨", "💫", "🌟", "⚡" };
            t.boardTabIcon = "🌌";
            t.notesTabIcon = "🔮";
            t.styleTabIcon = "✨";
            t.boardHeaderIcon = "🌌";
            t.notesHeaderIcon = "🔮";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "🧑‍🚀";
            t.priorityFilterIcon = "⚡";
            t.parentLinkIcon = "💠";
            t.childLinkIcon = "🔷";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "✨";
            t.overdueIcon = "💥";
            t.dueTodayIcon = "🌟";
            t.dueSoonIcon = "💫";
            t.dueDateIcon = "📆";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "🗃️";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "✨";
            t.checklistIcon = "✨";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.pro_BoardHeader = new Color(0.00f, 0.95f, 0.85f, 1.00f);
            t.personal_BoardHeader = new Color(0.05f, 0.35f, 0.35f, 1.00f);
            t.pro_ColumnHeader = new Color(0.70f, 0.98f, 0.95f, 1.00f);
            t.personal_ColumnHeader = new Color(0.08f, 0.25f, 0.30f, 1.00f);
            t.pro_CardTitle = new Color(0.90f, 1.00f, 0.98f, 1.00f);
            t.personal_CardTitle = new Color(0.05f, 0.18f, 0.22f, 1.00f);
            t.pro_CardText = new Color(0.70f, 0.90f, 0.92f, 1.00f);
            t.personal_CardText = new Color(0.15f, 0.32f, 0.38f, 1.00f);
            t.pro_SectionLabel = new Color(0.00f, 0.95f, 0.85f, 1.00f);
            t.personal_SectionLabel = new Color(0.05f, 0.35f, 0.35f, 1.00f);

            t.pro_BoardBg = new Color(0.04f, 0.07f, 0.12f, 1.00f);
            t.personal_BoardBg = new Color(0.90f, 0.95f, 0.96f, 1.00f);
            t.pro_TopBarBg = new Color(0.06f, 0.11f, 0.18f, 0.80f);
            t.personal_TopBarBg = new Color(1.00f, 1.00f, 1.00f, 0.78f);
            t.pro_StatusBarBg = new Color(0.06f, 0.11f, 0.18f, 0.80f);
            t.personal_StatusBarBg = new Color(1.00f, 1.00f, 1.00f, 0.78f);
            t.pro_StatusBarText = new Color(0.68f, 0.88f, 0.95f, 1.00f);
            t.personal_StatusBarText = new Color(0.20f, 0.40f, 0.48f, 1.00f);

            t.pro_ColumnBg = new Color(0.00f, 0.85f, 0.70f, 0.08f);
            t.pro_ColumnBgAlt = new Color(0.55f, 0.25f, 0.85f, 0.08f);
            t.personal_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.60f);
            t.personal_ColumnBgAlt = new Color(0.85f, 0.95f, 0.96f, 0.50f);

            t.pro_CardBg = new Color(0.08f, 0.18f, 0.28f, 0.55f);
            t.personal_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_CardHighlighted = new Color(0.00f, 0.85f, 0.70f, 0.40f);
            t.personal_CardHighlighted = new Color(0.00f, 0.80f, 0.65f, 0.25f);

            t.pro_NoteSidebarBg = new Color(0.06f, 0.12f, 0.20f, 0.50f);
            t.personal_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.45f);
            t.pro_NoteEditorBg = new Color(0.08f, 0.15f, 0.24f, 0.45f);
            t.personal_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.pro_NotePopoutBg = new Color(0.06f, 0.11f, 0.18f, 0.95f);
            t.personal_NotePopoutBg = new Color(0.92f, 0.96f, 0.98f, 0.96f);
            t.pro_NoteInputBg = new Color(0.02f, 0.06f, 0.10f, 0.50f);
            t.personal_NoteInputBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_NoteInputText = new Color(0.90f, 1.00f, 0.98f, 1.00f);
            t.personal_NoteInputText = new Color(0.05f, 0.18f, 0.22f, 1.00f);
            t.pro_NoteTitle = new Color(0.90f, 1.00f, 0.98f, 1.00f);
            t.personal_NoteTitle = new Color(0.05f, 0.18f, 0.22f, 1.00f);
            t.pro_NoteCardBg = new Color(0.08f, 0.18f, 0.28f, 0.50f);
            t.personal_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.60f);
            t.pro_NoteCardSelectedBg = new Color(0.00f, 0.85f, 0.70f, 0.40f);
            t.personal_NoteCardSelectedBg = new Color(0.00f, 0.75f, 0.65f, 0.25f);
            t.pro_NoteCardHoverBg = new Color(0.12f, 0.25f, 0.38f, 0.60f);
            t.personal_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);

            t.pro_CardDetailBg = new Color(0.06f, 0.11f, 0.18f, 0.95f);
            t.personal_CardDetailBg = new Color(0.92f, 0.96f, 0.98f, 0.96f);
            t.pro_ButtonBg = new Color(0.00f, 0.80f, 0.70f, 0.20f);
            t.personal_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_ButtonText = new Color(0.70f, 1.00f, 0.95f, 1.00f);
            t.personal_ButtonText = new Color(0.05f, 0.25f, 0.28f, 1.00f);
            t.pro_ButtonHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.40f);
            t.personal_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = new Color(0.02f, 0.15f, 0.18f, 1.00f);

            t.pro_DropdownBg = new Color(0.08f, 0.18f, 0.28f, 0.50f);
            t.personal_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.pro_DropdownText = new Color(0.70f, 1.00f, 0.95f, 1.00f);
            t.personal_DropdownText = new Color(0.05f, 0.25f, 0.28f, 1.00f);
            t.pro_DropdownHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.30f);
            t.personal_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.02f, 0.15f, 0.18f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.06f, 0.11f, 0.18f, 0.95f);
            t.personal_DropdownMenuBg = new Color(0.94f, 0.98f, 0.99f, 0.96f);
            t.pro_DropdownMenuText = new Color(0.90f, 1.00f, 0.98f, 1.00f);
            t.personal_DropdownMenuText = new Color(0.05f, 0.25f, 0.28f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.50f);
            t.personal_DropdownMenuHoverBg = new Color(0.00f, 0.80f, 0.65f, 0.20f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = new Color(0.00f, 0.45f, 0.40f, 1.00f);

            t.pro_PopupBg = new Color(0.06f, 0.11f, 0.18f, 0.95f);
            t.personal_PopupBg = new Color(0.94f, 0.98f, 0.99f, 0.96f);
            t.pro_DeleteBtnBg = new Color(0.90f, 0.25f, 0.40f, 0.35f);
            t.personal_DeleteBtnBg = new Color(0.90f, 0.25f, 0.40f, 0.18f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = new Color(0.80f, 0.15f, 0.30f, 1.00f);
            t.pro_DeleteBtnHoverBg = new Color(0.95f, 0.25f, 0.40f, 0.65f);
            t.personal_DeleteBtnHoverBg = new Color(0.95f, 0.25f, 0.40f, 0.35f);

            t.pro_HeaderTabActiveBg = new Color(0.00f, 0.85f, 0.70f, 0.80f);
            t.personal_HeaderTabActiveBg = new Color(0.00f, 0.75f, 0.65f, 0.85f);
            t.pro_HeaderTabActiveText = new Color(0.02f, 0.15f, 0.18f, 1.00f);
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(0.08f, 0.18f, 0.28f, 0.40f);
            t.personal_HeaderTabInactiveBg = new Color(0.00f, 0.00f, 0.00f, 0.05f);
            t.pro_HeaderTabInactiveText = new Color(0.70f, 0.95f, 0.92f, 0.90f);
            t.personal_HeaderTabInactiveText = new Color(0.15f, 0.32f, 0.38f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.30f);
            t.personal_HeaderTabHoverBg = new Color(0.00f, 0.00f, 0.00f, 0.10f);

            t.pro_AddCardBg = new Color(0.00f, 0.85f, 0.70f, 0.25f);
            t.personal_AddCardBg = new Color(0.00f, 0.75f, 0.65f, 0.15f);
            t.pro_AddCardText = new Color(0.50f, 1.00f, 0.90f, 1.00f);
            t.personal_AddCardText = new Color(0.00f, 0.45f, 0.40f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.45f);
            t.personal_AddCardHoverBg = new Color(0.00f, 0.75f, 0.65f, 0.25f);

            t.pro_NoteActionBg = new Color(0.00f, 0.78f, 0.75f, 0.30f);
            t.personal_NoteActionBg = new Color(0.00f, 0.78f, 0.75f, 0.20f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.00f, 0.45f, 0.45f, 1.00f);
            t.pro_NoteActionHoverBg = new Color(0.00f, 0.78f, 0.75f, 0.50f);
            t.personal_NoteActionHoverBg = new Color(0.00f, 0.78f, 0.75f, 0.35f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.00f, 0.45f, 0.45f, 1.00f);

            t.pro_TooltipBg = new Color(0.06f, 0.12f, 0.14f, 0.85f);
            t.personal_TooltipBg = new Color(0.92f, 0.97f, 0.98f, 0.85f);
            t.pro_TooltipText = new Color(0.70f, 1.00f, 0.90f);
            t.personal_TooltipText = new Color(0.05f, 0.25f, 0.22f);
            t.pro_TooltipBorder = new Color(0.20f, 0.90f, 0.70f, 0.45f);
            t.personal_TooltipBorder = new Color(0.10f, 0.65f, 0.50f, 0.35f);

            t.pro_AddNoteBg = new Color(0.00f, 0.85f, 0.70f, 0.35f);
            t.personal_AddNoteBg = new Color(0.00f, 0.75f, 0.65f, 0.22f);
            t.pro_AddNoteText = new Color(0.70f, 1.00f, 0.95f, 1.00f);
            t.personal_AddNoteText = new Color(0.00f, 0.45f, 0.40f, 1.00f);
            t.pro_AddNoteHoverBg = new Color(0.00f, 0.85f, 0.70f, 0.55f);
            t.personal_AddNoteHoverBg = new Color(0.00f, 0.75f, 0.65f, 0.35f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.00f, 0.35f, 0.30f, 1.00f);

            t.pro_ImportNoteBg = new Color(0.40f, 0.20f, 0.70f, 0.30f);
            t.personal_ImportNoteBg = new Color(0.60f, 0.40f, 0.80f, 0.18f);
            t.pro_ImportNoteText = new Color(0.85f, 0.80f, 1.00f, 1.00f);
            t.personal_ImportNoteText = new Color(0.30f, 0.15f, 0.45f, 1.00f);
            t.pro_ImportNoteHoverBg = new Color(0.40f, 0.20f, 0.70f, 0.50f);
            t.personal_ImportNoteHoverBg = new Color(0.60f, 0.40f, 0.80f, 0.30f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.20f, 0.10f, 0.35f, 1.00f);

            t.pro_NoteFolderText = new Color(0.75f, 0.98f, 0.92f, 1.00f);
            t.personal_NoteFolderText = new Color(0.10f, 0.30f, 0.32f, 1.00f);

            t.pro_CardDetailsText = new Color(0.70f, 0.90f, 0.88f, 1.00f);
            t.personal_CardDetailsText = new Color(0.20f, 0.38f, 0.40f, 1.00f);
            t.pro_CardTasksText = new Color(0.65f, 0.85f, 0.82f, 1.00f);
            t.personal_CardTasksText = new Color(0.18f, 0.34f, 0.36f, 1.00f);
            t.pro_CardCategoryTag = new Color(0.00f, 0.95f, 0.85f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.05f, 0.50f, 0.48f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.08f, 0.14f, 0.18f, 0.90f);
            t.personal_AssigneeAvatarBg = new Color(0.90f, 0.96f, 0.96f, 0.90f);

            t.pro_StatusOverdue = new Color(1.00f, 0.30f, 0.45f, 1.00f);
            t.personal_StatusOverdue = new Color(0.85f, 0.20f, 0.30f, 1.00f);
            t.pro_StatusDueToday = new Color(1.00f, 0.65f, 0.20f, 1.00f);
            t.personal_StatusDueToday = new Color(0.88f, 0.50f, 0.08f, 1.00f);
            t.pro_StatusDueSoon = new Color(0.95f, 0.90f, 0.25f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.70f, 0.10f, 1.00f);
            t.pro_StatusCompleted = new Color(0.00f, 0.95f, 0.75f, 1.00f);
            t.personal_StatusCompleted = new Color(0.05f, 0.65f, 0.45f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.00f, 0.95f, 0.75f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.05f, 0.65f, 0.45f, 1.00f);

            t.pro_ChecklistTickBg = new Color(0.02f, 0.08f, 0.14f, 0.65f);
            t.personal_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.75f);
            t.pro_ChecklistTickCheckedBg = new Color(0.00f, 0.85f, 0.70f, 0.90f);
            t.personal_ChecklistTickCheckedBg = new Color(0.00f, 0.70f, 0.62f, 0.95f);
            t.pro_ChecklistTickBorder = new Color(0.00f, 0.95f, 0.85f, 0.70f);
            t.personal_ChecklistTickBorder = new Color(0.00f, 0.65f, 0.60f, 0.55f);
            t.pro_ChecklistTickColor = new Color(0.02f, 0.08f, 0.12f);
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Dot;

            t.tabActive = new Color(0.00f, 0.85f, 0.70f, 0.85f);
            t.noteSelectedAccent = new Color(0.00f, 0.85f, 0.70f, 0.95f);
            t.linkColor = new Color(0.00f, 0.90f, 0.80f, 1.00f);

            return t;
        }

        public static ThemeData CreateCupertinoTwilight()
        {
            var t = CreateDefault();
            t.name = "Cupertino Twilight";
            t.priorityIcons = new List<string> { "", "🌸", "🌺", "🌅", "🔥" };
            t.boardTabIcon = "🌅";
            t.notesTabIcon = "📜";
            t.styleTabIcon = "🎨";
            t.boardHeaderIcon = "🌅";
            t.notesHeaderIcon = "📜";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👤";
            t.priorityFilterIcon = "🔥";
            t.parentLinkIcon = "🌺";
            t.childLinkIcon = "🌸";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "✔";
            t.overdueIcon = "🔥";
            t.dueTodayIcon = "🌅";
            t.dueSoonIcon = "🌺";
            t.dueDateIcon = "📆";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "🗃️";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "✨";
            t.checklistIcon = "✔";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.pro_BoardHeader = new Color(1.00f, 0.70f, 0.60f, 1.00f);
            t.personal_BoardHeader = new Color(0.40f, 0.15f, 0.18f, 1.00f);
            t.pro_ColumnHeader = new Color(0.98f, 0.85f, 0.80f, 1.00f);
            t.personal_ColumnHeader = new Color(0.35f, 0.18f, 0.22f, 1.00f);
            t.pro_CardTitle = new Color(1.00f, 0.95f, 0.92f, 1.00f);
            t.personal_CardTitle = new Color(0.25f, 0.10f, 0.15f, 1.00f);
            t.pro_CardText = new Color(0.90f, 0.80f, 0.82f, 1.00f);
            t.personal_CardText = new Color(0.40f, 0.25f, 0.30f, 1.00f);
            t.pro_SectionLabel = new Color(1.00f, 0.70f, 0.60f, 1.00f);
            t.personal_SectionLabel = new Color(0.40f, 0.15f, 0.18f, 1.00f);

            t.pro_BoardBg = new Color(0.12f, 0.08f, 0.14f, 1.00f);
            t.personal_BoardBg = new Color(0.97f, 0.93f, 0.92f, 1.00f);
            t.pro_TopBarBg = new Color(0.18f, 0.11f, 0.20f, 0.82f);
            t.personal_TopBarBg = new Color(1.00f, 1.00f, 1.00f, 0.80f);
            t.pro_StatusBarBg = new Color(0.18f, 0.11f, 0.20f, 0.82f);
            t.personal_StatusBarBg = new Color(1.00f, 1.00f, 1.00f, 0.80f);
            t.pro_StatusBarText = new Color(0.85f, 0.78f, 0.92f, 1.00f);
            t.personal_StatusBarText = new Color(0.38f, 0.30f, 0.48f, 1.00f);

            t.pro_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.07f);
            t.pro_ColumnBgAlt = new Color(0.96f, 0.48f, 0.38f, 0.06f);
            t.personal_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.60f);
            t.personal_ColumnBgAlt = new Color(1.00f, 0.92f, 0.90f, 0.45f);

            t.pro_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.12f);
            t.personal_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.88f);
            t.pro_CardHighlighted = new Color(0.96f, 0.48f, 0.38f, 0.40f);
            t.personal_CardHighlighted = new Color(0.96f, 0.48f, 0.38f, 0.25f);

            t.pro_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.05f);
            t.personal_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.50f);
            t.pro_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.68f);
            t.pro_NotePopoutBg = new Color(0.16f, 0.10f, 0.18f, 0.95f);
            t.personal_NotePopoutBg = new Color(0.98f, 0.94f, 0.94f, 0.96f);
            t.pro_NoteInputBg = new Color(0.00f, 0.00f, 0.00f, 0.35f);
            t.personal_NoteInputBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_NoteInputText = new Color(1.00f, 0.95f, 0.92f, 1.00f);
            t.personal_NoteInputText = new Color(0.25f, 0.10f, 0.15f, 1.00f);
            t.pro_NoteTitle = new Color(1.00f, 0.95f, 0.92f, 1.00f);
            t.personal_NoteTitle = new Color(0.25f, 0.10f, 0.15f, 1.00f);
            t.pro_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.60f);
            t.pro_NoteCardSelectedBg = new Color(0.96f, 0.48f, 0.38f, 0.45f);
            t.personal_NoteCardSelectedBg = new Color(0.96f, 0.48f, 0.38f, 0.25f);
            t.pro_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.16f);
            t.personal_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);

            t.pro_CardDetailBg = new Color(0.16f, 0.10f, 0.18f, 0.95f);
            t.personal_CardDetailBg = new Color(0.98f, 0.94f, 0.94f, 0.96f);
            t.pro_ButtonBg = new Color(0.96f, 0.48f, 0.38f, 0.20f);
            t.personal_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_ButtonText = new Color(1.00f, 0.85f, 0.80f, 1.00f);
            t.personal_ButtonText = new Color(0.40f, 0.15f, 0.18f, 1.00f);
            t.pro_ButtonHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.40f);
            t.personal_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = new Color(0.25f, 0.08f, 0.10f, 1.00f);

            t.pro_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.10f);
            t.personal_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.pro_DropdownText = new Color(1.00f, 0.85f, 0.80f, 1.00f);
            t.personal_DropdownText = new Color(0.40f, 0.15f, 0.18f, 1.00f);
            t.pro_DropdownHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.30f);
            t.personal_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.25f, 0.08f, 0.10f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.18f, 0.11f, 0.20f, 0.95f);
            t.personal_DropdownMenuBg = new Color(0.99f, 0.96f, 0.95f, 0.96f);
            t.pro_DropdownMenuText = new Color(1.00f, 0.92f, 0.88f, 1.00f);
            t.personal_DropdownMenuText = new Color(0.40f, 0.15f, 0.18f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.60f);
            t.personal_DropdownMenuHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.20f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = new Color(0.60f, 0.20f, 0.15f, 1.00f);

            t.pro_PopupBg = new Color(0.18f, 0.11f, 0.20f, 0.95f);
            t.personal_PopupBg = new Color(0.99f, 0.96f, 0.95f, 0.96f);
            t.pro_DeleteBtnBg = new Color(0.85f, 0.20f, 0.25f, 0.35f);
            t.personal_DeleteBtnBg = new Color(0.85f, 0.20f, 0.25f, 0.18f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = new Color(0.75f, 0.12f, 0.18f, 1.00f);
            t.pro_DeleteBtnHoverBg = new Color(0.90f, 0.20f, 0.25f, 0.65f);
            t.personal_DeleteBtnHoverBg = new Color(0.90f, 0.20f, 0.25f, 0.35f);

            t.pro_HeaderTabActiveBg = new Color(0.96f, 0.48f, 0.38f, 0.85f);
            t.personal_HeaderTabActiveBg = new Color(0.96f, 0.48f, 0.38f, 0.85f);
            t.pro_HeaderTabActiveText = Color.white;
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_HeaderTabInactiveBg = new Color(0.00f, 0.00f, 0.00f, 0.05f);
            t.pro_HeaderTabInactiveText = new Color(0.92f, 0.82f, 0.80f, 0.90f);
            t.personal_HeaderTabInactiveText = new Color(0.35f, 0.18f, 0.22f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.30f);
            t.personal_HeaderTabHoverBg = new Color(0.00f, 0.00f, 0.00f, 0.10f);

            t.pro_AddCardBg = new Color(0.96f, 0.48f, 0.38f, 0.25f);
            t.personal_AddCardBg = new Color(0.96f, 0.48f, 0.38f, 0.15f);
            t.pro_AddCardText = new Color(1.00f, 0.85f, 0.80f, 1.00f);
            t.personal_AddCardText = new Color(0.60f, 0.20f, 0.15f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.50f);
            t.personal_AddCardHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.28f);

            t.pro_NoteActionBg = new Color(0.85f, 0.40f, 0.60f, 0.35f);
            t.personal_NoteActionBg = new Color(0.85f, 0.40f, 0.60f, 0.20f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.55f, 0.15f, 0.35f, 1.00f);
            t.pro_NoteActionHoverBg = new Color(0.85f, 0.40f, 0.60f, 0.55f);
            t.personal_NoteActionHoverBg = new Color(0.85f, 0.40f, 0.60f, 0.35f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.55f, 0.15f, 0.35f, 1.00f);

            t.pro_TooltipBg = new Color(0.15f, 0.10f, 0.18f, 0.85f);
            t.personal_TooltipBg = new Color(0.98f, 0.94f, 0.98f, 0.85f);
            t.pro_TooltipText = new Color(1.00f, 0.92f, 0.96f);
            t.personal_TooltipText = new Color(0.30f, 0.12f, 0.32f);
            t.pro_TooltipBorder = new Color(0.90f, 0.55f, 0.80f, 0.40f);
            t.personal_TooltipBorder = new Color(0.80f, 0.40f, 0.70f, 0.30f);

            t.pro_AddNoteBg = new Color(0.96f, 0.48f, 0.38f, 0.35f);
            t.personal_AddNoteBg = new Color(0.96f, 0.48f, 0.38f, 0.20f);
            t.pro_AddNoteText = new Color(1.00f, 0.90f, 0.88f, 1.00f);
            t.personal_AddNoteText = new Color(0.60f, 0.20f, 0.15f, 1.00f);
            t.pro_AddNoteHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.55f);
            t.personal_AddNoteHoverBg = new Color(0.96f, 0.48f, 0.38f, 0.35f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.50f, 0.12f, 0.10f, 1.00f);

            t.pro_ImportNoteBg = new Color(0.70f, 0.35f, 0.55f, 0.30f);
            t.personal_ImportNoteBg = new Color(0.70f, 0.35f, 0.55f, 0.18f);
            t.pro_ImportNoteText = new Color(0.98f, 0.88f, 0.95f, 1.00f);
            t.personal_ImportNoteText = new Color(0.48f, 0.15f, 0.35f, 1.00f);
            t.pro_ImportNoteHoverBg = new Color(0.70f, 0.35f, 0.55f, 0.50f);
            t.personal_ImportNoteHoverBg = new Color(0.70f, 0.35f, 0.55f, 0.30f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.38f, 0.10f, 0.28f, 1.00f);

            t.pro_NoteFolderText = new Color(0.95f, 0.85f, 0.85f, 1.00f);
            t.personal_NoteFolderText = new Color(0.35f, 0.18f, 0.22f, 1.00f);

            t.pro_CardDetailsText = new Color(0.90f, 0.80f, 0.82f, 1.00f);
            t.personal_CardDetailsText = new Color(0.40f, 0.25f, 0.30f, 1.00f);
            t.pro_CardTasksText = new Color(0.85f, 0.75f, 0.78f, 1.00f);
            t.personal_CardTasksText = new Color(0.35f, 0.20f, 0.25f, 1.00f);
            t.pro_CardCategoryTag = new Color(0.96f, 0.60f, 0.50f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.65f, 0.22f, 0.18f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.18f, 0.16f, 0.22f, 0.90f);
            t.personal_AssigneeAvatarBg = new Color(0.94f, 0.92f, 0.96f, 0.90f);

            t.pro_StatusOverdue = new Color(0.95f, 0.30f, 0.35f, 1.00f);
            t.personal_StatusOverdue = new Color(0.85f, 0.20f, 0.25f, 1.00f);
            t.pro_StatusDueToday = new Color(0.98f, 0.55f, 0.25f, 1.00f);
            t.personal_StatusDueToday = new Color(0.88f, 0.42f, 0.12f, 1.00f);
            t.pro_StatusDueSoon = new Color(0.95f, 0.82f, 0.30f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.80f, 0.65f, 0.12f, 1.00f);
            t.pro_StatusCompleted = new Color(0.35f, 0.88f, 0.60f, 1.00f);
            t.personal_StatusCompleted = new Color(0.15f, 0.65f, 0.35f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.35f, 0.88f, 0.60f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.15f, 0.65f, 0.35f, 1.00f);

            t.pro_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.75f);
            t.pro_ChecklistTickCheckedBg = new Color(0.96f, 0.48f, 0.38f, 0.90f);
            t.personal_ChecklistTickCheckedBg = new Color(0.96f, 0.48f, 0.38f, 0.95f);
            t.pro_ChecklistTickBorder = new Color(0.96f, 0.48f, 0.38f, 0.60f);
            t.personal_ChecklistTickBorder = new Color(0.90f, 0.45f, 0.35f, 0.50f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Vector;

            t.tabActive = new Color(0.96f, 0.48f, 0.38f, 0.90f);
            t.noteSelectedAccent = new Color(0.96f, 0.48f, 0.38f, 0.95f);
            t.linkColor = new Color(1.00f, 0.55f, 0.45f, 1.00f);

            return t;
        }

        public static ThemeData CreateFrostedIce()
        {
            var t = CreateDefault();
            t.name = "Frosted Ice";
            t.priorityIcons = new List<string> { "", "🧊", "🔹", "💎", "❄️" };
            t.boardTabIcon = "🧊";
            t.notesTabIcon = "📄";
            t.styleTabIcon = "✨";
            t.boardHeaderIcon = "💎";
            t.notesHeaderIcon = "📄";
            t.categoryIcon = "🏷️";
            t.assigneeIcon = "👤";
            t.priorityFilterIcon = "❄️";
            t.parentLinkIcon = "🔷";
            t.childLinkIcon = "🔹";
            t.pinnedNoteIcon = "📍";
            t.completedIcon = "✔";
            t.overdueIcon = "❄️";
            t.dueTodayIcon = "💎";
            t.dueSoonIcon = "🔹";
            t.dueDateIcon = "📆";
            t.archiveIcon = "📦";
            t.unarchiveIcon = "🗃️";
            t.cardDetailIcon = "📝";
            t.newCardIcon = "✨";
            t.checklistIcon = "✔";
            t.attachmentIcon = "📎";
            t.urlIcon = "🔗";
            t.deleteIcon = "🗑";
            t.saveIcon = "💾";
            t.cancelIcon = "✕";
            t.moveUpIcon = "▲";
            t.moveDownIcon = "▼";

            t.pro_BoardHeader = new Color(0.75f, 0.90f, 1.00f, 1.00f);
            t.personal_BoardHeader = new Color(0.10f, 0.25f, 0.40f, 1.00f);
            t.pro_ColumnHeader = new Color(0.85f, 0.95f, 1.00f, 1.00f);
            t.personal_ColumnHeader = new Color(0.12f, 0.20f, 0.30f, 1.00f);
            t.pro_CardTitle = Color.white;
            t.personal_CardTitle = new Color(0.08f, 0.15f, 0.25f, 1.00f);
            t.pro_CardText = new Color(0.80f, 0.90f, 0.98f, 1.00f);
            t.personal_CardText = new Color(0.20f, 0.30f, 0.40f, 1.00f);
            t.pro_SectionLabel = new Color(0.75f, 0.90f, 1.00f, 1.00f);
            t.personal_SectionLabel = new Color(0.10f, 0.25f, 0.40f, 1.00f);

            t.pro_BoardBg = new Color(0.06f, 0.08f, 0.10f, 1.00f);
            t.personal_BoardBg = new Color(0.94f, 0.96f, 0.98f, 1.00f);
            t.pro_TopBarBg = new Color(0.10f, 0.14f, 0.18f, 0.75f);
            t.personal_TopBarBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_StatusBarBg = new Color(0.10f, 0.14f, 0.18f, 0.75f);
            t.personal_StatusBarBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);
            t.pro_StatusBarText = new Color(0.75f, 0.88f, 0.95f, 1.00f);
            t.personal_StatusBarText = new Color(0.25f, 0.38f, 0.48f, 1.00f);

            t.pro_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.05f);
            t.pro_ColumnBgAlt = new Color(0.35f, 0.75f, 1.00f, 0.04f);
            t.personal_ColumnBg = new Color(1.00f, 1.00f, 1.00f, 0.65f);
            t.personal_ColumnBgAlt = new Color(0.88f, 0.94f, 0.98f, 0.45f);

            t.pro_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.09f);
            t.personal_CardBg = new Color(1.00f, 1.00f, 1.00f, 0.90f);
            t.pro_CardHighlighted = new Color(0.35f, 0.75f, 1.00f, 0.35f);
            t.personal_CardHighlighted = new Color(0.35f, 0.75f, 1.00f, 0.20f);

            t.pro_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.04f);
            t.personal_NoteSidebarBg = new Color(1.00f, 1.00f, 1.00f, 0.50f);
            t.pro_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.personal_NoteEditorBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_NotePopoutBg = new Color(0.08f, 0.12f, 0.16f, 0.95f);
            t.personal_NotePopoutBg = new Color(0.96f, 0.98f, 1.00f, 0.96f);
            t.pro_NoteInputBg = new Color(0.00f, 0.00f, 0.00f, 0.30f);
            t.personal_NoteInputBg = new Color(1.00f, 1.00f, 1.00f, 0.88f);
            t.pro_NoteInputText = Color.white;
            t.personal_NoteInputText = new Color(0.08f, 0.15f, 0.25f, 1.00f);
            t.pro_NoteTitle = Color.white;
            t.personal_NoteTitle = new Color(0.08f, 0.15f, 0.25f, 1.00f);
            t.pro_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.personal_NoteCardBg = new Color(1.00f, 1.00f, 1.00f, 0.60f);
            t.pro_NoteCardSelectedBg = new Color(0.35f, 0.75f, 1.00f, 0.40f);
            t.personal_NoteCardSelectedBg = new Color(0.35f, 0.75f, 1.00f, 0.20f);
            t.pro_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.14f);
            t.personal_NoteCardHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.85f);

            t.pro_CardDetailBg = new Color(0.08f, 0.12f, 0.16f, 0.95f);
            t.personal_CardDetailBg = new Color(0.96f, 0.98f, 1.00f, 0.96f);
            t.pro_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.10f);
            t.personal_ButtonBg = new Color(1.00f, 1.00f, 1.00f, 0.75f);
            t.pro_ButtonText = new Color(0.85f, 0.95f, 1.00f, 1.00f);
            t.personal_ButtonText = new Color(0.10f, 0.20f, 0.30f, 1.00f);
            t.pro_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.20f);
            t.personal_ButtonHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_ButtonHoverText = Color.white;
            t.personal_ButtonHoverText = new Color(0.05f, 0.10f, 0.15f, 1.00f);

            t.pro_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.08f);
            t.personal_DropdownBg = new Color(1.00f, 1.00f, 1.00f, 0.70f);
            t.pro_DropdownText = new Color(0.85f, 0.95f, 1.00f, 1.00f);
            t.personal_DropdownText = new Color(0.10f, 0.20f, 0.30f, 1.00f);
            t.pro_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.18f);
            t.personal_DropdownHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.95f);
            t.pro_DropdownHoverText = Color.white;
            t.personal_DropdownHoverText = new Color(0.05f, 0.10f, 0.15f, 1.00f);

            t.pro_DropdownMenuBg = new Color(0.10f, 0.14f, 0.18f, 0.95f);
            t.personal_DropdownMenuBg = new Color(0.97f, 0.98f, 1.00f, 0.96f);
            t.pro_DropdownMenuText = new Color(0.90f, 0.96f, 1.00f, 1.00f);
            t.personal_DropdownMenuText = new Color(0.10f, 0.20f, 0.30f, 1.00f);
            t.pro_DropdownMenuHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.55f);
            t.personal_DropdownMenuHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.18f);
            t.pro_DropdownMenuHoverText = Color.white;
            t.personal_DropdownMenuHoverText = new Color(0.05f, 0.35f, 0.65f, 1.00f);

            t.pro_PopupBg = new Color(0.10f, 0.14f, 0.18f, 0.95f);
            t.personal_PopupBg = new Color(0.97f, 0.98f, 1.00f, 0.96f);
            t.pro_DeleteBtnBg = new Color(0.90f, 0.30f, 0.35f, 0.35f);
            t.personal_DeleteBtnBg = new Color(0.90f, 0.30f, 0.35f, 0.18f);
            t.pro_DeleteBtnText = Color.white;
            t.personal_DeleteBtnText = new Color(0.80f, 0.15f, 0.20f, 1.00f);
            t.pro_DeleteBtnHoverBg = new Color(0.95f, 0.30f, 0.35f, 0.65f);
            t.personal_DeleteBtnHoverBg = new Color(0.95f, 0.30f, 0.35f, 0.35f);

            t.pro_HeaderTabActiveBg = new Color(0.35f, 0.75f, 1.00f, 0.85f);
            t.personal_HeaderTabActiveBg = new Color(0.35f, 0.75f, 1.00f, 0.85f);
            t.pro_HeaderTabActiveText = new Color(0.02f, 0.10f, 0.15f, 1.00f);
            t.personal_HeaderTabActiveText = Color.white;
            t.pro_HeaderTabInactiveBg = new Color(1.00f, 1.00f, 1.00f, 0.06f);
            t.personal_HeaderTabInactiveBg = new Color(0.00f, 0.00f, 0.00f, 0.05f);
            t.pro_HeaderTabInactiveText = new Color(0.80f, 0.90f, 0.98f, 0.90f);
            t.personal_HeaderTabInactiveText = new Color(0.20f, 0.30f, 0.40f, 1.00f);
            t.pro_HeaderTabHoverBg = new Color(1.00f, 1.00f, 1.00f, 0.14f);
            t.personal_HeaderTabHoverBg = new Color(0.00f, 0.00f, 0.00f, 0.10f);

            t.pro_AddCardBg = new Color(0.35f, 0.75f, 1.00f, 0.25f);
            t.personal_AddCardBg = new Color(0.35f, 0.75f, 1.00f, 0.15f);
            t.pro_AddCardText = new Color(0.60f, 0.90f, 1.00f, 1.00f);
            t.personal_AddCardText = new Color(0.05f, 0.35f, 0.65f, 1.00f);
            t.pro_AddCardHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.45f);
            t.personal_AddCardHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.25f);

            t.pro_NoteActionBg = new Color(0.40f, 0.80f, 1.00f, 0.30f);
            t.personal_NoteActionBg = new Color(0.40f, 0.80f, 1.00f, 0.20f);
            t.pro_NoteActionText = Color.white;
            t.personal_NoteActionText = new Color(0.10f, 0.35f, 0.60f, 1.00f);
            t.pro_NoteActionHoverBg = new Color(0.40f, 0.80f, 1.00f, 0.50f);
            t.personal_NoteActionHoverBg = new Color(0.40f, 0.80f, 1.00f, 0.35f);
            t.pro_NoteActionHoverText = Color.white;
            t.personal_NoteActionHoverText = new Color(0.10f, 0.35f, 0.60f, 1.00f);

            t.pro_TooltipBg = new Color(0.10f, 0.15f, 0.22f, 0.85f);
            t.personal_TooltipBg = new Color(0.93f, 0.96f, 1.00f, 0.85f);
            t.pro_TooltipText = new Color(0.85f, 0.95f, 1.00f);
            t.personal_TooltipText = new Color(0.08f, 0.20f, 0.32f);
            t.pro_TooltipBorder = new Color(0.40f, 0.75f, 1.00f, 0.45f);
            t.personal_TooltipBorder = new Color(0.30f, 0.60f, 0.90f, 0.35f);

            t.pro_AddNoteBg = new Color(0.35f, 0.75f, 1.00f, 0.35f);
            t.personal_AddNoteBg = new Color(0.35f, 0.75f, 1.00f, 0.22f);
            t.pro_AddNoteText = new Color(0.85f, 0.95f, 1.00f, 1.00f);
            t.personal_AddNoteText = new Color(0.08f, 0.32f, 0.58f, 1.00f);
            t.pro_AddNoteHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.55f);
            t.personal_AddNoteHoverBg = new Color(0.35f, 0.75f, 1.00f, 0.35f);
            t.pro_AddNoteHoverText = Color.white;
            t.personal_AddNoteHoverText = new Color(0.05f, 0.25f, 0.48f, 1.00f);

            t.pro_ImportNoteBg = new Color(0.20f, 0.50f, 0.75f, 0.30f);
            t.personal_ImportNoteBg = new Color(0.25f, 0.55f, 0.80f, 0.18f);
            t.pro_ImportNoteText = new Color(0.85f, 0.95f, 1.00f, 1.00f);
            t.personal_ImportNoteText = new Color(0.10f, 0.30f, 0.50f, 1.00f);
            t.pro_ImportNoteHoverBg = new Color(0.20f, 0.50f, 0.75f, 0.50f);
            t.personal_ImportNoteHoverBg = new Color(0.25f, 0.55f, 0.80f, 0.30f);
            t.pro_ImportNoteHoverText = Color.white;
            t.personal_ImportNoteHoverText = new Color(0.08f, 0.22f, 0.40f, 1.00f);

            t.pro_NoteFolderText = new Color(0.80f, 0.92f, 1.00f, 1.00f);
            t.personal_NoteFolderText = new Color(0.15f, 0.25f, 0.35f, 1.00f);

            t.pro_CardDetailsText = new Color(0.75f, 0.88f, 0.98f, 1.00f);
            t.personal_CardDetailsText = new Color(0.25f, 0.35f, 0.45f, 1.00f);
            t.pro_CardTasksText = new Color(0.70f, 0.85f, 0.95f, 1.00f);
            t.personal_CardTasksText = new Color(0.20f, 0.30f, 0.40f, 1.00f);
            t.pro_CardCategoryTag = new Color(0.40f, 0.80f, 1.00f, 1.00f);
            t.personal_CardCategoryTag = new Color(0.10f, 0.45f, 0.70f, 1.00f);

            t.pro_AssigneeAvatarBg = new Color(0.10f, 0.16f, 0.22f, 0.90f);
            t.personal_AssigneeAvatarBg = new Color(0.90f, 0.95f, 0.98f, 0.90f);

            t.pro_StatusOverdue = new Color(0.95f, 0.35f, 0.45f, 1.00f);
            t.personal_StatusOverdue = new Color(0.85f, 0.25f, 0.30f, 1.00f);
            t.pro_StatusDueToday = new Color(0.98f, 0.65f, 0.30f, 1.00f);
            t.personal_StatusDueToday = new Color(0.88f, 0.50f, 0.15f, 1.00f);
            t.pro_StatusDueSoon = new Color(0.90f, 0.85f, 0.35f, 1.00f);
            t.personal_StatusDueSoon = new Color(0.75f, 0.68f, 0.18f, 1.00f);
            t.pro_StatusCompleted = new Color(0.30f, 0.90f, 0.75f, 1.00f);
            t.personal_StatusCompleted = new Color(0.10f, 0.65f, 0.48f, 1.00f);
            t.pro_TasksCompletedCount = new Color(0.30f, 0.90f, 0.75f, 1.00f);
            t.personal_TasksCompletedCount = new Color(0.10f, 0.65f, 0.48f, 1.00f);

            t.pro_ChecklistTickBg = new Color(0.08f, 0.16f, 0.26f, 0.65f);
            t.personal_ChecklistTickBg = new Color(1.00f, 1.00f, 1.00f, 0.75f);
            t.pro_ChecklistTickCheckedBg = new Color(0.20f, 0.65f, 0.95f, 0.90f);
            t.personal_ChecklistTickCheckedBg = new Color(0.15f, 0.62f, 0.90f, 0.95f);
            t.pro_ChecklistTickBorder = new Color(0.35f, 0.75f, 1.00f, 0.70f);
            t.personal_ChecklistTickBorder = new Color(0.35f, 0.70f, 0.90f, 0.55f);
            t.pro_ChecklistTickColor = Color.white;
            t.personal_ChecklistTickColor = Color.white;
            t.checklistTickStyle = ChecklistTickStyle.Classic;

            t.tabActive = new Color(0.35f, 0.75f, 1.00f, 0.85f);
            t.noteSelectedAccent = new Color(0.35f, 0.75f, 1.00f, 0.95f);
            t.linkColor = new Color(0.40f, 0.80f, 1.00f, 1.00f);

            return t;
        }

        public static List<ThemeData> GetBuiltInPresets()
        {
            return new List<ThemeData>
            {
                CreateDefault(),
                CreateDarkSlate(),
                CreateCyberpunk(),
                CreateForest(),
                CreatePastel(),
                CreateSunset(),
                CreateMonochrome(),
                CreateRetro(),
                CreateVintage8Bit(),
                CreateFruitCompanyGlassmorphism(),
                CreateAuroraGlowGlass(),
                CreateCupertinoTwilight(),
                CreateFrostedIce()
            };
        }
    }

    [Serializable]
    public class ThemeSaveData
    {
        public string selectedThemeName = "Default";
        public int currentThemeIndex = 0;
        public List<ThemeData> themes = new List<ThemeData>();
        public string version = "1.0";

        public void Normalize()
        {
            themes ??= new List<ThemeData>();
            if (themes.Count == 0)
            {
                themes.AddRange(ThemeData.GetBuiltInPresets());
            }
            else
            {
                foreach (var t in themes)
                {
                    if (t == null) continue;
                    if (t.name == "ThatFruitCompany Glassmorphism" || t.name == "Apple Glassmorphism")
                    {
                        t.name = "Fruit Company Glassmorphism";
                    }
                    t.Normalize();
                }
            }

            if (!string.IsNullOrEmpty(selectedThemeName))
            {
                int found = themes.FindIndex(t => t != null && t.name == selectedThemeName);
                if (found >= 0)
                {
                    currentThemeIndex = found;
                }
            }

            currentThemeIndex = Mathf.Clamp(currentThemeIndex, 0, Mathf.Max(0, themes.Count - 1));
            if (themes.Count > 0 && currentThemeIndex < themes.Count && themes[currentThemeIndex] != null)
            {
                selectedThemeName = themes[currentThemeIndex].name;
            }
        }
    }

    [Serializable]
    public class ThemeExportBundle
    {
        public string bundleName = "Awesome Task Manager Theme Pack";
        public string version = "1.0";
        public List<ThemeData> themes = new List<ThemeData>();
    }

    [Serializable]
    public class ExportBoardData
    {
        public TaskBoard board;
        public List<Assignee> assignees = new List<Assignee>();
        public List<CategoryColorEntry> categoryColors = new List<CategoryColorEntry>();
        public string version = "1.0";
    }

    [Serializable]
    public class ExportColumnData
    {
        public TaskColumn column;
        public List<Assignee> assignees = new List<Assignee>();
        public string version = "1.0";
    }

    [Serializable]
    public class ExportCardData
    {
        public TaskCard card;
        public List<Assignee> assignees = new List<Assignee>();
        public string version = "1.0";
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
        public List<ImportFieldMappingProfile> importMappingProfiles = new List<ImportFieldMappingProfile>();
        [NonSerialized] public List<ThemeData> themes = new List<ThemeData>();
        public int lastBoardIndex;
        [NonSerialized] public int currentThemeIndex;
        [NonSerialized] public ThemeSaveData themeSettings;

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
            importMappingProfiles ??= new List<ImportFieldMappingProfile>();
            themes ??= new List<ThemeData>();
            if (themes.Count == 0)
            {
                themes.Add(ThemeData.CreateDefault());
            }
            foreach (var t in themes)
            {
                if (t.name == "ThatFruitCompany Glassmorphism" || t.name == "Apple Glassmorphism")
                {
                    t.name = "Fruit Company Glassmorphism";
                }
                t.Normalize();
            }
            currentThemeIndex = Mathf.Clamp(currentThemeIndex, 0, themes.Count - 1);

            if (templates.Count == 0) AddDefaultTemplates();

            boards = boards
                .Where(b => b != null && !string.IsNullOrWhiteSpace(b.name))
                .GroupBy(b => b.id)
                .Select(g => g.First())
                .GroupBy(b => b.name)
                .Select(g => g.First())
                .ToList();

            if (boards.Count == 0)
            {
                boards.Add(new TaskBoard("My First Board"));
            }

            categories = categories
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            foreach (var board in boards)
            {
                board.columns ??= new List<TaskColumn>();
                board.columns = board.columns.Where(c => c != null).ToList();

                foreach (var column in board.columns)
                {
                    column.cards ??= new List<TaskCard>();
                    column.cards = column.cards.Where(c => c != null).ToList();
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

            importMappingProfiles = importMappingProfiles
                .Where(x => x != null)
                .Select(x =>
                {
                    x.Normalize();
                    return x;
                })
                .ToList();

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
