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
        private static TaskBoardWindow _instance;
        public static TaskBoardWindow Instance
        {
            get
            {
                if (_instance == null) _instance = null;
                return _instance;
            }
            private set => _instance = value;
        }

        private SaveData _data;
        [SerializeField] private int _tab;
        [SerializeField] private int _boardIndex;
        public int BoardIndex => _boardIndex;

        // GIF cache state
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;
        [SerializeField] private Vector2 _boardScroll, _notesListScroll, _noteEditorScroll, _styleScroll;
        [SerializeField] private string _searchFilter = "";
        [SerializeField] private string _categoryFilter = "";
        [SerializeField] private string _assigneeFilter = ""; // New filter
        [SerializeField] private int _priorityFilter = 0; // 0 = All
        [SerializeField] private string _styleSearchFilter = "";
        private bool _styleSearchForceShowCurrentSection;
        private bool _styleSectionHasVisibleAttribute;
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
            wantsMouseMove = true;
            // Always refresh from disk on enable; EditorWindow serialized state can be stale.
            _data = Persistence.Load();
            if (_data != null)
            {
                ApplyPostLoadVisualState();
            }
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable() 
        { 
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (_instance == this) _instance = null;
            Save(); 
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (_instance == this) _instance = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (_data != null)
            {
                ApplyPostLoadVisualState();
            }
        }

        public void LoadData()
        {
            var freshData = Persistence.Load();
            if (freshData == null) 
            {
                ThemedDialog.Show("Awesome Task Manager", "Failed to load data. The save file might be corrupted.", "OK");
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
            if (_data != null && _data.themes != null && _data.themes.Count > 0)
            {
                int themeIdx = Mathf.Clamp(_data.currentThemeIndex, 0, _data.themes.Count - 1);
                TBStyles.ApplyTheme(_data.themes[themeIdx]);
            }

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

            if (_tab < 0 || _tab > 2)
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
            _styleSearchFilter = "";
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

        public void SaveTheme()
        {
            if (_data == null) return;
            _data.themeSettings ??= new ThemeSaveData();
            _data.themeSettings.themes = _data.themes;
            _data.themeSettings.currentThemeIndex = _data.currentThemeIndex;
            if (_data.themes != null && _data.currentThemeIndex >= 0 && _data.currentThemeIndex < _data.themes.Count)
            {
                _data.themeSettings.selectedThemeName = _data.themes[_data.currentThemeIndex].name;
            }
            Persistence.SaveTheme(_data.themeSettings);
            ReloadAllOpenWindows();
        }

        public void AddCardFromDetail(string boardId, string columnId, TaskCard card)
        {
            int targetIdx = -1;
            if (_data != null)
            {
                targetIdx = _data.boards.FindIndex(b => b.id == boardId);
            }

            var freshData = Persistence.Load();
            if (freshData == null) return;
            _data = freshData;

            var board = _data.boards.FirstOrDefault(b => b.id == boardId);
            if (board != null)
            {
                var col = board.columns.FirstOrDefault(c => c.id == columnId);
                if (col != null)
                {
                    col.cards.Add(card);
                    targetIdx = _data.boards.IndexOf(board);
                }
            }

            if (targetIdx >= 0 && targetIdx < _data.boards.Count)
            {
                _boardIndex = targetIdx;
                _data.lastBoardIndex = targetIdx;
            }

            Save();
        }

        public void UpdateCardFromDetail(TaskCard updatedCard)
        {
            ReloadAllOpenWindows(); // Reload everything from disk and notify all windows
        }

        public void DeleteCardFromDetail(string boardId, string columnId, string cardId)
        {
            int targetIdx = -1;
            if (_data != null)
            {
                targetIdx = _data.boards.FindIndex(b => b.id == boardId);
            }

            var freshData = Persistence.Load();
            if (freshData == null) return;
            _data = freshData;

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
                    targetIdx = _data.boards.IndexOf(board);
                }
            }

            if (targetIdx >= 0 && targetIdx < _data.boards.Count)
            {
                _boardIndex = targetIdx;
                _data.lastBoardIndex = targetIdx;
            }

            Save();
            RefreshLinkCache();
            ReloadAllOpenWindows();
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

            if (_boardIndex >= 0 && _boardIndex < _data.boards.Count)
            {
                _data.lastBoardIndex = _boardIndex;
            }
            else
            {
                _boardIndex = Mathf.Clamp(_data.lastBoardIndex, 0, _data.boards.Count - 1);
            }
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
                if (ThemedDialog.Show("Open URL", $"Open this link in your browser?\n\n{url}", "Open", "Cancel"))
                {
                    Application.OpenURL(url);
                }
            };
        }

        private void OnGUI()
        {
            try
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

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.BoardBg, false);
                }

                DrawTabs();
                GUILayout.Space(1);

                if (_tab == 0) DrawBoardView();
                else if (_tab == 1) DrawNotesView();
                else DrawStyleView();

                DrawSuccessNotification();
                DrawErrorNotification();
            }
            finally
            {
                // Draw custom themed tooltip overlay at the end of OnGUI
                ThemedTooltip.Draw(this);
            }

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
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);
                    EditorGUI.DrawRect(new Rect(scope.rect.x, scope.rect.yMax - 1, scope.rect.width, 1), EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.12f));
                }

                GUILayout.Space(8);
                if (ThemedTooltip.Button($"{TBStyles.BoardTabIcon} Board", "Switch to Task Board view", _tab == 0 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    _tab = 0;
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(4);
                if (ThemedTooltip.Button($"{TBStyles.NotesTabIcon} Notes", "Switch to Notes workspace", _tab == 1 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    _tab = 1;
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(4);
                if (ThemedTooltip.Button($"{TBStyles.StyleTabIcon} Style", "Switch to Theme & Style customization view", _tab == 2 ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    _tab = 2;
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

            using (var tbScope = new EditorGUILayout.VerticalScope(GUILayout.Height(26)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawGlassPanel(tbScope.rect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.25f), false);
                    EditorGUI.DrawRect(new Rect(tbScope.rect.x, tbScope.rect.yMax - 1, tbScope.rect.width, 1), EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.30f) : new Color(0f, 0f, 0f, 0.10f));
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);

                    string[] names = _data.boards.Select(b => b.name).ToArray();
                    TBStyles.DrawThemedDropdown(_boardIndex, names, (newIdx) =>
                    {
                        if (newIdx != _boardIndex)
                        {
                            _boardIndex = newIdx;
                            _data.lastBoardIndex = _boardIndex;
                            Persistence.Save(_data);
                            ResetFilters();
                            GUIUtility.ExitGUI();
                        }
                    }, TBStyles.ToolbarPopup, "Select current board", GUILayout.Width(mediumWidth ? 150 : 120), GUILayout.Height(20));

                    GUILayout.Space(2);
                    if (ThemedContextMenu.DropdownButton("+", "Board Options", TBStyles.ToolbarButton, out Rect btnRect, GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        var menu = new ThemedContextMenu();
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
                                    if (ThemedDialog.Show("Delete Template", $"Delete template \"{t.name}\"?", "Delete", "Cancel"))
                                    {
                                        _data.templates.Remove(t);
                                        Save();
                                    }
                                });
                            }
                        }
                        menu.Show(btnRect);
                    }
                    if (_data.boards.Count > 1)
                    {
                        GUILayout.Space(2);
                        if (ThemedTooltip.Button(TBStyles.DeleteIcon, "Delete Board", TBStyles.ToolbarDeleteButton, GUILayout.Width(22), GUILayout.Height(20)))
                        {
                            var targetBoard = _data.boards[_boardIndex];
                            string boardName = targetBoard.name;
                            EditorApplication.delayCall += () =>
                            {
                                if (ThemedDialog.Show("Delete Board", $"Delete \"{boardName}\"?", "Delete", "Cancel"))
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
                    }
                
                    GUILayout.Space(showLabels ? 8 : 4);

                    if (showLabels) ThemedTooltip.Label("Category:", "Filter tasks by category", null, GUILayout.Width(58), GUILayout.Height(20));

                    var catFilterOptions = new List<string> { "All" };
                    catFilterOptions.AddRange(_data.categories);
                    int catIdx = 0;
                    if (!string.IsNullOrEmpty(_categoryFilter))
                    {
                        int f = catFilterOptions.IndexOf(_categoryFilter);
                        if (f >= 0) catIdx = f;
                    }
                    TBStyles.DrawThemedDropdown(catIdx, catFilterOptions.ToArray(), (newCatIdx) =>
                    {
                        _categoryFilter = newCatIdx == 0 ? "" : catFilterOptions[newCatIdx];
                        Repaint();
                    }, TBStyles.ToolbarPopup, "Filter tasks by category", GUILayout.Width(90), GUILayout.Height(20));
                    
                    GUILayout.Space(2);
                    if (ThemedTooltip.Button(TBStyles.CategoryIcon, "Category Editor", TBStyles.ToolbarButton, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        CategoryEditorWindow.Open(_data, () => { LoadData(); });
                    }

                    GUILayout.Space(showLabels ? 8 : 4);

                    if (showLabels) ThemedTooltip.Label("Assignee:", "Filter tasks by assignee", null, GUILayout.Width(56), GUILayout.Height(20));

                    var assigneeOptions = new List<string> { "All" };
                    assigneeOptions.AddRange(_data.assignees.Select(a => a.name));
                    int assIdx = 0;
                    if (!string.IsNullOrEmpty(_assigneeFilter))
                    {
                        var found = _data.assignees.FirstOrDefault(a => a.id == _assigneeFilter);
                        if (found != null) assIdx = assigneeOptions.IndexOf(found.name);
                        if (assIdx < 0) assIdx = 0;
                    }
                  
                    TBStyles.DrawThemedDropdown(assIdx, assigneeOptions.ToArray(), (newAssIdx) =>
                    {
                        if (newAssIdx == 0) _assigneeFilter = "";
                        else
                        {
                            var selectedName = assigneeOptions[newAssIdx];
                            var ass = _data.assignees.FirstOrDefault(a => a.name == selectedName);
                            if (ass != null) _assigneeFilter = ass.id;
                        }
                        Repaint();
                    }, TBStyles.ToolbarPopup, "Filter tasks by assignee", GUILayout.Width(90), GUILayout.Height(20));

                    GUILayout.Space(2);
                    if (ThemedTooltip.Button(TBStyles.AssigneeIcon, "Assignee Manager", TBStyles.ToolbarButton, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        AssigneeManagerWindow.ShowWindow(_data, () => { LoadData(); });
                    }

                    GUILayout.Space(showLabels ? 8 : 4);

                    if (showLabels) ThemedTooltip.Label("Priority:", "Filter tasks by priority", null, GUILayout.Width(52), GUILayout.Height(20));
                    else ThemedTooltip.Label(TBStyles.PriorityFilterIcon, "Filter tasks by priority", null, GUILayout.Width(18), GUILayout.Height(20));

                    var priorityOptions = new List<string> { "All" };
                    priorityOptions.AddRange(TBStyles.GetPriorityDisplayNames());
                    TBStyles.DrawThemedDropdown(_priorityFilter, priorityOptions.ToArray(), (newPri) =>
                    {
                        _priorityFilter = newPri;
                        Repaint();
                    }, TBStyles.ToolbarPopup, "Filter tasks by priority", GUILayout.Width(showLabels ? 80 : 60), GUILayout.Height(20));

                    GUILayout.Space(showLabels ? 8 : 4);

                    ThemedTooltip.Label("🔍", "Search Tasks", null, GUILayout.Width(18), GUILayout.Height(20));
                    _searchFilter = TBStyles.DrawThemedTextField(_searchFilter, TBStyles.ThemedSearchField, GUILayout.Width(mediumWidth ? 140 : 85), GUILayout.Height(20));

                    GUILayout.Space(showLabels ? 8 : 4);
                    
                    if (ThemedTooltip.Button(showLabels ? "▾ Show All" : "▾", "Show All Checklists", TBStyles.ToolbarButton, GUILayout.Width(showLabels ? 70 : 25), GUILayout.Height(20)))
                    {
                        var board2 = _data.boards[_boardIndex];
                        foreach (var c in board2.columns)
                            foreach (var card in c.cards)
                                card.showChecklist = true;
                        Save();
                    }
                    GUILayout.Space(2);
                    if (ThemedTooltip.Button(showLabels ? "▸ Hide All" : "▸", "Hide All Checklists", TBStyles.ToolbarButton, GUILayout.Width(showLabels ? 68 : 25), GUILayout.Height(20)))
                    {
                        var board2 = _data.boards[_boardIndex];
                        foreach (var c in board2.columns)
                            foreach (var card in c.cards)
                                card.showChecklist = false;
                        Save();
                    }
                    GUILayout.Space(2);
                    // Show/Hide Archived toggle
                    bool newShowArchived = ThemedTooltip.Toggle(_showArchived, _showArchived ? TBStyles.UnarchiveIcon : TBStyles.ArchiveIcon, _showArchived ? "Hide Archived Cards" : "Show Archived Cards", TBStyles.ToolbarButton, GUILayout.Width(28), GUILayout.Height(20));
                    if (newShowArchived != _showArchived)
                    {
                        _showArchived = newShowArchived;
                        Repaint();
                    }
                    
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(2);
            }

            GUILayout.Space(2);

            // Board title row
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_renamingBoard)
                {
                    _renameBoardName = TBStyles.DrawThemedTextField(_renameBoardName, TBStyles.ThemedTextField, GUILayout.Width(250), GUILayout.Height(26));
                    if (ThemedTooltip.IconButton("✔", "Save Board Name", GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        if (!string.IsNullOrWhiteSpace(_renameBoardName)) board.name = _renameBoardName.Trim();
                        _renamingBoard = false; Save();
                    }
                    if (ThemedTooltip.IconButton("✕","Cancel Renaming", GUILayout.Width(26), GUILayout.Height(24))) _renamingBoard = false;
                }
                else
                {
                    string headerText = $"{TBStyles.BoardHeaderIcon} {TBStyles.TruncateString(board.name, 50)}";
                    Vector2 headerSize = TBStyles.BoardHeader.CalcSize(new GUIContent(headerText));
                    EditorGUILayout.LabelField(headerText, TBStyles.BoardHeader, GUILayout.Width(headerSize.x + 4), GUILayout.Height(30));
                    
                    if (ThemedTooltip.IconButton("✏", "Rename Board", GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        _renamingBoard = true;
                        _renameBoardName = board.name;
                    }
                }
                GUILayout.FlexibleSpace();

                if (_showAddColumn)
                {
                    _newColumnTitle = TBStyles.DrawThemedTextField(_newColumnTitle, TBStyles.ThemedTextField, GUILayout.Width(140), GUILayout.Height(22));
                    if (ThemedTooltip.Button("Add", "Add a new Column", TBStyles.StandardButton, GUILayout.Width(42), GUILayout.Height(22)) && !string.IsNullOrWhiteSpace(_newColumnTitle))
                    {
                        board.columns.Add(new TaskColumn(_newColumnTitle.Trim()));
                        _newColumnTitle = ""; _showAddColumn = false; Save();
                        GUIUtility.ExitGUI();
                    }
                    if (ThemedTooltip.IconButton("✕", "Cancel Column Creation", GUILayout.Width(22), GUILayout.Height(22))) _showAddColumn = false;
                }
                else
                {
                    if (ThemedTooltip.Button("+ Column", "Add a new Column", TBStyles.StandardButton, GUILayout.Width(80), GUILayout.Height(24)))
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
            if (Event.current.type == EventType.Repaint)
            {
                TBStyles.DrawGlassPanel(statusRect, TBStyles.StatusBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.30f), false);
            }

            string statusText = $"  {totalCards} card(s)";
            if (completedCount > 0) statusText += $"  •  {TBStyles.CompletedIcon} <color=#{ColorUtility.ToHtmlStringRGBA(TBStyles.StatusCompletedColor)}>{completedCount} Completed</color>";
            if (overdueCount > 0) statusText += $"  •  {TBStyles.OverdueIcon} <color=#{ColorUtility.ToHtmlStringRGBA(TBStyles.StatusOverdueColor)}>{overdueCount} overdue</color>";
            if (dueTodayCount > 0) statusText += $"  •  {TBStyles.DueTodayIcon} <color=#{ColorUtility.ToHtmlStringRGBA(TBStyles.StatusDueTodayColor)}>{dueTodayCount} due today</color>";
            statusText += $"  •  {board.columns.Count} column(s)";
            EditorGUI.LabelField(statusRect, statusText, TBStyles.StatusBar);
        }

        private void DrawColumn(TaskColumn col, int colIdx, TaskBoard board, float width)
        {
            Color bg = colIdx % 2 == 0 ? TBStyles.ColumnBg : TBStyles.ColumnBgAlt;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                using (var scope = new EditorGUILayout.VerticalScope(TBStyles.ColumnBox))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        // Custom themed column background with glass panel highlights
                        TBStyles.DrawGlassPanel(scope.rect, bg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);
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
                            if (colIdx > 0 && ThemedTooltip.IconButton("◀", "Move Column Left", GUILayout.Width(22), GUILayout.Height(20)))
                            {
                                board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx - 1, col);
                                Save(); GUIUtility.ExitGUI();
                            }
                            if (colIdx < board.columns.Count - 1 && ThemedTooltip.IconButton("▶", "Move Column Right", GUILayout.Width(22), GUILayout.Height(20)))
                            {
                                board.columns.RemoveAt(colIdx); board.columns.Insert(colIdx + 1, col);
                                Save(); GUIUtility.ExitGUI();
                            }
                        }
                        if (ThemedContextMenu.DropdownButton("⋮", "Show Column Options", TBStyles.IconButton, out Rect btnRect, GUILayout.Width(22), GUILayout.Height(20)))
                        {
                            var menu = new ThemedContextMenu();
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
                                if (ThemedDialog.Show("Clear Column", $"Remove all cards from \"{col.title}\"?", "Clear", "Cancel"))
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
                                if (ThemedDialog.Show("Delete Column", $"Delete \"{col.title}\" and all its cards?", "Delete", "Cancel"))
                                {
                                    foreach(var card in col.cards) _data.CleanupReferencesToCard(card.id);
                                    board.columns.RemoveAt(ci); 
                                    Save(); 
                                    RefreshLinkCache();
                                    Repaint(); 
                                }
                            });
                            menu.Show(btnRect);
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

                    if (GUILayout.Button("+ Add Card", TBStyles.AddCardButton, GUILayout.Height(26)))
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
            var labelColor = TBStyles.GetLabelColor(card.colorLabel);
            
            bool isLinkHighlighted = _linkHighlightCardId == card.id;
            bool isChildOfHighlighted = _linkHighlightMode == LinkHighlightMode.Children && !string.IsNullOrEmpty(_linkHighlightCardId) && _parentToChildren.TryGetValue(_linkHighlightCardId, out var children) && children.Contains(card.id);
            bool isParentOfHighlighted = _linkHighlightMode == LinkHighlightMode.Parents && !string.IsNullOrEmpty(_linkHighlightCardId) && _childToParents.TryGetValue(_linkHighlightCardId, out var parents) && parents.Contains(card.id);

            bool shouldHighlight = isLinkHighlighted || isChildOfHighlighted || isParentOfHighlighted;

            Rect cardRect;
            using (var cardScope = new EditorGUILayout.VerticalScope(shouldHighlight ? TBStyles.CardBoxHighlighted : TBStyles.CardBox))
            {
                cardRect = cardScope.rect;
                if (Event.current.type == EventType.Repaint)
                {
                    Color cardBg = shouldHighlight
                        ? (EditorGUIUtility.isProSkin ? TBStyles.Pro_CardHighlighted : TBStyles.Personal_CardHighlighted)
                        : TBStyles.CardBg;
                    Color? border = shouldHighlight ? (Color?)null : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.45f));
                    TBStyles.DrawGlassPanel(cardRect, cardBg, border, true);
                }

                if (card.colorLabel > 0)
                {
                    var stripRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(stripRect, labelColor);
                    GUILayout.Space(2);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Completed toggle
                    string compIcon = card.completed ? TBStyles.CompletedIcon : "⬜";
                    string compToolTip = card.completed ? "Untick Card" : "Tick to Complete Card";
                    if (ThemedTooltip.IconButton(compIcon, compToolTip, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        card.completed = !card.completed;
                        _data.SyncLinkedChecklistItems(card.id, card.completed);
                        Save();
                    }
                    if (card.priority > 0)
                        EditorGUILayout.LabelField(TBStyles.GetPriorityIcon(card.priority), GUILayout.Width(18));

                    var titleStyle = new GUIStyle(TBStyles.CardTitle);
                    if (card.completed)
                    {
                        titleStyle.fontStyle = FontStyle.Italic;
                        titleStyle.normal = new GUIStyleState { textColor = TBStyles.StatusCompletedColor };
                    }
                    EditorGUILayout.LabelField(card.title, titleStyle);
                    Rect dragHandleRect = GUILayoutUtility.GetRect(new GUIContent("↕"), TBStyles.IconButton, GUILayout.Width(26), GUILayout.Height(24));
                    GUI.Box(dragHandleRect, new GUIContent("↕"), TBStyles.IconButton);
                    ThemedTooltip.SetTooltip(dragHandleRect, "Drag to reorder");
                    HandleCardDragHandle(card, col, dragHandleRect);
                    if (ThemedTooltip.IconButton("✏", "Show card details", GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        string boardId = Board.id;
                        string columnId = col.id;
                        CardDetailWindow.Show(card, _data, boardId, columnId, () => { LoadData(); }, () =>
                        {
                            DeleteCardFromDetail(boardId, columnId, card.id);
                        });
                    }
                    
                // Card Options (⋮)
                if (ThemedContextMenu.DropdownButton("⋮", "Card Options", TBStyles.IconButton, out Rect btnRect, GUILayout.Width(22), GUILayout.Height(24)))
                {
                    var menu = new ThemedContextMenu();
                    menu.AddItem(new GUIContent(card.archived ? $"{TBStyles.UnarchiveIcon} Unarchive Card" : $"{TBStyles.ArchiveIcon} Archive Card"), false, () =>
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
                                ThemedDialog.Show("Error", "Target board has no columns.", "OK");
                            }
                        });
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete Card"), false, () =>
                    {
                        if (ThemedDialog.Show("Delete Card", $"Delete \"{card.title}\"?", "Delete", "Cancel"))
                        {
                            if (_linkHighlightCardId == card.id)
                            {
                                _linkHighlightMode = LinkHighlightMode.None;
                                _linkHighlightCardId = null;
                            }
                            _data.CleanupReferencesToCard(card.id);
                            col.cards.Remove(card);
                            Save();
                            RefreshLinkCache();
                            Repaint();
                        }
                    });
                    menu.Show(btnRect);
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
                            normal = { textColor = TBStyles.CardCategoryTagColor }
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
                        GUILayout.Label($"{TBStyles.ArchiveIcon} ARCHIVED", archiveStyle);
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            if (!string.IsNullOrWhiteSpace(card.description))
            {
                string preview = TBStyles.TruncateString(card.description, 80);
                var descStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    normal = { textColor = TBStyles.CardDetailsTextColor }
                };
                ThemedTooltip.Label(preview, card.description, descStyle);
            }

            if (card.checklistItems.Count > 0)
            {
                int done = card.checklistStates.Count(s => s);
                bool allDone = done == card.checklistItems.Count;
                var summaryStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = allDone ? TBStyles.StatusCompletedColor : TBStyles.TasksCompletedCountColor }
                };

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(allDone ? $"{TBStyles.CompletedIcon} {done}/{card.checklistItems.Count} complete" : $"{TBStyles.ChecklistIcon} {done}/{card.checklistItems.Count}", summaryStyle);
                    // Hide Checklist button
                    if (card.checklistItems.Count > 0)
                    {
                        string toggleLabel = card.showChecklist ? "▾" : "▸";
                        string toggleToolTip = card.showChecklist ? "Hide Checklist" : "Show Checklist";
                        if (ThemedTooltip.IconButton(toggleLabel, toggleToolTip, GUILayout.Width(22), GUILayout.Height(24)))
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
                            bool nowDone = TBStyles.DrawThemedCheckbox(wasDone, wasDone ? "Mark item incomplete" : "Mark item complete", GUILayout.Width(16), GUILayout.Height(18));
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
                            var itemStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                normal = { textColor = TBStyles.CardTasksTextColor }
                            };
                            if (wasDone) itemStyle.fontStyle = FontStyle.Italic;
                            string itemText = card.checklistItems[ci];
                            string displayItemText = TBStyles.TruncateString(itemText, 40);
                            ThemedTooltip.Label(displayItemText, itemText, itemStyle);
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
                                if (ThemedTooltip.Button(sceneIcon, $"[{sceneName}] {sref.name}", GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
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
                            if (ThemedTooltip.Button(noteIcon, $"[Note] {noteTitle}", GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
                            {
                                if (note != null) NotePopupWindow.OpenInPreviewMode(note, _data, () => { LoadData(); });
                            }
                            shown++;
                        }
                        else if (item.isUrl)
                        {
                            var urlIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
                            string label = string.IsNullOrEmpty(item.displayName) ? item.url : item.displayName;
                            if (ThemedTooltip.Button(urlIcon, $"[Link] {label}", GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
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
                                if (ThemedTooltip.Button(icon, obj.name, GUIStyle.none, GUILayout.Width(20), GUILayout.Height(20)))
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
                        var linkStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = TBStyles.CardDetailsTextColor } };
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
                        string dueDateText = $"{TBStyles.DueDateIcon} {card.dueDate}";

                        if (card.completed)
                        {
                            dueDateText = $"{TBStyles.CompletedIcon} Completed";
                            dueDateStyle.normal = new GUIStyleState { textColor = TBStyles.StatusCompletedColor };
                            dueDateStyle.fontStyle = FontStyle.Bold;
                        }
                        else if (DateTime.TryParse(card.dueDate, out DateTime parsedDue))
                        {
                            var today = DateTime.Today;
                            int daysUntil = (parsedDue.Date - today).Days;

                            if (daysUntil < 0)
                            {
                                dueDateText = $"{TBStyles.OverdueIcon} Overdue ({-daysUntil}d ago)";
                                dueDateStyle.normal = new GUIStyleState { textColor = TBStyles.StatusOverdueColor };
                                dueDateStyle.fontStyle = FontStyle.Bold;
                            }
                            else if (daysUntil == 0)
                            {
                                dueDateText = $"{TBStyles.DueTodayIcon} Due today!";
                                dueDateStyle.normal = new GUIStyleState { textColor = TBStyles.StatusDueTodayColor };
                                dueDateStyle.fontStyle = FontStyle.Bold;
                            }
                            else if (daysUntil <= 3)
                            {
                                dueDateText = $"{TBStyles.DueSoonIcon} Due in {daysUntil}d ({parsedDue:MMM dd})";
                                dueDateStyle.normal = new GUIStyleState { textColor = TBStyles.StatusDueSoonColor };
                            }
                            else
                            {
                                dueDateText = $"{TBStyles.DueDateIcon} {parsedDue:MMM dd, yyyy}";
                                dueDateStyle.normal = new GUIStyleState { textColor = TBStyles.CardDetailsTextColor };
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
                                        var assigneeNameStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = TBStyles.CardDetailsTextColor } };
                                        GUILayout.Label(assignee.name, assigneeNameStyle);
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
                                    var extraStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = TBStyles.CardDetailsTextColor } };
                                    EditorGUILayout.LabelField($"+{card.assigneeIds.Count - 4}", extraStyle, GUILayout.Width(18));
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
                if (idx > 0 && ThemedTooltip.IconButton(TBStyles.MoveUpIcon, "Move card up the column", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    col.cards.RemoveAt(idx); col.cards.Insert(idx - 1, card);
                    Save(); GUIUtility.ExitGUI();
                }
                if (idx < col.cards.Count - 1 && ThemedTooltip.IconButton(TBStyles.MoveDownIcon, "Move card down the column", GUILayout.Width(22), GUILayout.Height(20)))
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
                    
                    if (ThemedTooltip.IconButton(TBStyles.ParentLinkIcon, tooltip, GUILayout.Width(22), GUILayout.Height(24)))
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

                    if (ThemedTooltip.IconButton(TBStyles.ChildLinkIcon, tooltip, GUILayout.Width(22), GUILayout.Height(24)))
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
                var stripColor = TBStyles.GetLabelColor(_dragCard.colorLabel);
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
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                // ──────── LEFT PANEL: Folders + Note List ────────
                using (var leftScope = new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.Width(260), GUILayout.ExpandHeight(true)))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        TBStyles.DrawGlassPanel(leftScope.rect, TBStyles.NoteSidebarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);
                    }

                    GUILayout.Space(6);
                    // Header + search
                    EditorGUILayout.LabelField($"{TBStyles.NotesHeaderIcon} Quick Notes", TBStyles.SectionLabel);
                    GUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("🔍", GUILayout.Width(16));
                        _noteSearchFilter = TBStyles.DrawThemedTextField(_noteSearchFilter, TBStyles.ThemedSearchField, GUILayout.Height(20));
                    }
                    GUILayout.Space(4);

                    // Add note + Import
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _newNoteTitle = TBStyles.DrawThemedTextField(_newNoteTitle, TBStyles.ThemedTextField, GUILayout.Height(22));
                        if (ThemedTooltip.Button("+ Note", "Add a new Note", TBStyles.AddNoteButton, GUILayout.Width(54), GUILayout.Height(22)))
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
                        if (ThemedTooltip.Button("📥", "Import Note from File", TBStyles.ImportNoteButton, GUILayout.Width(28), GUILayout.Height(22)))
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
                        var folderHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = TBStyles.NoteFolderTextColor } };
                        EditorGUILayout.LabelField("📁 Folders", folderHeaderStyle);
                        if (_showAddFolder)
                        {
                            _newFolderName = TBStyles.DrawThemedTextField(_newFolderName, TBStyles.ThemedTextField, GUILayout.Width(80), GUILayout.Height(20));
                            if (ThemedTooltip.IconButton("✔", "Save Folder Name", GUILayout.Width(22), GUILayout.Height(20)) && !string.IsNullOrWhiteSpace(_newFolderName))
                            {
                                _data.noteFolders.Add(new NoteFolder(_newFolderName.Trim()));
                                _newFolderName = ""; _showAddFolder = false; Save();
                            }
                            if (ThemedTooltip.IconButton("✕", "Cancel Adding Folder", GUILayout.Width(22), GUILayout.Height(20))) _showAddFolder = false;
                        }
                        else
                        {
                            if (ThemedTooltip.IconButton("+", "Add Folder", GUILayout.Width(22), GUILayout.Height(20))) _showAddFolder = true;
                        }
                    }

                    // "All Notes"
                    bool allSelected = string.IsNullOrEmpty(_selectedFolderId);
                    int totalNotes = _data.notes.Count;
                    var allNotesStyle = new GUIStyle(allSelected ? EditorStyles.boldLabel : EditorStyles.label)
                    {
                        normal = { textColor = allSelected ? TBStyles.NoteSelectedAccent : TBStyles.NoteFolderTextColor },
                        alignment = TextAnchor.MiddleLeft
                    };
                    if (GUILayout.Button(allSelected ? $"▸ All Notes ({totalNotes})" : $"   All Notes ({totalNotes})", allNotesStyle, GUILayout.Height(20)))
                    {
                        _selectedFolderId = "";
                        _selectedNote = -1;
                    }

                    // "Unfiled" — also a drop target
                    int unfiledCount = _data.notes.Count(n => string.IsNullOrEmpty(n.folderId));
                    bool unfiledSel = _selectedFolderId == "__unfiled__";
                    var unfiledStyle = new GUIStyle(unfiledSel ? EditorStyles.boldLabel : EditorStyles.label)
                    {
                        normal = { textColor = unfiledSel ? TBStyles.NoteSelectedAccent : TBStyles.NoteFolderTextColor },
                        alignment = TextAnchor.MiddleLeft
                    };
                    if (GUILayout.Button(unfiledSel ? $"▸ Unfiled ({unfiledCount})" : $"   Unfiled ({unfiledCount})", unfiledStyle, GUILayout.Height(20)))
                    {
                        _selectedFolderId = "__unfiled__";
                        _selectedNote = -1;
                    }
                    var unfiledRect = GUILayoutUtility.GetLastRect();
                    _folderDropRects["__unfiled__"] = unfiledRect;

                    // Highlight unfiled during drag
                    if (_noteDragging && Event.current.type == EventType.Repaint)
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

                    // Each folder
                    for (int fi = 0; fi < _data.noteFolders.Count; fi++)
                    {
                        var folder = _data.noteFolders[fi];
                        int count = _data.notes.Count(n => n.folderId == folder.id);
                        bool fsel = _selectedFolderId == folder.id;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            string fLabel = fsel ? $"▸ 📁 {folder.name} ({count})" : $"   📁 {folder.name} ({count})";
                            var folderStyle = new GUIStyle(fsel ? EditorStyles.boldLabel : EditorStyles.label)
                            {
                                normal = { textColor = fsel ? TBStyles.NoteSelectedAccent : TBStyles.NoteFolderTextColor },
                                alignment = TextAnchor.MiddleLeft
                            };

                            if (GUILayout.Button(new GUIContent(fLabel), folderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
                            {
                                _selectedFolderId = folder.id;
                                _selectedNote = -1;
                            }
                            var folderBtnRect = GUILayoutUtility.GetLastRect();
                            _folderDropRects[folder.id] = folderBtnRect;

                            if (_noteDragging && Event.current.type == EventType.Repaint)
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

                            if (ThemedContextMenu.DropdownButton("⋮", "Folder Options", TBStyles.IconButton, out Rect btnRect, GUILayout.Width(16), GUILayout.Height(20)))
                            {
                                var menu = new ThemedContextMenu();
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
                                    if (ThemedDialog.Show("Delete Folder",
                                        $"Delete folder \"{folder.name}\"?\nNotes inside will become unfiled.", "Delete", "Cancel"))
                                    {
                                        foreach (var n in _data.notes.Where(n => n.folderId == folder.id))
                                            n.folderId = "";
                                        _data.noteFolders.RemoveAt(capturedFi);
                                        if (_selectedFolderId == folder.id) _selectedFolderId = "";
                                        Save(); Repaint();
                                    }
                                });
                                menu.Show(btnRect);
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
                GUILayout.Space(4);
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
            var noteColor = TBStyles.GetLabelColor(note.colorIndex);

            // Use selected style or normal style
            var boxStyle = selected ? TBStyles.NoteBoxSelected : TBStyles.NoteBox;
            Rect itemRect;
            using (var scope = new EditorGUILayout.HorizontalScope(boxStyle))
            {
                itemRect = scope.rect;
                using (new EditorGUILayout.VerticalScope())
                {
                    string label = (note.pinned ? $"{TBStyles.PinnedNoteIcon} " : "") + note.title;
                    var titleStyle = selected
                        ? new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = TBStyles.NoteTitleColor }, fontSize = 12, wordWrap = true }
                        : new GUIStyle(EditorStyles.label) { normal = { textColor = TBStyles.NoteTitleColor }, fontSize = 12, wordWrap = true };
                    EditorGUILayout.LabelField(label, titleStyle);

                    var subColor = new Color(TBStyles.CardTextColor.r, TBStyles.CardTextColor.g, TBStyles.CardTextColor.b, 0.75f);
                    var infoStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = selected ? TBStyles.CardTextColor : subColor }
                    };
                    EditorGUILayout.LabelField($"{note.modifiedDate}  •  {note.WordCount} words", infoStyle);
                }

                GUILayout.FlexibleSpace();
                if (ThemedTooltip.IconButton("↗", "Popout Note", GUILayout.Width(24), GUILayout.Height(22)))
                {
                    NotePopupWindow.Open(note, _data, () => { LoadData(); });
                }
            }

            // ── Drawn over the box during Repaint to save horizontal space ──
            if (Event.current.type == EventType.Repaint)
            {
                var lastRect = itemRect;
                Color border = selected
                    ? TBStyles.NoteSelectedAccent
                    : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(1f, 1f, 1f, 0.40f));

                // 1px top specular highlight
                EditorGUI.DrawRect(new Rect(lastRect.x + 1, lastRect.y + 1, lastRect.width - 2, 1),
                    EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.45f));

                // Border
                TBStyles.DrawBorderRect(lastRect, border);

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
            using (var rightScope = new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawGlassPanel(rightScope.rect, TBStyles.NoteEditorBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);
                }

                GUILayout.Space(6);

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
                GUILayout.Space(6);
                string newTitle = TBStyles.DrawThemedTextField(note.title, TBStyles.NoteTitle, GUILayout.Height(24));
                if (newTitle != note.title) { note.title = newTitle; MarkNoteModified(note); }

                // Pin
                if (ThemedTooltip.Button(note.pinned ? TBStyles.PinnedNoteIcon : "Pin", note.pinned ? "Unpin Note" : "Pin Note", TBStyles.StandardButton, GUILayout.Width(36), GUILayout.Height(24)))
                {
                    note.pinned = !note.pinned; Save(); Repaint();
                }

                // Color
                TBStyles.DrawThemedDropdown(note.colorIndex, TBStyles.LabelNames, (newCol) =>
                {
                    if (newCol != note.colorIndex) { note.colorIndex = newCol; Save(); Repaint(); }
                }, TBStyles.StandardDropdown, TBStyles.GetLabelColorsArray(), "Note color label", GUILayout.Width(70));

                // Move to folder
                if (ThemedContextMenu.DropdownButton("📁", "Move Note to Folder", TBStyles.IconButton, out Rect btnRect, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    var menu = new ThemedContextMenu();
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
                    menu.Show(btnRect);
                }

                // Export single note
                if (ThemedTooltip.IconButton("📤", "Export Note", GUILayout.Width(28), GUILayout.Height(24)))
                {
                    EditorApplication.delayCall += () => ExportSingleNote(note);
                }

                if (ThemedTooltip.IconButton("↗", "Popout Note", GUILayout.Width(28), GUILayout.Height(24)))
                {
                    NotePopupWindow.Open(note, _data, () => { LoadData(); });
                }

                // Delete
                if (ThemedTooltip.DeleteIconButton(TBStyles.DeleteIcon, "Delete Note", GUILayout.Width(28), GUILayout.Height(24)))
                {
                    int idx = _selectedNote;
                    EditorApplication.delayCall += () =>
                    {
                        if (ThemedDialog.Show("Delete Note", $"Delete \"{note.title}\"?", "Delete", "Cancel"))
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
                GUILayout.Space(6);
            }

            // ── Metadata row ──
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
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
                GUILayout.Space(6);
            }

            DrawSeparator();
            GUILayout.Space(4);

            // ── Toolbar: image insert + edit/preview toggle ──
            note.imagePaths ??= new List<string>();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField("🖼 Insert", EditorStyles.miniLabel, GUILayout.Width(46));
                GUILayout.Space(4);
                if (GUILayout.Button(new GUIContent("📋 Paste", "Paste Image from Clipboard"), TBStyles.NoteActionButton, GUILayout.Width(64), GUILayout.Height(20)))
                {
                    PasteImageFromClipboard(note);
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button(new GUIContent("📎 Browse", "Browse for Image"), TBStyles.NoteActionButton, GUILayout.Width(72), GUILayout.Height(20)))
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
                if (GUILayout.Button(new GUIContent("✏ Edit", "Edit Note Mode"), _noteEditMode ? TBStyles.ToolbarButtonActive : TBStyles.ToolbarButton, GUILayout.Width(58), GUILayout.Height(20)))
                    _noteEditMode = true;
                if (GUILayout.Button(new GUIContent("👁 Preview", "Preview Note Mode"), !_noteEditMode ? TBStyles.ToolbarButtonActive : TBStyles.ToolbarButton, GUILayout.Width(76), GUILayout.Height(20)))
                    _noteEditMode = false;
                GUILayout.Space(6);
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

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                if (_noteEditMode)
                {
                    // ── Raw markdown editor ──
                    using (var editorScope = new EditorGUILayout.ScrollViewScope(_noteEditorScroll))
                    {
                        _noteEditorScroll = editorScope.scrollPosition;
                        string newContent = EditorGUILayout.TextArea(note.content, TBStyles.NoteTextArea, GUILayout.ExpandHeight(true));
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
                GUILayout.Space(6);
            }
            GUILayout.Space(6);
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
            ThemedDialog.Show("Exported", $"Note exported to:\n{path}", "OK");
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
                ThemedDialog.Show("Empty", "This folder has no notes to export.", "OK");
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
            ThemedDialog.Show("Exported", $"Exported {count} note(s) to:\n{path}", "OK");
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
                ThemedDialog.Show("Imported",
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
                    if (!ThemedDialog.Show("Cannot open scene in play mode",
                        $"This is an asset that was linked from {sceneName}. Please stop playing scene and try again.",
                        "OK"))
                    {
                        return;
                    }
                    return;
                }
                
                if (!ThemedDialog.Show("Open Scene?",
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
                    int choice = ThemedDialog.ShowComplex("Include Cards?",
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


        // ════════════════════════════════════════════
        //  STYLE & THEME VIEW
        // ════════════════════════════════════════════
        private void DrawStyleView()
        {
            if (_data == null) return;
            _data.themes ??= new List<ThemeData>();
            if (_data.themes.Count == 0)
            {
                _data.themes.Add(ThemeData.CreateDefault());
            }

            _data.currentThemeIndex = Mathf.Clamp(_data.currentThemeIndex, 0, _data.themes.Count - 1);
            var currentTheme = _data.themes[_data.currentThemeIndex];
            currentTheme.Normalize();

            // ── Top Toolbar ──
            using (var tbScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(24)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(tbScope.rect, TBStyles.TopBarBg);
                }

                ThemedTooltip.Label("Theme:", "Select or switch the active visual theme", EditorStyles.miniLabel, GUILayout.Width(48));

                string[] themeNames = _data.themes.Select(t => t.name).ToArray();
                TBStyles.DrawThemedDropdown(_data.currentThemeIndex, themeNames, (newThemeIdx) =>
                {
                    if (newThemeIdx != _data.currentThemeIndex)
                    {
                        _data.currentThemeIndex = newThemeIdx;
                        TBStyles.ApplyTheme(_data.themes[_data.currentThemeIndex]);
                        SaveTheme();
                        GUIUtility.ExitGUI();
                    }
                }, TBStyles.ToolbarPopup, "Select active theme", GUILayout.Width(160));

                // Theme Options Menu (+)
                if (ThemedContextMenu.DropdownButton("+", "Theme Options (Create, Duplicate, Apply Preset, Reset, Import, Export, Delete)", TBStyles.ToolbarButton, out Rect themeBtnRect, GUILayout.Width(24)))
                {
                    var menu = new ThemedContextMenu();
                    menu.AddItem(new GUIContent("Create New Theme/Blank Theme (Default)"), false, () =>
                    {
                        var newTheme = ThemeData.CreateDefault();
                        newTheme.name = GetUniqueThemeName("New Theme");
                        _data.themes.Add(newTheme);
                        _data.currentThemeIndex = _data.themes.Count - 1;
                        TBStyles.ApplyTheme(newTheme);
                        SaveTheme();
                        TriggerSuccessNotification($"Created theme \"{newTheme.name}\"");
                    });
                    menu.AddItem(new GUIContent("Create New Theme/Duplicate Current Theme"), false, () =>
                    {
                        var clone = currentTheme.Clone();
                        clone.name = GetUniqueThemeName($"{currentTheme.name} Copy");
                        _data.themes.Add(clone);
                        _data.currentThemeIndex = _data.themes.Count - 1;
                        TBStyles.ApplyTheme(clone);
                        SaveTheme();
                        TriggerSuccessNotification($"Duplicated theme as \"{clone.name}\"");
                    });

                    menu.AddSeparator("Create New Theme/");
                    foreach (var preset in ThemeData.GetBuiltInPresets())
                    {
                        var p = preset;
                        menu.AddItem(new GUIContent($"Create New Theme/From Preset: {p.name}"), false, () =>
                        {
                            var newPreset = p.Clone();
                            newPreset.name = GetUniqueThemeName(p.name);
                            _data.themes.Add(newPreset);
                            _data.currentThemeIndex = _data.themes.Count - 1;
                            TBStyles.ApplyTheme(newPreset);
                            SaveTheme();
                            TriggerSuccessNotification($"Created theme from preset \"{p.name}\"");
                        });
                    }

                    menu.AddSeparator("");
                    foreach (var preset in ThemeData.GetBuiltInPresets())
                    {
                        var p = preset;
                        menu.AddItem(new GUIContent($"Apply Preset to Current Theme/{p.name}"), false, () =>
                        {
                            if (ThemedDialog.Show("Apply Preset", $"Overwrite current theme \"{currentTheme.name}\" with preset \"{p.name}\"?", "Apply", "Cancel"))
                            {
                                string savedName = currentTheme.name;
                                var pClone = p.Clone();
                                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(pClone), currentTheme);
                                currentTheme.name = savedName;
                                currentTheme.Normalize();
                                TBStyles.ApplyTheme(currentTheme);
                                SaveTheme();
                                TriggerSuccessNotification($"Applied preset \"{p.name}\" to current theme");
                            }
                        });
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Reset Current Theme to Default Values"), false, () =>
                    {
                        if (ThemedDialog.Show("Reset Theme", $"Reset all colors and icons in \"{currentTheme.name}\" to defaults?", "Reset", "Cancel"))
                        {
                            string savedName = currentTheme.name;
                            var def = ThemeData.CreateDefault();
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(def), currentTheme);
                            currentTheme.name = savedName;
                            currentTheme.Normalize();
                            TBStyles.ApplyTheme(currentTheme);
                            SaveTheme();
                            TriggerSuccessNotification("Theme reset to defaults");
                        }
                    });

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Export/Export Active Theme (JSON)..."), false, () => ExportTheme(currentTheme));
                    menu.AddItem(new GUIContent("Export/Export All Themes Pack (JSON)..."), false, ExportAllThemes);
                    menu.AddItem(new GUIContent("Import/Import Theme(s) from JSON..."), false, ImportTheme);

                    if (_data.themes.Count > 1)
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent($"Delete Theme/{currentTheme.name}"), false, () =>
                        {
                            if (ThemedDialog.Show("Delete Theme", $"Are you sure you want to delete theme \"{currentTheme.name}\"?", "Delete", "Cancel"))
                            {
                                string deletedName = currentTheme.name;
                                _data.themes.RemoveAt(_data.currentThemeIndex);
                                _data.currentThemeIndex = Mathf.Clamp(_data.currentThemeIndex, 0, _data.themes.Count - 1);
                                TBStyles.ApplyTheme(_data.themes[_data.currentThemeIndex]);
                                SaveTheme();
                                TriggerSuccessNotification($"Deleted theme \"{deletedName}\"");
                            }
                        });
                    }

                    menu.Show(themeBtnRect);
                }

                GUILayout.Space(6);

                if (ThemedContextMenu.DropdownButton("Presets ▾", "Load or apply built-in theme presets (Dark Slate, Cyberpunk, Forest, Pastel, Sunset, Monochrome, Retro, Vintage 8-Bit, etc.)", TBStyles.ToolbarButton, out Rect presetBtnRect, GUILayout.Width(72)))
                {
                    var presetMenu = new ThemedContextMenu();
                    foreach (var preset in ThemeData.GetBuiltInPresets())
                    {
                        var p = preset;
                        presetMenu.AddItem(new GUIContent($"Add as New Theme/{p.name}"), false, () =>
                        {
                            var newPreset = p.Clone();
                            newPreset.name = GetUniqueThemeName(p.name);
                            _data.themes.Add(newPreset);
                            _data.currentThemeIndex = _data.themes.Count - 1;
                            TBStyles.ApplyTheme(newPreset);
                            SaveTheme();
                            TriggerSuccessNotification($"Added theme preset \"{p.name}\"");
                        });
                        presetMenu.AddItem(new GUIContent($"Apply to Current Theme/{p.name}"), false, () =>
                        {
                            if (ThemedDialog.Show("Apply Preset", $"Overwrite \"{currentTheme.name}\" with preset \"{p.name}\"?", "Apply", "Cancel"))
                            {
                                string savedName = currentTheme.name;
                                var pClone = p.Clone();
                                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(pClone), currentTheme);
                                currentTheme.name = savedName;
                                currentTheme.Normalize();
                                TBStyles.ApplyTheme(currentTheme);
                                SaveTheme();
                                TriggerSuccessNotification($"Applied preset \"{p.name}\"");
                            }
                        });
                    }
                    presetMenu.Show(presetBtnRect);
                }

                GUILayout.Space(4);

                if (ThemedContextMenu.DropdownButton("💾 Export", "Export theme configuration to JSON file", TBStyles.ToolbarButton, out Rect exportBtnRect, GUILayout.Width(64)))
                {
                    var exportMenu = new ThemedContextMenu();
                    exportMenu.AddItem(new GUIContent($"Export Active Theme (\"{currentTheme.name}\")..."), false, () => ExportTheme(currentTheme));
                    exportMenu.AddItem(new GUIContent($"Export All Themes Pack ({_data.themes.Count} themes)..."), false, ExportAllThemes);
                    exportMenu.Show(exportBtnRect);
                }

                if (ThemedTooltip.Button("📥 Import", "Import theme JSON file or Theme Pack into your theme collection", TBStyles.ToolbarButton, GUILayout.Width(64)))
                {
                    ImportTheme();
                }

                if (_data.themes.Count > 1)
                {
                    GUILayout.Space(4);
                    if (ThemedTooltip.Button($"{TBStyles.DeleteIcon} Delete", $"Delete active theme \"{currentTheme.name}\"", TBStyles.ToolbarDeleteButton, GUILayout.Width(76)))
                    {
                        if (ThemedDialog.Show("Delete Theme", $"Are you sure you want to delete theme \"{currentTheme.name}\"?", "Delete", "Cancel"))
                        {
                            string deletedName = currentTheme.name;
                            _data.themes.RemoveAt(_data.currentThemeIndex);
                            _data.currentThemeIndex = Mathf.Clamp(_data.currentThemeIndex, 0, _data.themes.Count - 1);
                            TBStyles.ApplyTheme(_data.themes[_data.currentThemeIndex]);
                            SaveTheme();
                            TriggerSuccessNotification($"Deleted theme \"{deletedName}\"");
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            // ── Style View Body ──
            using (var scope = new EditorGUILayout.ScrollViewScope(_styleScroll))
            {
                _styleScroll = scope.scrollPosition;

            

                EditorGUI.BeginChangeCheck();

                // ── Section 1: Theme Identity & Live Interactive Preview ──
                DrawStyleHeaderAndPreview(currentTheme);

                GUILayout.Space(12);

                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    ThemedTooltip.Label("🔍 Style Search", "Filter style options by label, tooltip text, or current value", null, GUILayout.Width(95));
                    _styleSearchFilter = TBStyles.DrawThemedTextField(_styleSearchFilter, TBStyles.ThemedSearchField, GUILayout.Height(20));

                    if (ThemedTooltip.Button("Clear", "Clear the current style search and show all style options", TBStyles.ToolbarButton, GUILayout.Width(52), GUILayout.Height(18)))
                    {
                        _styleSearchFilter = "";
                        GUI.FocusControl(null);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_styleSearchFilter))
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.HelpBox($"Searching style options for \"{_styleSearchFilter.Trim()}\". Matching sections are shown below; clear the search to reveal everything.", MessageType.Info);
                    }
                }

                GUILayout.Space(12);
                
                // ── Section 2: Priority Icons ──
                DrawStylePriorityIcons(currentTheme);

                GUILayout.Space(12);

                // ── Section 3: Interface & Navigation Icons ──
                DrawStyleInterfaceIcons(currentTheme);

                GUILayout.Space(12);

                // ── Section 4: Card & Note Label Colors ──
                DrawStyleLabelColors(currentTheme);

                GUILayout.Space(12);

                // ── Section 5: Board, Column & UI Accent Colors ──
                DrawStyleBoardAndAccentColors(currentTheme);

                GUILayout.Space(12);

                // ── Section 6: Export / Import Tools ──
                DrawStyleImportExportActions(currentTheme);

                GUILayout.Space(24);

                if (EditorGUI.EndChangeCheck())
                {
                    currentTheme.Normalize();
                    TBStyles.ApplyTheme(currentTheme);
                    SaveTheme();
                }
            }
        }

        private void DrawStyleHeaderAndPreview(ThemeData theme)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{TBStyles.StyleTabIcon} Theme Information & Preview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Customize theme name, colors, and icons. All changes apply live to your workspace in real-time.", MessageType.None);
                GUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    ThemedTooltip.Label("Theme Name", "The display name identifying this visual theme preset in the theme selector and export packages", null, GUILayout.Width(90));
                    theme.name = TBStyles.DrawThemedTextField(theme.name, TBStyles.ThemedTextField, GUILayout.Height(20));
                    ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), "The display name identifying this visual theme preset in the theme selector and export packages");

                    if (ThemedTooltip.Button("Duplicate", "Create a duplicate copy of this theme with all its color and icon settings", GUI.skin.button, GUILayout.Width(75)))
                    {
                        var clone = theme.Clone();
                        clone.name = GetUniqueThemeName($"{theme.name} Copy");
                        _data.themes.Add(clone);
                        _data.currentThemeIndex = _data.themes.Count - 1;
                        TBStyles.ApplyTheme(clone);
                        SaveTheme();
                        TriggerSuccessNotification($"Duplicated theme as \"{clone.name}\"");
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.Space(8);

                // ── Live Interactive Preview Mockup ──
                EditorGUILayout.LabelField("Live Workspace Preview", EditorStyles.miniBoldLabel);
                
                var previewRect = GUILayoutUtility.GetRect(0, 156, GUILayout.ExpandWidth(true));
                ThemedTooltip.SetTooltip(previewRect, "Live interactive preview reflecting the colors, typography, icons, and contrast settings of the currently selected theme");
                Color boardBgColor = EditorGUIUtility.isProSkin ? theme.pro_BoardBg : theme.personal_BoardBg;
                TBStyles.DrawCanvasBackground(previewRect, boardBgColor, false);
                TBStyles.DrawBorderRect(previewRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.12f));

                // Draw mock top bar strip inside preview
                var topBarRect = new Rect(previewRect.x, previewRect.y, previewRect.width, 32);
                Color topBarBgColor = EditorGUIUtility.isProSkin ? theme.pro_TopBarBg : theme.personal_TopBarBg;
                TBStyles.DrawGlassPanel(topBarRect, topBarBgColor, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);

                // Draw mock tab bar inside preview (Active Tab)
                var tabRect = new Rect(previewRect.x + 8, previewRect.y + 4, 85, 24);
                Color tabActiveBg = EditorGUIUtility.isProSkin ? theme.pro_HeaderTabActiveBg : theme.personal_HeaderTabActiveBg;
                Color tabActiveText = EditorGUIUtility.isProSkin ? theme.pro_HeaderTabActiveText : theme.personal_HeaderTabActiveText;
                TBStyles.DrawGlassPanel(tabRect, tabActiveBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.20f) : new Color(1f, 1f, 1f, 0.60f), true);
                var tabStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = tabActiveText }, fontSize = 11 };
                GUI.Label(tabRect, $"{theme.boardTabIcon} Board", tabStyle);

                // Inactive Tab
                var noteTabRect = new Rect(tabRect.xMax + 4, previewRect.y + 4, 85, 24);
                Color tabInactiveBg = EditorGUIUtility.isProSkin ? theme.pro_HeaderTabInactiveBg : theme.personal_HeaderTabInactiveBg;
                Color tabInactiveText = EditorGUIUtility.isProSkin ? theme.pro_HeaderTabInactiveText : theme.personal_HeaderTabInactiveText;
                TBStyles.DrawGlassPanel(noteTabRect, tabInactiveBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(1f, 1f, 1f, 0.40f), true);
                var inactiveTabStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = tabInactiveText }, fontSize = 11 };
                GUI.Label(noteTabRect, $"{theme.notesTabIcon} Notes", inactiveTabStyle);

                // Mock Dropdown in top bar
                var dropRect = new Rect(noteTabRect.xMax + 8, previewRect.y + 6, 90, 20);
                Color dropBg = EditorGUIUtility.isProSkin ? theme.pro_DropdownBg : theme.personal_DropdownBg;
                Color dropText = EditorGUIUtility.isProSkin ? theme.pro_DropdownText : theme.personal_DropdownText;
                TBStyles.DrawGlassPanel(dropRect, dropBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(1f, 1f, 1f, 0.40f), true);
                var dropStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 4, 0, 0), normal = { textColor = dropText }, fontSize = 10 };
                GUI.Label(dropRect, $"{theme.categoryIcon} All Categories ▾", dropStyle);

                // Draw mock column inside preview
                float colWidth = Mathf.Min(260, (previewRect.width - 24) * 0.55f);
                var colRect = new Rect(previewRect.x + 8, previewRect.y + 38, colWidth, 94);
                Color colBg = EditorGUIUtility.isProSkin ? theme.pro_ColumnBg : theme.personal_ColumnBg;
                TBStyles.DrawGlassPanel(colRect, colBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);

                var colHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_ColumnHeader : theme.personal_ColumnHeader } };
                GUI.Label(new Rect(colRect.x + 6, colRect.y + 4, colRect.width - 12, 18), $"{theme.boardHeaderIcon} In Progress (1)", colHeaderStyle);

                // Draw mock card inside column
                var cardRect = new Rect(colRect.x + 6, colRect.y + 22, colRect.width - 12, 46);
                Color cardBgColor = EditorGUIUtility.isProSkin ? theme.pro_CardBg : theme.personal_CardBg;
                TBStyles.DrawGlassPanel(cardRect, cardBgColor, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.45f), true);
                
                // Card label bar
                Color sampleLabelColor = theme.labelColors != null && theme.labelColors.Count > 2 ? theme.labelColors[2] : new Color(0.13f, 0.59f, 0.95f);
                EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 3), sampleLabelColor);

                string priIcon = theme.priorityIcons != null && theme.priorityIcons.Count > 3 ? theme.priorityIcons[3] : "🟠";
                var cardTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_CardTitle : theme.personal_CardTitle } };
                GUI.Label(new Rect(cardRect.x + 4, cardRect.y + 4, cardRect.width - 8, 16), $"{theme.completedIcon} {priIcon} Task Title", cardTitleStyle);

                var cardCatStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, fontStyle = FontStyle.Bold, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_CardCategoryTag : theme.personal_CardCategoryTag } };
                var cardDueStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, fontStyle = FontStyle.Bold, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_StatusDueToday : theme.personal_StatusDueToday } };
                var cardTasksStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, fontStyle = FontStyle.Bold, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_TasksCompletedCount : theme.personal_TasksCompletedCount } };
                
                float catWidth = 48;
                GUI.Label(new Rect(cardRect.x + 4, cardRect.y + 24, catWidth, 16), $"[{theme.categoryIcon} Dev]", cardCatStyle);
                float dueWidth = 54;
                GUI.Label(new Rect(cardRect.x + 4 + catWidth + 2, cardRect.y + 24, dueWidth, 16), $"{theme.dueTodayIcon} Today", cardDueStyle);
                float tasksOffset = catWidth + dueWidth + 6;
                if (cardRect.width - tasksOffset > 38)
                {
                    var tickBoxRect = new Rect(cardRect.x + 4 + tasksOffset, cardRect.y + 26, 11, 11);
                    Color tickBg = EditorGUIUtility.isProSkin ? theme.pro_ChecklistTickCheckedBg : theme.personal_ChecklistTickCheckedBg;
                    Color tickBorder = EditorGUIUtility.isProSkin ? theme.pro_ChecklistTickBorder : theme.personal_ChecklistTickBorder;
                    Color tickColor = EditorGUIUtility.isProSkin ? theme.pro_ChecklistTickColor : theme.personal_ChecklistTickColor;
                    EditorGUI.DrawRect(tickBoxRect, tickBg);
                    TBStyles.DrawBorderRect(tickBoxRect, tickBorder, 1f);
                    var tickStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 8, fontStyle = FontStyle.Bold, normal = { textColor = tickColor } };
                    TBStyles.DrawCheckmarkIcon(tickBoxRect, tickColor, theme.checklistTickStyle, theme.customChecklistTickChar);

                    GUI.Label(new Rect(tickBoxRect.xMax + 3, cardRect.y + 24, cardRect.width - tickBoxRect.xMax - 7, 16), "2/3", cardTasksStyle);
                }
                else if (cardRect.width - tasksOffset > 24)
                {
                    GUI.Label(new Rect(cardRect.x + 4 + tasksOffset, cardRect.y + 24, cardRect.width - 8 - tasksOffset, 16), $"{theme.checklistIcon} 2/3", cardTasksStyle);
                }

                // Mock Add Card button inside column
                var addCardRect = new Rect(colRect.x + 6, colRect.y + 72, colRect.width - 12, 18);
                Color addCardBg = EditorGUIUtility.isProSkin ? theme.pro_AddCardBg : theme.personal_AddCardBg;
                Color addCardText = EditorGUIUtility.isProSkin ? theme.pro_AddCardText : theme.personal_AddCardText;
                TBStyles.DrawGlassPanel(addCardRect, addCardBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.45f), true);
                var addCardStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = addCardText }, fontSize = 9, fontStyle = FontStyle.Bold };
                GUI.Label(addCardRect, "+ Add Card", addCardStyle);

                // Draw mock note inside preview
                float noteX = colRect.xMax + 12;
                float noteWidth = previewRect.xMax - noteX - 8;
                if (noteWidth > 120)
                {
                    var noteRect = new Rect(noteX, previewRect.y + 38, noteWidth, 94);
                    Color noteEdBg = EditorGUIUtility.isProSkin ? theme.pro_NoteEditorBg : theme.personal_NoteEditorBg;
                    TBStyles.DrawGlassPanel(noteRect, noteEdBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);
                    
                    // Note selection accent bar
                    EditorGUI.DrawRect(new Rect(noteRect.x, noteRect.y, 4, noteRect.height), theme.noteSelectedAccent);

                    var noteTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = EditorGUIUtility.isProSkin ? theme.pro_NoteTitle : theme.personal_NoteTitle } };
                    GUI.Label(new Rect(noteRect.x + 8, noteRect.y + 6, noteRect.width - 12, 18), $"{theme.pinnedNoteIcon} Level 1 Design", noteTitleStyle);

                    // Mock text input area
                    var noteInputMockRect = new Rect(noteRect.x + 8, noteRect.y + 26, noteRect.width - 16, 34);
                    Color noteInBg = EditorGUIUtility.isProSkin ? theme.pro_NoteInputBg : theme.personal_NoteInputBg;
                    Color noteInTxt = EditorGUIUtility.isProSkin ? theme.pro_NoteInputText : theme.personal_NoteInputText;
                    TBStyles.DrawGlassPanel(noteInputMockRect, noteInBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f), false);

                    var noteBodyStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = noteInTxt } };
                    GUI.Label(new Rect(noteInputMockRect.x + 4, noteInputMockRect.y + 2, noteInputMockRect.width - 8, noteInputMockRect.height - 4), $"• Bowling pins layout\n• {theme.cardDetailIcon} Setup shaders", noteBodyStyle);

                    // Mock action button inside note preview
                    float actionBtnWidth = Mathf.Min(52, (noteRect.width - 32) * 0.36f);
                    var mockBtnRect = new Rect(noteRect.x + 8, noteRect.y + 64, actionBtnWidth, 20);
                    Color addNoteBg = EditorGUIUtility.isProSkin ? theme.pro_AddNoteBg : theme.personal_AddNoteBg;
                    Color addNoteText = EditorGUIUtility.isProSkin ? theme.pro_AddNoteText : theme.personal_AddNoteText;
                    EditorGUI.DrawRect(mockBtnRect, addNoteBg);
                    var mockBtnStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = addNoteText }, fontSize = 9, fontStyle = FontStyle.Bold };
                    GUI.Label(mockBtnRect, "+ Note", mockBtnStyle);

                    // Mock import button inside note preview
                    var mockImportRect = new Rect(mockBtnRect.xMax + 4, noteRect.y + 64, 22, 20);
                    Color importNoteBg = EditorGUIUtility.isProSkin ? theme.pro_ImportNoteBg : theme.personal_ImportNoteBg;
                    Color importNoteText = EditorGUIUtility.isProSkin ? theme.pro_ImportNoteText : theme.personal_ImportNoteText;
                    EditorGUI.DrawRect(mockImportRect, importNoteBg);
                    var mockImportStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = importNoteText }, fontSize = 10, fontStyle = FontStyle.Bold };
                    GUI.Label(mockImportRect, "📥", mockImportStyle);

                    // Mock delete button inside note preview
                    var mockDelRect = new Rect(mockImportRect.xMax + 6, noteRect.y + 64, Mathf.Min(54, noteRect.width - mockImportRect.xMax - 14), 20);
                    Color delBg = EditorGUIUtility.isProSkin ? theme.pro_DeleteBtnBg : theme.personal_DeleteBtnBg;
                    Color delText = EditorGUIUtility.isProSkin ? theme.pro_DeleteBtnText : theme.personal_DeleteBtnText;
                    EditorGUI.DrawRect(mockDelRect, delBg);
                    var mockDelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = delText }, fontSize = 9, fontStyle = FontStyle.Bold };
                    GUI.Label(mockDelRect, $"{theme.deleteIcon} Delete", mockDelStyle);
                }

                // Draw mock floating tooltip inside preview
                var tooltipMockRect = new Rect(previewRect.xMax - 116, previewRect.y + 4, 108, 18);
                Color ttBg = EditorGUIUtility.isProSkin ? theme.pro_TooltipBg : theme.personal_TooltipBg;
                Color ttBorder = EditorGUIUtility.isProSkin ? theme.pro_TooltipBorder : theme.personal_TooltipBorder;
                Color ttText = EditorGUIUtility.isProSkin ? theme.pro_TooltipText : theme.personal_TooltipText;
                EditorGUI.DrawRect(new Rect(tooltipMockRect.x + 1, tooltipMockRect.y + 1, tooltipMockRect.width, tooltipMockRect.height), new Color(0, 0, 0, 0.35f));
                EditorGUI.DrawRect(tooltipMockRect, ttBg);
                TBStyles.DrawBorderRect(tooltipMockRect, ttBorder, 1f);
                var ttMockStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = ttText }, fontSize = 9 };
                GUI.Label(tooltipMockRect, "💬 Themed Tooltip", ttMockStyle);

                // Draw mock status bar inside preview
                var mockStatusRect = new Rect(previewRect.x, previewRect.yMax - 18, previewRect.width, 18);
                Color statusBg = EditorGUIUtility.isProSkin ? theme.pro_StatusBarBg : theme.personal_StatusBarBg;
                Color statusTextCol = EditorGUIUtility.isProSkin ? theme.pro_StatusBarText : theme.personal_StatusBarText;
                TBStyles.DrawGlassPanel(mockStatusRect, statusBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.30f), false);
                var mockStatusStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 0, 0), normal = { textColor = statusTextCol }, fontSize = 9, richText = true };
                GUI.Label(mockStatusRect, $" 1 card  •  {theme.completedIcon} 0 completed  •  1 column", mockStatusStyle);
            }
        }

        private void DrawStylePriorityIcons(ThemeData theme)
        {
            if (!StyleSectionMatchesSearch("priority", "priorities", "low", "medium", "high", "urgent", "none")) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _styleSectionHasVisibleAttribute = false;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🚩 Priority Icons", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (ThemedTooltip.Button("Reset Priority Icons", "Reset all priority icons (None, Low, Medium, High, Urgent) to their default symbols", GUI.skin.button, GUILayout.Width(135)))
                    {
                        theme.priorityIcons = new List<string>(ThemeData.DefaultPriorityIcons);
                    }
                }

                EditorGUILayout.HelpBox("Configure the icon / symbol used for each priority level (supports emoji or plain text).", MessageType.None);
                GUILayout.Space(4);

                theme.priorityIcons ??= new List<string>();
                while (theme.priorityIcons.Count < 5) theme.priorityIcons.Add("");

                string[] priorityLabels = { "0: None (—)", "1: Low", "2: Medium", "3: High", "4: Urgent" };
                string[] priorityTooltips = {
                    "Icon/symbol displayed on task cards and filter dropdowns when priority is None (empty by default)",
                    "Icon/symbol displayed on task cards and filter dropdowns when priority is set to Low (e.g. 🔵)",
                    "Icon/symbol displayed on task cards and filter dropdowns when priority is set to Medium (e.g. 🟡)",
                    "Icon/symbol displayed on task cards and filter dropdowns when priority is set to High (e.g. 🟠)",
                    "Icon/symbol displayed on task cards and filter dropdowns when priority is set to Urgent (e.g. 🔴)"
                };
                for (int i = 0; i < 5; i++)
                {
                    if (!StyleOptionMatches(priorityLabels[i], priorityTooltips[i], theme.priorityIcons[i]))
                    {
                        continue;
                    }

                    _styleSectionHasVisibleAttribute = true;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        ThemedTooltip.Label(priorityLabels[i], priorityTooltips[i], null, GUILayout.Width(110));
                        theme.priorityIcons[i] = TBStyles.DrawThemedTextField(theme.priorityIcons[i], GUILayout.Width(100), GUILayout.Height(20));
                        ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), priorityTooltips[i]);

                        string preview = string.IsNullOrEmpty(theme.priorityIcons[i]) ? TBStyles.PriorityNames[i] : $"{theme.priorityIcons[i]} {TBStyles.PriorityNames[i]}";
                        EditorGUILayout.LabelField($"Preview: {preview}", EditorStyles.miniBoldLabel);
                    }
                }

                DrawNoStyleAttributeMatchesHint();
            }
        }

        private void DrawStyleInterfaceIcons(ThemeData theme)
        {
            if (!StyleSectionMatchesSearch("icon", "icons", "interface", "status", "navigation", "header", "headers", "tab", "tabs", "board", "notes", "style", "theme", "category", "assignee", "priority", "pinned", "completed", "overdue", "due", "archive", "unarchive", "attachment", "link", "checklist", "save", "cancel", "delete", "move", "new")) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _styleSectionHasVisibleAttribute = false;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🏷 Interface & Status Icons", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (ThemedTooltip.Button("Reset Interface Icons", "Reset all navigation tabs, headers, status indicators, and action icons to their default symbols", GUI.skin.button, GUILayout.Width(145)))
                    {
                        var def = ThemeData.CreateDefault();
                        theme.boardTabIcon = def.boardTabIcon;
                        theme.notesTabIcon = def.notesTabIcon;
                        theme.styleTabIcon = def.styleTabIcon;
                        theme.boardHeaderIcon = def.boardHeaderIcon;
                        theme.notesHeaderIcon = def.notesHeaderIcon;
                        theme.categoryIcon = def.categoryIcon;
                        theme.assigneeIcon = def.assigneeIcon;
                        theme.priorityFilterIcon = def.priorityFilterIcon;
                        theme.parentLinkIcon = def.parentLinkIcon;
                        theme.childLinkIcon = def.childLinkIcon;
                        theme.pinnedNoteIcon = def.pinnedNoteIcon;
                        theme.completedIcon = def.completedIcon;
                        theme.overdueIcon = def.overdueIcon;
                        theme.dueTodayIcon = def.dueTodayIcon;
                        theme.dueSoonIcon = def.dueSoonIcon;
                        theme.dueDateIcon = def.dueDateIcon;
                        theme.archiveIcon = def.archiveIcon;
                        theme.unarchiveIcon = def.unarchiveIcon;
                        theme.cardDetailIcon = def.cardDetailIcon;
                        theme.newCardIcon = def.newCardIcon;
                        theme.checklistIcon = def.checklistIcon;
                        theme.attachmentIcon = def.attachmentIcon;
                        theme.urlIcon = def.urlIcon;
                        theme.deleteIcon = def.deleteIcon;
                        theme.saveIcon = def.saveIcon;
                        theme.cancelIcon = def.cancelIcon;
                        theme.moveUpIcon = def.moveUpIcon;
                        theme.moveDownIcon = def.moveDownIcon;
                    }
                }

                EditorGUILayout.HelpBox("Customize icons used across navigation tabs, headers, buttons, cards, status tags, dialogs, and archival actions.", MessageType.None);
                GUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        theme.boardTabIcon = DrawIconRow("Board Tab Icon", theme.boardTabIcon, "Icon displayed on the main Task Board navigation tab at the top toolbar");
                        theme.notesTabIcon = DrawIconRow("Notes Tab Icon", theme.notesTabIcon, "Icon displayed on the Notes workspace navigation tab at the top toolbar");
                        theme.styleTabIcon = DrawIconRow("Style Tab Icon", theme.styleTabIcon, "Icon displayed on the Style / Theme customization navigation tab at the top toolbar");
                        theme.boardHeaderIcon = DrawIconRow("Board Header Icon", theme.boardHeaderIcon, "Icon prefix displayed on task column header titles across the board");
                        theme.notesHeaderIcon = DrawIconRow("Notes Header Icon", theme.notesHeaderIcon, "Icon prefix displayed on note section headers and note workspace titles");
                        theme.pinnedNoteIcon = DrawIconRow("Pinned Note Icon", theme.pinnedNoteIcon, "Icon displayed on pinned notes and toggle pin buttons to indicate stickied priority notes");
                        theme.completedIcon = DrawIconRow("Completed Icon", theme.completedIcon, "Icon prefix displayed on completed task cards and checklist items when finished");
                        theme.overdueIcon = DrawIconRow("Overdue Icon", theme.overdueIcon, "Status indicator icon displayed on task cards when their due date has passed without completion");
                        theme.dueTodayIcon = DrawIconRow("Due Today Icon", theme.dueTodayIcon, "Status indicator icon displayed on task cards when their due date is set to today");
                        theme.dueSoonIcon = DrawIconRow("Due Soon Icon", theme.dueSoonIcon, "Status indicator icon displayed on task cards when their due date is approaching within the next 48 hours");
                        theme.dueDateIcon = DrawIconRow("Due Date Icon", theme.dueDateIcon, "Icon prefix for due date picker labels, calendar badges, and timeline indicators");
                        theme.cardDetailIcon = DrawIconRow("Card Details Icon", theme.cardDetailIcon, "Icon used for the card details inspector button and edit card actions");
                        theme.newCardIcon = DrawIconRow("New Card Icon", theme.newCardIcon, "Icon displayed on buttons and menu items for creating a new task card");
                        theme.checklistIcon = DrawIconRow("Checklist Icon", theme.checklistIcon, "Icon displayed on card checklist progress indicators and subtask sections");
                    }

                    GUILayout.Space(16);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        theme.categoryIcon = DrawIconRow("Category Icon", theme.categoryIcon, "Icon prefix for category filter dropdowns, category badges, and category management");
                        theme.assigneeIcon = DrawIconRow("Assignee Icon", theme.assigneeIcon, "Icon prefix for assignee selector dropdowns, member lists, and assignment filters");
                        theme.priorityFilterIcon = DrawIconRow("Priority Filter Icon", theme.priorityFilterIcon, "Icon prefix for the priority filter dropdown on the main board toolbar");
                        theme.parentLinkIcon = DrawIconRow("Parent Link Icon", theme.parentLinkIcon, "Icon representing parent / ancestor relationships on linked hierarchical task cards");
                        theme.childLinkIcon = DrawIconRow("Child Link Icon", theme.childLinkIcon, "Icon representing subtask or dependent child cards on linked task cards");
                        theme.archiveIcon = DrawIconRow("Archive Icon", theme.archiveIcon, "Icon displayed on buttons and menu items for archiving completed or inactive cards");
                        theme.unarchiveIcon = DrawIconRow("Unarchive Icon", theme.unarchiveIcon, "Icon displayed on buttons to restore or unarchive previously archived cards");
                        theme.attachmentIcon = DrawIconRow("Attachment Icon", theme.attachmentIcon, "Icon prefix for file attachment buttons, screenshot lists, and attached asset files");
                        theme.urlIcon = DrawIconRow("URL / Link Icon", theme.urlIcon, "Icon displayed on hyperlinks, external URL attachments, and reference links");
                        theme.deleteIcon = DrawIconRow("Delete Icon", theme.deleteIcon, "Icon displayed on delete and remove buttons across columns, cards, notes, and tags");
                        theme.saveIcon = DrawIconRow("Save Icon", theme.saveIcon, "Icon displayed on save, commit, and export action buttons");
                        theme.cancelIcon = DrawIconRow("Cancel / Close Icon", theme.cancelIcon, "Icon displayed on cancel, close, and dismiss buttons across dialogs and popouts");
                        theme.moveUpIcon = DrawIconRow("Move Up Icon", theme.moveUpIcon, "Icon displayed on buttons to reorder columns, checklist items, or notes upward");
                        theme.moveDownIcon = DrawIconRow("Move Down Icon", theme.moveDownIcon, "Icon displayed on buttons to reorder columns, checklist items, or notes downward");
                    }
                }

                DrawNoStyleAttributeMatchesHint();
            }
        }

        private string DrawIconRow(string label, string currentVal, string tooltip = null)
        {
            if (!_styleSearchForceShowCurrentSection && !StyleOptionMatches(label, tooltip, currentVal))
            {
                return currentVal;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _styleSectionHasVisibleAttribute = true;

                if (!string.IsNullOrEmpty(tooltip))
                {
                    ThemedTooltip.Label(label, tooltip, null, GUILayout.Width(130));
                }
                else
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(130));
                }

                string newVal = TBStyles.DrawThemedTextField(currentVal ?? "", GUILayout.Width(60), GUILayout.Height(20));
                if (!string.IsNullOrEmpty(tooltip))
                {
                    ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
                }

                EditorGUILayout.LabelField($" {newVal}", EditorStyles.boldLabel, GUILayout.Width(35));
                return newVal;
            }
        }

        private void DrawStyleLabelColors(ThemeData theme)
        {
            if (!StyleSectionMatchesSearch("label", "labels", "card", "cards", "note", "notes", "category", "categories", "avatar", "avatars", "color", "colors")) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _styleSectionHasVisibleAttribute = false;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🏷 Card & Note Label Colors", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (ThemedTooltip.Button("Reset Label Colors", "Reset all 17 card and note label category colors to their default palette", GUI.skin.button, GUILayout.Width(130)))
                    {
                        theme.labelColors = new List<Color>(ThemeData.DefaultLabelColors);
                    }
                }

                EditorGUILayout.HelpBox("Customize the 17 color tags used for cards, quick notes, categories, and member avatars.", MessageType.None);
                GUILayout.Space(4);

                theme.labelColors ??= new List<Color>();
                while (theme.labelColors.Count < TBStyles.LabelNames.Length)
                {
                    int idx = theme.labelColors.Count;
                    theme.labelColors.Add(ThemeData.DefaultLabelColors[Mathf.Clamp(idx, 0, ThemeData.DefaultLabelColors.Length - 1)]);
                }

                int half = Mathf.CeilToInt(TBStyles.LabelNames.Length / 2f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = 0; i < half; i++)
                        {
                            DrawColorSwatchRow(i, theme);
                        }
                    }

                    GUILayout.Space(16);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = half; i < TBStyles.LabelNames.Length; i++)
                        {
                            DrawColorSwatchRow(i, theme);
                        }
                    }
                }

                DrawNoStyleAttributeMatchesHint();
            }
        }

        private void DrawColorSwatchRow(int index, ThemeData theme)
        {
            string labelName = (index >= 0 && index < TBStyles.LabelNames.Length) ? TBStyles.LabelNames[index] : $"Color {index}";
            string tooltip = $"Customize tag color [{index}] ({labelName}), used for card color strips, note highlights, category tags, and member avatar borders";
            if (!_styleSearchForceShowCurrentSection && !StyleOptionMatches($"[{index}] {labelName}", tooltip))
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _styleSectionHasVisibleAttribute = true;

                ThemedTooltip.Label($"[{index}] {labelName}", tooltip, null, GUILayout.Width(120));
                theme.labelColors[index] = EditorGUILayout.ColorField(GUIContent.none, theme.labelColors[index], false, true, false, GUILayout.Width(65));
                ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);

                var swatchRect = GUILayoutUtility.GetRect(24, 18, GUILayout.Width(24));
                EditorGUI.DrawRect(swatchRect, theme.labelColors[index]);
                ThemedTooltip.SetTooltip(swatchRect, tooltip);
            }
        }

        private Color DrawThemeColorOption(string label, Color current, string tooltip, float labelWidth, float fieldWidth)
        {
            if (!_styleSearchForceShowCurrentSection && !StyleOptionMatches(label, tooltip))
            {
                return current;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _styleSectionHasVisibleAttribute = true;

                ThemedTooltip.Label(label, tooltip, null, GUILayout.Width(labelWidth));
                current = EditorGUILayout.ColorField(GUIContent.none, current, false, true, false, GUILayout.Width(fieldWidth));
                ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }

            return current;
        }

        private static string BuildThemeOptionTooltip(string skinMode, string optionDescription)
        {
            return $"{skinMode}: {optionDescription}";
        }

        private void DrawNoStyleAttributeMatchesHint()
        {
            if (!string.IsNullOrWhiteSpace(_styleSearchFilter) && !_styleSectionHasVisibleAttribute)
            {
                EditorGUILayout.HelpBox("No attribute matches in this section.", MessageType.None);
            }
        }

        private static string NormalizeStyleSearchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        private static bool StyleSearchCandidateMatches(string candidate, string normalizedFilter, string[] normalizedTerms)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            string normalizedCandidate = NormalizeStyleSearchText(candidate);
            if (normalizedCandidate.Contains(normalizedFilter))
            {
                return true;
            }

            for (int i = 0; i < normalizedTerms.Length; i++)
            {
                if (!normalizedCandidate.Contains(normalizedTerms[i]))
                {
                    return false;
                }
            }

            return normalizedTerms.Length > 1;
        }

        private bool StyleSectionMatchesSearch(params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(_styleSearchFilter))
            {
                return true;
            }

            if (keywords == null)
            {
                return true;
            }

            string filter = _styleSearchFilter.Trim();
            string normalizedFilter = NormalizeStyleSearchText(filter);
            string[] normalizedTerms = Regex.Split(filter, @"\s+")
                .Select(NormalizeStyleSearchText)
                .Where(term => term.Length > 0)
                .ToArray();

            // Also match against the section keyword set as a whole.
            string combinedKeywords = string.Join(" ", keywords.Where(k => !string.IsNullOrWhiteSpace(k)));
            if (StyleSearchCandidateMatches(combinedKeywords, normalizedFilter, normalizedTerms))
            {
                return true;
            }

            foreach (string keyword in keywords)
            {
                if (StyleSearchCandidateMatches(keyword, normalizedFilter, normalizedTerms))
                {
                    return true;
                }
            }

            // Do not hide sections on keyword-list misses.
            // Row-level filtering is more precise and still narrows the actual attributes.
            return true;
        }

        private bool StyleOptionMatches(string label, string tooltip = null, params string[] extraTerms)
        {
            if (string.IsNullOrWhiteSpace(_styleSearchFilter))
            {
                return true;
            }

            string filter = _styleSearchFilter.Trim();
            string normalizedFilter = NormalizeStyleSearchText(filter);
            string[] normalizedTerms = Regex.Split(filter, @"\s+")
                .Select(NormalizeStyleSearchText)
                .Where(term => term.Length > 0)
                .ToArray();

            bool Matches(string value)
            {
                return StyleSearchCandidateMatches(value, normalizedFilter, normalizedTerms);
            }

            if (Matches(label) || Matches(tooltip))
            {
                return true;
            }

            if (extraTerms != null)
            {
                foreach (string term in extraTerms)
                {
                    if (Matches(term))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void DrawStyleBoardAndAccentColors(ThemeData theme)
        {
            if (!StyleSectionMatchesSearch("board", "notes", "button", "buttons", "ui", "tab", "tabs", "dropdown", "filter", "background", "backgrounds", "bg", "sidebar", "topbar", "statusbar", "dialog", "popup", "title", "text", "tasks", "task", "completed", "due", "colors", "color", "card", "cards", "column", "columns", "details", "status", "tooltip", "hover", "add", "plus", "+", "new", "delete", "save", "cancel", "move", "import", "export", "check", "checklist", "checkbox", "tick", "tickbox", "ticks", "checkmark")) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _styleSectionHasVisibleAttribute = false;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🎨 Board, Notes, Buttons & UI Colors", EditorStyles.boldLabel, GUILayout.Width(250));
                    GUILayout.FlexibleSpace();
                    if (ThemedTooltip.Button("Reset All Theme Colors", "Reset all workspace and element colors to the default palette values", GUI.skin.button, GUILayout.Width(160)))
                    {
                        var def = ThemeData.CreateDefault();
                        theme.tabActive = def.tabActive;
                        theme.noteSelectedAccent = def.noteSelectedAccent;
                        theme.linkColor = def.linkColor;

                        theme.pro_BoardHeader = def.pro_BoardHeader;
                        theme.pro_ColumnHeader = def.pro_ColumnHeader;
                        theme.pro_CardTitle = def.pro_CardTitle;
                        theme.pro_CardText = def.pro_CardText;
                        theme.pro_SectionLabel = def.pro_SectionLabel;
                        theme.pro_BoardBg = def.pro_BoardBg;
                        theme.pro_TopBarBg = def.pro_TopBarBg;
                        theme.pro_StatusBarBg = def.pro_StatusBarBg;
                        theme.pro_StatusBarText = def.pro_StatusBarText;
                        theme.pro_NoteSidebarBg = def.pro_NoteSidebarBg;
                        theme.pro_NoteEditorBg = def.pro_NoteEditorBg;
                        theme.pro_NotePopoutBg = def.pro_NotePopoutBg;
                        theme.pro_NoteInputBg = def.pro_NoteInputBg;
                        theme.pro_NoteInputText = def.pro_NoteInputText;
                        theme.pro_NoteTitle = def.pro_NoteTitle;
                        theme.pro_CardDetailBg = def.pro_CardDetailBg;
                        theme.pro_ButtonBg = def.pro_ButtonBg;
                        theme.pro_ButtonText = def.pro_ButtonText;
                        theme.pro_ButtonHoverBg = def.pro_ButtonHoverBg;
                        theme.pro_ButtonHoverText = def.pro_ButtonHoverText;
                        theme.pro_DropdownBg = def.pro_DropdownBg;
                        theme.pro_DropdownText = def.pro_DropdownText;
                        theme.pro_DropdownHoverBg = def.pro_DropdownHoverBg;
                        theme.pro_DropdownHoverText = def.pro_DropdownHoverText;
                        theme.pro_DropdownMenuBg = def.pro_DropdownMenuBg;
                        theme.pro_DropdownMenuText = def.pro_DropdownMenuText;
                        theme.pro_DropdownMenuHoverBg = def.pro_DropdownMenuHoverBg;
                        theme.pro_DropdownMenuHoverText = def.pro_DropdownMenuHoverText;
                        theme.pro_PopupBg = def.pro_PopupBg;
                        theme.pro_DeleteBtnBg = def.pro_DeleteBtnBg;
                        theme.pro_DeleteBtnText = def.pro_DeleteBtnText;
                        theme.pro_DeleteBtnHoverBg = def.pro_DeleteBtnHoverBg;
                        theme.pro_HeaderTabActiveBg = def.pro_HeaderTabActiveBg;
                        theme.pro_HeaderTabActiveText = def.pro_HeaderTabActiveText;
                        theme.pro_HeaderTabInactiveBg = def.pro_HeaderTabInactiveBg;
                        theme.pro_HeaderTabInactiveText = def.pro_HeaderTabInactiveText;
                        theme.pro_HeaderTabHoverBg = def.pro_HeaderTabHoverBg;
                        theme.pro_AddCardBg = def.pro_AddCardBg;
                        theme.pro_AddCardText = def.pro_AddCardText;
                        theme.pro_AddCardHoverBg = def.pro_AddCardHoverBg;
                        theme.pro_NoteCardBg = def.pro_NoteCardBg;
                        theme.pro_NoteCardSelectedBg = def.pro_NoteCardSelectedBg;
                        theme.pro_NoteCardHoverBg = def.pro_NoteCardHoverBg;
                        theme.pro_NoteActionBg = def.pro_NoteActionBg;
                        theme.pro_NoteActionText = def.pro_NoteActionText;
                        theme.pro_NoteActionHoverBg = def.pro_NoteActionHoverBg;
                        theme.pro_NoteActionHoverText = def.pro_NoteActionHoverText;
                        theme.pro_AddNoteBg = def.pro_AddNoteBg;
                        theme.pro_AddNoteText = def.pro_AddNoteText;
                        theme.pro_AddNoteHoverBg = def.pro_AddNoteHoverBg;
                        theme.pro_AddNoteHoverText = def.pro_AddNoteHoverText;
                        theme.pro_ImportNoteBg = def.pro_ImportNoteBg;
                        theme.pro_ImportNoteText = def.pro_ImportNoteText;
                        theme.pro_ImportNoteHoverBg = def.pro_ImportNoteHoverBg;
                        theme.pro_ImportNoteHoverText = def.pro_ImportNoteHoverText;
                        theme.pro_NoteFolderText = def.pro_NoteFolderText;
                        theme.pro_CardDetailsText = def.pro_CardDetailsText;
                        theme.pro_CardTasksText = def.pro_CardTasksText;
                        theme.pro_CardCategoryTag = def.pro_CardCategoryTag;
                        theme.pro_AssigneeAvatarBg = def.pro_AssigneeAvatarBg;
                        theme.pro_ChecklistTickBg = def.pro_ChecklistTickBg;
                        theme.pro_ChecklistTickCheckedBg = def.pro_ChecklistTickCheckedBg;
                        theme.pro_ChecklistTickBorder = def.pro_ChecklistTickBorder;
                        theme.pro_ChecklistTickColor = def.pro_ChecklistTickColor;
                        theme.pro_StatusOverdue = def.pro_StatusOverdue;
                        theme.pro_StatusDueToday = def.pro_StatusDueToday;
                        theme.pro_StatusDueSoon = def.pro_StatusDueSoon;
                        theme.pro_StatusCompleted = def.pro_StatusCompleted;
                        theme.pro_TasksCompletedCount = def.pro_TasksCompletedCount;
                        theme.pro_TooltipBg = def.pro_TooltipBg;
                        theme.pro_TooltipText = def.pro_TooltipText;
                        theme.pro_TooltipBorder = def.pro_TooltipBorder;
                        theme.pro_ColumnBg = def.pro_ColumnBg;
                        theme.pro_ColumnBgAlt = def.pro_ColumnBgAlt;
                        theme.pro_CardBg = def.pro_CardBg;
                        theme.pro_CardHighlighted = def.pro_CardHighlighted;

                        theme.personal_BoardHeader = def.personal_BoardHeader;
                        theme.personal_ColumnHeader = def.personal_ColumnHeader;
                        theme.personal_CardTitle = def.personal_CardTitle;
                        theme.personal_CardText = def.personal_CardText;
                        theme.personal_SectionLabel = def.personal_SectionLabel;
                        theme.personal_BoardBg = def.personal_BoardBg;
                        theme.personal_TopBarBg = def.personal_TopBarBg;
                        theme.personal_StatusBarBg = def.personal_StatusBarBg;
                        theme.personal_StatusBarText = def.personal_StatusBarText;
                        theme.personal_NoteSidebarBg = def.personal_NoteSidebarBg;
                        theme.personal_NoteEditorBg = def.personal_NoteEditorBg;
                        theme.personal_NotePopoutBg = def.personal_NotePopoutBg;
                        theme.personal_NoteInputBg = def.personal_NoteInputBg;
                        theme.personal_NoteInputText = def.personal_NoteInputText;
                        theme.personal_NoteTitle = def.personal_NoteTitle;
                        theme.personal_CardDetailBg = def.personal_CardDetailBg;
                        theme.personal_ButtonBg = def.personal_ButtonBg;
                        theme.personal_ButtonText = def.personal_ButtonText;
                        theme.personal_ButtonHoverBg = def.personal_ButtonHoverBg;
                        theme.personal_ButtonHoverText = def.personal_ButtonHoverText;
                        theme.personal_DropdownBg = def.personal_DropdownBg;
                        theme.personal_DropdownText = def.personal_DropdownText;
                        theme.personal_DropdownHoverBg = def.personal_DropdownHoverBg;
                        theme.personal_DropdownHoverText = def.personal_DropdownHoverText;
                        theme.personal_DropdownMenuBg = def.personal_DropdownMenuBg;
                        theme.personal_DropdownMenuText = def.personal_DropdownMenuText;
                        theme.personal_DropdownMenuHoverBg = def.personal_DropdownMenuHoverBg;
                        theme.personal_DropdownMenuHoverText = def.personal_DropdownMenuHoverText;
                        theme.personal_PopupBg = def.personal_PopupBg;
                        theme.personal_DeleteBtnBg = def.personal_DeleteBtnBg;
                        theme.personal_DeleteBtnText = def.personal_DeleteBtnText;
                        theme.personal_DeleteBtnHoverBg = def.personal_DeleteBtnHoverBg;
                        theme.personal_HeaderTabActiveBg = def.personal_HeaderTabActiveBg;
                        theme.personal_HeaderTabActiveText = def.personal_HeaderTabActiveText;
                        theme.personal_HeaderTabInactiveBg = def.personal_HeaderTabInactiveBg;
                        theme.personal_HeaderTabInactiveText = def.personal_HeaderTabInactiveText;
                        theme.personal_HeaderTabHoverBg = def.personal_HeaderTabHoverBg;
                        theme.personal_AddCardBg = def.personal_AddCardBg;
                        theme.personal_AddCardText = def.personal_AddCardText;
                        theme.personal_AddCardHoverBg = def.personal_AddCardHoverBg;
                        theme.personal_NoteCardBg = def.personal_NoteCardBg;
                        theme.personal_NoteCardSelectedBg = def.personal_NoteCardSelectedBg;
                        theme.personal_NoteCardHoverBg = def.personal_NoteCardHoverBg;
                        theme.personal_NoteActionBg = def.personal_NoteActionBg;
                        theme.personal_NoteActionText = def.personal_NoteActionText;
                        theme.personal_NoteActionHoverBg = def.personal_NoteActionHoverBg;
                        theme.personal_NoteActionHoverText = def.personal_NoteActionHoverText;
                        theme.personal_AddNoteBg = def.personal_AddNoteBg;
                        theme.personal_AddNoteText = def.personal_AddNoteText;
                        theme.personal_AddNoteHoverBg = def.personal_AddNoteHoverBg;
                        theme.personal_AddNoteHoverText = def.personal_AddNoteHoverText;
                        theme.personal_ImportNoteBg = def.personal_ImportNoteBg;
                        theme.personal_ImportNoteText = def.personal_ImportNoteText;
                        theme.personal_ImportNoteHoverBg = def.personal_ImportNoteHoverBg;
                        theme.personal_ImportNoteHoverText = def.personal_ImportNoteHoverText;
                        theme.personal_NoteFolderText = def.personal_NoteFolderText;
                        theme.personal_CardDetailsText = def.personal_CardDetailsText;
                        theme.personal_CardTasksText = def.personal_CardTasksText;
                        theme.personal_CardCategoryTag = def.personal_CardCategoryTag;
                        theme.personal_AssigneeAvatarBg = def.personal_AssigneeAvatarBg;
                        theme.personal_ChecklistTickBg = def.personal_ChecklistTickBg;
                        theme.personal_ChecklistTickCheckedBg = def.personal_ChecklistTickCheckedBg;
                        theme.personal_ChecklistTickBorder = def.personal_ChecklistTickBorder;
                        theme.personal_ChecklistTickColor = def.personal_ChecklistTickColor;
                        theme.personal_StatusOverdue = def.personal_StatusOverdue;
                        theme.personal_StatusDueToday = def.personal_StatusDueToday;
                        theme.personal_StatusDueSoon = def.personal_StatusDueSoon;
                        theme.personal_StatusCompleted = def.personal_StatusCompleted;
                        theme.personal_TasksCompletedCount = def.personal_TasksCompletedCount;
                        theme.personal_TooltipBg = def.personal_TooltipBg;
                        theme.personal_TooltipText = def.personal_TooltipText;
                        theme.personal_TooltipBorder = def.personal_TooltipBorder;
                        theme.personal_ColumnBg = def.personal_ColumnBg;
                        theme.personal_ColumnBgAlt = def.personal_ColumnBgAlt;
                        theme.personal_CardBg = def.personal_CardBg;
                        theme.personal_CardHighlighted = def.personal_CardHighlighted;
                        theme.checklistTickStyle = def.checklistTickStyle;
                        theme.customChecklistTickChar = def.customChecklistTickChar;
                    }
                }

                EditorGUILayout.HelpBox("Configure tab header buttons, standard buttons, dropdowns, add card buttons, notes backgrounds & cards, card details, and workspace tones.", MessageType.None);
                GUILayout.Space(4);

                string darkMode = "Dark mode (Pro Skin)";
                string lightMode = "Light mode (Personal Skin)";

                EditorGUILayout.LabelField("Accent & Global Colors", EditorStyles.boldLabel);
                theme.tabActive = DrawThemeColorOption("Active Tab Highlight", theme.tabActive, BuildThemeOptionTooltip("Global", "Color used to highlight the currently selected top navigation tab"), 160f, 70f);
                theme.noteSelectedAccent = DrawThemeColorOption("Note Selection Accent", theme.noteSelectedAccent, BuildThemeOptionTooltip("Global", "Accent strip color used for the selected note card and note preview marker"), 160f, 70f);
                theme.linkColor = DrawThemeColorOption("Link / Reference Text", theme.linkColor, BuildThemeOptionTooltip("Global", "Text color used for clickable links and reference URLs"), 160f, 70f);

                GUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Dark Theme (Unity Pro Skin)
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Dark Mode (Pro Skin)", EditorStyles.boldLabel);
                        GUILayout.Space(2);

                        // Header / Tabs
                        EditorGUILayout.LabelField("Top Header & Tabs", EditorStyles.miniBoldLabel);
                        theme.pro_HeaderTabActiveBg = DrawThemeColorOption("Tab Active Background", theme.pro_HeaderTabActiveBg, BuildThemeOptionTooltip(darkMode, "Background color of the active top tab"), 140f, 60f);
                        theme.pro_HeaderTabActiveText = DrawThemeColorOption("Tab Active Text", theme.pro_HeaderTabActiveText, BuildThemeOptionTooltip(darkMode, "Text color of the active top tab"), 140f, 60f);
                        theme.pro_HeaderTabInactiveBg = DrawThemeColorOption("Tab Inactive Background", theme.pro_HeaderTabInactiveBg, BuildThemeOptionTooltip(darkMode, "Background color of inactive top tabs"), 140f, 60f);
                        theme.pro_HeaderTabInactiveText = DrawThemeColorOption("Tab Inactive Text", theme.pro_HeaderTabInactiveText, BuildThemeOptionTooltip(darkMode, "Text color of inactive top tabs"), 140f, 60f);
                        theme.pro_HeaderTabHoverBg = DrawThemeColorOption("Tab Hover Background", theme.pro_HeaderTabHoverBg, BuildThemeOptionTooltip(darkMode, "Background color shown when hovering top tabs"), 140f, 60f);

                        GUILayout.Space(4);

                        // Buttons & Hover
                        EditorGUILayout.LabelField("Buttons & Hover", EditorStyles.miniBoldLabel);
                        theme.pro_ButtonBg = DrawThemeColorOption("Button Background", theme.pro_ButtonBg, BuildThemeOptionTooltip(darkMode, "Background color for standard buttons"), 140f, 60f);
                        theme.pro_ButtonText = DrawThemeColorOption("Button Text Color", theme.pro_ButtonText, BuildThemeOptionTooltip(darkMode, "Text color for standard buttons"), 140f, 60f);
                        theme.pro_ButtonHoverBg = DrawThemeColorOption("Button Hover Background", theme.pro_ButtonHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when standard buttons are hovered"), 140f, 60f);
                        theme.pro_ButtonHoverText = DrawThemeColorOption("Button Hover Text", theme.pro_ButtonHoverText, BuildThemeOptionTooltip(darkMode, "Text color when standard buttons are hovered"), 140f, 60f);

                        GUILayout.Space(4);

                        // Dropdowns & Hover
                        EditorGUILayout.LabelField("Dropdowns & Hover", EditorStyles.miniBoldLabel);
                        theme.pro_DropdownBg = DrawThemeColorOption("Dropdown Background", theme.pro_DropdownBg, BuildThemeOptionTooltip(darkMode, "Background color of closed dropdown controls"), 140f, 60f);
                        theme.pro_DropdownText = DrawThemeColorOption("Dropdown Text Color", theme.pro_DropdownText, BuildThemeOptionTooltip(darkMode, "Text color of closed dropdown controls"), 140f, 60f);
                        theme.pro_DropdownHoverBg = DrawThemeColorOption("Dropdown Hover Bg", theme.pro_DropdownHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering dropdown controls"), 140f, 60f);
                        theme.pro_DropdownHoverText = DrawThemeColorOption("Dropdown Hover Text", theme.pro_DropdownHoverText, BuildThemeOptionTooltip(darkMode, "Text color when hovering dropdown controls"), 140f, 60f);

                        GUILayout.Space(4);

                        // Dropdown Menu (Options Popup)
                        EditorGUILayout.LabelField("Dropdown Menu (Options List)", EditorStyles.miniBoldLabel);
                        theme.pro_DropdownMenuBg = DrawThemeColorOption("Menu Background", theme.pro_DropdownMenuBg, BuildThemeOptionTooltip(darkMode, "Background color of the opened dropdown option list"), 140f, 60f);
                        theme.pro_DropdownMenuText = DrawThemeColorOption("Menu Text Color", theme.pro_DropdownMenuText, BuildThemeOptionTooltip(darkMode, "Text color of options in opened dropdown menus"), 140f, 60f);
                        theme.pro_DropdownMenuHoverBg = DrawThemeColorOption("Menu Hover / Select Bg", theme.pro_DropdownMenuHoverBg, BuildThemeOptionTooltip(darkMode, "Background color of hovered or selected dropdown options"), 140f, 60f);
                        theme.pro_DropdownMenuHoverText = DrawThemeColorOption("Menu Hover / Select Text", theme.pro_DropdownMenuHoverText, BuildThemeOptionTooltip(darkMode, "Text color of hovered or selected dropdown options"), 140f, 60f);

                        GUILayout.Space(4);

                        // Delete Buttons & Actions
                        EditorGUILayout.LabelField("Delete Buttons & Actions", EditorStyles.miniBoldLabel);
                        theme.pro_DeleteBtnBg = DrawThemeColorOption("Delete Button Background", theme.pro_DeleteBtnBg, BuildThemeOptionTooltip(darkMode, "Background color for delete and destructive action buttons"), 140f, 60f);
                        theme.pro_DeleteBtnText = DrawThemeColorOption("Delete Button Text Color", theme.pro_DeleteBtnText, BuildThemeOptionTooltip(darkMode, "Text color for delete and destructive action buttons"), 140f, 60f);
                        theme.pro_DeleteBtnHoverBg = DrawThemeColorOption("Delete Button Hover Bg", theme.pro_DeleteBtnHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering delete buttons"), 140f, 60f);

                        GUILayout.Space(4);

                        // Add Card Button
                        EditorGUILayout.LabelField("Add Card Button", EditorStyles.miniBoldLabel);
                        theme.pro_AddCardBg = DrawThemeColorOption("Add Card Background", theme.pro_AddCardBg, BuildThemeOptionTooltip(darkMode, "Background color for the '+ Add Card' button in columns"), 140f, 60f);
                        theme.pro_AddCardText = DrawThemeColorOption("Add Card Text Color", theme.pro_AddCardText, BuildThemeOptionTooltip(darkMode, "Text color for the '+ Add Card' button"), 140f, 60f);
                        theme.pro_AddCardHoverBg = DrawThemeColorOption("Add Card Hover Bg", theme.pro_AddCardHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering the '+ Add Card' button"), 140f, 60f);

                        GUILayout.Space(4);

                        // Notes View Backgrounds & Cards
                        EditorGUILayout.LabelField("Notes View & Cards", EditorStyles.miniBoldLabel);
                        theme.pro_NoteSidebarBg = DrawThemeColorOption("Note Sidebar Bg", theme.pro_NoteSidebarBg, BuildThemeOptionTooltip(darkMode, "Background color of the note list sidebar"), 140f, 60f);
                        theme.pro_NoteEditorBg = DrawThemeColorOption("Note Editor Bg", theme.pro_NoteEditorBg, BuildThemeOptionTooltip(darkMode, "Background color of the note editor panel"), 140f, 60f);
                        theme.pro_NotePopoutBg = DrawThemeColorOption("Note Popout Bg", theme.pro_NotePopoutBg, BuildThemeOptionTooltip(darkMode, "Background color of detached or popout note windows"), 140f, 60f);
                        theme.pro_NoteInputBg = DrawThemeColorOption("Note Text Input Bg", theme.pro_NoteInputBg, BuildThemeOptionTooltip(darkMode, "Background color of note text input fields"), 140f, 60f);
                        theme.pro_NoteInputText = DrawThemeColorOption("Note Text Color", theme.pro_NoteInputText, BuildThemeOptionTooltip(darkMode, "Text color used inside note input fields"), 140f, 60f);
                        theme.pro_NoteTitle = DrawThemeColorOption("Note Title Text", theme.pro_NoteTitle, BuildThemeOptionTooltip(darkMode, "Text color of note titles"), 140f, 60f);
                        theme.pro_NoteCardBg = DrawThemeColorOption("Note Card Background", theme.pro_NoteCardBg, BuildThemeOptionTooltip(darkMode, "Background color of note list cards"), 140f, 60f);
                        theme.pro_NoteCardSelectedBg = DrawThemeColorOption("Note Card Selected", theme.pro_NoteCardSelectedBg, BuildThemeOptionTooltip(darkMode, "Background color of the selected note card"), 140f, 60f);
                        theme.pro_NoteCardHoverBg = DrawThemeColorOption("Note Card Hover", theme.pro_NoteCardHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering note cards"), 140f, 60f);
                        theme.pro_NoteActionBg = DrawThemeColorOption("Note Action Button Bg", theme.pro_NoteActionBg, BuildThemeOptionTooltip(darkMode, "Background color of action buttons in notes"), 140f, 60f);
                        theme.pro_NoteActionText = DrawThemeColorOption("Note Action Text Color", theme.pro_NoteActionText, BuildThemeOptionTooltip(darkMode, "Text color of action buttons in notes"), 140f, 60f);
                        theme.pro_NoteActionHoverBg = DrawThemeColorOption("Note Action Hover Bg", theme.pro_NoteActionHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering note action buttons"), 140f, 60f);
                        theme.pro_NoteActionHoverText = DrawThemeColorOption("Note Action Hover Text", theme.pro_NoteActionHoverText, BuildThemeOptionTooltip(darkMode, "Text color when hovering note action buttons"), 140f, 60f);
                        theme.pro_NoteFolderText = DrawThemeColorOption("Note Folders Text", theme.pro_NoteFolderText, BuildThemeOptionTooltip(darkMode, "Text color of note folder names"), 140f, 60f);
                        theme.pro_AddNoteBg = DrawThemeColorOption("+ Note Button Bg", theme.pro_AddNoteBg, BuildThemeOptionTooltip(darkMode, "Background color of the add note button"), 140f, 60f);
                        theme.pro_AddNoteText = DrawThemeColorOption("+ Note Text Color", theme.pro_AddNoteText, BuildThemeOptionTooltip(darkMode, "Text color of the add note button"), 140f, 60f);
                        theme.pro_AddNoteHoverBg = DrawThemeColorOption("+ Note Hover Bg", theme.pro_AddNoteHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering the add note button"), 140f, 60f);
                        theme.pro_AddNoteHoverText = DrawThemeColorOption("+ Note Hover Text", theme.pro_AddNoteHoverText, BuildThemeOptionTooltip(darkMode, "Text color when hovering the add note button"), 140f, 60f);
                        theme.pro_ImportNoteBg = DrawThemeColorOption("Import Note Button Bg", theme.pro_ImportNoteBg, BuildThemeOptionTooltip(darkMode, "Background color of the note import button"), 140f, 60f);
                        theme.pro_ImportNoteText = DrawThemeColorOption("Import Note Text Color", theme.pro_ImportNoteText, BuildThemeOptionTooltip(darkMode, "Text color of the note import button"), 140f, 60f);
                        theme.pro_ImportNoteHoverBg = DrawThemeColorOption("Import Note Hover Bg", theme.pro_ImportNoteHoverBg, BuildThemeOptionTooltip(darkMode, "Background color when hovering the note import button"), 140f, 60f);
                        theme.pro_ImportNoteHoverText = DrawThemeColorOption("Import Note Hover Text", theme.pro_ImportNoteHoverText, BuildThemeOptionTooltip(darkMode, "Text color when hovering the note import button"), 140f, 60f);

                        GUILayout.Space(4);

                        // Card Details & Board
                        EditorGUILayout.LabelField("Board, Columns, Popups & Details", EditorStyles.miniBoldLabel);
                        theme.pro_PopupBg = DrawThemeColorOption("Popup Dialog Bg", theme.pro_PopupBg, BuildThemeOptionTooltip(darkMode, "Background color of popup dialogs and context windows"), 140f, 60f);
                        theme.pro_CardDetailBg = DrawThemeColorOption("Card Details Bg", theme.pro_CardDetailBg, BuildThemeOptionTooltip(darkMode, "Background color of the card details panel"), 140f, 60f);
                        theme.pro_BoardBg = DrawThemeColorOption("Board Background", theme.pro_BoardBg, BuildThemeOptionTooltip(darkMode, "Background color of the task board canvas"), 140f, 60f);
                        theme.pro_TopBarBg = DrawThemeColorOption("Top Bar Background", theme.pro_TopBarBg, BuildThemeOptionTooltip(darkMode, "Background color of the top toolbar"), 140f, 60f);
                        theme.pro_StatusBarBg = DrawThemeColorOption("Status Bar Background", theme.pro_StatusBarBg, BuildThemeOptionTooltip(darkMode, "Background color of the bottom status bar"), 140f, 60f);
                        theme.pro_StatusBarText = DrawThemeColorOption("Status Bar Text", theme.pro_StatusBarText, BuildThemeOptionTooltip(darkMode, "Text color of the bottom status bar"), 140f, 60f);
                        theme.pro_BoardHeader = DrawThemeColorOption("Board Header Text", theme.pro_BoardHeader, BuildThemeOptionTooltip(darkMode, "Text color of board-level headers"), 140f, 60f);
                        theme.pro_ColumnHeader = DrawThemeColorOption("Column Header Text", theme.pro_ColumnHeader, BuildThemeOptionTooltip(darkMode, "Text color of column headers"), 140f, 60f);
                        theme.pro_CardTitle = DrawThemeColorOption("Card Title Text", theme.pro_CardTitle, BuildThemeOptionTooltip(darkMode, "Text color of card titles"), 140f, 60f);
                        theme.pro_CardText = DrawThemeColorOption("Card Text / Badges", theme.pro_CardText, BuildThemeOptionTooltip(darkMode, "Text color of card body text and badge labels"), 140f, 60f);
                        theme.pro_CardDetailsText = DrawThemeColorOption("Card Details Text", theme.pro_CardDetailsText, BuildThemeOptionTooltip(darkMode, "Text color in the card details view"), 140f, 60f);
                        theme.pro_CardTasksText = DrawThemeColorOption("Card Tasks Text", theme.pro_CardTasksText, BuildThemeOptionTooltip(darkMode, "Text color of checklist and task list content"), 140f, 60f);
                        theme.pro_CardCategoryTag = DrawThemeColorOption("Card Category Tag", theme.pro_CardCategoryTag, BuildThemeOptionTooltip(darkMode, "Text color of category tags shown on cards"), 140f, 60f);
                        theme.pro_AssigneeAvatarBg = DrawThemeColorOption("Assignee Picture Bg", theme.pro_AssigneeAvatarBg, BuildThemeOptionTooltip(darkMode, "Background color behind member avatar pictures"), 140f, 60f);
                        theme.pro_StatusOverdue = DrawThemeColorOption("Status Overdue", theme.pro_StatusOverdue, BuildThemeOptionTooltip(darkMode, "Text/accent color for overdue status indicators"), 140f, 60f);
                        theme.pro_StatusDueToday = DrawThemeColorOption("Status Due Today", theme.pro_StatusDueToday, BuildThemeOptionTooltip(darkMode, "Text/accent color for due-today status indicators"), 140f, 60f);
                        theme.pro_StatusDueSoon = DrawThemeColorOption("Status Due Soon", theme.pro_StatusDueSoon, BuildThemeOptionTooltip(darkMode, "Text/accent color for due-soon status indicators"), 140f, 60f);
                        theme.pro_StatusCompleted = DrawThemeColorOption("Status Completed", theme.pro_StatusCompleted, BuildThemeOptionTooltip(darkMode, "Text/accent color for completed status indicators"), 140f, 60f);
                        theme.pro_TasksCompletedCount = DrawThemeColorOption("Tasks Completed Count", theme.pro_TasksCompletedCount, BuildThemeOptionTooltip(darkMode, "Text color of checklist completion counters"), 140f, 60f);
                        theme.pro_SectionLabel = DrawThemeColorOption("Section Label Text", theme.pro_SectionLabel, BuildThemeOptionTooltip(darkMode, "Text color of section labels and minor headings"), 140f, 60f);
                        theme.pro_ColumnBg = DrawThemeColorOption("Column Background", theme.pro_ColumnBg, BuildThemeOptionTooltip(darkMode, "Background color of task columns"), 140f, 60f);
                        theme.pro_ColumnBgAlt = DrawThemeColorOption("Column Background Alt", theme.pro_ColumnBgAlt, BuildThemeOptionTooltip(darkMode, "Alternate/background blend color used for column depth"), 140f, 60f);
                        theme.pro_CardBg = DrawThemeColorOption("Card Background", theme.pro_CardBg, BuildThemeOptionTooltip(darkMode, "Background color of task cards"), 140f, 60f);
                        theme.pro_CardHighlighted = DrawThemeColorOption("Card Highlight / Hover", theme.pro_CardHighlighted, BuildThemeOptionTooltip(darkMode, "Highlight color shown when cards are selected or hovered"), 140f, 60f);

                        GUILayout.Space(4);

                        // Tooltip & Hover Popup
                        EditorGUILayout.LabelField("Tooltip & Truncated Text Hover", EditorStyles.miniBoldLabel);
                        theme.pro_TooltipBg = DrawThemeColorOption("Tooltip Background", theme.pro_TooltipBg, BuildThemeOptionTooltip(darkMode, "Background color of custom themed tooltips"), 140f, 60f);
                        theme.pro_TooltipText = DrawThemeColorOption("Tooltip Text Color", theme.pro_TooltipText, BuildThemeOptionTooltip(darkMode, "Text color of custom themed tooltips"), 140f, 60f);
                        theme.pro_TooltipBorder = DrawThemeColorOption("Tooltip Border Color", theme.pro_TooltipBorder, BuildThemeOptionTooltip(darkMode, "Border color of custom themed tooltips"), 140f, 60f);

                        GUILayout.Space(4);

                        // Checklist Tick Boxes
                        EditorGUILayout.LabelField("Checklist Tick Boxes", EditorStyles.miniBoldLabel);
                        theme.pro_ChecklistTickBg = DrawThemeColorOption("Tick Box Background", theme.pro_ChecklistTickBg, BuildThemeOptionTooltip(darkMode, "Background color of unchecked checklist tick boxes"), 140f, 60f);
                        theme.pro_ChecklistTickCheckedBg = DrawThemeColorOption("Tick Box Checked Bg", theme.pro_ChecklistTickCheckedBg, BuildThemeOptionTooltip(darkMode, "Background fill color of checked checklist tick boxes"), 140f, 60f);
                        theme.pro_ChecklistTickBorder = DrawThemeColorOption("Tick Box Border Color", theme.pro_ChecklistTickBorder, BuildThemeOptionTooltip(darkMode, "Border outline color of checklist tick boxes"), 140f, 60f);
                        theme.pro_ChecklistTickColor = DrawThemeColorOption("Checkmark Icon Color", theme.pro_ChecklistTickColor, BuildThemeOptionTooltip(darkMode, "Color of the checkmark icon inside checked tick boxes"), 140f, 60f);
                        DrawStyleChecklistTickIconOption(theme, isDarkMode: true);
                    }

                    GUILayout.Space(8);

                    // Light Theme (Unity Personal Skin)
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Light Mode (Personal Skin)", EditorStyles.boldLabel);
                        GUILayout.Space(2);

                        // Header & Tabs
                        EditorGUILayout.LabelField("Top Header & Tabs", EditorStyles.miniBoldLabel);
                        theme.personal_HeaderTabActiveBg = DrawThemeColorOption("Tab Active Background", theme.personal_HeaderTabActiveBg, BuildThemeOptionTooltip(lightMode, "Background color of the active top tab"), 140f, 60f);
                        theme.personal_HeaderTabActiveText = DrawThemeColorOption("Tab Active Text", theme.personal_HeaderTabActiveText, BuildThemeOptionTooltip(lightMode, "Text color of the active top tab"), 140f, 60f);
                        theme.personal_HeaderTabInactiveBg = DrawThemeColorOption("Tab Inactive Background", theme.personal_HeaderTabInactiveBg, BuildThemeOptionTooltip(lightMode, "Background color of inactive top tabs"), 140f, 60f);
                        theme.personal_HeaderTabInactiveText = DrawThemeColorOption("Tab Inactive Text", theme.personal_HeaderTabInactiveText, BuildThemeOptionTooltip(lightMode, "Text color of inactive top tabs"), 140f, 60f);
                        theme.personal_HeaderTabHoverBg = DrawThemeColorOption("Tab Hover Background", theme.personal_HeaderTabHoverBg, BuildThemeOptionTooltip(lightMode, "Background color shown when hovering top tabs"), 140f, 60f);

                        GUILayout.Space(4);

                        // Buttons & Hover
                        EditorGUILayout.LabelField("Buttons & Hover", EditorStyles.miniBoldLabel);
                        theme.personal_ButtonBg = DrawThemeColorOption("Button Background", theme.personal_ButtonBg, BuildThemeOptionTooltip(lightMode, "Background color for standard buttons"), 140f, 60f);
                        theme.personal_ButtonText = DrawThemeColorOption("Button Text Color", theme.personal_ButtonText, BuildThemeOptionTooltip(lightMode, "Text color for standard buttons"), 140f, 60f);
                        theme.personal_ButtonHoverBg = DrawThemeColorOption("Button Hover Background", theme.personal_ButtonHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when standard buttons are hovered"), 140f, 60f);
                        theme.personal_ButtonHoverText = DrawThemeColorOption("Button Hover Text", theme.personal_ButtonHoverText, BuildThemeOptionTooltip(lightMode, "Text color when standard buttons are hovered"), 140f, 60f);

                        GUILayout.Space(4);

                        // Dropdowns & Hover
                        EditorGUILayout.LabelField("Dropdowns & Hover", EditorStyles.miniBoldLabel);
                        theme.personal_DropdownBg = DrawThemeColorOption("Dropdown Background", theme.personal_DropdownBg, BuildThemeOptionTooltip(lightMode, "Background color of closed dropdown controls"), 140f, 60f);
                        theme.personal_DropdownText = DrawThemeColorOption("Dropdown Text Color", theme.personal_DropdownText, BuildThemeOptionTooltip(lightMode, "Text color of closed dropdown controls"), 140f, 60f);
                        theme.personal_DropdownHoverBg = DrawThemeColorOption("Dropdown Hover Bg", theme.personal_DropdownHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering dropdown controls"), 140f, 60f);
                        theme.personal_DropdownHoverText = DrawThemeColorOption("Dropdown Hover Text", theme.personal_DropdownHoverText, BuildThemeOptionTooltip(lightMode, "Text color when hovering dropdown controls"), 140f, 60f);

                        GUILayout.Space(4);

                        // Dropdown Menu (Options Popup)
                        EditorGUILayout.LabelField("Dropdown Menu (Options List)", EditorStyles.miniBoldLabel);
                        theme.personal_DropdownMenuBg = DrawThemeColorOption("Menu Background", theme.personal_DropdownMenuBg, BuildThemeOptionTooltip(lightMode, "Background color of the opened dropdown option list"), 140f, 60f);
                        theme.personal_DropdownMenuText = DrawThemeColorOption("Menu Text Color", theme.personal_DropdownMenuText, BuildThemeOptionTooltip(lightMode, "Text color of options in opened dropdown menus"), 140f, 60f);
                        theme.personal_DropdownMenuHoverBg = DrawThemeColorOption("Menu Hover / Select Bg", theme.personal_DropdownMenuHoverBg, BuildThemeOptionTooltip(lightMode, "Background color of hovered or selected dropdown options"), 140f, 60f);
                        theme.personal_DropdownMenuHoverText = DrawThemeColorOption("Menu Hover / Select Text", theme.personal_DropdownMenuHoverText, BuildThemeOptionTooltip(lightMode, "Text color of hovered or selected dropdown options"), 140f, 60f);

                        GUILayout.Space(4);

                        // Delete Buttons & Actions
                        EditorGUILayout.LabelField("Delete Buttons & Actions", EditorStyles.miniBoldLabel);
                        theme.personal_DeleteBtnBg = DrawThemeColorOption("Delete Button Background", theme.personal_DeleteBtnBg, BuildThemeOptionTooltip(lightMode, "Background color for delete and destructive action buttons"), 140f, 60f);
                        theme.personal_DeleteBtnText = DrawThemeColorOption("Delete Button Text Color", theme.personal_DeleteBtnText, BuildThemeOptionTooltip(lightMode, "Text color for delete and destructive action buttons"), 140f, 60f);
                        theme.personal_DeleteBtnHoverBg = DrawThemeColorOption("Delete Button Hover Bg", theme.personal_DeleteBtnHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering delete buttons"), 140f, 60f);

                        GUILayout.Space(4);

                        // Add Card Button
                        EditorGUILayout.LabelField("Add Card Button", EditorStyles.miniBoldLabel);
                        theme.personal_AddCardBg = DrawThemeColorOption("Add Card Background", theme.personal_AddCardBg, BuildThemeOptionTooltip(lightMode, "Background color for the '+ Add Card' button in columns"), 140f, 60f);
                        theme.personal_AddCardText = DrawThemeColorOption("Add Card Text Color", theme.personal_AddCardText, BuildThemeOptionTooltip(lightMode, "Text color for the '+ Add Card' button"), 140f, 60f);
                        theme.personal_AddCardHoverBg = DrawThemeColorOption("Add Card Hover Bg", theme.personal_AddCardHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering the '+ Add Card' button"), 140f, 60f);

                        GUILayout.Space(4);

                        // Notes View Backgrounds & Cards
                        EditorGUILayout.LabelField("Notes View & Cards", EditorStyles.miniBoldLabel);
                        theme.personal_NoteSidebarBg = DrawThemeColorOption("Note Sidebar Bg", theme.personal_NoteSidebarBg, BuildThemeOptionTooltip(lightMode, "Background color of the note list sidebar"), 140f, 60f);
                        theme.personal_NoteEditorBg = DrawThemeColorOption("Note Editor Bg", theme.personal_NoteEditorBg, BuildThemeOptionTooltip(lightMode, "Background color of the note editor panel"), 140f, 60f);
                        theme.personal_NotePopoutBg = DrawThemeColorOption("Note Popout Bg", theme.personal_NotePopoutBg, BuildThemeOptionTooltip(lightMode, "Background color of detached or popout note windows"), 140f, 60f);
                        theme.personal_NoteInputBg = DrawThemeColorOption("Note Text Input Bg", theme.personal_NoteInputBg, BuildThemeOptionTooltip(lightMode, "Background color of note text input fields"), 140f, 60f);
                        theme.personal_NoteInputText = DrawThemeColorOption("Note Text Color", theme.personal_NoteInputText, BuildThemeOptionTooltip(lightMode, "Text color used inside note input fields"), 140f, 60f);
                        theme.personal_NoteTitle = DrawThemeColorOption("Note Title Text", theme.personal_NoteTitle, BuildThemeOptionTooltip(lightMode, "Text color of note titles"), 140f, 60f);
                        theme.personal_NoteCardBg = DrawThemeColorOption("Note Card Background", theme.personal_NoteCardBg, BuildThemeOptionTooltip(lightMode, "Background color of note list cards"), 140f, 60f);
                        theme.personal_NoteCardSelectedBg = DrawThemeColorOption("Note Card Selected", theme.personal_NoteCardSelectedBg, BuildThemeOptionTooltip(lightMode, "Background color of the selected note card"), 140f, 60f);
                        theme.personal_NoteCardHoverBg = DrawThemeColorOption("Note Card Hover", theme.personal_NoteCardHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering note cards"), 140f, 60f);
                        theme.personal_NoteActionBg = DrawThemeColorOption("Note Action Button Bg", theme.personal_NoteActionBg, BuildThemeOptionTooltip(lightMode, "Background color of action buttons in notes"), 140f, 60f);
                        theme.personal_NoteActionText = DrawThemeColorOption("Note Action Text Color", theme.personal_NoteActionText, BuildThemeOptionTooltip(lightMode, "Text color of action buttons in notes"), 140f, 60f);
                        theme.personal_NoteActionHoverBg = DrawThemeColorOption("Note Action Hover Bg", theme.personal_NoteActionHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering note action buttons"), 140f, 60f);
                        theme.personal_NoteActionHoverText = DrawThemeColorOption("Note Action Hover Text", theme.personal_NoteActionHoverText, BuildThemeOptionTooltip(lightMode, "Text color when hovering note action buttons"), 140f, 60f);
                        theme.personal_NoteFolderText = DrawThemeColorOption("Note Folders Text", theme.personal_NoteFolderText, BuildThemeOptionTooltip(lightMode, "Text color of note folder names"), 140f, 60f);
                        theme.personal_AddNoteBg = DrawThemeColorOption("+ Note Button Bg", theme.personal_AddNoteBg, BuildThemeOptionTooltip(lightMode, "Background color of the add note button"), 140f, 60f);
                        theme.personal_AddNoteText = DrawThemeColorOption("+ Note Text Color", theme.personal_AddNoteText, BuildThemeOptionTooltip(lightMode, "Text color of the add note button"), 140f, 60f);
                        theme.personal_AddNoteHoverBg = DrawThemeColorOption("+ Note Hover Bg", theme.personal_AddNoteHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering the add note button"), 140f, 60f);
                        theme.personal_AddNoteHoverText = DrawThemeColorOption("+ Note Hover Text", theme.personal_AddNoteHoverText, BuildThemeOptionTooltip(lightMode, "Text color when hovering the add note button"), 140f, 60f);
                        theme.personal_ImportNoteBg = DrawThemeColorOption("Import Note Button Bg", theme.personal_ImportNoteBg, BuildThemeOptionTooltip(lightMode, "Background color of the note import button"), 140f, 60f);
                        theme.personal_ImportNoteText = DrawThemeColorOption("Import Note Text Color", theme.personal_ImportNoteText, BuildThemeOptionTooltip(lightMode, "Text color of the note import button"), 140f, 60f);
                        theme.personal_ImportNoteHoverBg = DrawThemeColorOption("Import Note Hover Bg", theme.personal_ImportNoteHoverBg, BuildThemeOptionTooltip(lightMode, "Background color when hovering the note import button"), 140f, 60f);
                        theme.personal_ImportNoteHoverText = DrawThemeColorOption("Import Note Hover Text", theme.personal_ImportNoteHoverText, BuildThemeOptionTooltip(lightMode, "Text color when hovering the note import button"), 140f, 60f);

                        GUILayout.Space(4);

                        // Card Details & Board
                        EditorGUILayout.LabelField("Board, Columns, Popups & Details", EditorStyles.miniBoldLabel);
                        theme.personal_PopupBg = DrawThemeColorOption("Popup Dialog Bg", theme.personal_PopupBg, BuildThemeOptionTooltip(lightMode, "Background color of popup dialogs and context windows"), 140f, 60f);
                        theme.personal_CardDetailBg = DrawThemeColorOption("Card Details Bg", theme.personal_CardDetailBg, BuildThemeOptionTooltip(lightMode, "Background color of the card details panel"), 140f, 60f);
                        theme.personal_BoardBg = DrawThemeColorOption("Board Background", theme.personal_BoardBg, BuildThemeOptionTooltip(lightMode, "Background color of the task board canvas"), 140f, 60f);
                        theme.personal_TopBarBg = DrawThemeColorOption("Top Bar Background", theme.personal_TopBarBg, BuildThemeOptionTooltip(lightMode, "Background color of the top toolbar"), 140f, 60f);
                        theme.personal_StatusBarBg = DrawThemeColorOption("Status Bar Background", theme.personal_StatusBarBg, BuildThemeOptionTooltip(lightMode, "Background color of the bottom status bar"), 140f, 60f);
                        theme.personal_StatusBarText = DrawThemeColorOption("Status Bar Text", theme.personal_StatusBarText, BuildThemeOptionTooltip(lightMode, "Text color of the bottom status bar"), 140f, 60f);
                        theme.personal_BoardHeader = DrawThemeColorOption("Board Header Text", theme.personal_BoardHeader, BuildThemeOptionTooltip(lightMode, "Text color of board-level headers"), 140f, 60f);
                        theme.personal_ColumnHeader = DrawThemeColorOption("Column Header Text", theme.personal_ColumnHeader, BuildThemeOptionTooltip(lightMode, "Text color of column headers"), 140f, 60f);
                        theme.personal_CardTitle = DrawThemeColorOption("Card Title Text", theme.personal_CardTitle, BuildThemeOptionTooltip(lightMode, "Text color of card titles"), 140f, 60f);
                        theme.personal_CardText = DrawThemeColorOption("Card Text / Badges", theme.personal_CardText, BuildThemeOptionTooltip(lightMode, "Text color of card body text and badge labels"), 140f, 60f);
                        theme.personal_CardDetailsText = DrawThemeColorOption("Card Details Text", theme.personal_CardDetailsText, BuildThemeOptionTooltip(lightMode, "Text color in the card details view"), 140f, 60f);
                        theme.personal_CardTasksText = DrawThemeColorOption("Card Tasks Text", theme.personal_CardTasksText, BuildThemeOptionTooltip(lightMode, "Text color of checklist and task list content"), 140f, 60f);
                        theme.personal_CardCategoryTag = DrawThemeColorOption("Card Category Tag", theme.personal_CardCategoryTag, BuildThemeOptionTooltip(lightMode, "Text color of category tags shown on cards"), 140f, 60f);
                        theme.personal_AssigneeAvatarBg = DrawThemeColorOption("Assignee Picture Bg", theme.personal_AssigneeAvatarBg, BuildThemeOptionTooltip(lightMode, "Background color behind member avatar pictures"), 140f, 60f);
                        theme.personal_StatusOverdue = DrawThemeColorOption("Status Overdue", theme.personal_StatusOverdue, BuildThemeOptionTooltip(lightMode, "Text/accent color for overdue status indicators"), 140f, 60f);
                        theme.personal_StatusDueToday = DrawThemeColorOption("Status Due Today", theme.personal_StatusDueToday, BuildThemeOptionTooltip(lightMode, "Text/accent color for due-today status indicators"), 140f, 60f);
                        theme.personal_StatusDueSoon = DrawThemeColorOption("Status Due Soon", theme.personal_StatusDueSoon, BuildThemeOptionTooltip(lightMode, "Text/accent color for due-soon status indicators"), 140f, 60f);
                        theme.personal_StatusCompleted = DrawThemeColorOption("Status Completed", theme.personal_StatusCompleted, BuildThemeOptionTooltip(lightMode, "Text/accent color for completed status indicators"), 140f, 60f);
                        theme.personal_TasksCompletedCount = DrawThemeColorOption("Tasks Completed Count", theme.personal_TasksCompletedCount, BuildThemeOptionTooltip(lightMode, "Text color of checklist completion counters"), 140f, 60f);
                        theme.personal_SectionLabel = DrawThemeColorOption("Section Label Text", theme.personal_SectionLabel, BuildThemeOptionTooltip(lightMode, "Text color of section labels and minor headings"), 140f, 60f);
                        theme.personal_ColumnBg = DrawThemeColorOption("Column Background", theme.personal_ColumnBg, BuildThemeOptionTooltip(lightMode, "Background color of task columns"), 140f, 60f);
                        theme.personal_ColumnBgAlt = DrawThemeColorOption("Column Background Alt", theme.personal_ColumnBgAlt, BuildThemeOptionTooltip(lightMode, "Alternate/background blend color used for column depth"), 140f, 60f);
                        theme.personal_CardBg = DrawThemeColorOption("Card Background", theme.personal_CardBg, BuildThemeOptionTooltip(lightMode, "Background color of task cards"), 140f, 60f);
                        theme.personal_CardHighlighted = DrawThemeColorOption("Card Highlight / Hover", theme.personal_CardHighlighted, BuildThemeOptionTooltip(lightMode, "Highlight color shown when cards are selected or hovered"), 140f, 60f);

                        GUILayout.Space(4);

                        // Tooltip & Hover Popup
                        EditorGUILayout.LabelField("Tooltip & Truncated Text Hover", EditorStyles.miniBoldLabel);
                        theme.personal_TooltipBg = DrawThemeColorOption("Tooltip Background", theme.personal_TooltipBg, BuildThemeOptionTooltip(lightMode, "Background color of custom themed tooltips"), 140f, 60f);
                        theme.personal_TooltipText = DrawThemeColorOption("Tooltip Text Color", theme.personal_TooltipText, BuildThemeOptionTooltip(lightMode, "Text color of custom themed tooltips"), 140f, 60f);
                        theme.personal_TooltipBorder = DrawThemeColorOption("Tooltip Border Color", theme.personal_TooltipBorder, BuildThemeOptionTooltip(lightMode, "Border color of custom themed tooltips"), 140f, 60f);

                        GUILayout.Space(4);

                        // Checklist Tick Boxes
                        EditorGUILayout.LabelField("Checklist Tick Boxes", EditorStyles.miniBoldLabel);
                        theme.personal_ChecklistTickBg = DrawThemeColorOption("Tick Box Background", theme.personal_ChecklistTickBg, BuildThemeOptionTooltip(lightMode, "Background color of unchecked checklist tick boxes"), 140f, 60f);
                        theme.personal_ChecklistTickCheckedBg = DrawThemeColorOption("Tick Box Checked Bg", theme.personal_ChecklistTickCheckedBg, BuildThemeOptionTooltip(lightMode, "Background fill color of checked checklist tick boxes"), 140f, 60f);
                        theme.personal_ChecklistTickBorder = DrawThemeColorOption("Tick Box Border Color", theme.personal_ChecklistTickBorder, BuildThemeOptionTooltip(lightMode, "Border outline color of checklist tick boxes"), 140f, 60f);
                        theme.personal_ChecklistTickColor = DrawThemeColorOption("Checkmark Icon Color", theme.personal_ChecklistTickColor, BuildThemeOptionTooltip(lightMode, "Color of the checkmark icon inside checked tick boxes"), 140f, 60f);
                        DrawStyleChecklistTickIconOption(theme, isDarkMode: false);
                    }
                }

                GUILayout.Space(8);
           

                DrawNoStyleAttributeMatchesHint();
            }
        }

        private void DrawStyleChecklistTickIconOption(ThemeData theme, bool isDarkMode)
        {
            string skinMode = isDarkMode ? "Dark mode (Pro Skin)" : "Light mode (Personal Skin)";
            string tooltip = BuildThemeOptionTooltip(skinMode, "Choose the visual style and glyph used when checklist tick boxes are checked off");

            if (!_styleSearchForceShowCurrentSection && !StyleOptionMatches("Checkmark Icon Style", tooltip, "checklist", "checkbox", "tick", "checkmark", "icon", "vector", "classic", "heavy", "square", "dot", "cross", "unity", "custom", theme.checklistTickStyle.ToString(), theme.customChecklistTickChar))
            {
                return;
            }

            _styleSectionHasVisibleAttribute = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                ThemedTooltip.Label("Checkmark Icon Style", tooltip, null, GUILayout.Width(140f));

                string[] tickStyleDisplayNames = {
                    "Vector Line Check (Crisp)",
                    "Classic Checkmark (✓)",
                    "Heavy Checkmark (✔)",
                    "Minimal Inset Square (■)",
                    "Minimal Dot / Circle (●)",
                    "Clean Cross Mark (✕)",
                    "Unity Native Icon",
                    "Custom Symbol / Emoji"
                };

                int selectedIndex = (int)theme.checklistTickStyle;
                if (selectedIndex < 0 || selectedIndex >= tickStyleDisplayNames.Length) selectedIndex = 0;

                TBStyles.DrawThemedDropdown(
                    selectedIndex,
                    tickStyleDisplayNames,
                    (newIndex) =>
                    {
                        if (newIndex != (int)theme.checklistTickStyle)
                        {
                            theme.checklistTickStyle = (ChecklistTickStyle)newIndex;
                            TBStyles.ChecklistTickStyle = theme.checklistTickStyle;
                            TBStyles.InvalidateCache();
                        }
                    },
                    TBStyles.StandardDropdown,
                    tooltip,
                    GUILayout.MinWidth(120),
                    GUILayout.MaxWidth(200),
                    GUILayout.Height(18)
                );

                GUILayout.Space(6);

                // Live Interactive Preview Box matching mode colors
                Rect prevRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                Color prevBg = isDarkMode ? theme.pro_ChecklistTickCheckedBg : theme.personal_ChecklistTickCheckedBg;
                Color prevBorder = isDarkMode ? theme.pro_ChecklistTickBorder : theme.personal_ChecklistTickBorder;
                Color prevTickCol = isDarkMode ? theme.pro_ChecklistTickColor : theme.personal_ChecklistTickColor;
                EditorGUI.DrawRect(prevRect, prevBg);
                TBStyles.DrawBorderRect(prevRect, prevBorder, 1f);
                TBStyles.DrawCheckmarkIcon(prevRect, prevTickCol, theme.checklistTickStyle, theme.customChecklistTickChar);
                ThemedTooltip.SetTooltip(prevRect, $"{skinMode} preview of checked tick box with {theme.checklistTickStyle} icon");

                GUILayout.FlexibleSpace();
            }

            if (theme.checklistTickStyle == ChecklistTickStyle.Custom)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string customTooltip = BuildThemeOptionTooltip(skinMode, "Type any custom character, emoji, or symbol to use as the checked glyph");
                    ThemedTooltip.Label("Custom Glyph / Symbol", customTooltip, null, GUILayout.Width(140f));
                    string newChar = TBStyles.DrawThemedTextField(theme.customChecklistTickChar ?? "", GUILayout.Width(60), GUILayout.Height(18));
                    ThemedTooltip.SetTooltip(GUILayoutUtility.GetLastRect(), customTooltip);
                    if (newChar != theme.customChecklistTickChar)
                    {
                        theme.customChecklistTickChar = newChar;
                        TBStyles.CustomChecklistTickChar = newChar;
                        TBStyles.InvalidateCache();
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawStyleImportExportActions(ThemeData theme)
        {
            if (!StyleSectionMatchesSearch("export", "exports", "import", "imports", "style", "styles", "theme", "themes", "json", "pack", "bundle")) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("💾 Style Import & Export", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Export custom styles to share with your teammates, or import style JSON files created by others.", MessageType.None);
                GUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("💾 Export Active Theme...", "Export active theme to a JSON file"), GUILayout.Height(24)))
                    {
                        ExportTheme(theme);
                    }

                    if (GUILayout.Button(new GUIContent("📦 Export All Themes Pack...", "Export all themes into a bundle JSON file"), GUILayout.Height(24)))
                    {
                        ExportAllThemes();
                    }

                    if (GUILayout.Button(new GUIContent("📥 Import Theme(s)...", "Import theme JSON file or Theme Pack"), GUILayout.Height(24)))
                    {
                        ImportTheme();
                    }
                }
            }
        }

        private void ExportTheme(ThemeData theme)
        {
            if (theme == null) return;
            string defaultName = $"{SanitizeFileName(theme.name)}_Theme.json";
            string path = EditorUtility.SaveFilePanel("Export Theme", "", defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                theme.Normalize();
                string json = JsonUtility.ToJson(theme, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                TriggerSuccessNotification($"Theme \"{theme.name}\" exported!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AwesomeTaskManager] Failed to export theme: {ex.Message}");
                TriggerErrorNotification($"Export failed: {ex.Message}");
            }
        }

        private void ExportAllThemes()
        {
            if (_data?.themes == null || _data.themes.Count == 0) return;
            string defaultName = "AwesomeTaskManager_ThemesPack.json";
            string path = EditorUtility.SaveFilePanel("Export All Themes", "", defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var bundle = new ThemeExportBundle
                {
                    bundleName = "Awesome Task Manager Themes",
                    version = "1.0",
                    themes = new List<ThemeData>(_data.themes.Select(t => t.Clone()))
                };
                string json = JsonUtility.ToJson(bundle, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                TriggerSuccessNotification($"Exported {_data.themes.Count} theme(s) to pack!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AwesomeTaskManager] Failed to export themes pack: {ex.Message}");
                TriggerErrorNotification($"Export failed: {ex.Message}");
            }
        }

        private void ImportTheme()
        {
            string path = EditorUtility.OpenFilePanel("Import Theme", "", "json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    TriggerErrorNotification("Selected file is empty.");
                    return;
                }

                // First try to parse as ThemeExportBundle
                ThemeExportBundle bundle = null;
                try
                {
                    bundle = JsonUtility.FromJson<ThemeExportBundle>(json);
                }
                catch { }

                if (bundle != null && bundle.themes != null && bundle.themes.Count > 0)
                {
                    int count = 0;
                    foreach (var t in bundle.themes)
                    {
                        if (t != null && !string.IsNullOrWhiteSpace(t.name))
                        {
                            t.Normalize();
                            t.name = GetUniqueThemeName(t.name);
                            _data.themes.Add(t);
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        _data.currentThemeIndex = _data.themes.Count - 1;
                        TBStyles.ApplyTheme(_data.themes[_data.currentThemeIndex]);
                        SaveTheme();
                        TriggerSuccessNotification($"Imported {count} theme(s) from pack!");
                        return;
                    }
                }

                // Try parsing as single ThemeData
                ThemeData single = null;
                try
                {
                    single = JsonUtility.FromJson<ThemeData>(json);
                }
                catch { }

                if (single != null && !string.IsNullOrWhiteSpace(single.name))
                {
                    single.Normalize();
                    single.name = GetUniqueThemeName(single.name);
                    _data.themes.Add(single);
                    _data.currentThemeIndex = _data.themes.Count - 1;
                    TBStyles.ApplyTheme(_data.themes[_data.currentThemeIndex]);
                    SaveTheme();
                    TriggerSuccessNotification($"Imported theme \"{single.name}\"!");
                    return;
                }

                TriggerErrorNotification("Could not parse valid theme data in JSON file.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AwesomeTaskManager] Failed to import theme: {ex.Message}");
                TriggerErrorNotification($"Import failed: {ex.Message}");
            }
        }

        private string GetUniqueThemeName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Theme";
            if (_data.themes.All(t => t.name != baseName)) return baseName;
            int counter = 2;
            while (_data.themes.Any(t => t.name == $"{baseName} {counter}"))
            {
                counter++;
            }
            return $"{baseName} {counter}";
        }

        private void DrawAssigneeCircleBoard(Assignee assignee)
        {
            var rect = GUILayoutUtility.GetRect(24, 24);
            string initials = GetInitials(assignee.name);
            var circleStyle = new GUIStyle(TBStyles.AssigneeCircle) { fixedWidth = 24, fixedHeight = 24, fontSize = 9 };
            
            TBStyles.DrawAssigneeIcon(rect, assignee, initials, circleStyle);
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

