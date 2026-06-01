using System;
using System.Collections.Generic;
using System.IO;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    public class AssigneeManagerWindow : EditorWindow
    {
        [SerializeField] private SaveData _data;
        private System.Action _onChanged;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private string _newName = "";
        [SerializeField] private int _newColorIndex = 1;
        [SerializeField] private int _newBorderColorIndex = 0;
        [SerializeField] private Texture2D _newProfileImage;
        private Dictionary<string, string> _nameBuffers = new Dictionary<string, string>();
        private Dictionary<string, Texture2D> _profileImageCache = new Dictionary<string, Texture2D>();

        public static void ShowWindow(SaveData data, System.Action onChanged)
        {
            var window = GetWindow<AssigneeManagerWindow>(true, "👥 Assignee Manager", true);
            window._data = data;
            window._onChanged = onChanged;
            window.minSize = new Vector2(400, 400);
            window.ShowUtility();
        }

        public void LoadData()
        {
            _data = Persistence.Load();
            Repaint();
        }

        private void OnGUI()
        {
            if (_data == null)
            {
                Close();
                return;
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scope.scrollPosition;

                EditorGUILayout.LabelField("Assignee Manager", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Manage project members. You can assign these people to task cards.", MessageType.None);
                GUILayout.Space(10);

                DrawAddSection();
                GUILayout.Space(15);
                DrawSeparator();
                GUILayout.Space(15);
                DrawList();
            }
        }

        private void DrawAddSection()
        {
            EditorGUILayout.LabelField("Add New Member", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Profile Image Picker
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(70)))
                    {
                        EditorGUILayout.LabelField("Icon", EditorStyles.miniLabel);
                        Rect iconRect = GUILayoutUtility.GetRect(64, 64);
                        var newFieldTex = (Texture2D)EditorGUI.ObjectField(iconRect, _newProfileImage, typeof(Texture2D), false);
                        if (newFieldTex != _newProfileImage)
                        {
                            _newProfileImage = newFieldTex;
                        }
                        
                        // Icon-specific handlers
                        HandleImageDragDrop(iconRect, (tex, path) => { 
                            _newProfileImage = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            Repaint(); 
                        });
                        HandleImagePaste(iconRect, (tex, path) => { 
                            _newProfileImage = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            Repaint(); 
                        });

                        if (GUILayout.Button(new GUIContent("Browse...", "Select Profile Image from File System"), EditorStyles.miniButton, GUILayout.Width(64)))
                        {
                            EditorApplication.delayCall += () =>
                            {
                                string path = EditorUtility.OpenFilePanel("Select Profile Image", "", "png,jpg,jpeg,gif,bmp");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    string assetPath = MarkdownRenderer.CopyImageToProject(path);
                                    _newProfileImage = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                                    Repaint();
                                }
                            };
                        }
                    }

                    GUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        _newName = EditorGUILayout.TextField("Name", _newName);
                        GUILayout.Space(4);
                        
                        _newColorIndex = EditorGUILayout.Popup(new GUIContent("Initials Color", "Colour for background when Image is not available"), _newColorIndex, TBStyles.LabelNames);
                        _newBorderColorIndex = EditorGUILayout.Popup(new GUIContent("Border Color", "Colour for border"), _newBorderColorIndex, TBStyles.LabelNames);
                    }
                }

                // Handle drag/paste for the whole add section box
                if (Event.current.type != EventType.Layout)
                {
                    var lastRect = GUILayoutUtility.GetLastRect();
                    HandleImageDragDrop(lastRect, (tex, path) => { 
                        _newProfileImage = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path); 
                        Repaint(); 
                    });
                    HandleImagePaste(lastRect, (tex, path) => { 
                        _newProfileImage = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path); 
                        Repaint(); 
                    });
                }

                GUILayout.Space(10);

                GUI.enabled = !string.IsNullOrWhiteSpace(_newName);
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
                if (GUILayout.Button("✅ Add Member", GUILayout.Height(30)))
                {
                    string guid = _newProfileImage != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_newProfileImage)) : "";
                    _data.assignees.Add(new Assignee { 
                        name = _newName.Trim(), 
                        colorIndex = _newColorIndex, 
                        borderColorIndex = _newBorderColorIndex,
                        profileImageGuid = guid
                    });
                    _newName = "";
                    _newProfileImage = null;
                    NotifyChanged();
                    GUI.FocusControl(null);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }
        }

        private void DrawList()
        {
            EditorGUILayout.LabelField("Current Members", EditorStyles.boldLabel);

            if (_data.assignees == null || _data.assignees.Count == 0)
            {
                EditorGUILayout.HelpBox("No members added yet.", MessageType.Info);
                return;
            }

            bool shouldExitGUI = false;
            for (int i = 0; i < _data.assignees.Count; i++)
            {
                var assignee = _data.assignees[i];
                if (!_nameBuffers.ContainsKey(assignee.id))
                    _nameBuffers[assignee.id] = assignee.name;

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Icon
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(54)))
                        {
                            Texture2D profileTex = GetProfileTexture(assignee);
                            Rect iconRect = GUILayoutUtility.GetRect(50, 50);
                            var newTex = (Texture2D)EditorGUI.ObjectField(iconRect, profileTex, typeof(Texture2D), false);

                            if (newTex != profileTex)
                            {
                                assignee.profileImageGuid = newTex != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newTex)) : "";
                                _profileImageCache[assignee.id] = newTex;
                                NotifyChanged();
                            }

                            if (GUILayout.Button(new GUIContent("Browse...", "Select Profile Image from File System"), EditorStyles.miniButton, GUILayout.Width(60)))
                            {
                                EditorApplication.delayCall += () =>
                                {
                                    string path = EditorUtility.OpenFilePanel("Select Profile Image", "", "png,jpg,jpeg,gif,bmp");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        string assetPath = MarkdownRenderer.CopyImageToProject(path);
                                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                                        assignee.profileImageGuid = AssetDatabase.AssetPathToGUID(assetPath);
                                        _profileImageCache[assignee.id] = tex;
                                        NotifyChanged();
                                    }
                                };
                            }
                        }

                        GUILayout.Space(10);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            // Name edit
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Name", GUILayout.Width(50));
                                _nameBuffers[assignee.id] = EditorGUILayout.TextField(_nameBuffers[assignee.id]);
                                if (_nameBuffers[assignee.id] != assignee.name)
                                {
                                    assignee.name = _nameBuffers[assignee.id];
                                    NotifyChanged();
                                }
                            }

                            // Colors
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField(new GUIContent("Initials","Colour for background when Image is not available"), GUILayout.Width(50));
                                int newColor = EditorGUILayout.Popup(assignee.colorIndex, TBStyles.LabelNames);
                                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select initials background color"));
                                if (newColor != assignee.colorIndex)
                                {
                                    assignee.colorIndex = newColor;
                                    NotifyChanged();
                                }

                                EditorGUILayout.LabelField(new GUIContent("Border", "Colour for border"), GUILayout.Width(50));
                                int newBorderColor = EditorGUILayout.Popup(assignee.borderColorIndex, TBStyles.LabelNames);
                                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", "Select border color"));
                                if (newBorderColor != assignee.borderColorIndex)
                                {
                                    assignee.borderColorIndex = newBorderColor;
                                    NotifyChanged();
                                }
                            }
                        }

                        GUILayout.Space(5);

                        // Delete
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button(new GUIContent("🗑", "Delete Assignee"), GUILayout.Width(24), GUILayout.Height(50)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Assignee", $"Remove \"{assignee.name}\"?\nThey will be unassigned from all cards.", "Delete", "Cancel"))
                            {
                                _data.assignees.RemoveAt(i);
                                _nameBuffers.Remove(assignee.id);

                                // Cleanup card assignments
                                foreach (var board in _data.boards)
                                    foreach (var col in board.columns)
                                        foreach (var card in col.cards)
                                            card.assigneeIds.Remove(assignee.id);

                                NotifyChanged();
                                shouldExitGUI = true;
                            }
                        }
                        GUI.backgroundColor = Color.white;
                    }

                    // Handle drag/paste for the whole row
                    if (Event.current.type != EventType.Layout)
                    {
                        var rowRect = GUILayoutUtility.GetLastRect();
                        HandleImageDragDrop(rowRect, (tex, path) =>
                        {
                            string guid = "";
                            if (tex != null) guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tex));
                            else if (!string.IsNullOrEmpty(path)) guid = AssetDatabase.AssetPathToGUID(path);

                            if (!string.IsNullOrEmpty(guid))
                            {
                                assignee.profileImageGuid = guid;
                                _profileImageCache[assignee.id] = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                NotifyChanged();
                            }
                        });
                        HandleImagePaste(rowRect, (tex, path) =>
                        {
                            string guid = "";
                            if (tex != null) guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tex));
                            else if (!string.IsNullOrEmpty(path)) guid = AssetDatabase.AssetPathToGUID(path);

                            if (!string.IsNullOrEmpty(guid))
                            {
                                assignee.profileImageGuid = guid;
                                _profileImageCache[assignee.id] = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                NotifyChanged();
                            }
                        });
                    }
                }

                if (shouldExitGUI) break;
            }

            if (shouldExitGUI) GUIUtility.ExitGUI();
        }

        private Texture2D GetProfileTexture(Assignee assignee)
        {
            if (string.IsNullOrEmpty(assignee.profileImageGuid)) return null;
            if (_profileImageCache.TryGetValue(assignee.id, out var tex) && tex != null) return tex;

            string path = AssetDatabase.GUIDToAssetPath(assignee.profileImageGuid);
            if (!string.IsNullOrEmpty(path))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                _profileImageCache[assignee.id] = tex;
                return tex;
            }
            return null;
        }

        private void NotifyChanged()
        {
            if (_data != null) Persistence.Save(_data);

            TaskBoardWindow.ReloadAllOpenWindows();

            if (_onChanged != null)
            {
                _onChanged.Invoke();
            }
        }

        private void HandleImageDragDrop(Rect rect, Action<Texture2D, string> onImageDropped)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

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
                        string assetPath = MarkdownRenderer.CopyImageToProject(p);
                        
                        // If it's a new asset, LoadAssetAtPath might fail until Refresh.
                        // We pass the path so caller can try to get GUID or force import.
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                        onImageDropped?.Invoke(tex, assetPath);
                    }
                    evt.Use();
                }
            }
        }

        private void HandleImagePaste(Rect rect, Action<Texture2D, string> onImagePasted)
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.V && (evt.control || evt.command)))
            {
                if (rect.Contains(evt.mousePosition))
                {
                    string assetPath = MarkdownRenderer.TryPasteImageToProject();
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                        onImagePasted?.Invoke(tex, assetPath);
                        evt.Use();
                    }
                }
            }
        }

        private void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}
