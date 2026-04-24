using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Editor
{
    /// <summary>
    /// Utility class to render Markdown content in the Unity Editor.
    /// Extracted from TaskBoardWindow to be reusable in NotePopupWindow.
    /// </summary>
    public static class MarkdownRenderer
    {
        private static readonly Regex _imageEmbedRegex = new Regex(@"!\[\[([^\]]+)\]\]", RegexOptions.Compiled);
        private static readonly Regex _urlRegex = new Regex(@"(https?://[^\s\n\r]+)", RegexOptions.Compiled);
        public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga", ".psd", ".tiff", ".tif" };

        private static readonly Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GifDecoder> _gifCache = new Dictionary<string, GifDecoder>();
        private static readonly HashSet<string> _failedGifPaths = new HashSet<string>();

        /// <summary>
        /// Renders note content with inline images, headers, bold, italic,
        /// bullet points, checkboxes, and horizontal rules.
        /// Returns true if an animated GIF was rendered (needs repaint).
        /// </summary>
        public static bool DrawMarkdownPreview(QuickNote note, Action<QuickNote> markModified)
        {
            if (note == null || string.IsNullOrEmpty(note.content))
            {
                EditorGUILayout.LabelField("(empty note — switch to Edit mode to add content)", EditorStyles.centeredGreyMiniLabel);
                return false;
            }

            bool hasAnimatedGif = false;
            string[] lines = note.content.Split('\n');
            var textBuffer = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');

                // Check for image embeds  ![[filename.png]]
                if (_imageEmbedRegex.IsMatch(line))
                {
                    FlushTextBlock(textBuffer);
                    var matches = _imageEmbedRegex.Matches(line);

                    int lastEnd = 0;
                    foreach (Match m in matches)
                    {
                        if (m.Index > lastEnd)
                        {
                            string before = line.Substring(lastEnd, m.Index - lastEnd).Trim();
                            if (!string.IsNullOrEmpty(before))
                                EditorGUILayout.LabelField(before, EditorStyles.wordWrappedLabel);
                        }

                        string fileName = m.Groups[1].Value.Trim();
                        if (DrawInlineImage(note, fileName)) hasAnimatedGif = true;
                        lastEnd = m.Index + m.Length;
                    }

                    if (lastEnd < line.Length)
                    {
                        string after = line.Substring(lastEnd).Trim();
                        if (!string.IsNullOrEmpty(after))
                            EditorGUILayout.LabelField(after, EditorStyles.wordWrappedLabel);
                    }
                    continue;
                }

                // Horizontal rule
                string trimmed = line.Trim();
                if (trimmed.Length >= 3 &&
                    (trimmed.Replace("-", "").Trim() == "" ||
                     trimmed.Replace("*", "").Trim() == "" ||
                     trimmed.Replace("_", "").Trim() == ""))
                {
                    FlushTextBlock(textBuffer);
                    DrawSeparator();
                    GUILayout.Space(2);
                    continue;
                }

                // Headers
                if (trimmed.StartsWith("#"))
                {
                    FlushTextBlock(textBuffer);
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    string headerText = trimmed.Substring(level).Trim();
                    int fontSize = level <= 1 ? 20 : level == 2 ? 16 : 14;
                    var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = fontSize,
                        wordWrap = true,
                        padding = new RectOffset(4, 4, 4, 2)
                    };
                    GUILayout.Space(level <= 1 ? 8 : 4);
                    EditorGUILayout.LabelField(headerText, headerStyle);
                    if (level <= 2)
                    {
                        DrawSeparator();
                    }
                    continue;
                }

                // Checkboxes
                if (trimmed.StartsWith("- [ ]") || trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]"))
                {
                    FlushTextBlock(textBuffer);
                    bool isChecked = trimmed[3] != ' ';
                    string itemText = trimmed.Substring(5).Trim();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(16);
                        bool newChecked = EditorGUILayout.Toggle(isChecked, GUILayout.Width(18));
                        var cbStyle = new GUIStyle(EditorStyles.label)
                        {
                            wordWrap = true
                        };
                        if (isChecked) cbStyle.fontStyle = FontStyle.Italic;
                        EditorGUILayout.LabelField(itemText, cbStyle);


                        if (newChecked != isChecked)
                        {
                            lines[i] = newChecked
                                ? lines[i].Replace("- [ ]", "- [x]")
                                : lines[i].Replace("- [x]", "- [ ]").Replace("- [X]", "- [ ]");
                            note.content = string.Join("\n", lines);
                            markModified?.Invoke(note);
                        }
                    }
                    
                    continue;
                }

                // Bullet points: - item or * item
                if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && trimmed.Length > 2)
                {
                    FlushTextBlock(textBuffer);
                    string bulletText = trimmed.Substring(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(16);
                        EditorGUILayout.LabelField("•", GUILayout.Width(12));
                        EditorGUILayout.LabelField(bulletText, new GUIStyle(EditorStyles.label) { wordWrap = true });
                    }
                    continue;
                }

                // Numbered list: 1. item
                if (trimmed.Length > 2 && char.IsDigit(trimmed[0]))
                {
                    int dotIdx = trimmed.IndexOf(". ");
                    if (dotIdx > 0 && dotIdx <= 3)
                    {
                        FlushTextBlock(textBuffer);
                        string numText = trimmed;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);
                            EditorGUILayout.LabelField(numText, new GUIStyle(EditorStyles.label) { wordWrap = true });
                        }
                        continue;
                    }
                }

                textBuffer.AppendLine(line);
            }

            FlushTextBlock(textBuffer);
            return hasAnimatedGif;
        }

        private static void FlushTextBlock(StringBuilder buffer)
        {
            if (buffer.Length == 0) return;
            string text = buffer.ToString().TrimEnd();
            buffer.Clear();
            if (string.IsNullOrWhiteSpace(text)) { GUILayout.Space(6); return; }

            // Strip simple markers for display
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = Regex.Replace(text, @"\*(.+?)\*", "$1");
            text = Regex.Replace(text, @"__(.+?)__", "$1");
            text = Regex.Replace(text, @"_(.+?)_", "$1");
            text = Regex.Replace(text, @"`(.+?)`", "[$1]");

            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 13,
                richText = false,
                padding = new RectOffset(6, 6, 2, 2)
            };

            var matches = _urlRegex.Matches(text);
            if (matches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match m in matches)
                {
                    if (m.Index > lastIndex)
                    {
                        string before = text.Substring(lastIndex, m.Index - lastIndex);
                        if (!string.IsNullOrWhiteSpace(before))
                            EditorGUILayout.LabelField(before.Trim(), style);
                    }
                    if (GUILayout.Button(m.Value, TBStyles.LinkStyle))
                    {
                        OpenURLWithConfirmation(m.Value);
                    }
                    lastIndex = m.Index + m.Length;
                }
                if (lastIndex < text.Length)
                {
                    string after = text.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(after))
                        EditorGUILayout.LabelField(after.Trim(), style);
                }
            }
            else
            {
                EditorGUILayout.LabelField(text, style);
            }
        }

        private static void OpenURLWithConfirmation(string url)
        {
            if (EditorUtility.DisplayDialog("Open URL", $"Open this link in your browser?\n\n{url}", "Open", "Cancel"))
            {
                Application.OpenURL(url);
            }
        }

        private static bool DrawInlineImage(QuickNote note, string fileName)
        {
            string resolvedPath = null;
            if (note.imagePaths != null)
            {
                foreach (var p in note.imagePaths)
                {
                    if (Path.GetFileName(p) == fileName || p == fileName)
                    {
                        resolvedPath = p;
                        break;
                    }
                }
            }

            if (resolvedPath == null)
            {
                string guessPath = $"Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/{fileName}";
                string fullGuess = Path.Combine(Application.dataPath, "..", guessPath);
                if (File.Exists(fullGuess))
                    resolvedPath = guessPath;
            }

            if (resolvedPath == null)
            {
                EditorGUILayout.LabelField($"⚠ Missing image: {fileName}", EditorStyles.miniLabel);
                return false;
            }

            GUILayout.Space(4);
            bool hasGif = DrawImageThumbnail(resolvedPath, 200f);
            GUILayout.Space(4);
            return hasGif;
        }

        public static bool DrawImageThumbnail(string imagePath, float maxHeight)
        {
            if (string.IsNullOrEmpty(imagePath)) return false;

            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (ext == ".gif")
            {
                return DrawAnimatedGif(imagePath, maxHeight);
            }

            Texture2D tex = null;
            if (_imageCache.TryGetValue(imagePath, out var cached) && cached != null)
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
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }

                if (tex == null && File.Exists(imagePath))
                {
                    try
                    {
                        if (ext == ".psd" || ext == ".tiff" || ext == ".tif" || ext == ".bmp")
                        {
                            string newAssetPath = CopyImageToProject(imagePath);
                            if (!string.IsNullOrEmpty(newAssetPath) && newAssetPath.StartsWith("Assets"))
                                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath);
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
                    _imageCache[imagePath] = tex;
            }

            if (tex != null)
            {
                DrawTexture(tex, maxHeight, imagePath);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠ Image not found: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
            }
            return false;
        }

        private static bool DrawAnimatedGif(string imagePath, float maxHeight)
        {
            if (_failedGifPaths.Contains(imagePath))
            {
                EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                return false;
            }

            string assetPath = imagePath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets"))
            {
                string fullPath = ResolveImageFullPath(imagePath);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    try { assetPath = CopyImageToProject(fullPath); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[TaskBoard] Failed to copy GIF: {e.Message}");
                        _failedGifPaths.Add(imagePath);
                        EditorGUILayout.LabelField($"⚠ Could not load GIF: {Path.GetFileName(imagePath)}", EditorStyles.miniLabel);
                        return false;
                    }
                }
            }

            if (!_gifCache.TryGetValue(assetPath, out var gif))
            {
                try
                {
                    string fullPath = assetPath.StartsWith("Assets")
                        ? Path.Combine(Application.dataPath, "..", assetPath)
                        : assetPath;

                    gif = GifDecoder.Load(fullPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TaskBoard] Failed to decode GIF: {e.Message}");
                    gif = null;
                }
                _gifCache[assetPath] = gif; // cache even if null to avoid retrying
            }

            if (gif != null && gif.FrameCount > 0)
            {
                int frameIdx = gif.GetFrameIndex(EditorApplication.timeSinceStartup);
                var frame = gif.Frames[frameIdx];
                if (frame.texture != null)
                {
                    DrawTexture(frame.texture, maxHeight, imagePath);
                    return gif.FrameCount > 1; // Indicates we need repaint for next frame if animated
                }
            }

            return false;
        }

        private static void DrawTexture(Texture2D tex, float maxHeight, string imagePath = null)
        {
            float aspect = (float)tex.width / tex.height;
            float targetHeight = Mathf.Min(tex.height, maxHeight);
            float targetWidth = targetHeight * aspect;

            float windowWidth = EditorGUIUtility.currentViewWidth - 40;
            if (targetWidth > windowWidth)
            {
                targetWidth = windowWidth;
                targetHeight = targetWidth / aspect;
            }

            var rect = GUILayoutUtility.GetRect(targetWidth, targetHeight, GUILayout.Width(targetWidth), GUILayout.Height(targetHeight));
            
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (!string.IsNullOrEmpty(imagePath))
                    ImageLargePreviewWindow.ShowWindow(imagePath);
                else
                    ImageLargePreviewWindow.ShowWindow(tex);
                
                Event.current.Use();
            }

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        }

        private static string ResolveImageFullPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            if (imagePath.StartsWith("Assets"))
                return Path.Combine(Application.dataPath, "..", imagePath).Replace('/', Path.DirectorySeparatorChar);
            return imagePath;
        }

        /// <summary>
        /// Copies an external image into Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/
        /// so Unity's asset importer handles it (supports GIF, PSD, etc).
        /// Returns the Assets/... path, or the original path if already inside Assets.
        /// </summary>
        public static string CopyImageToProject(string externalPath)
        {
            if (string.IsNullOrEmpty(externalPath) || !File.Exists(externalPath)) return externalPath;

            // Already inside Assets?
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedInput = externalPath.Replace('\\', '/');
            if (normalizedInput.StartsWith(dataPath))
                return "Assets" + normalizedInput.Substring(dataPath.Length);

            // Copy into project
            string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            string fileName = Path.GetFileName(externalPath);
            string destPath = Path.Combine(destDir, fileName);

            // Avoid overwriting — add unique suffix if needed
            if (File.Exists(destPath))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                destPath = Path.Combine(destDir, $"{nameNoExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            }

            File.Copy(externalPath, destPath, false);

            // Import immediately to get a valid asset path and GUID
            string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPath.Length);
            AssetDatabase.ImportAsset(assetPath);

            return assetPath;
        }

        private static void DrawSeparator()
        {
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        #region Clipboard & Drag-and-Drop Support

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint format);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        private const uint CF_DIB = 8;
        private const uint CF_HDROP = 15;
