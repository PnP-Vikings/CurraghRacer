using UnityEditor;
using UnityEngine;
using System.IO;

namespace AwesomeTaskManager.UI
{
    public class ImageLargePreviewWindow : EditorWindow
    {
        private string _imagePath;
        private Texture2D _texture;
        private GifDecoder _gif;
        private int _currentFrame;
        private double _lastFrameTime;

        public static void ShowWindow(string path)
        {
            var window = GetWindow<ImageLargePreviewWindow>(true, "Image Preview", true);
            window._imagePath = path;
            window.LoadImage();
            window.Show();
        }

        public static void ShowWindow(Texture2D tex)
        {
            var window = GetWindow<ImageLargePreviewWindow>(true, "Image Preview", true);
            window._texture = tex;
            window._imagePath = null;
            window._gif = null;
            window.AdjustSize();
            window.Show();
        }

        private void LoadImage()
        {
            _gif = null;
            _texture = null;
            if (string.IsNullOrEmpty(_imagePath)) return;

            string ext = Path.GetExtension(_imagePath).ToLowerInvariant();
            if (ext == ".gif")
            {
                string fullPath = _imagePath;
                if (_imagePath.StartsWith("Assets"))
                {
                    fullPath = Path.Combine(Application.dataPath, "..", _imagePath);
                }
                
                try
                {
                    _gif = GifDecoder.Load(fullPath);
                    if (_gif != null && _gif.FrameCount > 0)
                    {
                        _texture = _gif.Frames[0].texture;
                        _currentFrame = 0;
                        _lastFrameTime = EditorApplication.timeSinceStartup;
                    }
                }
                catch { _gif = null; }
            }

            if (_gif == null)
            {
                // Load static texture
                if (_imagePath.StartsWith("Assets"))
                {
                    _texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_imagePath);
                }
                else if (File.Exists(_imagePath))
                {
                    byte[] data = File.ReadAllBytes(_imagePath);
                    _texture = new Texture2D(2, 2);
                    _texture.LoadImage(data);
                    _texture.hideFlags = HideFlags.DontSave;
                }
            }

            AdjustSize();
        }

        private void AdjustSize()
        {
            if (_texture == null) return;

            float width = _texture.width;
            float height = _texture.height;

            // Cap at 800x800 for initially showing, but user can resize?
            // Actually let's just use the texture size but cap at screen size
            float maxWidth = 1200;
            float maxHeight = 900;

            if (width > maxWidth)
            {
                float ratio = maxWidth / width;
                width = maxWidth;
                height *= ratio;
            }

            if (height > maxHeight)
            {
                float ratio = maxHeight / height;
                height = maxHeight;
                width *= ratio;
            }

            this.minSize = new Vector2(200, 200);
            this.position = new Rect(100, 100, width, height);
        }

        private void Update()
        {
            if (_gif != null && _gif.FrameCount > 1)
            {
                var frame = _gif.Frames[_currentFrame];
                double elapsed = EditorApplication.timeSinceStartup - _lastFrameTime;
                if (elapsed >= frame.delay)
                {
                    _currentFrame = (_currentFrame + 1) % _gif.FrameCount;
                    _lastFrameTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }
        }

        private void OnDisable()
        {
            CleanupTextures();
        }

        private void OnDestroy()
        {
            CleanupTextures();
        }

        private void CleanupTextures()
        {
            if (_texture != null && (_texture.hideFlags & HideFlags.DontSave) != 0)
            {
                DestroyImmediate(_texture);
                _texture = null;
            }

            if (_gif != null && _gif.Frames != null)
            {
                foreach (var frame in _gif.Frames)
                {
                    if (frame.texture != null)
                    {
                        DestroyImmediate(frame.texture);
                    }
                }
                _gif = null;
            }
        }

        private void OnGUI()
        {
            if (_gif != null && _gif.FrameCount > 0)
            {
                var frame = _gif.Frames[_currentFrame];
                if (frame.texture != null)
                {
                    GUI.DrawTexture(new Rect(0, 0, position.width, position.height), frame.texture, ScaleMode.ScaleToFit);
                }
            }
            else if (_texture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, position.width, position.height), _texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUILayout.HelpBox("Image not found or could not be loaded.", MessageType.Warning);
            }

            // Close when clicking?
            if (Event.current.type == EventType.MouseDown)
            {
                Close();
            }
        }
    }
}
