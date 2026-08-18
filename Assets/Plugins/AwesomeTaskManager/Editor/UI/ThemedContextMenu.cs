using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Custom themed context menu replacing Unity's generic native OS context menu
    /// with active theme colors, icons, hierarchy drill-down navigation, search filtering, and checkmarks.
    /// </summary>
    public class ThemedContextMenu
    {
        public class MenuItem
        {
            public string fullPath = "";
            public string menuPath = "";
            public string displayName = "";
            public GUIContent content;
            public bool isSeparator;
            public bool isChecked;
            public bool isDisabled;
            public Action action;
            public Texture icon;
            public Color? swatchColor;
        }

        private readonly List<MenuItem> _items = new List<MenuItem>();

        public IReadOnlyList<MenuItem> Items => _items;

        public void AddItem(GUIContent content, bool on, Action action)
        {
            string rawPath = content != null ? content.text : "";
            ParsePath(rawPath, out string menuPath, out string displayName);

            _items.Add(new MenuItem
            {
                fullPath = rawPath,
                menuPath = menuPath,
                displayName = displayName,
                content = content ?? new GUIContent(displayName),
                isChecked = on,
                isDisabled = false,
                action = action,
                icon = content?.image
            });
        }

        public void AddItem(GUIContent content, bool on, Action<object> action, object userData)
        {
            AddItem(content, on, action != null ? (Action)(() => action(userData)) : null);
        }

        public void AddDisabledItem(GUIContent content, bool on = false)
        {
            string rawPath = content != null ? content.text : "";
            ParsePath(rawPath, out string menuPath, out string displayName);

            _items.Add(new MenuItem
            {
                fullPath = rawPath,
                menuPath = menuPath,
                displayName = displayName,
                content = content ?? new GUIContent(displayName),
                isChecked = on,
                isDisabled = true,
                action = null,
                icon = content?.image
            });
        }

        public void AddSeparator(string path = "")
        {
            string cleanPath = (path ?? "").TrimEnd('/');
            _items.Add(new MenuItem
            {
                fullPath = cleanPath,
                menuPath = cleanPath,
                displayName = "",
                isSeparator = true
            });
        }

        public static bool DropdownButton(GUIContent content, GUIStyle style, out Rect buttonRect, params GUILayoutOption[] layoutOptions)
        {
            string tooltip = content != null ? content.tooltip : null;
            var cleanContent = (content == null || string.IsNullOrEmpty(tooltip)) ? content : new GUIContent(content.text, content.image);
            buttonRect = GUILayoutUtility.GetRect(cleanContent, style, layoutOptions);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ThemedTooltip.SetTooltip(buttonRect, tooltip);
            }
            return EditorGUI.DropdownButton(buttonRect, cleanContent, FocusType.Passive, style);
        }

        public static bool DropdownButton(string text, GUIStyle style, out Rect buttonRect, params GUILayoutOption[] layoutOptions)
        {
            return DropdownButton(new GUIContent(text), style, out buttonRect, layoutOptions);
        }

        public static bool DropdownButton(string text, string tooltip, GUIStyle style, out Rect buttonRect, params GUILayoutOption[] layoutOptions)
        {
            var content = new GUIContent(text);
            buttonRect = GUILayoutUtility.GetRect(content, style, layoutOptions);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ThemedTooltip.SetTooltip(buttonRect, tooltip);
            }
            return EditorGUI.DropdownButton(buttonRect, content, FocusType.Passive, style);
        }

        public static bool DropdownButton(Texture icon, string tooltip, GUIStyle style, out Rect buttonRect, params GUILayoutOption[] layoutOptions)
        {
            var content = new GUIContent(icon);
            buttonRect = GUILayoutUtility.GetRect(content, style, layoutOptions);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ThemedTooltip.SetTooltip(buttonRect, tooltip);
            }
            return EditorGUI.DropdownButton(buttonRect, content, FocusType.Passive, style);
        }

        public void Show(Rect activatorRect, float customWidth = 0f)
        {
            if (activatorRect.width <= 0f || activatorRect.height <= 0f || (activatorRect.x == 0 && activatorRect.y == 0))
            {
                if (Event.current != null && Event.current.mousePosition != Vector2.zero)
                {
                    activatorRect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1, 1);
                }
                else
                {
                    if (activatorRect.width <= 0f) activatorRect.width = 160f;
                    if (activatorRect.height <= 0f) activatorRect.height = 20f;
                }
            }
            PopupWindow.Show(activatorRect, new ThemedContextMenuPopup(this, customWidth));
        }

        public void ShowAsContext(float customWidth = 0f)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            Rect activator = new Rect(mouse.x, mouse.y, 1, 1);
            PopupWindow.Show(activator, new ThemedContextMenuPopup(this, customWidth));
        }

        public void DropDown(Rect position)
        {
            Show(position);
        }

        private static void ParsePath(string rawPath, out string menuPath, out string displayName)
        {
            if (string.IsNullOrEmpty(rawPath))
            {
                menuPath = "";
                displayName = "";
                return;
            }

            int lastSlash = rawPath.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                menuPath = rawPath.Substring(0, lastSlash).Trim();
                displayName = rawPath.Substring(lastSlash + 1).Trim();
            }
            else
            {
                menuPath = "";
                displayName = rawPath.Trim();
            }
        }
    }

    /// <summary>
    /// Popup window content rendering a ThemedContextMenu with drill-down hierarchy and search bar.
    /// </summary>
    public class ThemedContextMenuPopup : PopupWindowContent
    {
        private readonly ThemedContextMenu _menu;
        private readonly float _customWidth;
        private string _currentSubmenuPath = "";
        private string _searchFilter = "";
        private Vector2 _scrollPosition = Vector2.zero;

        public ThemedContextMenuPopup(ThemedContextMenu menu, float customWidth = 0f)
        {
            _menu = menu;
            _customWidth = customWidth;
        }

        public override Vector2 GetWindowSize()
        {
            float width = _customWidth > 0 ? _customWidth : 220f;
            
            // Check longest text
            foreach (var item in _menu.Items)
            {
                if (!item.isSeparator && !string.IsNullOrEmpty(item.displayName))
                {
                    float estimatedWidth = item.displayName.Length * 7.5f + 60f;
                    if (estimatedWidth > width) width = Mathf.Min(estimatedWidth, 340f);
                }
            }

            int visibleCount = 0;
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                visibleCount = _menu.Items.Count(i => !i.isSeparator && i.fullPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else
            {
                string prefix = string.IsNullOrEmpty(_currentSubmenuPath) ? "" : _currentSubmenuPath + "/";
                var uniqueFolders = new HashSet<string>();
                foreach (var it in _menu.Items)
                {
                    if (it.menuPath == _currentSubmenuPath) visibleCount++;
                    else if (it.menuPath.StartsWith(prefix))
                    {
                        string rest = it.menuPath.Substring(prefix.Length);
                        string directFolder = rest.Split('/')[0];
                        if (uniqueFolders.Add(directFolder)) visibleCount++;
                    }
                }
            }

            float headerHeight = string.IsNullOrEmpty(_currentSubmenuPath) ? 36f : 64f;
            float totalHeight = Mathf.Clamp(visibleCount * 23f + headerHeight + 12f, 80f, 380f);

            return new Vector2(width, totalHeight);
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
                    // Search bar
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _searchFilter = TBStyles.DrawThemedTextField(_searchFilter, TBStyles.ThemedSearchField, GUILayout.Height(20));
                        if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("✕", TBStyles.ToolbarButton, GUILayout.Width(18), GUILayout.Height(20)))
                        {
                            _searchFilter = "";
                        }
                    }
                    GUILayout.Space(3);

                    if (!string.IsNullOrEmpty(_searchFilter))
                    {
                        DrawSearchResults();
                    }
                    else
                    {
                        // Back button / Submenu header if inside a subfolder
                        if (!string.IsNullOrEmpty(_currentSubmenuPath))
                        {
                            DrawSubmenuHeader();
                        }

                        DrawCurrentFolderItems();
                    }
                }
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(editorWindow);
            }
        }

        private void DrawSubmenuHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ Back", TBStyles.ToolbarButton, GUILayout.Width(54), GUILayout.Height(20)))
                {
                    int lastSlash = _currentSubmenuPath.LastIndexOf('/');
                    _currentSubmenuPath = lastSlash >= 0 ? _currentSubmenuPath.Substring(0, lastSlash) : "";
                    _scrollPosition = Vector2.zero;
                    GUIUtility.ExitGUI();
                }

                string currentTitle = _currentSubmenuPath.Split('/').Last();
                var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    normal = { textColor = TBStyles.DropdownMenuText }
                };
                EditorGUILayout.LabelField(currentTitle, headerStyle);
            }

            var sepRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(sepRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.12f));
            }
            GUILayout.Space(2);
        }

        private void DrawSearchResults()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var matches = _menu.Items.Where(i => !i.isSeparator && i.fullPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (matches.Count == 0)
            {
                var noMatchesStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
                GUILayout.Space(12);
                EditorGUILayout.LabelField("No matches found", noMatchesStyle);
            }
            else
            {
                foreach (var item in matches)
                {
                    string breadcrumb = item.fullPath.Replace("/", " ▸ ");
                    DrawMenuItemRow(item, breadcrumb, false, null);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCurrentFolderItems()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            string prefix = string.IsNullOrEmpty(_currentSubmenuPath) ? "" : _currentSubmenuPath + "/";
            var renderedSubfolders = new HashSet<string>();

            for (int i = 0; i < _menu.Items.Count; i++)
            {
                var item = _menu.Items[i];

                if (item.isSeparator)
                {
                    if (item.menuPath == _currentSubmenuPath)
                    {
                        GUILayout.Space(3);
                        var sRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                        if (Event.current.type == EventType.Repaint)
                        {
                            EditorGUI.DrawRect(sRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.12f));
                        }
                        GUILayout.Space(3);
                    }
                    continue;
                }

                // Direct action item at current level
                if (item.menuPath == _currentSubmenuPath)
                {
                    DrawMenuItemRow(item, item.displayName, false, null);
                }
                // Subfolder item
                else if (item.menuPath.StartsWith(prefix))
                {
                    string rest = item.menuPath.Substring(prefix.Length);
                    string immediateSubfolder = rest.Split('/')[0];
                    string fullSubfolderPath = string.IsNullOrEmpty(prefix) ? immediateSubfolder : prefix + immediateSubfolder;

                    if (renderedSubfolders.Add(immediateSubfolder))
                    {
                        DrawSubmenuRow(immediateSubfolder, fullSubfolderPath);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSubmenuRow(string subfolderName, string fullSubfolderPath)
        {
            Rect itemRect = GUILayoutUtility.GetRect(new GUIContent(subfolderName), TBStyles.DropdownMenuItem, GUILayout.Height(22), GUILayout.ExpandWidth(true));
            bool isHovered = itemRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (isHovered)
                {
                    EditorGUI.DrawRect(itemRect, TBStyles.DropdownMenuHoverBg);
                }

                Color textColor = isHovered ? TBStyles.DropdownMenuHoverText : TBStyles.DropdownMenuText;
                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    normal = { textColor = textColor }
                };

                // Label + folder icon
                GUI.Label(new Rect(itemRect.x + 8, itemRect.y, itemRect.width - 28, itemRect.height), subfolderName, labelStyle);

                // Submenu arrow ▸
                var arrowStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = textColor }
                };
                GUI.Label(new Rect(itemRect.xMax - 22, itemRect.y, 16, itemRect.height), "▸", arrowStyle);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
            {
                _currentSubmenuPath = fullSubfolderPath;
                _scrollPosition = Vector2.zero;
                Event.current.Use();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawMenuItemRow(ThemedContextMenu.MenuItem item, string displayLabel, bool isSubmenu, Action onSubmenuClick)
        {
            Rect itemRect = GUILayoutUtility.GetRect(new GUIContent(displayLabel), TBStyles.DropdownMenuItem, GUILayout.Height(22), GUILayout.ExpandWidth(true));
            bool isHovered = itemRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (isHovered && !item.isDisabled)
                {
                    EditorGUI.DrawRect(itemRect, TBStyles.DropdownMenuHoverBg);
                }

                Color textColor;
                if (item.isDisabled)
                {
                    textColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.55f) : new Color(0.55f, 0.55f, 0.55f, 0.65f);
                }
                else if (isHovered)
                {
                    textColor = TBStyles.DropdownMenuHoverText;
                }
                else
                {
                    textColor = TBStyles.DropdownMenuText;
                }

                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    normal = { textColor = textColor }
                };

                Rect textRect = itemRect;
                textRect.x += 6;
                textRect.width -= 12;

                string prefix = item.isChecked ? "✓ " : "   ";
                if (item.icon != null)
                {
                    Rect iconRect = new Rect(textRect.x + 14, textRect.y + 3, 16, 16);
                    GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                    textRect.x += 20;
                    textRect.width -= 20;
                }

                GUI.Label(new Rect(textRect.x, textRect.y, textRect.width, textRect.height), prefix + displayLabel, labelStyle);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
            {
                if (!item.isDisabled)
                {
                    var act = item.action;
                    editorWindow?.Close();
                    if (act != null)
                    {
                        EditorApplication.delayCall += () => act();
                    }
                    Event.current.Use();
                }
            }
        }
    }
}
