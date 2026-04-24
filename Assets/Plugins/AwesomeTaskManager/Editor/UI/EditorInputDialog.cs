using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    public class EditorInputDialog : EditorWindow
    {
        private string _value;
        private string _label;
        private static string _result;

        public static string Show(string title, string label, string defaultValue = "")
        {
            _result = null;
            var w = CreateInstance<EditorInputDialog>();
            w.titleContent = new GUIContent(title);
            w._label = label;
            w._value = defaultValue;
            w.minSize = w.maxSize = new Vector2(320, 90);
            w.ShowModal();
            return _result;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_label);
            _value = EditorGUILayout.TextField(_value);
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OK"))    { _result = _value; Close(); }
                if (GUILayout.Button("Cancel")) { _result = null;  Close(); }
            }
        }
    }
}