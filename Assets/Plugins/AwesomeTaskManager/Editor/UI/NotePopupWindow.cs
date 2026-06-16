using System;
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
            win.titleContent = new GUIContent($"📝 {note.title}");
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
            win.titleContent = new GUIContent($"📝 {note.title}");
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
            if (_note == null) { Close(); return; }

            _hasAnimatedGif = false;

            // Title bar
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string newTitle = EditorGUILayout.TextField(_note.title, EditorStyles.toolbarTextField);
                if (newTitle != _note.title)
                {
                    _note.title = newTitle;
                    titleContent = new GUIContent($"📝 {_note.title}");
                    MarkModified();
                }

                // Preview Toggle
                GUI.backgroundColor = _isPreview ? new Color(0.3f, 0.7f, 0.95f) : Color.white;
                if (GUILayout.Button(_isPreview ? new GUIContent( "✍ Edit", "Switch to Preview Note Mode") : new GUIContent("👁 Preview", "Switch to Edit Note Mode"), EditorStyles.toolbarButton, GUILayout.Width(75)))
                {
                    _isPreview = !_isPreview;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;

                // Pin
                if (GUILayout.Button(_note.pinned ? new GUIContent( "📌","Unpin Card") : new GUIContent("Pin", "Pin Card"), EditorStyles.toolbarButton, GUILayout.Width(34)))
                {
                    _note.pinned = !_note.pinned;
                    MarkModified();
                }

                // Color
                int newCol = EditorGUILayout.Popup(_note.colorIndex, TBStyles.LabelNames, EditorStyles.toolbarPopup, GUILayout.Width(65));
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Note color label"));
                if (newCol != _note.colorIndex) { _note.colorIndex = newCol; MarkModified(); }

                // Folder
                if (_saveData != null && GUILayout.Button(new GUIContent("📁", "Select Folder"), EditorStyles.toolbarButton, GUILayout.Width(26)))
                {
                    var menu = new GenericMenu();
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
                    menu.ShowAsContext();
                }
            }

            // Color strip
            if (_note.colorIndex > 0)
            {
                var c = TBStyles.LabelColors[Mathf.Clamp(_note.colorIndex, 0, TBStyles.LabelColors.Length - 1)];
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

                    string newContent = EditorGUILayout.TextArea(_note.content, new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true,
                        fontSize = 13,
                        padding = new RectOffset(8, 8, 8, 8)
                    }, GUILayout.ExpandHeight(true));

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
            EditorGUI.DrawRect(statusRect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.85f, 0.85f, 0.85f));
            EditorGUI.LabelField(statusRect,
                $"  {_note.WordCount} words  •  {_note.CharCount} chars  •  Created: {_note.createdDate}",
                EditorStyles.miniLabel);

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

