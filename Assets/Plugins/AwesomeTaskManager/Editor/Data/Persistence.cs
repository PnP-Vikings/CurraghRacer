using System.IO;
using UnityEngine;

namespace AwesomeTaskManager.Data
{
    public static class Persistence
    {
        private const string FileName = "AwesomeTaskManager.json";

        private static string GetSavePath()
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

        public static SaveData Load()
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                return CreateFreshData();
            }

            // Check if empty
            if (new FileInfo(path).Length == 0)
            {
                Debug.LogWarning("[AwesomeTaskManager] Save file is empty.");
                var backupData = TryLoad(path + ".bak");
                if (backupData != null) return backupData;

                // Existing file but no recoverable data: do not return fresh data,
                // otherwise callers may overwrite the user's file with defaults.
                return null;
            }

            var data = TryLoad(path);
            if (data != null) return data;

            Debug.LogError("[AwesomeTaskManager] Main save corrupted, trying backup.");
            var backup = TryLoad(path + ".bak");
            if (backup != null) return backup;

            // Existing file but both main and backup failed to deserialize.
            // Return null so UI can show an error instead of silently resetting.
            return null;
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
    }
}

