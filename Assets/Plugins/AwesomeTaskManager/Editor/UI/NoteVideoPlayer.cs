using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Audio;
using UnityEngine.Experimental.Video;
using UnityEngine.Video;

namespace AwesomeTaskManager.Editor
{
    internal static class NoteVideoPlayer
    {
        private sealed class PlayerState
        {
            public string source;
            public string projectAssetPath;
            public GameObject gameObject;
            public VideoPlayer player;
            public AudioSource audioSource;
            public AudioClip streamClip;
            public RenderTexture renderTexture;
            public string error;
            public float aspect = 16f / 9f;
            public bool preparing;
            public bool prepared;
            public bool loop;
            public bool muted;
            public bool hasAudio;
            public float volume = 1f;
            public bool compatibilityChecked;
            public bool needsConversion;

            // Audio bridge: the VideoPlayer runs in APIOnly mode and the audio thread pulls the
            // decoded samples straight into a streaming AudioClip (the editor's audio engine does
            // not consume VideoPlayer audio outside Play Mode, which is what caused the
            // "AudioSampleProvider buffer overflow" warnings and the silence).
            public readonly object audioLock = new object();
            public AudioSampleProvider sampleProvider;
            public NativeArray<float> scratch;
            public int channelCount;
            public bool audioActive;
            public long lastConsumeTicks;
        }

        private const int ScratchSampleCapacity = 65536;

        private static readonly Dictionary<string, PlayerState> _players = new Dictionary<string, PlayerState>();
        private static bool _updateSubscribed;

        static NoteVideoPlayer()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeAll;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeAll;
        }

        public static bool Draw(string noteId, string source)
        {
            if (Event.current == null || string.IsNullOrEmpty(source)) return false;

            string key = (noteId ?? string.Empty) + "\n" + source;
            if (!_players.TryGetValue(key, out PlayerState state))
            {
                state = new PlayerState { source = source };
                TryGetProjectAssetPath(source, out state.projectAssetPath);
                _players.Add(key, state);
            }

            if (state.player == null)
            {
                DrawPlaceholder(state);
                DrawCompatibilityControls(state);
                return state.preparing;
            }

            DrawFrame(state);
            DrawControls(state);
            DrawCompatibilityControls(state);
            RefreshUpdateSubscription();
            return state.preparing || (state.player != null && state.player.isPlaying);
        }

