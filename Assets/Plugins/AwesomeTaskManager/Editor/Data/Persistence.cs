using System.IO;
using UnityEngine;

namespace AwesomeTaskManager.Data
{
    public static class Persistence
    {
        private const string FileName = "AwesomeTaskManager.json";

        private static string GetSavePath()
        {
            string dir = Path.Combine(Application.dataPath, "..", "ProjectSettings", "AwesomeTaskManager");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            // Migration: load from old location if new doesn't exist yet
            string newPath = Path.Combine(dir, FileName);
            if (!System.IO.File.Exists(newPath))
            {
                string oldDir = Path.Combine(Application.dataPath, "..", "ProjectSettings", "TheUltimateTaskBoard");
                string oldPath = Path.Combine(oldDir, "TheUltimateTaskBoard.json");
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Copy(oldPath, newPath);
                    Debug.Log("[AwesomeTaskManager] Migrated data from TheUltimateTaskBoard.");
                }
            }
            return newPath;
        }

        public static SaveData Load()
        {
            string path = GetSavePath();
            if (File.Exists(path))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    data.Normalize();
                    return data;
                }
                catch (System.Exception e) { Debug.LogError("[AwesomeTaskManager] Load failed: " + e.Message); }
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try { File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data, true)); }
            catch (System.Exception e) { Debug.LogError("[AwesomeTaskManager] Save failed: " + e.Message); }
        }
    }
}

