using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Custom themed context menu replacing Unity's generic native OS context menu
    /// with active theme colors, icons, hierarchy drill-down navigation, search filtering, shortcuts, and checkmarks.
    /// </summary>
    public class ThemedContextMenu
    {
        public class MenuItem
        {
            public string fullPath = "";
            public string menuPath = "";
            public string displayName = "";
            public string cleanLabel = "";
            public string shortcut = "";
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
        public Action OnClosed { get; set; }
        public bool FitContent { get; set; }

        public void AddItem(GUIContent content, bool on, Action action)
        {
            string rawPath = content != null ? content.text : "";
            ParsePathAndShortcut(rawPath, out string menuPath, out string displayName, out string cleanLabel, out string shortcut);

            _items.Add(new MenuItem
            {
                fullPath = rawPath,
                menuPath = menuPath,
                displayName = displayName,
                cleanLabel = cleanLabel,
                shortcut = shortcut,
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
            ParsePathAndShortcut(rawPath, out string menuPath, out string displayName, out string cleanLabel, out string shortcut);

            _items.Add(new MenuItem
            {
                fullPath = rawPath,
                menuPath = menuPath,
                displayName = displayName,
                cleanLabel = cleanLabel,
                shortcut = shortcut,
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
                cleanLabel = "",
                shortcut = "",
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

            try
            {
                PopupWindow.Show(activatorRect, new ThemedContextMenuPopup(this, customWidth));
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception)
            {
                // Headless test runner fallback
            }
        }

        public GenericMenu ToGenericMenu()
        {
            var menu = new GenericMenu();
            foreach (var item in _items)
            {
                if (item.isSeparator)
                {
                    menu.AddSeparator(item.menuPath ?? "");
                }
                else if (item.isDisabled)
                {
                    menu.AddDisabledItem(item.content ?? new GUIContent(item.displayName), item.isChecked);
                }
                else
                {
                    var act = item.action;
                    menu.AddItem(item.content ?? new GUIContent(item.displayName), item.isChecked, () => act?.Invoke());
                }
            }
            return menu;
        }

        public void ShowAsContext(float customWidth = 0f)
        {
            Rect activatorRect;
            if (Event.current != null && Event.current.mousePosition != Vector2.zero)
            {
                activatorRect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1, 1);
            }
            else
            {
                activatorRect = new Rect(100, 100, 1, 1);
            }

            try
            {
                Show(activatorRect, customWidth);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception)
            {
                // Headless test runner fallback
                ToGenericMenu().ShowAsContext();
            }
        }

        public void DropDown(Rect position)
        {
            Show(position);
        }

        private static void ParsePathAndShortcut(string rawPath, out string menuPath, out string displayName, out string cleanLabel, out string shortcut)
        {
            if (string.IsNullOrEmpty(rawPath))
            {
                menuPath = "";
                displayName = "";
                cleanLabel = "";
                shortcut = "";
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

            cleanLabel = displayName;
            shortcut = "";

            if (displayName.Contains('\t'))
            {
                var parts = displayName.Split('\t');
                cleanLabel = parts[0].Trim();
                shortcut = parts.Length > 1 ? parts[1].Trim() : "";
                return;
            }

            // Auto-detect common shortcuts
            bool isOSX = Application.platform == RuntimePlatform.OSXEditor;
            string testLabel = cleanLabel.ToLowerInvariant();
            if (testLabel == "cut" || testLabel.EndsWith(" cut") || testLabel.StartsWith("✂ cut"))
            {
                shortcut = isOSX ? "⌘X" : "Ctrl+X";
            }
            else if (testLabel == "copy" || testLabel.EndsWith(" copy") || testLabel.StartsWith("📋 copy"))
            {
                if (!testLabel.Contains("entire note") && !testLabel.Contains("content"))
                {
                    shortcut = isOSX ? "⌘C" : "Ctrl+C";
                }
            }
            else if (testLabel.Contains("smart paste"))
            {
                shortcut = isOSX ? "⌘⇧V" : "Ctrl+Shift+V";
            }
            else if (testLabel == "paste" || testLabel.EndsWith(" paste") || testLabel.StartsWith("📋 paste"))
            {
                shortcut = isOSX ? "⌘V" : "Ctrl+V";
            }
            else if (testLabel == "undo")
            {
                shortcut = isOSX ? "⌘Z" : "Ctrl+Z";
            }
            else if (testLabel == "redo")
            {
                shortcut = isOSX ? "⌘⇧Z" : "Ctrl+Y";
            }
        }
    }

    /// <summary>
    /// Popup window content rendering a ThemedContextMenu with drill-down hierarchy and shortcuts.
    /// </summary>
    public class ThemedContextMenuPopup : PopupWindowContent
    {
        private readonly ThemedContextMenu _menu;
        private readonly float _customWidth;
        private string _currentSubmenuPath = "";
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly GUIStyle _fitContentItemStyle;

        public ThemedContextMenuPopup(ThemedContextMenu menu, float customWidth = 0f)
        {
            _menu = menu;
            _customWidth = customWidth;
            _fitContentItemStyle = new GUIStyle(TBStyles.DropdownMenuItem)
            {
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        public override void OnClose()
        {
            _menu.OnClosed?.Invoke();
        }

        public override Vector2 GetWindowSize()
        {
            float width = _customWidth > 0 ? _customWidth : 200f;
            
            // Check longest text
            foreach (var item in _menu.Items)
            {
                if (!item.isSeparator && !string.IsNullOrEmpty(item.displayName))
                {
                    float estimatedWidth = item.displayName.Length * 7.5f + (!string.IsNullOrEmpty(item.shortcut) ? 65f : 35f) + 30f;
                    if (estimatedWidth > width) width = Mathf.Min(estimatedWidth, 340f);
                }
            }

            int visibleCount = 0;
            int separatorCount = 0;

            string prefix = string.IsNullOrEmpty(_currentSubmenuPath) ? "" : _currentSubmenuPath + "/";
            var uniqueFolders = new HashSet<string>();
            foreach (var it in _menu.Items)
            {
                if (it.isSeparator)
                {
                    if (it.menuPath == _currentSubmenuPath) separatorCount++;
                }
                else if (it.menuPath == _currentSubmenuPath)
                {
                    visibleCount++;
                }
                else if (it.menuPath.StartsWith(prefix))
                {
                    string rest = it.menuPath.Substring(prefix.Length);
                    string directFolder = rest.Split('/')[0];
                    if (uniqueFolders.Add(directFolder)) visibleCount++;
                }
            }

            float headerHeight = string.IsNullOrEmpty(_currentSubmenuPath) ? 0f : 28f;
            float contentHeight = visibleCount * 24f + separatorCount * 7f + headerHeight + 8f;
            float totalHeight = _menu.FitContent
                ? Mathf.Max(32f, contentHeight)
                : Mathf.Clamp(contentHeight, 32f, 420f);

            return new Vector2(width, totalHeight);
        }

        public override void OnGUI(Rect rect)
        {
            try
            {
                Color borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.20f);

                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(rect, TBStyles.DropdownMenuBg, true);
                    TBStyles.DrawBorderRect(rect, borderColor);

                    // Sleek accent top line matching active theme
                    Color accentColor = TBStyles.HeaderTabActiveBg;
                    if (accentColor.a < 0.1f)
                    {
                        accentColor = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.49f, 0.90f, 0.9f) : new Color(0.20f, 0.40f, 0.80f, 0.9f);
                    }
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), accentColor);
                }

                if (Event.current.type == EventType.MouseMove)
                {
                    editorWindow?.Repaint();
                }

                using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(4, 4, 4, 4) }))
                {
                    if (!string.IsNullOrEmpty(_currentSubmenuPath))
                    {
                        DrawSubmenuHeader();
                    }

                    DrawCurrentFolderItems();
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

        private void DrawCurrentFolderItems()
        {
            if (!_menu.FitContent)
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
                        GUILayout.Space(2);
                        var sRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                        if (Event.current.type == EventType.Repaint)
                        {
                            EditorGUI.DrawRect(new Rect(sRect.x + 6, sRect.y, sRect.width - 12, 1), EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(0f, 0f, 0f, 0.12f));
                        }
                        GUILayout.Space(2);
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

            if (!_menu.FitContent)
                EditorGUILayout.EndScrollView();
        }

        private void DrawSubmenuRow(string subfolderName, string fullSubfolderPath)
        {
            Rect itemRect = GUILayoutUtility.GetRect(new GUIContent(subfolderName), GetMenuItemStyle(), GUILayout.Height(24), GUILayout.ExpandWidth(true));
            bool isHovered = itemRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (isHovered)
                {
                    Rect pillRect = new Rect(itemRect.x + 2, itemRect.y + 1, itemRect.width - 4, itemRect.height - 2);
                    EditorGUI.DrawRect(pillRect, TBStyles.DropdownMenuHoverBg);
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
            Rect itemRect = GUILayoutUtility.GetRect(new GUIContent(displayLabel), GetMenuItemStyle(), GUILayout.Height(24), GUILayout.ExpandWidth(true));
            bool isHovered = itemRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (isHovered && !item.isDisabled)
                {
                    Rect pillRect = new Rect(itemRect.x + 2, itemRect.y + 1, itemRect.width - 4, itemRect.height - 2);
                    EditorGUI.DrawRect(pillRect, TBStyles.DropdownMenuHoverBg);
                }

                Color textColor;
                if (item.isDisabled)
                {
                    textColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.45f) : new Color(0.55f, 0.55f, 0.55f, 0.55f);
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

                // Checkmark column
                if (item.isChecked)
                {
                    var checkStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 11,
                        normal = { textColor = isHovered ? TBStyles.DropdownMenuHoverText : (EditorGUIUtility.isProSkin ? new Color(0.35f, 0.65f, 1f) : new Color(0.15f, 0.45f, 0.9f)) }
                    };
                    GUI.Label(new Rect(textRect.x, textRect.y, 14, textRect.height), "✓", checkStyle);
                }

                textRect.x += 16;
                textRect.width -= 16;

                if (item.icon != null)
                {
                    Rect iconRect = new Rect(textRect.x, textRect.y + 4, 16, 16);
                    GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                    textRect.x += 20;
                    textRect.width -= 20;
                }

                float shortcutWidth = 0f;
                if (!string.IsNullOrEmpty(item.shortcut))
                {
                    shortcutWidth = 60f;
                    textRect.width -= shortcutWidth;
                }

                GUI.Label(textRect, displayLabel, labelStyle);

                if (!string.IsNullOrEmpty(item.shortcut))
                {
                    var shortcutStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleRight,
                        fontSize = 10,
                        normal = { textColor = item.isDisabled ? textColor : (EditorGUIUtility.isProSkin ? new Color(0.65f, 0.65f, 0.65f, 0.65f) : new Color(0.4f, 0.4f, 0.4f, 0.75f)) }
                    };
                    Rect shortcutRect = new Rect(itemRect.xMax - shortcutWidth - 8, itemRect.y, shortcutWidth, itemRect.height);
                    GUI.Label(shortcutRect, item.shortcut, shortcutStyle);
                }
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

        private GUIStyle GetMenuItemStyle()
        {
            return _menu.FitContent ? _fitContentItemStyle : TBStyles.DropdownMenuItem;
        }
    }
}
