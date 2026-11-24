using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    public sealed class NoteClipboardController
    {
        public struct SelectionSnapshot
        {
            public int caretIndex;
            public int selectIndex;
            public string selectedText;

            public bool HasSelection => !string.IsNullOrEmpty(selectedText);
            public int Start => Math.Min(caretIndex, selectIndex);
            public int End => Math.Max(caretIndex, selectIndex);
        }

        public sealed class Host
        {
            public Action<QuickNote> MarkModified;
            public Action<QuickNote> RecordUndo;
            public Action Repaint;
            public Action<string> Notify;
            public Func<QuickNote, bool> CanUndo;
            public Func<QuickNote, bool> CanRedo;
            public Action<QuickNote> Undo;
            public Action<QuickNote> Redo;
            public Action<bool> SetEditMode;
            public Action<QuickNote> AttachImage;
            public Action<QuickNote> PopoutNote;
            public Action<QuickNote> ExportNote;
            public string SwitchToPreviewLabel = "👁 Switch to Preview Mode";
            public string SwitchToEditLabel = "✏ Switch to Edit Mode";
        }

        private readonly string _controlName;
        private readonly GUIStyle _textAreaStyle;
        private readonly Host _host;
        private SelectionSnapshot _live;
        private SelectionSnapshot _snapshot;
        private SelectionSnapshot _previewSelection;
        private SelectionSnapshot _pendingRestore;
        private bool _hasPendingRestore;
        private int _pendingRestoreFrames;
        private bool _menuOpen;
        private Rect _lastRect;
        private QuickNote _currentNote;
        private int _lastSelectionControlId;
        private bool _rightClickPrepared;
        private TextEditor _fallbackTextEditor;

        private static FieldInfo s_RecycledEditorField;
        private static bool s_RecycledEditorFieldResolved;
        private static MethodInfo s_IsEditingControlMethod;
        private static bool s_IsEditingControlMethodResolved;
        private static readonly Regex s_RichTextTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);

        public NoteClipboardController(string controlName, GUIStyle textAreaStyle, Host host)
        {
            _controlName = controlName;
            _textAreaStyle = textAreaStyle;
            _host = host ?? new Host();
            _live = new SelectionSnapshot { caretIndex = -1, selectIndex = -1, selectedText = "" };
            _snapshot = _live;
            _previewSelection = _live;
            _pendingRestore = _live;
        }

        public SelectionSnapshot Live => _live;

        public int CaretIndex
        {
            get
            {
                int contentLength = NormalizeNewlines(_currentNote?.content).Length;
                return _live.caretIndex >= 0 && _live.caretIndex <= contentLength ? _live.caretIndex : contentLength;
            }
        }

        public void GetInsertTarget(QuickNote note, out int insertPosition, out int replaceLength)
        {
            _currentNote = note;
            insertPosition = ResolveInsertPosition(note?.content, _live, out replaceLength);
        }

        public void SetCaret(int caretIndex)
        {
            if (_currentNote == null) return;

            int clampedCaret = Mathf.Clamp(caretIndex, 0, NormalizeNewlines(_currentNote.content).Length);
            _live = CreateSnapshot(clampedCaret, clampedCaret, "");
            _snapshot = _live;
            RequestRestore(_live);
            SyncTextEditors(_currentNote.content, clampedCaret, _lastSelectionControlId);
            _host.Repaint?.Invoke();
        }

        public void Reset()
        {
            _live = new SelectionSnapshot { caretIndex = -1, selectIndex = -1, selectedText = "" };
            _snapshot = _live;
            _previewSelection = _live;
            _pendingRestore = _live;
            _hasPendingRestore = false;
            _pendingRestoreFrames = 0;
            _menuOpen = false;
            _rightClickPrepared = false;
            _lastRect = default;
            _currentNote = null;
            _lastSelectionControlId = 0;
            MarkdownRenderer.ClearTableSelection();
        }

        public void BeginTextArea(QuickNote note)
        {
            _currentNote = note;

            if (_hasPendingRestore)
            {
                GUI.FocusControl(_controlName);
                EditorGUIUtility.editingTextField = true;
            }

            GUI.SetNextControlName(_controlName);

            Event current = Event.current;
            if (current == null) return;

            if (current.type == EventType.MouseDown)
            {
                _rightClickPrepared = current.button == 1 && _lastRect.Contains(current.mousePosition);
                if (_rightClickPrepared)
                {
                    PrepareRightClick(note, current.mousePosition);
                }
            }

            if (current.type == EventType.ContextClick && _lastRect.Contains(current.mousePosition))
            {
                int clickIndex = GetClickIndex(note, current.mousePosition);
                if (!_rightClickPrepared)
                {
                    PrepareRightClick(note, current.mousePosition);
                }

                _snapshot = _live;
                if (_snapshot.caretIndex < 0)
                {
                    _snapshot = CreateSnapshot(clickIndex, clickIndex, "");
                }

                current.Use();
                _menuOpen = true;
                _host.Repaint?.Invoke();
                BuildContextMenu(note, true).ShowAsContext();
                _rightClickPrepared = false;
            }
        }

        public void EndTextArea(QuickNote note, Rect textAreaRect)
        {
            _currentNote = note;

            Event current = Event.current;
            if (current != null && current.type == EventType.Repaint && textAreaRect.width > 0f && textAreaRect.height > 0f)
            {
                _lastRect = textAreaRect;
            }

            bool isRightClickEvent = current != null &&
                                     ((current.type == EventType.MouseDown && current.button == 1) || current.type == EventType.ContextClick);
            bool activelyEditing = IsActivelyEditing();
            if (!_menuOpen && !isRightClickEvent && activelyEditing)
            {
                TextEditor editor = GetActiveTextEditor(note);
                if (editor != null)
                {
                    _lastSelectionControlId = editor.controlID;
                    _live = CreateSnapshot(editor.cursorIndex, editor.selectIndex, editor.SelectedText);
                    _snapshot = _live;
                }
            }

            if (current != null && current.type == EventType.Repaint)
            {
                SelectionSnapshot highlight = _menuOpen ? _snapshot : _live;
                if (highlight.HasSelection && (_menuOpen || _hasPendingRestore))
                {
                    DrawSelectionHighlight(textAreaRect, NormalizeNewlines(note?.content), highlight.Start, highlight.End, _textAreaStyle);
                }
            }

            if (_hasPendingRestore && current != null && current.type != EventType.Layout)
            {
                if (IsActivelyEditing())
                {
                    TextEditor editor = GetActiveTextEditor(note);
                    if (editor != null)
                    {
                        RestoreSelection(editor, _pendingRestore, note);
                        _live = _pendingRestore;
                        _snapshot = _pendingRestore;
                        _hasPendingRestore = false;
                        _pendingRestoreFrames = 0;
                        activelyEditing = true;
                        _host.Repaint?.Invoke();
                    }
                }
                else if (current != null && current.type == EventType.Repaint && ++_pendingRestoreFrames >= 3)
                {
                    _hasPendingRestore = false;
                    _pendingRestoreFrames = 0;
                }
            }

            if (!activelyEditing && !_menuOpen && !_hasPendingRestore && !_rightClickPrepared &&
                current != null && current.type == EventType.Repaint && _live.HasSelection)
            {
                int caret = _live.caretIndex >= 0 ? _live.caretIndex : _live.selectIndex;
                _live = CreateSnapshot(caret, caret, "");
                _snapshot = _live;
            }
        }

        public void TrackPreviewSelection(QuickNote note)
        {
            _currentNote = note;
            if (note == null || _menuOpen) return;

            if (MarkdownRenderer.TryGetTableSelectionText(note, out string tableSelection))
            {
                _previewSelection = CreateSnapshot(-1, -1, tableSelection);
                _live = _previewSelection;
                _snapshot = _live;
                return;
            }

            TextEditor editor = GetFocusedTextEditor();
            if (editor == null) return;

            string selectedText = StripRichTextTags(editor.SelectedText);
            _previewSelection = string.IsNullOrEmpty(selectedText)
                ? CreateSnapshot(editor.cursorIndex, editor.selectIndex, "")
                : CreateSnapshot(editor.cursorIndex, editor.selectIndex, selectedText);
            _live = _previewSelection;
            _snapshot = _live;
        }

        public void HandlePreviewContextClick(QuickNote note)
        {
            _currentNote = note;
            Event current = Event.current;
            if (current == null || current.type != EventType.ContextClick) return;

            if (MarkdownRenderer.TryGetTableSelectionText(note, out string tableSelection))
            {
                _snapshot = CreateSnapshot(-1, -1, tableSelection);
                _previewSelection = _snapshot;
                _live = _snapshot;
                current.Use();
                _menuOpen = true;
                _host.Repaint?.Invoke();
                BuildContextMenu(note, false).ShowAsContext();
                return;
            }

            TextEditor editor = GetFocusedTextEditor();

            string selectedText = editor != null ? StripRichTextTags(editor.SelectedText) : "";
            if (!string.IsNullOrEmpty(selectedText))
            {
                _snapshot = CreateSnapshot(editor.cursorIndex, editor.selectIndex, selectedText);
                _previewSelection = _snapshot;
                _live = _snapshot;
            }
            else if (_previewSelection.HasSelection)
            {
                _snapshot = _previewSelection;
                _live = _snapshot;
            }
            else
            {
                _snapshot = CreateSnapshot(-1, -1, "");
                _live = _snapshot;
            }

            current.Use();
            _menuOpen = true;
            _host.Repaint?.Invoke();
            BuildContextMenu(note, false).ShowAsContext();
        }

        public void HandleKeyboardShortcuts(QuickNote note, bool editMode)
        {
            _currentNote = note;
            Event current = Event.current;
            if (note == null || current == null || current.type != EventType.KeyDown || !(current.control || current.command)) return;

            if (current.keyCode == KeyCode.C && !IsActivelyEditing() && _live.HasSelection)
            {
                Copy(_live);
                current.Use();
                return;
            }

            if (editMode && current.keyCode == KeyCode.X && _live.HasSelection)
            {
                Cut(note, _live);
                current.Use();
                return;
            }

            if (editMode && current.keyCode == KeyCode.V)
            {
                string content = NormalizeNewlines(note.content);
                int insertPosition = ResolveInsertPosition(content, _live, out int replaceLength);
                _host.RecordUndo?.Invoke(note);
                if (MarkdownRenderer.TryPasteImageFromClipboard(note, null, null, insertPosition, replaceLength))
                {
                    string updatedContent = NormalizeNewlines(note.content);
                    int caret = CalculateSmartPasteCaret(insertPosition, content.Length, replaceLength, updatedContent.Length);
                    note.content = updatedContent;
                    CompleteMutation(note, caret, "");
                    current.Use();
                }
            }
        }

        public void Cut(QuickNote note, SelectionSnapshot selection)
        {
            if (note == null || !TryResolveSelectionRange(note.content, selection, out int start, out int length)) return;

            string content = NormalizeNewlines(note.content);
            string textToCut = content.Substring(start, length);
            _host.RecordUndo?.Invoke(note);
            GUIUtility.systemCopyBuffer = textToCut;
            EditorGUIUtility.systemCopyBuffer = textToCut;
            note.content = content.Remove(start, length);
            CompleteMutation(note, start, "Cut to clipboard");
        }

        public void Copy(SelectionSnapshot selection)
        {
            if (!selection.HasSelection) return;

            string textToCopy = selection.selectedText;
            GUIUtility.systemCopyBuffer = textToCopy;
            EditorGUIUtility.systemCopyBuffer = textToCopy;
            _host.Notify?.Invoke("Copied to clipboard");
            _host.Repaint?.Invoke();
        }

        public void CopyEntireNote(QuickNote note)
        {
            if (note == null) return;

            string allText = NormalizeNewlines(note.content);
            GUIUtility.systemCopyBuffer = allText;
            EditorGUIUtility.systemCopyBuffer = allText;
            _host.Notify?.Invoke("Note copied to clipboard");
            _host.Repaint?.Invoke();
        }

        public void Paste(QuickNote note, SelectionSnapshot selection)
        {
            if (note == null) return;

            string clipboardText = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardText)) clipboardText = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardText))
            {
                ThemedDialog.Show("Clipboard Empty", "No text found on clipboard to paste.", "OK");
                return;
            }

            string content = NormalizeNewlines(note.content);
            clipboardText = NormalizeNewlines(clipboardText);
            int insertPosition = ResolveInsertPosition(content, selection, out int replaceLength);
            string updatedContent = ReplaceRange(content, insertPosition, replaceLength, clipboardText, out int caret);
            _host.RecordUndo?.Invoke(note);
            note.content = updatedContent;
            CompleteMutation(note, caret, "Pasted from clipboard");
        }

        public void SmartPaste(QuickNote note, SelectionSnapshot selection)
        {
            if (note == null) return;

            string oldContent = NormalizeNewlines(note.content);
            int insertPosition = ResolveInsertPosition(oldContent, selection, out int replaceLength);
            _host.RecordUndo?.Invoke(note);
            if (!MarkdownRenderer.SmartPasteToNote(note, null, null, out string feedback, insertPosition, replaceLength)) return;

            string updatedContent = NormalizeNewlines(note.content);
            int caret = CalculateSmartPasteCaret(insertPosition, oldContent.Length, replaceLength, updatedContent.Length);
            note.content = updatedContent;
            CompleteMutation(note, caret, feedback);
        }

        private ThemedContextMenu BuildContextMenu(QuickNote note, bool inEditMode)
        {
            var menu = new ThemedContextMenu { FitContent = !inEditMode };
            SelectionSnapshot selection = _snapshot;

            if (inEditMode)
            {
                if (selection.HasSelection)
                    menu.AddItem(new GUIContent("✂ Cut"), false, () => Cut(note, selection));
                else
                    menu.AddDisabledItem(new GUIContent("✂ Cut"));

                if (selection.HasSelection)
                    menu.AddItem(new GUIContent("📋 Copy"), false, () => Copy(selection));
                else
                    menu.AddDisabledItem(new GUIContent("📋 Copy"));

                menu.AddItem(new GUIContent("📋 Copy Entire Note"), false, () => CopyEntireNote(note));
                menu.AddItem(new GUIContent("📋 Paste"), false, () => Paste(note, selection));
                menu.AddItem(new GUIContent("📋 Smart Paste (Text or Image)"), false, () => SmartPaste(note, selection));

                menu.AddSeparator("");
                if (_host.CanUndo?.Invoke(note) == true && _host.Undo != null)
                    menu.AddItem(new GUIContent("Undo"), false, () => _host.Undo(note));
                else
                    menu.AddDisabledItem(new GUIContent("Undo"));

                if (_host.CanRedo?.Invoke(note) == true && _host.Redo != null)
                    menu.AddItem(new GUIContent("Redo"), false, () => _host.Redo(note));
                else
                    menu.AddDisabledItem(new GUIContent("Redo"));

                menu.AddSeparator("");
                menu.AddItem(new GUIContent(_host.SwitchToPreviewLabel), false, () => SwitchMode(false));
                menu.AddItem(new GUIContent("📎 Attach Image..."), false, () => _host.AttachImage?.Invoke(note));
            }
            else
            {
                if (selection.HasSelection)
                    menu.AddItem(new GUIContent("📋 Copy"), false, () => Copy(selection));
                else
                    menu.AddDisabledItem(new GUIContent("📋 Copy"));

                menu.AddItem(new GUIContent("📋 Copy Entire Note"), false, () => CopyEntireNote(note));
                menu.AddItem(new GUIContent(_host.SwitchToEditLabel), false, () => SwitchMode(true));
            }

            if (_host.PopoutNote != null)
                menu.AddItem(new GUIContent("↗ Popout Note"), false, () => _host.PopoutNote(note));
            if (_host.ExportNote != null)
                menu.AddItem(new GUIContent("📤 Export Note..."), false, () => _host.ExportNote(note));

            menu.OnClosed = () =>
            {
                _menuOpen = false;
                RequestRestore(_snapshot);
                _host.Repaint?.Invoke();
            };
            return menu;
        }

        private void SwitchMode(bool editMode)
        {
            _host.SetEditMode?.Invoke(editMode);
            Reset();
            GUI.FocusControl(null);
            _host.Repaint?.Invoke();
        }

        private void PrepareRightClick(QuickNote note, Vector2 mousePosition)
        {
            int clickIndex = GetClickIndex(note, mousePosition);
            TextEditor editor = GetActiveTextEditor(note);
            if (editor != null && !string.IsNullOrEmpty(editor.SelectedText))
            {
                _live = CreateSnapshot(editor.cursorIndex, editor.selectIndex, editor.SelectedText);
                _lastSelectionControlId = editor.controlID;
            }

            _live = ResolveRightClickSelection(clickIndex, _live);
            if (!_live.HasSelection)
            {
                if (editor != null)
                {
                    editor.cursorIndex = _live.caretIndex;
                    editor.selectIndex = _live.selectIndex;
                }
            }
            _snapshot = _live;
        }

        private int GetClickIndex(QuickNote note, Vector2 mousePosition)
        {
            string content = NormalizeNewlines(note?.content);
            if (_textAreaStyle == null) return Mathf.Clamp(_live.caretIndex, 0, content.Length);
            try
            {
                TextEditor editor = GetActiveTextEditor(note);
                if (editor == null)
                {
                    _fallbackTextEditor ??= new TextEditor();
                    editor = _fallbackTextEditor;
                }

                return GetCursorIndexAtPosition(editor, _lastRect, content, mousePosition, _textAreaStyle);
            }
            catch { return Mathf.Clamp(_live.caretIndex, 0, content.Length); }
        }

        private static int GetCursorIndexAtPosition(TextEditor editor, Rect position, string content, Vector2 mousePosition, GUIStyle style)
        {
            if (editor == null || style == null) return 0;

            int originalCursorIndex = editor.cursorIndex;
            int originalSelectIndex = editor.selectIndex;
            string originalText = editor.text;
            Rect originalPosition = editor.position;
            GUIStyle originalStyle = editor.style;
            bool originalMultiline = editor.isMultiline;
            Vector2 originalScrollOffset = editor.scrollOffset;

            try
            {
                editor.text = content ?? "";
                editor.position = position;
                editor.style = style;
                editor.isMultiline = true;
                editor.scrollOffset = originalScrollOffset;
                editor.MoveCursorToPosition(mousePosition);
                return Mathf.Clamp(editor.cursorIndex, 0, (content ?? "").Length);
            }
            finally
            {
                try
                {
                    editor.text = originalText;
                    editor.position = originalPosition;
                    editor.style = originalStyle;
                    editor.isMultiline = originalMultiline;
                    editor.cursorIndex = originalCursorIndex;
                    editor.selectIndex = originalSelectIndex;
                    editor.scrollOffset = originalScrollOffset;
                }
                catch { }
            }
        }

        private TextEditor GetActiveTextEditor(QuickNote note)
        {
            string noteText = NormalizeNewlines(note?.content);
            try
            {
                int keyboardControl = GUIUtility.keyboardControl;
                var matchingEditors = new List<TextEditor>();
                var matchingControlIds = new List<int>();
                foreach (TextEditor editor in GetAllActiveTextEditors(_lastSelectionControlId))
                {
                    if (!MatchesNote(editor, noteText)) continue;
                    matchingEditors.Add(editor);
                    matchingControlIds.Add(editor.controlID);
                }

                int preferredControlId = ResolvePreferredEditorControlId(matchingControlIds, keyboardControl, _lastSelectionControlId);
                if (preferredControlId == 0) return null;

                foreach (TextEditor editor in matchingEditors)
                {
                    if (editor.controlID == preferredControlId) return editor;
                }
            }
            catch { }
            return null;
        }

        private TextEditor GetFocusedTextEditor()
        {
            try
            {
                int keyboardControl = GUIUtility.keyboardControl;
                if (keyboardControl == 0) return null;

                TextEditor editor = GUIUtility.GetStateObject(typeof(TextEditor), keyboardControl) as TextEditor;
                if (editor != null && editor.controlID == keyboardControl) return editor;

                editor = GetRecycledEditor();
                return editor != null && editor.controlID == keyboardControl ? editor : null;
            }
            catch { return null; }
        }

        public static int ResolvePreferredEditorControlId(IEnumerable<int> availableControlIds, int focusedControlId,
            int lastSelectionControlId)
        {
            if (availableControlIds == null) return 0;

            bool hasFocusedControl = false;
            bool hasLastSelectionControl = false;
            foreach (int controlId in availableControlIds)
            {
                if (controlId == focusedControlId) hasFocusedControl = true;
                if (controlId == lastSelectionControlId) hasLastSelectionControl = true;
            }

            if (focusedControlId != 0 && hasFocusedControl) return focusedControlId;
            if (lastSelectionControlId != 0 && hasLastSelectionControl) return lastSelectionControlId;

            return 0;
        }

        private static bool MatchesNote(TextEditor editor, string noteText)
        {
            try { return editor != null && NormalizeNewlines(editor.text) == noteText; }
            catch { return false; }
        }

        private static SelectionSnapshot CreateSnapshot(int caretIndex, int selectIndex, string selectedText)
        {
            return new SelectionSnapshot
            {
                caretIndex = caretIndex,
                selectIndex = selectIndex,
                selectedText = selectedText ?? ""
            };
        }

        private void RequestRestore(SelectionSnapshot selection)
        {
            _pendingRestore = selection;
            _hasPendingRestore = true;
            _pendingRestoreFrames = 0;
        }

        private static void RestoreSelection(TextEditor editor, SelectionSnapshot selection, QuickNote note)
        {
            if (editor == null) return;

            int contentLength = NormalizeNewlines(note?.content).Length;
            editor.cursorIndex = Mathf.Clamp(selection.caretIndex < 0 ? contentLength : selection.caretIndex, 0, contentLength);
            editor.selectIndex = Mathf.Clamp(selection.selectIndex < 0 ? editor.cursorIndex : selection.selectIndex, 0, contentLength);
        }

        private void CompleteMutation(QuickNote note, int caret, string notification)
        {
            int clampedCaret = Mathf.Clamp(caret, 0, NormalizeNewlines(note?.content).Length);
            _live = CreateSnapshot(clampedCaret, clampedCaret, "");
            _snapshot = _live;
            RequestRestore(_live);
            SyncTextEditors(note?.content, clampedCaret, _lastSelectionControlId);
            _host.MarkModified?.Invoke(note);
            if (!string.IsNullOrEmpty(notification)) _host.Notify?.Invoke(notification);
            _host.Repaint?.Invoke();
        }

        private static void DrawSelectionHighlight(Rect textAreaRect, string text, int startIndex, int endIndex, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || endIndex > text.Length || startIndex >= endIndex || style == null) return;
            try
            {
                var content = new GUIContent(text);
                Vector2 startPosition = style.GetCursorPixelPosition(textAreaRect, content, startIndex);
                Vector2 endPosition = style.GetCursorPixelPosition(textAreaRect, content, endIndex);
                float lineHeight = style.lineHeight > 0 ? style.lineHeight : 15f;
                Color highlightColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.49f, 0.90f, 0.40f)
                    : new Color(0.24f, 0.49f, 0.90f, 0.35f);

                if (Mathf.Abs(startPosition.y - endPosition.y) < lineHeight * 0.5f)
                {
                    EditorGUI.DrawRect(new Rect(startPosition.x, startPosition.y,
                        Mathf.Max(2f, endPosition.x - startPosition.x), lineHeight), highlightColor);
                }
                else
                {
                    int currentIndex = startIndex;
                    float y = startPosition.y;
                    while (currentIndex < endIndex && y <= endPosition.y + lineHeight)
                    {
                        int nextNewline = text.IndexOf('\n', currentIndex);
                        int lineEndIndex = nextNewline >= 0 && nextNewline < endIndex ? nextNewline : endIndex;
                        Vector2 lineStart = style.GetCursorPixelPosition(textAreaRect, content, currentIndex);
                        Vector2 lineEnd = style.GetCursorPixelPosition(textAreaRect, content, lineEndIndex);
                        EditorGUI.DrawRect(new Rect(lineStart.x, lineStart.y, Mathf.Max(4f, lineEnd.x - lineStart.x), lineHeight), highlightColor);
                        if (nextNewline < 0 || nextNewline >= endIndex) break;
                        currentIndex = nextNewline + 1;
                        y += lineHeight;
                    }
                }
            }
            catch { }
        }

        public static string NormalizeNewlines(string value)
        {
            return (value ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static bool TryResolveSelectionRange(string content, SelectionSnapshot selection, out int start, out int length)
        {
            start = 0;
            length = 0;

            string normalizedContent = NormalizeNewlines(content);
            string selectedText = NormalizeNewlines(selection.selectedText);
            if (string.IsNullOrEmpty(selectedText)) return false;

            int selectionStart = Math.Min(selection.caretIndex, selection.selectIndex);
            int selectionEnd = Math.Max(selection.caretIndex, selection.selectIndex);
            if (selectionStart >= 0 && selectionEnd > selectionStart && selectionEnd <= normalizedContent.Length &&
                normalizedContent.Substring(selectionStart, selectionEnd - selectionStart) == selectedText)
            {
                start = selectionStart;
                length = selectionEnd - selectionStart;
                return true;
            }

            int fallbackIndex = normalizedContent.IndexOf(selectedText, StringComparison.Ordinal);
            if (fallbackIndex < 0) return false;

            start = fallbackIndex;
            length = selectedText.Length;
            return true;
        }

        public static int ResolveInsertPosition(string content, SelectionSnapshot selection, out int replaceLength)
        {
            string normalizedContent = NormalizeNewlines(content);
            if (selection.HasSelection && TryResolveSelectionRange(normalizedContent, selection, out int selectionStart, out replaceLength))
            {
                return selectionStart;
            }

            replaceLength = 0;
            if (selection.caretIndex >= 0 && selection.caretIndex <= normalizedContent.Length)
            {
                return selection.caretIndex;
            }

            return normalizedContent.Length;
        }

        public static string ReplaceRange(string content, int start, int length, string insert, out int newCaret)
        {
            string source = content ?? "";
            string insertion = insert ?? "";
            int safeStart = Mathf.Clamp(start, 0, source.Length);
            int safeLength = Mathf.Clamp(length, 0, source.Length - safeStart);
            newCaret = safeStart + insertion.Length;
            return source.Substring(0, safeStart) + insertion + source.Substring(safeStart + safeLength);
        }

        public static bool IsClickInsideSelection(int clickIndex, SelectionSnapshot selection)
        {
            return selection.HasSelection && selection.Start >= 0 && selection.End > selection.Start &&
                   clickIndex >= selection.Start && clickIndex <= selection.End;
        }

        public static SelectionSnapshot ResolveRightClickSelection(int clickIndex, SelectionSnapshot selection)
        {
            return IsClickInsideSelection(clickIndex, selection)
                ? selection
                : CreateSnapshot(clickIndex, clickIndex, "");
        }

        public static string StripRichTextTags(string value)
        {
            return s_RichTextTagRegex.Replace(value ?? "", "");
        }

        public static int CalculateSmartPasteCaret(int insertPosition, int oldContentLength, int replaceLength, int newContentLength)
        {
            return insertPosition + Math.Max(0, newContentLength - (oldContentLength - replaceLength));
        }

        public static void SyncTextEditors(string text, int caret)
        {
            SyncTextEditors(text, caret, 0);
        }

        private static void SyncTextEditors(string text, int caret, int lastControlId)
        {
            string editorText = text ?? "";
            int clampedCaret = Mathf.Clamp(caret, 0, editorText.Length);
            foreach (TextEditor editor in GetAllActiveTextEditors(lastControlId))
            {
                try
                {
                    if (editor == null) continue;
                    editor.text = editorText;
                    editor.cursorIndex = clampedCaret;
                    editor.selectIndex = clampedCaret;
                }
                catch { }
            }
        }

        private bool IsActivelyEditing()
        {
            try
            {
                int keyboardControl = GUIUtility.keyboardControl;
                if (keyboardControl == 0 || _currentNote == null) return false;

                TextEditor editor = GetRecycledEditor();
                if (editor == null || editor.controlID != keyboardControl || !MatchesNote(editor, NormalizeNewlines(_currentNote.content)))
                {
                    editor = GetActiveTextEditor(_currentNote);
                }

                if (editor == null || editor.controlID != keyboardControl || !MatchesNote(editor, NormalizeNewlines(_currentNote.content)))
                    return false;

                string focusedControlName = GUI.GetNameOfFocusedControl();
                if (focusedControlName != _controlName && editor.controlID != keyboardControl) return false;

                return IsEditingControl(keyboardControl);
            }
            catch { return false; }
        }

        private static bool IsEditingControl(int controlId)
        {
            try
            {
                var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                if (!s_IsEditingControlMethodResolved)
                {
                    s_IsEditingControlMethod = typeof(EditorGUI).GetMethod("IsEditingControl", flags, null,
                        new[] { typeof(int) }, null);
                    s_IsEditingControlMethodResolved = true;
                }

                if (s_IsEditingControlMethod != null && s_IsEditingControlMethod.Invoke(null, new object[] { controlId }) is bool isEditing)
                    return isEditing;
            }
            catch { }

            try { return EditorGUIUtility.editingTextField; }
            catch { return false; }
        }

        private static TextEditor GetRecycledEditor()
        {
            try
            {
                var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                if (!s_RecycledEditorFieldResolved)
                {
                    s_RecycledEditorField = typeof(EditorGUI).GetField("s_RecycledEditor", flags)
                                         ?? typeof(EditorGUI).GetField("activeEditor", flags)
                                         ?? typeof(EditorGUI).GetField("s_ActiveEditor", flags)
                                         ?? typeof(GUI).GetField("s_RecycledEditor", flags)
                                         ?? typeof(GUI).GetField("activeEditor", flags)
                                         ?? typeof(GUI).GetField("s_ActiveEditor", flags)
                                         ?? typeof(EditorGUIUtility).GetField("s_RecycledEditor", flags);
                    s_RecycledEditorFieldResolved = true;
                }
                if (s_RecycledEditorField != null)
                {
                    return s_RecycledEditorField.GetValue(null) as TextEditor;
                }
            }
            catch { }
            return null;
        }

        private static List<TextEditor> GetAllActiveTextEditors(int lastControlId = 0)
        {
            var list = new List<TextEditor>();
            try
            {
                var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                var fEditorGUI = typeof(EditorGUI).GetField("s_RecycledEditor", flags)
                              ?? typeof(EditorGUI).GetField("activeEditor", flags)
                              ?? typeof(EditorGUI).GetField("s_ActiveEditor", flags);
                if (fEditorGUI != null)
                {
                    var editor = fEditorGUI.GetValue(null) as TextEditor;
                    if (editor != null && !list.Contains(editor)) list.Add(editor);
                }

                var fGUI = typeof(GUI).GetField("s_RecycledEditor", flags)
                        ?? typeof(GUI).GetField("activeEditor", flags)
                        ?? typeof(GUI).GetField("s_ActiveEditor", flags);
                if (fGUI != null)
                {
                    var editor = fGUI.GetValue(null) as TextEditor;
                    if (editor != null && !list.Contains(editor)) list.Add(editor);
                }

                var fEditorGUIUtil = typeof(EditorGUIUtility).GetField("s_RecycledEditor", flags);
                if (fEditorGUIUtil != null)
                {
                    var editor = fEditorGUIUtil.GetValue(null) as TextEditor;
                    if (editor != null && !list.Contains(editor)) list.Add(editor);
                }

                int keyboardControl = GUIUtility.keyboardControl;
                if (keyboardControl != 0)
                {
                    var editor = GUIUtility.GetStateObject(typeof(TextEditor), keyboardControl) as TextEditor;
                    if (editor != null && !list.Contains(editor)) list.Add(editor);
                }

                if (lastControlId != 0 && lastControlId != keyboardControl)
                {
                    var editor = GUIUtility.GetStateObject(typeof(TextEditor), lastControlId) as TextEditor;
                    if (editor != null && !list.Contains(editor)) list.Add(editor);
                }
            }
            catch { }
            return list;
        }
    }
}