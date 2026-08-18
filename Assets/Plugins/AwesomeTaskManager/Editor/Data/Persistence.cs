using System.IO;
using UnityEngine;

namespace AwesomeTaskManager.Data
{
    public static class Persistence
    {
        private const string FileName = "AwesomeTaskManager.json";
        private const string ThemeFileName = "AwesomeTaskManagerTheme.json";

        public static string GetSavePath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string dir = Path.Combine(projectRoot, "ProjectSettings", "AwesomeTaskManager");
            
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (System.Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create directory: " + e.Message); }
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
                    catch (System.Exception e) { Debug.LogWarning("[AwesomeTaskManager] Migration failed: " + e.Message); }
                }
            }
            return newPath;
        }

        public static string GetThemeSavePath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            string dir = Path.Combine(projectRoot, "ProjectSettings", "AwesomeTaskManager");
            
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (System.Exception e) { Debug.LogError("[AwesomeTaskManager] Failed to create directory: " + e.Message); }
            }

            return Path.Combine(dir, ThemeFileName);
        }

        public static SaveData Load()
        {
            string path = GetSavePath();
            SaveData data = null;

            if (!File.Exists(path))
            {
                data = CreateFreshData();
            }
            else if (new FileInfo(path).Length == 0)
            {
                Debug.LogWarning("[AwesomeTaskManager] Save file is empty.");
                var backupData = TryLoad(path + ".bak");
                if (backupData != null) data = backupData;
                else return null;
            }
            else
            {
                data = TryLoad(path);
                if (data == null)
                {
                    Debug.LogError("[AwesomeTaskManager] Main save corrupted, trying backup.");
                    var backup = TryLoad(path + ".bak");
                    if (backup != null) data = backup;
                    else return null;
                }
            }

            if (data != null)
            {
                var themeSettings = LoadTheme(data);
                data.themeSettings = themeSettings;
                data.themes = themeSettings.themes;
                data.currentThemeIndex = themeSettings.currentThemeIndex;
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

                int boardCount = data.boards != null ? data.boards.Count : 0;
                int noteCount = data.notes != null ? data.notes.Count : 0;
                // Corruption check: If we have significant data in the file, we should have it in memory.
                if (boardCount == 0 && noteCount == 0 && new FileInfo(path).Length > 0)
                {
                    Debug.LogError("[AwesomeTaskManager] Data in " + path + " seems corrupted.");
                    return null;
                }

                data.Normalize();
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Failed to load " + path + ": " + e.Message);
                return null;
            }
        }

        public static ThemeSaveData LoadTheme(SaveData fallbackData = null)
        {
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
            catch (System.Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Failed to load theme " + path + ": " + e.Message);
                return null;
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
            string path = GetSavePath();
            string tempPath = path + ".tmp";
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tempPath, json);
                
                if (File.Exists(path))
                {
                    string backupPath = path + ".bak";
                    File.Copy(path, backupPath, true);
                }
                
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Save failed: " + e.Message);
            }
        }

        public static void SaveTheme(ThemeSaveData themeData)
        {
            if (themeData == null) return;
            string path = GetThemeSavePath();
            string tempPath = path + ".tmp";
            try
            {
                themeData.Normalize();
                string json = JsonUtility.ToJson(themeData, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    string backupPath = path + ".bak";
                    File.Copy(path, backupPath, true);
                }

                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AwesomeTaskManager] Save theme failed: " + e.Message);
            }
        }
    }
}

