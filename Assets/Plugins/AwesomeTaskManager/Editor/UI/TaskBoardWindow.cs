using System;
using System.Collections.Generic;
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
    public class TaskBoardWindow : EditorWindow
    {
        // ── State ──
        private SaveData _data;
        private int _tab;
        private int _boardIndex;

        // GIF cache state
        private readonly HashSet<string> _failedGifPaths = new HashSet<string>();
        private static readonly Dictionary<string, GifDecoder> _gifCache = new Dictionary<string, GifDecoder>();
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        private Vector2 _boardScroll, _notesListScroll, _noteEditorScroll;
        private string _searchFilter = "";
        private string _categoryFilter = "";
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

        // ── Menu ──
        [MenuItem("Tools/Awesome Task Manager %#t")]
        public static void Open()
        {
            var w = GetWindow<TaskBoardWindow>("🎯 Awesome Task Manager");
            w.minSize = new Vector2(750, 420);
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
        private void OnGUI()
        {
            if (_data == null) { _data = Persistence.Load(); ClampBoard(); }

            _hasAnimatedGif = false;

            DrawTabs();
            GUILayout.Space(1);

            if (_tab == 0) DrawBoardView();
            else           DrawNotesView();

            // Throttled repaint for GIF animation (~15 fps)
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        private void DrawTabs()
        {
            var tabBarRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(34));
            EditorGUI.DrawRect(tabBarRect, EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.82f, 0.82f, 0.82f));

            GUILayout.Space(8);
            if (GUILayout.Button("📋 Board", _tab == 0 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                _tab = 0;
            GUILayout.Space(4);
            if (GUILayout.Button("📝 Notes", _tab == 1 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                _tab = 1;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  BOARD VIEW
        // ════════════════════════════════════════════
        private void DrawBoardView()
        {
            var board = Board;
            _cardDropRects.Clear();
            _columnFullRects.Clear();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24));
            {
                string[] names = _data.boards.Select(b => b.name).ToArray();
                int newIdx = EditorGUILayout.Popup(_boardIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(150));
                if (newIdx != _boardIndex) { _boardIndex = newIdx; _searchFilter = ""; _categoryFilter = ""; }

                if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    _data.boards.Add(new TaskBoard("New Board"));
                    _boardIndex = _data.boards.Count - 1;
                    Save();
                }
                if (_data.boards.Count > 1 && GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    if (EditorUtility.DisplayDialog("Delete Board", $"Delete \"{Board.name}\"?", "Delete", "Cancel"))
                    {
                        _data.boards.RemoveAt(_boardIndex);
                        _boardIndex = Mathf.Clamp(_boardIndex, 0, _data.boards.Count - 1);
                        Save();
                    }
                }

                GUILayout.Space(12);

                EditorGUILayout.LabelField("Category:", GUILayout.Width(58));
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

                if (GUILayout.Button("🏷", EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    CategoryEditorWindow.Open(_data, () => { Save(); Repaint(); });
                }
                
                GUILayout.Space(8);

                EditorGUILayout.LabelField("🔍", GUILayout.Width(18));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(140));

                GUILayout.Space(8);
                if (GUILayout.Button("▾ Show All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    var board2 = _data.boards[_boardIndex];
                    foreach (var c in board2.columns)
                        foreach (var card in c.cards)
                            card.showChecklist = true;
                    Save();
                }
                if (GUILayout.Button("▸ Hide All", EditorStyles.toolbarButton, GUILayout.Width(68)))
                {
                    var board2 = _data.boards[_boardIndex];
                    foreach (var c in board2.columns)
                        foreach (var card in c.cards)
                            card.showChecklist = false;
                    Save();
                }
                
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Board title row
            EditorGUILayout.BeginHorizontal();
            if (_renamingBoard)
            {
                _renameBoardName = EditorGUILayout.TextField(_renameBoardName, GUILayout.Width(250), GUILayout.Height(26));
                if (GUILayout.Button("✔", GUILayout.Width(26), GUILayout.Height(24)))
                {
                    if (!string.IsNullOrWhiteSpace(_renameBoardName)) board.name = _renameBoardName.Trim();
                    _renamingBoard = false; Save();
                }
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(24))) _renamingBoard = false;
            }
            else
            {
                EditorGUILayout.LabelField($"🎯 {board.name}", TBStyles.BoardHeader, GUILayout.Height(30));
                if (GUILayout.Button("✏", GUILayout.Width(26), GUILayout.Height(24)))
                {
                    _renamingBoard = true;
                    _renameBoardName = board.name;
                }
            }
            GUILayout.FlexibleSpace();

            if (_showAddColumn)
            {
                _newColumnTitle = EditorGUILayout.TextField(_newColumnTitle, GUILayout.Width(140), GUILayout.Height(22));
                if (GUILayout.Button("Add", GUILayout.Width(42), GUILayout.Height(22)) && !string.IsNullOrWhiteSpace(_newColumnTitle))
                {
                    board.columns.Add(new TaskColumn(_newColumnTitle.Trim()));
                    _newColumnTitle = ""; _showAddColumn = false; Save();
                }
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22))) _showAddColumn = false;
            }
            else
            {
                if (GUILayout.Button("+ Column", GUILayout.Width(80), GUILayout.Height(24)))
                    _showAddColumn = true;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            _boardScroll = EditorGUILayout.BeginScrollView(_boardScroll, true, false);
            EditorGUILayout.BeginHorizontal();

            float colWidth = Mathf.Max(260, (position.width - 40) / Mathf.Max(board.columns.Count, 1));
            colWidth = Mathf.Min(colWidth, 340);

            for (int ci = 0; ci < board.columns.Count; ci++)
            {
                DrawColumn(board.columns[ci], ci, board, colWidth);
                GUILayout.Space(4);
            }

            EditorGUILayout.EndHorizontal();

            // Hit-test card drag INSIDE the scroll view so coordinates match column rects
            UpdateCardDragHitTest(board);

            EditorGUILayout.EndScrollView();

            // Draw overlays OUTSIDE the scroll view (not clipped)
            HandleDragDrop(board);
            DrawCardDragOverlay();

            // ── Status bar ──
            var allCards = board.columns.SelectMany(c => c.cards).Where(c => !c.archived).ToList();
            int totalCards = allCards.Count;
            int overdueCount = allCards.Count(c => !string.IsNullOrWhiteSpace(c.dueDate)
                && DateTime.TryParse(c.dueDate, out var d) && d.Date < DateTime.Today);
            int dueTodayCount = allCards.Count(c => !string.IsNullOrWhiteSpace(c.dueDate)
                && DateTime.TryParse(c.dueDate, out var d) && d.Date == DateTime.Today);

            var statusRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(statusRect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.85f, 0.85f, 0.85f));

            string statusText = $"  {totalCards} card(s)";
            if (overdueCount > 0) statusText += $"  •  🔴 {overdueCount} overdue";
            if (dueTodayCount > 0) statusText += $"  •  🟠 {dueTodayCount} due today";
            statusText += $"  •  {board.columns.Count} column(s)";
            EditorGUI.LabelField(statusRect, statusText, EditorStyles.miniLabel);
        }

        private void DrawColumn(TaskColumn col, int colIdx, TaskBoard board, float width)
        {
            Color bg = colIdx % 2 == 0 ? TBStyles.ColumnBg : TBStyles.ColumnBgAlt;

            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            var colRect = EditorGUILayout.BeginVertical("box");
            EditorGUI.DrawRect(colRect, bg);

            // Column drag highlight overlay
            if (_cardDragging && _dragCard != null)
            {
                if (_hoveredColumnDropId == col.id)
                    EditorGUI.DrawRect(colRect, TBStyles.ColumnDropHovered);
                else if (_dragSourceCol != null && _dragSourceCol.id == col.id)
                    EditorGUI.DrawRect(colRect, TBStyles.ColumnDragSource);
                else
                    EditorGUI.DrawRect(colRect, TBStyles.ColumnDropOther);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{col.title}  ({col.cards.Count(c => !c.archived)})", TBStyles.ColumnHeader);

            if (board.columns.Count > 1)
            {
                if (colIdx > 0 && GUILayout.Button("◀", TBStyles.IconButton))
                {
                    board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx - 1, col);
                    Save(); GUIUtility.ExitGUI();
                }
                if (colIdx < board.columns.Count - 1 && GUILayout.Button("▶", TBStyles.IconButton))
                {
                    board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx + 1, col);
                    Save(); GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("⋮", TBStyles.IconButton))
            {
                var menu = new GenericMenu();
                int ci = colIdx;
                menu.AddItem(new GUIContent("Rename Column"), false, () =>
                {
                    string newName = EditorInputDialog.Show("Rename Column", "Column name:", col.title);
                    if (!string.IsNullOrWhiteSpace(newName)) { col.title = newName; Save(); Repaint(); }
                });
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
            EditorGUILayout.EndHorizontal();

            var sepRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sepRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            GUILayout.Space(4);

            bool hasTextFilter = !string.IsNullOrWhiteSpace(_searchFilter);
            string filter = hasTextFilter ? _searchFilter.ToLowerInvariant() : "";
            bool hasCatFilter = !string.IsNullOrEmpty(_categoryFilter);

            for (int i = 0; i < col.cards.Count; i++)
            {
                var card = col.cards[i];
                if (card.archived) continue;
                if (hasCatFilter && (card.category ?? "") != _categoryFilter) continue;
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

            EditorGUILayout.EndVertical();
            // Store full column rect for drag detection
            _columnFullRects[col.id] = colRect;
            EditorGUILayout.EndVertical();
        }

        private void DrawCard(TaskCard card, TaskColumn col, int idx)
        {
            var labelColor = TBStyles.LabelColors[Mathf.Clamp(card.colorLabel, 0, TBStyles.LabelColors.Length - 1)];

            var cardRect = EditorGUILayout.BeginVertical(TBStyles.CardBox);

            if (card.colorLabel > 0)
            {
                var stripRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(stripRect, labelColor);
                GUILayout.Space(2);
            }

            EditorGUILayout.BeginHorizontal();
            if (card.priority > 0)
                EditorGUILayout.LabelField(TBStyles.PriorityIcons[card.priority], GUILayout.Width(18));
            EditorGUILayout.LabelField(card.title, TBStyles.CardTitle);
            Rect dragHandleRect = GUILayoutUtility.GetRect(new GUIContent("↕"), TBStyles.IconButton, GUILayout.Width(26), GUILayout.Height(24));
            GUI.Box(dragHandleRect, "↕", TBStyles.IconButton);
            HandleCardDragHandle(card, col, dragHandleRect);
            if (GUILayout.Button("✏", TBStyles.IconButton))
            {
                CardDetailWindow.Show(card, _data, () => { Save(); Repaint(); }, () =>
                {
                    col.cards.Remove(card); Save(); Repaint();
                });
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(card.category))
            {
                var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.4f, 0.75f, 1f) }
                };
                EditorGUILayout.LabelField($"[{card.category}]", badgeStyle);
            }

            if (!string.IsNullOrWhiteSpace(card.description))
            {
                string preview = card.description.Length > 80 ? card.description.Substring(0, 80) + "…" : card.description;
                EditorGUILayout.LabelField(preview, EditorStyles.wordWrappedMiniLabel);
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

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(allDone ? $"✅ {done}/{card.checklistItems.Count} complete" : $"☑ {done}/{card.checklistItems.Count}", summaryStyle);
                string toggleLabel = card.showChecklist ? "▾" : "▸";
                if (GUILayout.Button(toggleLabel, TBStyles.IconButton, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    card.showChecklist = !card.showChecklist;
                    Save();
                }
                EditorGUILayout.EndHorizontal();
                
                // Show individual checklist items inline
                if (card.showChecklist)
                {
                    for (int ci = 0; ci < card.checklistItems.Count; ci++)
                    {
                        EditorGUILayout.BeginHorizontal();
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
                        if (itemText.Length > 40) itemText = itemText.Substring(0, 40) + "…";
                        EditorGUILayout.LabelField(itemText, itemStyle);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            // Image thumbnail
            if (!string.IsNullOrEmpty(card.imagePath))
            {
                DrawImageThumbnail(card.imagePath, 60f);
            }

            if (!string.IsNullOrWhiteSpace(card.dueDate))
            {
                var dueDateStyle = new GUIStyle(EditorStyles.miniLabel);
                string dueDateText = $"📅 {card.dueDate}";

                if (DateTime.TryParse(card.dueDate, out DateTime parsedDue))
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

            EditorGUILayout.BeginHorizontal();
            if (idx > 0 && GUILayout.Button("▲", TBStyles.IconButton))
            {
                col.cards.RemoveAt(idx); col.cards.Insert(idx - 1, card);
                Save(); GUIUtility.ExitGUI();
            }
            if (idx < col.cards.Count - 1 && GUILayout.Button("▼", TBStyles.IconButton))
            {
                col.cards.RemoveAt(idx); col.cards.Insert(idx + 1, card);
                Save(); GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_cardDragging && _dragCard == card)
            {
                // Dim the source card while dragging
                EditorGUI.DrawRect(cardRect, new Color(0f, 0f, 0f, 0.35f));
                var dragBanner = new Rect(cardRect.x + 4f, cardRect.yMax - 18f, Mathf.Max(60f, cardRect.width - 8f), 14f);
                EditorGUI.DrawRect(dragBanner, new Color(0.3f, 0.7f, 1f, 0.3f));
            }

            EditorGUILayout.EndVertical();

            // ── Handle click on entire card for drag ──
            {
                var evt = Event.current;
                if (cardRect.width > 1 && cardRect.height > 1
                    && evt.type == EventType.MouseDown && evt.button == 0 && cardRect.Contains(evt.mousePosition))
                {
                    _dragCard = card;
                    _dragSourceCol = col;
                    _cardDragStartPos = evt.mousePosition;
                    _cardDragging = false;
                    evt.Use();
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
                var r = new Rect(10, position.height - 30, position.width - 20, 24);
                EditorGUI.DrawRect(r, new Color(0.2f, 0.5f, 0.85f, 0.85f));
                string targetHint = !string.IsNullOrEmpty(_hoveredColumnDropId) ? "release to drop!" : "drag over a column and release";
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
            EditorGUILayout.BeginHorizontal();

            // ──────── LEFT PANEL: Folders + Note List ────────
            EditorGUILayout.BeginVertical("box", GUILayout.Width(260), GUILayout.ExpandHeight(true));

            // Header + search
            EditorGUILayout.LabelField("📝 Quick Notes", TBStyles.SectionLabel);
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(16));
            _noteSearchFilter = EditorGUILayout.TextField(_noteSearchFilter, GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            // Add note + Import
            EditorGUILayout.BeginHorizontal();
            _newNoteTitle = EditorGUILayout.TextField(_newNoteTitle, GUILayout.Height(22));
            if (GUILayout.Button("+ Note", GUILayout.Width(52), GUILayout.Height(22)))
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
            if (GUILayout.Button("📥", GUILayout.Width(28), GUILayout.Height(22)))
            {
                ImportNoteFromFile();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ── Folders section ──
            DrawSeparator();
            _folderDropRects.Clear();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📁 Folders", EditorStyles.boldLabel);
            if (_showAddFolder)
            {
                _newFolderName = EditorGUILayout.TextField(_newFolderName, GUILayout.Width(80));
                if (GUILayout.Button("✔", TBStyles.IconButton) && !string.IsNullOrWhiteSpace(_newFolderName))
                {
                    _data.noteFolders.Add(new NoteFolder(_newFolderName.Trim()));
                    _newFolderName = ""; _showAddFolder = false; Save();
                }
                if (GUILayout.Button("✕", TBStyles.IconButton)) _showAddFolder = false;
            }
            else
            {
                if (GUILayout.Button("+", TBStyles.IconButton)) _showAddFolder = true;
            }
            EditorGUILayout.EndHorizontal();

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

                EditorGUILayout.BeginHorizontal();
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

                if (GUILayout.Button("⋮", TBStyles.IconButton))
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
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            DrawSeparator();
            GUILayout.Space(4);

            // ── Note list ──
            _notesListScroll = EditorGUILayout.BeginScrollView(_notesListScroll);

            var filteredNotes = GetFilteredNotes();

            foreach (var (note, origIdx) in filteredNotes)
            {
                DrawNoteListItem(note, origIdx);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(4);

            // ──────── RIGHT PANEL: Note Editor ────────
            DrawNoteEditor();

            EditorGUILayout.EndHorizontal();

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
            var itemRect = EditorGUILayout.BeginHorizontal(boxStyle);

            // Left accent bar for selected note
            if (selected)
            {
                var accentRect = new Rect(itemRect.x, itemRect.y, 4, itemRect.height);
                EditorGUI.DrawRect(accentRect, TBStyles.NoteSelectedAccent);
            }

            // Color dot
            if (note.colorIndex > 0)
            {
                var dotRect = GUILayoutUtility.GetRect(6, 22, GUILayout.Width(6));
                EditorGUI.DrawRect(dotRect, noteColor);
                GUILayout.Space(4);
            }

            EditorGUILayout.BeginVertical();
            string label = (note.pinned ? "📌 " : "") + note.title;
            var titleStyle = selected
                ? new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, fontSize = 12 }
                : new GUIStyle(EditorStyles.label) { fontSize = 12 };
            EditorGUILayout.LabelField(label, titleStyle);

            var infoStyle = selected
                ? new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.9f, 1f) } }
                : EditorStyles.miniLabel;
            EditorGUILayout.LabelField($"{note.modifiedDate}  •  {note.WordCount} words", infoStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↗", TBStyles.IconButton, GUILayout.Width(24), GUILayout.Height(22)))
            {
                NotePopupWindow.Open(note, _data, () => { Save(); Repaint(); });
            }

            EditorGUILayout.EndHorizontal();

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

            if (_noteDragIdx < 0) return;

            _hoveredFolderDropId = string.Empty;
            if (_noteDragging)
            {
                foreach (var kv in _folderDropRects)
                {
                    if (kv.Value.Contains(evt.mousePosition))
                    {
                        _hoveredFolderDropId = kv.Key;
                        break;
                    }
                }
            }

            if (evt.type == EventType.MouseDrag && !_noteDragging)
            {
                float dist = Vector2.Distance(evt.mousePosition, _noteDragStartPos);
                if (dist > NoteDragThreshold)
                {
                    _noteDragging = true;
                    Repaint();
                }
            }

            if (evt.type == EventType.MouseUp && _noteDragIdx >= 0)
            {
                if (_noteDragging && _noteDragIdx < _data.notes.Count)
                {
                    var note = _data.notes[_noteDragIdx];
                    foreach (var kv in _folderDropRects)
                    {
                        if (kv.Value.Contains(evt.mousePosition))
                        {
                            string targetFolderId = kv.Key == "__unfiled__" ? "" : kv.Key;
                            note.folderId = targetFolderId;
                            Save();
                            break;
                        }
                    }
                }

                _noteDragIdx = -1;
                _noteDragging = false;
                _hoveredFolderDropId = string.Empty;
                evt.Use();
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
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedNote < 0 || _selectedNote >= _data.notes.Count)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select or create a note to start writing ✍️",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            var note = _data.notes[_selectedNote];

            // ── Title bar ──
            EditorGUILayout.BeginHorizontal();
            string newTitle = EditorGUILayout.TextField(note.title, TBStyles.NoteTitle, GUILayout.Height(24));
            if (newTitle != note.title) { note.title = newTitle; MarkNoteModified(note); }

            // Pin
            string pinLabel = note.pinned ? "📌" : "Pin";
            if (GUILayout.Button(pinLabel, GUILayout.Width(36), GUILayout.Height(24)))
            {
                note.pinned = !note.pinned; Save(); Repaint();
            }

            // Color
            int newCol = EditorGUILayout.Popup(note.colorIndex, TBStyles.LabelNames, GUILayout.Width(70));
            if (newCol != note.colorIndex) { note.colorIndex = newCol; Save(); }

            // Move to folder
            if (GUILayout.Button("📁", GUILayout.Width(28), GUILayout.Height(24)))
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
            if (GUILayout.Button("📤", GUILayout.Width(28), GUILayout.Height(24)))
            {
                ExportSingleNote(note);
            }

            if (GUILayout.Button("↗", GUILayout.Width(28), GUILayout.Height(24)))
            {
                NotePopupWindow.Open(note, _data, () => { Save(); Repaint(); });
            }

            // Delete
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            bool deleteNote = false;
            if (GUILayout.Button("🗑", GUILayout.Width(28), GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Delete Note", $"Delete \"{note.title}\"?", "Delete", "Cancel"))
                {
                    _data.notes.RemoveAt(_selectedNote);
                    _selectedNote = -1;
                    Save(); Repaint();
                    deleteNote = true;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (deleteNote)
            {
                EditorGUILayout.EndVertical();  // close the note editor "box" vertical
                EditorGUILayout.EndHorizontal(); // close the outer horizontal from DrawNotesView
                GUIUtility.ExitGUI();
                return;
            }

            // ── Metadata row ──
            GUILayout.Space(2);
            string folderName = "Unfiled";
            if (!string.IsNullOrEmpty(note.folderId))
            {
                var f = _data.noteFolders.FirstOrDefault(x => x.id == note.folderId);
                if (f != null) folderName = f.name;
            }
            EditorGUILayout.LabelField(
                $"📁 {folderName}  |  Created: {note.createdDate}  |  Modified: {note.modifiedDate}  |  {note.WordCount} words, {note.CharCount} chars",
                EditorStyles.miniLabel);

            DrawSeparator();
            GUILayout.Space(4);

            // ── Toolbar: image insert + edit/preview toggle ──
            note.imagePaths ??= new List<string>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🖼 Insert", EditorStyles.miniLabel, GUILayout.Width(44));

            if (GUILayout.Button("📋 Paste", GUILayout.Width(60), GUILayout.Height(18)))
            {
                PasteImageFromClipboard(note);
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("📎 Browse", GUILayout.Width(68), GUILayout.Height(18)))
            {
                string imgPath = EditorUtility.OpenFilePanel("Attach Image", "",
                    "png,jpg,jpeg,gif,bmp,tga,psd,tiff");
                if (!string.IsNullOrEmpty(imgPath))
                {
                    string assetPath = CopyImageToProject(imgPath);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (!note.imagePaths.Contains(assetPath))
                            note.imagePaths.Add(assetPath);
                        string fileName = Path.GetFileName(assetPath);
                        note.content = (note.content ?? "") + $"\n![[{fileName}]]";
                        MarkNoteModified(note);
                        GUI.FocusControl(null);
                    }
                }
            }

            GUILayout.FlexibleSpace();

            // Edit / Preview toggle
            GUI.backgroundColor = _noteEditMode ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
            if (GUILayout.Button("✏ Edit", GUILayout.Width(54), GUILayout.Height(18)))
                _noteEditMode = true;
            GUI.backgroundColor = !_noteEditMode ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
            if (GUILayout.Button("👁 Preview", GUILayout.Width(70), GUILayout.Height(18)))
                _noteEditMode = false;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);

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
                _noteEditorScroll = EditorGUILayout.BeginScrollView(_noteEditorScroll);
                string newContent = EditorGUILayout.TextArea(note.content, new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true, fontSize = 13, padding = new RectOffset(10, 10, 10, 10),
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 13)
                }, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (newContent != note.content) { note.content = newContent; MarkNoteModified(note); }
            }
            else
            {
                // ── Rendered preview (Obsidian-style inline images) ──
                _noteEditorScroll = EditorGUILayout.BeginScrollView(_noteEditorScroll);
                DrawMarkdownPreview(note);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
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

        private static readonly Regex _imageEmbedRegex =
            new Regex(@"!\[\[([^\]]+)\]\]", RegexOptions.Compiled);

        /// <summary>
        /// Renders note content with inline images, headers, bold, italic,
        /// bullet points, checkboxes, and horizontal rules — Obsidian style.
        /// </summary>
        private void DrawMarkdownPreview(QuickNote note)
        {
            if (string.IsNullOrEmpty(note.content))
            {
                EditorGUILayout.LabelField("(empty note — switch to Edit mode to add content)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            string[] lines = note.content.Split('\n');
            var textBuffer = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');

                // Check for image embeds  ![[filename.png]]
                if (_imageEmbedRegex.IsMatch(line))
                {
                    // Flush any pending text
                    FlushTextBlock(textBuffer);

                    // Render each image embed on this line
                    var matches = _imageEmbedRegex.Matches(line);

                    // Render any text before/between/after embeds
                    int lastEnd = 0;
                    foreach (Match m in matches)
                    {
                        if (m.Index > lastEnd)
                        {
                            string before = line.Substring(lastEnd, m.Index - lastEnd).Trim();
                            if (!string.IsNullOrEmpty(before))
                                EditorGUILayout.LabelField(before, EditorStyles.wordWrappedLabel);
                        }

                        string fileName = m.Groups[1].Value.Trim();
                        DrawInlineImage(note, fileName);
                        lastEnd = m.Index + m.Length;
                    }

                    if (lastEnd < line.Length)
                    {
                        string after = line.Substring(lastEnd).Trim();
                        if (!string.IsNullOrEmpty(after))
                            EditorGUILayout.LabelField(after, EditorStyles.wordWrappedLabel);
                    }
                    continue;
                }

                // Horizontal rule: ---, ***, ___
                string trimmed = line.Trim();
                if (trimmed.Length >= 3 &&
                    (trimmed.Replace("-", "").Trim() == "" ||
                     trimmed.Replace("*", "").Trim() == "" ||
                     trimmed.Replace("_", "").Trim() == ""))
                {
                    FlushTextBlock(textBuffer);
                    DrawSeparator();
                    GUILayout.Space(2);
                    continue;
                }

                // Headers: # H1, ## H2, ### H3
                if (trimmed.StartsWith("#"))
                {
                    FlushTextBlock(textBuffer);
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    string headerText = trimmed.Substring(level).Trim();
                    int fontSize = level <= 1 ? 20 : level == 2 ? 16 : 14;
                    var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = fontSize,
                        wordWrap = true,
                        padding = new RectOffset(4, 4, 4, 2)
                    };
                    GUILayout.Space(level <= 1 ? 8 : 4);
                    EditorGUILayout.LabelField(headerText, headerStyle);
                    if (level <= 2)
                    {
                        var r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    }
                    continue;
                }

                // Checkbox lines: - [ ] or - [x]
                if (trimmed.StartsWith("- [ ]") || trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]"))
                {
                    FlushTextBlock(textBuffer);
                    bool isChecked = trimmed[3] != ' ';
                    string itemText = trimmed.Substring(5).Trim();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    bool newChecked = EditorGUILayout.Toggle(isChecked, GUILayout.Width(18));
                    var cbStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
                    if (isChecked) cbStyle.fontStyle = FontStyle.Italic;
                    EditorGUILayout.LabelField(itemText, cbStyle);
                    EditorGUILayout.EndHorizontal();

                    // Update content if checkbox toggled
                    if (newChecked != isChecked)
                    {
                        lines[i] = newChecked
                            ? lines[i].Replace("- [ ]", "- [x]")
                            : lines[i].Replace("- [x]", "- [ ]").Replace("- [X]", "- [ ]");
                        note.content = string.Join("\n", lines);
                        MarkNoteModified(note);
                    }
                    continue;
                }

                // Bullet points: - item or * item
                if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && trimmed.Length > 2)
                {
                    FlushTextBlock(textBuffer);
                    string bulletText = trimmed.Substring(2);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    EditorGUILayout.LabelField("•", GUILayout.Width(12));
                    EditorGUILayout.LabelField(bulletText, new GUIStyle(EditorStyles.label) { wordWrap = true });
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                // Numbered list: 1. item
                if (trimmed.Length > 2 && char.IsDigit(trimmed[0]))
                {
                    int dotIdx = trimmed.IndexOf(". ");
                    if (dotIdx > 0 && dotIdx <= 3)
                    {
                        FlushTextBlock(textBuffer);
                        string numText = trimmed;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(16);
                        EditorGUILayout.LabelField(numText, new GUIStyle(EditorStyles.label) { wordWrap = true });
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                }

                // Regular text — accumulate into buffer
                textBuffer.AppendLine(line);
            }

            // Flush remaining text
            FlushTextBlock(textBuffer);
        }

        private void FlushTextBlock(StringBuilder buffer)
        {
            if (buffer.Length == 0) return;
            string text = buffer.ToString().TrimEnd();
            buffer.Clear();
            if (string.IsNullOrWhiteSpace(text)) { GUILayout.Space(6); return; }

            // Apply inline formatting: **bold** and *italic*
            // (Unity GUIStyle can't mix, so we strip markers for display)
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = Regex.Replace(text, @"\*(.+?)\*", "$1");
            text = Regex.Replace(text, @"__(.+?)__", "$1");
            text = Regex.Replace(text, @"_(.+?)_", "$1");
            // Inline code
            text = Regex.Replace(text, @"`(.+?)`", "[$1]");

            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 13,
                richText = false,
                padding = new RectOffset(6, 6, 2, 2)
            };
            EditorGUILayout.LabelField(text, style);
        }

        private void DrawInlineImage(QuickNote note, string fileName)
        {
            // Resolve the filename to a path in imagePaths
            string resolvedPath = null;
            if (note.imagePaths != null)
            {
                foreach (var p in note.imagePaths)
                {
                    if (Path.GetFileName(p) == fileName || p == fileName)
                    {
                        resolvedPath = p;
                        break;
                    }
                }
            }

            // Also try as a direct asset path
            if (resolvedPath == null)
            {
                string guessPath = $"Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/{fileName}";
                string fullGuess = Path.Combine(Application.dataPath, "..", guessPath);
                if (File.Exists(fullGuess))
                    resolvedPath = guessPath;
            }

            if (resolvedPath == null)
            {
                EditorGUILayout.LabelField($"⚠ Missing image: {fileName}", EditorStyles.miniLabel);
                return;
            }

            GUILayout.Space(4);
            DrawImageThumbnail(resolvedPath, 200f);
            GUILayout.Space(4);
        }

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
        private static readonly Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga", ".psd", ".tiff", ".tif" };

        /// <summary>
        /// Copies an external image into Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/
        /// so Unity's asset importer handles it (supports GIF, PSD, etc).
        /// Returns the Assets/... path, or the original path if already inside Assets.
        /// </summary>
        private static string CopyImageToProject(string externalPath)
        {
            if (string.IsNullOrEmpty(externalPath) || !File.Exists(externalPath)) return externalPath;

            // Already inside Assets?
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedInput = externalPath.Replace('\\', '/');
            if (normalizedInput.StartsWith(dataPath))
                return "Assets" + normalizedInput.Substring(dataPath.Length);

            // Copy into project
            string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            string fileName = Path.GetFileName(externalPath);
            string destPath = Path.Combine(destDir, fileName);

            // Avoid overwriting — add unique suffix if needed
            if (File.Exists(destPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                destPath = Path.Combine(destDir, $"{nameNoExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            }

            File.Copy(externalPath, destPath, false);

            // Defer Refresh to avoid calling during OnGUI (causes crashes / domain-reload mid-layout)
            EditorApplication.delayCall += () => AssetDatabase.Refresh();

            // Convert to asset path
            string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPath.Length);
            return assetPath;
        }

        /// <summary>Resolves an image path to an absolute file path.</summary>
        private static string ResolveImageFullPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            if (imagePath.StartsWith("Assets"))
                return Path.Combine(Application.dataPath, "..", imagePath).Replace('/', Path.DirectorySeparatorChar);
            return imagePath;
        }

        private void DrawImageThumbnail(string imagePath, float maxHeight)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            // Check if it's an animated GIF
            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (ext == ".gif")
            {
                DrawAnimatedGif(imagePath, maxHeight);
                return;
            }

            Texture2D tex = null;

            // Try loading from cache
            if (_imageCache.TryGetValue(imagePath, out var cached) && cached != null)
            {
                tex = cached;
            }
            else
            {
                // Normalize to asset path
                string assetPath = imagePath.Replace('\\', '/');
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (assetPath.StartsWith(dataPath))
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);

                // Try via AssetDatabase first (handles PSD, etc)
                if (assetPath.StartsWith("Assets"))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }

                // Fallback: load from absolute path (PNG/JPG only)
                if (tex == null && File.Exists(imagePath))
                {
                    try
                    {
                        // For unsupported formats, copy to project first
                        if (ext == ".psd" || ext == ".tiff" || ext == ".tif" || ext == ".bmp")
                        {
                            string newAssetPath = CopyImageToProject(imagePath);
                            if (newAssetPath.StartsWith("Assets"))
                                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath);
                        }
                        else
                        {
                            byte[] data = File.ReadAllBytes(imagePath);
                            tex = new Texture2D(2, 2);
                            if (!tex.LoadImage(data))
                            {
                                UnityEngine.Object.DestroyImmediate(tex);
                                tex = null;
                            }
                            else
                            {
                                tex.hideFlags = HideFlags.DontSave;
                            }
                        }
                    }
                    catch { tex = null; }
                }

                if (tex != null)
                    _imageCache[imagePath] = tex;
            }

            if (tex != null)
            {
                DrawTexture(tex, maxHeight);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠ Image not found: {Path.GetFileName(imagePath)}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawAnimatedGif(string imagePath, float maxHeight)
        {
            if (_failedGifPaths.Contains(imagePath))
            {
                EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                return;
            }

            // Ensure asset is copied into the project
            string assetPath = imagePath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets"))
            {
                string fullPath = ResolveImageFullPath(imagePath);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    try { assetPath = CopyImageToProject(fullPath); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[TaskBoard] Failed to copy GIF: {e.Message}");
                        _failedGifPaths.Add(imagePath);
                        EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                        return;
                    }
                }
            }

            // Try to decode & cache
            if (!_gifCache.TryGetValue(assetPath, out var gif))
            {
                try
                {
                    string fullPath = assetPath.StartsWith("Assets")
                        ? Path.Combine(Application.dataPath, "..", assetPath)
                        : assetPath;
                    gif = GifDecoder.Load(fullPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TaskBoard] Failed to decode GIF: {e.Message}");
                    gif = null;
                }
                _gifCache[assetPath] = gif; // cache even if null to avoid retrying
            }

            if (gif != null && gif.FrameCount > 0)
            {
                int frameIdx = gif.GetFrameIndex(EditorApplication.timeSinceStartup);
                var frame = gif.Frames[frameIdx];
                if (frame.texture != null)
                    DrawTexture(frame.texture, maxHeight);
                if (gif.FrameCount > 1)
                    _hasAnimatedGif = true;
                return;
            }

            // Fallback: try Unity's importer for a static preview
            Texture2D tex = null;
            if (_imageCache.TryGetValue(assetPath, out var cached) && cached != null)
                tex = cached;
            else if (assetPath.StartsWith("Assets"))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex != null) _imageCache[assetPath] = tex;
                else _failedGifPaths.Add(imagePath);
            }

            if (tex != null) DrawTexture(tex, maxHeight);
            else EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
        }

        private void DrawTexture(Texture2D tex, float maxHeight)
        {
            float aspect = (float)tex.width / tex.height;
            float displayHeight = Mathf.Min(maxHeight, tex.height);
            float displayWidth = displayHeight * aspect;
            float availWidth = EditorGUIUtility.currentViewWidth - 60f;
            if (displayWidth > availWidth)
            {
                displayWidth = availWidth;
                displayHeight = displayWidth / aspect;
            }

            var imgRect = GUILayoutUtility.GetRect(displayWidth, displayHeight,
                GUILayout.MaxWidth(displayWidth), GUILayout.MaxHeight(displayHeight));
            GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
        }

        // ── Clipboard paste ──