#endif

        public static void PasteImageFromClipboard(QuickNote note, Action<QuickNote> markModified, Action repaint)
        {
            if (TryPasteImageFromClipboard(note, markModified, repaint))
                return;
            EditorUtility.DisplayDialog("No Image", "No image found on the clipboard.\n\nTip: Copy an image or image file first, then paste.", "OK");
        }

        public static bool TryPasteImageFromClipboard(QuickNote note, Action<QuickNote> markModified, Action repaint)
        {
            string assetPath = TryPasteImageToProject();
            if (string.IsNullOrEmpty(assetPath)) return false;

            note.imagePaths ??= new List<string>();
            if (!note.imagePaths.Contains(assetPath))
                note.imagePaths.Add(assetPath);

            string fn = Path.GetFileName(assetPath);
            note.content = (note.content ?? "") + $"\n![[{fn}]]";
            markModified?.Invoke(note);
            repaint?.Invoke();
            return true;
        }

        public static string TryPasteImageToProject()
        {
#if UNITY_EDITOR_WIN
            try
            {
                if (!OpenClipboard(IntPtr.Zero)) return null;
                try
                {
                    // Check for file drop (user copied a file in Explorer)
                    if (IsClipboardFormatAvailable(CF_HDROP))
                    {
                        var hDrop = GetClipboardData(CF_HDROP);
                        if (hDrop != IntPtr.Zero)
                        {
                            uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                            for (uint i = 0; i < fileCount; i++)
                            {
                                var sb = new StringBuilder(260);
                                DragQueryFile(hDrop, i, sb, 260);
                                string filePath = sb.ToString();
                                string fext = Path.GetExtension(filePath).ToLowerInvariant();
                                if (Array.Exists(ImageExtensions, e => e == fext))
                                {
                                    return CopyImageToProject(filePath);
                                }
                            }
                        }
                    }

                    // Check for bitmap data (user did PrintScreen or copied from image editor)
                    if (IsClipboardFormatAvailable(CF_DIB))
                    {
                        var hMem = GetClipboardData(CF_DIB);
                        if (hMem != IntPtr.Zero)
                        {
                            int size = GlobalSize(hMem);
                            var ptr = GlobalLock(hMem);
                            if (ptr != IntPtr.Zero && size > 40)
                            {
                                try
                                {
                                    byte[] dibData = new byte[size];
                                    Marshal.Copy(ptr, dibData, 0, size);

                                    // Parse DIB header directly into a Texture2D
                                    Texture2D tex = CreateTextureFromDib(dibData);
                                    if (tex != null)
                                    {
                                        byte[] pngBytes = tex.EncodeToPNG();
                                        UnityEngine.Object.DestroyImmediate(tex);

                                        string destDir = Path.Combine(Application.dataPath, "Plugins", "AwesomeTaskManager", "Editor", "AttachedImages");
                                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                                        string fileName = $"pasted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                                        string destPath = Path.Combine(destDir, fileName);
                                        File.WriteAllBytes(destPath, pngBytes);
                                        
                                        string dataPathNorm = Application.dataPath.Replace('\\', '/');
                                        string assetPath = "Assets" + destPath.Replace('\\', '/').Substring(dataPathNorm.Length);
                                        AssetDatabase.ImportAsset(assetPath);

                                        return assetPath;
                                    }
                                }
                                finally { GlobalUnlock(hMem); }
                            }
                        }
                    }
                }
                finally { CloseClipboard(); }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TaskBoard] Clipboard paste failed: " + e.Message);
            }
