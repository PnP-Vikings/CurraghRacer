using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    //Card Detail Script
    public class CardDetailWindow : EditorWindow
    {
        [SerializeField] private TaskCard _card;
        [SerializeField] private TaskCard _originalCard;
        private Action _onChanged;
        private Action _onDelete;
        private Action<TaskCard> _onCreated;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private string _newChecklistItem = "";
        [SerializeField] private bool _isNewCard;
        [SerializeField] private List<string> _categories;
        private SaveData _saveData;
        [SerializeField] private string _newCategory = "";
        [SerializeField] private bool _dirty;
        [SerializeField] private bool _closedExplicitly;
        [SerializeField] private string _boardId;
        [SerializeField] private string _columnId;
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        private bool _shouldFocusTitle;
        private bool _shouldFocusChecklist;
        [SerializeField] private bool _showArchivedInPicker;

        public static CardDetailWindow Instance { get; private set; }

        public bool HasUnsavedChanges()
        {
            if (_card == null) return false;
            return _isNewCard ? IsNewCardDirty() : _dirty;
        }

        // ── Open existing card ──
        public static void Show(TaskCard card, SaveData saveData, string boardId, string columnId, Action onChanged, Action onDelete)
        {
            var existingWin = Instance != null ? Instance : Resources.FindObjectsOfTypeAll<CardDetailWindow>().FirstOrDefault(w => w != null);
            if (existingWin != null && existingWin.HasUnsavedChanges())
            {
                // If it's the exact same card, just bring it to focus
                if (!existingWin._isNewCard && existingWin._card != null && existingWin._card.id == card.id)
                {
                    existingWin.Focus();
                    return;
                }

                string cardName = !string.IsNullOrWhiteSpace(existingWin._card?.title)
                    ? $"\"{existingWin._card.title}\""
                    : (existingWin._isNewCard ? "the new card" : "the open card");

                string message = $"You have unsaved changes on {cardName}.\nDo you want to discard these changes and open \"{card.title}\"?";
                if (!ThemedDialog.Show("Discard Changes?", message, "Discard", "Cancel"))
                {
                    existingWin.Focus();
                    return;
                }

                existingWin._dirty = false;
                existingWin._closedExplicitly = true;
            }

            var win = existingWin != null ? existingWin : GetWindow<CardDetailWindow>(true, $"{TBStyles.CardDetailIcon} Card Details", true);
            win.titleContent = new GUIContent($"{TBStyles.CardDetailIcon} Card Details");
            win._originalCard = card;
            win._card = card.Clone(false);
            win._saveData = saveData ?? Persistence.Load();
            win._boardId = boardId;
            win._columnId = columnId;
            win._categories = win._saveData != null ? win._saveData.categories : new List<string>();
            win._onChanged = onChanged;
            win._onDelete = onDelete;
            win._onCreated = null;
            win._isNewCard = false;
            win._dirty = false;
            win._closedExplicitly = false;
            win._scroll = Vector2.zero;
            win._newChecklistItem = "";
            win._newCategory = "";
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win._shouldFocusTitle = false;
            win.ShowUtility();
            win.Focus();
        }

        // ── Open to create a NEW card ──
        public static void ShowNew(SaveData saveData, string boardId, string columnId, Action<TaskCard> onCreated)
        {
            var existingWin = Instance != null ? Instance : Resources.FindObjectsOfTypeAll<CardDetailWindow>().FirstOrDefault(w => w != null);
            if (existingWin != null && existingWin.HasUnsavedChanges())
            {
                string cardName = !string.IsNullOrWhiteSpace(existingWin._card?.title)
                    ? $"\"{existingWin._card.title}\""
                    : (existingWin._isNewCard ? "the new card" : "the open card");

                string message = $"You have unsaved changes on {cardName}.\nDo you want to discard these changes and create a new card?";
                if (!ThemedDialog.Show("Discard Changes?", message, "Discard", "Cancel"))
                {
                    existingWin.Focus();
                    return;
                }

                existingWin._dirty = false;
                existingWin._closedExplicitly = true;
            }

            var win = existingWin != null ? existingWin : GetWindow<CardDetailWindow>(true, $"{TBStyles.NewCardIcon} New Card", true);
            win.titleContent = new GUIContent($"{TBStyles.NewCardIcon} New Card");
            win._card = new TaskCard("") { description = "" };
            win._saveData = saveData ?? Persistence.Load();
            win._boardId = boardId;
            win._columnId = columnId;
            win._categories = win._saveData != null ? win._saveData.categories : new List<string>();
            win._onCreated = onCreated;
            win._onChanged = null;
            win._onDelete = null;
            win._isNewCard = true;
            win._originalCard = null;
            win._newChecklistItem = "";
            win._newCategory = "";
            win._dirty = false;
            win._closedExplicitly = false;
            win._scroll = Vector2.zero;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win._shouldFocusTitle = true;
            win.ShowUtility();
            win.Focus();
        }

        private void OpenURL(string url)
        {
            EditorApplication.delayCall += () =>
            {
                if (ThemedDialog.Show("Open URL", $"Open this link in your browser?\n\n{url}", "Open", "Cancel"))
                {
                    Application.OpenURL(url);
                }
            };
        }

        public void LoadData()
        {
            var freshData = Persistence.Load();
            if (freshData == null) return;
            _saveData = freshData;
            _categories = _saveData.categories;
            
            // Update _originalCard reference in case it became stale after a global reload
            if (_card != null)
            {
                foreach(var board in _saveData.boards)
                {
                    foreach(var col in board.columns)
                    {
                        var found = col.cards.FirstOrDefault(c => c.id == _card.id);
                        if (found != null)
                        {
                            _originalCard = found;
                            break;
                        }
                    }
                }
            }

            RefreshVisualState();
        }

        private void OnEnable()
        {
            Instance = this;
            wantsMouseMove = true;
            LoadData();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_closedExplicitly) return;

            bool hasChanges = HasUnsavedChanges();
            if (!hasChanges) return;

            // User closed the window via the window 'X' button with unsaved modifications.
            // Capture state before this EditorWindow is destroyed.
            bool isNew = _isNewCard;
            TaskCard card = _card != null ? _card.Clone(false) : null;
            TaskCard origCard = _originalCard;
            SaveData data = _saveData;
            string bId = _boardId;
            string cId = _columnId;
            List<string> cats = _categories != null ? new List<string>(_categories) : null;
            Action changed = _onChanged;
            Action del = _onDelete;
            Action<TaskCard> created = _onCreated;
            Vector2 sc = _scroll;
            string newCheck = _newChecklistItem;
            string newCat = _newCategory;

            if (card == null) return;

            EditorApplication.delayCall += () =>
            {
                string title = isNew ? "Unsaved New Card" : "Unsaved Changes";
                string message = isNew
                    ? "You have unsaved changes. Do you want to create this card before closing?"
                    : $"You have unsaved changes to \"{card.title}\". Do you want to save them before closing?";
                string saveBtn = isNew ? "Create" : "Save";

                int choice = ThemedDialog.ShowComplex(title, message, saveBtn, "Cancel", "Discard");

                if (choice == 0) // Save / Create
                {
                    if (isNew)
                    {
                        if (string.IsNullOrWhiteSpace(card.title))
                        {
                            ThemedDialog.Show("Title Required", "A card requires a title to be saved. Please enter a title before saving, or choose 'Discard' to exit without creating the card.", "OK");
                            ReopenWindow(card, origCard, data, bId, cId, cats, changed, del, created, isNew, sc, newCheck, newCat);
                            return;
                        }

                        PerformCreateCard(card, data, bId, cId, created);
                    }
                    else
                    {
                        PerformSaveExistingCard(card, origCard, data, bId, cId, changed);
                    }
                }
                else if (choice == 1) // Cancel (Keep editing)
                {
                    ReopenWindow(card, origCard, data, bId, cId, cats, changed, del, created, isNew, sc, newCheck, newCat);
                }
                else if (choice == 2) // Discard
                {
                    if (!isNew && origCard != null)
                    {
                        string json = JsonUtility.ToJson(origCard);
                        JsonUtility.FromJsonOverwrite(json, card);
                    }
                }
            };
        }

        private void RefreshVisualState()
        {
            TBStyles.InvalidateCache();
            titleContent = new GUIContent(_isNewCard ? $"{TBStyles.NewCardIcon} New Card" : $"{TBStyles.CardDetailIcon} Card Details");
            Repaint();
            EditorApplication.delayCall += () =>
            {
                if (this != null) Repaint();
            };
        }

        private void OnGUI()
        {
            try
            {
                if (_card == null) { Close(); return; }

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.CardDetailBg, true);
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    if (GUIUtility.keyboardControl != 0)
                    {
                        GUIUtility.keyboardControl = 0;
                        Repaint();
                    }
                    else
                    {
                        Event.current.Use();
                        AttemptClose();
                        return;
                    }
                }

                _hasAnimatedGif = false;

                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scope.scrollPosition;

            // ── Unsaved changes detected banner ──
            bool hasChanges = _isNewCard ? IsNewCardDirty() : _dirty;
            if (hasChanges)
            {
                using (var unsavedScope = new EditorGUILayout.HorizontalScope(TBStyles.GlassItemBox, GUILayout.Height(24)))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color bannerBg = EditorGUIUtility.isProSkin 
                            ? new Color(0.9f, 0.6f, 0.1f, 0.15f) 
                            : new Color(0.9f, 0.6f, 0.1f, 0.22f);
                        Color bannerBorder = EditorGUIUtility.isProSkin 
                            ? new Color(1f, 0.75f, 0.2f, 0.5f) 
                            : new Color(0.85f, 0.55f, 0.1f, 0.6f);
                        TBStyles.DrawGlassPanel(unsavedScope.rect, bannerBg, bannerBorder, false);
                    }

                    var unsavedStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.85f, 0.45f) : new Color(0.65f, 0.38f, 0.05f) },
                        alignment = TextAnchor.MiddleLeft
                    };
                    ThemedTooltip.Label("⚠️ Unsaved changes detected", "This card has unsaved modifications", unsavedStyle, GUILayout.ExpandWidth(true));
                }
                GUILayout.Space(6);
            }

            // ── Color label bar ──
            var labelColor = TBStyles.GetLabelColor(_card.colorLabel);
            if(labelColor != TBStyles.GetLabelColor(0))
            {
                var barRect = GUILayoutUtility.GetRect(0, 6, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(barRect, labelColor);
            }
            GUILayout.Space(10);

            // ── Title ──
            EditorGUILayout.LabelField(_isNewCard ? "Card Title" : "Title", EditorStyles.boldLabel);
            GUI.SetNextControlName("CardTitleField");
            string newTitle = EditorGUILayout.TextField(_card.title, TBStyles.GlassTextField, GUILayout.Height(22));
            if (newTitle != _card.title) { _card.title = newTitle; MarkDirty(); }

            if (_shouldFocusTitle)
            {
                _shouldFocusTitle = false;
                GUI.FocusControl("CardTitleField");
                EditorGUI.FocusTextInControl("CardTitleField");
            }
            GUILayout.Space(8);

            // ── Category ──
            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var catOptions = new List<string> { "None" };
                catOptions.AddRange(_categories);
                int currentIdx = 0;
                if (!string.IsNullOrEmpty(_card.category))
                {
                    int found = catOptions.IndexOf(_card.category);
                    if (found >= 0) currentIdx = found;
                }
                TBStyles.DrawThemedDropdown(currentIdx, catOptions.ToArray(), (newIdx) =>
                {
                    string picked = newIdx == 0 ? "" : catOptions[newIdx];
                    if (picked != (_card.category ?? ""))
                    {
                        _card.category = picked;
                        // Auto-apply default color for this category
                        if (!string.IsNullOrEmpty(picked) && _saveData != null)
                        {
                            int defaultColor = _saveData.GetCategoryColor(picked);
                            if (defaultColor > 0)
                                _card.colorLabel = defaultColor;
                        }
                        MarkDirty();
                        Repaint();
                    }
                }, TBStyles.StandardDropdown, "Select task category", GUILayout.ExpandWidth(true), GUILayout.Height(20));

                GUILayout.Space(4);

                // Add new category
                _newCategory = EditorGUILayout.TextField(_newCategory, TBStyles.GlassTextField, GUILayout.Width(75), GUILayout.Height(20));
                if (ThemedTooltip.IconButton("+", "Add New Category", GUILayout.Width(20), GUILayout.Height(20)) && !string.IsNullOrWhiteSpace(_newCategory))
                {
                    string nc = _newCategory.Trim();
                    if (!_categories.Contains(nc))
                        _categories.Add(nc);
                    _card.category = nc;
                    int defaultColor = _saveData != null ? _saveData.GetCategoryColor(nc) : 0;
                    if (defaultColor > 0) _card.colorLabel = defaultColor;
                    _newCategory = "";
                    MarkDirty();
                    GUIUtility.ExitGUI();
                }
                // Remove selected category from the global list
                if (!string.IsNullOrEmpty(_card.category) && ThemedTooltip.DeleteIconButton(TBStyles.DeleteIcon, "Delete Category", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (ThemedDialog.Show("Remove Category",
                            $"Remove \"{_card.category}\" from the category list?\n(Cards already using it will keep it as text.)",
                            "Remove", "Cancel"))
                        {
                            _categories.Remove(_card.category);
                            _card.category = "";
                            MarkDirty();
                            Repaint();
                        }
                    };
                }
            }
            GUILayout.Space(8);

            // ── Description ──
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            string newDesc = EditorGUILayout.TextArea(_card.description, TBStyles.CardTextArea, GUILayout.MinHeight(70));
            if (newDesc != _card.description) { _card.description = newDesc; MarkDirty(); }
            GUILayout.Space(8);

            // ── Color Label & Priority ──
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Color Label", EditorStyles.boldLabel);
                    TBStyles.DrawThemedDropdown(_card.colorLabel, TBStyles.LabelNames, (newColor) =>
                    {
                        if (newColor != _card.colorLabel) { _card.colorLabel = newColor; MarkDirty(); Repaint(); }
                    }, TBStyles.StandardDropdown, TBStyles.GetLabelColorsArray(), "Select card color label", GUILayout.ExpandWidth(true), GUILayout.Height(20));
                }
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Priority", EditorStyles.boldLabel);
                    TBStyles.DrawThemedDropdown(_card.priority, TBStyles.GetPriorityDisplayNames(), (newPri) =>
                    {
                        if (newPri != _card.priority) { _card.priority = newPri; MarkDirty(); Repaint(); }
                    }, TBStyles.StandardDropdown, "Select task priority", GUILayout.ExpandWidth(true), GUILayout.Height(20));
                }
            }
            GUILayout.Space(12);

            // ── Assignees ──
            EditorGUILayout.LabelField("Assignees", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_card.assigneeIds != null)
                {
                    foreach (var id in _card.assigneeIds.ToList())
                    {
                        var assignee = _saveData.assignees.FirstOrDefault(a => a.id == id);
                        if (assignee != null)
                        {
                            DrawAssigneeCircle(assignee, true);
                            GUILayout.Space(4);
                        }
                        else
                        {
                            _card.assigneeIds.Remove(id);
                        }
                    }
                }

                if (ThemedContextMenu.DropdownButton("+", "Add Assignee", TBStyles.IconButton, out Rect btnRect, GUILayout.Width(22), GUILayout.Height(22)))
                {
                    ShowAssigneePicker(btnRect);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(8);

            // ── Set as default color for this category ──
            if (!string.IsNullOrEmpty(_card.category) && _saveData != null)
            {
                int currentDefault = _saveData.GetCategoryColor(_card.category);
                string btnLabel = currentDefault == _card.colorLabel
                    ? $"✔ \"{_card.category}\" default = {TBStyles.LabelNames[_card.colorLabel]}"
                    : $"Set {TBStyles.LabelNames[_card.colorLabel]} as default for \"{_card.category}\"";
                if (GUILayout.Button(btnLabel, TBStyles.StandardButton, GUILayout.Height(20)))
                {
                    _saveData.SetCategoryColor(_card.category, _card.colorLabel);
                    MarkDirty();
                }
            }
            GUILayout.Space(8);

            // ── Completed Toggle ──
            using (new EditorGUILayout.HorizontalScope())
            {
                var compStyle = new GUIStyle(EditorStyles.boldLabel);
                if (_card.completed) compStyle.normal = new GUIStyleState { textColor = TBStyles.StatusCompletedColor };
                EditorGUILayout.LabelField(_card.completed ? $"{TBStyles.CompletedIcon} Completed" : "Status", compStyle, GUILayout.Width(100));

                if (GUILayout.Button(_card.completed ? "Mark Incomplete" : "Mark Complete", TBStyles.StandardButton, GUILayout.Height(22)))
                {
                    _card.completed = !_card.completed;
                    if (_saveData != null) _saveData.SyncLinkedChecklistItems(_card.id, _card.completed);
                    _dirty = true;
                }
            }
            GUILayout.Space(4);

            // ── Due Date ──
            EditorGUILayout.LabelField("Due Date", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                // Parse existing date or show empty
                bool hasDueDate = !string.IsNullOrWhiteSpace(_card.dueDate) && DateTime.TryParse(_card.dueDate, out _);

                if (hasDueDate)
                {
                    DateTime parsed = DateTime.Parse(_card.dueDate);
                    int daysUntil = (parsed.Date - DateTime.Today).Days;
                    string statusIcon, statusText;
                    Color statusCol;
                    if (_card.completed)
                    {
                        statusIcon = TBStyles.CompletedIcon;
                        statusText = $"Completed (was due {parsed:MMM dd})";
                        statusCol = TBStyles.StatusCompletedColor;
                    }
                    else
                    {
                        statusIcon = daysUntil < 0 ? TBStyles.OverdueIcon : daysUntil == 0 ? TBStyles.DueTodayIcon : daysUntil <= 3 ? TBStyles.DueSoonIcon : TBStyles.DueDateIcon;
                        statusText = daysUntil < 0 ? $"Overdue by {-daysUntil}d"
                            : daysUntil == 0 ? "Due today!"
                            : daysUntil <= 3 ? $"Due in {daysUntil}d"
                            : $"Due {parsed:MMM dd, yyyy}";
                        statusCol = daysUntil < 0 ? TBStyles.StatusOverdueColor
                            : daysUntil == 0 ? TBStyles.StatusDueTodayColor
                            : daysUntil <= 3 ? TBStyles.StatusDueSoonColor
                            : TBStyles.CardDetailsTextColor;
                    }

                    var dateStatusStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = statusCol } };
                    EditorGUILayout.LabelField($"{statusIcon} {statusText}", dateStatusStyle, GUILayout.Width(180));
                }

                // Year / Month / Day dropdowns
                int year, month, day;
                if (hasDueDate)
                {
                    var d = DateTime.Parse(_card.dueDate);
                    year = d.Year; month = d.Month; day = d.Day;
                }
                else
                {
                    var t = DateTime.Today;
                    year = t.Year; month = t.Month; day = t.Day;
                }

                EditorGUILayout.LabelField("Y:", GUILayout.Width(16));
                int newYear = EditorGUILayout.IntField(year, TBStyles.DateInputField, GUILayout.Width(50), GUILayout.Height(20));
                EditorGUILayout.LabelField("M:", GUILayout.Width(18));
                string[] monthNames = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
                int newMonth = month;
                TBStyles.DrawThemedDropdown(month - 1, monthNames, (m) =>
                {
                    int chosenMonth = m + 1;
                    if (chosenMonth != month)
                    {
                        try
                        {
                            int maxD = DateTime.DaysInMonth(Mathf.Clamp(newYear, 1, 9999), Mathf.Clamp(chosenMonth, 1, 12));
                            int safeD = Mathf.Clamp(day, 1, maxD);
                            _card.dueDate = new DateTime(newYear, chosenMonth, safeD).ToString("yyyy-MM-dd");
                            MarkDirty();
                            Repaint();
                        }
                        catch { }
                    }
                }, TBStyles.StandardDropdown, "Select due month", GUILayout.Width(52), GUILayout.Height(20));
                EditorGUILayout.LabelField("D:", GUILayout.Width(16));
                int maxDay = DateTime.DaysInMonth(Mathf.Clamp(newYear, 1, 9999), Mathf.Clamp(newMonth, 1, 12));
                int newDay = EditorGUILayout.IntField(Mathf.Clamp(day, 1, maxDay), TBStyles.DateInputField, GUILayout.Width(34), GUILayout.Height(20));
                newDay = Mathf.Clamp(newDay, 1, maxDay);

                if (hasDueDate && (newYear != year || newDay != day))
                {
                    try
                    {
                        _card.dueDate = new DateTime(newYear, newMonth, newDay).ToString("yyyy-MM-dd");
                        MarkDirty();
                    }
                    catch { /* invalid date combo, ignore */ }
                }

                if (!hasDueDate)
                {
                    if (GUILayout.Button("Set Date", TBStyles.StandardButton, GUILayout.Width(66), GUILayout.Height(20)))
                    {
                        _card.dueDate = new DateTime(newYear, newMonth, newDay).ToString("yyyy-MM-dd");
                        MarkDirty();
                    }
                }
                else
                {
                    if (ThemedTooltip.IconButton(TBStyles.CancelIcon, "Clear Due Date", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _card.dueDate = "";
                        MarkDirty();
                    }
                }
            }

            // Quick-set buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                if (GUILayout.Button("Today", TBStyles.StandardButton, GUILayout.Width(50), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1d", TBStyles.StandardButton, GUILayout.Width(36), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+3d", TBStyles.StandardButton, GUILayout.Width(36), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1w", TBStyles.StandardButton, GUILayout.Width(36), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+2w", TBStyles.StandardButton, GUILayout.Width(36), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1m", TBStyles.StandardButton, GUILayout.Width(38), GUILayout.Height(20)))
                { _card.dueDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"); MarkDirty(); }
            }

            GUILayout.Space(4);

            if (!_isNewCard)
                EditorGUILayout.LabelField($"Created: {_card.createdDate}", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // ── Checklist ──
            EditorGUILayout.LabelField($"{TBStyles.ChecklistIcon} Checklist", EditorStyles.boldLabel);
            for (int i = 0; i < _card.checklistItems.Count; i++)
            {
                using (var itemScope = new EditorGUILayout.HorizontalScope(TBStyles.GlassItemBox, GUILayout.Height(24)))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color itemBg = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.45f);
                        Color itemBorder = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f);
                        TBStyles.DrawGlassPanel(itemScope.rect, itemBg, itemBorder, false);
                    }

                    bool done = TBStyles.DrawThemedCheckbox(_card.checklistStates[i], _card.checklistStates[i] ? "Mark item incomplete" : "Mark item complete", GUILayout.Width(18), GUILayout.Height(20));
                    if (done != _card.checklistStates[i]) 
                    { 
                        _card.checklistStates[i] = done; 
                        MarkDirty(); 
                        
                        // Reverse sync: if this checklist item is linked to a card, update that card's completion status
                        if (i < _card.checklistLinkedCardIds.Count && !string.IsNullOrEmpty(_card.checklistLinkedCardIds[i]))
                        {
                            var subId = _card.checklistLinkedCardIds[i];
                            var subCard = _saveData.AllCards().FirstOrDefault(c => c.id == subId);
                            if (subCard != null && subCard.completed != done)
                            {
                                subCard.completed = done;
                                _saveData.SyncLinkedChecklistItems(subId, done);
                            }
                        }
                    }

                    var style = new GUIStyle(TBStyles.GlassTextField);
                    if (done) style.fontStyle = FontStyle.Italic;
                    string itemText = EditorGUILayout.TextField(_card.checklistItems[i], style, GUILayout.Height(20));
                    if (itemText != _card.checklistItems[i]) { _card.checklistItems[i] = itemText; MarkDirty(); }

                    // Linked card indicator/picker
                    string linkedCardId = _card.checklistLinkedCardIds[i];
                    string linkToolTip = "Link to another card as a subtask";
                    string linkLabel = TBStyles.ChildLinkIcon;
                    
                    if (!string.IsNullOrEmpty(linkedCardId))
                    {
                        var linkedCard = _saveData.AllCards().FirstOrDefault(c => c.id == linkedCardId);
                        if (linkedCard != null)
                        {
                            linkLabel = $"{TBStyles.ChildLinkIcon} " + TBStyles.TruncateString(linkedCard.title, 15);
                            linkToolTip = $"Linked to: {linkedCard.title}\nClick to change or remove link.";
                        }
                        else
                        {
                            _card.checklistLinkedCardIds[i] = string.Empty; // clean up broken link
                        }
                    }

                    if (ThemedTooltip.Button(linkLabel, linkToolTip, TBStyles.StandardButton, GUILayout.Width(string.IsNullOrEmpty(linkedCardId) ? 26 : 100), GUILayout.Height(20)))
                    {
                        ShowCardPickerMenu(i);
                    }

                    EditorGUI.BeginDisabledGroup(i == 0);
                    if (ThemedTooltip.IconButton(TBStyles.MoveUpIcon, "Move Up", GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        var item = _card.checklistItems[i];
                        var state = _card.checklistStates[i];
                        var linkedId = _card.checklistLinkedCardIds[i];
                        _card.checklistItems.RemoveAt(i);
                        _card.checklistStates.RemoveAt(i);
                        _card.checklistLinkedCardIds.RemoveAt(i);
                        _card.checklistItems.Insert(i - 1, item);
                        _card.checklistStates.Insert(i - 1, state);
                        _card.checklistLinkedCardIds.Insert(i - 1, linkedId);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(i == _card.checklistItems.Count - 1);
                    if (ThemedTooltip.IconButton(TBStyles.MoveDownIcon, "Move Down", GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        var item = _card.checklistItems[i];
                        var state = _card.checklistStates[i];
                        var linkedId = _card.checklistLinkedCardIds[i];
                        _card.checklistItems.RemoveAt(i);
                        _card.checklistStates.RemoveAt(i);
                        _card.checklistLinkedCardIds.RemoveAt(i);
                        _card.checklistItems.Insert(i + 1, item);
                        _card.checklistStates.Insert(i + 1, state);
                        _card.checklistLinkedCardIds.Insert(i + 1, linkedId);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUI.EndDisabledGroup();

                    if (ThemedTooltip.DeleteIconButton(TBStyles.CancelIcon, "Remove Checklist Item", GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        _card.checklistItems.RemoveAt(i);
                        _card.checklistStates.RemoveAt(i);
                        _card.checklistLinkedCardIds.RemoveAt(i);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (var addScope = new EditorGUILayout.HorizontalScope(TBStyles.GlassItemBox, GUILayout.Height(24)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    Color itemBg = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.04f) : new Color(1f, 1f, 1f, 0.35f);
                    Color itemBorder = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f);
                    TBStyles.DrawGlassPanel(addScope.rect, itemBg, itemBorder, false);
                }

                bool enterPressed = Event.current.type == EventType.KeyDown && 
                                   (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                                   GUI.GetNameOfFocusedControl() == "NewChecklistItemField";

                GUI.SetNextControlName("NewChecklistItemField");
                _newChecklistItem = EditorGUILayout.TextField(_newChecklistItem, TBStyles.GlassTextField, GUILayout.Height(20));

                if (_shouldFocusChecklist)
                {
                    _shouldFocusChecklist = false;
                    GUI.FocusControl("NewChecklistItemField");
                    EditorGUI.FocusTextInControl("NewChecklistItemField");
                }

                if ((ThemedTooltip.IconButton("+", "Add Checklist Item", GUILayout.Width(22), GUILayout.Height(20)) || enterPressed) && !string.IsNullOrWhiteSpace(_newChecklistItem))
                {
                    _card.checklistItems.Add(_newChecklistItem.Trim());
                    _card.checklistStates.Add(false);
                    _card.checklistLinkedCardIds.Add(string.Empty);
                    _newChecklistItem = "";
                    MarkDirty();
                    if (enterPressed)
                    {
                        Event.current.Use();
                    }
                    _shouldFocusChecklist = true;
                    Repaint();
                }
            }

            GUILayout.Space(16);

            // ── Linked Assets & Notes ──
            EditorGUILayout.LabelField($"{TBStyles.UrlIcon} Linked Items (Assets, Scene, Notes, URLs)", EditorStyles.boldLabel);
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                Color dropBg = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.04f) : new Color(1f, 1f, 1f, 0.40f);
                Color dropBorder = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.12f);
                TBStyles.DrawGlassPanel(dropRect, dropBg, dropBorder, false);
            }
            GUI.Label(dropRect, "Drag & Drop Assets, Scene Objects or Notes here", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            
            Event currentEvent = Event.current;
            if (dropRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    // 1. Handle Object References (Assets, Scene Objects)
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (AssetDatabase.Contains(draggedObject))
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            string guid = AssetDatabase.AssetPathToGUID(path);
                            if (!string.IsNullOrEmpty(guid) && !_card.linkedItems.Any(li => !li.isSceneObject && !li.isNote && !li.isUrl && li.guid == guid))
                            {
                                _card.linkedItems.Add(new LinkedItem(guid));
                                MarkDirty();
                            }
                        }
                        else if (draggedObject is GameObject go && go.scene.IsValid())
                        {
                            string scenePath = go.scene.path;
                            string gid = GlobalObjectId.GetGlobalObjectIdSlow(draggedObject).ToString();
                            if (!_card.linkedItems.Any(li => li.isSceneObject && li.sceneObject != null && li.sceneObject.globalObjectId == gid))
                            {
                                _card.linkedItems.Add(new LinkedItem(new SceneObjectReference(scenePath, gid, go.name)));
                                MarkDirty();
                            }
                        }
                    }

                    // 2. Handle Notes (Generic Data)
                    var noteId = DragAndDrop.GetGenericData("AwesomeTaskNoteId") as string;
                    if (!string.IsNullOrEmpty(noteId))
                    {
                        if (!_card.linkedItems.Any(li => li.isNote && li.guid == noteId))
                        {
                            _card.linkedItems.Add(LinkedItem.CreateNote(noteId));
                            MarkDirty();
                        }
                    }
                    currentEvent.Use();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"{TBStyles.UrlIcon} Add URL", TBStyles.StandardButton, GUILayout.Height(24)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string res = EditorInputDialog.Show("Add Link", "Paste URL here:", "https://");
                        if (!string.IsNullOrEmpty(res) && res.StartsWith("http"))
                        {
                            _card.linkedItems.Add(LinkedItem.CreateUrl(res));
                            MarkDirty();
                            Repaint();
                        }
                    };
                }
                if (ThemedContextMenu.DropdownButton($"{TBStyles.NotesTabIcon} Link Note", TBStyles.StandardButton, out Rect linkNoteBtnRect, GUILayout.Height(24)))
                {
                    ThemedContextMenu menu = new ThemedContextMenu();
                    if (_saveData != null && _saveData.notes.Count > 0)
                    {
                        var folders = _saveData.noteFolders;

                        // 1. Notes in Folders
                        foreach (var folder in folders)
                        {
                            var notesInFolder = _saveData.notes.Where(n => n.folderId == folder.id).OrderBy(n => n.title).ToList();
                            if (notesInFolder.Count == 0) continue;

                            foreach (var note in notesInFolder)
                            {
                                string noteId = note.id;
                                string noteTitle = string.IsNullOrEmpty(note.title) ? "Untitled" : note.title;
                                string fullPath = $"{folder.name}/{noteTitle}";

                                menu.AddItem(new GUIContent(fullPath), false, () =>
                                {
                                    if (!_card.linkedItems.Any(li => li.isNote && li.guid == noteId))
                                    {
                                        _card.linkedItems.Add(LinkedItem.CreateNote(noteId));
                                        MarkDirty();
                                    }
                                });
                            }
                        }

                        // 2. Unfiled Notes
                        var unfiledNotes = _saveData.notes
                            .Where(n => string.IsNullOrEmpty(n.folderId) || !folders.Any(f => f.id == n.folderId))
                            .OrderBy(n => n.title)
                            .ToList();

                        if (unfiledNotes.Count > 0)
                        {
                            if (folders.Count > 0) menu.AddSeparator("");

                            foreach (var note in unfiledNotes)
                            {
                                string noteId = note.id;
                                string noteTitle = string.IsNullOrEmpty(note.title) ? "Untitled" : note.title;

                                menu.AddItem(new GUIContent(noteTitle), false, () =>
                                {
                                    if (!_card.linkedItems.Any(li => li.isNote && li.guid == noteId))
                                    {
                                        _card.linkedItems.Add(LinkedItem.CreateNote(noteId));
                                        MarkDirty();
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("No notes found"));
                    }
                    menu.Show(linkNoteBtnRect);
                }
            }
            GUILayout.Space(4);

            for (int i = 0; i < _card.linkedItems.Count; i++)
            {
                var item = _card.linkedItems[i];
                using (var rowScope = new EditorGUILayout.HorizontalScope(TBStyles.GlassItemBox))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color rowBg = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.45f);
                        Color rowBorder = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f);
                        TBStyles.DrawGlassPanel(rowScope.rect, rowBg, rowBorder, false);
                    }

                    if (item.isSceneObject)
                    {
                        var sceneRef = item.sceneObject;
                        string sceneName = Path.GetFileNameWithoutExtension(sceneRef.scenePath);
                        var sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
                        if (GUILayout.Button(sceneIcon, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            EditorApplication.delayCall += () => HandleSceneObjectClick(sceneRef);
                        }
                        string displayLabel = $"[{sceneName}] {sceneRef.name}";
                        if (ThemedTooltip.Button(TBStyles.TruncateString(displayLabel, 50), displayLabel, EditorStyles.label))
                        {
                            EditorApplication.delayCall += () => HandleSceneObjectClick(sceneRef);
                        }
                    }
                    else if (item.isNote)
                    {
                        var note = _saveData?.notes.FirstOrDefault(n => n.id == item.guid);
                        var noteIcon = EditorGUIUtility.IconContent("TextAsset Icon").image;
                        if (GUILayout.Button(noteIcon, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            if (note != null) NotePopupWindow.Open(note, _saveData, MarkDirty);
                        }
                        string noteTitle = note != null ? note.title : "Missing Note";
                        string displayLabel = $"[Note] {noteTitle}";
                        if (ThemedTooltip.Button(TBStyles.TruncateString(displayLabel, 50), displayLabel, EditorStyles.label))
                        {
                            if (note != null) NotePopupWindow.Open(note, _saveData, MarkDirty);
                        }
                    }
                    else if (item.isUrl)
                    {
                        var urlIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
                        if (GUILayout.Button(urlIcon, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            OpenURL(item.url);
                        }
                        string label = string.IsNullOrEmpty(item.displayName) ? item.url : item.displayName;
                        if (ThemedTooltip.Button(TBStyles.TruncateString(label, 50), label, EditorStyles.label))
                        {
                            OpenURL(item.url);
                        }
                    }
                    else
                    {
                        string guid = item.guid;
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                        if (obj != null)
                        {
                            if (GUILayout.Button(EditorGUIUtility.ObjectContent(obj, obj.GetType()).image, GUILayout.Width(20), GUILayout.Height(20)))
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                                EditorUtility.FocusProjectWindow();
                            }
                            if (ThemedTooltip.Button(TBStyles.TruncateString(obj.name, 50), obj.name, EditorStyles.label))
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                                EditorUtility.FocusProjectWindow();
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Missing Asset", EditorStyles.label);
                        }
                    }

                    if (i > 0 && ThemedTooltip.IconButton(TBStyles.MoveUpIcon, "Move Up", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _card.linkedItems.RemoveAt(i); _card.linkedItems.Insert(i - 1, item);
                        MarkDirty(); GUIUtility.ExitGUI();
                    }
                    if (i < _card.linkedItems.Count - 1 && ThemedTooltip.IconButton(TBStyles.MoveDownIcon, "Move Down", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _card.linkedItems.RemoveAt(i); _card.linkedItems.Insert(i + 1, item);
                        MarkDirty(); GUIUtility.ExitGUI();
                    }

                    if (ThemedTooltip.DeleteIconButton(TBStyles.CancelIcon, "Remove Link", GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _card.linkedItems.RemoveAt(i);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            GUILayout.Space(16);

            // ── Image Attachment ──
            EditorGUILayout.LabelField($"{TBStyles.AttachmentIcon} Image / GIF", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(_card.imagePath))
                {
                    EditorGUILayout.LabelField($"🖼 {Path.GetFileName(_card.imagePath)}", EditorStyles.miniLabel);
                    if (ThemedTooltip.DeleteIconButton(TBStyles.CancelIcon, "Remove Image"))
                    {
                        _card.imagePath = "";
                        MarkDirty();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No image attached (Drag & Drop or Paste)", EditorStyles.miniLabel);
                }
                if (ThemedTooltip.Button("📋 Paste", "Paste Image from Clipboard", TBStyles.NoteActionButton, GUILayout.Width(64), GUILayout.Height(20)))
                {
                    string assetPath = MarkdownRenderer.TryPasteImageToProject();
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        _card.imagePath = assetPath;
                        MarkDirty();
                        Repaint();
                    }
                }
                if (ThemedTooltip.Button("📎 Browse…", "Attach Image", TBStyles.NoteActionButton, GUILayout.Width(76), GUILayout.Height(20)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string imgPath = EditorUtility.OpenFilePanel("Attach Image", "",
                            "png,jpg,jpeg,gif,bmp,tga,psd,tiff");
                        if (!string.IsNullOrEmpty(imgPath))
                        {
                            _card.imagePath = MarkdownRenderer.CopyImageToProject(imgPath);
                            MarkDirty();
                            Repaint();
                        }
                    };
                }
            }

            // Handle Drag & Drop / Paste for the image attachment
            if (Event.current.type != EventType.Layout)
            {
                var imageSectionRect = GUILayoutUtility.GetLastRect();
                HandleImageAttachmentEvents(imageSectionRect);
            }

            // Display image preview
            if (!string.IsNullOrEmpty(_card.imagePath))
            {
                GUILayout.Space(4);
                if (MarkdownRenderer.DrawImageThumbnail(_card.imagePath, 350f))
                {
                    _hasAnimatedGif = true;
                }
            }

            GUILayout.Space(16);

            // ── Bottom buttons ──
            if (_isNewCard)
            {
                if (GUILayout.Button($"{TBStyles.NewCardIcon}  Create Card", TBStyles.AddCardButton, GUILayout.Height(32)))
                {
                    if (string.IsNullOrWhiteSpace(_card.title))
                    {
                        ThemedDialog.Show("Title Required", "A card requires a title to be saved.", "OK");
                    }
                    else
                    {
                        _closedExplicitly = true;
                        SaveChanges();
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.Space(4);
                if (GUILayout.Button("Cancel", TBStyles.StandardButton, GUILayout.Height(24)))
                {
                    AttemptClose();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                // Save Changes button
                GUI.enabled = _dirty;
                if (GUILayout.Button(_dirty ? $"{TBStyles.SaveIcon}  Save Changes" : $"{TBStyles.CompletedIcon}  All Saved", _dirty ? TBStyles.AddCardButton : TBStyles.StandardButton, GUILayout.Height(30)))
                {
                    SaveChanges();
                }
                GUI.enabled = true;

                GUILayout.Space(8);

                // Delete button
                if (GUILayout.Button($"{TBStyles.DeleteIcon}  Delete Card", TBStyles.DeleteButton, GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (ThemedDialog.Show("Delete Card", $"Delete \"{_card.title}\"?", "Delete", "Cancel"))
                        {
                            _closedExplicitly = true;
                            _dirty = false;
                            if (_onDelete != null)
                            {
                                _onDelete.Invoke();
                            }
                            else if (TaskBoardWindow.Instance != null)
                            {
                                TaskBoardWindow.Instance.DeleteCardFromDetail(_boardId, _columnId, _card.id);
                            }
                            else
                            {
                                // Fallback: load and delete directly from disk
                                var data = Persistence.Load();
                                var board = data.boards.FirstOrDefault(b => b.id == _boardId);
                                if (board != null)
                                {
                                    var col = board.columns.FirstOrDefault(c => c.id == _columnId);
                                    if (col != null)
                                    {
                                        col.cards.RemoveAll(c => c.id == _card.id);
                                        Persistence.Save(data);
                                        TaskBoardWindow.ReloadAllOpenWindows();
                                    }
                                }
                            }

                            Close();
                        }
                    };
                }
            }

            } // end scroll view scope
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(this);
            }

            // Throttled repaint for GIF animation
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        public void AttemptClose()
        {
            bool hasChanges = HasUnsavedChanges();
            if (!hasChanges)
            {
                _closedExplicitly = true;
                Close();
                return;
            }

            _closedExplicitly = true;
            EditorApplication.delayCall += () =>
            {
                string title = _isNewCard ? "Unsaved New Card" : "Unsaved Changes";
                string message = _isNewCard
                    ? "You have unsaved changes. Do you want to create this card before closing?"
                    : $"You have unsaved changes to \"{_card.title}\". Do you want to save them before closing?";
                string saveBtn = _isNewCard ? "Create" : "Save";

                int choice = ThemedDialog.ShowComplex(title, message, saveBtn, "Cancel", "Discard");

                if (choice == 0) // Save / Create
                {
                    if (_isNewCard && string.IsNullOrWhiteSpace(_card.title))
                    {
                        ThemedDialog.Show("Title Required", "A card requires a title to be saved. Please enter a title before saving, or choose 'Discard' to exit without creating the card.", "OK");
                        _closedExplicitly = false;
                        return;
                    }

                    _closedExplicitly = true;
                    SaveChanges();
                    Close();
                }
                else if (choice == 2) // Discard
                {
                    if (!_isNewCard && _originalCard != null)
                    {
                        string json = JsonUtility.ToJson(_originalCard);
                        JsonUtility.FromJsonOverwrite(json, _card);
                    }
                    _dirty = false;
                    _closedExplicitly = true;
                    Close();
                }
                else // choice == 1 (Cancel) -> Keep editing
                {
                    _closedExplicitly = false;
                }
            };
        }

        public bool IsNewCardDirty()
        {
            if (_card == null) return false;
            if (!string.IsNullOrWhiteSpace(_card.title)) return true;
            if (!string.IsNullOrWhiteSpace(_card.description)) return true;
            if (!string.IsNullOrEmpty(_card.category)) return true;
            if (!string.IsNullOrEmpty(_card.imagePath)) return true;
            if (!string.IsNullOrWhiteSpace(_card.dueDate)) return true;
            if (_card.checklistItems != null && _card.checklistItems.Count > 0) return true;
            if (_card.colorLabel > 0) return true;
            if (_card.priority > 0) return true;
            if (_card.assigneeIds != null && _card.assigneeIds.Count > 0) return true;
            if (_card.linkedAssetGuids != null && _card.linkedAssetGuids.Count > 0) return true;
            if (_card.linkedSceneObjects != null && _card.linkedSceneObjects.Count > 0) return true;
            if (_card.linkedItems != null && _card.linkedItems.Count > 0) return true;
            if (!string.IsNullOrWhiteSpace(_newChecklistItem)) return true;
            if (!string.IsNullOrWhiteSpace(_newCategory)) return true;
            if (_dirty) return true;
            return false;
        }

        public override void SaveChanges()
        {
            if (_isNewCard)
            {
                if (!string.IsNullOrWhiteSpace(_card.title))
                {
                    PerformCreateCard(_card, _saveData, _boardId, _columnId, _onCreated);
                    _dirty = false;
                    base.SaveChanges();
                }
                else
                {
                    ThemedDialog.Show("Title Required", "A card requires a title to be saved. Please enter a title before saving, or choose 'Discard' to exit without creating the card.", "OK");
                }
            }
            else
            {
                if (_dirty && _card != null)
                {
                    PerformSaveExistingCard(_card, _originalCard, _saveData, _boardId, _columnId, _onChanged);
                    _dirty = false;
                }
                base.SaveChanges();
            }
        }

        private static void PerformCreateCard(TaskCard card, SaveData saveData, string boardId, string columnId, Action<TaskCard> onCreated)
        {
            if (onCreated != null)
            {
                onCreated.Invoke(card);
            }
            else if (TaskBoardWindow.Instance != null)
            {
                TaskBoardWindow.Instance.AddCardFromDetail(boardId, columnId, card);
            }
            else
            {
                var data = Persistence.Load() ?? saveData;
                if (data != null)
                {
                    var board = data.boards.FirstOrDefault(b => b.id == boardId);
                    if (board != null)
                    {
                        var col = board.columns.FirstOrDefault(c => c.id == columnId);
                        if (col != null)
                        {
                            col.cards.Add(card);
                            int bIdx = data.boards.IndexOf(board);
                            if (bIdx >= 0) data.lastBoardIndex = bIdx;
                            Persistence.Save(data);
                            TaskBoardWindow.ReloadAllOpenWindows();
                        }
                    }
                }
            }
        }

        private static void PerformSaveExistingCard(TaskCard card, TaskCard originalCard, SaveData saveData, string boardId, string columnId, Action onChanged)
        {
            var freshData = Persistence.Load() ?? saveData;
            if (freshData != null && card != null)
            {
                TaskCard targetCard = null;
                foreach (var b in freshData.boards)
                {
                    foreach (var c in b.columns)
                    {
                        var found = c.cards.FirstOrDefault(x => x.id == card.id);
                        if (found != null)
                        {
                            targetCard = found;
                            break;
                        }
                    }
                    if (targetCard != null) break;
                }

                if (targetCard != null)
                {
                    string json = JsonUtility.ToJson(card);
                    JsonUtility.FromJsonOverwrite(json, targetCard);
                }
                else if (originalCard != null)
                {
                    string json = JsonUtility.ToJson(card);
                    JsonUtility.FromJsonOverwrite(json, originalCard);
                }

                if (!string.IsNullOrEmpty(boardId))
                {
                    int bIdx = freshData.boards.FindIndex(b => b.id == boardId);
                    if (bIdx >= 0) freshData.lastBoardIndex = bIdx;
                }
                else if (TaskBoardWindow.Instance != null)
                {
                    freshData.lastBoardIndex = TaskBoardWindow.Instance.BoardIndex;
                }

                Persistence.Save(freshData);

                if (onChanged != null)
                {
                    onChanged.Invoke();
                }
                else
                {
                    TaskBoardWindow.ReloadAllOpenWindows();
                }
            }
        }

        private static void ReopenWindow(TaskCard card, TaskCard originalCard, SaveData data, string boardId, string columnId, List<string> categories, Action onChanged, Action onDelete, Action<TaskCard> onCreated, bool isNewCard, Vector2 scroll, string newChecklistItem, string newCategory)
        {
            var win = GetWindow<CardDetailWindow>(true, isNewCard ? $"{TBStyles.NewCardIcon} New Card" : $"{TBStyles.CardDetailIcon} Card Details", true);
            win._originalCard = originalCard;
            win._card = card;
            win._saveData = data ?? Persistence.Load();
            win._boardId = boardId;
            win._columnId = columnId;
            win._categories = categories ?? (win._saveData != null ? win._saveData.categories : new List<string>());
            win._onChanged = onChanged;
            win._onDelete = onDelete;
            win._onCreated = onCreated;
            win._isNewCard = isNewCard;
            win._dirty = !isNewCard;
            win._scroll = scroll;
            win._newChecklistItem = newChecklistItem;
            win._newCategory = newCategory;
            win._closedExplicitly = false;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win._shouldFocusTitle = false;
            win.ShowUtility();
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void DrawAssigneeCircle(Assignee assignee, bool clickable)
        {
            var rect = GUILayoutUtility.GetRect(30, 30);
            string initials = GetInitials(assignee.name);

            if (clickable && ThemedTooltip.Button(rect, "", assignee.name, GUIStyle.none))
            {
                Rect btnRect = rect;
                var menu = new ThemedContextMenu();
                menu.AddItem(new GUIContent("Remove"), false, () => {
                    _card.assigneeIds.Remove(assignee.id);
                    MarkDirty();
                });
                menu.Show(btnRect);
            }

            var circleStyle = new GUIStyle(TBStyles.AssigneeCircle) { fixedWidth = 30, fixedHeight = 30, fontSize = 11 };
            
            TBStyles.DrawAssigneeIcon(rect, assignee, initials, circleStyle);
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var words = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpper();
            return (words[0][0].ToString() + words[words.Length - 1][0].ToString()).ToUpper();
        }

        private void ShowAssigneePicker(Rect activatorRect = default)
        {
            var menu = new ThemedContextMenu();
            foreach (var a in _saveData.assignees)
            {
                bool assigned = _card.assigneeIds.Contains(a.id);
                menu.AddItem(new GUIContent(a.name), assigned, () => {
                    if (assigned) _card.assigneeIds.Remove(a.id);
                    else _card.assigneeIds.Add(a.id);
                    MarkDirty();
                });
            }

            if (_saveData.assignees.Count > 0) menu.AddSeparator("");

            menu.AddItem(new GUIContent("Manage Assignees..."), false, () => {
                AssigneeManagerWindow.ShowWindow(_saveData, () => {
                    MarkDirty();
                    Repaint();
                });
            });

            if (activatorRect.width > 0)
                menu.Show(activatorRect);
            else
                menu.ShowAsContext();
        }

        private void HandleImageAttachmentEvents(Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            // 1. Paste
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.V && (evt.control || evt.command)))
            {
                string assetPath = MarkdownRenderer.TryPasteImageToProject();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _card.imagePath = assetPath;
                    MarkDirty();
                    evt.Use();
                }
            }

            // 2. Drag & Drop
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasImage = false;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    string p = DragAndDrop.paths[0];
                    string ext = Path.GetExtension(p).ToLowerInvariant();
                    if (Array.Exists(MarkdownRenderer.ImageExtensions, e => e == ext))
                        hasImage = true;
                }

                if (hasImage)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        string p = DragAndDrop.paths[0];
                        _card.imagePath = MarkdownRenderer.CopyImageToProject(p);
                        MarkDirty();
                    }
                    evt.Use();
                }
            }
        }

        private void HandleSceneObjectClick(SceneObjectReference sceneRef)
        {
            if (string.IsNullOrEmpty(sceneRef.scenePath)) return;
            
            if (EditorSceneManager.GetActiveScene().path != sceneRef.scenePath)
            {
                string sceneName = Path.GetFileNameWithoutExtension(sceneRef.scenePath);
            
                if(sceneName == "")
                    sceneName = "a different scene";
                if (EditorApplication.isPlaying)
                {
                    ThemedDialog.Show("Cannot open scene in play mode",
                        $"This is an asset that was linked from {sceneName}. Please stop playing scene and try again.",
                        "OK");
                    return;
                }
                
                if (!ThemedDialog.Show("Open Scene?",
                    $"This is an asset that was linked from {sceneName}. Would you like to open that scene and select the {sceneRef.name} item?",
                    "Yes", "No"))
                {
                    return;
                }

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(sceneRef.scenePath);
                }
                else
                {
                    return;
                }
            }

            if (GlobalObjectId.TryParse(sceneRef.globalObjectId, out var gid))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                else
                {
                    Debug.LogWarning($"[Task Manager] Could not find scene object: {sceneRef.name}");
                }
            }
        }

        private void ShowCardPickerMenu(int checklistIndex)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            PopupWindow.Show(rect, new CardPickerPopup(_saveData, _card, checklistIndex, this));
        }

        private class CardPickerPopup : PopupWindowContent
        {
            private SaveData _saveData;
            private TaskCard _card;
            private int _index;
            private CardDetailWindow _parent;
            private Vector2 _scroll;
            private string _search = "";
            private string _selectedBoardId;
            private string _selectedColumnId;

            private enum SortMode { Default, Alphabetical }
            private SortMode _sortMode = SortMode.Default;

            public CardPickerPopup(SaveData saveData, TaskCard card, int index, CardDetailWindow parent)
            {
                _saveData = saveData;
                _card = card;
                _index = index;
                _parent = parent;
                _selectedBoardId = _parent._boardId;

                // Safety check: if board was deleted or doesn't exist
                if (!_saveData.boards.Any(b => b.id == _selectedBoardId))
                {
                    _selectedBoardId = _saveData.boards.FirstOrDefault()?.id;
                }
            }

            public override Vector2 GetWindowSize() => new Vector2(350, 450);

            public override void OnGUI(Rect rect)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(rect, TBStyles.PopupBg, true);
                    TBStyles.DrawBorderRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.15f));
                }

                EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(8, 8, 6, 6) });
                GUILayout.Space(2);
                string taskName = (_index >= 0 && _index < _card.checklistItems.Count) ? _card.checklistItems[_index] : "Unknown Task";
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black } };
                EditorGUILayout.LabelField($"Linking {taskName} to Card", titleStyle);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🔍", GUILayout.Width(16));
                    _search = TBStyles.DrawThemedTextField(_search, TBStyles.ThemedSearchField, GUILayout.Height(20));
                    if (!string.IsNullOrEmpty(_search) && GUILayout.Button("✕", TBStyles.ToolbarButton, GUILayout.Width(18), GUILayout.Height(20)))
                    {
                        _search = "";
                    }
                }

                // Breadcrumb Address Bar
                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                if (GUILayout.Button("🏠 Boards", TBStyles.ToolbarButton, GUILayout.Width(70)))
                {
                    _selectedBoardId = null;
                    _selectedColumnId = null;
                }
                
                if (!string.IsNullOrEmpty(_selectedBoardId))
                {
                    var board = _saveData.boards.FirstOrDefault(b => b.id == _selectedBoardId);
                    string boardName = board != null ? board.name : "Unknown Board";
                    if (GUILayout.Button($"> {boardName}", TBStyles.ToolbarButton))
                    {
                        _selectedColumnId = null;
                    }
                }

                if (!string.IsNullOrEmpty(_selectedColumnId))
                {
                    var board = _saveData.boards.FirstOrDefault(b => b.id == _selectedBoardId);
                    var col = board?.columns.FirstOrDefault(c => c.id == _selectedColumnId);
                    string colName = col != null ? col.title : "Unknown Column";
                    GUILayout.Label($"> {colName}", TBStyles.ToolbarButton);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.BeginChangeCheck();
                _parent._showArchivedInPicker = EditorGUILayout.ToggleLeft($"Show Archived Cards ({TBStyles.ArchiveIcon})", _parent._showArchivedInPicker);
                if (EditorGUI.EndChangeCheck())
                {
                    _parent.Repaint();
                }

                string[] sortModes = { "Default", "Alphabetical" };
                TBStyles.DrawThemedDropdown((int)_sortMode, sortModes, (s) => { _sortMode = (SortMode)s; }, TBStyles.StandardDropdown);

                if (GUILayout.Button("None (Clear Link)", TBStyles.StandardButton))
                {
                    _card.checklistLinkedCardIds[_index] = string.Empty;
                    _parent.MarkDirty();
                    _parent.Repaint();
                    editorWindow.Close();
                }

                GUILayout.Space(5);
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                GUIStyle itemStyle = new GUIStyle(EditorStyles.miniButton);
                itemStyle.alignment = TextAnchor.MiddleLeft;
                itemStyle.margin = new RectOffset(4, 4, 2, 2);

                if (!string.IsNullOrEmpty(_search))
                {
                    DrawSearchResults(itemStyle);
                }
                else if (string.IsNullOrEmpty(_selectedBoardId))
                {
                    DrawBoards(itemStyle);
                }
                else if (string.IsNullOrEmpty(_selectedColumnId))
                {
                    DrawColumns(itemStyle);
                }
                else
                {
                    DrawCards(itemStyle);
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            private void DrawSearchResults(GUIStyle itemStyle)
            {
                var results = new List<(TaskCard card, string path)>();
                foreach (var board in _saveData.boards)
                {
                    if (!string.IsNullOrEmpty(_selectedBoardId) && board.id != _selectedBoardId) continue;
                    foreach (var col in board.columns)
                    {
                        if (!string.IsNullOrEmpty(_selectedColumnId) && col.id != _selectedColumnId) continue;
                        foreach (var card in col.cards)
                        {
                            if (card.id == _card.id) continue;
                            if (card.archived && !_parent._showArchivedInPicker) continue;

                            string path = $"{board.name} / {col.title} / {card.title}";
                            if (!path.ToLower().Contains(_search.ToLower())) continue;

                            results.Add((card, path));
                        }
                    }
                }

                if (_sortMode == SortMode.Alphabetical)
                {
                    results = results.OrderBy(r => r.path).ToList();
                }

                foreach (var result in results)
                {
                    SelectableCardButton(result.card, result.path, itemStyle);
                }
            }

            private void DrawBoards(GUIStyle itemStyle)
            {
                foreach (var board in _saveData.boards)
                {
                    if (GUILayout.Button($"📁 {board.name}", itemStyle))
                    {
                        _selectedBoardId = board.id;
                    }
                }
            }

            private void DrawColumns(GUIStyle itemStyle)
            {
                var board = _saveData.boards.FirstOrDefault(b => b.id == _selectedBoardId);
                if (board == null) return;
                foreach (var col in board.columns)
                {
                    if (GUILayout.Button($"📑 {col.title}", itemStyle))
                    {
                        _selectedColumnId = col.id;
                    }
                }
            }

            private void DrawCards(GUIStyle itemStyle)
            {
                var board = _saveData.boards.FirstOrDefault(b => b.id == _selectedBoardId);
                var col = board?.columns.FirstOrDefault(c => c.id == _selectedColumnId);
                if (col == null) return;

                var cards = col.cards.Where(card => card.id != _card.id && (card.archived == false || _parent._showArchivedInPicker)).ToList();
                
                if (_sortMode == SortMode.Alphabetical)
                {
                    cards = cards.OrderBy(c => c.title).ToList();
                }

                foreach (var card in cards)
                {
                    SelectableCardButton(card, card.title, itemStyle);
                }
            }

            private void SelectableCardButton(TaskCard card, string label, GUIStyle itemStyle)
            {
                bool isCurrent = _card.checklistLinkedCardIds[_index] == card.id;
                if (isCurrent) itemStyle.fontStyle = FontStyle.Bold;
                else itemStyle.fontStyle = FontStyle.Normal;

                if (GUILayout.Button($"{(isCurrent ? "✓ " : "  ")}{label}", itemStyle))
                {
                    _card.checklistLinkedCardIds[_index] = card.id;
                    _card.checklistStates[_index] = card.completed;
                    _parent.MarkDirty();
                    _parent.Repaint();
                    editorWindow.Close();
                }
            }
        }
    }
}