#if UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool OpenClipboard(System.IntPtr hWndNewOwner);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr GetClipboardData(uint format);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern System.IntPtr GlobalLock(System.IntPtr hMem);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(System.IntPtr hMem);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern int GlobalSize(System.IntPtr hMem);

        private const uint CF_DIB = 8;
        private const uint CF_HDROP = 15;
#endif

        private void PasteImageFromClipboard(QuickNote note)
        {
            if (TryPasteImageFromClipboard(note))
                return;
            EditorUtility.DisplayDialog("No Image", "No image found on the clipboard.\n\nTip: Copy an image or image file first, then paste.", "OK");
        }

        private bool TryPasteImageFromClipboard(QuickNote note)
        {
#if UNITY_EDITOR_WIN
            try
            {
                if (!OpenClipboard(System.IntPtr.Zero)) return false;
                try
                {
                    // Check for file drop (user copied a file in Explorer)
                    if (IsClipboardFormatAvailable(CF_HDROP))
                    {
                        var hDrop = GetClipboardData(CF_HDROP);
                        if (hDrop != System.IntPtr.Zero)
                        {
                            uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                            for (uint i = 0; i < fileCount; i++)
                            {
                                var sb = new StringBuilder(260);
                                DragQueryFile(hDrop, i, sb, 260);
                                string filePath = sb.ToString();
                                string fext = Path.GetExtension(filePath).ToLowerInvariant();
                                if (System.Array.Exists(ImageExtensions, e => e == fext))
                                {
                                    string assetPath = CopyImageToProject(filePath);
                                    note.imagePaths ??= new List<string>();
                                    if (!note.imagePaths.Contains(assetPath))
                                    {
                                        note.imagePaths.Add(assetPath);
                                        string fn = Path.GetFileName(assetPath);
                                        note.content = (note.content ?? "") + $"\n![[{fn}]]";
                                        MarkNoteModified(note);
                                        Repaint();
                                        return true;
                                    }
                                }
                            }
                        }
                    }

                    // Check for bitmap data (user did PrintScreen or copied from image editor)
                    if (IsClipboardFormatAvailable(CF_DIB))
                    {
                        var hMem = GetClipboardData(CF_DIB);
                        if (hMem != System.IntPtr.Zero)
                        {
                            int size = GlobalSize(hMem);
                            var ptr = GlobalLock(hMem);
                            if (ptr != System.IntPtr.Zero && size > 40)
                            {
                                try
                                {
                                    byte[] dibData = new byte[size];
                                    System.Runtime.InteropServices.Marshal.Copy(ptr, dibData, 0, size);

                                    // Parse DIB header directly into a Texture2D
                                    Texture2D tex = CreateTextureFromDib(dibData);
                                    if (tex != null)
                                    {
                                        byte[] pngBytes = tex.EncodeToPNG();
                                        UnityEngine.Object.DestroyImmediate(tex);

                                        string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
                                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                                        string fileName = $"pasted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                                        string destPath = Path.Combine(destDir, fileName);
                                        File.WriteAllBytes(destPath, pngBytes);
                                        AssetDatabase.Refresh();

                                        string dataPathNorm = Application.dataPath.Replace('\\', '/');
                                        string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPathNorm.Length);
                                        note.imagePaths ??= new List<string>();
                                        note.imagePaths.Add(assetPath);
                                        note.content = (note.content ?? "") + $"\n![[{fileName}]]";
                                        MarkNoteModified(note);
                                        Repaint();
                                        return true;
                                    }
                                }
                                finally { GlobalUnlock(hMem); }
                            }
                        }
                    }
                }
                finally { CloseClipboard(); }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TaskBoard] Clipboard paste failed: " + e.Message);
            }
