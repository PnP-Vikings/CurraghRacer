using System;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Custom themed modal dialog window matching the active Awesome Task Manager theme.
    /// Replaces EditorUtility.DisplayDialog and EditorUtility.DisplayDialogComplex with full theme customization.
    /// </summary>
    public class ThemedDialog : EditorWindow
    {
        private string _titleText = "";
        private string _messageText = "";
        private string _okText = "OK";
        private string _cancelText = "";
        private string _altText = "";
        private bool _isComplex;
        private int _choiceResult; // 0 = ok, 1 = cancel, 2 = alt
        private Vector2 _scrollPos;

        /// <summary>
        /// Displays a themed modal dialog with OK and optional Cancel buttons.
        /// </summary>
        public static bool Show(string title, string message, string ok = "OK", string cancel = "")
        {
            var dialog = CreateInstance<ThemedDialog>();
            dialog._titleText = title;
            dialog._messageText = message;
            dialog._okText = string.IsNullOrEmpty(ok) ? "OK" : ok;
            dialog._cancelText = cancel ?? "";
            dialog._altText = "";
            dialog._isComplex = false;
            dialog._choiceResult = string.IsNullOrEmpty(cancel) ? 0 : 1;

            dialog.titleContent = new GUIContent(title);
            dialog.ConfigureDimensionsAndPosition();
            dialog.ShowModal();

            return dialog._choiceResult == 0;
        }

        /// <summary>
        /// Displays a themed 3-button modal dialog (OK = 0, Cancel = 1, Alt = 2).
        /// </summary>
        public static int ShowComplex(string title, string message, string ok, string cancel, string alt)
        {
            var dialog = CreateInstance<ThemedDialog>();
            dialog._titleText = title;
            dialog._messageText = message;
            dialog._okText = string.IsNullOrEmpty(ok) ? "OK" : ok;
            dialog._cancelText = string.IsNullOrEmpty(cancel) ? "Cancel" : cancel;
            dialog._altText = string.IsNullOrEmpty(alt) ? "Alt" : alt;
            dialog._isComplex = true;
            dialog._choiceResult = 1; // Default to cancel if closed

            dialog.titleContent = new GUIContent(title);
            dialog.ConfigureDimensionsAndPosition();
            dialog.ShowModal();

            return dialog._choiceResult;
        }

        private void ConfigureDimensionsAndPosition()
        {
            float width = 410f;
            var msgStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 12
            };

            float msgHeight = msgStyle.CalcHeight(new GUIContent(_messageText), width - 36f);
            float totalHeight = Mathf.Clamp(msgHeight + 115f, 145f, 480f);

            minSize = maxSize = new Vector2(width, totalHeight);

            try
            {
                var mainPos = EditorGUIUtility.GetMainWindowPosition();
                var center = new Vector2(mainPos.x + mainPos.width * 0.5f, mainPos.y + mainPos.height * 0.5f);
                position = new Rect(center.x - width * 0.5f, center.y - totalHeight * 0.5f, width, totalHeight);
            }
            catch
            {
                // Fallback to default positioning
            }
        }

        private string GetDialogIcon()
        {
            string combined = (_titleText + " " + _messageText).ToLowerInvariant();
            if (combined.Contains("delete") || combined.Contains("remove") || combined.Contains("clear") || combined.Contains("discard"))
            {
                return !string.IsNullOrEmpty(TBStyles.DeleteIcon) ? TBStyles.DeleteIcon : "🗑️";
            }
            if (combined.Contains("url") || combined.Contains("link") || combined.Contains("browser") || combined.Contains("http"))
            {
                return !string.IsNullOrEmpty(TBStyles.UrlIcon) ? TBStyles.UrlIcon : "🔗";
            }
            if (combined.Contains("scene"))
            {
                return "🎬";
            }
            if (combined.Contains("unsaved") || combined.Contains("warning") || combined.Contains("error") || combined.Contains("cannot") || combined.Contains("failed") || combined.Contains("required"))
            {
                return "⚠️";
            }
            if (combined.Contains("export") || combined.Contains("import"))
            {
                return "📦";
            }
            if (combined.Contains("reset") || combined.Contains("preset") || combined.Contains("theme"))
            {
                return "🎨";
            }
            return "💬";
        }

        private bool IsDestructive(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string lower = text.Trim().ToLowerInvariant();
            return lower == "delete" || lower == "remove" || lower == "clear" || lower == "discard" || lower == "reset";
        }

        private bool IsPrimaryAction(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string lower = text.Trim().ToLowerInvariant();
            return lower == "save" || lower == "create" || lower == "add" || lower == "apply" || lower == "export" || lower == "import" || lower == "ok";
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            try
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.PopupBg, true);
                    TBStyles.DrawBorderRect(new Rect(0, 0, position.width, position.height),
                        EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.18f));
                }

                // Keyboard navigation
                bool enterPressed = Event.current.type == EventType.KeyDown &&
                                   (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                bool escapePressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

                if (escapePressed)
                {
                    _choiceResult = string.IsNullOrEmpty(_cancelText) ? 0 : 1;
                    Close();
                    Event.current.Use();
                    return;
                }

                if (enterPressed)
                {
                    _choiceResult = 0;
                    Close();
                    Event.current.Use();
                    return;
                }

                using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(16, 16, 14, 14) }))
                {
                    // Header row
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string icon = GetDialogIcon();
                        var headerIconStyle = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 18,
                            alignment = TextAnchor.MiddleLeft,
                            fixedWidth = 28,
                            fixedHeight = 24
                        };
                        EditorGUILayout.LabelField(icon, headerIconStyle, GUILayout.Width(28), GUILayout.Height(24));

                        var headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 13,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = TBStyles.BoardHeaderColor }
                        };
                        EditorGUILayout.LabelField(_titleText, headerTitleStyle, GUILayout.Height(24));
                    }

                    GUILayout.Space(6);
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1),
                        EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.12f));
                    GUILayout.Space(8);

                    // Message text body
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandHeight(true)))
                    {
                        _scrollPos = scroll.scrollPosition;
                        var msgStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                        {
                            fontSize = 12,
                            richText = true,
                            normal = { textColor = TBStyles.CardTextColor }
                        };
                        EditorGUILayout.LabelField(_messageText, msgStyle);
                    }

                    GUILayout.Space(12);

                    // Buttons row
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (_isComplex)
                        {
                            // Alt button
                            var altBtnStyle = IsDestructive(_altText) ? TBStyles.DeleteButton : TBStyles.StandardButton;
                            if (GUILayout.Button(_altText, altBtnStyle, GUILayout.Height(26), GUILayout.MinWidth(85)))
                            {
                                _choiceResult = 2;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                            GUILayout.Space(8);

                            // Cancel button
                            if (GUILayout.Button(_cancelText, TBStyles.StandardButton, GUILayout.Height(26), GUILayout.MinWidth(80)))
                            {
                                _choiceResult = 1;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                            GUILayout.Space(8);

                            // OK button
                            var okBtnStyle = IsDestructive(_okText) ? TBStyles.DeleteButton : (IsPrimaryAction(_okText) ? TBStyles.AddCardButton : TBStyles.StandardButton);
                            if (GUILayout.Button(_okText, okBtnStyle, GUILayout.Height(26), GUILayout.MinWidth(85)))
                            {
                                _choiceResult = 0;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                        else if (!string.IsNullOrEmpty(_cancelText))
                        {
                            // Cancel button
                            if (GUILayout.Button(_cancelText, TBStyles.StandardButton, GUILayout.Height(26), GUILayout.MinWidth(80)))
                            {
                                _choiceResult = 1;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                            GUILayout.Space(8);

                            // OK / Action button
                            var okBtnStyle = IsDestructive(_okText) ? TBStyles.DeleteButton : (IsPrimaryAction(_okText) ? TBStyles.AddCardButton : TBStyles.StandardButton);
                            if (GUILayout.Button(_okText, okBtnStyle, GUILayout.Height(26), GUILayout.MinWidth(85)))
                            {
                                _choiceResult = 0;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            // Single OK button
                            var okBtnStyle = IsDestructive(_okText) ? TBStyles.DeleteButton : (IsPrimaryAction(_okText) ? TBStyles.AddCardButton : TBStyles.StandardButton);
                            if (GUILayout.Button(_okText, okBtnStyle, GUILayout.Height(26), GUILayout.MinWidth(80)))
                            {
                                _choiceResult = 0;
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
            finally
            {
                // Draw custom themed tooltip overlay
                ThemedTooltip.Draw(this);
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
