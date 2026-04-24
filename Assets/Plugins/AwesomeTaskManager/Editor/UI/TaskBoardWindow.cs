using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    //Main Board Script
    public class TaskBoardWindow : EditorWindow
    {
        // ── State ──
        private SaveData _data;
        private int _tab;
        private int _boardIndex;

        // GIF cache state
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        private Vector2 _boardScroll, _notesListScroll, _noteEditorScroll;
        private string _searchFilter = "";
        private string _categoryFilter = "";
        private string _assigneeFilter = ""; // New filter
        private string _newColumnTitle = "";
        private bool _showAddColumn;
        private string _renameBoardName = "";
        private bool _renamingBoard;

        // Drag‐and‐drop card
        private TaskCard _dragCard;
        private TaskColumn _dragSourceCol;
        private bool _cardDragging;
        private Vector2 _cardDragStartPos;
        private const float CardDragThreshold = 15f;
        private Dictionary<string, Rect> _cardDropRects = new Dictionary<string, Rect>();
        private Dictionary<string, Rect> _columnFullRects = new Dictionary<string, Rect>();
        private string _hoveredColumnDropId = "";
        private string _hoveredFolderDropId = "";

        private static TaskCard _copiedCard;

        // Notes
        private int _selectedNote = -1;
        private string _newNoteTitle = "";
        private bool _noteEditMode = true;  // true = edit raw markdown, false = rendered preview
        private string _noteSearchFilter = "";
        private string _selectedFolderId = "";  // "" = show all / root
        private string _newFolderName = "";
        private bool _showAddFolder;

        // Note drag state
        private int _noteDragIdx = -1;          // index into _data.notes being dragged
        private Vector2 _noteDragStartPos;
        private bool _noteDragging;
        private const float NoteDragThreshold = 15f;
        private Dictionary<string, Rect> _folderDropRects = new Dictionary<string, Rect>();
        private Dictionary<string, Rect> _assigneeGroupRects = new Dictionary<string, Rect>();
        private Dictionary<string, bool> _assigneeHoverStates = new Dictionary<string, bool>();

        // Success Notification
        private string _successNotificationMessage = "";
        private double _successNotificationEndTime = 0;
        private string _errorNotificationMessage = "";
        private double _errorNotificationEndTime = 0;
        private bool _showArchived = false;

        // ── Menu ──
        [MenuItem("Tools/Awesome Task Manager/Open Board %#t", false, 0)]
        public static void Open()
        {
            var w = GetWindow<TaskBoardWindow>("🎯 Awesome Task Manager");
            w.minSize = new Vector2(750, 420);
        }

        [MenuItem("Tools/Awesome Task Manager/New Card &#c", false, 100)]
        public static void NewCardShortcut()
        {
            var windows = Resources.FindObjectsOfTypeAll<TaskBoardWindow>();
            if (windows != null && windows.Length > 0)
            {
                windows[0].CreateNewCardFromShortcut(false);
            }
            else
            {
                var data = Persistence.Load();
                if (data.boards.Count == 0) data.boards.Add(new TaskBoard("My First Board"));
                int boardIdx = Mathf.Clamp(data.lastBoardIndex, 0, data.boards.Count - 1);
                var board = data.boards[boardIdx];
                if (board.columns.Count == 0) board.columns.Add(new TaskColumn("To Do"));

                CardDetailWindow.ShowNew(data, (newCard) =>
                {
                    if (board.columns.Count > 0)
                        board.columns[0].cards.Add(newCard);
                    Persistence.Save(data);
                });
            }
        }

        [MenuItem("Tools/Awesome Task Manager/New Note %&n", false, 101)]
        public static void NewNoteShortcut()
        {
            var windows = Resources.FindObjectsOfTypeAll<TaskBoardWindow>();
            if (windows != null && windows.Length > 0)
            {
                windows[0].CreateNewNoteFromShortcut(false);
            }
            else
            {
                var data = Persistence.Load();
                var n = new QuickNote { title = "New Note" };
                data.notes.Insert(0, n);
                Persistence.Save(data);
                NotePopupWindow.Open(n, data, () => Persistence.Save(data));
            }
        }

        public void CreateNewCardFromShortcut(bool focus = true)
        {
            if (_data == null) { _data = Persistence.Load(); ClampBoard(); }
            _tab = 0; // Switch to Board tab
            _searchFilter = ""; // Clear filters to ensure the new card is visible
            _categoryFilter = "";
            
            var board = Board;
            if (board.columns.Count == 0)
                board.columns.Add(new TaskColumn("To Do"));

            CardDetailWindow.ShowNew(_data, (newCard) =>
            {
                // Add to the first column by default if we use the shortcut
                if (board.columns.Count > 0)
                    board.columns[0].cards.Add(newCard);
                Save();
                Repaint();
            });
            if (focus) Focus();
        }

        public void CreateNewNoteFromShortcut(bool focus = true)
        {
            if (_data == null) { _data = Persistence.Load(); ClampBoard(); }
            _tab = 1; // Switch to Notes tab
            _noteSearchFilter = ""; // Clear search to ensure the new note is visible
            
            string fid = _selectedFolderId;
            if (string.IsNullOrEmpty(fid) || fid == "__unfiled__") fid = "";

            var n = new QuickNote { title = "New Note", folderId = fid };
            _data.notes.Insert(0, n);
            _selectedNote = 0;
            _noteEditMode = true; // Start in edit mode
            Save();
            Repaint();
            if (focus) Focus();
         
            NotePopupWindow.Open(n, _data, () => { Save(); Repaint(); });
        }

        // ── Lifecycle ──
        private void OnEnable()
        {
            _data = Persistence.Load();
            ClampBoard();
        }

        private void OnDisable() { Save(); }

        private void Save()
        {
            if (_data == null) return;
            _data.lastBoardIndex = _boardIndex;
            Persistence.Save(_data);
        }

        private void ClampBoard()
        {
            if (_data.boards.Count == 0)
                _data.boards.Add(new TaskBoard("My First Board"));
            if (_data.categories == null || _data.categories.Count == 0)
                _data.categories = new List<string> { "Audio", "Art", "Code", "Design", "UI", "Bug", "Feature" };
            if (_data.categoryColors == null)
                _data.categoryColors = new List<CategoryColorEntry>();
            if (_data.noteFolders == null)
                _data.noteFolders = new List<NoteFolder>();
            _boardIndex = Mathf.Clamp(_data.lastBoardIndex, 0, _data.boards.Count - 1);
        }

        private TaskBoard Board => _data.boards[_boardIndex];

        // ════════════════════════════════════════════
        //  MAIN GUI
        // ════════════════════════════════════════════
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

        private void OnGUI()
        {
            if (_data == null) { _data = Persistence.Load(); ClampBoard(); }

            _hasAnimatedGif = false;

            DrawTabs();
            GUILayout.Space(1);

            if (_tab == 0) DrawBoardView();
            else           DrawNotesView();

            DrawSuccessNotification();
            DrawErrorNotification();

            // Throttled repaint for GIF animation (~15 fps)
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        private void DrawTabs()
        {
            using (var scope = new EditorGUILayout.HorizontalScope(GUILayout.Height(34)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(scope.rect, EditorGUIUtility.isProSkin
                        ? new Color(0.18f, 0.18f, 0.18f)
                        : new Color(0.82f, 0.82f, 0.82f));
                }

                GUILayout.Space(8);
                if (GUILayout.Button("📋 Board", _tab == 0 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    _tab = 0;
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(4);
                if (GUILayout.Button("📝 Notes", _tab == 1 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    _tab = 1;
                    GUIUtility.ExitGUI();
                }

                GUILayout.FlexibleSpace();
            }
        }

        // ════════════════════════════════════════════
        //  BOARD VIEW
        // ════════════════════════════════════════════
        private void DrawBoardView()
        {
            var board = Board;
            _cardDropRects.Clear();
            _columnFullRects.Clear();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(24)))
            {
                string[] names = _data.boards.Select(b => b.name).ToArray();
                int newIdx = EditorGUILayout.Popup(_boardIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(150));
                if (newIdx != _boardIndex)
                {
                    _boardIndex = newIdx;
                    _searchFilter = ""; _categoryFilter = ""; _assigneeFilter = "";
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(new GUIContent("+", "Board Options"), EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Create New Board/Blank Board"), false, () => CreateBoard(null));
                    menu.AddSeparator("Create New Board/");
                    foreach (var template in _data.templates)
                    {
                        var t = template;
                        menu.AddItem(new GUIContent("Create New Board/Template: " + t.name), false, () => CreateBoard(t));
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Save Current as Template..."), false, SaveCurrentAsTemplate);
                    if (_data.templates.Count > 0)
                    {
                        foreach (var template in _data.templates)
                        {
                            var t = template;
                            menu.AddItem(new GUIContent("Delete Template/" + t.name), false, () =>
                            {
                                if (EditorUtility.DisplayDialog("Delete Template", $"Delete template \"{t.name}\"?", "Delete", "Cancel"))
                                {
                                    _data.templates.Remove(t);
                                    Save();
                                }
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                if (_data.boards.Count > 1 && GUILayout.Button(new GUIContent("✕", "Delete Board"), EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    string boardName = _data.boards[_boardIndex].name;
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("Delete Board", $"Delete \"{boardName}\"?", "Delete", "Cancel"))
                        {
                            _data.boards.RemoveAt(_boardIndex);
                            _boardIndex = Mathf.Clamp(_boardIndex, 0, _data.boards.Count - 1);
                            Save();
                            Repaint();
                        }
                    };
                }

                GUILayout.Space(2);

                EditorGUILayout.LabelField(new GUIContent("Category:","You can use the dropdown\nTo the right to filter tasks by category"), GUILayout.Width(58));
                var catFilterOptions = new List<string> { "All" };
                catFilterOptions.AddRange(_data.categories);
                int catIdx = 0;
                if (!string.IsNullOrEmpty(_categoryFilter))
                {
                    int f = catFilterOptions.IndexOf(_categoryFilter);
                    if (f >= 0) catIdx = f;
                }
                int newCatIdx = EditorGUILayout.Popup(catIdx, catFilterOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(90));
                _categoryFilter = newCatIdx == 0 ? "" : catFilterOptions[newCatIdx];
                if (GUILayout.Button(new GUIContent("🏷", "Category Editor"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    CategoryEditorWindow.Open(_data, () => { Save(); Repaint(); });
                }
                GUILayout.Space(8);
                EditorGUILayout.LabelField(new GUIContent("Assignee:", "You can use the dropdown\nTo the right to filter tasks by assignee"), GUILayout.Width(56));
                var assigneeOptions = new List<string> { "All" };
                assigneeOptions.AddRange(_data.assignees.Select(a => a.name));
                int assIdx = 0;
                if (!string.IsNullOrEmpty(_assigneeFilter))
                {
                    var found = _data.assignees.FirstOrDefault(a => a.id == _assigneeFilter);
                    if (found != null) assIdx = assigneeOptions.IndexOf(found.name);
                    if (assIdx < 0) assIdx = 0;
                }
              
                int newAssIdx = EditorGUILayout.Popup(assIdx, assigneeOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(90));
                if (newAssIdx == 0) _assigneeFilter = "";
                else
                {
                    var selectedName = assigneeOptions[newAssIdx];
                    var ass = _data.assignees.FirstOrDefault(a => a.name == selectedName);
                    if (ass != null) _assigneeFilter = ass.id;
                }

               
                
                if (GUILayout.Button(new GUIContent("👥", "Assignee Manager"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    AssigneeManagerWindow.ShowWindow(_data, () => { Save(); Repaint(); });
                }
                
                GUILayout.Space(8);

                EditorGUILayout.LabelField(new GUIContent("🔍", "Search Tasks"), GUILayout.Width(18));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(140));

                GUILayout.Space(8);
                if (GUILayout.Button(new GUIContent("▾ Show All", "Show All Checklists"), EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    var board2 = _data.boards[_boardIndex];
                    foreach (var c in board2.columns)
                        foreach (var card in c.cards)
                            card.showChecklist = true;
                    Save();
                }
                if (GUILayout.Button(new GUIContent("▸ Hide All", "Hide All Checklists"), EditorStyles.toolbarButton, GUILayout.Width(68)))
                {
                    var board2 = _data.boards[_boardIndex];
                    foreach (var c in board2.columns)
                        foreach (var card in c.cards)
                            card.showChecklist = false;
                    Save();
                }
                // Show/Hide Archived toggle
                bool newShowArchived = GUILayout.Toggle(_showArchived, new GUIContent(_showArchived ? "📦" : "🗃️", (_showArchived ? "Hide Archived Cards" : "Show Archived Cards")), EditorStyles.toolbarButton, GUILayout.Width(28));
                if (newShowArchived != _showArchived)
                {
                    _showArchived = newShowArchived;
                    Repaint();
                }
                
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(2);

            // Board title row
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_renamingBoard)
                {
                    _renameBoardName = EditorGUILayout.TextField(_renameBoardName, GUILayout.Width(250), GUILayout.Height(26));
                    if (GUILayout.Button(new GUIContent("✔", "Save Board Name"), GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        if (!string.IsNullOrWhiteSpace(_renameBoardName)) board.name = _renameBoardName.Trim();
                        _renamingBoard = false; Save();
                    }
                    if (GUILayout.Button(new GUIContent("✕","Cancel Renaming"), GUILayout.Width(26), GUILayout.Height(24))) _renamingBoard = false;
                }
                else
                {
                    EditorGUILayout.LabelField($"🎯 {board.name}", TBStyles.BoardHeader, GUILayout.Height(30));
                    if (GUILayout.Button(new GUIContent("✏", "Rename Board"), GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        _renamingBoard = true;
                        _renameBoardName = board.name;
                    }
                }
                GUILayout.FlexibleSpace();

                if (_showAddColumn)
                {
                    _newColumnTitle = EditorGUILayout.TextField(_newColumnTitle, GUILayout.Width(140), GUILayout.Height(22));
                    if (GUILayout.Button(new GUIContent("Add", "Add a new Column"), GUILayout.Width(42), GUILayout.Height(22)) && !string.IsNullOrWhiteSpace(_newColumnTitle))
                    {
                        board.columns.Add(new TaskColumn(_newColumnTitle.Trim()));
                        _newColumnTitle = ""; _showAddColumn = false; Save();
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button(new GUIContent("✕", "Cancel Column Creation"), GUILayout.Width(22), GUILayout.Height(22))) _showAddColumn = false;
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("+ Column" ,"Add a new Column"), GUILayout.Width(80), GUILayout.Height(24)))
                        _showAddColumn = true;
                }
            }

            GUILayout.Space(4);

            using (var scope = new EditorGUILayout.ScrollViewScope(_boardScroll, true, false))
            {
                _boardScroll = scope.scrollPosition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    float colWidth = Mathf.Max(260, (position.width - 40) / Mathf.Max(board.columns.Count, 1));
                    colWidth = Mathf.Min(colWidth, 340);

                    for (int ci = 0; ci < board.columns.Count; ci++)
                    {
                        DrawColumn(board.columns[ci], ci, board, colWidth);
                        GUILayout.Space(4);
                    }
                }

                // Hit-test card drag INSIDE the scroll view so coordinates match column rects
                UpdateCardDragHitTest(board);
            }

            // Draw overlays OUTSIDE the scroll view (not clipped)
            HandleDragDrop(board);
            DrawCardDragOverlay();
          

            // ── Status bar ──
            var allCards = board.columns.SelectMany(c => c.cards).Where(c => _showArchived || !c.archived).ToList();
            int totalCards = allCards.Count;
            int completedCount = allCards.Count(c => c.completed);
            int overdueCount = allCards.Count(c => !c.completed && !string.IsNullOrWhiteSpace(c.dueDate)
                && DateTime.TryParse(c.dueDate, out var d) && d.Date < DateTime.Today);
            int dueTodayCount = allCards.Count(c => !c.completed && !string.IsNullOrWhiteSpace(c.dueDate)
                && DateTime.TryParse(c.dueDate, out var d) && d.Date == DateTime.Today);

            var statusRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(statusRect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.85f, 0.85f, 0.85f));

            string statusText = $"  {totalCards} card(s)";
            if(completedCount > 0) statusText += $"  •  🟢 {completedCount} Completed";
            if (overdueCount > 0) statusText += $"  •  🔴 {overdueCount} overdue";
            if (dueTodayCount > 0) statusText += $"  •  🟠 {dueTodayCount} due today";
            statusText += $"  •  {board.columns.Count} column(s)";
            EditorGUI.LabelField(statusRect, statusText, EditorStyles.miniLabel);
        }

        private void DrawColumn(TaskColumn col, int colIdx, TaskBoard board, float width)
        {
            Color bg = colIdx % 2 == 0 ? TBStyles.ColumnBg : TBStyles.ColumnBgAlt;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                using (var scope = new EditorGUILayout.VerticalScope("box"))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        // The "box" style already draws a background, but we want our custom one
                        EditorGUI.DrawRect(scope.rect, bg);
                    }

                    // Column drag highlight overlay
                    if (_cardDragging && _dragCard != null)
                    {
                        if (_hoveredColumnDropId == col.id)
                            EditorGUI.DrawRect(scope.rect, TBStyles.ColumnDropHovered);
                        else if (_dragSourceCol != null && _dragSourceCol.id == col.id)
                            EditorGUI.DrawRect(scope.rect, TBStyles.ColumnDragSource);
                        else
                            EditorGUI.DrawRect(scope.rect, TBStyles.ColumnDropOther);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        int columnsCardCountWithFliter = col.cards.Count(c => (_showArchived || !c.archived) && (string.IsNullOrWhiteSpace(_categoryFilter) || (c.category ?? "") == _categoryFilter)
                            && (string.IsNullOrEmpty(_assigneeFilter) || c.assigneeIds.Contains(_assigneeFilter))
                            && (string.IsNullOrWhiteSpace(_searchFilter) || c.title.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())
                                || (c.description ?? "").ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())
                                || (c.category ?? "").ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())));
                        
                        EditorGUILayout.LabelField($"{col.title}  ({columnsCardCountWithFliter})", TBStyles.ColumnHeader);

                        if (board.columns.Count > 1)
                        {
                            if (colIdx > 0 && GUILayout.Button(new GUIContent("◀","Move Column Left"), TBStyles.IconButton))
                            {
                                board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx - 1, col);
                                Save(); GUIUtility.ExitGUI();
                            }
                            if (colIdx < board.columns.Count - 1 && GUILayout.Button(new GUIContent("▶","Move Column Right"), TBStyles.IconButton))
                            {
                                board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx + 1, col);
                                Save(); GUIUtility.ExitGUI();
                            }
                        }
                        if (GUILayout.Button(new GUIContent("⋮", "Show Column Options"), TBStyles.IconButton))
                        {
                            var menu = new GenericMenu();
                            int ci = colIdx;
                            menu.AddItem(new GUIContent("Rename Column"), false, () =>
                            {
                                string newName = EditorInputDialog.Show("Rename Column", "Column name:", col.title);
                                if (!string.IsNullOrWhiteSpace(newName)) { col.title = newName; Save(); Repaint(); }
                            });

                            if (_copiedCard != null)
                            {
                                menu.AddItem(new GUIContent($"Paste Card ({(_copiedCard.title.Length > 20 ? _copiedCard.title.Substring(0, 20) + "..." : _copiedCard.title)})"), false, () =>
                                {
                                    col.cards.Add(_copiedCard.Clone());
                                    Save();
                                    Repaint();
                                });
                            }
                            else
                            {
                                menu.AddDisabledItem(new GUIContent("Paste Card (Clipboard Empty)"));
                            }

                            menu.AddItem(new GUIContent("Clear All Cards"), false, () =>
                            {
                                if (EditorUtility.DisplayDialog("Clear Column", $"Remove all cards from \"{col.title}\"?", "Clear", "Cancel"))
                                { col.cards.Clear(); Save(); Repaint(); }
                            });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Delete Column"), false, () =>
                            {
                                if (EditorUtility.DisplayDialog("Delete Column", $"Delete \"{col.title}\" and all its cards?", "Delete", "Cancel"))
                                { board.columns.RemoveAt(ci); Save(); Repaint(); }
                            });
                            menu.ShowAsContext();
                        }
                    }

                    var sepRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(sepRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    GUILayout.Space(4);

                    bool hasTextFilter = !string.IsNullOrWhiteSpace(_searchFilter);
                    string filter = hasTextFilter ? _searchFilter.ToLowerInvariant() : "";
                    bool hasCatFilter = !string.IsNullOrEmpty(_categoryFilter);
                    bool hasAssFilter = !string.IsNullOrEmpty(_assigneeFilter);

                    for (int i = 0; i < col.cards.Count; i++)
                    {
                        var card = col.cards[i];
                        if (!_showArchived && card.archived) continue;
                        if (hasCatFilter && (card.category ?? "") != _categoryFilter) continue;
                        if (hasAssFilter && !card.assigneeIds.Contains(_assigneeFilter)) continue;
                        if (hasTextFilter && !card.title.ToLowerInvariant().Contains(filter)
                                          && !(card.description ?? "").ToLowerInvariant().Contains(filter)
                                          && !(card.category ?? "").ToLowerInvariant().Contains(filter))
                            continue;
                        DrawCard(card, col, i);
                    }

                    GUILayout.Space(6);

                    if (GUILayout.Button("+ Add Card", GUILayout.Height(26)))
                    {
                        int capturedColIdx = colIdx;
                        CardDetailWindow.ShowNew(_data, (newCard) =>
                        {
                            if (capturedColIdx < board.columns.Count)
                                board.columns[capturedColIdx].cards.Add(newCard);
                            Save(); Repaint();
                        });
                    }

                    var dropRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                    if (_cardDragging && _dragCard != null)
                    {
                        EditorGUI.DrawRect(dropRect, new Color(0.3f, 0.7f, 1f, 0.2f));
                        EditorGUI.LabelField(dropRect, "  ⬇ Drop here", EditorStyles.centeredGreyMiniLabel);
                    }
                    _cardDropRects[col.id] = dropRect;

                    // Store full column rect for drag detection
                    _columnFullRects[col.id] = scope.rect;
                }
            }
        }

        private void DrawCard(TaskCard card, TaskColumn col, int idx)
        {
            var labelColor = TBStyles.LabelColors[Mathf.Clamp(card.colorLabel, 0, TBStyles.LabelColors.Length - 1)];

            Rect cardRect;
            using (var cardScope = new EditorGUILayout.VerticalScope(TBStyles.CardBox))
            {
                cardRect = cardScope.rect;
                if (card.colorLabel > 0)
                {
                    var stripRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(stripRect, labelColor);
                    GUILayout.Space(2);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Completed toggle
                    string compIcon = card.completed ? "✅" : "⬜";
                    string compToolTip = card.completed ? "Untick Card" : "Tick to Complete Card";
                    if (GUILayout.Button(new GUIContent(compIcon,compToolTip), TBStyles.IconButton, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        card.completed = !card.completed;
                        Save();
                    }
                    if (card.priority > 0)
                        EditorGUILayout.LabelField(TBStyles.PriorityIcons[card.priority], GUILayout.Width(18));
                    var titleStyle = new GUIStyle(TBStyles.CardTitle);
                    if (card.completed)
                    {
                        titleStyle.fontStyle = FontStyle.Italic;
                        titleStyle.normal = new GUIStyleState { textColor = new Color(0.5f, 0.8f, 0.5f) };
                    }
                    EditorGUILayout.LabelField(card.title, titleStyle);
                    Rect dragHandleRect = GUILayoutUtility.GetRect(new GUIContent("↕"), TBStyles.IconButton, GUILayout.Width(26), GUILayout.Height(24));
                    GUI.Box(dragHandleRect, new GUIContent("↕", "Drag to reorder"), TBStyles.IconButton);
                    HandleCardDragHandle(card, col, dragHandleRect);
                    if (GUILayout.Button(new GUIContent("✏","Show card details"), TBStyles.IconButton))
                    {
                        CardDetailWindow.Show(card, _data, () => { Save(); Repaint(); }, () =>
                        {
                            col.cards.Remove(card); Save(); Repaint();
                        });
                    }
                    if (GUILayout.Button(new GUIContent("⋮", "Card Options"), TBStyles.IconButton))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent(card.archived ? "Unarchive Card" : "Archive Card"), false, () =>
                        {
                            card.archived = !card.archived;
                            Save();
                            TriggerSuccessNotification(card.archived ? "Card archived" : "Card unarchived");
                            Repaint();
                        });
                        menu.AddSeparator("");

                        menu.AddItem(new GUIContent("Duplicate Card"), false, () =>
                        {
                            var clone = card.Clone();
                            col.cards.Insert(idx + 1, clone);
                            Save();
                            Repaint();
                        });

                        menu.AddItem(new GUIContent("Copy Card"), false, () =>
                        {
                            _copiedCard = card.Clone();
                            TriggerSuccessNotification("Card copied to clipboard");
                        });

                        if (_copiedCard != null)
                        {
                            menu.AddItem(new GUIContent($"Paste Card After ({TBStyles.TruncateString(_copiedCard.title, 20)})"), false, () =>
                            {
                                col.cards.Insert(idx + 1, _copiedCard.Clone());
                                Save();
                                Repaint();
                            });
                        }

                        foreach (var b in _data.boards)
                        {
                            if (b == Board) continue;
                            menu.AddItem(new GUIContent($"Copy to Board/{b.name}"), false, () =>
                            {
                                var clone = card.Clone();
                                if (b.columns.Count > 0)
                                {
                                    b.columns[0].cards.Add(clone);
                                    Save();
                                    TriggerSuccessNotification($"Copied to board: {b.name}");
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "Target board has no columns.", "OK");
                                }
                            });
                        }

                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Card"), false, () =>
                        {
                            if (EditorUtility.DisplayDialog("Delete Card", $"Delete \"{card.title}\"?", "Delete", "Cancel"))
                            {
                                col.cards.Remove(card);
                                Save();
                                Repaint();
                            }
                        });
                        menu.ShowAsContext();
                    }
                }

            if (!string.IsNullOrEmpty(card.category) || card.archived)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!string.IsNullOrEmpty(card.category))
                    {
                        var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(0.4f, 0.75f, 1f) }
                        };
                        GUILayout.Label($"[{card.category}]", badgeStyle);
                        GUILayout.Space(5);
                    }

                    if (card.archived)
                    {
                        var archiveStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.65f, 0.1f) }
                        };
                        GUILayout.Label("📦 ARCHIVED", archiveStyle);
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            if (!string.IsNullOrWhiteSpace(card.description))
            {
                string preview = TBStyles.TruncateString(card.description, 80);
                EditorGUILayout.LabelField(new GUIContent(preview, card.description), EditorStyles.wordWrappedMiniLabel);
            }

            if (card.checklistItems.Count > 0)
            {
                int done = card.checklistStates.Count(s => s);
                bool allDone = done == card.checklistItems.Count;
                var summaryStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = allDone ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.7f, 0.7f, 0.7f) }
                };

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(allDone ? $"✅ {done}/{card.checklistItems.Count} complete" : $"☑ {done}/{card.checklistItems.Count}", summaryStyle);
                    string toggleLabel = card.showChecklist ? "▾" : "▸";
                    string toggleToolTip = card.showChecklist ? "Hide Checklist" : "Show Checklist";
                    if (GUILayout.Button(new GUIContent(toggleLabel,toggleToolTip), TBStyles.IconButton, GUILayout.Width(20), GUILayout.Height(16)))
                    {
                        card.showChecklist = !card.showChecklist;
                        Save();
                    }
                }
                
                // Show individual checklist items inline
                if (card.showChecklist)
                {
                    for (int ci = 0; ci < card.checklistItems.Count; ci++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(8);
                            bool wasDone = card.checklistStates[ci];
                            bool nowDone = EditorGUILayout.Toggle(wasDone, GUILayout.Width(16));
                            if (nowDone != wasDone)
                            {
                                card.checklistStates[ci] = nowDone;
                                Save();
                            }
                            var itemStyle = new GUIStyle(EditorStyles.miniLabel);
                            if (wasDone) itemStyle.fontStyle = FontStyle.Italic;
                            string itemText = card.checklistItems[ci];
                            string displayItemText = TBStyles.TruncateString(itemText, 40);
                            EditorGUILayout.LabelField(new GUIContent(displayItemText, itemText), itemStyle);
                        }
                    }
                }
            }

            // Image thumbnail
            if (!string.IsNullOrEmpty(card.imagePath))
            {
                MarkdownRenderer.DrawImageThumbnail(card.imagePath, 60f);
            }

            bool hasLinkedItems = card.linkedItems != null && card.linkedItems.Count > 0;

            if (hasLinkedItems)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int totalCount = card.linkedItems.Count;
                    int displayLimit = 8;
                    int shown = 0;

                    for (int i = 0; i < totalCount && shown < displayLimit; i++)
                    {
                        var item = card.linkedItems[i];
                        if (item.isSceneObject)
                        {
                            var sref = item.sceneObject;
                            if (sref != null)
                            {
                                var sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
                                string sceneName = Path.GetFileNameWithoutExtension(sref.scenePath);
                                if (GUILayout.Button(new GUIContent(sceneIcon, $"[{sceneName}] {sref.name}"), GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
                                {
                                    EditorApplication.delayCall += () => HandleSceneObjectClick(sref);
                                }
                                shown++;
                            }
                        }
                        else if (item.isNote)
                        {
                            var note = _data.notes.FirstOrDefault(n => n.id == item.guid);
                            var noteIcon = EditorGUIUtility.IconContent("TextAsset Icon").image;
                            string noteTitle = note != null ? note.title : "Missing Note";
                            if (GUILayout.Button(new GUIContent(noteIcon, $"[Note] {noteTitle}"), GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
                            {
                                if (note != null) NotePopupWindow.OpenInPreviewMode(note, _data, () => { Save(); Repaint(); });
                            }
                            shown++;
                        }
                        else if (item.isUrl)
                        {
                            var urlIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
                            string label = string.IsNullOrEmpty(item.displayName) ? item.url : item.displayName;
                            if (GUILayout.Button(new GUIContent(urlIcon, $"[Link] {label}"), GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
                            {
                                OpenURL(item.url);
                            }
                            shown++;
                        }
                        else
                        {
                            string guid = item.guid;
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                            if (obj != null)
                            {
                                var icon = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
                                if (GUILayout.Button(new GUIContent(icon, obj.name), GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
                                {
                                    EditorGUIUtility.PingObject(obj);
                                    Selection.activeObject = obj;
                                    EditorUtility.FocusProjectWindow();
                                }
                                shown++;
                            }
                        }
                    }

                    if (totalCount > displayLimit)
                    {
                        var linkStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
                        EditorGUILayout.LabelField($"+{totalCount - displayLimit}", linkStyle, GUILayout.Width(30));
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(2);
            }

            bool hasDueDate = !string.IsNullOrWhiteSpace(card.dueDate);
            bool hasAssignees = card.assigneeIds != null && card.assigneeIds.Count > 0;

            if (hasDueDate || hasAssignees)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (hasDueDate)
                    {
                        var dueDateStyle = new GUIStyle(EditorStyles.miniLabel);
                        string dueDateText = $"📅 {card.dueDate}";

                        if (card.completed)
                        {
                            dueDateText = "✅ Completed";
                            dueDateStyle.normal = new GUIStyleState { textColor = new Color(0.3f, 0.85f, 0.4f) };
                            dueDateStyle.fontStyle = FontStyle.Bold;
                        }
                        else if (DateTime.TryParse(card.dueDate, out DateTime parsedDue))
                        {
                            var today = DateTime.Today;
                            int daysUntil = (parsedDue.Date - today).Days;

                            if (daysUntil < 0)
                            {
                                dueDateText = $"🔴 Overdue ({-daysUntil}d ago)";
                                dueDateStyle.normal = new GUIStyleState { textColor = new Color(1f, 0.3f, 0.25f) };
                                dueDateStyle.fontStyle = FontStyle.Bold;
                            }
                            else if (daysUntil == 0)
                            {
                                dueDateText = "🟠 Due today!";
                                dueDateStyle.normal = new GUIStyleState { textColor = new Color(1f, 0.65f, 0.1f) };
                                dueDateStyle.fontStyle = FontStyle.Bold;
                            }
                            else if (daysUntil <= 3)
                            {
                                dueDateText = $"🟡 Due in {daysUntil}d ({parsedDue:MMM dd})";
                                dueDateStyle.normal = new GUIStyleState { textColor = new Color(0.95f, 0.85f, 0.15f) };
                            }
                            else
                            {
                                dueDateText = $"📅 {parsedDue:MMM dd, yyyy}";
                            }
                        }
                        EditorGUILayout.LabelField(dueDateText, dueDateStyle);
                    }

                    GUILayout.FlexibleSpace();

                    if (hasAssignees)
                    {
                        string cardId = card.id;
                        bool wasHovered = false;
                        _assigneeHoverStates.TryGetValue(cardId, out wasHovered);

                        using (var groupScope = new EditorGUILayout.HorizontalScope())
                        {
                            if (wasHovered)
                            {
                                foreach (var id in card.assigneeIds)
                                {
                                    var assignee = _data.assignees.FirstOrDefault(a => a.id == id);
                                    if (assignee != null)
                                    {
                                        DrawAssigneeCircleBoard(assignee);
                                        GUILayout.Label(assignee.name, EditorStyles.miniLabel);
                                        GUILayout.Space(2);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var id in card.assigneeIds.Take(4))
                                {
                                    var assignee = _data.assignees.FirstOrDefault(a => a.id == id);
                                    if (assignee != null)
                                    {
                                        DrawAssigneeCircleBoard(assignee);
                                        GUILayout.Space(-8); // Overlap
                                    }
                                }
                                if (card.assigneeIds.Count > 4)
                                {
                                    GUILayout.Space(6);
                                    EditorGUILayout.LabelField($"+{card.assigneeIds.Count - 4}", EditorStyles.miniLabel, GUILayout.Width(18));
                                }
                                else GUILayout.Space(6);
                            }

                            if (Event.current.type == EventType.Repaint)
                            {
                                bool currentlyHovered = groupScope.rect.Contains(Event.current.mousePosition);
                                if (currentlyHovered != wasHovered)
                                {
                                    _assigneeHoverStates[cardId] = currentlyHovered;
                                    Repaint();
                                }
                            }
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (idx > 0 && GUILayout.Button(new GUIContent("▲","Move card up the column"), TBStyles.IconButton))
                {
                    col.cards.RemoveAt(idx); col.cards.Insert(idx - 1, card);
                    Save(); GUIUtility.ExitGUI();
                }
                if (idx < col.cards.Count - 1 && GUILayout.Button(new GUIContent("▼","Move card down the column"), TBStyles.IconButton))
                {
                    col.cards.RemoveAt(idx); col.cards.Insert(idx + 1, card);
                    Save(); GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
            }

            if (_cardDragging && _dragCard == card)
            {
                // Dim the source card while dragging
                EditorGUI.DrawRect(cardRect, new Color(0f, 0f, 0f, 0.35f));
                var dragBanner = new Rect(cardRect.x + 4f, cardRect.yMax - 18f, Mathf.Max(60f, cardRect.width - 8f), 14f);
                EditorGUI.DrawRect(dragBanner, new Color(0.3f, 0.7f, 1f, 0.3f));
            }

            // Asset Drag & Drop handling onto the card
            Event currentEvt = Event.current;
            if (cardRect.Contains(currentEvt.mousePosition))
            {
                bool isDraggingItems = (DragAndDrop.objectReferences?.Length > 0) || (DragAndDrop.GetGenericData("AwesomeTaskNoteId") != null);
                if (currentEvt.type == EventType.DragUpdated && isDraggingItems)
                {
                    // Check if we are dragging a column or card vs dragging an asset target
                    if (!_cardDragging)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        currentEvt.Use();
                    }
                }
                else if (currentEvt.type == EventType.DragPerform && isDraggingItems)
                {
                    if (!_cardDragging)
                    {
                        DragAndDrop.AcceptDrag();
                        bool changed = false;

                        // Assets / Scene Objects
                        if (DragAndDrop.objectReferences != null)
                        {
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (AssetDatabase.Contains(obj))
                                {
                                    string path = AssetDatabase.GetAssetPath(obj);
                                    string guid = AssetDatabase.AssetPathToGUID(path);
                                    if (!string.IsNullOrEmpty(guid) && !card.linkedItems.Any(li => !li.isSceneObject && !li.isNote && !li.isUrl && li.guid == guid))
                                    {
                                        card.linkedItems.Add(new LinkedItem(guid));
                                        changed = true;
                                    }
                                }
                                else if (obj is GameObject go && go.scene.IsValid())
                                {
                                    string scenePath = go.scene.path;
                                    string gid = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                                    if (!card.linkedItems.Any(li => li.isSceneObject && li.sceneObject != null && li.sceneObject.globalObjectId == gid))
                                    {
                                        card.linkedItems.Add(new LinkedItem(new SceneObjectReference(scenePath, gid, go.name)));
                                        changed = true;
                                    }
                                }
                            }
                        }

                        // Notes
                        var noteId = DragAndDrop.GetGenericData("AwesomeTaskNoteId") as string;
                        if (!string.IsNullOrEmpty(noteId))
                        {
                            if (!card.linkedItems.Any(li => li.isNote && li.guid == noteId))
                            {
                                card.linkedItems.Add(LinkedItem.CreateNote(noteId));
                                changed = true;
                            }
                        }

                        if (changed) Save();
                        currentEvt.Use();
                    }
                }
            }

            }

            // ── Handle click on entire card for drag ──
            {
                var evt = Event.current;
                if (cardRect.width > 1 && cardRect.height > 1
                    && evt.type == EventType.MouseDown && evt.button == 0 && cardRect.Contains(evt.mousePosition))
                {
                    if (!_cardDragging)
                    {
                        _dragCard = card;
                        _dragSourceCol = col;
                        _cardDragStartPos = evt.mousePosition;
                        _cardDragging = false;
                        evt.Use();
                    }
                }
            }

            GUILayout.Space(3);
        }

        private void HandleCardDragHandle(TaskCard card, TaskColumn sourceColumn, Rect dragHandleRect)
        {
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && dragHandleRect.Contains(evt.mousePosition))
            {
                _dragCard = card;
                _dragSourceCol = sourceColumn;
                _cardDragStartPos = evt.mousePosition;
                _cardDragging = false;
                evt.Use();
            }
        }

        // Hit-test and state updates — called INSIDE the scroll view so mouse coords match column rects
        private void UpdateCardDragHitTest(TaskBoard board)
        {
            var evt = Event.current;
            if (_dragCard == null || _dragSourceCol == null) return;

            // Track hovered column (coordinates match here because we're inside the scroll view)
            _hoveredColumnDropId = "";
            if (_cardDragging)
            {
                foreach (var kv in _columnFullRects)
                {
                    if (kv.Value.Contains(evt.mousePosition))
                    {
                        _hoveredColumnDropId = kv.Key;
                        break;
                    }
                }
            }

            if (evt.type == EventType.MouseDrag)
            {
                if (!_cardDragging && Vector2.Distance(evt.mousePosition, _cardDragStartPos) > CardDragThreshold)
                {
                    _cardDragging = true;
                }
                Repaint();
            }

            if (evt.type == EventType.MouseUp)
            {
                if (_cardDragging)
                {
                    // Check full column rects for drop — coordinates are correct here
                    TaskColumn targetColumn = null;
                    foreach (var boardColumn in board.columns)
                    {
                        if (_columnFullRects.TryGetValue(boardColumn.id, out var rect) && rect.Contains(evt.mousePosition))
                        {
                            targetColumn = boardColumn;
                            break;
                        }
                    }

                    if (targetColumn != null && targetColumn != _dragSourceCol)
                    {
                        _dragSourceCol.cards.Remove(_dragCard);
                        if (!targetColumn.cards.Contains(_dragCard))
                            targetColumn.cards.Add(_dragCard);
                        Save();
                        TriggerSuccessNotification($"Card moved to {targetColumn.title}");
                    }
                    else if(targetColumn == _dragSourceCol)
                    {
                        TriggerErrorNotification("Card not moved, was released on the same column");
                    }
                    else if(targetColumn == null)
                    {
                        TriggerErrorNotification("Release the card over a different column to move it.");
                    }
                }

                _dragCard = null;
                _dragSourceCol = null;
                _cardDragging = false;
                _hoveredColumnDropId = "";
                Repaint();
                evt.Use();
            }
        }

        // Draw floating ghost card — called OUTSIDE the scroll view so it's not clipped
        private void DrawCardDragOverlay()
        {
            if (!_cardDragging || _dragCard == null) return;

            var evt = Event.current;
            var mousePos = evt.mousePosition;
            float ghostW = 220f;
            float ghostH = 40f;
            var ghostRect = new Rect(mousePos.x + 12f, mousePos.y - 10f, ghostW, ghostH);

            // Shadow
            var shadowRect = new Rect(ghostRect.x + 3, ghostRect.y + 3, ghostW, ghostH);
            EditorGUI.DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.3f));

            // Card background
            EditorGUI.DrawRect(ghostRect, EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.35f, 0.55f, 0.95f)
                : new Color(0.7f, 0.82f, 0.95f, 0.95f));

            // Color strip
            if (_dragCard.colorLabel > 0)
            {
                var stripColor = TBStyles.LabelColors[Mathf.Clamp(_dragCard.colorLabel, 0, TBStyles.LabelColors.Length - 1)];
                EditorGUI.DrawRect(new Rect(ghostRect.x, ghostRect.y, ghostRect.width, 3), stripColor);
            }

            // Title text
            var titleRect = new Rect(ghostRect.x + 8, ghostRect.y + 8, ghostRect.width - 16, 24);
            var ghostStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.white },
                clipping = TextClipping.Clip
            };
            string ghostTitle = _dragCard.title.Length > 30 ? _dragCard.title.Substring(0, 30) + "…" : _dragCard.title;
            EditorGUI.LabelField(titleRect, $"  ✋ {ghostTitle}", ghostStyle);

            // Border
            var borderColor = new Color(0.3f, 0.7f, 1f, 0.6f);
            if (_dragCard.archived) borderColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            EditorGUI.DrawRect(new Rect(ghostRect.x, ghostRect.y, ghostRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(ghostRect.x, ghostRect.yMax - 1, ghostRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(ghostRect.x, ghostRect.y, 1, ghostRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(ghostRect.xMax - 1, ghostRect.y, 1, ghostRect.height), borderColor);

            // Mouse-drag events already trigger redraws; avoid forced repaint loops.
        }

        private void HandleDragDrop(TaskBoard board)
        {
            if ((_dragCard != null || _noteDragging) && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _dragCard = null; _dragSourceCol = null; _cardDragging = false; _hoveredColumnDropId = "";
                _noteDragIdx = -1; _noteDragging = false; _hoveredFolderDropId = string.Empty;
                Repaint(); Event.current.Use();
            }
            if (_cardDragging && _dragCard != null)
            {
                var r = new Rect(10, position.height - 45, position.width - 20, 24);
                EditorGUI.DrawRect(r, new Color(0.2f, 0.5f, 0.85f, 0.85f));
                
                string targetColName = "";
                if (!string.IsNullOrEmpty(_hoveredColumnDropId))
                {
                    var col = board.columns.FirstOrDefault(c => c.id == _hoveredColumnDropId);
                    if (col != null) targetColName = col.title;
                }

                string targetHint = !string.IsNullOrEmpty(targetColName) ? $"release to drop! On {targetColName} column" : "drag over a column and release";
                EditorGUI.LabelField(r, $"  Moving card: \"{_dragCard.title}\"  — {targetHint}",
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft });
            }
        }

        private static bool IsBlankCard(TaskCard card)
        {
            if (card == null) return true;
            return string.IsNullOrWhiteSpace(card.title)
                   && string.IsNullOrWhiteSpace(card.description)
                   && string.IsNullOrWhiteSpace(card.category)
                   && string.IsNullOrWhiteSpace(card.dueDate)
                   && (card.checklistItems == null || card.checklistItems.Count == 0)
                   && card.colorLabel == 0
                   && card.priority == 0
                   && !card.archived;
        }

        // ════════════════════════════════════════════
        //  NOTES VIEW
        // ════════════════════════════════════════════
        private void DrawNotesView()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // ──────── LEFT PANEL: Folders + Note List ────────
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(260), GUILayout.ExpandHeight(true)))
                {

            // Header + search
            EditorGUILayout.LabelField("📝 Quick Notes", TBStyles.SectionLabel);
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("🔍", GUILayout.Width(16));
                _noteSearchFilter = EditorGUILayout.TextField(_noteSearchFilter, GUILayout.Height(20));
            }
            GUILayout.Space(4);

            // Add note + Import
            using (new EditorGUILayout.HorizontalScope())
            {
                _newNoteTitle = EditorGUILayout.TextField(_newNoteTitle, GUILayout.Height(22));
                if (GUILayout.Button(new GUIContent("+ Note", "Add a new Note"), GUILayout.Width(52), GUILayout.Height(22)))
                {
                    string fid = _selectedFolderId;
                    if (fid == "__unfiled__") fid = "";
                    string t = string.IsNullOrWhiteSpace(_newNoteTitle) ? "New Note" : _newNoteTitle.Trim();
                    var n = new QuickNote { title = t, folderId = fid };
                    _data.notes.Insert(0, n);
                    _newNoteTitle = "";
                    _selectedNote = 0;
                    Save();
                }
                if (GUILayout.Button(new GUIContent("📥", "Import Note from File"), GUILayout.Width(28), GUILayout.Height(22)))
                {
                    EditorApplication.delayCall += () => ImportNoteFromFile();
                }
            }
            GUILayout.Space(4);

            // ── Folders section ──
            DrawSeparator();
            _folderDropRects.Clear();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("📁 Folders", EditorStyles.boldLabel);
                if (_showAddFolder)
                {
                    _newFolderName = EditorGUILayout.TextField(_newFolderName, GUILayout.Width(80));
                    if (GUILayout.Button(new GUIContent("✔", "Save Folder Name"), TBStyles.IconButton) && !string.IsNullOrWhiteSpace(_newFolderName))
                    {
                        _data.noteFolders.Add(new NoteFolder(_newFolderName.Trim()));
                        _newFolderName = ""; _showAddFolder = false; Save();
                    }
                    if (GUILayout.Button(new GUIContent("✕", "Cancel Adding Folder"), TBStyles.IconButton)) _showAddFolder = false;
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("+", "Add Folder"), TBStyles.IconButton)) _showAddFolder = true;
                }
            }

            // "All Notes"
            bool allSelected = string.IsNullOrEmpty(_selectedFolderId);
            int totalNotes = _data.notes.Count;
            if (GUILayout.Button(allSelected ? $"▸ All Notes ({totalNotes})" : $"   All Notes ({totalNotes})",
                allSelected ? EditorStyles.boldLabel : EditorStyles.label))
            {
                _selectedFolderId = "";
                _selectedNote = -1;
            }

            // "Unfiled" — also a drop target
            int unfiledCount = _data.notes.Count(n => string.IsNullOrEmpty(n.folderId));
            bool unfiledSel = _selectedFolderId == "__unfiled__";
            var unfiledRect = GUILayoutUtility.GetRect(
                new GUIContent(unfiledSel ? $"▸ Unfiled ({unfiledCount})" : $"   Unfiled ({unfiledCount})"),
                unfiledSel ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.ExpandWidth(true));

            // Highlight unfiled during drag
            if (_noteDragging)
            {
                string dragNoteFolderId = (_noteDragIdx >= 0 && _noteDragIdx < _data.notes.Count) ? (_data.notes[_noteDragIdx].folderId ?? "") : "";
                bool isSource = string.IsNullOrEmpty(dragNoteFolderId); // note is currently unfiled
                if (_hoveredFolderDropId == "__unfiled__")
                    EditorGUI.DrawRect(unfiledRect, TBStyles.FolderDropHighlight);
                else if (isSource)
                    EditorGUI.DrawRect(unfiledRect, TBStyles.FolderDragSourceHighlight);
                else
                    EditorGUI.DrawRect(unfiledRect, TBStyles.FolderDragOtherHighlight);
            }
            if (GUI.Button(unfiledRect,
                unfiledSel ? $"▸ Unfiled ({unfiledCount})" : $"   Unfiled ({unfiledCount})",
                unfiledSel ? EditorStyles.boldLabel : EditorStyles.label))
            {
                _selectedFolderId = "__unfiled__";
                _selectedNote = -1;
            }
            _folderDropRects["__unfiled__"] = unfiledRect;

            // Each folder
            for (int fi = 0; fi < _data.noteFolders.Count; fi++)
            {
                var folder = _data.noteFolders[fi];
                int count = _data.notes.Count(n => n.folderId == folder.id);
                bool fsel = _selectedFolderId == folder.id;

                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    string fLabel = fsel ? $"▸ 📁 {folder.name} ({count})" : $"   📁 {folder.name} ({count})";
                    var folderBtnRect = GUILayoutUtility.GetRect(new GUIContent(fLabel),
                        fsel ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.ExpandWidth(true));

                    if (_noteDragging)
                    {
                        string dragNoteFolderId = (_noteDragIdx >= 0 && _noteDragIdx < _data.notes.Count) ? (_data.notes[_noteDragIdx].folderId ?? "") : "";
                        bool isSource = dragNoteFolderId == folder.id;
                        if (_hoveredFolderDropId == folder.id)
                            EditorGUI.DrawRect(folderBtnRect, TBStyles.FolderDropHighlight);
                        else if (isSource)
                            EditorGUI.DrawRect(folderBtnRect, TBStyles.FolderDragSourceHighlight);
                        else
                            EditorGUI.DrawRect(folderBtnRect, TBStyles.FolderDragOtherHighlight);
                    }

                    if (GUI.Button(folderBtnRect, fLabel, fsel ? EditorStyles.boldLabel : EditorStyles.label))
                    {
                        _selectedFolderId = folder.id;
                        _selectedNote = -1;
                    }
                    _folderDropRects[folder.id] = folderBtnRect;

                    if (GUILayout.Button(new GUIContent("⋮", "Folder Options"), TBStyles.IconButton))
                    {
                        var menu = new GenericMenu();
                        int capturedFi = fi;
                        menu.AddItem(new GUIContent("Rename"), false, () =>
                        {
                            string nn = EditorInputDialog.Show("Rename Folder", "Folder name:", folder.name);
                            if (!string.IsNullOrWhiteSpace(nn)) { folder.name = nn; Save(); Repaint(); }
                        });
                        menu.AddItem(new GUIContent("Export Folder (.md)"), false, () => ExportFolder(folder));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Folder"), false, () =>
                        {
                            if (EditorUtility.DisplayDialog("Delete Folder",
                                $"Delete folder \"{folder.name}\"?\nNotes inside will become unfiled.", "Delete", "Cancel"))
                            {
                                foreach (var n in _data.notes.Where(n => n.folderId == folder.id))
                                    n.folderId = "";
                                _data.noteFolders.RemoveAt(capturedFi);
                                if (_selectedFolderId == folder.id) _selectedFolderId = "";
                                Save(); Repaint();
                            }
                        });
                        menu.ShowAsContext();
                    }
                }
            }

            GUILayout.Space(4);
            DrawSeparator();
            GUILayout.Space(4);

            // ── Note list ──
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_notesListScroll))
            {
                _notesListScroll = scrollScope.scrollPosition;

                var filteredNotes = GetFilteredNotes();

                foreach (var (note, origIdx) in filteredNotes)
                {
                    DrawNoteListItem(note, origIdx);
                }
            }
                } // end left panel vertical scope

                GUILayout.Space(4);

                // ──────── RIGHT PANEL: Note Editor ────────
                DrawNoteEditor();
            } // end outer horizontal scope

            // Process drag only after all folder rects have been registered this frame.
            HandleNoteDragEvents();


            // Draw drag banner at bottom
            if (_noteDragging && _noteDragIdx >= 0 && _noteDragIdx < _data.notes.Count)
            {
                var note = _data.notes[_noteDragIdx];
                var r = new Rect(10, position.height - 30, position.width - 20, 24);
                EditorGUI.DrawRect(r, new Color(0.15f, 0.45f, 0.75f, 0.9f));
                EditorGUI.LabelField(r, $"  ✋ Dragging: \"{note.title}\"  — drop on a folder, or press Esc to cancel",
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft });
            }
        }

        // ── Draw a single note item in the list ──
        private void DrawNoteListItem(QuickNote note, int origIdx)
        {
            bool selected = _selectedNote == origIdx;
            var noteColor = TBStyles.LabelColors[Mathf.Clamp(note.colorIndex, 0, TBStyles.LabelColors.Length - 1)];

            // Use selected style or normal style
            var boxStyle = selected ? TBStyles.NoteBoxSelected : TBStyles.NoteBox;
            Rect itemRect;
            using (var scope = new EditorGUILayout.HorizontalScope(boxStyle))
            {
                itemRect = scope.rect;
                using (new EditorGUILayout.VerticalScope())
                {
                    string label = (note.pinned ? "📌 " : "") + note.title;
                    var titleStyle = selected
                        ? new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, fontSize = 12, wordWrap = true }
                        : new GUIStyle(EditorStyles.label) { fontSize = 12, wordWrap = true };
                    EditorGUILayout.LabelField(label, titleStyle);

                    var infoStyle = selected
                        ? new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.9f, 1f) } }
                        : EditorStyles.miniLabel;
                    EditorGUILayout.LabelField($"{note.modifiedDate}  •  {note.WordCount} words", infoStyle);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("↗", "Popout Note"), TBStyles.IconButton, GUILayout.Width(24), GUILayout.Height(22)))
                {
                    NotePopupWindow.Open(note, _data, () => { Save(); Repaint(); });
                }
            }

            // ── Drawn over the box during Repaint to save horizontal space ──
            if (Event.current.type == EventType.Repaint)
            {
                var lastRect = itemRect;
                // Selection accent (left edge)
                if (selected)
                {
                    var accentRect = new Rect(lastRect.x, lastRect.y, 4, lastRect.height);
                    EditorGUI.DrawRect(accentRect, TBStyles.NoteSelectedAccent);
                }

                // Color indicator (vertical pill shape inside the left padding)
                if (note.colorIndex > 0)
                {
                    float dotX = lastRect.x + (selected ? 7 : 4);
                    var dotRect = new Rect(dotX, lastRect.y + (lastRect.height - 18) / 2, 4, 18);
                    EditorGUI.DrawRect(dotRect, noteColor);
                }
            }

            // ── Handle click / drag on the entire row ──
            var evt = Event.current;
            if (itemRect.width > 1 && itemRect.height > 1) // rect is valid
            {
                if (evt.type == EventType.MouseDown && itemRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    if (_selectedNote != origIdx)
                    {
                        _selectedNote = origIdx;
                        _noteEditorScroll = Vector2.zero;
                        GUI.FocusControl(null); // release TextArea so it picks up the new note's content
                        GUIUtility.ExitGUI();
                    }
                    _noteDragIdx = origIdx;
                    _noteDragStartPos = evt.mousePosition;
                    _noteDragging = false;
                    evt.Use();
                    Repaint();
                }
            }

            GUILayout.Space(2);
        }

        // ── Handle drag events globally ──
        private void HandleNoteDragEvents()
        {
            var evt = Event.current;

            // Handle DragAndDrop events for folder drops
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                var noteId = DragAndDrop.GetGenericData("AwesomeTaskNoteId") as string;
                if (!string.IsNullOrEmpty(noteId))
                {
                    bool overAnyFolder = false;
                    foreach (var kv in _folderDropRects)
                    {
                        if (kv.Value.Contains(evt.mousePosition))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            _hoveredFolderDropId = kv.Key;
                            overAnyFolder = true;

                            if (evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                var note = _data.notes.FirstOrDefault(n => n.id == noteId);
                                if (note != null)
                                {
                                    string targetFolderId = kv.Key == "__unfiled__" ? "" : kv.Key;
                                    note.folderId = targetFolderId;
                                    Save();
                                    string folderName = kv.Key == "__unfiled__" ? "Quick Notes" : _data.noteFolders.FirstOrDefault(f => f.id == targetFolderId)?.name ?? "Folder";
                                    TriggerSuccessNotification($"Note moved to {folderName}");
                                }
                                _noteDragIdx = -1;
                                _noteDragging = false;
                                _hoveredFolderDropId = string.Empty;
                            }
                            evt.Use();
                            break;
                        }
                    }
                    if (!overAnyFolder)_hoveredFolderDropId = string.Empty;
                }
            }

            if (evt.type == EventType.DragExited)
            {
                _noteDragIdx = -1;
                _noteDragging = false;
                _hoveredFolderDropId = string.Empty;
            }

            if (_noteDragIdx < 0) return;

            // Manual drag start detection
            if (evt.type == EventType.MouseDrag && !_noteDragging)
            {
                float dist = Vector2.Distance(evt.mousePosition, _noteDragStartPos);
                if (dist > NoteDragThreshold)
                {
                    _noteDragging = true;
                    
                    // Unity standard drag-and-drop
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("AwesomeTaskNoteId", _data.notes[_noteDragIdx].id);
                    DragAndDrop.StartDrag(_data.notes[_noteDragIdx].title);
                    
                    Repaint();
                }
            }

            // Fallback for non-DragAndDrop scenarios or to clean up state
            if (evt.type == EventType.MouseUp)
            {
                _noteDragIdx = -1;
                _noteDragging = false;
                _hoveredFolderDropId = string.Empty;
                Repaint();
            }

            // Cancel drag on Escape
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && _noteDragging)
            {
                _noteDragIdx = -1;
                _noteDragging = false;
                _hoveredFolderDropId = string.Empty;
                evt.Use();
                Repaint();
            }
        }

        private List<(QuickNote note, int origIdx)> GetFilteredNotes()
        {
            bool hasSearch = !string.IsNullOrWhiteSpace(_noteSearchFilter);
            string search = hasSearch ? _noteSearchFilter.ToLowerInvariant() : "";

            return _data.notes
                .Select((n, i) => (note: n, origIdx: i))
                .Where(x =>
                {
                    // Folder filter
                    if (_selectedFolderId == "__unfiled__" && !string.IsNullOrEmpty(x.note.folderId)) return false;
                    if (!string.IsNullOrEmpty(_selectedFolderId) && _selectedFolderId != "__unfiled__"
                        && x.note.folderId != _selectedFolderId) return false;

                    // Search filter
                    if (hasSearch && !x.note.title.ToLowerInvariant().Contains(search)
                                  && !x.note.content.ToLowerInvariant().Contains(search))
                        return false;
                    return true;
                })
                .OrderByDescending(x => x.note.pinned)
                .ThenByDescending(x => x.note.modifiedDate)
                .ToList();
        }

        private void DrawNoteEditor()
        {
            using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                if (_selectedNote < 0 || _selectedNote >= _data.notes.Count)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select or create a note to start writing ✍️",
                        new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                    GUILayout.FlexibleSpace();
                    return;
                }

            var note = _data.notes[_selectedNote];

            // ── Title bar ──
            using (new EditorGUILayout.HorizontalScope())
            {
                string newTitle = EditorGUILayout.TextField(note.title, TBStyles.NoteTitle, GUILayout.Height(24));
                if (newTitle != note.title) { note.title = newTitle; MarkNoteModified(note); }

                // Pin
                GUIContent pinLabel = note.pinned ? new GUIContent("📌","Unpin Note") : new GUIContent("Pin","Pin Note");
                if (GUILayout.Button(pinLabel, GUILayout.Width(36), GUILayout.Height(24)))
                {
                    note.pinned = !note.pinned; Save(); Repaint();
                }

                // Color
                int newCol = EditorGUILayout.Popup(note.colorIndex, TBStyles.LabelNames, GUILayout.Width(70));
                if (newCol != note.colorIndex) { note.colorIndex = newCol; Save(); }

                // Move to folder
                if (GUILayout.Button(new GUIContent("📁", "Move Note to Folder"), GUILayout.Width(28), GUILayout.Height(24)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Unfiled"), string.IsNullOrEmpty(note.folderId), () =>
                    {
                        note.folderId = ""; Save(); Repaint();
                    });
                    foreach (var folder in _data.noteFolders)
                    {
                        string fid = folder.id;
                        menu.AddItem(new GUIContent(folder.name), note.folderId == fid, () =>
                        {
                            note.folderId = fid; Save(); Repaint();
                        });
                    }
                    menu.ShowAsContext();
                }

                // Export single note
                if (GUILayout.Button(new GUIContent("📤", "Export Note"), GUILayout.Width(28), GUILayout.Height(24)))
                {
                    EditorApplication.delayCall += () => ExportSingleNote(note);
                }

                if (GUILayout.Button(new GUIContent("↗", "Popout Note"), GUILayout.Width(28), GUILayout.Height(24)))
                {
                    NotePopupWindow.Open(note, _data, () => { Save(); Repaint(); });
                }

                // Delete
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button(new GUIContent("🗑", "Delete Note"), GUILayout.Width(28), GUILayout.Height(24)))
                {
                    int idx = _selectedNote;
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("Delete Note", $"Delete \"{note.title}\"?", "Delete", "Cancel"))
                        {
                            if (idx >= 0 && idx < _data.notes.Count)
                            {
                                _data.notes.RemoveAt(idx);
                                _selectedNote = -1;
                                Save();
                                Repaint();
                            }
                        }
                    };
                }
                GUI.backgroundColor = Color.white;
            }

            // ── Metadata row ──
            GUILayout.Space(2);
            Rect metadataRect = EditorGUILayout.GetControlRect(false, 18);
            string folderName = "Unfiled";
            if (!string.IsNullOrEmpty(note.folderId))
            {
                var f = _data.noteFolders.FirstOrDefault(x => x.id == note.folderId);
                if (f != null) folderName = f.name;
            }

            // Hover highlight for drag handle
            if (metadataRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(metadataRect, new Color(1f, 1f, 1f, 0.05f));
                EditorGUIUtility.AddCursorRect(metadataRect, MouseCursor.Link);
            }

            EditorGUI.LabelField(metadataRect,
                $"📁 {folderName}  |  Created: {note.createdDate}  |  Modified: {note.modifiedDate}  |  {note.WordCount} words, {note.CharCount} chars",
                EditorStyles.miniLabel);

            // Drag handle for the open note
            if (metadataRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _noteDragIdx = _selectedNote;
                _noteDragStartPos = Event.current.mousePosition;
                _noteDragging = false;
                Event.current.Use();
            }
            EditorGUI.LabelField(new Rect(metadataRect.xMax - 80, metadataRect.y, 80, 18), "(Drag to move)", EditorStyles.centeredGreyMiniLabel);

            DrawSeparator();
            GUILayout.Space(4);

            // ── Toolbar: image insert + edit/preview toggle ──
            note.imagePaths ??= new List<string>();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("🖼 Insert", EditorStyles.miniLabel, GUILayout.Width(44));

                if (GUILayout.Button(new GUIContent("📋 Paste", "Paste Image from Clipboard"), GUILayout.Width(60), GUILayout.Height(18)))
                {
                    PasteImageFromClipboard(note);
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button(new GUIContent("📎 Browse", "Browse for Image"), GUILayout.Width(70), GUILayout.Height(18)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string imgPath = EditorUtility.OpenFilePanel("Attach Image", "",
                            "png,jpg,jpeg,gif,bmp,tga,psd,tiff");
                        if (!string.IsNullOrEmpty(imgPath))
                        {
                            string assetPath = MarkdownRenderer.CopyImageToProject(imgPath);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                if (!note.imagePaths.Contains(assetPath))
                                    note.imagePaths.Add(assetPath);
                                string fileName = Path.GetFileName(assetPath);
                                note.content = (note.content ?? "") + $"\n![[{fileName}]]";
                                MarkNoteModified(note);
                                Repaint();
                            }
                        }
                    };
                }

                GUILayout.FlexibleSpace();

                // Edit / Preview toggle
                GUI.backgroundColor = _noteEditMode ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
                if (GUILayout.Button(new GUIContent("✏ Edit", "Edit Note Mode"), GUILayout.Width(54), GUILayout.Height(18)))
                    _noteEditMode = true;
                GUI.backgroundColor = !_noteEditMode ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
                if (GUILayout.Button(new GUIContent("👁 Preview", "Preview Note Mode"), GUILayout.Width(72), GUILayout.Height(18)))
                    _noteEditMode = false;
                GUI.backgroundColor = Color.white;
            }
            GUILayout.Space(3);

            // ── Content area ──
            HandleNoteDragDropImages(note);

            // ── Ctrl+V / Cmd+V to paste images from clipboard ──
            if (Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.V
                && (Event.current.control || Event.current.command))
            {
                if (TryPasteImageFromClipboard(note))
                {
                    Event.current.Use();
                    GUI.FocusControl(null); // release TextArea so it picks up the updated content
                    Repaint();
                }
            }

            if (_noteEditMode)
            {
                // ── Raw markdown editor ──
                using (var editorScope = new EditorGUILayout.ScrollViewScope(_noteEditorScroll))
                {
                    _noteEditorScroll = editorScope.scrollPosition;
                    string newContent = EditorGUILayout.TextArea(note.content, new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true, fontSize = 13, padding = new RectOffset(10, 10, 10, 10),
                        font = Font.CreateDynamicFontFromOSFont("Consolas", 13)
                    }, GUILayout.ExpandHeight(true));
                    if (newContent != note.content) { note.content = newContent; MarkNoteModified(note); }
                }
            }
            else
            {
                // ── Rendered preview (Obsidian-style inline images) ──
                using (var previewScope = new EditorGUILayout.ScrollViewScope(_noteEditorScroll))
                {
                    _noteEditorScroll = previewScope.scrollPosition;
                    if (MarkdownRenderer.DrawMarkdownPreview(note, (n) => MarkNoteModified(n)))
                    {
                        _hasAnimatedGif = true;
                    }
                }
            }
            }
        }

        // ── Export helpers ──
        private void ExportSingleNote(QuickNote note)
        {
            string defaultName = SanitizeFileName(note.title);
            string path = EditorUtility.SaveFilePanel("Export Note", "", defaultName, "md");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"# {note.title}");
            sb.AppendLine();
            sb.AppendLine($"*Created: {note.createdDate}  |  Modified: {note.modifiedDate}*");
            sb.AppendLine();
            sb.AppendLine(note.content);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("Exported", $"Note exported to:\n{path}", "OK");
        }

        private void ExportFolder(NoteFolder folder)
        {
            string path = EditorUtility.SaveFolderPanel("Export Folder", "", folder.name);
            if (string.IsNullOrEmpty(path)) return;

            var folderNotes = _data.notes.Where(n => n.folderId == folder.id).ToList();
            if (folderNotes.Count == 0)
            {
                EditorUtility.DisplayDialog("Empty", "This folder has no notes to export.", "OK");
                return;
            }

            int count = 0;
            foreach (var note in folderNotes)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# {note.title}");
                sb.AppendLine();
                sb.AppendLine($"*Created: {note.createdDate}  |  Modified: {note.modifiedDate}*");
                sb.AppendLine();
                sb.AppendLine(note.content);

                string fileName = SanitizeFileName(note.title) + ".md";
                File.WriteAllText(Path.Combine(path, fileName), sb.ToString(), Encoding.UTF8);
                count++;
            }
            EditorUtility.DisplayDialog("Exported", $"Exported {count} note(s) to:\n{path}", "OK");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private void MarkNoteModified(QuickNote note)
        {
            note.modifiedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Save();
        }

        // ════════════════════════════════════════════
        //  MARKDOWN PREVIEW RENDERER (Obsidian-style)
        // ════════════════════════════════════════════




        // ── Import helpers ──
        private void ImportNoteFromFile()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Import Note",  "",
                new[] {
                    "Text Files", "md,txt,rtf,log,csv,json,xml,html,htm,yaml,yml",
                    "All Files", "*"
                });
            if (string.IsNullOrEmpty(path)) return;

            ImportSingleFile(path);
        }

        private void ImportSingleFile(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path, Encoding.UTF8);

            // Strip markdown header if it's the first line
            string title = fileName;
            if (content.StartsWith("# "))
            {
                int lineEnd = content.IndexOf('\n');
                if (lineEnd > 0)
                {
                    title = content.Substring(2, lineEnd - 2).Trim();
                    content = content.Substring(lineEnd + 1).TrimStart('\r', '\n');
                }
                else
                {
                    title = content.Substring(2).Trim();
                    content = "";
                }
            }

            string fid = _selectedFolderId;
            if (fid == "__unfiled__") fid = "";

            var note = new QuickNote
            {
                title = string.IsNullOrWhiteSpace(title) ? fileName : title,
                content = content,
                folderId = fid
            };
            _data.notes.Insert(0, note);
            _selectedNote = 0;
            Save();
            Repaint();

            EditorUtility.DisplayDialog("Imported",
                $"Imported \"{note.title}\" ({note.WordCount} words) from:\n{path}", "OK");
        }

        // ── Image display ──

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

        // ── Clipboard paste ──
        private void PasteImageFromClipboard(QuickNote note)
        {
            MarkdownRenderer.PasteImageFromClipboard(note, n => MarkNoteModified(n), Repaint);
        }

        private bool TryPasteImageFromClipboard(QuickNote note)
        {
            return MarkdownRenderer.TryPasteImageFromClipboard(note, n => MarkNoteModified(n), Repaint);
        }

        /// <summary>Handle Unity editor drag-and-drop of image files onto note editor area.</summary>
        private void HandleNoteDragDropImages(QuickNote note)
        {
            MarkdownRenderer.HandleNoteDragDropImages(note, n => MarkNoteModified(n), Repaint);
        }

        private void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private void TriggerSuccessNotification(string message)
        {
            _successNotificationMessage = message;
            _successNotificationEndTime = EditorApplication.timeSinceStartup + 2.0; //2 seconds
            Repaint();
        }

        private void TriggerErrorNotification(string message)
        {
            _errorNotificationMessage = message;
            _errorNotificationEndTime = EditorApplication.timeSinceStartup + 2.0; // 2 seconds
            Repaint();
        }
        private void DrawErrorNotification()
        {
            if (EditorApplication.timeSinceStartup > _errorNotificationEndTime) return;

            // Fade out in last 0.5s
            float alpha = 1.0f;
            double timeLeft = _errorNotificationEndTime - EditorApplication.timeSinceStartup;
            if (timeLeft < 0.5) alpha = (float)(timeLeft / 0.5);

            var rect = new Rect(position.width * 0.5f - 120, position.height - 80, 350, 36);
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(139f, 0f, 0f, 0.9f * alpha)); // Dark red
            
            // Draw border
            var borderRect = rect;
            borderRect.height = 1; EditorGUI.DrawRect(borderRect, new Color(1,1,1, 0.2f * alpha));
            borderRect.y += rect.height - 1; EditorGUI.DrawRect(borderRect, new Color(1,1,1, 0.2f * alpha));
            
            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 1, alpha) },
                fontSize = 13
            };
            
            GUI.Label(rect, "" + _errorNotificationMessage, labelStyle);
            
            // Force repaints while notification is active for smooth fade
            EditorApplication.delayCall += Repaint;
        }
        private void DrawSuccessNotification()
        {
            if (EditorApplication.timeSinceStartup > _successNotificationEndTime) return;

            // Fade out in last 0.5s
            float alpha = 1.0f;
            double timeLeft = _successNotificationEndTime - EditorApplication.timeSinceStartup;
            if (timeLeft < 0.5) alpha = (float)(timeLeft / 0.5);

            var rect = new Rect(position.width * 0.5f - 120, position.height - 80, 240, 36);
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.45f, 0.15f, 0.9f * alpha)); // Dark green
            
            // Draw border
            var borderRect = rect;
            borderRect.height = 1; EditorGUI.DrawRect(borderRect, new Color(1,1,1, 0.2f * alpha));
            borderRect.y += rect.height - 1; EditorGUI.DrawRect(borderRect, new Color(1,1,1, 0.2f * alpha));
            
            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 1, alpha) },
                fontSize = 13
            };
            
            GUI.Label(rect, "✓ " + _successNotificationMessage, labelStyle);
            
            // Force repaints while notification is active for smooth fade
            EditorApplication.delayCall += Repaint;
        }

        private void CreateBoard(TaskBoard template)
        {
            string baseName = template != null ? template.name : "New Board";
            string newName = baseName;
            int suffix = 2;
            while (_data.boards.Any(b => b.name == newName))
            {
                newName = $"{baseName} {suffix}";
                suffix++;
            }

            TaskBoard newBoard = template != null ? template.Clone(true) : new TaskBoard(newName);
            newBoard.name = newName;
            _data.boards.Add(newBoard);
            _boardIndex = _data.boards.Count - 1;
            Save();
        }

        private void SaveCurrentAsTemplate()
        {
            string result = EditorInputDialog.Show("Save as Template", "Template Name:", Board.name);
            if (!string.IsNullOrWhiteSpace(result))
            {
                bool includeCards = true;
                bool hasCards = Board.columns.Any(c => c.cards.Count > 0);

                if (hasCards)
                {
                    int choice = EditorUtility.DisplayDialogComplex("Include Cards?",
                        "This board contains cards. Do you want to include them in the template, or only save the column layout?",
                        "Include Cards", "Only Columns", "Cancel");

                    if (choice == 2) return; // Cancel
                    includeCards = (choice == 0);
                }

                TaskBoard template = Board.Clone(true, includeCards);
                template.name = result;
                _data.templates.Add(template);
                Save();
                TriggerSuccessNotification("Template saved!");
            }
        }


        private void DrawAssigneeCircleBoard(Assignee assignee)
        {
            var rect = GUILayoutUtility.GetRect(24, 24);
            string initials = GetInitials(assignee.name);
            var circleStyle = new GUIStyle(TBStyles.AssigneeCircle) { fixedWidth = 24, fixedHeight = 24, fontSize = 9 };
            
            // Mask color matches card background (helpBox)
            Color maskColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
            
            TBStyles.DrawAssigneeIcon(rect, assignee, initials, circleStyle, maskColor);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var words = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpper();
            return (words[0][0].ToString() + words[words.Length - 1][0].ToString()).ToUpper();
        }
    }
}