#endif
            // Fallback: check if clipboard text is a file path to an image
            string clipText = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipText))
            {
                clipText = clipText.Trim().Trim('"');
                if (File.Exists(clipText))
                {
                    string fext = Path.GetExtension(clipText).ToLowerInvariant();
                    if (System.Array.Exists(ImageExtensions, e => e == fext))
                    {
                        string assetPath = CopyImageToProject(clipText);
                        note.imagePaths ??= new List<string>();
                        if (!note.imagePaths.Contains(assetPath))
                        {
                            note.imagePaths.Add(assetPath);
                            string fn = Path.GetFileName(assetPath);
                            note.content = (note.content ?? "") + $"\n![[{fn}]]";
                            MarkNoteModified(note);
                            Repaint();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

#if UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern uint DragQueryFile(System.IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        /// <summary>
        /// Directly parses DIB (CF_DIB) clipboard data into a Texture2D.
        /// Supports 24-bit and 32-bit uncompressed bitmaps.
        /// </summary>
        private static Texture2D CreateTextureFromDib(byte[] dibData)
        {
            if (dibData == null || dibData.Length < 40) return null;

            int headerSize = BitConverter.ToInt32(dibData, 0);
            int width = BitConverter.ToInt32(dibData, 4);
            int rawHeight = BitConverter.ToInt32(dibData, 8);
            int bitCount = BitConverter.ToInt16(dibData, 14);
            int compression = BitConverter.ToInt32(dibData, 16);

            // Only handle uncompressed 24-bit or 32-bit, or BI_BITFIELDS (3) for 32-bit
            if (width <= 0 || rawHeight == 0) return null;
            if (bitCount != 24 && bitCount != 32) return null;
            if (compression != 0 && compression != 3) return null;

            bool bottomUp = rawHeight > 0;
            int absHeight = Math.Abs(rawHeight);

            // Calculate palette/masks size
            int extraOffset = 0;
            if (compression == 3) // BI_BITFIELDS — 3 DWORDs of color masks after header
                extraOffset = 12;

            int pixelDataStart = headerSize + extraOffset;

            int bytesPerPixel = bitCount / 8;
            int rowStride = ((width * bytesPerPixel + 3) / 4) * 4; // 4-byte aligned

            if (pixelDataStart + rowStride * absHeight > dibData.Length)
            {
                // Try without extra offset
                pixelDataStart = headerSize;
                if (pixelDataStart + rowStride * absHeight > dibData.Length)
                    return null;
            }

            var tex = new Texture2D(width, absHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * absHeight];

            for (int y = 0; y < absHeight; y++)
            {
                // DIB bottom-up: row 0 is bottom of image
                int srcRow = bottomUp ? y : (absHeight - 1 - y);
                int srcRowStart = pixelDataStart + srcRow * rowStride;

                for (int x = 0; x < width; x++)
                {
                    int srcIdx = srcRowStart + x * bytesPerPixel;
                    if (srcIdx + bytesPerPixel > dibData.Length) break;

                    byte b = dibData[srcIdx];
                    byte g = dibData[srcIdx + 1];
                    byte r = dibData[srcIdx + 2];
                    byte a = bytesPerPixel == 4 ? dibData[srcIdx + 3] : (byte)255;

                    // Some 32-bit DIBs have alpha channel zeroed out
                    if (bytesPerPixel == 4 && a == 0) a = 255;

                    pixels[y * width + x] = new Color32(r, g, b, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }
#endif

        /// <summary>Handle Unity editor drag-and-drop of image files onto note editor area.</summary>
        private void HandleNoteDragDropImages(QuickNote note)
        {
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasImage = false;
                if (DragAndDrop.paths != null)
                {
                    foreach (var p in DragAndDrop.paths)
                    {
                        string ext = Path.GetExtension(p).ToLowerInvariant();
                        if (System.Array.Exists(ImageExtensions, e => e == ext))
                        { hasImage = true; break; }
                    }
                }

                if (hasImage)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        note.imagePaths ??= new List<string>();
                        foreach (var p in DragAndDrop.paths)
                        {
                            string ext = Path.GetExtension(p).ToLowerInvariant();
                            if (System.Array.Exists(ImageExtensions, e => e == ext))
                            {
                                string assetPath = CopyImageToProject(p);
                                if (!note.imagePaths.Contains(assetPath))
                                    note.imagePaths.Add(assetPath);
                                string fn = Path.GetFileName(assetPath);
                                note.content = (note.content ?? "") + $"\n![[{fn}]]";
                            }
                        }
                        MarkNoteModified(note);
                    }
                    evt.Use();
                }
            }
        }

        private void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }

    // ── Tiny input dialog utility ──
    public class EditorInputDialog : EditorWindow
    {
        private string _value;
        private string _label;
        private static string _result;

        public static string Show(string title, string label, string defaultValue = "")
        {
            _result = null;
            var w = CreateInstance<EditorInputDialog>();
            w.titleContent = new GUIContent(title);
            w._label = label;
            w._value = defaultValue;
            w.minSize = w.maxSize = new Vector2(320, 90);
            w.ShowModal();
            return _result;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_label);
            _value = EditorGUILayout.TextField(_value);
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))    { _result = _value; Close(); }
            if (GUILayout.Button("Cancel")) { _result = null;  Close(); }
            EditorGUILayout.EndHorizontal();
        }
    }
}

