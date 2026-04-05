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
        private QuickNote _note;
        private SaveData _saveData;
        private System.Action _onChanged;
        private Vector2 _scroll;

        public static NotePopupWindow Open(QuickNote note, SaveData saveData, System.Action onChanged)
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

        private void OnGUI()
        {
            if (_note == null) { Close(); return; }

            // Title bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string newTitle = EditorGUILayout.TextField(_note.title, EditorStyles.toolbarTextField);
            if (newTitle != _note.title)
            {
                _note.title = newTitle;
                titleContent = new GUIContent($"📝 {_note.title}");
                MarkModified();
            }

            // Pin
            if (GUILayout.Button(_note.pinned ? "📌" : "Pin", EditorStyles.toolbarButton, GUILayout.Width(34)))
            {
                _note.pinned = !_note.pinned;
                MarkModified();
            }

            // Color
            int newCol = EditorGUILayout.Popup(_note.colorIndex, TBStyles.LabelNames, EditorStyles.toolbarPopup, GUILayout.Width(65));
            if (newCol != _note.colorIndex) { _note.colorIndex = newCol; MarkModified(); }

            // Folder
            if (_saveData != null && GUILayout.Button("📁", EditorStyles.toolbarButton, GUILayout.Width(26)))
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

            EditorGUILayout.EndHorizontal();

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

            // Content
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            string newContent = EditorGUILayout.TextArea(_note.content, new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true, fontSize = 13, padding = new RectOffset(8, 8, 8, 8)
            }, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (newContent != _note.content)
            {
                _note.content = newContent;
                MarkModified();
            }

            // Bottom status
            var statusRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(statusRect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.85f, 0.85f, 0.85f));
            EditorGUI.LabelField(statusRect,
                $"  {_note.WordCount} words  •  {_note.CharCount} chars  •  Created: {_note.createdDate}",
                EditorStyles.miniLabel);
        }

        private void MarkModified()
        {
            _note.modifiedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            _onChanged?.Invoke();
            Repaint();
        }
    }
}

