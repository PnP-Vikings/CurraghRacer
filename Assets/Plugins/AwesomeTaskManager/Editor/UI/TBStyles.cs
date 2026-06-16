using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AwesomeTaskManager.Data;

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
            new Color(1.00f, 0.92f, 0.15f),  // 3 Yellow
            new Color(1.00f, 0.60f, 0.00f),  // 4 Orange
            new Color(0.96f, 0.26f, 0.21f),  // 5 Red
            new Color(0.61f, 0.15f, 0.69f),  // 6 Purple
            new Color(0.00f, 0.59f, 0.53f),  // 7 Teal
            new Color(0.91f, 0.12f, 0.39f),  // 8 Pink
            new Color(0.80f, 0.86f, 0.22f),  // 9 Lime
            new Color(0.25f, 0.32f, 0.71f),  // 10 Indigo
            new Color(0.00f, 0.74f, 0.83f),  // 11 Cyan
            new Color(1.00f, 0.76f, 0.03f),  // 12 Amber
            new Color(1.00f, 0.34f, 0.13f),  // 13 Deep Orange
            new Color(0.40f, 0.23f, 0.72f),  // 14 Deep Purple
            new Color(0.38f, 0.49f, 0.55f),  // 15 Blue Grey
            new Color(0.47f, 0.33f, 0.28f),  // 16 Brown
        };

        public static readonly string[] LabelNames = { 
            "None", "Green", "Blue", "Yellow", "Orange", "Red", "Purple", "Teal", 
            "Pink", "Lime", "Indigo", "Cyan", "Amber", "Deep Orange", "Deep Purple", "Blue Grey", "Brown"
        };
        public static readonly string[] PriorityNames = { "—", "Low", "Medium", "High", "Urgent" };
        public static readonly string[] PriorityIcons = { "", "🔵", "🟡", "🟠", "🔴" };

        // Cached textures
        private static readonly Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _profileTexCache = new Dictionary<string, Texture2D>();
        private static Texture2D _circleTex, _circleBorderTex, _cornersMaskTex;

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

        public static Texture2D GetProfileTexture(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (_profileTexCache.TryGetValue(guid, out var tex) && tex != null) return tex;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                _profileTexCache[guid] = tex;
                return tex;
            }
            return null;
        }

        public static void DrawAssigneeIcon(Rect rect, Assignee assignee, string initials, GUIStyle style, Color? maskColor = null)
        {
            var borderColor = LabelColors[Mathf.Clamp(assignee.borderColorIndex, 0, LabelColors.Length - 1)];
            var bgColor = LabelColors[Mathf.Clamp(assignee.colorIndex, 0, LabelColors.Length - 1)];

            _circleTex ??= CreateCircleTexture(64);
            _circleBorderTex ??= CreateCircleBorderTexture(64, 4);
            _cornersMaskTex ??= CreateCornersMaskTexture(64);

            Texture2D profileTex = GetProfileTexture(assignee.profileImageGuid);
            if (profileTex != null)
            {
                // Draw profile image (scaled and cropped)
                GUI.DrawTexture(rect, profileTex, ScaleMode.ScaleAndCrop);

                // Hide corners if maskColor is provided
                if (maskColor.HasValue)
                {
                    var oldColor = GUI.color;
                    GUI.color = maskColor.Value;
                    GUI.DrawTexture(rect, _cornersMaskTex);
                    GUI.color = oldColor;
                }
            }
            else
            {
                // Draw initials on a circular background
                var oldColor = GUI.color;
                GUI.color = bgColor;
                GUI.DrawTexture(rect, _circleTex);
                GUI.color = oldColor;

                GUI.Label(rect, initials, style);
            }

            // Draw border (circular)
            if (assignee.borderColorIndex > 0)
            {
                var oldColor = GUI.color;
                GUI.color = borderColor;
                GUI.DrawTexture(rect, _circleBorderTex);
                GUI.color = oldColor;
            }
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // Use a small antialiasing factor
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        private static Texture2D CreateCircleBorderTexture(int size, int thickness)
        {
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f;
            var innerRadius = radius - thickness;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist) * Mathf.Clamp01(dist - (innerRadius - 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        private static Texture2D CreateCornersMaskTexture(int size)
        {
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(dist - (radius - 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        // ── Reusable styles (lazy init) ──

        private static GUIStyle _boardHeader, _columnHeader, _cardBox, _cardBoxHighlighted, _cardTitle,
                                _addButton, _tabActive, _tabInactive, _noteBox, _noteBoxSelected,
                                _noteTitle, _sectionLabel, _iconButton, _linkStyle, _assigneeCircle;

        public static GUIStyle AssigneeCircle => _assigneeCircle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 11, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 24, fixedHeight = 24,
            padding = new RectOffset(0,0,0,0),
            normal = { textColor = Color.white }
        };

        public static GUIStyle LinkStyle => _linkStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.2f, 0.55f, 0.95f) },
            hover  = { textColor = new Color(0.3f, 0.7f, 1.0f) },
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

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

        public static GUIStyle CardBoxHighlighted
        {
            get
            {
                if (_cardBoxHighlighted == null)
                {
                    _cardBoxHighlighted = new GUIStyle(CardBox)
                    {
                        normal = { background = GetColorTex(EditorGUIUtility.isProSkin
                            ? new Color(0.15f, 0.32f, 0.55f)
                            : new Color(0.55f, 0.72f, 0.95f)) }
                    };
                }
                return _cardBoxHighlighted;
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
                        padding = new RectOffset(12, 8, 6, 6),
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
                        padding = new RectOffset(12, 8, 6, 6),
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

        public static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        public static void InvalidateCache()
        {
            _boardHeader = null;
            _columnHeader = null;
            _cardBox = null;
            _cardBoxHighlighted = null;
            _cardTitle = null;
            _addButton = null;
            _tabActive = null;
            _tabInactive = null;
            _noteBox = null;
            _noteBoxSelected = null;
            _noteTitle = null;
            _sectionLabel = null;
            _iconButton = null;
            _linkStyle = null;
            _assigneeCircle = null;

            _texCache.Clear();
            _profileTexCache.Clear();
            _circleTex = null;
            _circleBorderTex = null;
            _cornersMaskTex = null;
        }

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

