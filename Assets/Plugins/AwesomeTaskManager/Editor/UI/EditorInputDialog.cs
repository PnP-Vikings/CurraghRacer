using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    public class EditorInputDialog : EditorWindow
    {
        private static int _dialogCounter = 0;
        private int _dialogSessionId;
        private string _value;
        private string _label;
        private string _secondValue;
        private string _secondLabel;
        private bool _confirmed;
        private bool _focused;

        public static string Show(string title, string label, string defaultValue = "")
        {
            return ShowInternal(title, label, defaultValue, null, null, out _);
        }

        public static string ShowWithSecondField(string title, string label, string defaultValue,
            string secondLabel, string secondDefault, out string secondValue)
        {
            return ShowInternal(title, label, defaultValue, secondLabel, secondDefault, out secondValue);
        }

        private static string ShowInternal(string title, string label, string defaultValue,
            string secondLabel, string secondDefault, out string secondValue)
        {
            var w = CreateInstance<EditorInputDialog>();
            w._dialogSessionId = ++_dialogCounter;
            w.titleContent = new GUIContent(title);
            w._label = label;
            w._value = defaultValue ?? "";
            w._secondLabel = secondLabel;
            w._secondValue = secondDefault ?? "";
            bool hasSecondField = !string.IsNullOrEmpty(secondLabel);
            float height = hasSecondField ? 160f : 110f;
            w.minSize = w.maxSize = new Vector2(340, height);
            w._confirmed = false;

            try
            {
                var mainPos = EditorGUIUtility.GetMainWindowPosition();
                var center = new Vector2(mainPos.x + mainPos.width * 0.5f, mainPos.y + mainPos.height * 0.5f);
                w.position = new Rect(center.x - 170f, center.y - height * 0.5f, 340f, height);
            }
            catch
            {
                // Fallback to default positioning
            }

            w.ShowModal();
            secondValue = w._confirmed && hasSecondField ? w._secondValue : null;
            return w._confirmed ? w._value : null;
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
        }

        private void OnDestroy()
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
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

                using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(14, 14, 12, 12) }))
                {
                    var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                    };
                    EditorGUILayout.LabelField(_label, labelStyle);
                    GUILayout.Space(4);

                    string controlName = $"InputDialogTextField_{_dialogSessionId}";
                    GUI.SetNextControlName(controlName);
                    _value = TBStyles.DrawThemedTextField(_value, GUILayout.Height(22));

                    if (!string.IsNullOrEmpty(_secondLabel))
                    {
                        GUILayout.Space(8);
                        EditorGUILayout.LabelField(_secondLabel, labelStyle);
                        string secondControlName = $"InputDialogSecondTextField_{_dialogSessionId}";
                        GUI.SetNextControlName(secondControlName);
                        _secondValue = TBStyles.DrawThemedTextField(_secondValue, GUILayout.Height(22));
                    }

                    if (!_focused)
                    {
                        _focused = true;
                        EditorGUI.FocusTextInControl(controlName);
                    }

                    bool enterPressed = Event.current.type == EventType.KeyDown &&
                                       (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                    bool escapePressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

                    if (escapePressed)
                    {
                        _confirmed = false;
                        GUIUtility.keyboardControl = 0;
                        GUIUtility.hotControl = 0;
                        GUI.FocusControl(null);
                        EditorGUIUtility.editingTextField = false;
                        Event.current.Use();
                        Close();
                        GUIUtility.ExitGUI();
                        return;
                    }

                    GUILayout.Space(10);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (ThemedTooltip.Button("OK", "Confirm", TBStyles.StandardButton, GUILayout.Height(24)) || enterPressed)
                        {
                            _confirmed = true;
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = 0;
                            GUI.FocusControl(null);
                            EditorGUIUtility.editingTextField = false;
                            if (enterPressed) Event.current.Use();
                            Close();
                            GUIUtility.ExitGUI();
                            return;
                        }
                        GUILayout.Space(6);
                        if (ThemedTooltip.Button("Cancel", "Cancel", TBStyles.StandardButton, GUILayout.Height(24)))
                        {
                            _confirmed = false;
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = 0;
                            GUI.FocusControl(null);
                            EditorGUIUtility.editingTextField = false;
                            Close();
                            GUIUtility.ExitGUI();
                            return;
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
    }
}
