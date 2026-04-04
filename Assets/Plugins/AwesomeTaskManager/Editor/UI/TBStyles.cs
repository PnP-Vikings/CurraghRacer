using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    public static class TBStyles
    {
        // Label colours (cards & notes)
        public static readonly Color[] LabelColors = new Color[]
        {
            new Color(0.55f, 0.55f, 0.55f),  // 0 Grey (none)
            new Color(0.30f, 0.69f, 0.31f),  // 1 Green
            new Color(0.13f, 0.59f, 0.95f),  // 2 Blue
            new Color(1.00f, 0.76f, 0.03f),  // 3 Yellow
            new Color(1.00f, 0.34f, 0.13f),  // 4 Orange
            new Color(0.91f, 0.12f, 0.39f),  // 5 Red / Pink
            new Color(0.61f, 0.15f, 0.69f),  // 6 Purple
            new Color(0.00f, 0.74f, 0.83f),  // 7 Teal
        };

        public static readonly string[] LabelNames = { "None", "Green", "Blue", "Yellow", "Orange", "Red", "Purple", "Teal" };
        public static readonly string[] PriorityNames = { "—", "Low", "Medium", "High", "Urgent" };
        public static readonly string[] PriorityIcons = { "", "🔵", "🟡", "🟠", "🔴" };

        // Cached textures
        private static readonly Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();

        public static Texture2D GetColorTex(Color c)
        {
            if (_texCache.TryGetValue(c, out var t) && t != null) return t;
            t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.DontSave;
            _texCache[c] = t;
            return t;
        }

        // ── Reusable styles (lazy init) ──

        private static GUIStyle _boardHeader, _columnHeader, _cardBox, _cardTitle,
                                _addButton, _tabActive, _tabInactive, _noteBox, _noteBoxSelected,
                                _noteTitle, _sectionLabel, _searchField, _iconButton;

        public static GUIStyle BoardHeader => _boardHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 6, 6),
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        public static GUIStyle ColumnHeader => _columnHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 4, 4),
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f,0.9f,0.9f) : Color.black }
        };

        public static GUIStyle CardBox
        {
            get
            {
                if (_cardBox == null)
                {
                    _cardBox = new GUIStyle("helpBox")
                    {
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(4, 4, 2, 2),
                        fontSize = 11,
                        wordWrap = true
                    };
                }
                return _cardBox;
            }
        }

        public static GUIStyle CardTitle => _cardTitle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = true,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        public static GUIStyle AddButton => _addButton ??= new GUIStyle("Button")
        {
            fontSize = 18, fixedHeight = 28, alignment = TextAnchor.MiddleCenter
        };

        public static GUIStyle TabActive
        {
            get
            {
                if (_tabActive == null)
                {
                    _tabActive = new GUIStyle("Button")
                    {
                        fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 30,
                        normal = { textColor = Color.white, background = GetColorTex(new Color(0.2f, 0.5f, 0.85f)) },
                        hover  = { textColor = Color.white, background = GetColorTex(new Color(0.25f, 0.55f, 0.9f)) }
                    };
                }
                return _tabActive;
            }
        }

        public static GUIStyle TabInactive => _tabInactive ??= new GUIStyle("Button")
        {
            fontSize = 13, fixedHeight = 30
        };

        public static GUIStyle NoteBox
        {
            get
            {
                if (_noteBox == null)
                {
                    _noteBox = new GUIStyle("helpBox")
                    {
                        padding = new RectOffset(10, 10, 8, 8),
                        margin  = new RectOffset(4, 4, 2, 2),
                        fontSize = 11, wordWrap = true
                    };
                }
                return _noteBox;
            }
        }

        public static GUIStyle NoteBoxSelected
        {
            get
            {
                if (_noteBoxSelected == null)
                {
                    _noteBoxSelected = new GUIStyle("helpBox")
                    {
                        padding = new RectOffset(10, 10, 8, 8),
                        margin  = new RectOffset(4, 4, 2, 2),
                        fontSize = 11, wordWrap = true,
                        normal = { background = GetColorTex(EditorGUIUtility.isProSkin
                            ? new Color(0.15f, 0.32f, 0.55f)
                            : new Color(0.55f, 0.72f, 0.95f)) }
                    };
                }
                return _noteBoxSelected;
            }
        }

        // Strong left-accent color for selected note
        public static Color NoteSelectedAccent => new Color(0.2f, 0.6f, 1f);

        // Drag-over folder highlight (hovered target)
        public static Color FolderDropHighlight => new Color(0.3f, 0.7f, 1f, 0.35f);

        // Source folder highlight during drag (green tint)
        public static Color FolderDragSourceHighlight => new Color(0.3f, 0.85f, 0.4f, 0.25f);

        // Other (non-hovered, non-source) folder hint during drag (dim grey)
        public static Color FolderDragOtherHighlight => new Color(0.5f, 0.5f, 0.5f, 0.12f);

        // Card drag: column being hovered
        public static Color ColumnDropHovered => new Color(0.3f, 0.7f, 1f, 0.18f);

        // Card drag: column NOT hovered (dim hint)
        public static Color ColumnDropOther => new Color(0.5f, 0.5f, 0.5f, 0.08f);

        // Card drag: source column highlight
        public static Color ColumnDragSource => new Color(0.3f, 0.85f, 0.4f, 0.15f);

        public static GUIStyle NoteTitle => _noteTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13
        };

        public static GUIStyle SectionLabel => _sectionLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14, padding = new RectOffset(4, 4, 8, 4)
        };

        public static GUIStyle IconButton => _iconButton ??= new GUIStyle("Button")
        {
            fontSize = 14, fixedWidth = 26, fixedHeight = 24, alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0,0,0,0)
        };

        // Column background tint
        public static Color ColumnBg => EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.22f, 0.22f)
            : new Color(0.88f, 0.90f, 0.92f);

        public static Color ColumnBgAlt => EditorGUIUtility.isProSkin
            ? new Color(0.25f, 0.25f, 0.25f)
            : new Color(0.92f, 0.93f, 0.95f);
    }
}

