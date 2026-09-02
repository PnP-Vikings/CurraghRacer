using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using WindowId = UnityEngine.EntityId;
#else
using WindowId = System.Int32;
#endif

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Custom themed tooltip renderer that displays hover tooltips and truncated text popups
    /// styled dynamically with the active theme colors, alpha opacity, border, and typography.
    /// Completely suppresses Unity's default black box tooltip popup while active and provides smooth, responsive hover delays.
    /// </summary>
    [InitializeOnLoad]
    public static class ThemedTooltip
    {
        private class TooltipState
        {
            public WeakReference<EditorWindow> windowRef;
            public string currentTooltip = "";
            public string displayedTooltip = "";
            public Vector2 lastMousePos;
            public Vector2 hoverAnchorPos;
            public double hoverStartTime;
            public double lastDismissTime;
            public bool isVisible;
            public bool isWarm;
        }

        private static readonly Dictionary<WindowId, TooltipState> _windowStates = new Dictionary<WindowId, TooltipState>();
        private const float ColdHoverDelay = 0.55f; // Standard delay (550ms) on cold hover before showing
        private const float WarmHoverDelay = 0.2f;  // Slight delay (200ms) when switching between adjacent items
        private const float WarmWindowDuration = 0.3f; // Time window to consider hover transition warm

        private static string _activeHoveredTooltip = null;

        // Reflection handles to suppress Unity's internal TooltipView, GUIView, ContainerWindow, and GUIUtility tooltips
        private static readonly Type TooltipViewType;
        private static readonly MethodInfo TooltipViewForceCloseMethod;
        private static readonly MethodInfo TooltipViewCloseMethod;
        private static readonly FieldInfo TooltipViewSGuiViewField;
        private static readonly PropertyInfo TooltipViewSGuiViewProp;
        private static readonly PropertyInfo TooltipViewWindowProp;

        private static readonly Type ContainerWindowType;
        private static readonly PropertyInfo ContainerWindowsProp;
        private static readonly PropertyInfo ContainerShowModeProp;
        private static readonly MethodInfo ContainerCloseMethod;
        private static readonly PropertyInfo ContainerRootViewProp;

        private static readonly Type GUIViewType;
        private static readonly PropertyInfo GUIViewCurrentProp;
        private static readonly FieldInfo GUIViewToolTipField;

        private static readonly FieldInfo MouseTooltipField;
        private static readonly PropertyInfo MouseTooltipProp;

        static ThemedTooltip()
        {
            try
            {
                var editorAssembly = typeof(EditorWindow).Assembly;

                // UnityEditor.TooltipView reflection
                TooltipViewType = editorAssembly.GetType("UnityEditor.TooltipView");
                if (TooltipViewType != null)
                {
                    TooltipViewForceCloseMethod = TooltipViewType.GetMethod("ForceClose", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    TooltipViewCloseMethod = TooltipViewType.GetMethod("Close", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    TooltipViewSGuiViewField = TooltipViewType.GetField("s_guiView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    TooltipViewSGuiViewProp = TooltipViewType.GetProperty("S_guiView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    TooltipViewWindowProp = TooltipViewType.GetProperty("window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                // UnityEditor.ContainerWindow reflection
                ContainerWindowType = editorAssembly.GetType("UnityEditor.ContainerWindow");
                if (ContainerWindowType != null)
                {
                    ContainerWindowsProp = ContainerWindowType.GetProperty("windows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    ContainerShowModeProp = ContainerWindowType.GetProperty("showMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    ContainerCloseMethod = ContainerWindowType.GetMethod("Close", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    ContainerRootViewProp = ContainerWindowType.GetProperty("rootView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                // UnityEditor.GUIView reflection
                GUIViewType = editorAssembly.GetType("UnityEditor.GUIView");
                if (GUIViewType != null)
                {
                    GUIViewCurrentProp = GUIViewType.GetProperty("current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    GUIViewToolTipField = GUIViewType.GetField("m_ToolTip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? GUIViewType.GetField("m_Tooltip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                // UnityEngine.GUIUtility reflection
                MouseTooltipField = typeof(GUIUtility).GetField("s_EditorTooltip", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? typeof(GUIUtility).GetField("s_MouseTooltip", BindingFlags.NonPublic | BindingFlags.Static);
                MouseTooltipProp = typeof(GUIUtility).GetProperty("mouseTooltip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }
            catch { }

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            ClearAll();
        }

        /// <summary>
        /// Clears all cached tooltip window states and active hover tracking.
        /// </summary>
        public static void ClearAll()
        {
            _windowStates.Clear();
            _activeHoveredTooltip = null;
            ClearNativeTooltips();
        }

        private static WindowId GetObjectId(UnityEngine.Object obj)
        {
#if UNITY_6000_3_OR_NEWER
            return obj.GetEntityId();
#else
            return obj.GetInstanceID();
#endif
        }

        private static bool IsMouseOverThemedWindow()
        {
            try
            {
                var mouseOver = EditorWindow.mouseOverWindow;
                if (mouseOver != null)
                {
                    WindowId winId = GetObjectId(mouseOver);
                    if (_windowStates.ContainsKey(winId)) return true;
                    string typeName = mouseOver.GetType().FullName;
                    if (typeName != null && (typeName.StartsWith("AwesomeTaskManager.") || typeName.Contains("Themed"))) return true;
                }

                foreach (var kvp in _windowStates)
                {
                    var state = kvp.Value;
                    if (state != null && (state.isVisible || !string.IsNullOrEmpty(state.currentTooltip)))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            List<WindowId> toRemove = null;

            foreach (var kvp in _windowStates)
            {
                var state = kvp.Value;
                if (state.windowRef == null || !state.windowRef.TryGetTarget(out var win) || win == null)
                {
                    if (toRemove == null) toRemove = new List<WindowId>();
                    toRemove.Add(kvp.Key);
                    continue;
                }

                // If mouse is no longer over this window, dismiss active tooltip
                if (EditorWindow.mouseOverWindow != win && (state.isVisible || !string.IsNullOrEmpty(state.currentTooltip)))
                {
                    state.isVisible = false;
                    state.currentTooltip = "";
                    state.displayedTooltip = "";
                    state.hoverStartTime = 0;
                    win.Repaint();
                    continue;
                }

                if (!string.IsNullOrEmpty(state.currentTooltip) && !state.isVisible)
                {
                    float requiredDelay = state.isWarm ? WarmHoverDelay : ColdHoverDelay;
                    if (now - state.hoverStartTime >= requiredDelay)
                    {
                        state.isVisible = true;
                        state.displayedTooltip = state.currentTooltip;
                        win.Repaint();
                    }
                }
            }

            if (toRemove != null)
            {
                foreach (WindowId id in toRemove)
                {
                    _windowStates.Remove(id);
                }
            }

            // If the mouse is over any active themed window, continuously suppress native tooltips
            if (IsMouseOverThemedWindow())
            {
                ClearNativeTooltips();
            }
        }

        /// <summary>
        /// Explicitly suppresses Unity's native default black box tooltip view and clears internal tooltip state.
        /// </summary>
        public static void ClearNativeTooltips()
        {
            try
            {
                // 1. Clear IMGUI global tooltip
                GUI.tooltip = "";

                // 2. Clear GUIUtility internal tooltip fields if present
                if (MouseTooltipField != null) MouseTooltipField.SetValue(null, "");
                if (MouseTooltipProp != null && MouseTooltipProp.CanWrite) MouseTooltipProp.SetValue(null, "");

                // 3. Clear current GUIView tooltip field if present
                if (GUIViewCurrentProp != null && GUIViewToolTipField != null)
                {
                    var curView = GUIViewCurrentProp.GetValue(null);
                    if (curView != null)
                    {
                        GUIViewToolTipField.SetValue(curView, "");
                    }
                }

                // 4. Force-close Unity's native TooltipView if active
                if (TooltipViewForceCloseMethod != null)
                {
                    TooltipViewForceCloseMethod.Invoke(null, null);
                }
                else if (TooltipViewCloseMethod != null)
                {
                    TooltipViewCloseMethod.Invoke(null, null);
                }

                // 5. If TooltipView.s_guiView is still non-null, force destroy its window
                if (TooltipViewSGuiViewField != null || TooltipViewSGuiViewProp != null)
                {
                    object sGuiView = TooltipViewSGuiViewField != null
                        ? TooltipViewSGuiViewField.GetValue(null)
                        : TooltipViewSGuiViewProp?.GetValue(null);

                    if (sGuiView != null)
                    {
                        if (TooltipViewWindowProp != null && ContainerCloseMethod != null)
                        {
                            var win = TooltipViewWindowProp.GetValue(sGuiView);
                            if (win != null)
                            {
                                ContainerCloseMethod.Invoke(win, null);
                            }
                        }
                        if (TooltipViewSGuiViewField != null)
                        {
                            TooltipViewSGuiViewField.SetValue(null, null);
                        }
                    }
                }

                // 6. Inspect ContainerWindows and close any stray Tooltip container windows
                if (ContainerWindowsProp != null && ContainerCloseMethod != null)
                {
                    var windows = ContainerWindowsProp.GetValue(null) as Array;
                    if (windows != null)
                    {
                        for (int i = 0; i < windows.Length; i++)
                        {
                            var win = windows.GetValue(i);
                            if (win == null) continue;

                            bool isTooltipWin = false;
                            if (ContainerShowModeProp != null)
                            {
                                object modeVal = ContainerShowModeProp.GetValue(win);
                                if (modeVal != null && (int)modeVal == 6) // ShowMode.Tooltip == 6
                                {
                                    isTooltipWin = true;
                                }
                            }

                            if (!isTooltipWin && ContainerRootViewProp != null)
                            {
                                var root = ContainerRootViewProp.GetValue(win);
                                if (root != null && root.GetType().Name.Contains("Tooltip"))
                                {
                                    isTooltipWin = true;
                                }
                            }

                            if (isTooltipWin)
                            {
                                ContainerCloseMethod.Invoke(win, null);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Explicitly registers a tooltip for a given GUI rectangle when hovered.
        /// </summary>
        public static void SetTooltip(Rect rect, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return;
            if (Event.current != null && rect.Contains(Event.current.mousePosition))
            {
                _activeHoveredTooltip = tooltip;
            }
        }

        public static bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            string tooltip = content != null ? content.tooltip : null;
            var clean = (content == null || string.IsNullOrEmpty(tooltip)) ? content : new GUIContent(content.text, content.image);
            bool clicked = GUILayout.Button(clean, style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return clicked;
        }

        public static bool Button(string text, string tooltip, GUIStyle style, params GUILayoutOption[] options)
        {
            bool clicked = GUILayout.Button(new GUIContent(text), style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return clicked;
        }

        public static bool Button(Texture icon, string tooltip, GUIStyle style, params GUILayoutOption[] options)
        {
            bool clicked = GUILayout.Button(new GUIContent(icon), style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return clicked;
        }

        public static bool Button(Rect rect, GUIContent content, GUIStyle style)
        {
            string tooltip = content != null ? content.tooltip : null;
            var clean = (content == null || string.IsNullOrEmpty(tooltip)) ? content : new GUIContent(content.text, content.image);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(rect, tooltip);
            }
            return GUI.Button(rect, clean, style);
        }

        public static bool Button(Rect rect, string text, string tooltip, GUIStyle style)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(rect, tooltip);
            }
            return GUI.Button(rect, new GUIContent(text), style);
        }

        public static bool Button(Rect rect, Texture icon, string tooltip, GUIStyle style)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(rect, tooltip);
            }
            return GUI.Button(rect, new GUIContent(icon), style);
        }

        public static bool IconButton(string icon, string tooltip, params GUILayoutOption[] options)
        {
            return Button(icon, tooltip, TBStyles.IconButton, options);
        }

        public static bool IconButton(Texture icon, string tooltip, params GUILayoutOption[] options)
        {
            return Button(icon, tooltip, TBStyles.IconButton, options);
        }

        public static bool DeleteButton(string text, string tooltip, params GUILayoutOption[] options)
        {
            return Button(text, tooltip, TBStyles.DeleteButton, options);
        }

        public static bool DeleteIconButton(string icon, string tooltip, params GUILayoutOption[] options)
        {
            return Button(icon, tooltip, TBStyles.DeleteIconButton, options);
        }

        public static bool DeleteIconButton(Texture icon, string tooltip, params GUILayoutOption[] options)
        {
            return Button(icon, tooltip, TBStyles.DeleteIconButton, options);
        }

        public static bool Toggle(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            string tooltip = content != null ? content.tooltip : null;
            var clean = (content == null || string.IsNullOrEmpty(tooltip)) ? content : new GUIContent(content.text, content.image);
            bool result = GUILayout.Toggle(value, clean, style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return result;
        }

        public static bool Toggle(bool value, string text, string tooltip, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result = GUILayout.Toggle(value, new GUIContent(text), style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return result;
        }

        public static bool Toggle(bool value, Texture icon, string tooltip, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result = GUILayout.Toggle(value, new GUIContent(icon), style, options);
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
            return result;
        }

        public static void Label(string text, string tooltip, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style != null)
                EditorGUILayout.LabelField(new GUIContent(text), style, options);
            else
                EditorGUILayout.LabelField(new GUIContent(text), options);

            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
        }

        public static void Label(GUIContent content, GUIStyle style = null, params GUILayoutOption[] options)
        {
            string tooltip = content != null ? content.tooltip : null;
            var clean = (content == null || string.IsNullOrEmpty(tooltip)) ? content : new GUIContent(content.text, content.image);
            if (style != null)
                EditorGUILayout.LabelField(clean, style, options);
            else
                EditorGUILayout.LabelField(clean, options);

            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
        }

        public static void Label(Texture icon, string tooltip, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style != null)
                EditorGUILayout.LabelField(new GUIContent(icon), style, options);
            else
                EditorGUILayout.LabelField(new GUIContent(icon), options);

            if (!string.IsNullOrEmpty(tooltip))
            {
                SetTooltip(GUILayoutUtility.GetLastRect(), tooltip);
            }
        }

        public static void TruncatedLabel(string fullText, int maxCharLength, GUIStyle style = null, params GUILayoutOption[] options)
        {
            string truncated = TBStyles.TruncateString(fullText, maxCharLength);
            string tooltip = (truncated != fullText) ? fullText : null;
            Label(truncated, tooltip, style, options);
        }

        /// <summary>
        /// Captures hovered tooltips, suppresses Unity's default black box tooltip, and renders
        /// the styled themed tooltip overlay during Repaint with a smooth hover delay.
        /// Call this at the end of OnGUI() in editor windows.
        /// </summary>
        public static void Draw(EditorWindow window)
        {
            if (window == null) return;

            WindowId winId = GetObjectId(window);
            if (!_windowStates.TryGetValue(winId, out var state))
            {
                state = new TooltipState { windowRef = new WeakReference<EditorWindow>(window) };
                _windowStates[winId] = state;
            }
            else if (state.windowRef == null)
            {
                state.windowRef = new WeakReference<EditorWindow>(window);
            }

            Event current = Event.current;
            if (current == null) return;

            double now = EditorApplication.timeSinceStartup;

            // Dismiss immediately on clicks or scrolls
            if (current.type == EventType.MouseDown || current.type == EventType.ScrollWheel)
            {
                if (state.isVisible || !string.IsNullOrEmpty(state.currentTooltip))
                {
                    state.isVisible = false;
                    state.displayedTooltip = "";
                    state.currentTooltip = "";
                    state.hoverStartTime = 0;
                    state.lastDismissTime = 0;
                }
                ClearNativeTooltips();
                return;
            }

            // Trigger repaint on mouse movement so button hover states switch immediately
            // and track movement to reset hover start time if moved significantly before showing
            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            {
                ClearNativeTooltips();
                if (state.lastMousePos != current.mousePosition)
                {
                    if (Vector2.Distance(current.mousePosition, state.hoverAnchorPos) > 6f)
                    {
                        state.hoverAnchorPos = current.mousePosition;
                        if (!state.isVisible)
                        {
                            state.hoverStartTime = now;
                        }
                    }
                    state.lastMousePos = current.mousePosition;
                    window.Repaint();
                }
                return;
            }

            // In Unity IMGUI, GUI.tooltip / _activeHoveredTooltip is evaluated during Repaint.
            if (current.type == EventType.Repaint)
            {
                // Capture raw tooltip from our active hovered tracking or GUI.tooltip
                string rawTooltip = _activeHoveredTooltip;
                _activeHoveredTooltip = null;

                if (string.IsNullOrEmpty(rawTooltip) && !string.IsNullOrEmpty(GUI.tooltip))
                {
                    rawTooltip = GUI.tooltip;
                }

                // Always suppress native Unity tooltip
                ClearNativeTooltips();

                if (!string.IsNullOrEmpty(rawTooltip))
                {
                    rawTooltip = rawTooltip.Trim();
                }

                if (!string.IsNullOrEmpty(rawTooltip))
                {
                    state.lastMousePos = current.mousePosition;

                    if (state.currentTooltip != rawTooltip)
                    {
                        // Switched to a new tooltip element
                        bool wasWarm = state.isVisible || ((now - state.lastDismissTime) < WarmWindowDuration);
                        state.currentTooltip = rawTooltip;
                        state.hoverStartTime = now;
                        state.hoverAnchorPos = current.mousePosition;
                        state.isWarm = wasWarm;
                        state.isVisible = false;
                        state.displayedTooltip = "";
                    }
                    else if (!state.isVisible)
                    {
                        float requiredDelay = state.isWarm ? WarmHoverDelay : ColdHoverDelay;
                        if (now - state.hoverStartTime >= requiredDelay)
                        {
                            state.isVisible = true;
                            state.displayedTooltip = rawTooltip;
                        }
                    }
                }
                else
                {
                    if (state.isVisible)
                    {
                        state.lastDismissTime = now;
                        state.isVisible = false;
                        state.displayedTooltip = "";
                        state.isWarm = false;
                        window.Repaint();
                    }
                    state.currentTooltip = "";
                    state.hoverStartTime = 0;
                }

                if (state.isVisible && !string.IsNullOrEmpty(state.displayedTooltip))
                {
                    DrawTooltipBox(window, state.displayedTooltip, state.lastMousePos);
                    ClearNativeTooltips();
                }
            }
            else
            {
                // On non-repaint events (e.g. Layout), ensure native tooltips remain suppressed
                ClearNativeTooltips();
            }
        }

        /// <summary>
        /// Draws a styled themed tooltip box at the specified position.
        /// </summary>
        public static void DrawTooltipBox(EditorWindow window, string text, Vector2 mousePos)
        {
            if (string.IsNullOrEmpty(text) || window == null) return;

            var style = TBStyles.TooltipStyle;
            var content = new GUIContent(text);

            float maxWindowWidth = window.position.width;
            float maxWindowHeight = window.position.height;

            float maxWidth = Mathf.Clamp(maxWindowWidth - 30f, 120f, 340f);

            Vector2 rawSize = style.CalcSize(content);
            float boxWidth;
            float boxHeight;

            if (rawSize.x > maxWidth || text.Contains("\n"))
            {
                boxWidth = Mathf.Min(rawSize.x, maxWidth);
                boxHeight = style.CalcHeight(content, boxWidth);
            }
            else
            {
                boxWidth = Mathf.Max(rawSize.x, 30f);
                boxHeight = rawSize.y;
            }

            float padX = 8f;
            float padY = 5f;
            float totalWidth = boxWidth + padX * 2f;
            float totalHeight = boxHeight + padY * 2f;

            // Position relative to mouse: 12px right, 18px down
            float x = mousePos.x + 12f;
            float y = mousePos.y + 18f;

            // Flip / clamp if near edges
            if (x + totalWidth > maxWindowWidth - 6f)
            {
                x = mousePos.x - totalWidth - 4f;
            }
            if (x < 4f)
            {
                x = 4f;
            }

            if (y + totalHeight > maxWindowHeight - 6f)
            {
                y = mousePos.y - totalHeight - 4f;
            }
            if (y < 4f)
            {
                y = 4f;
            }

            Rect boxRect = new Rect(x, y, totalWidth, totalHeight);
            Rect textRect = new Rect(x + padX, y + padY, boxWidth, boxHeight);

            // Drop shadow
            EditorGUI.DrawRect(new Rect(boxRect.x + 2, boxRect.y + 2, boxRect.width, boxRect.height), new Color(0f, 0f, 0f, 0.35f));

            // Themed Background
            EditorGUI.DrawRect(boxRect, TBStyles.TooltipBg);

            // Themed Border
            TBStyles.DrawBorderRect(boxRect, TBStyles.TooltipBorder, 1f);

            // Top specular sheen
            Color sheenColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.45f);
            if (boxRect.width > 2 && boxRect.height > 2)
            {
                EditorGUI.DrawRect(new Rect(boxRect.x + 1, boxRect.y + 1, boxRect.width - 2, 1), sheenColor);
            }

            // Tooltip text
            GUI.Label(textRect, content, style);
        }
    }
}
