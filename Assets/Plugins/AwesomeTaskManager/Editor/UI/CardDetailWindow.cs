using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    //Card Detail Script
    public class CardDetailWindow : EditorWindow
    {
        [SerializeField] private TaskCard _card;
        [SerializeField] private TaskCard _originalCard;
        private System.Action _onChanged;
        private System.Action _onDelete;
        private System.Action<TaskCard> _onCreated;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private string _newChecklistItem = "";
        [SerializeField] private bool _isNewCard;
        [SerializeField] private List<string> _categories;
        [SerializeField] private SaveData _saveData;
        [SerializeField] private string _newCategory = "";
        [SerializeField] private bool _dirty;
        [SerializeField] private string _boardId;
        [SerializeField] private string _columnId;
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        private bool _shouldFocusTitle;
        private bool _shouldFocusChecklist;

        // ── Open existing card ──
        public static void Show(TaskCard card, SaveData saveData, string boardId, string columnId, System.Action onChanged, System.Action onDelete)
        {
            var win = GetWindow<CardDetailWindow>(true, "📝 Card Details", true);
            win._originalCard = card;
            win._card = card.Clone(false);
            win._saveData = saveData;
            win._boardId = boardId;
            win._columnId = columnId;
            win._categories = saveData.categories;
            win._onChanged = onChanged;
            win._onDelete = onDelete;
            win._onCreated = null;
            win._isNewCard = false;
            win._dirty = false;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win.saveChangesMessage = "You have unsaved changes to this card. Do you want to save them before closing?";
            win._shouldFocusTitle = true;
            win.ShowUtility();
        }

        // ── Open to create a NEW card ──
        public static void ShowNew(SaveData saveData, string boardId, string columnId, System.Action<TaskCard> onCreated)
        {
            var win = GetWindow<CardDetailWindow>(true, "✨ New Card", true);
            win._card = new TaskCard("") { description = "" };
            win._saveData = saveData;
            win._boardId = boardId;
            win._columnId = columnId;
            win._categories = saveData.categories;
            win._onCreated = onCreated;
            win._onChanged = null;
            win._onDelete = null;
            win._isNewCard = true;
            win._originalCard = null;
            win._newChecklistItem = "";
            win._newCategory = "";
            win._dirty = false;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win.saveChangesMessage = "You have unsaved changes. Do you want to create the card before closing?";
            win._shouldFocusTitle = true;
            win.ShowUtility();
        }

        private void OpenURL(string url)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Open URL", $"Open this link in your browser?\n\n{url}", "Open", "Cancel"))
                {
                    Application.OpenURL(url);
                }
            };
        }

        public void LoadData()
        {
            _saveData = Persistence.Load();
            Repaint();
        }

        private void OnGUI()
        {
            if (_card == null) { Close(); return; }

            if (_isNewCard)
            {
                bool isDirty = IsNewCardDirty();
                hasUnsavedChanges = isDirty;
                if (isDirty && string.IsNullOrWhiteSpace(_card.title))
                {
                    saveChangesMessage = "A title is required to save this card. If you close now, your changes will be lost. Click 'Cancel' to go back and add a title.";
                }
                else
                {
                    saveChangesMessage = "You have unsaved changes. Do you want to create the card before closing?";
                }
            }
            else
            {
                hasUnsavedChanges = _dirty;
                saveChangesMessage = "You have unsaved changes to this card. Do you want to save them before closing?";
            }

            _hasAnimatedGif = false;

            using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scope.scrollPosition;

            // ── Color label bar ──
            var labelColor = TBStyles.LabelColors[Mathf.Clamp(_card.colorLabel, 0, TBStyles.LabelColors.Length - 1)];
            if(labelColor != TBStyles.LabelColors[0])
            {
                var barRect = GUILayoutUtility.GetRect(0, 6, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(barRect, labelColor);
            }
            GUILayout.Space(10);

            // ── Title ──
            EditorGUILayout.LabelField(_isNewCard ? "Card Title" : "Title", EditorStyles.boldLabel);
            GUI.SetNextControlName("CardTitleField");
            string newTitle = EditorGUILayout.TextField(_card.title);
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
                int newIdx = EditorGUILayout.Popup(currentIdx, catOptions.ToArray());
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select task category"));
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
                }

                // Add new category
                _newCategory = EditorGUILayout.TextField(_newCategory, GUILayout.Width(80));
                if (GUILayout.Button(new GUIContent("+","Add New Category"), TBStyles.IconButton) && !string.IsNullOrWhiteSpace(_newCategory))
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
                if (!string.IsNullOrEmpty(_card.category) && GUILayout.Button(new GUIContent("🗑","Delete Category"), TBStyles.IconButton))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("Remove Category",
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
            string newDesc = EditorGUILayout.TextArea(_card.description, new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true, fontSize = 12, padding = new RectOffset(6, 6, 6, 6)
            }, GUILayout.MinHeight(70));
            if (newDesc != _card.description) { _card.description = newDesc; MarkDirty(); }
            GUILayout.Space(8);

            // ── Color Label & Priority ──
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Color Label", EditorStyles.boldLabel);
                    int newColor = EditorGUILayout.Popup(_card.colorLabel, TBStyles.LabelNames);
                    GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select card color label"));
                    if (newColor != _card.colorLabel) { _card.colorLabel = newColor; MarkDirty(); }
                }
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Priority", EditorStyles.boldLabel);
                    int newPri = EditorGUILayout.Popup(_card.priority, TBStyles.PriorityNames);
                    GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select task priority"));
                    if (newPri != _card.priority) { _card.priority = newPri; MarkDirty(); }
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

                if (GUILayout.Button(new GUIContent("+","Add Assignee"), TBStyles.IconButton))
                {
                    ShowAssigneePicker();
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
                if (GUILayout.Button(btnLabel, GUILayout.Height(20)))
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
                if (_card.completed) compStyle.normal = new GUIStyleState { textColor = new Color(0.3f, 0.85f, 0.4f) };
                EditorGUILayout.LabelField(_card.completed ? "✅ Completed" : "Status", compStyle, GUILayout.Width(100));

                GUI.backgroundColor = _card.completed ? new Color(0.3f, 0.85f, 0.4f) : Color.white;
                if (GUILayout.Button(_card.completed ? "Mark Incomplete" : "Mark Complete", GUILayout.Height(22)))
                {
                    _card.completed = !_card.completed;
                    _dirty = true;
                }
                GUI.backgroundColor = Color.white;
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
                    if (_card.completed)
                    {
                        statusIcon = "✅";
                        statusText = $"Completed (was due {parsed:MMM dd})";
                    }
                    else
                    {
                        statusIcon = daysUntil < 0 ? "🔴" : daysUntil == 0 ? "🟠" : daysUntil <= 3 ? "🟡" : "📅";
                        statusText = daysUntil < 0 ? $"Overdue by {-daysUntil}d"
                            : daysUntil == 0 ? "Due today!"
                            : daysUntil <= 3 ? $"Due in {daysUntil}d"
                            : $"Due {parsed:MMM dd, yyyy}";
                    }

                    EditorGUILayout.LabelField($"{statusIcon} {statusText}", GUILayout.Width(180));
                }

                // Year / Month / Day dropdowns
                int year = 0, month = 1, day = 1;
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
                int newYear = EditorGUILayout.IntField(year, GUILayout.Width(50));
                EditorGUILayout.LabelField("M:", GUILayout.Width(18));
                int newMonth = EditorGUILayout.IntPopup(month, 
                    new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" },
                    new[] { 1,2,3,4,5,6,7,8,9,10,11,12 }, GUILayout.Width(52));
                EditorGUILayout.LabelField("D:", GUILayout.Width(16));
                int maxDay = DateTime.DaysInMonth(Mathf.Clamp(newYear, 1, 9999), Mathf.Clamp(newMonth, 1, 12));
                int newDay = EditorGUILayout.IntField(Mathf.Clamp(day, 1, maxDay), GUILayout.Width(34));
                newDay = Mathf.Clamp(newDay, 1, maxDay);

                if (hasDueDate && (newYear != year || newMonth != month || newDay != day))
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
                    if (GUILayout.Button("Set Date", GUILayout.Width(66), GUILayout.Height(20)))
                    {
                        _card.dueDate = new DateTime(newYear, newMonth, newDay).ToString("yyyy-MM-dd");
                        MarkDirty();
                    }
                }
                else
                {
                    if (GUILayout.Button("✕", TBStyles.IconButton))
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
                if (GUILayout.Button("Today", EditorStyles.miniButton, GUILayout.Width(50)))
                { _card.dueDate = DateTime.Today.ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1d", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+3d", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1w", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+2w", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1m", EditorStyles.miniButton, GUILayout.Width(38)))
                { _card.dueDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"); MarkDirty(); }
            }

            GUILayout.Space(4);

            if (!_isNewCard)
                EditorGUILayout.LabelField($"Created: {_card.createdDate}", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // ── Checklist ──
            EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel);
            for (int i = 0; i < _card.checklistItems.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool done = EditorGUILayout.Toggle(_card.checklistStates[i], GUILayout.Width(20));
                    if (done != _card.checklistStates[i]) { _card.checklistStates[i] = done; MarkDirty(); }

                    var style = new GUIStyle(EditorStyles.textField);
                    if (done) style.fontStyle = FontStyle.Italic;
                    string itemText = EditorGUILayout.TextField(_card.checklistItems[i], style);
                    if (itemText != _card.checklistItems[i]) { _card.checklistItems[i] = itemText; MarkDirty(); }

                    if (GUILayout.Button(new GUIContent("✕","Remove Checklist Item"), TBStyles.IconButton))
                    {
                        _card.checklistItems.RemoveAt(i);
                        _card.checklistStates.RemoveAt(i);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool enterPressed = Event.current.type == EventType.KeyDown && 
                                   (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                                   GUI.GetNameOfFocusedControl() == "NewChecklistItemField";

                GUI.SetNextControlName("NewChecklistItemField");
                _newChecklistItem = EditorGUILayout.TextField(_newChecklistItem);

                if (_shouldFocusChecklist)
                {
                    _shouldFocusChecklist = false;
                    GUI.FocusControl("NewChecklistItemField");
                    EditorGUI.FocusTextInControl("NewChecklistItemField");
                }

                if ((GUILayout.Button(new GUIContent("+","Add Checklist Item"), TBStyles.IconButton) || enterPressed) && !string.IsNullOrWhiteSpace(_newChecklistItem))
                {
                    _card.checklistItems.Add(_newChecklistItem.Trim());
                    _card.checklistStates.Add(false);
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
            EditorGUILayout.LabelField("Linked Items (Assets, Scene, Notes, URLs)", EditorStyles.boldLabel);
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag & Drop Assets, Scene Objects or Notes here", EditorStyles.helpBox);
            
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
                if (GUILayout.Button("🔗 Add URL", GUILayout.Height(24)))
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
                if (GUILayout.Button("📝 Link Note", GUILayout.Height(24)))
                {
                    GenericMenu menu = new GenericMenu();
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
                                string title = string.IsNullOrEmpty(note.title) ? "Untitled" : note.title;
                                string fullPath = $"{folder.name}/{title}";

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
                                string title = string.IsNullOrEmpty(note.title) ? "Untitled" : note.title;

                                menu.AddItem(new GUIContent(title), false, () =>
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
                    menu.ShowAsContext();
                }
            }
            GUILayout.Space(4);

            for (int i = 0; i < _card.linkedItems.Count; i++)
            {
                var item = _card.linkedItems[i];
                using (new EditorGUILayout.HorizontalScope())
                {
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
                        if (GUILayout.Button(TBStyles.TruncateString(displayLabel, 50), EditorStyles.label))
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
                        if (GUILayout.Button(TBStyles.TruncateString(displayLabel, 50), EditorStyles.label))
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
                        if (GUILayout.Button(TBStyles.TruncateString(label, 50), EditorStyles.label))
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
                            if (GUILayout.Button(TBStyles.TruncateString(obj.name, 50), EditorStyles.label))
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

                    if (i > 0 && GUILayout.Button(new GUIContent("▲","Move Up"), TBStyles.IconButton))
                    {
                        _card.linkedItems.RemoveAt(i); _card.linkedItems.Insert(i - 1, item);
                        MarkDirty(); GUIUtility.ExitGUI();
                    }
                    if (i < _card.linkedItems.Count - 1 && GUILayout.Button(new GUIContent("▼","Move Down"), TBStyles.IconButton))
                    {
                        _card.linkedItems.RemoveAt(i); _card.linkedItems.Insert(i + 1, item);
                        MarkDirty(); GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(new GUIContent("✕","Remove Link"), TBStyles.IconButton))
                    {
                        _card.linkedItems.RemoveAt(i);
                        MarkDirty();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            GUILayout.Space(16);

            // ── Image Attachment ──
            EditorGUILayout.LabelField("Image / GIF", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(_card.imagePath))
                {
                    EditorGUILayout.LabelField($"🖼 {Path.GetFileName(_card.imagePath)}", EditorStyles.miniLabel);
                    if (GUILayout.Button(new GUIContent("✕","Remove Image"), TBStyles.IconButton))
                    {
                        _card.imagePath = "";
                        MarkDirty();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No image attached (Drag & Drop or Paste)", EditorStyles.miniLabel);
                }
                if (GUILayout.Button(new GUIContent("Browse…", "Attach Image"), GUILayout.Width(70), GUILayout.Height(20)))
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
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
                if (GUILayout.Button("✅  Create Card", GUILayout.Height(32)))
                {
                    if (string.IsNullOrWhiteSpace(_card.title))
                    {
                        EditorUtility.DisplayDialog("Title Required", "A card requires a title to be saved.", "OK");
                    }
                    else
                    {
                        SaveChanges();
                        hasUnsavedChanges = false;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.Space(4);
                if (GUILayout.Button("Cancel", GUILayout.Height(24)))
                {
                    if (IsNewCardDirty())
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (EditorUtility.DisplayDialog("Discard New Card?",
                                "You have unsaved changes. Are you sure you want to discard this card?",
                                "Discard", "Keep Editing"))
                            {
                                hasUnsavedChanges = false;
                                Close();
                            }
                        };
                    }
                    else
                    {
                        hasUnsavedChanges = false;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                // Save Changes button
                GUI.enabled = _dirty;
                GUI.backgroundColor = _dirty ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
                if (GUILayout.Button(_dirty ? "💾  Save Changes" : "✔  All Saved", GUILayout.Height(30)))
                {
                    SaveChanges();
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                GUILayout.Space(8);

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑  Delete Card", GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("Delete Card", $"Delete \"{_card.title}\"?", "Delete", "Cancel"))
                        {
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

                            hasUnsavedChanges = false;
                            Close();
                        }
                    };
                }
                GUI.backgroundColor = Color.white;
            }

            } // end scroll view scope

            // Throttled repaint for GIF animation
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        private bool IsNewCardDirty()
        {
            if (!string.IsNullOrWhiteSpace(_card.title)) return true;
            if (!string.IsNullOrWhiteSpace(_card.description)) return true;
            if (!string.IsNullOrEmpty(_card.category)) return true;
            if (!string.IsNullOrEmpty(_card.imagePath)) return true;
            if (!string.IsNullOrWhiteSpace(_card.dueDate)) return true;
            if (_card.checklistItems != null && _card.checklistItems.Count > 0) return true;
            if (_card.colorLabel > 0) return true;
            if (_card.priority > 0) return true;
            return false;
        }

        public override void SaveChanges()
        {
            if (_isNewCard)
            {
                if (!string.IsNullOrWhiteSpace(_card.title))
                {
                    if (_onCreated != null)
                    {
                        _onCreated.Invoke(_card);
                    }
                    else if (TaskBoardWindow.Instance != null)
                    {
                        TaskBoardWindow.Instance.AddCardFromDetail(_boardId, _columnId, _card);
                    }
                    else
                    {
                        // Fallback: load and save directly if everything else failed
                        var data = Persistence.Load();
                        var board = data.boards.FirstOrDefault(b => b.id == _boardId);
                        if (board != null)
                        {
                            var col = board.columns.FirstOrDefault(c => c.id == _columnId);
                            if (col != null)
                            {
                                col.cards.Add(_card);
                                Persistence.Save(data);
                                TaskBoardWindow.ReloadAllOpenWindows();
                            }
                        }
                    }
                    base.SaveChanges();
                }
                else
                {
                    EditorUtility.DisplayDialog("Title Required", "A card requires a title to be saved. Please enter a title before saving, or choose 'Discard' to exit without creating the card.", "OK");
                }
            }
            else
            {
                if (_dirty && _originalCard != null)
                {
                    string json = JsonUtility.ToJson(_card);
                    JsonUtility.FromJsonOverwrite(json, _originalCard);
                    
                    if (_onChanged != null)
                    {
                        _onChanged.Invoke();
                    }
                    else if (TaskBoardWindow.Instance != null)
                    {
                        TaskBoardWindow.Instance.UpdateCardFromDetail(_card);
                    }
                    else
                    {
                        // Fallback: load, find and update the card directly on disk
                        var data = Persistence.Load();
                        bool found = false;
                        foreach (var b in data.boards)
                        {
                            foreach (var col in b.columns)
                            {
                                var existing = col.cards.FirstOrDefault(c => c.id == _card.id);
                                if (existing != null)
                                {
                                    JsonUtility.FromJsonOverwrite(json, existing);
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                        if (found) 
                        {
                            Persistence.Save(data);
                            TaskBoardWindow.ReloadAllOpenWindows();
                        }
                    }
                    
                    _dirty = false;
                }
                base.SaveChanges();
            }
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void DrawAssigneeCircle(Assignee assignee, bool clickable)
        {
            var rect = GUILayoutUtility.GetRect(30, 30);
            string initials = GetInitials(assignee.name);

            if (clickable && GUI.Button(rect, new GUIContent("", assignee.name), GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove"), false, () => {
                    _card.assigneeIds.Remove(assignee.id);
                    MarkDirty();
                });
                menu.ShowAsContext();
            }

            // Mask color matches window background
            Color maskColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            var circleStyle = new GUIStyle(TBStyles.AssigneeCircle) { fixedWidth = 30, fixedHeight = 30, fontSize = 11 };
            
            TBStyles.DrawAssigneeIcon(rect, assignee, initials, circleStyle, maskColor);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var words = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpper();
            return (words[0][0].ToString() + words[words.Length - 1][0].ToString()).ToUpper();
        }

        private void ShowAssigneePicker()
        {
            GenericMenu menu = new GenericMenu();
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
            
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != sceneRef.scenePath)
            {
                string sceneName = Path.GetFileNameWithoutExtension(sceneRef.scenePath);
            
                if(sceneName == "")
                    sceneName = "a different scene";
                if(UnityEditor.EditorApplication.isPlaying)
                {
                    if (!EditorUtility.DisplayDialog("Cannot open scene in play mode",
                        $"This is an asset that was linked from {sceneName}. Please stop playing scene and try again.",
                        "OK"))
                    {
                        return;
                    }
                    return;
                }
                
                if (!EditorUtility.DisplayDialog("Open Scene?",
                    $"This is an asset that was linked from {sceneName}. Would you like to open that scene and select the {sceneRef.name} item?",
                    "Yes", "No"))
                {
                    return;
                }

                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sceneRef.scenePath);
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
    }
}
