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
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private string _newCategoryName = "";
        [SerializeField] private int _newCategoryColor;
        private Dictionary<string, string> _renameBuffers = new Dictionary<string, string>();

        public static void Open(SaveData data, System.Action onChanged)
        {
            var window = GetWindow<CategoryEditorWindow>(true, $"{TBStyles.CategoryIcon} Category Editor", true);
            window._data = data;
            window._onChanged = onChanged;
            window.minSize = new Vector2(420, 360);
            window.ShowUtility();
        }

        public void LoadData()
        {
            var freshData = Persistence.Load();
            if (freshData == null) return;
            _data = freshData;
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
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnDestroy()
        {
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
            try
            {
                if (_data == null)
                {
                    Close();
                    return;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.PopupBg, true);
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
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(this);
            }
        }

        private void DrawAddCategorySection()
        {
            EditorGUILayout.LabelField("Add Category", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newCategoryName = TBStyles.DrawThemedTextField(_newCategoryName, GUILayout.Height(22));
                TBStyles.DrawThemedDropdown(_newCategoryColor, TBStyles.LabelNames, (c) =>
                {
                    _newCategoryColor = c;
                    Repaint();
                }, TBStyles.StandardDropdown, TBStyles.GetLabelColorsArray(), "Select default color for this category", GUILayout.Width(90));
                GUI.enabled = !string.IsNullOrWhiteSpace(_newCategoryName) && !_data.categories.Contains(_newCategoryName.Trim());
                if (ThemedTooltip.Button("Add", "Add a new category", TBStyles.StandardButton, GUILayout.Width(54)))
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

                using (var cardScope = new EditorGUILayout.VerticalScope(TBStyles.CardBox))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        TBStyles.DrawGlassPanel(cardScope.rect, TBStyles.CardBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), true);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Rect colorRect = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14));
                        EditorGUI.DrawRect(colorRect, TBStyles.GetLabelColor(_data.GetCategoryColor(category)));

                        _renameBuffers[category] = TBStyles.DrawThemedTextField(_renameBuffers[category], GUILayout.Height(20));

                        string currentCat = category;
                        TBStyles.DrawThemedDropdown(_data.GetCategoryColor(category), TBStyles.LabelNames, (newColor) =>
                        {
                            if (newColor != _data.GetCategoryColor(currentCat))
                            {
                                _data.SetCategoryColor(currentCat, newColor);
                                NotifyChanged();
                            }
                        }, TBStyles.StandardDropdown, TBStyles.GetLabelColorsArray(), "Select category color", GUILayout.Width(90));

                        GUI.enabled = !string.IsNullOrWhiteSpace(_renameBuffers[category].Trim())
                                      && _renameBuffers[category].Trim() != category
                                      && !_data.categories.Contains(_renameBuffers[category].Trim());
                        if (ThemedTooltip.Button("Rename", "Rename the selected category", TBStyles.StandardButton, GUILayout.Width(62)))
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

                        if (ThemedTooltip.DeleteButton("Delete", "Delete the selected category", GUILayout.Width(54)))
                        {
                            if (ThemedDialog.Show("Delete Category",
                                $"Delete category \"{category}\"?\nCards using it will be cleared to no category.",
                                "Delete", "Cancel"))
                            {
                                _data.DeleteCategory(category);
                                _renameBuffers.Remove(category);
                                NotifyChanged();
                                shouldExitGUI = true;
                            }
                        }
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
            if (_data != null) Persistence.Save(_data);

            TaskBoardWindow.ReloadAllOpenWindows();

            if (_onChanged != null)
            {
                _onChanged.Invoke();
            }
            Repaint();
        }

        private void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}

