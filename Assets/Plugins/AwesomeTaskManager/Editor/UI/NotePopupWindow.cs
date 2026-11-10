using System;
using System.Collections.Generic;
using System.IO;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    /// <summary>
    /// A standalone pop-out window for editing a single note.
    /// Opened by dragging a note away from the list.
    /// </summary>
    public class NotePopupWindow : EditorWindow
    {
        [SerializeField] private QuickNote _note;
        private SaveData _saveData;
        private Action _onChanged;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private bool _isPreview;
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;

        private NoteClipboardController _clipboard;
        private readonly List<string> _noteUndoStack = new List<string>();
        private readonly List<string> _noteRedoStack = new List<string>();
        private string _lastCommittedContent = null;
        private float _lastUndoPushTime = 0;
        private bool _needsUndoBaseline = false;

        public static NotePopupWindow Open(QuickNote note, SaveData saveData, Action onChanged)
        {
            // Allow multiple instances (one per note)
            var win = CreateInstance<NotePopupWindow>();
            win.titleContent = new GUIContent($"{TBStyles.NotesTabIcon} {note.title}");
            win._note = note;
            win._saveData = saveData;
            win._onChanged = onChanged;
            float startWidth = 360f;
            float startHeight = 280f;
            win.minSize = new Vector2(300f, 220f);
            try
            {
                var mainPos = EditorGUIUtility.GetMainWindowPosition();
                var center = new Vector2(mainPos.x + mainPos.width * 0.5f, mainPos.y + mainPos.height * 0.5f);
                win.position = new Rect(center.x - startWidth * 0.5f, center.y - startHeight * 0.5f, startWidth, startHeight);
            }
            catch
            {
                try { win.position = new Rect(100f, 100f, startWidth, startHeight); } catch { }
            }
            try
            {
                win.Show();
            }
            catch { }
            return win;
        }

        public static NotePopupWindow OpenInPreviewMode(QuickNote note, SaveData saveData, Action onChanged)
        {
            // Allow multiple instances (one per note)
            var win = CreateInstance<NotePopupWindow>();
            win.titleContent = new GUIContent($"{TBStyles.NotesTabIcon} {note.title}");
            win._note = note;
            win._saveData = saveData;
            win._onChanged = onChanged;
            float startWidth = 360f;
            float startHeight = 280f;
            win.minSize = new Vector2(300f, 220f);
            try
            {
                var mainPos = EditorGUIUtility.GetMainWindowPosition();
                var center = new Vector2(mainPos.x + mainPos.width * 0.5f, mainPos.y + mainPos.height * 0.5f);
                win.position = new Rect(center.x - startWidth * 0.5f, center.y - startHeight * 0.5f, startWidth, startHeight);
            }
            catch
            {
                try { win.position = new Rect(100f, 100f, startWidth, startHeight); } catch { }
            }
            try
            {
                win.Show();
            }
            catch { }
            win._isPreview = true;
            return win;
        }

        public void LoadData()
        {
            var freshData = Persistence.Load();
            if (freshData == null) return;
            _saveData = freshData;
            if (_note != null)
            {
                var found = _saveData.notes.Find(n => n.id == _note.id);
                if (found != null) _note = found;
            }
            RefreshVisualState();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            LoadData();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnDestroy()
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            TBStyles.InvalidateCache();
            Repaint();
            EditorApplication.delayCall += () =>
            {
                if (this != null) Repaint();
            };
        }

        private void OnGUI()
        {
            if (_note?.content != null && _note.content.Contains("\r"))
            {
                _note.content = _note.content.Replace("\r\n", "\n").Replace("\r", "\n");
            }

            try
            {
                if (_note == null) { Close(); return; }

                _hasAnimatedGif = false;

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.NotePopoutBg, true);
                }

                // Title bar
                using (var tbScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(24), GUILayout.ExpandWidth(true)))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        TBStyles.DrawGlassPanel(tbScope.rect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);
                    }

                    string newTitle = TBStyles.DrawThemedTextField(_note.title, TBStyles.NoteTitle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                    if (newTitle != _note.title)
                    {
                        _note.title = newTitle;
                        titleContent = new GUIContent($"{TBStyles.NotesTabIcon} {_note.title}");
                        MarkModified();
                    }

                    // Preview Toggle
                    if (ThemedTooltip.Button(_isPreview ? "✍ Edit" : "👁 Preview", _isPreview ? "Switch to Edit Note Mode" : "Switch to Preview Note Mode", TBStyles.ToolbarButton, GUILayout.Width(75)))
                    {
                        _isPreview = !_isPreview;
                        Clipboard.Reset();
                        GUI.FocusControl(null);
                    }

                    // Pin
                    if (ThemedTooltip.Button(_note.pinned ? TBStyles.PinnedNoteIcon : "Pin", _note.pinned ? "Unpin Card" : "Pin Card", TBStyles.ToolbarButton, GUILayout.Width(34)))
                    {
                        _note.pinned = !_note.pinned;
                        MarkModified();
                    }

                    // Color
                    TBStyles.DrawThemedDropdown(_note.colorIndex, TBStyles.LabelNames, (newCol) =>
                    {
                        if (newCol != _note.colorIndex) { _note.colorIndex = newCol; MarkModified(); Repaint(); }
                    }, TBStyles.ToolbarPopup, TBStyles.GetLabelColorsArray(), "Note color label", GUILayout.Width(65));

                    // Folder
                    if (_saveData != null && ThemedContextMenu.DropdownButton("📁", "Select Folder", TBStyles.ToolbarButton, out Rect folderBtnRect, GUILayout.Width(26)))
                    {
                        var menu = new ThemedContextMenu();
                        menu.AddItem(new GUIContent("Unfiled"), string.IsNullOrEmpty(_note.folderId), () =>
                        {
                            _note.folderId = ""; MarkModified();
                        });
                        foreach (var folder in _saveData.noteFolders)
                        {
                            string fid = folder.id;
                            menu.AddItem(new GUIContent(folder.name), _note.folderId == fid, () =>
                            {
                                _note.folderId = fid; MarkModified();
                            });
                        }
                        menu.Show(folderBtnRect);
                    }
                }

                // Color strip
                if (_note.colorIndex > 0)
                {
                    var c = TBStyles.GetLabelColor(_note.colorIndex);
                    var strip = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(strip, c);
                }

                // Metadata
                GUILayout.Space(2);
                string folderName = "Unfiled";
                if (!string.IsNullOrEmpty(_note.folderId) && _saveData != null)
                {
                    var f = _saveData.noteFolders.Find(x => x.id == _note.folderId);
                    if (f != null) folderName = f.name;
                }
                EditorGUILayout.LabelField(
                    $"📁 {folderName}  |  {_note.modifiedDate}  |  {_note.WordCount} words",
                    EditorStyles.miniLabel);
                GUILayout.Space(2);

                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    _scroll = scope.scrollPosition;
                    
                    // ── Command events: Undo / Redo (from Unity Edit menu or Mac Cmd+Z) ──
                    if (Event.current != null)
                    {
                        if (Event.current.type == EventType.ValidateCommand)
                        {
                            if (Event.current.commandName == "Undo" || Event.current.commandName == "Redo")
                            {
                                Event.current.Use();
                            }
                        }
                        else if (Event.current.type == EventType.ExecuteCommand)
                        {
                            if (Event.current.commandName == "Undo")
                            {
                                PerformUndo();
                                Event.current.Use();
                            }
                            else if (Event.current.commandName == "Redo")
                            {
                                PerformRedo();
                                Event.current.Use();
                            }
                        }
                    }

                    // ── Keyboard shortcuts: Undo / Redo / Cut / Copy / Paste ──
                    if (Event.current != null && Event.current.type == EventType.KeyDown && (Event.current.control || Event.current.command))
                    {
                        if (Event.current.keyCode == KeyCode.Z)
                        {
                            if (Event.current.shift) PerformRedo();
                            else PerformUndo();
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.Y)
                        {
                            PerformRedo();
                            Event.current.Use();
                        }
                    }
                    Clipboard.HandleKeyboardShortcuts(_note, !_isPreview);

                    if (!_isPreview)
                    {
                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                        {
                            EditorGUILayout.LabelField("🖼 Insert", EditorStyles.miniLabel, GUILayout.Width(46));
                            if (ThemedTooltip.Button("📋 Paste", "Paste Image or Text from Clipboard", TBStyles.NoteActionButton, GUILayout.Width(64), GUILayout.Height(18)))
                            {
                                Clipboard.SmartPaste(_note, Clipboard.Live);
                            }
                            if (ThemedTooltip.Button("📎 Browse", "Browse for Image or Video", TBStyles.NoteActionButton, GUILayout.Width(72), GUILayout.Height(18)))
                            {
                                AttachImage(_note);
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.Space(2);

                        // ── Drag and Drop images ──
                        if (Event.current.type == EventType.DragPerform) RecordUndo(true);
                        MarkdownRenderer.HandleNoteDragDropImages(_note, _ => MarkModified(), Repaint);

                        Clipboard.BeginTextArea(_note);
                        string newContent = EditorGUILayout.TextArea(_note.content, TBStyles.NoteTextArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                        if (newContent != _note.content)
                        {
                            newContent = newContent.Replace("\r\n", "\n").Replace("\r", "\n");

                            bool isFirstEdit = _needsUndoBaseline || _lastCommittedContent == null || _noteUndoStack.Count == 0;
                            bool isPause = (Time.realtimeSinceStartup - _lastUndoPushTime) > 0.8f;
                            bool isMajorDelta = Math.Abs(newContent.Length - (_note.content ?? "").Length) > 5;
                            bool isNewlineOrWordBreak = newContent.EndsWith("\n") || newContent.EndsWith(" ");

                            if (isFirstEdit || isPause || isMajorDelta || isNewlineOrWordBreak)
                            {
                                RecordUndo(true);
                            }
                            _needsUndoBaseline = false;
                            _note.content = newContent;
                            MarkModified();
                        }

                        Rect r = GUILayoutUtility.GetLastRect();
                        Clipboard.EndTextArea(_note, r);
                    }
                    else
                    {
                        Clipboard.HandlePreviewContextClick(_note);
                        if (MarkdownRenderer.DrawMarkdownPreview(_note, _ => MarkModified(), _ => RecordUndo(true)))
                        {
                            _hasAnimatedGif = true;
                        }
                        Clipboard.TrackPreviewSelection(_note);
                    }
                }

                // Bottom status
                var statusRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                TBStyles.DrawGlassPanel(statusRect, TBStyles.StatusBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);
                EditorGUI.LabelField(statusRect,
                    $"  {_note.WordCount} words  •  {_note.CharCount} chars  •  Created: {_note.createdDate}",
                    TBStyles.StatusBar);
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(this);
            }

            // GIF repaint
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        private void MarkModified()
        {
            _note.modifiedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            // Reload to ensure we merge into the latest version of everything else
            var freshData = Persistence.Load();
            if (freshData == null)
            {
                return;
            }
            var existing = freshData.notes.Find(n => n.id == _note.id);
            if (existing != null)
            {
                // Push our note into freshData
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_note), existing);
                _saveData = freshData;
            }
            
            if (_saveData != null) Persistence.Save(_saveData);

            TaskBoardWindow.ReloadAllOpenWindows();

            if (_onChanged != null)
            {
                _onChanged.Invoke();
            }
            
            Repaint();
        }

        private NoteClipboardController Clipboard => _clipboard ??= new NoteClipboardController("NoteEditorPopout", TBStyles.NoteTextArea, new NoteClipboardController.Host
        {
            MarkModified = _ => MarkModified(),
            RecordUndo = _ => RecordUndo(true),
            Repaint = Repaint,
            CanUndo = _ => _noteUndoStack.Count > 0,
            CanRedo = _ => _noteRedoStack.Count > 0,
            Undo = _ => PerformUndo(),
            Redo = _ => PerformRedo(),
            SetEditMode = editMode =>
            {
                _isPreview = !editMode;
                GUI.FocusControl(null);
            },
            AttachImage = AttachImage,
            PopoutNote = note =>
            {
                if (_isPreview)
                    OpenInPreviewMode(note, _saveData, _onChanged);
                else
                    Open(note, _saveData, _onChanged);
            },
            ExportNote = note => { EditorApplication.delayCall += () => TaskBoardWindow.ExportSingleNote(note); },
            SwitchToEditLabel = "✍ Switch to Edit Mode"
        });

        private void AttachImage(QuickNote note)
        {
            if (note == null) return;

            int insertionIndex;
            int replaceLength;
            if (_isPreview)
            {
                insertionIndex = NoteClipboardController.NormalizeNewlines(note.content).Length;
                replaceLength = 0;
            }
            else
            {
                Clipboard.GetInsertTarget(note, out insertionIndex, out replaceLength);
            }

            EditorApplication.delayCall += () =>
            {
                if (this == null || note == null) return;

                string imagePath = EditorUtility.OpenFilePanelWithFilters("Attach Image or Video", "", MarkdownRenderer.NoteAttachmentFileFilters);
                if (string.IsNullOrEmpty(imagePath)) return;

                string assetPath = MarkdownRenderer.CopyImageToProject(imagePath);
                if (string.IsNullOrEmpty(assetPath)) return;

                if (note.imagePaths == null) note.imagePaths = new List<string>();
                if (!note.imagePaths.Contains(assetPath)) note.imagePaths.Add(assetPath);

                string content = NoteClipboardController.NormalizeNewlines(note.content);
                int insertPosition = Mathf.Clamp(insertionIndex, 0, content.Length);
                string prefix = insertPosition > 0 && !content.Substring(0, insertPosition).EndsWith("\n") ? "\n" : "";
                string tag = $"![[{Path.GetFileName(assetPath)}]]";
                RecordUndo(true);
                note.content = NoteClipboardController.ReplaceRange(content, insertPosition, replaceLength, prefix + tag, out int caret);
                Clipboard.SetCaret(caret);
                MarkModified();
                Repaint();
            };
        }

        private void RecordUndo(bool force = false)
        {
            if (_note == null) return;
            string content = (_note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            if (_noteUndoStack.Count > 0 && _noteUndoStack[_noteUndoStack.Count - 1] == content) return;

            if (!force && _lastCommittedContent != null && _lastCommittedContent == content) return;

            _noteUndoStack.Add(content);
            if (_noteUndoStack.Count > 100) _noteUndoStack.RemoveAt(0);
            _noteRedoStack.Clear();
            _lastCommittedContent = content;
            _lastUndoPushTime = Time.realtimeSinceStartup;
            _needsUndoBaseline = false;
        }

        private void PerformUndo()
        {
            if (_note == null || _noteUndoStack.Count == 0) return;

            string currentContent = (_note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            while (_noteUndoStack.Count > 0 && _noteUndoStack[_noteUndoStack.Count - 1] == currentContent)
            {
                _noteUndoStack.RemoveAt(_noteUndoStack.Count - 1);
            }
            if (_noteUndoStack.Count == 0) return;

            _noteRedoStack.Add(currentContent);
            if (_noteRedoStack.Count > 100) _noteRedoStack.RemoveAt(0);

            string previousContent = _noteUndoStack[_noteUndoStack.Count - 1];
            _noteUndoStack.RemoveAt(_noteUndoStack.Count - 1);

            int caret = Clipboard.CaretIndex;
            _note.content = previousContent;
            _lastCommittedContent = previousContent;
            _lastUndoPushTime = Time.realtimeSinceStartup;
            _needsUndoBaseline = true;

            NoteClipboardController.SyncTextEditors(previousContent, caret);
            Clipboard.Reset();

            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;

            MarkModified();
            GUI.changed = true;
            Repaint();
        }

        private void PerformRedo()
        {
            if (_note == null || _noteRedoStack.Count == 0) return;

            string currentContent = (_note.content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            while (_noteRedoStack.Count > 0 && _noteRedoStack[_noteRedoStack.Count - 1] == currentContent)
            {
                _noteRedoStack.RemoveAt(_noteRedoStack.Count - 1);
            }
            if (_noteRedoStack.Count == 0) return;

            _noteUndoStack.Add(currentContent);
            if (_noteUndoStack.Count > 100) _noteUndoStack.RemoveAt(0);

            string nextContent = _noteRedoStack[_noteRedoStack.Count - 1];
            _noteRedoStack.RemoveAt(_noteRedoStack.Count - 1);

            int caret = Clipboard.CaretIndex;
            _note.content = nextContent;
            _lastCommittedContent = nextContent;
            _lastUndoPushTime = Time.realtimeSinceStartup;
            _needsUndoBaseline = true;

            NoteClipboardController.SyncTextEditors(nextContent, caret);
            Clipboard.Reset();

            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;

            MarkModified();
            GUI.changed = true;
            Repaint();
        }

    }
}