#endif
            // Fallback: check if clipboard text is a file path to an image
            string clipText = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipText))
            {
                clipText = clipText.Trim().Trim('"');
                if (File.Exists(clipText))
                {
                    string fext = Path.GetExtension(clipText).ToLowerInvariant();
                    if (Array.Exists(ImageExtensions, e => e == fext))
                    {
                        return CopyImageToProject(clipText);
                    }
                }
            }
            return null;
        }

        /// <summary>Handle Unity editor drag-and-drop of image files onto note editor area.</summary>
        public static void HandleNoteDragDropImages(QuickNote note, Action<QuickNote> markModified, Action repaint)
        {
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasImage = false;
                if (DragAndDrop.paths != null)
                {
                    foreach (var p in DragAndDrop.paths)
                    {
                        string ext = Path.GetExtension(p).ToLowerInvariant();
                        if (Array.Exists(ImageExtensions, e => e == ext))
                        { hasImage = true; break; }
                    }
                }

                if (hasImage)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        note.imagePaths ??= new List<string>();
                        foreach (var p in DragAndDrop.paths)
                        {
                            string ext = Path.GetExtension(p).ToLowerInvariant();
                            if (Array.Exists(ImageExtensions, e => e == ext))
                            {
                                string assetPath = CopyImageToProject(p);
                                if (!note.imagePaths.Contains(assetPath))
                                    note.imagePaths.Add(assetPath);
                                string fn = Path.GetFileName(assetPath);
                                note.content = (note.content ?? "") + $"\n![[{fn}]]";
                            }
                        }
                        markModified?.Invoke(note);
                        repaint?.Invoke();
                    }
                    evt.Use();
                }
            }
        }

