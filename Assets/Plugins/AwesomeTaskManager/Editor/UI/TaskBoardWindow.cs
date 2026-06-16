using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    //Main Board Script
    public class TaskBoardWindow : EditorWindow
    {
        public static TaskBoardWindow Instance { get; private set; }

        private SaveData _data;
        [SerializeField] private int _tab;
        [SerializeField] private int _boardIndex;

        // GIF cache state
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        [SerializeField] private Vector2 _boardScroll, _notesListScroll, _noteEditorScroll;
        [SerializeField] private string _searchFilter = "";
        [SerializeField] private string _categoryFilter = "";
        [SerializeField] private string _assigneeFilter = ""; // New filter
        [SerializeField] private int _priorityFilter = 0; // 0 = All
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
        private string _linkHighlightCardId;
        private enum LinkHighlightMode { None, Children, Parents }
        private LinkHighlightMode _linkHighlightMode = LinkHighlightMode.None;
        private Dictionary<string, List<string>> _parentToChildren = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _childToParents = new Dictionary<string, List<string>>();
        private Dictionary<string, string> _cardTitles = new Dictionary<string, string>();

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
                if (data == null) return;

                if (data.boards.Count == 0) data.boards.Add(new TaskBoard("My First Board"));
                int boardIdx = Mathf.Clamp(data.lastBoardIndex, 0, data.boards.Count - 1);
                var board = data.boards[boardIdx];
                if (board.columns.Count == 0) board.columns.Add(new TaskColumn("To Do"));

                CardDetailWindow.ShowNew(data, board.id, board.columns[0].id, (newCard) =>
                {
                    // Fresh load to ensure we don't overwrite other recent changes
                    var latest = Persistence.Load();
                    if (latest == null) return;

                    var b = latest.boards.Find(x => x.id == board.id);
                    if (b != null && b.columns.Count > 0)
                    {
                        b.columns[0].cards.Add(newCard);
                        Persistence.Save(latest);
                        ReloadAllOpenWindows();
                    }
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
                if (data == null) return;

                var n = new QuickNote { title = "New Note" };
                data.notes.Insert(0, n);
                Persistence.Save(data);
                ReloadAllOpenWindows();
                NotePopupWindow.Open(n, data, () => {
                    ReloadAllOpenWindows();
                });
            }
        }

        public void CreateNewCardFromShortcut(bool focus = true)
        {
            if (_data == null) 
            { 
                _data = Persistence.Load(); 
                if (_data != null) ClampBoard(); 
            }
            if (_data == null) return;

            _tab = 0; // Switch to Board tab
            _searchFilter = ""; // Clear filters to ensure the new card is visible
            _categoryFilter = "";
            _priorityFilter = 0;
            _assigneeFilter = "";
            
            var board = Board;
            if (board.columns.Count == 0)
                board.columns.Add(new TaskColumn("To Do"));

            CardDetailWindow.ShowNew(_data, board.id, board.columns[0].id, (newCard) =>
            {
                AddCardFromDetail(board.id, board.columns[0].id, newCard);
            });
            if (focus) Focus();
        }

        public void CreateNewNoteFromShortcut(bool focus = true)
        {
            if (_data == null) 
            { 
                _data = Persistence.Load(); 
                if (_data != null) ClampBoard(); 
            }
            if (_data == null) return;

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
         
            NotePopupWindow.Open(n, _data, () => { LoadData(); });
        }

        // ── Lifecycle ──
        private void OnEnable()
        {
            Instance = this;
            // Always refresh from disk on enable; EditorWindow serialized state can be stale.
            _data = Persistence.Load();
            if (_data != null)
            {
                ApplyPostLoadVisualState();
            }
        }

        private void OnDisable() 
        { 
            if (Instance == this) Instance = null;
            Save(); 
        }

        public void LoadData()
        {
            var freshData = Persistence.Load();
            if (freshData == null) 
            {
                EditorUtility.DisplayDialog("Awesome Task Manager", "Failed to load data. The save file might be corrupted.", "OK");
                return;
            }

            _data = freshData;
            ApplyPostLoadVisualState();
            
            // Also notify any open sub-windows to reload their data from disk
            NotifySubWindowsToReload();

            Repaint();
        }

        private void ApplyPostLoadVisualState()
        {
            ClampBoard();
            RefreshLinkCache();
            ValidateFiltersAgainstData();

            // Clear transient drag/hover state so first paint after load/import is consistent.
            _cardDragging = false;
            _dragCard = null;
            _dragSourceCol = null;
            _hoveredColumnDropId = string.Empty;
            _hoveredFolderDropId = string.Empty;
            _cardDropRects.Clear();
            _columnFullRects.Clear();
            _folderDropRects.Clear();

            // Force style recreation to avoid stale GUIStyle/texture caches after reload/import.
            TBStyles.InvalidateCache();

            Repaint();
            EditorApplication.delayCall += () =>
            {
                if (this != null) Repaint();
            };
        }

        private void ValidateFiltersAgainstData()
        {
            if (_data == null) return;

            if (!string.IsNullOrEmpty(_categoryFilter) && !_data.categories.Contains(_categoryFilter))
                _categoryFilter = string.Empty;

            if (!string.IsNullOrEmpty(_assigneeFilter) && !_data.assignees.Any(a => a.id == _assigneeFilter))
                _assigneeFilter = string.Empty;

            if (_priorityFilter < 0 || _priorityFilter > TBStyles.PriorityNames.Length)
                _priorityFilter = 0;

            if (_tab < 0 || _tab > 1)
                _tab = 0;
        }

        private void RefreshAfterImport()
        {
            if (_data == null) return;

            ApplyPostLoadVisualState();
        }

        private IEnumerable<ImportFieldMappingProfile> GetImportProfilesForScope(ImportMappingScope scope)
        {
            if (_data?.importMappingProfiles == null) yield break;

            foreach (var profile in _data.importMappingProfiles)
            {
                if (profile != null && profile.MatchesScope(scope))
                    yield return profile;
            }
        }

        private IEnumerable<ImportFieldMappingProfile> GetAllImportProfilesForScope(ImportMappingScope scope)
        {
            foreach (var profile in ImportFieldMappingPresets.GetBuiltInProfiles(scope))
                yield return profile;

            foreach (var profile in GetImportProfilesForScope(scope))
                yield return profile;
        }

        private static string BuildHeaderSignature(string[] headers)
        {
            return ImportFieldMappingPresets.BuildHeaderSignature(headers);
        }

        private ImportFieldMapping GetSuggestedImportMapping(string path, string[] headers, ImportMappingScope scope, out string suggestedProfileId)
        {
            suggestedProfileId = null;
            string headerSignature = BuildHeaderSignature(headers);
            var profiles = GetAllImportProfilesForScope(scope).ToList();

            var signatureMatch = profiles
                .Where(p => p.MatchesHeaderSignature(headerSignature))
                .OrderByDescending(p => p.lastUsedUtcTicks)
                .FirstOrDefault();

            if (signatureMatch != null)
            {
                suggestedProfileId = signatureMatch.id;
                var matchedMapping = signatureMatch.mapping.Clone();
                matchedMapping.Normalize();
                return matchedMapping;
            }

            var rememberedProfile = profiles
                .Where(p => p != null && p.mapping != null && p.MatchesSourcePath(path))
                .OrderByDescending(p => string.IsNullOrWhiteSpace(p.sourceFilePattern) ? 0 : p.sourceFilePattern.Length)
                .ThenByDescending(p => p.lastUsedUtcTicks)
                .FirstOrDefault();

            if (rememberedProfile != null)
            {
                suggestedProfileId = rememberedProfile.id;
                var rememberedMapping = rememberedProfile.mapping.Clone();
                rememberedMapping.Normalize();
                return rememberedMapping;
            }

            var detectedMapping = ImportFieldMappingPresets.AutoDetect(headers);
            detectedMapping.Normalize();
            return detectedMapping;
        }

        private bool DeleteImportProfile(string profileId)
        {
            if (_data == null || string.IsNullOrWhiteSpace(profileId)) return false;

            if (_data.importMappingProfiles == null || _data.importMappingProfiles.Count == 0) return false;

            int removed = _data.importMappingProfiles.RemoveAll(p => p != null && p.id == profileId);
            if (removed <= 0) return false;

            Save();
            return true;
        }

        private void ApplyImportMappingPreferences(ImportFieldMappingWindowResult result, ImportMappingScope scope, string path, string[] headers)
        {
            if (_data == null || result?.mapping == null) return;

            _data.importMappingProfiles ??= new List<ImportFieldMappingProfile>();
            result.mapping.Normalize();

            string headerSignature = BuildHeaderSignature(headers);
            string normalizedPattern = result.rememberLastMappingForPattern
                ? ImportFieldMappingPresets.NormalizePattern(result.sourceFilePattern)
                : string.Empty;

            ImportFieldMappingProfile selectedProfile = null;
            if (!string.IsNullOrWhiteSpace(result.selectedProfileId))
                selectedProfile = _data.importMappingProfiles.FirstOrDefault(p => p != null && p.id == result.selectedProfileId && p.MatchesScope(scope));

            if (result.saveProfile)
            {
                string profileName = string.IsNullOrWhiteSpace(result.profileName)
                    ? BuildImportProfileName(scope, path, result.mapping)
                    : result.profileName.Trim();

                var profile = selectedProfile;
                if (profile == null)
                {
                    profile = _data.importMappingProfiles.FirstOrDefault(p =>
                        p != null &&
                        p.MatchesScope(scope) &&
                        string.Equals(p.profileName, profileName, StringComparison.OrdinalIgnoreCase));
                }

                if (profile == null)
                {
                    profile = new ImportFieldMappingProfile();
                    _data.importMappingProfiles.Add(profile);
                }

                profile.scope = scope;
                profile.profileName = profileName;
                profile.mapping = result.mapping.Clone();
                profile.rememberLastMappingForPattern = result.rememberLastMappingForPattern;
                profile.sourceFilePattern = result.rememberLastMappingForPattern ? normalizedPattern : string.Empty;
                profile.headerSignature = headerSignature;
                profile.lastUsedUtcTicks = DateTime.UtcNow.Ticks;
                profile.Normalize();
                return;
            }

            if (result.rememberLastMappingForPattern && !string.IsNullOrWhiteSpace(normalizedPattern))
            {
                var profile = _data.importMappingProfiles.FirstOrDefault(p =>
                    p != null &&
                    p.MatchesScope(scope) &&
                    p.rememberLastMappingForPattern &&
                    string.Equals(p.sourceFilePattern, normalizedPattern, StringComparison.OrdinalIgnoreCase));

                if (profile == null)
                {
                    profile = new ImportFieldMappingProfile
                    {
                        scope = scope,
                        profileName = BuildImportProfileName(scope, path, result.mapping)
                    };
                    _data.importMappingProfiles.Add(profile);
                }

                profile.scope = scope;
                profile.mapping = result.mapping.Clone();
                profile.rememberLastMappingForPattern = true;
                profile.sourceFilePattern = normalizedPattern;
                profile.headerSignature = headerSignature;
                profile.lastUsedUtcTicks = DateTime.UtcNow.Ticks;
                profile.Normalize();
                return;
            }

            if (selectedProfile != null)
            {
                selectedProfile.lastUsedUtcTicks = DateTime.UtcNow.Ticks;
                selectedProfile.scope = scope;
                selectedProfile.headerSignature = headerSignature;
                selectedProfile.Normalize();
            }
        }

        private static string BuildImportProfileName(ImportMappingScope scope, string sourcePath, ImportFieldMapping mapping)
        {
            string scopeName = scope == ImportMappingScope.Board ? "Board" : scope == ImportMappingScope.Column ? "Column" : scope == ImportMappingScope.Card ? "Card" : "Import";

            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName.Trim() + " " + scopeName + " Mapping";

            string presetName = mapping != null ? mapping.preset.ToString() : ImportMappingPreset.Generic.ToString();
            return scopeName + " " + presetName + " Mapping";
        }

        public static void ReloadAllOpenWindows()
        {
            var mainWindows = Resources.FindObjectsOfTypeAll<TaskBoardWindow>();
            if (mainWindows != null && mainWindows.Length > 0)
            {
                foreach (var w in mainWindows)
                {
                    if (w != null) w.LoadData();
                }
            }
            else
            {
                NotifySubWindowsToReload();
            }
        }

        private void ResetFilters()
        {
            _searchFilter = "";
            _categoryFilter = "";
            _assigneeFilter = "";
            _priorityFilter = 0;
            _noteSearchFilter = "";
        }

        private static void NotifySubWindowsToReload()
        {
            var cardWindows = Resources.FindObjectsOfTypeAll<CardDetailWindow>();
            foreach (var w in cardWindows) w.LoadData();

            var catWindows = Resources.FindObjectsOfTypeAll<CategoryEditorWindow>();
            foreach (var w in catWindows) w.LoadData();

            var assWindows = Resources.FindObjectsOfTypeAll<AssigneeManagerWindow>();
            foreach (var w in assWindows) w.LoadData();

            var noteWindows = Resources.FindObjectsOfTypeAll<NotePopupWindow>();
            foreach (var w in noteWindows) w.LoadData();
        }

        public void Save()
        {
            if (_data == null) return;
            _data.lastBoardIndex = _boardIndex;
            Persistence.Save(_data);
            ReloadAllOpenWindows();
        }

        public void AddCardFromDetail(string boardId, string columnId, TaskCard card)
        {
            LoadData(); // Fresh load to ensure we don't overwrite other recent changes
            if (_data == null) return;
            var board = _data.boards.FirstOrDefault(b => b.id == boardId);
            if (board != null)
            {
                var col = board.columns.FirstOrDefault(c => c.id == columnId);
                if (col != null)
                {
                    col.cards.Add(card);
                    Save();
                }
            }
        }

        public void UpdateCardFromDetail(TaskCard updatedCard)
        {
            ReloadAllOpenWindows(); // Reload everything from disk and notify all windows
        }

        public void DeleteCardFromDetail(string boardId, string columnId, string cardId)
        {
            LoadData(); // Fresh load to ensure we don't overwrite other recent changes
            if (_data == null) return;
            var board = _data.boards.FirstOrDefault(b => b.id == boardId);
            if (board != null)
            {
                var col = board.columns.FirstOrDefault(c => c.id == columnId);
                if (col != null)
                {
                    _data.CleanupReferencesToCard(cardId);
                    col.cards.RemoveAll(c => c.id == cardId);
                    if (_linkHighlightCardId == cardId)
                    {
                        _linkHighlightCardId = null;
                        _linkHighlightMode = LinkHighlightMode.None;
                    }
                    Save();
                    RefreshLinkCache();
                    ReloadAllOpenWindows();
                }
            }
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

        private void RefreshLinkCache()
        {
            _parentToChildren.Clear();
            _childToParents.Clear();
            _cardTitles.Clear();
            if (_data == null) return;

            foreach (var card in _data.AllCards())
            {
                _cardTitles[card.id] = card.title;
                if (card.checklistLinkedCardIds == null || card.checklistLinkedCardIds.Count == 0) continue;

                foreach (var childId in card.checklistLinkedCardIds)
                {
                    if (string.IsNullOrEmpty(childId)) continue;

                    if (!_parentToChildren.TryGetValue(card.id, out var children))
                    {
                        children = new List<string>();
                        _parentToChildren[card.id] = children;
                    }
                    if (!children.Contains(childId)) children.Add(childId);

                    if (!_childToParents.TryGetValue(childId, out var parents))
                    {
                        parents = new List<string>();
                        _childToParents[childId] = parents;
                    }
                    if (!parents.Contains(card.id)) parents.Add(card.id);
                }
            }
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
            if (_data == null) 
            {
                _data = Persistence.Load(); 
                if (_data != null) ApplyPostLoadVisualState();
            }

            if (_data == null)
            {
                EditorGUILayout.HelpBox("Task Board data could not be loaded. This might happen if the save file is corrupted or locked by another process (like a Git sync).\n\nPlease check the Console for detailed error messages.", MessageType.Error);
                if (GUILayout.Button("Retry Loading Data", GUILayout.Height(30)))
                {
                    LoadData();
                }
                return;
            }

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

            float windowWidth = position.width;
            bool showLabels = windowWidth > 1000;
            bool mediumWidth = windowWidth > 850;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(24)))
            {
                string[] names = _data.boards.Select(b => b.name).ToArray();
                int newIdx = EditorGUILayout.Popup(_boardIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(mediumWidth ? 150 : 120));
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select current board"));
                if (newIdx != _boardIndex)
                {
                    _boardIndex = newIdx;
                    ResetFilters();
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
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Export Board/Export Board (JSON - Native)..."), false, () => ExportBoard(board));
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Board/Import Board (JSON - Native)..."), false, ImportBoard);
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Column/Import Column from CSV..."), false, () => ImportColumnFromCSV(board));
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Column/Import Column from Excel..."), false, () => ImportColumnFromExcel(board));
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Column/Import Column (.atcl)..."), false, () => ImportColumnIntoBoard(board));
                    menu.AddSeparator("Beta (Export or Import)/");
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Export Board/Export Board... (CSV - External)"), false, () => ExportBoardToCSV(board));
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Export Board/Export Board... (Excel - External)"), false, () => ExportBoardToExcel(board));
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Board/Import Board... (CSV - External)"), false, ImportBoardFromCSV);
                    menu.AddItem(new GUIContent("Beta (Export or Import)/Import Board/Import Board... (Excel - External)"), false, ImportBoardFromExcel);
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
                    var targetBoard = _data.boards[_boardIndex];
                    string boardName = targetBoard.name;
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("Delete Board", $"Delete \"{boardName}\"?", "Delete", "Cancel"))
                        {
                            foreach (var col in targetBoard.columns)
                                foreach (var card in col.cards)
                                    _data.CleanupReferencesToCard(card.id);
                            
                            _data.boards.Remove(targetBoard);
                            _boardIndex = Mathf.Clamp(_boardIndex, 0, _data.boards.Count - 1);
                            Save();
                            RefreshLinkCache();
                            Repaint();
                        }
                    };
                }
            
                GUILayout.Space(showLabels ? 2:0);

                if (showLabels) EditorGUILayout.LabelField(new GUIContent("Category:","Filter tasks by category"), GUILayout.Width(58));
                
                if (!showLabels && GUILayout.Button(new GUIContent("🏷", "Category Editor"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    CategoryEditorWindow.Open(_data, () => { LoadData(); });
                }

                var catFilterOptions = new List<string> { "All" };
                catFilterOptions.AddRange(_data.categories);
                int catIdx = 0;
                if (!string.IsNullOrEmpty(_categoryFilter))
                {
                    int f = catFilterOptions.IndexOf(_categoryFilter);
                    if (f >= 0) catIdx = f;
                }
                int newCatIdx = EditorGUILayout.Popup(catIdx, catFilterOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(90));
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Filter tasks by category"));
                _categoryFilter = newCatIdx == 0 ? "" : catFilterOptions[newCatIdx];
                
                if (showLabels && GUILayout.Button(new GUIContent("🏷", "Category Editor"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    CategoryEditorWindow.Open(_data, () => { LoadData(); });
                }

                GUILayout.Space(showLabels ? 8 : 0);

                if (showLabels) EditorGUILayout.LabelField(new GUIContent("Assignee:", "Filter tasks by assignee"), GUILayout.Width(56));
                
                if (!showLabels && GUILayout.Button(new GUIContent("👥", "Assignee Manager"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    AssigneeManagerWindow.ShowWindow(_data, () => { LoadData(); });
                }

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
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Filter tasks by assignee"));
                if (newAssIdx == 0) _assigneeFilter = "";
                else
                {
                    var selectedName = assigneeOptions[newAssIdx];
                    var ass = _data.assignees.FirstOrDefault(a => a.name == selectedName);
                    if (ass != null) _assigneeFilter = ass.id;
                }

                if (showLabels && GUILayout.Button(new GUIContent("👥", "Assignee Manager"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    AssigneeManagerWindow.ShowWindow(_data, () => { LoadData(); });
                }

                GUILayout.Space(showLabels ? 8 : 4);

                if (showLabels) EditorGUILayout.LabelField(new GUIContent("Priority:", "Filter tasks by priority"), GUILayout.Width(52));
                else EditorGUILayout.LabelField(new GUIContent("🚩", "Filter tasks by priority"), GUILayout.Width(18));

                var priorityOptions = new List<string> { "All" };
                priorityOptions.AddRange(TBStyles.PriorityNames);
                _priorityFilter = EditorGUILayout.Popup(_priorityFilter, priorityOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(showLabels ? 80 : 60));
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Filter tasks by priority"));

                GUILayout.Space(showLabels ? 8 : 4);

                EditorGUILayout.LabelField(new GUIContent("🔍", "Search Tasks"), GUILayout.Width(18));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(mediumWidth ? 140 : 85));

                GUILayout.Space(showLabels ? 8 : 4);
                
                if (GUILayout.Button(new GUIContent(showLabels ? "▾ Show All" : "▾", "Show All Checklists"), EditorStyles.toolbarButton, GUILayout.Width(showLabels ? 70 : 25)))
                {
                    var board2 = _data.boards[_boardIndex];
                    foreach (var c in board2.columns)
                        foreach (var card in c.cards)
                            card.showChecklist = true;
                    Save();
                }
                if (GUILayout.Button(new GUIContent(showLabels ? "▸ Hide All" : "▸", "Hide All Checklists"), EditorStyles.toolbarButton, GUILayout.Width(showLabels ? 68 : 25)))
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
                    string headerText = $"🎯 {TBStyles.TruncateString(board.name, 50)}";
                    Vector2 headerSize = TBStyles.BoardHeader.CalcSize(new GUIContent(headerText));
                    EditorGUILayout.LabelField(headerText, TBStyles.BoardHeader, GUILayout.Width(headerSize.x + 4), GUILayout.Height(30));
                    
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
                            && (_priorityFilter == 0 || c.priority == _priorityFilter - 1)
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
                                {
                                    foreach(var card in col.cards) _data.CleanupReferencesToCard(card.id);
                                    col.cards.Clear(); 
                                    Save(); 
                                    RefreshLinkCache();
                                    Repaint(); 
                                }
                            });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Export Column/Export Column to CSV..."), false, () => ExportColumnToCSV(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Export Column/Export Column to Excel..."), false, () => ExportColumnToExcel(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Import Column/Import Column from CSV..."), false, () => ImportColumnFromCSV(board));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Import Column/Import Column from Excel..."), false, () => ImportColumnFromExcel(board));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Import Card/Import Card from CSV..."), false, () => ImportCardFromCSV(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Import Card/Import Card from Excel..."), false, () => ImportCardFromExcel(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Import Card/Import Card... (JSON)"), false, () => ImportCardIntoColumn(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Export Column/Export Column (.atcl)..."), false, () => ExportColumn(col));
                            menu.AddItem(new GUIContent("Beta (Export or Import)/Export Column/Import Column (.atcl)..."), false, () => ImportColumnIntoBoard(board));
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Delete Column"), false, () =>
                            {
                                if (EditorUtility.DisplayDialog("Delete Column", $"Delete \"{col.title}\" and all its cards?", "Delete", "Cancel"))
                                {
                                    foreach(var card in col.cards) _data.CleanupReferencesToCard(card.id);
                                    board.columns.RemoveAt(ci); 
                                    Save(); 
                                    RefreshLinkCache();
                                    Repaint(); 
                                }
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
                    bool hasPriFilter = _priorityFilter > 0;

                    for (int i = 0; i < col.cards.Count; i++)
                    {
                        var card = col.cards[i];
                        if (!_showArchived && card.archived) continue;
                        if (hasCatFilter && (card.category ?? "") != _categoryFilter) continue;
                        if (hasAssFilter && !card.assigneeIds.Contains(_assigneeFilter)) continue;
                        if (hasPriFilter && card.priority != _priorityFilter - 1) continue;
                        if (hasTextFilter && !card.title.ToLowerInvariant().Contains(filter)
                                          && !(card.description ?? "").ToLowerInvariant().Contains(filter)
                                          && !(card.category ?? "").ToLowerInvariant().Contains(filter))
                            continue;
                        DrawCard(card, col, i);
                    }

                    GUILayout.Space(6);

                    if (GUILayout.Button("+ Add Card", GUILayout.Height(26)))
                    {
                        string boardId = board.id;
                        string columnId = col.id;
                        CardDetailWindow.ShowNew(_data, boardId, columnId, (newCard) =>
                        {
                            AddCardFromDetail(boardId, columnId, newCard);
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
            
            bool isLinkHighlighted = _linkHighlightCardId == card.id;
            bool isChildOfHighlighted = _linkHighlightMode == LinkHighlightMode.Children && !string.IsNullOrEmpty(_linkHighlightCardId) && _parentToChildren.TryGetValue(_linkHighlightCardId, out var children) && children.Contains(card.id);
            bool isParentOfHighlighted = _linkHighlightMode == LinkHighlightMode.Parents && !string.IsNullOrEmpty(_linkHighlightCardId) && _childToParents.TryGetValue(_linkHighlightCardId, out var parents) && parents.Contains(card.id);

            bool shouldHighlight = isLinkHighlighted || isChildOfHighlighted || isParentOfHighlighted;

            Rect cardRect;
            using (var cardScope = new EditorGUILayout.VerticalScope(shouldHighlight ? TBStyles.CardBoxHighlighted : TBStyles.CardBox))
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
                        _data.SyncLinkedChecklistItems(card.id, card.completed);
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
                        string boardId = Board.id;
                        string columnId = col.id;
                        CardDetailWindow.Show(card, _data, boardId, columnId, () => { LoadData(); }, () =>
                        {
                            DeleteCardFromDetail(boardId, columnId, card.id);
                        });
                    }
                    
                     // Card Options (⋮)
                if (GUILayout.Button(new GUIContent("⋮", "Card Options"), TBStyles.IconButton, GUILayout.Width(22), GUILayout.Height(24)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent(card.archived ? "Unarchive Card" : "Archive Card"), false, () =>
                    {
                        card.archived = !card.archived;
                        Save();
                        RefreshLinkCache();
                        TriggerSuccessNotification(card.archived ? "Card archived" : "Card unarchived");
                        Repaint();
                    });
                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Duplicate Card"), false, () =>
                    {
                        var clone = card.Clone();
                        col.cards.Insert(idx + 1, clone);
                        Save();
                        RefreshLinkCache();
                        Repaint();
                    });

                    menu.AddItem(new GUIContent("Beta (Export Card)/as CSV..."), false, () => ExportCardToCSV(card, col));
                    menu.AddItem(new GUIContent("Beta (Export Card)/as Excel..."), false, () => ExportCardToExcel(card, col));
                    menu.AddSeparator("Beta (Export Card)/");
                    menu.AddItem(new GUIContent("Beta (Export Card)/as JSON (.atc)..."), false, () => ExportCard(card));

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
                            RefreshLinkCache();
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
                                RefreshLinkCache();
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
                            if (_linkHighlightCardId == card.id)
                            {
                                _linkHighlightCardId = null;
                                _linkHighlightMode = LinkHighlightMode.None;
                            }
                            _data.CleanupReferencesToCard(card.id);
                            col.cards.Remove(card);
                            Save();
                            RefreshLinkCache();
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
                    // Hide Checklist button
                    if (card.checklistItems.Count > 0)
                    {
                        string toggleLabel = card.showChecklist ? "▾" : "▸";
                        string toggleToolTip = card.showChecklist ? "Hide Checklist" : "Show Checklist";
                        if (GUILayout.Button(new GUIContent(toggleLabel, toggleToolTip), TBStyles.IconButton, GUILayout.Width(22), GUILayout.Height(24)))
                        {
                            card.showChecklist = !card.showChecklist;
                            Save();
                        }
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
                                
                                // Reverse sync: if this checklist item is linked to a card, update that card's completion status
                                if (card.checklistLinkedCardIds != null && ci < card.checklistLinkedCardIds.Count && !string.IsNullOrEmpty(card.checklistLinkedCardIds[ci]))
                                {
                                    var subId = card.checklistLinkedCardIds[ci];
                                    var subCard = _data.AllCards().FirstOrDefault(c => c.id == subId);
                                    if (subCard != null && subCard.completed != nowDone)
                                    {
                                        subCard.completed = nowDone;
                                        _data.SyncLinkedChecklistItems(subId, nowDone);
                                    }
                                }

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
                                if (note != null) NotePopupWindow.OpenInPreviewMode(note, _data, () => { LoadData(); });
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

              

                // Link indicators
                bool isParent = _parentToChildren.ContainsKey(card.id);
                bool isChild = _childToParents.ContainsKey(card.id);
                if (isParent)
                {
                    bool isThisActive = _linkHighlightCardId == card.id && _linkHighlightMode == LinkHighlightMode.Children;
                    string tooltip = isThisActive ? "Click to deselect" : "Parent Card: Click to highlight subtasks";
                    if (!isThisActive && _parentToChildren.TryGetValue(card.id, out var cIds))
                    {
                        var names = cIds.Select(cid => _cardTitles.TryGetValue(cid, out var t) ? t : "Unknown").ToArray();
                        tooltip = "Parent Card: Click to highlight subtasks:\n• " + string.Join("\n• ", names);
                    }
                    
                    if (GUILayout.Button(new GUIContent("🌳", tooltip), TBStyles.IconButton, GUILayout.Width(22), GUILayout.Height(24)))
                    {
                        if (_linkHighlightCardId == card.id && _linkHighlightMode == LinkHighlightMode.Children)
                        {
                            _linkHighlightCardId = null;
                            _linkHighlightMode = LinkHighlightMode.None;
                        }
                        else
                        {
                            _linkHighlightCardId = card.id;
                            _linkHighlightMode = LinkHighlightMode.Children;
                        }
                    }
                }
                if (isChild)
                {
                    bool isThisActive = _linkHighlightCardId == card.id && _linkHighlightMode == LinkHighlightMode.Parents;
                    string tooltip = isThisActive ? "Click to deselect" : "Subtask: Click to highlight parent card";
                    if (!isThisActive && _childToParents.TryGetValue(card.id, out var pIds))
                    {
                        var names = pIds.Select(pid => _cardTitles.TryGetValue(pid, out var t) ? t : "Unknown").ToArray();
                        tooltip = "Subtask: Click to highlight parent card: " + string.Join(", ", names);
                    }

                    if (GUILayout.Button(new GUIContent("🌿", tooltip), TBStyles.IconButton, GUILayout.Width(22), GUILayout.Height(24)))
                    {
                        if (_linkHighlightCardId == card.id && _linkHighlightMode == LinkHighlightMode.Parents)
                        {
                            _linkHighlightCardId = null;
                            _linkHighlightMode = LinkHighlightMode.None;
                        }
                        else
                        {
                            _linkHighlightCardId = card.id;
                            _linkHighlightMode = LinkHighlightMode.Parents;
                        }
                    }
                }
                


               
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
                        menu.AddItem(new GUIContent("Beta (Export or Import)/Export Folder (.md)"), false, () => ExportFolder(folder));
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
                    NotePopupWindow.Open(note, _data, () => { LoadData(); });
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
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Note color label"));
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
                    NotePopupWindow.Open(note, _data, () => { LoadData(); });
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

        private void ExportBoard(TaskBoard board)
        {
            if (board == null) return;
            string defaultName = SanitizeFileName(board.name);
            string path = EditorUtility.SaveFilePanel("Export Board", "", defaultName, "atb");
            if (string.IsNullOrEmpty(path)) return;

            var exportData = new ExportBoardData { board = board };

            // Gather assignees used in this board
            var assigneeIds = new HashSet<string>();
            foreach (var col in board.columns)
                foreach (var card in col.cards)
                    foreach (var aid in card.assigneeIds)
                        if (!string.IsNullOrEmpty(aid)) assigneeIds.Add(aid);

            exportData.assignees = _data.assignees.Where(a => assigneeIds.Contains(a.id)).ToList();

            // Gather category colors used in this board
            var categories = new HashSet<string>();
            foreach (var col in board.columns)
                foreach (var card in col.cards)
                    if (!string.IsNullOrEmpty(card.category))
                        categories.Add(card.category);

            exportData.categoryColors = _data.categoryColors.Where(cc => categories.Contains(cc.category)).ToList();

            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            TriggerSuccessNotification("Board exported successfully");
        }

        private void ExportBoardToExcel(TaskBoard board)
        {
            if (board == null) return;
            string defaultName = SanitizeFileName(board.name);
            string path = EditorUtility.SaveFilePanel("Export Board to Excel", "", defaultName, "xml");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            
            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"sHeader\">");
            sb.AppendLine("   <Font ss:Bold=\"1\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("  </Style>");
            sb.AppendLine(" </Styles>");

            sb.AppendLine($" <Worksheet ss:Name=\"{SecurityElement.Escape(board.name)}\">");
            sb.AppendLine("  <Table>");

            // Header
            string[] headers = {
                "Task ID", "Task Link", "Task Type", "Task Custom ID", "Task Name", "Task Content",
                "Status", "Date created", "Date created Text", "Due date", "Due date Text",
                "Start date", "Start date Text", "Parent ID", "Subtask IDs", "Attachments",
                "Assignees", "Tags", "Priority", "List Name", "Space Name", "Time Estimated",
                "Time Estimated Text", "Checklists", "Comments", "Assigned Comments",
                "Time Spent", "Time Spent Text", "Rolled Up Time", "Rolled Up Time Text"
            };

            sb.AppendLine("   <Row ss:StyleID=\"sHeader\">");
            foreach (var h in headers)
            {
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(h)}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");

            foreach (var col in board.columns)
            {
                if (col.cards.Count == 0)
                {
                    // Export empty column
                    string[] rowValues = new string[30];
                    for (int j = 0; j < 30; j++) rowValues[j] = "";
                    rowValues[2] = "status_placeholder";
                    rowValues[6] = col.title; // Status
                    rowValues[19] = board.name; // List Name
                    rowValues[20] = "AwesomeTaskManager"; // Space Name
                    rowValues[15] = "[]"; // Attachments
                    rowValues[16] = "[]"; // Assignees
                    rowValues[17] = "[]"; // Tags
                    rowValues[25] = "0"; // Assigned Comments

                    sb.AppendLine("   <Row>");
                    foreach (var val in rowValues)
                    {
                        sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(val ?? "")}</Data></Cell>");
                    }
                    sb.AppendLine("   </Row>");
                }
                else
                {
                    foreach (var card in col.cards)
                    {
                        string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
                        string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
                        string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
                        
                        List<string> checklistStrings = new List<string>();
                        for (int i = 0; i < card.checklistItems.Count; i++)
                        {
                            string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                            checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
                        }
                        string checklists = string.Join("; ", checklistStrings);

                        string[] rowValues = new string[30];
                        rowValues[0] = card.id;
                        rowValues[1] = ""; // Task Link
                        rowValues[2] = "task"; // Task Type
                        rowValues[3] = ""; // Custom ID
                        rowValues[4] = card.title;
                        rowValues[5] = card.description;
                        rowValues[6] = col.title;
                        rowValues[7] = card.createdDate;
                        rowValues[8] = card.createdDate;
                        rowValues[9] = card.dueDate;
                        rowValues[10] = card.dueDate;
                        rowValues[11] = ""; // Start date
                        rowValues[12] = ""; // Start date Text
                        rowValues[13] = ""; // Parent ID
                        rowValues[14] = ""; // Subtask IDs
                        rowValues[15] = "[]"; // Attachments
                        rowValues[16] = assignees;
                        rowValues[17] = tags;
                        rowValues[18] = priority;
                        rowValues[19] = board.name;
                        rowValues[20] = "AwesomeTaskManager";
                        rowValues[21] = ""; // Time Est
                        rowValues[22] = ""; 
                        rowValues[23] = checklists;
                        rowValues[24] = ""; // Comments
                        rowValues[25] = "0";
                        rowValues[26] = "";
                        rowValues[27] = "";
                        rowValues[28] = "";
                        rowValues[29] = "";

                        sb.AppendLine("   <Row>");
                        foreach (var val in rowValues)
                        {
                            sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(val ?? "")}</Data></Cell>");
                        }
                        sb.AppendLine("   </Row>");
                    }
                }
            }

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Board exported to Excel (XML Spreadsheet) successfully");
        }

        private void ImportBoardFromExcel()
        {
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Board from Excel", "", "xlsx,xml");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                List<string[]> rows = new List<string[]>();
                if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXlsx(path);
                }
                else if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXmlSpreadsheet(path);
                }

                if (rows.Count < 2)
                {
                    TriggerErrorNotification("Excel file is empty or missing header");
                    return;
                }

                string[] headers = rows[0];
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Board, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Board Mapping (Excel)", ImportMappingScope.Board, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        ApplyImportMappingPreferences(result, ImportMappingScope.Board, path, headers);
                        ImportBoardRowsWithMapping(rows, path, result.mapping, "Board imported from Excel successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        TriggerErrorNotification("Failed to import Excel: " + ex.Message);
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                TriggerErrorNotification("Failed to import Excel: " + ex.Message);
            }
        }

        private List<string[]> ParseXlsx(string path)
        {
            List<string[]> rows = new List<string[]>();
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                // Load shared strings
                List<string> sharedStrings = new List<string>();
                var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (sharedStringsEntry != null)
                {
                    using (Stream s = sharedStringsEntry.Open())
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.Load(s);
                        XmlNodeList tNodes = xml.GetElementsByTagName("t");
                        foreach (XmlNode t in tNodes)
                        {
                            sharedStrings.Add(t.InnerText);
                        }
                    }
                }

                // Load sheet1
                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry != null)
                {
                    using (Stream s = sheetEntry.Open())
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.Load(s);
                        XmlNodeList rowNodes = xml.GetElementsByTagName("row");
                        foreach (XmlNode rowNode in rowNodes)
                        {
                            // Cells can be missing or in different order, but let's assume sequential for basic ClickUp exports
                            // Real xlsx parsing needs to handle cell references (A1, B1 etc)
                            List<string> rowValues = new List<string>();
                            int lastCol = -1;
                            foreach (XmlNode cNode in rowNode.ChildNodes)
                            {
                                if (cNode.Name != "c") continue;
                                
                                // Handle missing cells by looking at 'r' attribute (e.g. A1, C1 means B1 is missing)
                                string r = cNode.Attributes["r"]?.Value;
                                if (!string.IsNullOrEmpty(r))
                                {
                                    int colIdx = GetColumnIndex(r);
                                    while (lastCol < colIdx - 1)
                                    {
                                        rowValues.Add("");
                                        lastCol++;
                                    }
                                    lastCol = colIdx;
                                }

                                string t = cNode.Attributes["t"]?.Value;
                                XmlNode vNode = cNode.SelectSingleNode("*[local-name()='v']");
                                string val = vNode != null ? vNode.InnerText : "";

                                if (t == "s")
                                {
                                    int sIdx = int.Parse(val);
                                    rowValues.Add(sIdx < sharedStrings.Count ? sharedStrings[sIdx] : "");
                                }
                                else
                                {
                                    rowValues.Add(val);
                                }
                            }
                            rows.Add(rowValues.ToArray());
                        }
                    }
                }
            }
            return rows;
        }

        private int GetColumnIndex(string cellRef)
        {
            string colPart = Regex.Replace(cellRef, @"[\d]", "");
            int column = 0;
            for (int i = 0; i < colPart.Length; i++)
            {
                column *= 26;
                column += (colPart[i] - 'A' + 1);
            }
            return column - 1;
        }

        private List<string[]> ParseXmlSpreadsheet(string path)
        {
            List<string[]> rows = new List<string[]>();
            XmlDocument xml = new XmlDocument();
            xml.Load(path);
            
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");

            XmlNodeList rowNodes = xml.SelectNodes("//ss:Table/ss:Row", nsmgr);
            foreach (XmlNode rowNode in rowNodes)
            {
                List<string> rowValues = new List<string>();
                foreach (XmlNode cellNode in rowNode.SelectNodes("ss:Cell", nsmgr))
                {
                    // Handle ss:Index attribute if some cells are skipped
                    var indexAttr = cellNode.Attributes["ss:Index"];
                    if (indexAttr != null)
                    {
                        int index = int.Parse(indexAttr.Value) - 1;
                        while (rowValues.Count < index) rowValues.Add("");
                    }

                    XmlNode dataNode = cellNode.SelectSingleNode("ss:Data", nsmgr);
                    rowValues.Add(dataNode != null ? dataNode.InnerText : "");
                }
                rows.Add(rowValues.ToArray());
            }
            return rows;
        }

        private void ExportBoardToCSV(TaskBoard board)
        {
            if (board == null) return;
            string defaultName = SanitizeFileName(board.name);
            string path = EditorUtility.SaveFilePanel("Export Board to CSV", "", defaultName, "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            // ClickUp compatible header
            sb.AppendLine("Task ID,Task Link,Task Type,Task Custom ID,Task Name,Task Content,Status,Date created,Date created Text,Due date,Due date Text,Start date,Start date Text,Parent ID,Subtask IDs,Attachments,Assignees,Tags,Priority,List Name,Space Name,Time Estimated,Time Estimated Text,Checklists,Comments,Assigned Comments,Time Spent,Time Spent Text,Rolled Up Time,Rolled Up Time Text");

            foreach (var col in board.columns)
            {
                if (col.cards.Count == 0)
                {
                    // Export empty column
                    string taskId = "";
                    string taskLink = "";
                    string taskType = "status_placeholder";
                    string customId = "";
                    string taskName = ""; // Empty name indicates it's just a column placeholder
                    string taskContent = "";
                    string status = col.title;
                    string dateCreated = "";
                    string dateCreatedText = "";
                    string dueDate = "";
                    string dueDateText = "";
                    string startDate = "";
                    string startDateText = "";
                    string parentId = "";
                    string subtaskIds = "";
                    string attachments = "[]";
                    string listName = board.name;
                    string spaceName = "AwesomeTaskManager";
                    string timeEst = "";
                    string timeEstText = "";
                    string checklists = "";
                    string comments = "";
                    string assignedComments = "0";
                    string timeSpent = "";
                    string timeSpentText = "";
                    string rolledUpTime = "";
                    string rolledUpTimeText = "";
                    string assignees = "[]";
                    string tags = "[]";
                    string priority = "";

                    sb.Append($"\"{EscapeCSV(taskId)}\",");
                    sb.Append($"\"{EscapeCSV(taskLink)}\",");
                    sb.Append($"\"{EscapeCSV(taskType)}\",");
                    sb.Append($"\"{EscapeCSV(customId)}\",");
                    sb.Append($"\"{EscapeCSV(taskName)}\",");
                    sb.Append($"\"{EscapeCSV(taskContent)}\",");
                    sb.Append($"\"{EscapeCSV(status)}\",");
                    sb.Append($"\"{EscapeCSV(dateCreated)}\",");
                    sb.Append($"\"{EscapeCSV(dateCreatedText)}\",");
                    sb.Append($"\"{EscapeCSV(dueDate)}\",");
                    sb.Append($"\"{EscapeCSV(dueDateText)}\",");
                    sb.Append($"\"{EscapeCSV(startDate)}\",");
                    sb.Append($"\"{EscapeCSV(startDateText)}\",");
                    sb.Append($"\"{EscapeCSV(parentId)}\",");
                    sb.Append($"\"{EscapeCSV(subtaskIds)}\",");
                    sb.Append($"\"{EscapeCSV(attachments)}\",");
                    sb.Append($"\"{EscapeCSV(assignees)}\",");
                    sb.Append($"\"{EscapeCSV(tags)}\",");
                    sb.Append($"\"{EscapeCSV(priority)}\",");
                    sb.Append($"\"{EscapeCSV(listName)}\",");
                    sb.Append($"\"{EscapeCSV(spaceName)}\",");
                    sb.Append($"\"{EscapeCSV(timeEst)}\",");
                    sb.Append($"\"{EscapeCSV(timeEstText)}\",");
                    sb.Append($"\"{EscapeCSV(checklists)}\",");
                    sb.Append($"\"{EscapeCSV(comments)}\",");
                    sb.Append($"\"{EscapeCSV(assignedComments)}\",");
                    sb.Append($"\"{EscapeCSV(timeSpent)}\",");
                    sb.Append($"\"{EscapeCSV(timeSpentText)}\",");
                    sb.Append($"\"{EscapeCSV(rolledUpTime)}\",");
                    sb.AppendLine($"\"{EscapeCSV(rolledUpTimeText)}\"");
                }
                else
                {
                    foreach (var card in col.cards)
                    {
                        string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
                        string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
                        string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
                        
                        // Format checklist: "Name (done); Name (todo)"
                        List<string> checklistStrings = new List<string>();
                        for (int i = 0; i < card.checklistItems.Count; i++)
                        {
                            string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                            checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
                        }
                        string checklists = string.Join("; ", checklistStrings);

                        // Fields mapping
                        string taskId = card.id;
                        string taskLink = ""; // We don't have links
                        string taskType = "task";
                        string customId = "";
                        string taskName = card.title;
                        string taskContent = card.description;
                        string status = col.title;
                        string dateCreated = card.createdDate;
                        string dateCreatedText = card.createdDate;
                        string dueDate = card.dueDate;
                        string dueDateText = card.dueDate;
                        string startDate = "";
                        string startDateText = "";
                        string parentId = ""; // No parent/child in CSV yet
                        string subtaskIds = "";
                        string attachments = "[]"; 
                        string listName = board.name;
                        string spaceName = "AwesomeTaskManager";
                        string timeEst = "";
                        string timeEstText = "";
                        string comments = "";
                        string assignedComments = "0";
                        string timeSpent = "";
                        string timeSpentText = "";
                        string rolledUpTime = "";
                        string rolledUpTimeText = "";

                        sb.Append($"\"{EscapeCSV(taskId)}\",");
                        sb.Append($"\"{EscapeCSV(taskLink)}\",");
                        sb.Append($"\"{EscapeCSV(taskType)}\",");
                        sb.Append($"\"{EscapeCSV(customId)}\",");
                        sb.Append($"\"{EscapeCSV(taskName)}\",");
                        sb.Append($"\"{EscapeCSV(taskContent)}\",");
                        sb.Append($"\"{EscapeCSV(status)}\",");
                        sb.Append($"\"{EscapeCSV(dateCreated)}\",");
                        sb.Append($"\"{EscapeCSV(dateCreatedText)}\",");
                        sb.Append($"\"{EscapeCSV(dueDate)}\",");
                        sb.Append($"\"{EscapeCSV(dueDateText)}\",");
                        sb.Append($"\"{EscapeCSV(startDate)}\",");
                        sb.Append($"\"{EscapeCSV(startDateText)}\",");
                        sb.Append($"\"{EscapeCSV(parentId)}\",");
                        sb.Append($"\"{EscapeCSV(subtaskIds)}\",");
                        sb.Append($"\"{EscapeCSV(attachments)}\",");
                        sb.Append($"\"{EscapeCSV(assignees)}\",");
                        sb.Append($"\"{EscapeCSV(tags)}\",");
                        sb.Append($"\"{EscapeCSV(priority)}\",");
                        sb.Append($"\"{EscapeCSV(listName)}\",");
                        sb.Append($"\"{EscapeCSV(spaceName)}\",");
                        sb.Append($"\"{EscapeCSV(timeEst)}\",");
                        sb.Append($"\"{EscapeCSV(timeEstText)}\",");
                        sb.Append($"\"{EscapeCSV(checklists)}\",");
                        sb.Append($"\"{EscapeCSV(comments)}\",");
                        sb.Append($"\"{EscapeCSV(assignedComments)}\",");
                        sb.Append($"\"{EscapeCSV(timeSpent)}\",");
                        sb.Append($"\"{EscapeCSV(timeSpentText)}\",");
                        sb.Append($"\"{EscapeCSV(rolledUpTime)}\",");
                        sb.AppendLine($"\"{EscapeCSV(rolledUpTimeText)}\"");
                    }
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Board exported to CSV (ClickUp compatible) successfully");
        }

        private static string EscapeCSV(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "\"\"");
        }

        private void ExportColumnToCSV(TaskColumn col)
        {
            if (col == null) return;
            string defaultName = SanitizeFileName(col.title);
            string path = EditorUtility.SaveFilePanel("Export Column to CSV", "", defaultName, "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            // ClickUp compatible header
            sb.AppendLine("Task ID,Task Link,Task Type,Task Custom ID,Task Name,Task Content,Status,Date created,Date created Text,Due date,Due date Text,Start date,Start date Text,Parent ID,Subtask IDs,Attachments,Assignees,Tags,Priority,List Name,Space Name,Time Estimated,Time Estimated Text,Checklists,Comments,Assigned Comments,Time Spent,Time Spent Text,Rolled Up Time,Rolled Up Time Text");

            string boardName = Board != null ? Board.name : "Board";

            if (col.cards.Count == 0)
            {
                // Export empty column placeholder
                sb.Append($"\"\","); // Task ID
                sb.Append($"\"\","); // Task Link
                sb.Append($"\"status_placeholder\","); // Task Type
                sb.Append($"\"\","); // Custom ID
                sb.Append($"\"\","); // Task Name
                sb.Append($"\"\","); // Task Content
                sb.Append($"\"{EscapeCSV(col.title)}\",");
                sb.Append($"\"\","); // Date created
                sb.Append($"\"\","); // Date created Text
                sb.Append($"\"\","); // Due date
                sb.Append($"\"\","); // Due date Text
                sb.Append($"\"\","); // Start date
                sb.Append($"\"\","); // Start date Text
                sb.Append($"\"\","); // Parent ID
                sb.Append($"\"\","); // Subtask IDs
                sb.Append($"\"[]\","); // Attachments
                sb.Append($"\"[]\","); // Assignees
                sb.Append($"\"[]\","); // Tags
                sb.Append($"\"\","); // Priority
                sb.Append($"\"{EscapeCSV(boardName)}\",");
                sb.Append($"\"AwesomeTaskManager\",");
                sb.Append($"\"\","); // Time Est
                sb.Append($"\"\","); 
                sb.Append($"\"\","); // Checklists
                sb.Append($"\"\","); // Comments
                sb.Append($"\"0\",");
                sb.Append($"\"\",");
                sb.Append($"\"\",");
                sb.Append($"\"\",");
                sb.AppendLine($"\"\"");
            }
            else
            {
                foreach (var card in col.cards)
                {
                    string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
                    string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
                    string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
                    
                    List<string> checklistStrings = new List<string>();
                    for (int i = 0; i < card.checklistItems.Count; i++)
                    {
                        string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                        checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
                    }
                    string checklists = string.Join("; ", checklistStrings);

                    sb.Append($"\"{EscapeCSV(card.id)}\",");
                    sb.Append($"\"\","); // Task Link
                    sb.Append($"\"task\","); // Task Type
                    sb.Append($"\"\","); // Custom ID
                    sb.Append($"\"{EscapeCSV(card.title)}\",");
                    sb.Append($"\"{EscapeCSV(card.description)}\",");
                    sb.Append($"\"{EscapeCSV(col.title)}\",");
                    sb.Append($"\"{EscapeCSV(card.createdDate)}\",");
                    sb.Append($"\"{EscapeCSV(card.createdDate)}\",");
                    sb.Append($"\"{EscapeCSV(card.dueDate)}\",");
                    sb.Append($"\"{EscapeCSV(card.dueDate)}\",");
                    sb.Append($"\"\","); // Start date
                    sb.Append($"\"\","); // Start date Text
                    sb.Append($"\"\","); // Parent ID
                    sb.Append($"\"\","); // Subtask IDs
                    sb.Append($"\"[]\","); // Attachments
                    sb.Append($"\"{EscapeCSV(assignees)}\",");
                    sb.Append($"\"{EscapeCSV(tags)}\",");
                    sb.Append($"\"{EscapeCSV(priority)}\",");
                    sb.Append($"\"{EscapeCSV(boardName)}\",");
                    sb.Append($"\"AwesomeTaskManager\",");
                    sb.Append($"\"\","); // Time Est
                    sb.Append($"\"\","); 
                    sb.Append($"\"{EscapeCSV(checklists)}\",");
                    sb.Append($"\"\","); // Comments
                    sb.Append($"\"0\",");
                    sb.Append($"\"\",");
                    sb.Append($"\"\",");
                    sb.Append($"\"\",");
                    sb.AppendLine($"\"\"");
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Column exported to CSV successfully");
        }

        private void ExportColumnToExcel(TaskColumn col)
        {
            if (col == null) return;
            string defaultName = SanitizeFileName(col.title);
            string path = EditorUtility.SaveFilePanel("Export Column to Excel", "", defaultName, "xml");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            
            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"sHeader\">");
            sb.AppendLine("   <Font ss:Bold=\"1\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("  </Style>");
            sb.AppendLine(" </Styles>");

            sb.AppendLine($" <Worksheet ss:Name=\"{SecurityElement.Escape(col.title)}\">");
            sb.AppendLine("  <Table>");

            string[] headers = {
                "Task ID", "Task Link", "Task Type", "Task Custom ID", "Task Name", "Task Content",
                "Status", "Date created", "Date created Text", "Due date", "Due date Text",
                "Start date", "Start date Text", "Parent ID", "Subtask IDs", "Attachments",
                "Assignees", "Tags", "Priority", "List Name", "Space Name", "Time Estimated",
                "Time Estimated Text", "Checklists", "Comments", "Assigned Comments",
                "Time Spent", "Time Spent Text", "Rolled Up Time", "Rolled Up Time Text"
            };

            sb.AppendLine("   <Row ss:StyleID=\"sHeader\">");
            foreach (var h in headers)
            {
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(h)}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");

            string boardName = Board != null ? Board.name : "Board";

            if (col.cards.Count == 0)
            {
                // Export empty column placeholder
                string[] rowValues = new string[30];
                for (int j = 0; j < 30; j++) rowValues[j] = "";
                rowValues[2] = "status_placeholder";
                rowValues[6] = col.title;
                rowValues[19] = boardName;
                rowValues[20] = "AwesomeTaskManager";
                rowValues[25] = "0";

                sb.AppendLine("   <Row>");
                foreach (var val in rowValues)
                {
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(val ?? "")}</Data></Cell>");
                }
                sb.AppendLine("   </Row>");
            }
            else
            {
                foreach (var card in col.cards)
                {
                    string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
                    string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
                    string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
                    
                    List<string> checklistStrings = new List<string>();
                    for (int i = 0; i < card.checklistItems.Count; i++)
                    {
                        string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                        checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
                    }
                    string checklists = string.Join("; ", checklistStrings);

                    string[] rowValues = new string[30];
                    rowValues[0] = card.id;
                    rowValues[4] = card.title;
                    rowValues[5] = card.description;
                    rowValues[6] = col.title;
                    rowValues[7] = card.createdDate;
                    rowValues[8] = card.createdDate;
                    rowValues[9] = card.dueDate;
                    rowValues[10] = card.dueDate;
                    rowValues[16] = assignees;
                    rowValues[17] = tags;
                    rowValues[18] = priority;
                    rowValues[19] = boardName;
                    rowValues[20] = "AwesomeTaskManager";
                    rowValues[23] = checklists;
                    rowValues[25] = "0";

                    sb.AppendLine("   <Row>");
                    foreach (var val in rowValues)
                    {
                        sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(val ?? "")}</Data></Cell>");
                    }
                    sb.AppendLine("   </Row>");
                }
            }

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Column exported to Excel successfully");
        }

        private void ExportColumn(TaskColumn col)
        {
            if (col == null) return;
            string defaultName = SanitizeFileName(col.title);
            string path = EditorUtility.SaveFilePanel("Export Column", "", defaultName, "atcl");
            if (string.IsNullOrEmpty(path)) return;

            var exportData = new ExportColumnData { column = col };
            HashSet<string> assigneeIds = new HashSet<string>();
            foreach (var card in col.cards)
                foreach (var id in card.assigneeIds)
                    assigneeIds.Add(id);

            exportData.assignees = _data.assignees.Where(a => assigneeIds.Contains(a.id)).ToList();

            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            TriggerSuccessNotification("Column exported successfully");
        }

        private void ImportColumnIntoBoard(TaskBoard board)
        {
            if (board == null) return;
            string path = EditorUtility.OpenFilePanel("Import Column", "", "atcl");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var importData = JsonUtility.FromJson<ExportColumnData>(json);
                if (importData == null || importData.column == null)
                {
                    TriggerErrorNotification("Invalid column file");
                    return;
                }

                var col = importData.column;
                
                // Ensure unique IDs for all cards in the column
                foreach (var card in col.cards)
                {
                    card.id = Guid.NewGuid().ToString();
                }

                // Handle assignees
                foreach (var a in importData.assignees)
                {
                    if (!_data.assignees.Any(existing => existing.id == a.id))
                    {
                        if (!_data.assignees.Any(existing => existing.name == a.name))
                        {
                            _data.assignees.Add(a);
                        }
                    }
                }

                board.columns.Add(col);
                ResetFilters();
                _data.Normalize();
                Save();
                RefreshAfterImport();
                TriggerSuccessNotification("Column imported successfully");
                GUIUtility.ExitGUI();
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Column Import failed: " + e.Message);
                TriggerErrorNotification("Column Import failed");
            }
        }

        private void ExportCardToCSV(TaskCard card, TaskColumn col)
        {
            if (card == null || col == null) return;
            string defaultName = SanitizeFileName(card.title);
            string path = EditorUtility.SaveFilePanel("Export Card to CSV", "", defaultName, "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            // ClickUp compatible header
            sb.AppendLine("Task ID,Task Link,Task Type,Task Custom ID,Task Name,Task Content,Status,Date created,Date created Text,Due date,Due date Text,Start date,Start date Text,Parent ID,Subtask IDs,Attachments,Assignees,Tags,Priority,List Name,Space Name,Time Estimated,Time Estimated Text,Checklists,Comments,Assigned Comments,Time Spent,Time Spent Text,Rolled Up Time,Rolled Up Time Text");

            string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
            string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
            string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
            
            List<string> checklistStrings = new List<string>();
            for (int i = 0; i < card.checklistItems.Count; i++)
            {
                string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
            }
            string checklists = string.Join("; ", checklistStrings);

            sb.Append($"\"{EscapeCSV(card.id)}\",");
            sb.Append($"\"\","); // Task Link
            sb.Append($"\"task\",");
            sb.Append($"\"\","); // Custom ID
            sb.Append($"\"{EscapeCSV(card.title)}\",");
            sb.Append($"\"{EscapeCSV(card.description)}\",");
            sb.Append($"\"{EscapeCSV(col.title)}\",");
            sb.Append($"\"{EscapeCSV(card.createdDate)}\",");
            sb.Append($"\"{EscapeCSV(card.createdDate)}\",");
            sb.Append($"\"{EscapeCSV(card.dueDate)}\",");
            sb.Append($"\"{EscapeCSV(card.dueDate)}\",");
            sb.Append($"\"\","); // Start date
            sb.Append($"\"\",");
            sb.Append($"\"\","); // Parent ID
            sb.Append($"\"\",");
            sb.Append($"\"[]\","); // Attachments
            sb.Append($"\"{EscapeCSV(assignees)}\",");
            sb.Append($"\"{EscapeCSV(tags)}\",");
            sb.Append($"\"{EscapeCSV(priority)}\",");
            sb.Append($"\"{EscapeCSV(Board.name)}\",");
            sb.Append($"\"AwesomeTaskManager\",");
            sb.Append($"\"\",");
            sb.Append($"\"\",");
            sb.Append($"\"{EscapeCSV(checklists)}\",");
            sb.Append($"\"\",");
            sb.Append($"\"0\",");
            sb.Append($"\"\",");
            sb.Append($"\"\",");
            sb.Append($"\"\",");
            sb.AppendLine($"\"\"");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Card exported to CSV successfully");
        }

        private void ExportCardToExcel(TaskCard card, TaskColumn col)
        {
            if (card == null || col == null) return;
            string defaultName = SanitizeFileName(card.title);
            string path = EditorUtility.SaveFilePanel("Export Card to Excel", "", defaultName, "xml");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            
            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"sHeader\">");
            sb.AppendLine("   <Font ss:Bold=\"1\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("  </Style>");
            sb.AppendLine(" </Styles>");

            sb.AppendLine($" <Worksheet ss:Name=\"{SecurityElement.Escape(card.title)}\">");
            sb.AppendLine("  <Table>");

            string[] headers = {
                "Task ID", "Task Link", "Task Type", "Task Custom ID", "Task Name", "Task Content",
                "Status", "Date created", "Date created Text", "Due date", "Due date Text",
                "Start date", "Start date Text", "Parent ID", "Subtask IDs", "Attachments",
                "Assignees", "Tags", "Priority", "List Name", "Space Name", "Time Estimated",
                "Time Estimated Text", "Checklists", "Comments", "Assigned Comments",
                "Time Spent", "Time Spent Text", "Rolled Up Time", "Rolled Up Time Text"
            };

            sb.AppendLine("   <Row ss:StyleID=\"sHeader\">");
            foreach (var h in headers)
            {
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(h)}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");

            string assignees = "[" + string.Join(",", _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).Select(a => a.name)) + "]";
            string tags = string.IsNullOrEmpty(card.category) ? "[]" : "[" + card.category + "]";
            string priority = card.priority == 0 ? "None" : card.priority == 1 ? "Low" : card.priority == 2 ? "Medium" : card.priority == 3 ? "High" : "Urgent";
            
            List<string> checklistStrings = new List<string>();
            for (int i = 0; i < card.checklistItems.Count; i++)
            {
                string itemStatus = (i < card.checklistStates.Count && card.checklistStates[i]) ? "RESOLVED" : "UNRESOLVED";
                checklistStrings.Add($"{card.checklistItems[i]} ({itemStatus})");
            }
            string checklists = string.Join("; ", checklistStrings);

            string[] rowValues = new string[30];
            rowValues[0] = card.id;
            rowValues[1] = "";
            rowValues[2] = "task";
            rowValues[3] = "";
            rowValues[4] = card.title;
            rowValues[5] = card.description;
            rowValues[6] = col.title;
            rowValues[7] = card.createdDate;
            rowValues[8] = card.createdDate;
            rowValues[9] = card.dueDate;
            rowValues[10] = card.dueDate;
            rowValues[11] = "";
            rowValues[12] = "";
            rowValues[13] = "";
            rowValues[14] = "";
            rowValues[15] = "[]";
            rowValues[16] = assignees;
            rowValues[17] = tags;
            rowValues[18] = priority;
            rowValues[19] = Board.name;
            rowValues[20] = "AwesomeTaskManager";
            rowValues[21] = "";
            rowValues[22] = "";
            rowValues[23] = checklists;
            rowValues[24] = "";
            rowValues[25] = "0";
            rowValues[26] = "";
            rowValues[27] = "";
            rowValues[28] = "";
            rowValues[29] = "";

            sb.AppendLine("   <Row>");
            foreach (var val in rowValues)
            {
                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{SecurityElement.Escape(val ?? "")}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            TriggerSuccessNotification("Card exported to Excel successfully");
        }

        private void ExportCard(TaskCard card)
        {
            if (card == null) return;
            string defaultName = SanitizeFileName(card.title);
            string path = EditorUtility.SaveFilePanel("Export Card", "", defaultName, "atc");
            if (string.IsNullOrEmpty(path)) return;

            var exportData = new ExportCardData { card = card };
            exportData.assignees = _data.assignees.Where(a => card.assigneeIds.Contains(a.id)).ToList();

            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            TriggerSuccessNotification("Card exported successfully");
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

        private void ImportColumnFromCSV(TaskBoard board)
        {
            if (board == null) return;
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Column from CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    TriggerErrorNotification("CSV file is empty or missing header");
                    return;
                }

                string[] headers = ParseCSVLine(lines[0]);
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Column, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Column Mapping (CSV)", ImportMappingScope.Column, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        var rows = new List<string[]>(lines.Length);
                        foreach (var line in lines)
                            rows.Add(ParseCSVLine(line));

                        ApplyImportMappingPreferences(result, ImportMappingScope.Column, path, headers);
                        ImportColumnRowsWithMapping(board, rows, path, result.mapping, "Column(s) imported from CSV successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[AwesomeTaskManager] CSV Column Import failed: " + e.Message);
                        TriggerErrorNotification("CSV Column Import failed. See console for details.");
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] CSV Column Import failed: " + e.Message);
                TriggerErrorNotification("CSV Column Import failed. See console for details.");
            }
        }

        private void ImportColumnFromExcel(TaskBoard board)
        {
            if (board == null) return;
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Column from Excel", "", "xlsx,xml");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                List<string[]> rows = new List<string[]>();
                if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXlsx(path);
                }
                else if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXmlSpreadsheet(path);
                }

                if (rows.Count < 2)
                {
                    TriggerErrorNotification("Excel file is empty or missing header");
                    return;
                }

                string[] headers = rows[0];
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Column, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Column Mapping (Excel)", ImportMappingScope.Column, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        ApplyImportMappingPreferences(result, ImportMappingScope.Column, path, headers);
                        ImportColumnRowsWithMapping(board, rows, path, result.mapping, "Column(s) imported from Excel successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[AwesomeTaskManager] Excel Column Import failed: " + e.Message);
                        TriggerErrorNotification("Excel Column Import failed. See console for details.");
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Excel Column Import failed: " + e.Message);
                TriggerErrorNotification("Excel Column Import failed. See console for details.");
            }
        }

        private void ImportBoardFromCSV()
        {
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Board from CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    TriggerErrorNotification("CSV file is empty or missing header");
                    return;
                }

                string[] headers = ParseCSVLine(lines[0]);
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Board, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Board Mapping (CSV)", ImportMappingScope.Board, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        var rows = new List<string[]>(lines.Length);
                        foreach (var line in lines)
                            rows.Add(ParseCSVLine(line));

                        ApplyImportMappingPreferences(result, ImportMappingScope.Board, path, headers);
                        ImportBoardRowsWithMapping(rows, path, result.mapping, "Board imported from CSV successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[AwesomeTaskManager] CSV Import failed: " + e.Message);
                        TriggerErrorNotification("CSV Import failed. See console for details.");
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] CSV Import failed: " + e.Message);
                TriggerErrorNotification("CSV Import failed. See console for details.");
            }
        }

        private void ImportBoardRowsWithMapping(List<string[]> rows, string path, ImportFieldMapping mapping, string successMessage)
        {
            if (rows == null || rows.Count < 2)
            {
                TriggerErrorNotification("Import data is empty.");
                return;
            }

            if (mapping == null || mapping.nameIndex < 0)
            {
                TriggerErrorNotification("Task name mapping is required.");
                return;
            }

            TaskBoard board = null;
            var columnMap = new Dictionary<string, TaskColumn>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i] ?? Array.Empty<string>();
                string taskName = GetMappedValue(values, mapping.nameIndex);

                if (string.IsNullOrWhiteSpace(taskName))
                {
                    // Preserve empty-column placeholder rows (status_placeholder).
                    // Create the column so round-tripping keeps all columns, even empty ones.
                    if (mapping.statusIndex >= 0)
                    {
                        string placeholderStatus = GetMappedValue(values, mapping.statusIndex);
                        if (!string.IsNullOrWhiteSpace(placeholderStatus))
                        {
                            if (board == null)
                            {
                                string boardName = GetMappedValue(values, mapping.listNameIndex);
                                if (string.IsNullOrWhiteSpace(boardName))
                                    boardName = Path.GetFileNameWithoutExtension(path);
                                board = new TaskBoard(boardName);
                                board.columns.Clear();
                            }

                            if (!columnMap.ContainsKey(placeholderStatus))
                            {
                                var emptyCol = new TaskColumn(placeholderStatus);
                                board.columns.Add(emptyCol);
                                columnMap[placeholderStatus] = emptyCol;
                            }
                        }
                    }
                    continue;
                }

                if (board == null)
                {
                    string boardName = GetMappedValue(values, mapping.listNameIndex);
                    if (string.IsNullOrWhiteSpace(boardName))
                        boardName = Path.GetFileNameWithoutExtension(path);

                    board = new TaskBoard(boardName);
                    board.columns.Clear();
                }

                string status = GetMappedValue(values, mapping.statusIndex);
                if (string.IsNullOrWhiteSpace(status)) status = "To Do";

                if (!columnMap.TryGetValue(status, out TaskColumn col))
                {
                    col = new TaskColumn(status);
                    board.columns.Add(col);
                    columnMap[status] = col;
                }

                TaskCard card = new TaskCard(taskName);
                MapValuesToCard(
                    card,
                    values,
                    status,
                    mapping.descriptionIndex,
                    mapping.priorityIndex,
                    mapping.assigneeIndex,
                    mapping.tagsIndex,
                    mapping.dueDateIndex,
                    mapping.checklistIndex,
                    mapping.customFieldsIndex,
                    mapping.preset == ImportMappingPreset.Trello);
                col.cards.Add(card);
            }

            if (board == null)
            {
                TriggerErrorNotification("No columns or tasks found to import.");
                return;
            }

            if (mapping.preset == ImportMappingPreset.ClickUp)
                SortColumnsLikeClickUp(board.columns);

            _data.boards.Add(board);
            _boardIndex = _data.boards.Count - 1;
            ResetFilters();
            _data.Normalize();
            Save();
            RefreshAfterImport();
            TriggerSuccessNotification(successMessage);
            GUIUtility.ExitGUI();
        }

        private void ImportColumnRowsWithMapping(TaskBoard board, List<string[]> rows, string path, ImportFieldMapping mapping, string successMessage)
        {
            if (board == null || rows == null || rows.Count < 2)
            {
                TriggerErrorNotification("Import data is empty.");
                return;
            }

            if (mapping == null || mapping.nameIndex < 0)
            {
                TriggerErrorNotification("Task name mapping is required.");
                return;
            }

            var columnMap = new Dictionary<string, TaskColumn>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i] ?? Array.Empty<string>();
                string taskName = GetMappedValue(values, mapping.nameIndex);

                if (string.IsNullOrWhiteSpace(taskName))
                {
                    // Preserve empty-column placeholder rows so round-tripping keeps all columns.
                    if (mapping.statusIndex >= 0)
                    {
                        string placeholderStatus = GetMappedValue(values, mapping.statusIndex);
                        if (!string.IsNullOrWhiteSpace(placeholderStatus) && !columnMap.ContainsKey(placeholderStatus))
                        {
                            var existingCol = board.columns.FirstOrDefault(c => string.Equals(c.title, placeholderStatus, StringComparison.OrdinalIgnoreCase));
                            if (existingCol == null)
                            {
                                existingCol = new TaskColumn(placeholderStatus);
                                board.columns.Add(existingCol);
                            }
                            columnMap[placeholderStatus] = existingCol;
                        }
                    }
                    continue;
                }

                string status = GetMappedValue(values, mapping.statusIndex);
                if (string.IsNullOrWhiteSpace(status)) status = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(status)) status = "Imported";

                if (!columnMap.TryGetValue(status, out TaskColumn col))
                {
                    col = board.columns.FirstOrDefault(c => string.Equals(c.title, status, StringComparison.OrdinalIgnoreCase));
                    if (col == null)
                    {
                        col = new TaskColumn(status);
                        board.columns.Add(col);
                    }
                    columnMap[status] = col;
                }

                TaskCard card = new TaskCard(taskName);
                MapValuesToCard(
                    card,
                    values,
                    status,
                    mapping.descriptionIndex,
                    mapping.priorityIndex,
                    mapping.assigneeIndex,
                    mapping.tagsIndex,
                    mapping.dueDateIndex,
                    mapping.checklistIndex,
                    mapping.customFieldsIndex,
                    mapping.preset == ImportMappingPreset.Trello);
                col.cards.Add(card);
            }

            ResetFilters();
            if (mapping.preset == ImportMappingPreset.ClickUp)
                SortColumnsLikeClickUp(board.columns);

            _data.Normalize();
            Save();
            RefreshAfterImport();
            TriggerSuccessNotification(successMessage);
            GUIUtility.ExitGUI();
        }

        private static string GetMappedValue(string[] values, int idx)
        {
            if (values == null || idx < 0 || idx >= values.Length) return string.Empty;
            return values[idx]?.Trim() ?? string.Empty;
        }

        private static void SortColumnsLikeClickUp(List<TaskColumn> columns)
        {
            if (columns == null) return;

            string[] order = { "To Do", "In Progress", "Review", "Done", "Complete", "Closed", "Archived" };
            columns.Sort((a, b) =>
            {
                int indexA = -1;
                for (int j = 0; j < order.Length; j++)
                    if (string.Equals(a.title, order[j], StringComparison.OrdinalIgnoreCase)) { indexA = j; break; }

                int indexB = -1;
                for (int j = 0; j < order.Length; j++)
                    if (string.Equals(b.title, order[j], StringComparison.OrdinalIgnoreCase)) { indexB = j; break; }

                if (indexA == -1) indexA = 999;
                if (indexB == -1) indexB = 999;
                if (indexA != indexB) return indexA.CompareTo(indexB);
                return string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase);
            });
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result.ToArray();
        }

        private void MapValuesToCard(TaskCard card, string[] values, string status, int descIdx, int priorityIdx, int assigneeIdx, int tagIdx, int dueIdx, int checklistIdx, int customFieldsIdx = -1, bool parseAsTrello = false)
        {
            if (status != null)
            {
                if (status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
                {
                    card.archived = true;
                    card.isArchived = true;
                }
                if (status.Equals("Done", StringComparison.OrdinalIgnoreCase) || status.Equals("Closed", StringComparison.OrdinalIgnoreCase) || status.Equals("Complete", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    card.completed = true;
                }
            }

            if (descIdx != -1 && descIdx < values.Length) card.description = values[descIdx];
            if (dueIdx != -1 && dueIdx < values.Length) card.dueDate = values[dueIdx];
            
            if (priorityIdx != -1 && priorityIdx < values.Length)
            {
                string p = values[priorityIdx].ToLower();
                if (p.Contains("urgent")) card.priority = 4;
                else if (p.Contains("high")) card.priority = 3;
                else if (p.Contains("medium") || p.Contains("normal")) card.priority = 2;
                else if (p.Contains("low")) card.priority = 1;
                else card.priority = 0;
            }

            if (tagIdx != -1 && tagIdx < values.Length)
            {
                string tags = values[tagIdx].Trim('[', ']');
                if (!string.IsNullOrEmpty(tags))
                {
                    string[] splitTags = tags.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    card.category = splitTags.Length > 0 ? splitTags[0].Trim() : string.Empty;
                    if (!string.IsNullOrEmpty(card.category) && !_data.categories.Contains(card.category))
                    {
                        _data.categories.Add(card.category);
                    }
                }
            }

            if (assigneeIdx != -1 && assigneeIdx < values.Length)
            {
                string assigneesStr = values[assigneeIdx].Trim('[', ']');
                if (!string.IsNullOrEmpty(assigneesStr))
                {
                    string[] names = assigneesStr.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var n in names)
                    {
                        string trimmedName = n.Trim();
                        if (string.IsNullOrEmpty(trimmedName)) continue;
                        var assignee = _data.assignees.FirstOrDefault(a => a.name == trimmedName);
                        if (assignee == null)
                        {
                            assignee = new Assignee { name = trimmedName };
                            _data.assignees.Add(assignee);
                        }
                        if (!card.assigneeIds.Contains(assignee.id))
                        {
                            card.assigneeIds.Add(assignee.id);
                        }
                    }
                }
            }

            if (checklistIdx != -1 && checklistIdx < values.Length)
            {
                string checklistsStr = values[checklistIdx];
                if (!string.IsNullOrEmpty(checklistsStr))
                {
                    string[] items = checklistsStr.Split(new[] { ";", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in items)
                    {
                        string itemName = item;
                        bool resolved = false;
                        if (item.EndsWith("(RESOLVED)"))
                        {
                            itemName = item.Substring(0, item.Length - 10).Trim();
                            resolved = true;
                        }
                        else if (item.EndsWith("(UNRESOLVED)"))
                        {
                            itemName = item.Substring(0, item.Length - 12).Trim();
                            resolved = false;
                        }
                        card.checklistItems.Add(itemName);
                        card.checklistStates.Add(resolved);
                    }
                }
            }

            if (customFieldsIdx != -1 && customFieldsIdx < values.Length)
            {
                string customFields = values[customFieldsIdx];
                if (!string.IsNullOrWhiteSpace(customFields))
                {
                    if (parseAsTrello && card.priority == 0)
                    {
                        string p = customFields.ToLowerInvariant();
                        if (p.Contains("urgent")) card.priority = 4;
                        else if (p.Contains("high")) card.priority = 3;
                        else if (p.Contains("medium") || p.Contains("normal")) card.priority = 2;
                        else if (p.Contains("low")) card.priority = 1;
                    }

                    string customText = customFields.Trim();
                    if (!string.IsNullOrWhiteSpace(customText))
                    {
                        string separator = string.IsNullOrWhiteSpace(card.description) ? "" : "\n\n";
                        card.description = (card.description ?? string.Empty) + separator + "Custom Fields: " + customText;
                    }
                }
            }
        }

        private void ImportBoard()
        {
            string path = EditorUtility.OpenFilePanel("Import Board", "", "atb");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var importData = JsonUtility.FromJson<ExportBoardData>(json);
                if (importData == null || importData.board == null)
                {
                    TriggerErrorNotification("Invalid board file");
                    return;
                }

                var board = importData.board;

                // Handle assignees
                foreach (var a in importData.assignees)
                {
                    if (!_data.assignees.Any(existing => existing.id == a.id))
                    {
                        if (!_data.assignees.Any(existing => existing.name == a.name))
                        {
                            _data.assignees.Add(a);
                        }
                    }
                }

                // Handle categories
                foreach (var cc in importData.categoryColors)
                {
                    if (!_data.categories.Contains(cc.category))
                    {
                        _data.categories.Add(cc.category);
                        _data.SetCategoryColor(cc.category, cc.colorIndex);
                    }
                }

                // Add board
                board.name += " (Imported)";
                _data.boards.Add(board);
                _boardIndex = _data.boards.Count - 1;
                ResetFilters();
                _data.Normalize();
                Save();
                RefreshAfterImport();
                TriggerSuccessNotification("Board imported successfully");
                GUIUtility.ExitGUI();
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Import failed: " + e.Message);
                TriggerErrorNotification("Import failed");
            }
        }

        private void ImportCardIntoColumn(TaskColumn col)
        {
            if (col == null) return;
            string path = EditorUtility.OpenFilePanel("Import Card", "", "atc");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var importData = JsonUtility.FromJson<ExportCardData>(json);
                if (importData == null || importData.card == null)
                {
                    TriggerErrorNotification("Invalid card file");
                    return;
                }

                var card = importData.card;
                card.id = Guid.NewGuid().ToString(); // Ensure unique ID for imported card
                card.createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                // Handle assignees
                foreach (var a in importData.assignees)
                {
                    if (!_data.assignees.Any(existing => existing.id == a.id))
                    {
                        if (!_data.assignees.Any(existing => existing.name == a.name))
                        {
                            _data.assignees.Add(a);
                        }
                    }
                }

                col.cards.Add(card);
                ResetFilters();
                _data.Normalize();
                Save();
                TriggerSuccessNotification("Card imported successfully");
                GUIUtility.ExitGUI();
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Import failed: " + e.Message);
                TriggerErrorNotification("Import failed");
            }
        }

        private void ImportCardRowsWithMapping(TaskColumn targetCol, List<string[]> rows, string path, ImportFieldMapping mapping, string successMessage)
        {
            if (targetCol == null || rows == null || rows.Count < 2)
            {
                TriggerErrorNotification("Import data is empty.");
                return;
            }

            if (mapping == null || mapping.nameIndex < 0)
            {
                TriggerErrorNotification("Task name mapping is required.");
                return;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i] ?? Array.Empty<string>();
                string taskName = GetMappedValue(values, mapping.nameIndex);
                if (string.IsNullOrWhiteSpace(taskName)) continue;

                TaskCard card = new TaskCard(taskName);
                MapValuesToCard(
                    card,
                    values,
                    targetCol.title,
                    mapping.descriptionIndex,
                    mapping.priorityIndex,
                    mapping.assigneeIndex,
                    mapping.tagsIndex,
                    mapping.dueDateIndex,
                    mapping.checklistIndex,
                    mapping.customFieldsIndex,
                    mapping.preset == ImportMappingPreset.Trello);

                targetCol.cards.Add(card);
            }

            ResetFilters();
            _data.Normalize();
            Save();
            TriggerSuccessNotification(successMessage);
            GUIUtility.ExitGUI();
        }

        private void ImportCardFromCSV(TaskColumn targetCol)
        {
            if (targetCol == null) return;
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Card from CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    TriggerErrorNotification("CSV file is empty or missing header");
                    return;
                }

                string[] headers = ParseCSVLine(lines[0]);
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Card, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Card Mapping (CSV)", ImportMappingScope.Card, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        var rows = new List<string[]>(lines.Length);
                        foreach (var line in lines)
                            rows.Add(ParseCSVLine(line));

                        ApplyImportMappingPreferences(result, ImportMappingScope.Card, path, headers);
                        ImportCardRowsWithMapping(targetCol, rows, path, result.mapping, "Card(s) imported from CSV successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[AwesomeTaskManager] CSV Card Import failed: " + e.Message);
                        TriggerErrorNotification("CSV Card Import failed.");
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] CSV Card Import failed: " + e.Message);
                TriggerErrorNotification("CSV Card Import failed.");
            }
        }

        private void ImportCardFromExcel(TaskColumn targetCol)
        {
            if (targetCol == null) return;
            if (_data == null)
            {
                _data = Persistence.Load();
                if (_data == null) return;
            }

            string path = EditorUtility.OpenFilePanel("Import Card from Excel", "", "xlsx,xml");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                List<string[]> rows = new List<string[]>();
                if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXlsx(path);
                }
                else if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    rows = ParseXmlSpreadsheet(path);
                }

                if (rows.Count < 2)
                {
                    TriggerErrorNotification("Excel file is empty or missing header");
                    return;
                }

                string[] headers = rows[0];
                string suggestedProfileId;
                var suggested = GetSuggestedImportMapping(path, headers, ImportMappingScope.Card, out suggestedProfileId);
                ImportFieldMappingWindow.Open("Import Card Mapping (Excel)", ImportMappingScope.Card, path, headers, suggested, _data?.importMappingProfiles, suggestedProfileId, result =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(result?.deleteProfileId))
                        {
                            if (DeleteImportProfile(result.deleteProfileId))
                                TriggerSuccessNotification("Import profile deleted successfully");
                            return;
                        }

                        ApplyImportMappingPreferences(result, ImportMappingScope.Card, path, headers);
                        ImportCardRowsWithMapping(targetCol, rows, path, result.mapping, "Card(s) imported from Excel successfully");
                    }
                    catch (ExitGUIException)
                    {
                        // Handled by the mapping window closing itself.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[AwesomeTaskManager] Excel Card Import failed: " + e.Message);
                        TriggerErrorNotification("Excel Card Import failed.");
                    }
                });
            }
            catch (ExitGUIException)
            {
                // Silent catch: when calling ExitGUI from a menu callback, 
                // Unity might log the exception as an error if we rethrow.
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Excel Card Import failed: " + e.Message);
                TriggerErrorNotification("Excel Card Import failed.");
            }
        }

        private void ImportSingleFile(string path)
        {
            try
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
                ResetFilters();
                Save();
                EditorUtility.DisplayDialog("Imported",
                    $"Imported \"{note.title}\" ({note.WordCount} words) from:\n{path}", "OK");
                GUIUtility.ExitGUI();
            }
            catch (ExitGUIException) { }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Note Import failed: " + e.Message);
                TriggerErrorNotification("Note Import failed");
            }
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
            try
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
                ResetFilters();
                Save();
                GUIUtility.ExitGUI();
            }
            catch (ExitGUIException) { }
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

