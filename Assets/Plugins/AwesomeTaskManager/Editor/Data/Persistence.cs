using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AwesomeTaskManager.Data
{
    public static class Persistence
    {
        private const string FileName = "AwesomeTaskManager.json";
        private const string ThemeFileName = "AwesomeTaskManagerTheme.json";
        private const string UserFileName = "AwesomeTaskManagerUser.json";
        private const string BoardsFolderName = "Boards";
        private const string NotesFolderName = "Notes";

        public static string SavePathOverride { get; set; } = null;
        public static string ThemeSavePathOverride { get; set; } = null;
        public static string UserSavePathOverride { get; set; } = null;
        public static SaveData InMemoryDataOverride { get; set; } = null;
        public static bool SuppressReloadEvents { get; set; } = false;

        public static event Action OnDiskDataChanged;

        public static string GetSaveDirectory()
        {
            if (!string.IsNullOrEmpty(SavePathOverride))
            {
                string overrideDir = Path.GetDirectoryName(SavePathOverride);
                return string.IsNullOrEmpty(overrideDir) ? "." : overrideDir;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string dir = Path.Combine(projectRoot, "ProjectSettings", "AwesomeTaskManager");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create directory: " + e.Message); }
            }
            return dir;
        }

        public static string GetBoardsDirectory()
        {
            string baseDir = GetSaveDirectory();
            if (string.IsNullOrEmpty(baseDir)) return string.Empty;
            string dir = Path.Combine(baseDir, BoardsFolderName);
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create boards directory: " + e.Message); }
            }
            return dir;
        }

        public static string GetNotesDirectory()
        {
            string baseDir = GetSaveDirectory();
            if (string.IsNullOrEmpty(baseDir)) return string.Empty;
            string dir = Path.Combine(baseDir, NotesFolderName);
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create notes directory: " + e.Message); }
            }
            return dir;
        }

        public static string GetSavePath()
        {
            if (!string.IsNullOrEmpty(SavePathOverride)) return SavePathOverride;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string dir = Path.Combine(projectRoot, "ProjectSettings", "AwesomeTaskManager");
            
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create directory: " + e.Message); }
            }

            string newPath = Path.Combine(dir, FileName);
            
            // Migration: load from old location if new doesn't exist yet
            if (!File.Exists(newPath))
            {
                string oldDir = Path.Combine(projectRoot, "ProjectSettings", "TheUltimateTaskBoard");
                string oldPath = Path.Combine(oldDir, "TheUltimateTaskBoard.json");
                if (File.Exists(oldPath))
                {
                    try
                    {
                        File.Copy(oldPath, newPath);
                        Debug.Log("[AwesomeTaskManager] Migrated data from TheUltimateTaskBoard.");
                    }
                    catch (Exception e) { Debug.LogWarning("[AwesomeTaskManager] Migration failed: " + e.Message); }
                }
            }
            return newPath;
        }

        public static string GetThemeSavePath()
        {
            if (!string.IsNullOrEmpty(ThemeSavePathOverride)) return ThemeSavePathOverride;
            string dir = GetSaveDirectory();
            if (string.IsNullOrEmpty(dir)) return string.Empty;
            return Path.Combine(dir, ThemeFileName);
        }

        public static string GetUserSavePath()
        {
            if (!string.IsNullOrEmpty(UserSavePathOverride)) return UserSavePathOverride;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string dir = Path.Combine(projectRoot, "UserSettings", "AwesomeTaskManager");

            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create UserSettings directory: " + e.Message); }
            }

            return Path.Combine(dir, UserFileName);
        }

        public static SaveData Load()
        {
            if (InMemoryDataOverride != null && string.IsNullOrEmpty(SavePathOverride))
            {
                string json = JsonUtility.ToJson(InMemoryDataOverride);
                var clone = JsonUtility.FromJson<SaveData>(json);
                clone.themes = InMemoryDataOverride.themes;
                clone.themeSettings = InMemoryDataOverride.themeSettings;
                clone.userSettings = InMemoryDataOverride.userSettings;
                clone.currentThemeIndex = InMemoryDataOverride.currentThemeIndex;
                clone.Normalize();
                return clone;
            }

            string path = GetSavePath();
            SaveData data = null;

            if (File.Exists(path) && new FileInfo(path).Length > 0)
            {
                data = TryLoad(path);
                if (data == null)
                {
                    Debug.LogError("[AwesomeTaskManager] Main save corrupted, trying backup.");
                    var backup = TryLoad(path + ".bak");
                    if (backup != null) data = backup;
                }
            }

            if (data == null)
            {
                data = CreateFreshData();
            }

            // Load modular boards if Boards directory exists and has files
            string boardsDir = GetBoardsDirectory();
            if (!string.IsNullOrEmpty(boardsDir) && Directory.Exists(boardsDir))
            {
                var boardFiles = Directory.GetFiles(boardsDir, "*.json");
                if (boardFiles.Length > 0)
                {
                    var loadedBoards = new List<TaskBoard>();
                    foreach (var file in boardFiles)
                    {
                        try
                        {
                            string bJson = File.ReadAllText(file);
                            if (!string.IsNullOrWhiteSpace(bJson))
                            {
                                var b = JsonUtility.FromJson<TaskBoard>(bJson);
                                if (b != null && !string.IsNullOrEmpty(b.id))
                                {
                                    loadedBoards.Add(b);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[AwesomeTaskManager] Failed to load board file " + file + ": " + ex.Message);
                        }
                    }

                    if (loadedBoards.Count > 0)
                    {
                        // Order loaded boards matching manifest order if present
                        if (data.boards != null && data.boards.Count > 0)
                        {
                            var orderedList = new List<TaskBoard>();
                            foreach (var manifestBoard in data.boards)
                            {
                                if (manifestBoard == null) continue;
                                var match = loadedBoards.FirstOrDefault(b => b.id == manifestBoard.id);
                                if (match != null)
                                {
                                    orderedList.Add(match);
                                    loadedBoards.Remove(match);
                                }
                            }
                            orderedList.AddRange(loadedBoards);
                            data.boards = orderedList;
                        }
                        else
                        {
                            data.boards = loadedBoards;
                        }
                    }
                }
            }

            // Load modular notes if Notes directory exists and has files
            string notesDir = GetNotesDirectory();
            if (!string.IsNullOrEmpty(notesDir) && Directory.Exists(notesDir))
            {
                var noteFiles = Directory.GetFiles(notesDir, "*.json");
                if (noteFiles.Length > 0)
                {
                    var loadedNotes = new List<QuickNote>();
                    foreach (var file in noteFiles)
                    {
                        try
                        {
                            string nJson = File.ReadAllText(file);
                            if (!string.IsNullOrWhiteSpace(nJson))
                            {
                                var n = JsonUtility.FromJson<QuickNote>(nJson);
                                if (n != null && !string.IsNullOrEmpty(n.id))
                                {
                                    loadedNotes.Add(n);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[AwesomeTaskManager] Failed to load note file " + file + ": " + ex.Message);
                        }
                    }

                    if (loadedNotes.Count > 0)
                    {
                        data.notes = loadedNotes;
                    }
                }
            }

            if (data != null)
            {
                var themeSettings = LoadTheme(data);
                data.themeSettings = themeSettings;
                data.themes = themeSettings.themes;
                data.currentThemeIndex = themeSettings.currentThemeIndex;

                var userSettings = LoadUser(data);
                data.userSettings = userSettings;
                if (!string.IsNullOrEmpty(userSettings.activeBoardId) && data.boards != null)
                {
                    int foundIdx = data.boards.FindIndex(b => b != null && b.id == userSettings.activeBoardId);
                    if (foundIdx >= 0)
                    {
                        data.lastBoardIndex = foundIdx;
                        userSettings.lastBoardIndex = foundIdx;
                    }
                    else if (userSettings.lastBoardIndex >= 0 && userSettings.lastBoardIndex < data.boards.Count)
                    {
                        data.lastBoardIndex = userSettings.lastBoardIndex;
                    }
                }
                else if (userSettings.lastBoardIndex >= 0 && data.boards != null && userSettings.lastBoardIndex < data.boards.Count)
                {
                    data.lastBoardIndex = userSettings.lastBoardIndex;
                }

                // Resolve user-specific theme selection
                if (!string.IsNullOrEmpty(userSettings.selectedThemeName) && data.themes != null && data.themes.Count > 0)
                {
                    int foundThemeIdx = data.themes.FindIndex(t => t != null && string.Equals(t.name, userSettings.selectedThemeName, StringComparison.OrdinalIgnoreCase));
                    if (foundThemeIdx >= 0)
                    {
                        data.currentThemeIndex = foundThemeIdx;
                        userSettings.currentThemeIndex = foundThemeIdx;
                        userSettings.selectedThemeName = data.themes[foundThemeIdx].name;
                    }
                    else if (userSettings.currentThemeIndex >= 0 && userSettings.currentThemeIndex < data.themes.Count)
                    {
                        data.currentThemeIndex = userSettings.currentThemeIndex;
                        userSettings.selectedThemeName = data.themes[userSettings.currentThemeIndex]?.name ?? string.Empty;
                    }
                }
                else if (!string.IsNullOrEmpty(themeSettings?.selectedThemeName) && data.themes != null && data.themes.Count > 0)
                {
                    int foundThemeIdx = data.themes.FindIndex(t => t != null && string.Equals(t.name, themeSettings.selectedThemeName, StringComparison.OrdinalIgnoreCase));
                    if (foundThemeIdx >= 0)
                    {
                        data.currentThemeIndex = foundThemeIdx;
                    }
                    else
                    {
                        data.currentThemeIndex = Mathf.Clamp(themeSettings.currentThemeIndex, 0, data.themes.Count - 1);
                    }
                    userSettings.currentThemeIndex = data.currentThemeIndex;
                    userSettings.selectedThemeName = data.themes[data.currentThemeIndex]?.name ?? string.Empty;
                }
                else if (userSettings.currentThemeIndex >= 0 && data.themes != null && userSettings.currentThemeIndex < data.themes.Count)
                {
                    data.currentThemeIndex = userSettings.currentThemeIndex;
                    userSettings.selectedThemeName = data.themes[userSettings.currentThemeIndex]?.name ?? string.Empty;
                }

                data.Normalize();

                // Auto-migrate legacy monolithic boards/notes to modular directories if not already created
                bool needsModularMigration = false;
                if (!string.IsNullOrEmpty(boardsDir) && Directory.Exists(boardsDir))
                {
                    var boardFiles = Directory.GetFiles(boardsDir, "*.json");
                    if (boardFiles.Length == 0 && data.boards != null && data.boards.Count > 0)
                    {
                        needsModularMigration = true;
                    }
                }
                if (!string.IsNullOrEmpty(notesDir) && Directory.Exists(notesDir))
                {
                    var noteFiles = Directory.GetFiles(notesDir, "*.json");
                    if (noteFiles.Length == 0 && data.notes != null && data.notes.Count > 0)
                    {
                        needsModularMigration = true;
                    }
                }

                if (needsModularMigration)
                {
                    Save(data);
                }
            }

            return data;
        }

        private static SaveData TryLoad(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[AwesomeTaskManager] Failed to deserialize data from " + path + ".");
                    return null;
                }

                data.Normalize();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Failed to load " + path + ": " + e.Message);
                return null;
            }
        }

        public static ThemeSaveData LoadTheme(SaveData fallbackData = null)
        {
            if (InMemoryDataOverride != null && string.IsNullOrEmpty(ThemeSavePathOverride) && string.IsNullOrEmpty(SavePathOverride))
            {
                var fallbackTheme = fallbackData?.themeSettings ?? InMemoryDataOverride.themeSettings ?? new ThemeSaveData();
                fallbackTheme.Normalize();
                return fallbackTheme;
            }

            string themePath = GetThemeSavePath();
            ThemeSaveData themeData = null;

            if (File.Exists(themePath) && new FileInfo(themePath).Length > 0)
            {
                themeData = TryLoadTheme(themePath);
                if (themeData == null)
                {
                    Debug.LogError("[AwesomeTaskManager] Main theme save corrupted, trying backup.");
                    themeData = TryLoadTheme(themePath + ".bak");
                }
            }

            // Migration: if theme file doesn't exist yet, extract from legacy save file or fallback
            if (themeData == null)
            {
                themeData = new ThemeSaveData();
                string mainSavePath = GetSavePath();
                if (File.Exists(mainSavePath))
                {
                    try
                    {
                        string json = File.ReadAllText(mainSavePath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var legacyData = JsonUtility.FromJson<ThemeSaveData>(json);
                            if (legacyData != null && legacyData.themes != null && legacyData.themes.Count > 0)
                            {
                                themeData = legacyData;
                            }
                        }
                    }
                    catch { }
                }

                themeData.Normalize();
                SaveTheme(themeData);
            }
            else
            {
                themeData.Normalize();
            }

            return themeData;
        }

        private static ThemeSaveData TryLoadTheme(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                var data = JsonUtility.FromJson<ThemeSaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[AwesomeTaskManager] Failed to deserialize theme data from " + path + ".");
                    return null;
                }

                data.Normalize();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Failed to load theme " + path + ": " + e.Message);
                return null;
            }
        }

        public static UserSaveData LoadUser(SaveData fallbackData = null)
        {
            if (InMemoryDataOverride != null && string.IsNullOrEmpty(UserSavePathOverride) && string.IsNullOrEmpty(SavePathOverride))
            {
                var fallback = fallbackData?.userSettings ?? InMemoryDataOverride.userSettings ?? new UserSaveData();
                fallback.Normalize();
                return fallback;
            }

            string userPath = GetUserSavePath();
            UserSaveData userData = null;

            if (!string.IsNullOrEmpty(userPath) && File.Exists(userPath) && new FileInfo(userPath).Length > 0)
            {
                try
                {
                    string json = File.ReadAllText(userPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        userData = JsonUtility.FromJson<UserSaveData>(json);
                    }
                }
                catch { }
            }

            if (userData == null)
            {
                int fallbackThemeIdx = fallbackData?.currentThemeIndex ?? fallbackData?.themeSettings?.currentThemeIndex ?? 0;
                string fallbackThemeName = string.Empty;
                if (fallbackData?.themes != null && fallbackThemeIdx >= 0 && fallbackThemeIdx < fallbackData.themes.Count && fallbackData.themes[fallbackThemeIdx] != null)
                {
                    fallbackThemeName = fallbackData.themes[fallbackThemeIdx].name;
                }
                else if (!string.IsNullOrEmpty(fallbackData?.themeSettings?.selectedThemeName))
                {
                    fallbackThemeName = fallbackData.themeSettings.selectedThemeName;
                }

                userData = new UserSaveData
                {
                    lastBoardIndex = fallbackData?.lastBoardIndex ?? 0,
                    localUserName = VcsHelper.GetCurrentUserName(),
                    currentThemeIndex = fallbackThemeIdx,
                    selectedThemeName = fallbackThemeName
                };
            }

            userData.Normalize();
            return userData;
        }

        public static void SaveUser(UserSaveData userData)
        {
            if (userData == null) return;
            if (InMemoryDataOverride != null && string.IsNullOrEmpty(UserSavePathOverride) && string.IsNullOrEmpty(SavePathOverride))
            {
                InMemoryDataOverride.userSettings = userData;
                return;
            }

            string userPath = GetUserSavePath();
            if (string.IsNullOrEmpty(userPath)) return;

            try
            {
                userData.Normalize();
                string json = JsonUtility.ToJson(userData, true);
                WriteAtomicFile(userPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Save user settings failed: " + e.Message);
            }
        }

        private static SaveData CreateFreshData()
        {
            var newData = new SaveData();
            newData.Normalize();
            return newData;
        }

        public static void Save(SaveData data)
        {
            if (data == null) return;

            if (InMemoryDataOverride != null)
            {
                data.Normalize();
                string inMemoryJson = JsonUtility.ToJson(data);
                InMemoryDataOverride = JsonUtility.FromJson<SaveData>(inMemoryJson);
                InMemoryDataOverride.themes = data.themes;
                InMemoryDataOverride.themeSettings = data.themeSettings;
                InMemoryDataOverride.userSettings = data.userSettings;
                InMemoryDataOverride.currentThemeIndex = data.currentThemeIndex;
                return;
            }

            data.Normalize();

            // Save user settings to UserSettings/
            var user = data.userSettings ?? new UserSaveData();
            user.lastBoardIndex = data.lastBoardIndex;
            if (data.boards != null && data.lastBoardIndex >= 0 && data.lastBoardIndex < data.boards.Count && data.boards[data.lastBoardIndex] != null)
            {
                user.activeBoardId = data.boards[data.lastBoardIndex].id;
            }
            user.currentThemeIndex = data.currentThemeIndex;
            if (data.themes != null && data.currentThemeIndex >= 0 && data.currentThemeIndex < data.themes.Count && data.themes[data.currentThemeIndex] != null)
            {
                user.selectedThemeName = data.themes[data.currentThemeIndex].name;
            }
            SaveUser(user);

            string mainPath = GetSavePath();
            string boardsDir = GetBoardsDirectory();
            string notesDir = GetNotesDirectory();

            try
            {
                // 1. Save individual boards in Boards/ folder
                if (!string.IsNullOrEmpty(boardsDir) && Directory.Exists(boardsDir))
                {
                    var savedBoardIds = new HashSet<string>();
                    if (data.boards != null)
                    {
                        foreach (var board in data.boards)
                        {
                            if (board == null || string.IsNullOrEmpty(board.id)) continue;
                            savedBoardIds.Add(board.id);
                            string bPath = Path.Combine(boardsDir, $"{board.id}.json");
                            WriteAtomicFile(bPath, JsonUtility.ToJson(board, true));
                        }
                    }

                    // Delete removed board files
                    var existingFiles = Directory.GetFiles(boardsDir, "*.json");
                    foreach (var file in existingFiles)
                    {
                        string id = Path.GetFileNameWithoutExtension(file);
                        if (!savedBoardIds.Contains(id))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }

                // 2. Save individual notes in Notes/ folder
                if (!string.IsNullOrEmpty(notesDir) && Directory.Exists(notesDir))
                {
                    var savedNoteIds = new HashSet<string>();
                    if (data.notes != null)
                    {
                        foreach (var note in data.notes)
                        {
                            if (note == null || string.IsNullOrEmpty(note.id)) continue;
                            savedNoteIds.Add(note.id);
                            string nPath = Path.Combine(notesDir, $"{note.id}.json");
                            WriteAtomicFile(nPath, JsonUtility.ToJson(note, true));
                        }
                    }

                    // Delete removed note files
                    var existingNoteFiles = Directory.GetFiles(notesDir, "*.json");
                    foreach (var file in existingNoteFiles)
                    {
                        string id = Path.GetFileNameWithoutExtension(file);
                        if (!savedNoteIds.Contains(id))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }

                // 3. Save main manifest file (AwesomeTaskManager.json)
                string mainJson = JsonUtility.ToJson(data, true);
                WriteAtomicFile(mainPath, mainJson);
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Save failed: " + e.Message);
            }
        }

        public static void SaveTheme(ThemeSaveData themeData)
        {
            if (themeData == null) return;

            if (InMemoryDataOverride != null && string.IsNullOrEmpty(ThemeSavePathOverride) && string.IsNullOrEmpty(SavePathOverride))
            {
                themeData.Normalize();
                InMemoryDataOverride.themeSettings = themeData;
                InMemoryDataOverride.themes = themeData.themes;
                InMemoryDataOverride.currentThemeIndex = themeData.currentThemeIndex;
                return;
            }

            string path = GetThemeSavePath();
            try
            {
                themeData.Normalize();
                string json = JsonUtility.ToJson(themeData, true);
                WriteAtomicFile(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Save theme failed: " + e.Message);
            }
        }

        private static void WriteAtomicFile(string path, string content)
        {
            if (string.IsNullOrEmpty(path)) return;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                if (File.Exists(path))
                {
                    string backupPath = path + ".bak";
                    try { File.Copy(path, backupPath, true); } catch { }
                }
                File.WriteAllText(path, content);
            }
            catch (Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Write file failed for " + path + ": " + e.Message);
            }
        }

        public static long GetDiskTimestamp()
        {
            if (InMemoryDataOverride != null && string.IsNullOrEmpty(SavePathOverride)) return 0;
            string mainPath = GetSavePath();
            long maxTicks = 0;

            if (File.Exists(mainPath))
            {
                maxTicks = Math.Max(maxTicks, File.GetLastWriteTimeUtc(mainPath).Ticks);
            }

            string boardsDir = GetBoardsDirectory();
            if (!string.IsNullOrEmpty(boardsDir) && Directory.Exists(boardsDir))
            {
                var files = Directory.GetFiles(boardsDir, "*.json");
                foreach (var f in files)
                {
                    maxTicks = Math.Max(maxTicks, File.GetLastWriteTimeUtc(f).Ticks);
                }
            }

            string notesDir = GetNotesDirectory();
            if (!string.IsNullOrEmpty(notesDir) && Directory.Exists(notesDir))
            {
                var files = Directory.GetFiles(notesDir, "*.json");
                foreach (var f in files)
                {
                    maxTicks = Math.Max(maxTicks, File.GetLastWriteTimeUtc(f).Ticks);
                }
            }

            return maxTicks;
        }

        public static bool HasDiskChanged(long lastKnownTimestamp)
        {
            if (lastKnownTimestamp <= 0) return false;
            long current = GetDiskTimestamp();
            return current > lastKnownTimestamp;
        }

        public static void NotifyDiskDataChanged()
        {
            try
            {
                OnDiskDataChanged?.Invoke();
            }
            catch { }
        }
    }
}
