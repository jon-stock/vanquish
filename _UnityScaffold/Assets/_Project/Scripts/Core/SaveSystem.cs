using System.IO;
using UnityEngine;

namespace Vanquish.Core
{
    /// <summary>
    /// Bare-bones JSON save/load. Phase 0 scope: single local save slot. Multiple
    /// slots, cloud save, and corruption/backup handling are post-MVP concerns.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "vanquish_save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Saved to {SavePath}");
        }

        public static SaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveSystem] No save file found, returning new SaveData.");
                return new SaveData();
            }

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data ?? new SaveData();
        }

        public static bool HasSave() => File.Exists(SavePath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
