using System.Collections.Generic;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    public class CategoryEditorWindow : EditorWindow
    {
        private SaveData _data;
        private System.Action _onChanged;
        private Vector2 _scroll;
        private string _newCategoryName = "";
        private int _newCategoryColor;
        private Dictionary<string, string> _renameBuffers = new Dictionary<string, string>();

        public static void Open(SaveData data, System.Action onChanged)
        {
            var window = GetWindow<CategoryEditorWindow>(true, "🏷 Category Editor", true);
            window._data = data;
            window._onChanged = onChanged;
            window.minSize = new Vector2(420, 360);
            window.ShowUtility();
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

                EditorGUILayout.LabelField("Category Manager", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Add, rename, delete categories and choose each category's default card color.", MessageType.None);
                GUILayout.Space(4);

                DrawAddCategorySection();
                GUILayout.Space(8);
                DrawSeparator();
                GUILayout.Space(8);
                DrawCategoryList();
            }
        }

        private void DrawAddCategorySection()
        {
            EditorGUILayout.LabelField("Add Category", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newCategoryName = EditorGUILayout.TextField(_newCategoryName);
                _newCategoryColor = EditorGUILayout.Popup(_newCategoryColor, TBStyles.LabelNames, GUILayout.Width(90));
                GUI.enabled = !string.IsNullOrWhiteSpace(_newCategoryName) && !_data.categories.Contains(_newCategoryName.Trim());
                if (GUILayout.Button(new GUIContent("Add", "Add a new category"), GUILayout.Width(54)))
                {
                    string categoryName = _newCategoryName.Trim();
                    _data.categories.Add(categoryName);
                    _data.SetCategoryColor(categoryName, _newCategoryColor);
                    _newCategoryName = "";
                    _newCategoryColor = 0;
                    NotifyChanged();
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
            }
        }

        private void DrawCategoryList()
        {
            EditorGUILayout.LabelField("Existing Categories", EditorStyles.boldLabel);

            if (_data.categories.Count == 0)
            {
                EditorGUILayout.HelpBox("No categories yet.", MessageType.Info);
                return;
            }

            bool shouldExitGUI = false;

            for (int i = 0; i < _data.categories.Count; i++)
            {
                string category = _data.categories[i];
                if (!_renameBuffers.ContainsKey(category))
                    _renameBuffers[category] = category;

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Rect colorRect = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14));
                        EditorGUI.DrawRect(colorRect, TBStyles.LabelColors[Mathf.Clamp(_data.GetCategoryColor(category), 0, TBStyles.LabelColors.Length - 1)]);

                        _renameBuffers[category] = EditorGUILayout.TextField(_renameBuffers[category]);

                        int newColor = EditorGUILayout.Popup(_data.GetCategoryColor(category), TBStyles.LabelNames, GUILayout.Width(90));
                        if (newColor != _data.GetCategoryColor(category))
                        {
                            _data.SetCategoryColor(category, newColor);
                            NotifyChanged();
                        }

                        GUI.enabled = !string.IsNullOrWhiteSpace(_renameBuffers[category].Trim())
                                      && _renameBuffers[category].Trim() != category
                                      && !_data.categories.Contains(_renameBuffers[category].Trim());
                        if (GUILayout.Button(new GUIContent("Rename", "Rename the selected category"), GUILayout.Width(62)))
                        {
                            string renamed = _renameBuffers[category].Trim();
                            if (_data.RenameCategory(category, renamed))
                            {
                                _renameBuffers.Remove(category);
                                _renameBuffers[renamed] = renamed;
                                NotifyChanged();
                                shouldExitGUI = true;
                            }
                        }
                        GUI.enabled = true;

                        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                        if (GUILayout.Button(new GUIContent("Delete", "Delete the selected category"), GUILayout.Width(54)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Category",
                                $"Delete category \"{category}\"?\nCards using it will be cleared to no category.",
                                "Delete", "Cancel"))
                            {
                                _data.DeleteCategory(category);
                                _renameBuffers.Remove(category);
                                NotifyChanged();
                                shouldExitGUI = true;
                            }
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
                GUILayout.Space(2);

                if (shouldExitGUI) break;
            }

            if (shouldExitGUI)
                GUIUtility.ExitGUI();
        }

        private void NotifyChanged()
        {
            _data.Normalize();
            _onChanged?.Invoke();
            Repaint();
        }

        private void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}

