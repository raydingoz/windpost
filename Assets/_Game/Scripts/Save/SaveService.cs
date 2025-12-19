using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Windpost.Save
{
    public sealed class SaveService : MonoBehaviour
    {
        private const int CurrentVersion = 1;

        [SerializeField] private string fileName = "windpost_save.json";
        [SerializeField] private bool log = true;

        public void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveService] Save called with null data.");
                return;
            }

            data.Version = CurrentVersion;

            var path = GetSavePath();
            try
            {
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
                if (log)
                {
                    Debug.Log($"[SaveService] Saved to: {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Save failed: {ex}");
            }
        }

        public bool TryLoad(out SaveData data)
        {
            data = null;

            var path = GetSavePath();
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    return false;
                }

                if (data.Version != CurrentVersion && log)
                {
                    Debug.LogWarning($"[SaveService] Save version mismatch: {data.Version} (expected {CurrentVersion}). Attempting best-effort load.");
                }

                if (log)
                {
                    Debug.Log($"[SaveService] Loaded from: {path}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Load failed: {ex}");
                data = null;
                return false;
            }
        }

        public void Delete()
        {
            var path = GetSavePath();
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    if (log)
                    {
                        Debug.Log($"[SaveService] Deleted save file: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Delete failed: {ex}");
            }
        }

        private string GetSavePath()
        {
            var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "windpost_save.json" : fileName;
            return Path.Combine(Application.persistentDataPath, safeFileName);
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        [Serializable]
        public struct FlagEntry
        {
            public string Key;
            public int Value;
        }

        public int Version = 0;
        public string Tone;
        public string Route;
        public string CurrentKnotOrPath;
        public List<FlagEntry> Flags = new List<FlagEntry>();

        public IReadOnlyList<KeyValuePair<string, int>> GetFlags()
        {
            if (Flags == null || Flags.Count == 0)
            {
                return Array.Empty<KeyValuePair<string, int>>();
            }

            var list = new List<KeyValuePair<string, int>>(Flags.Count);
            for (var i = 0; i < Flags.Count; i++)
            {
                var entry = Flags[i];
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                list.Add(new KeyValuePair<string, int>(entry.Key, entry.Value));
            }

            return list;
        }

        public void SetFlags(IReadOnlyDictionary<string, int> flags)
        {
            Flags ??= new List<FlagEntry>();
            Flags.Clear();

            if (flags == null)
            {
                return;
            }

            foreach (var pair in flags)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                Flags.Add(new FlagEntry { Key = pair.Key, Value = pair.Value });
            }
        }
    }
}