        private static void DrawPlaceholder(PlayerState state)
        {
            float width = GetPlayerWidth();
            float height = width * 9f / 16f;
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));

            if (!string.IsNullOrEmpty(state.error))
            {
                GUI.Label(rect, "⚠ " + state.error, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var buttonRect = new Rect(rect.center.x - 68f, rect.center.y - 15f, 136f, 30f);
            if (GUI.Button(buttonRect, "▶  Play video"))
                StartPlayback(state);
            GUILayout.Space(2);
        }

        private static void DrawCompatibilityControls(PlayerState state)
        {
            if (string.IsNullOrEmpty(state.projectAssetPath)) return;

            if (!state.compatibilityChecked)
            {
                state.compatibilityChecked = true;
                var currentImporter = AssetImporter.GetAtPath(state.projectAssetPath) as VideoClipImporter;
                state.needsConversion = currentImporter != null && !NoteVideoImportSettings.IsEditorPlaybackReady(currentImporter);
            }
            if (!state.needsConversion) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Convert this video to play it with sound and without decoder warnings.", EditorStyles.miniLabel);
                if (GUILayout.Button("Convert for editor playback", EditorStyles.miniButton, GUILayout.Width(170)) &&
                    EditorUtility.DisplayDialog("Convert video for editor playback",
                        "Unity will re-import this video as VP8/WebM with its audio track so it plays with Unity's built-in decoder. " +
                        "The original file is not modified. This can take a while for large videos.",
                        "Convert", "Cancel"))
                {
                    ConvertForEditorPlayback(state);
                }
            }
        }

        private static void ConvertForEditorPlayback(PlayerState state)
        {
            try
            {
                ReleasePlaybackObjects(state);
                var importer = AssetImporter.GetAtPath(state.projectAssetPath) as VideoClipImporter;
                if (importer == null) return;

                EditorUtility.DisplayProgressBar("Converting video", Path.GetFileName(state.projectAssetPath), 0.5f);
                if (NoteVideoImportSettings.ApplyEditorPlaybackSettings(importer))
                    importer.SaveAndReimport();
                state.error = null;
            }
            catch (Exception exception)
            {
                state.error = "Video conversion failed: " + exception.Message;
            }
            finally
            {
                state.compatibilityChecked = false;
                EditorUtility.ClearProgressBar();
                EditorApplication.delayCall += () => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static bool TryGetProjectAssetPath(string source, out string assetPath)
        {
            assetPath = null;
            if (string.IsNullOrEmpty(source) || IsHttpUrl(source)) return false;

            string normalizedSource = source.Replace('\\', '/');
            if (normalizedSource.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                assetPath = normalizedSource;
                return true;
            }

            try
            {
                string fullPath = Path.GetFullPath(source).Replace('\\', '/');
                string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
                string dataPrefix = dataPath + "/";
                if (!fullPath.StartsWith(dataPrefix, StringComparison.OrdinalIgnoreCase)) return false;

                assetPath = "Assets/" + fullPath.Substring(dataPrefix.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void DrawFrame(PlayerState state)
        {
            float width = GetPlayerWidth();
            float height = width / Mathf.Max(0.1f, state.aspect);
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, new Color(0.06f, 0.06f, 0.06f));

            if (state.renderTexture != null && state.prepared)
                GUI.DrawTexture(rect, state.renderTexture, ScaleMode.ScaleToFit);
            else if (!string.IsNullOrEmpty(state.error))
                GUI.Label(rect, "⚠ " + state.error, EditorStyles.centeredGreyMiniLabel);
            else
                GUI.Label(rect, "Preparing video…", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(2);
        }

        private static void DrawControls(PlayerState state)
        {
            if (state.player == null) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!state.prepared))
                {
                    if (GUILayout.Button(state.player.isPlaying ? "❚❚" : "▶", GUILayout.Width(32)))
                    {
                        if (state.player.isPlaying)
                        {
                            state.player.Pause();
                            PauseAudio(state);
                        }
                        else
                        {
                            state.player.Play();
                            EnsureAudioPlaying(state);
                        }
                        RefreshUpdateSubscription();
                    }

                    if (GUILayout.Button("■", GUILayout.Width(32)))
                    {
                        StopAudio(state);
                        state.player.Stop();
                        state.prepared = false;
                        state.preparing = true;
                        state.player.Prepare();
                        RefreshUpdateSubscription();
                    }

                    double duration = state.player.length;
                    if (duration > 0d)
                    {
                        EditorGUI.BeginChangeCheck();
                        float time = GUILayout.HorizontalSlider((float)state.player.time, 0f, (float)duration);
                        if (EditorGUI.EndChangeCheck())
                        {
                            state.player.time = time;
                            DrainAudio(state);
                        }
                        GUILayout.Label(FormatTime(state.player.time) + " / " + FormatTime(duration),
                            EditorStyles.miniLabel, GUILayout.Width(82));
                    }
                    else
                    {
                        GUILayout.Label("--:--", EditorStyles.miniLabel, GUILayout.Width(82));
                    }
                }

                using (new EditorGUI.DisabledScope(!state.prepared))
                {
                    if (GUILayout.Button(state.muted ? "Unmute" : "Mute", GUILayout.Width(55)))
                    {
                        state.muted = !state.muted;
                        ApplyAudioSettings(state);
                    }
                    EditorGUI.BeginChangeCheck();
                    state.volume = GUILayout.HorizontalSlider(state.volume, 0f, 1f, GUILayout.Width(50));
                    if (EditorGUI.EndChangeCheck()) ApplyAudioSettings(state);
                    state.loop = GUILayout.Toggle(state.loop, "Loop", GUILayout.Width(48));
                    state.player.isLooping = state.loop;
                }
            }

            if (state.prepared && !state.hasAudio)
            {
                EditorGUILayout.LabelField("This video has no audio track.", EditorStyles.miniLabel);
            }
            else if (EditorUtility.audioMasterMute)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🔇 Editor audio is muted (Game view \"Mute Audio\").", EditorStyles.miniLabel);
                    if (GUILayout.Button("Unmute Editor", EditorStyles.miniButton, GUILayout.Width(90)))
                        EditorUtility.audioMasterMute = false;
                }
            }

            if (!string.IsNullOrEmpty(state.error))
                EditorGUILayout.LabelField("⚠ " + state.error, EditorStyles.miniLabel);
        }

        /// <summary>
        /// Claims the first audio track before Prepare(). The VideoPlayer only decodes audio for
        /// tracks that are enabled up front, and URL sources don't know their track count yet.
        /// </summary>
        private static void ConfigureAudio(PlayerState state, bool expectAudio)
        {
            if (!expectAudio)
            {
                state.player.audioOutputMode = VideoAudioOutputMode.None;
                state.player.controlledAudioTrackCount = 0;
                return;
            }

            state.player.audioOutputMode = VideoAudioOutputMode.APIOnly;
            state.player.controlledAudioTrackCount = 1;
            state.player.EnableAudioTrack(0, true);
        }

        private static void ApplyAudioSettings(PlayerState state)
        {
            if (state.audioSource != null)
            {
                state.audioSource.volume = state.volume;
                state.audioSource.mute = state.muted;
            }
        }

        /// <summary>Hooks the prepared VideoPlayer's sample provider up to a streaming AudioClip.</summary>
        private static void SetupAudioBridge(PlayerState state)
        {
            ReleaseAudioBridge(state);
            state.hasAudio = false;
            if (state.player == null || state.audioSource == null) return;
            if (state.player.audioOutputMode != VideoAudioOutputMode.APIOnly) return;
            if (state.player.audioTrackCount == 0 || state.player.controlledAudioTrackCount == 0) return;

            try
            {
                AudioSampleProvider provider = state.player.GetAudioSampleProvider(0);
                if (provider == null || provider.channelCount == 0 || provider.sampleRate == 0) return;

                provider.enableSampleFramesAvailableEvents = false;
                provider.enableSilencePadding = false;

                int channels = provider.channelCount;
                int sampleRate = (int)provider.sampleRate;
                var scratch = new NativeArray<float>(ScratchSampleCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                lock (state.audioLock)
                {
                    state.sampleProvider = provider;
                    state.scratch = scratch;
                    state.channelCount = channels;
                    state.lastConsumeTicks = DateTime.UtcNow.Ticks;
                    state.audioActive = true;
                }

                // One second of streamed audio; the clip loops so the reader callback keeps pulling
                // fresh samples from the video for as long as the AudioSource plays.
                state.streamClip = AudioClip.Create("ATM Note Video Audio", sampleRate, channels, sampleRate, true,
                    data => ReadVideoSamples(state, data));
                state.streamClip.hideFlags = HideFlags.HideAndDontSave;
                state.audioSource.clip = state.streamClip;
                state.audioSource.loop = true;
                ApplyAudioSettings(state);
                state.hasAudio = true;
            }
            catch (Exception exception)
            {
                ReleaseAudioBridge(state);
                state.error = "Audio unavailable: " + exception.Message;
            }
        }

        // Runs on the audio thread.
        private static void ReadVideoSamples(PlayerState state, float[] data)
        {
            int written = 0;
            try
            {
                lock (state.audioLock)
                {
                    if (state.audioActive && state.sampleProvider != null && state.scratch.IsCreated)
                    {
                        int channels = Math.Max(1, state.channelCount);
                        int frames = Math.Min(data.Length / channels, state.scratch.Length / channels);
                        if (frames > 0)
                        {
                            NativeArray<float> view = state.scratch.GetSubArray(0, frames * channels);
                            uint consumed = state.sampleProvider.ConsumeSampleFrames(view);
                            written = Math.Min(data.Length, (int)consumed * channels);
                            if (written > 0) NativeArray<float>.Copy(view, 0, data, 0, written);
                        }
                        state.lastConsumeTicks = DateTime.UtcNow.Ticks;
                    }
                }
            }
            catch
            {
                written = 0;
            }

            if (written < data.Length)
                Array.Clear(data, written, data.Length - written);
        }

        private static void EnsureAudioPlaying(PlayerState state)
        {
            if (!state.hasAudio || state.audioSource == null || state.audioSource.clip == null) return;
            if (state.audioSource.isPlaying) return;
            EnsureAudioListener(state);
            state.audioSource.Play();
        }

        // Buffered samples are kept on pause: they are the ones that follow the paused position.
        private static void PauseAudio(PlayerState state)
        {
            if (state.audioSource != null && state.audioSource.isPlaying) state.audioSource.Pause();
        }

        private static void StopAudio(PlayerState state)
        {
            if (state.audioSource != null) state.audioSource.Stop();
            DrainAudio(state);
        }

        /// <summary>Discards buffered samples so stale audio isn't heard after a seek or stop.</summary>
        private static void DrainAudio(PlayerState state)
        {
            lock (state.audioLock)
            {
                if (state.sampleProvider == null || !state.scratch.IsCreated) return;
                try
                {
                    int channels = Math.Max(1, state.channelCount);
                    int frames = state.scratch.Length / channels;
                    NativeArray<float> view = state.scratch.GetSubArray(0, frames * channels);
                    for (int i = 0; i < 16 && state.sampleProvider.availableSampleFrameCount > 0; i++)
                    {
                        if (state.sampleProvider.ConsumeSampleFrames(view) == 0) break;
                    }
                }
                catch
                {
                    // Provider went away; nothing to drain.
                }
            }
        }

        /// <summary>
        /// Audio is only audible with an AudioListener. Scenes normally have one on the camera;
        /// otherwise a hidden one is added to the player object (and destroyed with it).
        /// </summary>
        private static void EnsureAudioListener(PlayerState state)
        {
            if (state.gameObject == null) return;
            if (state.gameObject.GetComponent<AudioListener>() != null) return;
            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>(FindObjectsInactive.Exclude) != null) return;
            state.gameObject.AddComponent<AudioListener>();
        }

        private static void ReleaseAudioBridge(PlayerState state)
        {
            NativeArray<float> scratch;
            lock (state.audioLock)
            {
                state.audioActive = false;
                state.sampleProvider = null;
                scratch = state.scratch;
                state.scratch = default;
            }

            if (state.audioSource != null)
            {
                state.audioSource.Stop();
                state.audioSource.clip = null;
            }
            if (state.streamClip != null)
            {
                UnityEngine.Object.DestroyImmediate(state.streamClip);
                state.streamClip = null;
            }
            if (scratch.IsCreated) scratch.Dispose();
            state.hasAudio = false;
        }

        private static void StartPlayback(PlayerState state)
        {
            try
            {
                state.gameObject = new GameObject("ATM Note Video Player")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                state.player = state.gameObject.AddComponent<VideoPlayer>();
                state.audioSource = state.gameObject.AddComponent<AudioSource>();
                state.audioSource.playOnAwake = false;
                state.audioSource.spatialBlend = 0f;
                state.player.playOnAwake = false;
                state.player.waitForFirstFrame = true;
                state.player.isLooping = state.loop;
                state.player.skipOnDrop = true;
                state.player.renderMode = VideoRenderMode.RenderTexture;
                state.renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                state.renderTexture.Create();
                state.player.targetTexture = state.renderTexture;
                state.player.prepareCompleted += _ => OnPrepared(state);
                state.player.errorReceived += (_, message) =>
                {
                    state.error = message;
                    state.preparing = false;
                    state.prepared = false;
                    EditorApplication.delayCall += () => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };

                VideoClip clip = null;
                if (!string.IsNullOrEmpty(state.projectAssetPath))
                    clip = AssetDatabase.LoadAssetAtPath<VideoClip>(state.projectAssetPath);

                if (clip != null)
                {
                    state.player.source = VideoSource.VideoClip;
                    state.player.clip = clip;
                    ConfigureAudio(state, clip.audioTrackCount > 0);
                }
                else
                {
                    string path = state.source;
                    if (!IsHttpUrl(path)) path = Path.GetFullPath(path);
                    state.player.source = VideoSource.Url;
                    state.player.url = path;
                    ConfigureAudio(state, true);
                }
                state.preparing = true;
                state.player.Prepare();
                RefreshUpdateSubscription();
            }
            catch (Exception exception)
            {
                state.error = exception.Message;
                state.preparing = false;
            }
        }

        private static void OnPrepared(PlayerState state)
        {
            if (state.player == null) return;
            state.preparing = false;
            state.prepared = true;
            SetupAudioBridge(state);

            ulong videoWidth = state.player.width;
            ulong videoHeight = state.player.height;
            if (videoWidth > 0 && videoHeight > 0)
            {
                state.aspect = (float)videoWidth / videoHeight;
                float scale = Mathf.Min(1f, 1920f / Mathf.Max(videoWidth, videoHeight));
                int width = Mathf.Max(1, Mathf.RoundToInt(videoWidth * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(videoHeight * scale));
                state.player.targetTexture = null;
                if (state.renderTexture != null)
                {
                    state.renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(state.renderTexture);
                }
                state.renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                state.renderTexture.Create();
                state.player.targetTexture = state.renderTexture;
            }

            EditorApplication.delayCall += () => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            RefreshUpdateSubscription();
        }

        private static float GetPlayerWidth()
        {
            return Mathf.Max(120f, Mathf.Min(MarkdownRenderer.GetAvailablePreviewWidth(null, 40f), 480f));
        }

        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0d) seconds = 0d;
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1d ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }

        private static bool IsHttpUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out Uri uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static void RefreshUpdateSubscription()
        {
            bool needsUpdate = false;
            foreach (PlayerState state in _players.Values)
            {
                if (state.preparing || (state.player != null && state.player.isPlaying))
                    needsUpdate = true;
            }

            if (needsUpdate && !_updateSubscribed)
            {
                EditorApplication.update += UpdatePlayers;
                _updateSubscribed = true;
            }
            else if (!needsUpdate && _updateSubscribed)
            {
                EditorApplication.update -= UpdatePlayers;
                _updateSubscribed = false;
            }
        }

        private static void UpdatePlayers()
        {
            bool needsUpdate = false;
            foreach (PlayerState state in _players.Values)
            {
                if (state.player == null) continue;

                if (state.player.isPlaying && !state.player.isPaused)
                {
                    EnsureAudioPlaying(state);
                    GuardAgainstAudioOverflow(state);
                }
                else if (state.audioSource != null && state.audioSource.isPlaying)
                {
                    // Video finished (or was paused elsewhere): stop pulling samples.
                    PauseAudio(state);
                }

                if (state.preparing || state.player.isPlaying)
                    needsUpdate = true;
            }

            if (!needsUpdate)
            {
                EditorApplication.update -= UpdatePlayers;
                _updateSubscribed = false;
                return;
            }

            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// If nothing has pulled samples for a while (no audio device, editor muted at OS level, ...)
        /// the provider is drained so Unity doesn't log "AudioSampleProvider buffer overflow".
        /// </summary>
        private static void GuardAgainstAudioOverflow(PlayerState state)
        {
            if (!state.hasAudio) return;
            lock (state.audioLock)
            {
                if (state.sampleProvider == null || !state.scratch.IsCreated) return;
                try
                {
                    double idleSeconds = (DateTime.UtcNow.Ticks - state.lastConsumeTicks) / (double)TimeSpan.TicksPerSecond;
                    uint available = state.sampleProvider.availableSampleFrameCount;
                    uint max = state.sampleProvider.maxSampleFrameCount;
                    if (idleSeconds < 0.25d || max == 0 || available < max / 2) return;

                    int channels = Math.Max(1, state.channelCount);
                    int frames = state.scratch.Length / channels;
                    NativeArray<float> view = state.scratch.GetSubArray(0, frames * channels);
                    for (int i = 0; i < 16 && state.sampleProvider.availableSampleFrameCount > 0; i++)
                    {
                        if (state.sampleProvider.ConsumeSampleFrames(view) == 0) break;
                    }
                }
                catch
                {
                    // Provider went away; nothing to drain.
                }
            }
        }

        public static void DisposeAll()
        {
            if (_updateSubscribed)
            {
                EditorApplication.update -= UpdatePlayers;
                _updateSubscribed = false;
            }

            foreach (PlayerState state in _players.Values)
            {
                ReleasePlaybackObjects(state);
            }
            _players.Clear();
        }

        private static void ReleasePlaybackObjects(PlayerState state)
        {
            ReleaseAudioBridge(state);
            if (state.player != null)
            {
                state.player.Stop();
                state.player.targetTexture = null;
            }
            if (state.gameObject != null)
                UnityEngine.Object.DestroyImmediate(state.gameObject);
            if (state.renderTexture != null)
            {
                state.renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(state.renderTexture);
            }

            state.player = null;
            state.audioSource = null;
            state.gameObject = null;
            state.renderTexture = null;
            state.prepared = false;
            state.preparing = false;
        }
    }
}