using System;
using UnityEditor;

namespace AwesomeTaskManager.Editor
{
    /// <summary>
    /// Import settings for videos attached to notes.
    /// Videos are transcoded to VP8/Vorbis (WebM) on import so the editor plays them with Unity's
    /// built-in software decoder instead of Windows Media Foundation. This avoids the
    /// "Unexpected timestamp values" / "Color primaries ... unknown" messages during playback
    /// and guarantees that the audio track is imported.
    /// </summary>
    public static class NoteVideoImportSettings
    {
        public const string AttachedImagesFolder = "Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/";

        public static bool IsNoteVideoAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return false;
            string normalized = assetPath.Replace('\\', '/');
            return normalized.StartsWith(AttachedImagesFolder, StringComparison.OrdinalIgnoreCase) &&
                   MarkdownRenderer.IsVideoPath(normalized);
        }

        public static bool IsEditorPlaybackReady(VideoClipImporter importer)
        {
            if (importer == null || !importer.importAudio) return false;
            if (!IsReady(GetDefaultSettings(importer))) return false;

            // A platform override for the active build target would take precedence in the editor.
            VideoImporterTargetSettings activeSettings = null;
            try { activeSettings = importer.GetTargetSettings(GetActivePlatformName()); }
            catch { /* unknown platform name: no override */ }
            return activeSettings == null || IsReady(activeSettings);
        }

        /// <summary>Applies the editor-playback settings. Returns true when something changed.</summary>
        public static bool ApplyEditorPlaybackSettings(VideoClipImporter importer)
        {
            if (importer == null || IsEditorPlaybackReady(importer)) return false;

            VideoImporterTargetSettings settings = GetDefaultSettings(importer) ?? new VideoImporterTargetSettings();
            settings.enableTranscoding = true;
            settings.codec = VideoCodec.VP8;
            importer.defaultTargetSettings = settings;
            try { importer.SetTargetSettings("Default", settings); }
            catch { /* older importer API: defaultTargetSettings already applied */ }
            try
            {
                string activePlatform = GetActivePlatformName();
                if (importer.GetTargetSettings(activePlatform) != null)
                    importer.ClearTargetSettings(activePlatform);
            }
            catch { /* unknown platform name: nothing to clear */ }
            importer.importAudio = true;
            return true;
        }

        private static bool IsReady(VideoImporterTargetSettings settings)
        {
            return settings != null && settings.enableTranscoding && settings.codec == VideoCodec.VP8;
        }

        private static VideoImporterTargetSettings GetDefaultSettings(VideoClipImporter importer)
        {
            try
            {
                VideoImporterTargetSettings settings = importer.GetTargetSettings("Default");
                if (settings != null) return settings;
            }
            catch { /* fall back to the property */ }
            return importer.defaultTargetSettings;
        }

        private static string GetActivePlatformName()
        {
            return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
        }
    }

    /// <summary>Applies <see cref="NoteVideoImportSettings"/> the first time a note video is imported.</summary>
    internal sealed class NoteVideoImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAsset()
        {
            if (!(assetImporter is VideoClipImporter importer)) return;
            if (!importer.importSettingsMissing) return;
            if (!NoteVideoImportSettings.IsNoteVideoAsset(assetPath)) return;

            NoteVideoImportSettings.ApplyEditorPlaybackSettings(importer);
        }
    }
}
