using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using SkinId = UnityEngine.EntityId;
#else
using SkinId = System.Int32;
#endif

namespace AwesomeTaskManager.UI
{
    [InitializeOnLoad]
    public static class ThemedScrollbars
    {
        private const int TextureSize = 12;
        private const int AntialiasSamples = 4;
        private const int SliceBorder = 5;

        private sealed class SkinEntry
        {
            public GUISkin Source;
            public GUISkin Themed;
        }

        private static readonly Dictionary<SkinId, SkinEntry> _skins = new Dictionary<SkinId, SkinEntry>();
        private static readonly HashSet<SkinId> _themedSkinIds = new HashSet<SkinId>();
        private static Texture2D _trackTexture;
        private static Texture2D _thumbTexture;
        private static Texture2D _thumbHoverTexture;
        private static Texture2D _thumbActiveTexture;
        private static bool _texturesDirty = true;
        private static bool _warningLogged;

        static ThemedScrollbars()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        public static Scope Begin()
        {
            try
            {
                GUISkin source = GUI.skin;
                if (source == null) return default;

                if (_texturesDirty)
                    UpdateTextures();

                SkinId sourceId = GetSkinId(source);
                if (_themedSkinIds.Contains(sourceId))
                    return default;

                if (!_skins.TryGetValue(sourceId, out SkinEntry entry) || entry.Source != source || entry.Themed == null)
                {
                    if (entry != null)
                        DestroySkinEntry(sourceId, entry);

                    entry = CreateSkinEntry(source);
                    _skins[sourceId] = entry;
                    _themedSkinIds.Add(GetSkinId(entry.Themed));
                }

                GUI.skin = entry.Themed;
                return new Scope(source);
            }
            catch (Exception exception)
            {
                LogWarningOnce(exception);
                return default;
            }
        }

        public static void MarkDirty()
        {
            _texturesDirty = true;
        }

        public struct Scope : IDisposable
        {
            private readonly GUISkin _previousSkin;
            private readonly bool _shouldRestore;

            internal Scope(GUISkin previousSkin)
            {
                _previousSkin = previousSkin;
                _shouldRestore = true;
            }

            public void Dispose()
            {
                if (!_shouldRestore) return;

                try
                {
                    GUI.skin = _previousSkin;
                }
                catch (Exception exception)
                {
                    LogWarningOnce(exception);
                }
            }
        }

        private static SkinEntry CreateSkinEntry(GUISkin source)
        {
            GUISkin themed = UnityEngine.Object.Instantiate(source);
            try
            {
                themed.name = source.name + " (ATM Themed Scrollbars)";
                themed.hideFlags = HideFlags.HideAndDontSave;
                ApplyScrollbarStyles(themed);
                return new SkinEntry { Source = source, Themed = themed };
            }
            catch
            {
                if (themed != null)
                    UnityEngine.Object.DestroyImmediate(themed);
                throw;
            }
        }

        private static void ApplyScrollbarStyles(GUISkin skin)
        {
            ApplyStyle(skin.verticalScrollbar, _trackTexture, _trackTexture, _trackTexture);
            ApplyStyle(skin.horizontalScrollbar, _trackTexture, _trackTexture, _trackTexture);
            ApplyStyle(skin.verticalScrollbarThumb, _thumbTexture, _thumbHoverTexture, _thumbActiveTexture);
            ApplyStyle(skin.horizontalScrollbarThumb, _thumbTexture, _thumbHoverTexture, _thumbActiveTexture);
        }

        private static SkinId GetSkinId(UnityEngine.Object skin)
        {
#if UNITY_6000_3_OR_NEWER
            return skin.GetEntityId();
#else
            return skin.GetInstanceID();
#endif
        }

        private static void ApplyStyle(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active)
        {
            if (style == null) return;

            style.border = new RectOffset(SliceBorder, SliceBorder, SliceBorder, SliceBorder);
            SetState(style.normal, normal);
            SetState(style.hover, hover);
            SetState(style.active, active);
            SetState(style.focused, normal);
            SetState(style.onNormal, normal);
            SetState(style.onHover, hover);
            SetState(style.onActive, active);
            SetState(style.onFocused, normal);
        }

