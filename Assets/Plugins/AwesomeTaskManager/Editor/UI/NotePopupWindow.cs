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

        public static NotePopupWindow Open(QuickNote note, SaveData saveData, Action onChanged)
        {
            // Allow multiple instances (one per note)
            var win = CreateInstance<NotePopupWindow>();
            win.titleContent = new GUIContent($"{TBStyles.NotesTabIcon} {note.title}");
            win._note = note;
            win._saveData = saveData;
            win._onChanged = onChanged;
            win.minSize = new Vector2(360, 300);
            win.Show();
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
            win.minSize = new Vector2(360, 300);
            win.Show();
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
            try
            {
                if (_note == null) { Close(); return; }

                _hasAnimatedGif = false;

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.NotePopoutBg, true);
                }

                // Title bar
                using (var tbScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(24)))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        TBStyles.DrawGlassPanel(tbScope.rect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);
                    }

                    string newTitle = TBStyles.DrawThemedTextField(_note.title, TBStyles.NoteTitle, GUILayout.Height(24));
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

                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scope.scrollPosition;
                    
                    if (!_isPreview)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("🖼 Insert", EditorStyles.miniLabel, GUILayout.Width(46));
                            if (ThemedTooltip.Button("📋 Paste", "Paste Image from Clipboard", TBStyles.NoteActionButton, GUILayout.Width(64), GUILayout.Height(18)))
                            {
                                if (MarkdownRenderer.TryPasteImageFromClipboard(_note, _ => MarkModified(), Repaint))
                                {
                                    GUI.FocusControl(null);
                                }
                            }
                            if (ThemedTooltip.Button("📎 Browse", "Browse for Image", TBStyles.NoteActionButton, GUILayout.Width(72), GUILayout.Height(18)))
                            {
                                EditorApplication.delayCall += () =>
                                {
                                    string imgPath = EditorUtility.OpenFilePanel("Attach Image", "", "png,jpg,jpeg,gif,bmp,tga,psd,tiff");
                                    if (!string.IsNullOrEmpty(imgPath))
                                    {
                                        string assetPath = MarkdownRenderer.CopyImageToProject(imgPath);
                                        if (!string.IsNullOrEmpty(assetPath))
                                        {
                                            if (_note.imagePaths == null) _note.imagePaths = new List<string>();
                                            if (!_note.imagePaths.Contains(assetPath)) _note.imagePaths.Add(assetPath);
                                            string fileName = Path.GetFileName(assetPath);
                                            _note.content = (_note.content ?? "") + $"\n![[{fileName}]]";
                                            MarkModified();
                                            Repaint();
                                        }
                                    }
                                };
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.Space(2);

                        // ── Ctrl+V / Cmd+V to paste images from clipboard ──
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.V && (Event.current.control || Event.current.command))
                        {
                            if (MarkdownRenderer.TryPasteImageFromClipboard(_note, _ => MarkModified(), Repaint))
                            {
                                GUI.FocusControl(null);
                                Event.current.Use();
                            }
                        }

                        // ── Drag and Drop images ──
                        MarkdownRenderer.HandleNoteDragDropImages(_note, _ => MarkModified(), Repaint);

                        string newContent = EditorGUILayout.TextArea(_note.content, TBStyles.NoteTextArea, GUILayout.ExpandHeight(true));

                        if (newContent != _note.content)
                        {
                            _note.content = newContent;
                            MarkModified();
                        }
                    }
                    else
                    {
                        if (MarkdownRenderer.DrawMarkdownPreview(_note, _ => MarkModified()))
                        {
                            _hasAnimatedGif = true;
                        }
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
    }
}

