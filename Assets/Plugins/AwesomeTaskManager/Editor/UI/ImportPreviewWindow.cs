using System;
using System.Collections.Generic;
using AwesomeTaskManager.Data;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    public sealed class ImportPreviewResult
    {
        public BoardImportExport.ImportConflictMode mode;
        public string newName;
    }

    public class ImportPreviewWindow : EditorWindow
    {
        private BoardImportExport.ImportPlan _plan;
        private Action<ImportPreviewResult> _onConfirm;
        private BoardImportExport.ImportConflictMode _mode;
        private string _newName;
        private Vector2 _scroll;
        private readonly List<bool> _expandedColumns = new List<bool>();

        public static void Open(string title, BoardImportExport.ImportPlan plan, Action<ImportPreviewResult> onConfirm)
        {
            if (plan == null) return;
            var window = CreateInstance<ImportPreviewWindow>();
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(480, 420);
            window._plan = plan;
            window._onConfirm = onConfirm;
            window._mode = plan.hasConflict ? BoardImportExport.ImportConflictMode.Merge : BoardImportExport.ImportConflictMode.CreateNew;
            window._newName = plan.suggestedCopyName ?? plan.sourceName ?? string.Empty;
            for (int i = 0; i < plan.columns.Count; i++) window._expandedColumns.Add(true);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            using (var scrollbarTheme = ThemedScrollbars.Begin())
            {
                if (_plan == null)
                {
                    Close();
                    return;
                }

                if (Event.current.type == EventType.Repaint)
                    TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.PopupBg, true);

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Import Preview", EditorStyles.boldLabel);
                    string itemName = string.IsNullOrWhiteSpace(_plan.sourceName) ? "Imported item" : _plan.sourceName;
                    EditorGUILayout.LabelField("Source: " + itemName, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.HelpBox(_plan.totalCards + " tasks  ·  " + _plan.columns.Count + " columns  ·  " +
                                            _plan.matchingCards + " matching tasks  ·  " + _plan.matchingColumns + " matching columns  ·  " +
                                            _plan.newColumns + " new columns", MessageType.None);
                }

                if (_plan.hasConflict)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.HelpBox(_plan.conflictText ?? "This import matches existing content.", MessageType.Warning);
                    bool mergeSelected = _mode == BoardImportExport.ImportConflictMode.Merge;
                    if (EditorGUILayout.ToggleLeft("Merge into existing", mergeSelected))
                        _mode = BoardImportExport.ImportConflictMode.Merge;
                    bool createSelected = _mode == BoardImportExport.ImportConflictMode.CreateNew;
                    if (EditorGUILayout.ToggleLeft("Create a new copy", createSelected))
                        _mode = BoardImportExport.ImportConflictMode.CreateNew;
                    if (_mode == BoardImportExport.ImportConflictMode.CreateNew)
                    {
                        EditorGUILayout.LabelField("New variant name");
                        _newName = TBStyles.DrawThemedTextField(_newName ?? string.Empty, GUILayout.Height(22));
                    }
                }

                EditorGUILayout.Space(5);
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scroll.scrollPosition;
                    for (int i = 0; i < _plan.columns.Count; i++)
                    {
                        BoardImportExport.ImportPlanColumn column = _plan.columns[i];
                        if (column == null) continue;
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            while (_expandedColumns.Count <= i) _expandedColumns.Add(true);
                            _expandedColumns[i] = EditorGUILayout.Foldout(_expandedColumns[i],
                                (string.IsNullOrWhiteSpace(column.name) ? "Tasks" : column.name) +
                                "  ·  " + column.cards.Count + " task(s)" + (column.exists ? "  ·  Existing column" : string.Empty), true);
                            if (!_expandedColumns[i]) continue;
                            foreach (BoardImportExport.ImportPlanCard card in column.cards)
                            {
                                if (card == null) continue;
                                bool willUpdate = _mode == BoardImportExport.ImportConflictMode.Merge && card.exists;
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField(card.title ?? "Untitled", EditorStyles.boldLabel);
                                        GUILayout.Label(willUpdate ? "Update" : "New", EditorStyles.miniLabel, GUILayout.Width(42));
                                    }
                                    if (!string.IsNullOrWhiteSpace(card.details))
                                        EditorGUILayout.LabelField(card.details, EditorStyles.miniLabel);
                                }
                            }
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cancel", TBStyles.StandardButton, GUILayout.Height(26)))
                    {
                        Close();
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledGroupScope(_plan.hasConflict && _mode == BoardImportExport.ImportConflictMode.CreateNew && string.IsNullOrWhiteSpace(_newName)))
                    {
                        if (GUILayout.Button("Import", TBStyles.AddCardButton, GUILayout.Height(26)))
                        {
                            Action<ImportPreviewResult> onConfirm = _onConfirm;
                            var result = new ImportPreviewResult
                            {
                                mode = _mode,
                                newName = string.IsNullOrWhiteSpace(_newName) ? _plan.sourceName : _newName.Trim()
                            };
                            Close();
                            try
                            {
                                onConfirm?.Invoke(result);
                            }
                            finally
                            {
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
                EditorGUILayout.Space(8);
            }
        }
    }
}