        private static void SetState(GUIStyleState state, Texture2D texture)
        {
            state.background = texture;
            state.scaledBackgrounds = Array.Empty<Texture2D>();
        }

        private static void UpdateTextures()
        {
            EnsureTextures();

            Color columnColor = TBStyles.ColumnBgAlt;
            Color trackColor = new Color(columnColor.r, columnColor.g, columnColor.b, columnColor.a * 0.28f);
            Color thumbColor = Color.Lerp(columnColor, TBStyles.CardTextColor, 0.30f);
            Color thumbHoverColor = Color.Lerp(columnColor, TBStyles.CardTextColor, 0.45f);
            Color thumbActiveColor = TBStyles.TabActiveColor;

            FillRoundedTexture(_trackTexture, trackColor);
            FillRoundedTexture(_thumbTexture, thumbColor);
            FillRoundedTexture(_thumbHoverTexture, thumbHoverColor);
            FillRoundedTexture(_thumbActiveTexture, thumbActiveColor);

            _texturesDirty = false;
        }

        private static void EnsureTextures()
        {
            _trackTexture ??= CreateTexture("ATM Scrollbar Track");
            _thumbTexture ??= CreateTexture("ATM Scrollbar Thumb");
            _thumbHoverTexture ??= CreateTexture("ATM Scrollbar Thumb Hover");
            _thumbActiveTexture ??= CreateTexture("ATM Scrollbar Thumb Active");
        }

        private static Texture2D CreateTexture(string textureName)
        {
            return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = textureName,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static void FillRoundedTexture(Texture2D texture, Color color)
        {
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int coveredSamples = 0;
                    for (int sampleY = 0; sampleY < AntialiasSamples; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < AntialiasSamples; sampleX++)
                        {
                            float px = x + (sampleX + 0.5f) / AntialiasSamples;
                            float py = y + (sampleY + 0.5f) / AntialiasSamples;
                            if (IsInsideRoundedRect(px, py))
                                coveredSamples++;
                        }
                    }

                    float coverage = coveredSamples / (float)(AntialiasSamples * AntialiasSamples);
                    pixels[y * TextureSize + x] = new Color(color.r, color.g, color.b, color.a * coverage);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        private static bool IsInsideRoundedRect(float x, float y)
        {
            const float inset = 2f;
            const float radius = 3f;
            float halfSize = TextureSize * 0.5f - inset;
            float qx = Mathf.Abs(x - TextureSize * 0.5f) - (halfSize - radius);
            float qy = Mathf.Abs(y - TextureSize * 0.5f) - (halfSize - radius);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                             + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return distance <= 0f;
        }

        private static void Cleanup()
        {
            foreach (KeyValuePair<SkinId, SkinEntry> pair in _skins)
            {
                SkinEntry entry = pair.Value;
                if (entry.Themed != null)
                {
                    _themedSkinIds.Remove(GetSkinId(entry.Themed));
                    UnityEngine.Object.DestroyImmediate(entry.Themed);
                }
            }
            _skins.Clear();
            _themedSkinIds.Clear();

            DestroyTexture(ref _trackTexture);
            DestroyTexture(ref _thumbTexture);
            DestroyTexture(ref _thumbHoverTexture);
            DestroyTexture(ref _thumbActiveTexture);
            _texturesDirty = true;
        }

        private static void DestroySkinEntry(SkinId sourceId, SkinEntry entry)
        {
            if (entry.Themed != null)
            {
                _themedSkinIds.Remove(GetSkinId(entry.Themed));
                UnityEngine.Object.DestroyImmediate(entry.Themed);
            }
            _skins.Remove(sourceId);
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }

        private static void LogWarningOnce(Exception exception)
        {
            if (_warningLogged) return;
            _warningLogged = true;
            Debug.LogWarning("ATM could not apply themed scrollbars; using the current editor skin. " + exception.Message);
        }
    }
}