#if UNITY_EDITOR_WIN
        private static Texture2D CreateTextureFromDib(byte[] dibData)
        {
            if (dibData == null || dibData.Length < 40) return null;

            int headerSize = BitConverter.ToInt32(dibData, 0);
            int width = BitConverter.ToInt32(dibData, 4);
            int rawHeight = BitConverter.ToInt32(dibData, 8);
            int bitCount = BitConverter.ToInt16(dibData, 14);
            int compression = BitConverter.ToInt32(dibData, 16);

            if (width <= 0 || rawHeight == 0) return null;
            if (bitCount != 24 && bitCount != 32) return null;
            if (compression != 0 && compression != 3) return null;

            bool bottomUp = rawHeight > 0;
            int absHeight = Math.Abs(rawHeight);

            int extraOffset = 0;
            if (compression == 3) extraOffset = 12;

            int pixelDataStart = headerSize + extraOffset;
            int bytesPerPixel = bitCount / 8;
            int rowStride = ((width * bytesPerPixel + 3) / 4) * 4;

            if (pixelDataStart + rowStride * absHeight > dibData.Length)
            {
                pixelDataStart = headerSize;
                if (pixelDataStart + rowStride * absHeight > dibData.Length) return null;
            }

            var tex = new Texture2D(width, absHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * absHeight];

            for (int y = 0; y < absHeight; y++)
            {
                int srcRow = bottomUp ? y : (absHeight - 1 - y);
                int srcRowStart = pixelDataStart + srcRow * rowStride;

                for (int x = 0; x < width; x++)
                {
                    int srcIdx = srcRowStart + x * bytesPerPixel;
                    if (srcIdx + bytesPerPixel > dibData.Length) break;

                    byte b = dibData[srcIdx];
                    byte g = dibData[srcIdx + 1];
                    byte r = dibData[srcIdx + 2];
                    byte a = bytesPerPixel == 4 ? dibData[srcIdx + 3] : (byte)255;
                    if (bytesPerPixel == 4 && a == 0) a = 255;

                    pixels[y * width + x] = new Color32(r, g, b, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }
#endif

        #endregion
    }
}
