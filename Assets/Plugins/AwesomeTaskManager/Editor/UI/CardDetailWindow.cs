using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    public class CardDetailWindow : EditorWindow
    {
        private TaskCard _card;
        private System.Action _onChanged;
        private System.Action _onDelete;
        private System.Action<TaskCard> _onCreated;
        private Vector2 _scroll;
        private string _newChecklistItem = "";
        private bool _isNewCard;
        private List<string> _categories;
        private SaveData _saveData;
        private string _newCategory = "";
        private bool _dirty;
        private bool _hasAnimatedGif;
        private double _lastGifRepaintTime;

        // ── Open existing card ──
        public static void Show(TaskCard card, SaveData saveData, System.Action onChanged, System.Action onDelete)
        {
            var win = GetWindow<CardDetailWindow>(true, "📝 Card Details", true);
            win._card = card;
            win._saveData = saveData;
            win._categories = saveData.categories;
            win._onChanged = onChanged;
            win._onDelete = onDelete;
            win._onCreated = null;
            win._isNewCard = false;
            win._dirty = false;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win.ShowUtility();
        }

        // ── Open to create a NEW card ──
        public static void ShowNew(SaveData saveData, System.Action<TaskCard> onCreated)
        {
            var win = GetWindow<CardDetailWindow>(true, "✨ New Card", true);
            win._card = new TaskCard("") { description = "" };
            win._saveData = saveData;
            win._categories = saveData.categories;
            win._onCreated = onCreated;
            win._onChanged = null;
            win._onDelete = null;
            win._isNewCard = true;
            win._newChecklistItem = "";
            win._newCategory = "";
            win._dirty = false;
            win.minSize = new Vector2(440, 560);
            win.maxSize = new Vector2(640, 880);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            if (_card == null) { Close(); return; }

            _hasAnimatedGif = false;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── Color label bar ──
            var labelColor = TBStyles.LabelColors[Mathf.Clamp(_card.colorLabel, 0, TBStyles.LabelColors.Length - 1)];
            var barRect = GUILayoutUtility.GetRect(0, 6, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, labelColor);
            GUILayout.Space(10);

            // ── Title ──
            EditorGUILayout.LabelField(_isNewCard ? "Card Title" : "Title", EditorStyles.boldLabel);
            string newTitle = EditorGUILayout.TextField(_card.title);
            if (newTitle != _card.title) { _card.title = newTitle; MarkDirty(); }
            GUILayout.Space(8);

            // ── Category ──
            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                var catOptions = new List<string> { "None" };
                catOptions.AddRange(_categories);
                int currentIdx = 0;
                if (!string.IsNullOrEmpty(_card.category))
                {
                    int found = catOptions.IndexOf(_card.category);
                    if (found >= 0) currentIdx = found;
                }
                int newIdx = EditorGUILayout.Popup(currentIdx, catOptions.ToArray());
                string picked = newIdx == 0 ? "" : catOptions[newIdx];
                if (picked != (_card.category ?? ""))
                {
                    _card.category = picked;
                    // Auto-apply default color for this category
                    if (!string.IsNullOrEmpty(picked) && _saveData != null)
                    {
                        int defaultColor = _saveData.GetCategoryColor(picked);
                        if (defaultColor > 0)
                            _card.colorLabel = defaultColor;
                    }
                    MarkDirty();
                }
            }
            // Add new category
            _newCategory = EditorGUILayout.TextField(_newCategory, GUILayout.Width(80));
            if (GUILayout.Button("+", TBStyles.IconButton) && !string.IsNullOrWhiteSpace(_newCategory))
            {
                string nc = _newCategory.Trim();
                if (!_categories.Contains(nc))
                    _categories.Add(nc);
                _card.category = nc;
                int defaultColor = _saveData != null ? _saveData.GetCategoryColor(nc) : 0;
                if (defaultColor > 0) _card.colorLabel = defaultColor;
                _newCategory = "";
                MarkDirty();
            }
            // Remove selected category from the global list
            if (!string.IsNullOrEmpty(_card.category) && GUILayout.Button("🗑", TBStyles.IconButton))
            {
                if (EditorUtility.DisplayDialog("Remove Category",
                    $"Remove \"{_card.category}\" from the category list?\n(Cards already using it will keep it as text.)",
                    "Remove", "Cancel"))
                {
                    _categories.Remove(_card.category);
                    _card.category = "";
                    MarkDirty();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            // ── Description ──
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            string newDesc = EditorGUILayout.TextArea(_card.description, new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true, fontSize = 12, padding = new RectOffset(6, 6, 6, 6)
            }, GUILayout.MinHeight(70));
            if (newDesc != _card.description) { _card.description = newDesc; MarkDirty(); }
            GUILayout.Space(8);

            // ── Color Label & Priority ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Color Label", EditorStyles.boldLabel);
            int newColor = EditorGUILayout.Popup(_card.colorLabel, TBStyles.LabelNames);
            if (newColor != _card.colorLabel) { _card.colorLabel = newColor; MarkDirty(); }
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Priority", EditorStyles.boldLabel);
            int newPri = EditorGUILayout.Popup(_card.priority, TBStyles.PriorityNames);
            if (newPri != _card.priority) { _card.priority = newPri; MarkDirty(); }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ── Set as default color for this category ──
            if (!string.IsNullOrEmpty(_card.category) && _saveData != null)
            {
                int currentDefault = _saveData.GetCategoryColor(_card.category);
                string btnLabel = currentDefault == _card.colorLabel
                    ? $"✔ \"{_card.category}\" default = {TBStyles.LabelNames[_card.colorLabel]}"
                    : $"Set {TBStyles.LabelNames[_card.colorLabel]} as default for \"{_card.category}\"";
                if (GUILayout.Button(btnLabel, GUILayout.Height(20)))
                {
                    _saveData.SetCategoryColor(_card.category, _card.colorLabel);
                    MarkDirty();
                }
            }
            GUILayout.Space(8);

            // ── Completed Toggle ──
            EditorGUILayout.BeginHorizontal();
            {
                var compStyle = new GUIStyle(EditorStyles.boldLabel);
                if (_card.completed) compStyle.normal = new GUIStyleState { textColor = new Color(0.3f, 0.85f, 0.4f) };
                EditorGUILayout.LabelField(_card.completed ? "✅ Completed" : "Status", compStyle, GUILayout.Width(100));

                GUI.backgroundColor = _card.completed ? new Color(0.3f, 0.85f, 0.4f) : Color.white;
                if (GUILayout.Button(_card.completed ? "Mark Incomplete" : "Mark Complete", GUILayout.Height(22)))
                {
                    _card.completed = !_card.completed;
                    _dirty = true;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ── Due Date ──
            EditorGUILayout.LabelField("Due Date", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                // Parse existing date or show empty
                bool hasDueDate = !string.IsNullOrWhiteSpace(_card.dueDate) && DateTime.TryParse(_card.dueDate, out _);

                if (hasDueDate)
                {
                    DateTime parsed = DateTime.Parse(_card.dueDate);
                    int daysUntil = (parsed.Date - DateTime.Today).Days;
                    string statusIcon, statusText;
                    if (_card.completed)
                    {
                        statusIcon = "✅";
                        statusText = $"Completed (was due {parsed:MMM dd})";
                    }
                    else
                    {
                        statusIcon = daysUntil < 0 ? "🔴" : daysUntil == 0 ? "🟠" : daysUntil <= 3 ? "🟡" : "📅";
                        statusText = daysUntil < 0 ? $"Overdue by {-daysUntil}d"
                            : daysUntil == 0 ? "Due today!"
                            : daysUntil <= 3 ? $"Due in {daysUntil}d"
                            : $"Due {parsed:MMM dd, yyyy}";
                    }

                    EditorGUILayout.LabelField($"{statusIcon} {statusText}", GUILayout.Width(180));
                }

                // Year / Month / Day dropdowns
                int year = 0, month = 1, day = 1;
                if (hasDueDate)
                {
                    var d = DateTime.Parse(_card.dueDate);
                    year = d.Year; month = d.Month; day = d.Day;
                }
                else
                {
                    var t = DateTime.Today;
                    year = t.Year; month = t.Month; day = t.Day;
                }

                EditorGUILayout.LabelField("Y:", GUILayout.Width(16));
                int newYear = EditorGUILayout.IntField(year, GUILayout.Width(50));
                EditorGUILayout.LabelField("M:", GUILayout.Width(18));
                int newMonth = EditorGUILayout.IntPopup(month, 
                    new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" },
                    new[] { 1,2,3,4,5,6,7,8,9,10,11,12 }, GUILayout.Width(52));
                EditorGUILayout.LabelField("D:", GUILayout.Width(16));
                int maxDay = DateTime.DaysInMonth(Mathf.Clamp(newYear, 1, 9999), Mathf.Clamp(newMonth, 1, 12));
                int newDay = EditorGUILayout.IntField(Mathf.Clamp(day, 1, maxDay), GUILayout.Width(34));
                newDay = Mathf.Clamp(newDay, 1, maxDay);

                if (hasDueDate && (newYear != year || newMonth != month || newDay != day))
                {
                    try
                    {
                        _card.dueDate = new DateTime(newYear, newMonth, newDay).ToString("yyyy-MM-dd");
                        MarkDirty();
                    }
                    catch { /* invalid date combo, ignore */ }
                }

                if (!hasDueDate)
                {
                    if (GUILayout.Button("Set Date", GUILayout.Width(66), GUILayout.Height(20)))
                    {
                        _card.dueDate = new DateTime(newYear, newMonth, newDay).ToString("yyyy-MM-dd");
                        MarkDirty();
                    }
                }
                else
                {
                    if (GUILayout.Button("✕", TBStyles.IconButton))
                    {
                        _card.dueDate = "";
                        MarkDirty();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Quick-set buttons
            if (!string.IsNullOrWhiteSpace(_card.dueDate) || true)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                if (GUILayout.Button("Today", EditorStyles.miniButton, GUILayout.Width(50)))
                { _card.dueDate = DateTime.Today.ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1d", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+3d", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1w", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+2w", EditorStyles.miniButton, GUILayout.Width(36)))
                { _card.dueDate = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd"); MarkDirty(); }
                if (GUILayout.Button("+1m", EditorStyles.miniButton, GUILayout.Width(38)))
                { _card.dueDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"); MarkDirty(); }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            if (!_isNewCard)
                EditorGUILayout.LabelField($"Created: {_card.createdDate}", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // ── Checklist ──
            EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel);
            for (int i = 0; i < _card.checklistItems.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                bool done = EditorGUILayout.Toggle(_card.checklistStates[i], GUILayout.Width(20));
                if (done != _card.checklistStates[i]) { _card.checklistStates[i] = done; MarkDirty(); }

                var style = new GUIStyle(EditorStyles.textField);
                if (done) style.fontStyle = FontStyle.Italic;
                string itemText = EditorGUILayout.TextField(_card.checklistItems[i], style);
                if (itemText != _card.checklistItems[i]) { _card.checklistItems[i] = itemText; MarkDirty(); }

                if (GUILayout.Button("✕", TBStyles.IconButton))
                {
                    _card.checklistItems.RemoveAt(i);
                    _card.checklistStates.RemoveAt(i);
                    MarkDirty();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            _newChecklistItem = EditorGUILayout.TextField(_newChecklistItem);
            if (GUILayout.Button("+", TBStyles.IconButton) && !string.IsNullOrWhiteSpace(_newChecklistItem))
            {
                _card.checklistItems.Add(_newChecklistItem.Trim());
                _card.checklistStates.Add(false);
                _newChecklistItem = "";
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // ── Image Attachment ──
            EditorGUILayout.LabelField("Image / GIF", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(_card.imagePath))
            {
                EditorGUILayout.LabelField($"🖼 {Path.GetFileName(_card.imagePath)}", EditorStyles.miniLabel);
                if (GUILayout.Button("✕", TBStyles.IconButton))
                {
                    _card.imagePath = "";
                    MarkDirty();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No image attached", EditorStyles.miniLabel);
            }
            if (GUILayout.Button("Browse…", GUILayout.Width(70), GUILayout.Height(20)))
            {
                string imgPath = EditorUtility.OpenFilePanel("Attach Image", "",
                    "png,jpg,jpeg,gif,bmp,tga,psd,tiff");
                if (!string.IsNullOrEmpty(imgPath))
                {
                    _card.imagePath = CopyImageToProject(imgPath);
                    MarkDirty();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Display image preview
            if (!string.IsNullOrEmpty(_card.imagePath))
            {
                GUILayout.Space(4);
                DrawCardImage(_card.imagePath, 180f);
            }

            GUILayout.Space(16);

            // ── Bottom buttons ──
            if (_isNewCard)
            {
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
                GUI.enabled = !string.IsNullOrWhiteSpace(_card.title);
                if (GUILayout.Button("✅  Create Card", GUILayout.Height(32)))
                {
                    _onCreated?.Invoke(_card);
                    Close();
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
                GUILayout.Space(4);
                if (GUILayout.Button("Cancel", GUILayout.Height(24)))
                {
                    if (IsNewCardDirty())
                    {
                        if (EditorUtility.DisplayDialog("Discard New Card?",
                            "You have unsaved changes. Are you sure you want to discard this card?",
                            "Discard", "Keep Editing"))
                        {
                            Close();
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                // Save Changes button
                GUI.enabled = _dirty;
                GUI.backgroundColor = _dirty ? new Color(0.3f, 0.7f, 0.95f) : Color.grey;
                if (GUILayout.Button(_dirty ? "💾  Save Changes" : "✔  All Saved", GUILayout.Height(30)))
                {
                    _onChanged?.Invoke();
                    _dirty = false;
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                GUILayout.Space(8);

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑  Delete Card", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Delete Card", $"Delete \"{_card.title}\"?", "Delete", "Cancel"))
                    {
                        _onDelete?.Invoke();
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();

            // Throttled repaint for GIF animation
            if (_hasAnimatedGif && EditorApplication.timeSinceStartup - _lastGifRepaintTime > 0.066)
            {
                _lastGifRepaintTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += Repaint;
            }
        }

        private bool IsNewCardDirty()
        {
            if (!string.IsNullOrWhiteSpace(_card.title)) return true;
            if (!string.IsNullOrWhiteSpace(_card.description)) return true;
            if (!string.IsNullOrEmpty(_card.category)) return true;
            if (!string.IsNullOrEmpty(_card.imagePath)) return true;
            if (!string.IsNullOrWhiteSpace(_card.dueDate)) return true;
            if (_card.checklistItems != null && _card.checklistItems.Count > 0) return true;
            if (_card.colorLabel > 0) return true;
            if (_card.priority > 0) return true;
            return false;
        }

        private void MarkDirty()
        {
            _dirty = true;
            if (!_isNewCard) _onChanged?.Invoke(); // live-save for existing cards
        }

        private static readonly Dictionary<string, Texture2D> _cardImageCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GifDecoder> _cardGifCache = new Dictionary<string, GifDecoder>();

        private static string CopyImageToProject(string externalPath)
        {
            if (string.IsNullOrEmpty(externalPath) || !File.Exists(externalPath)) return externalPath;

            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedInput = externalPath.Replace('\\', '/');
            if (normalizedInput.StartsWith(dataPath))
                return "Assets" + normalizedInput.Substring(dataPath.Length);

            string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            string fileName = Path.GetFileName(externalPath);
            string destPath = Path.Combine(destDir, fileName);
            if (File.Exists(destPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                destPath = Path.Combine(destDir, $"{nameNoExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            }

            File.Copy(externalPath, destPath, false);

            // Defer Refresh to avoid calling during OnGUI (causes crashes / domain-reload mid-layout)
            EditorApplication.delayCall += () => AssetDatabase.Refresh();

            return "Assets" + destPath.Replace('\\', '/').Substring(dataPath.Length);
        }

        private void DrawCardImage(string imagePath, float maxHeight)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            string ext = Path.GetExtension(imagePath).ToLowerInvariant();

            // Animated GIF
            if (ext == ".gif")
            {
                if (!_cardGifCache.TryGetValue(imagePath, out var gif))
                {
                    try
                    {
                        string fullPath = imagePath;
                        if (imagePath.StartsWith("Assets"))
                            fullPath = Path.Combine(Application.dataPath, "..", imagePath);
                        gif = GifDecoder.Load(fullPath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CardDetail] Failed to load GIF: {e.Message}");
                        gif = null;
                    }
                    _cardGifCache[imagePath] = gif;
                }

                if (gif != null && gif.FrameCount > 0)
                {
                    int frameIdx = gif.GetFrameIndex(EditorApplication.timeSinceStartup);
                    var frame = gif.Frames[frameIdx];
                    if (frame.texture != null)
                        DrawTex(frame.texture, maxHeight);
                    if (gif.FrameCount > 1)
                        _hasAnimatedGif = true;
                    return;
                }
                // Fallback to static
            }

            Texture2D tex = null;
            if (_cardImageCache.TryGetValue(imagePath, out var cached) && cached != null)
            {
                tex = cached;
            }
            else
            {
                string assetPath = imagePath.Replace('\\', '/');
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (assetPath.StartsWith(dataPath))
                    assetPath = "Assets" + assetPath.Substring(dataPath.Length);

                if (assetPath.StartsWith("Assets"))
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (tex == null && File.Exists(imagePath))
                {
                    try
                    {
                        if (ext == ".psd" || ext == ".tiff" || ext == ".tif" || ext == ".bmp" || ext == ".gif")
                        {
                            string newPath = CopyImageToProject(imagePath);
                            if (newPath.StartsWith("Assets"))
                                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
                        }
                        else
                        {
                            byte[] data = File.ReadAllBytes(imagePath);
                            tex = new Texture2D(2, 2);
                            if (!tex.LoadImage(data))
                            {
                                UnityEngine.Object.DestroyImmediate(tex);
                                tex = null;
                            }
                            else
                            {
                                tex.hideFlags = HideFlags.DontSave;
                            }
                        }
                    }
                    catch { tex = null; }
                }

                if (tex != null)
                    _cardImageCache[imagePath] = tex;
            }

            if (tex != null)
            {
                DrawTex(tex, maxHeight);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠ Image not found: {Path.GetFileName(imagePath)}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawTex(Texture2D tex, float maxHeight)
        {
            float aspect = (float)tex.width / tex.height;
            float displayHeight = Mathf.Min(maxHeight, tex.height);
            float displayWidth = displayHeight * aspect;
            float availWidth = EditorGUIUtility.currentViewWidth - 40f;
            if (displayWidth > availWidth)
            {
                displayWidth = availWidth;
                displayHeight = displayWidth / aspect;
            }

            var imgRect = GUILayoutUtility.GetRect(displayWidth, displayHeight,
                GUILayout.MaxWidth(displayWidth), GUILayout.MaxHeight(displayHeight));
            GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
        }
    }
}
