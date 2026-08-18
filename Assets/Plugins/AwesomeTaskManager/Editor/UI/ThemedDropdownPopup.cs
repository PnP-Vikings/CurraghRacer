using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Custom themed popup menu window that renders dropdown options matching the active theme colors.
    /// </summary>
    public class ThemedDropdownPopup : PopupWindowContent
    {
        private readonly string[] _options;
        private readonly int _selectedIndex;
        private readonly Action<int> _onSelect;
        private readonly Color[] _optionColors;
        private readonly string[] _optionIcons;
        private readonly float _customWidth;
        private Vector2 _scrollPosition;
        private string _searchFilter = "";

        public ThemedDropdownPopup(string[] options, int selectedIndex, Action<int> onSelect, float customWidth = 0f, Color[] optionColors = null, string[] optionIcons = null)
        {
            _options = options ?? Array.Empty<string>();
            _selectedIndex = selectedIndex;
            _onSelect = onSelect;
            _customWidth = customWidth;
            _optionColors = optionColors;
            _optionIcons = optionIcons;
        }

        public static void Show(Rect activatorRect, string[] options, int selectedIndex, Action<int> onSelect, Color[] optionColors = null, string[] optionIcons = null, float customWidth = 0f)
        {
            if (activatorRect.width <= 0f) activatorRect.width = 170f;
            if (activatorRect.height <= 0f) activatorRect.height = 20f;
            float width = customWidth > 0 ? customWidth : Mathf.Max(activatorRect.width, 170f);
            PopupWindow.Show(activatorRect, new ThemedDropdownPopup(options, selectedIndex, onSelect, width, optionColors, optionIcons));
        }

        public override Vector2 GetWindowSize()
        {
            int count = _options.Length;
            float searchHeight = 28f;
            float itemHeight = 22f;
            float totalHeight = Mathf.Clamp(count * itemHeight + searchHeight + 10f, 60f, 340f);
            return new Vector2(Mathf.Max(_customWidth, 180f), totalHeight);
        }

        public override void OnGUI(Rect rect)
        {
            try
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(rect, TBStyles.DropdownMenuBg, true);
                    TBStyles.DrawBorderRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.18f));
                }

                if (Event.current.type == EventType.MouseMove)
                {
                    editorWindow?.Repaint();
                }

                using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(4, 4, 4, 4) }))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _searchFilter = TBStyles.DrawThemedTextField(_searchFilter, TBStyles.ThemedSearchField, GUILayout.Height(20));
                        if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("✕", TBStyles.ToolbarButton, GUILayout.Width(18), GUILayout.Height(20)))
                        {
                            _searchFilter = "";
                        }
                    }
                    GUILayout.Space(2);

                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                    for (int i = 0; i < _options.Length; i++)
                    {
                        string option = _options[i];
                        if (!string.IsNullOrEmpty(_searchFilter) && !option.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant()))
                        {
                            continue;
                        }

                        bool isSelected = i == _selectedIndex;
                        Rect itemRect = GUILayoutUtility.GetRect(new GUIContent(option), TBStyles.DropdownMenuItem, GUILayout.Height(22), GUILayout.ExpandWidth(true));
                        bool isHovered = itemRect.Contains(Event.current.mousePosition);

                        if (Event.current.type == EventType.Repaint)
                        {
                            if (isHovered || isSelected)
                            {
                                EditorGUI.DrawRect(itemRect, TBStyles.DropdownMenuHoverBg);
                            }

                            Rect textRect = itemRect;
                            if (_optionColors != null && i < _optionColors.Length)
                            {
                                Rect swatchRect = new Rect(itemRect.x + 6, itemRect.y + 4, 14, 14);
                                EditorGUI.DrawRect(swatchRect, _optionColors[i]);
                                textRect.x += 20;
                                textRect.width -= 20;
                            }

                            Color textColor = (isHovered || isSelected) ? TBStyles.DropdownMenuHoverText : TBStyles.DropdownMenuText;
                            var labelStyle = new GUIStyle(EditorStyles.label)
                            {
                                alignment = TextAnchor.MiddleLeft,
                                fontSize = 11,
                                normal = { textColor = textColor }
                            };

                            string prefix = isSelected ? "✓ " : "   ";
                            if (_optionIcons != null && i < _optionIcons.Length && !string.IsNullOrEmpty(_optionIcons[i]))
                            {
                                prefix = isSelected ? $"✓ {_optionIcons[i]} " : $"   {_optionIcons[i]} ";
                            }

                            GUI.Label(new Rect(textRect.x + 4, textRect.y, textRect.width - 8, textRect.height), prefix + option, labelStyle);
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
                        {
                            int chosenIndex = i;
                            _onSelect?.Invoke(chosenIndex);
                            editorWindow?.Close();
                            Event.current.Use();
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(editorWindow);
            }
        }

        private void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }
    }
}
