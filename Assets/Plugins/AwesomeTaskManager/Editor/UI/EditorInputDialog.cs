using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    public class EditorInputDialog : EditorWindow
    {
        private string _value;
        private string _label;
        private static string _result;
        private bool _focused;

        public static string Show(string title, string label, string defaultValue = "")
        {
            _result = null;
            var w = CreateInstance<EditorInputDialog>();
            w.titleContent = new GUIContent(title);
            w._label = label;
            w._value = defaultValue;
            w.minSize = w.maxSize = new Vector2(340, 110);
            w.ShowModal();
            return _result;
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

                using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(14, 14, 12, 12) }))
                {
                    var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                    };
                    EditorGUILayout.LabelField(_label, labelStyle);
                    GUILayout.Space(4);

                    GUI.SetNextControlName("InputDialogTextField");
                    _value = TBStyles.DrawThemedTextField(_value, GUILayout.Height(22));

                    if (!_focused)
                    {
                        _focused = true;
                        EditorGUI.FocusTextInControl("InputDialogTextField");
                    }

                    bool enterPressed = Event.current.type == EventType.KeyDown &&
                                       (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                    bool escapePressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

                    if (escapePressed)
                    {
                        _result = null;
                        Close();
                        Event.current.Use();
                        return;
                    }

                    GUILayout.Space(10);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (ThemedTooltip.Button("OK", "Confirm", TBStyles.StandardButton, GUILayout.Height(24)) || enterPressed)
                        {
                            _result = _value;
                            Close();
                            if (enterPressed) Event.current.Use();
                            return;
                        }
                        GUILayout.Space(6);
                        if (ThemedTooltip.Button("Cancel", "Cancel", TBStyles.StandardButton, GUILayout.Height(24)))
                        {
                            _result = null;
                            Close();
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
