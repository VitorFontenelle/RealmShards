using System;
using System.IO;
using UnityEngine;

namespace RealmShards.Save
{
    /// <summary>
    /// Versioned JSON save under Application.persistentDataPath.
    /// Writes via temp file then replace; keeps a .bak backup.
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        private const string FileName = "save.json";
        private const string BackupSuffix = ".bak";
        private const string TempSuffix = ".tmp";

        private readonly string _fileName;
        private SaveData _current;

        public JsonSaveService() : this(FileName) { }

        /// <summary>Test / alternate profile path under persistentDataPath.</summary>
        public JsonSaveService(string fileName)
        {
            _fileName = string.IsNullOrEmpty(fileName) ? FileName : fileName;
        }

        public SaveData Current => _current ??= CreateDefault();

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, _fileName);

        public bool HasSaveFile => File.Exists(SaveFilePath);

        public SaveData LoadOrCreate()
        {
            if (TryLoad(SaveFilePath, out var data))
            {
                _current = Migrate(data);
                return _current;
            }

            var backupPath = SaveFilePath + BackupSuffix;
            if (TryLoad(backupPath, out data))
            {
                Debug.LogWarning("[Save] Primary save missing/corrupt; restored from backup.");
                _current = Migrate(data);
                Save(_current);
                return _current;
            }

            _current = CreateDefault();
            Save(_current);
            return _current;
        }

        public void Save()
        {
            Save(Current);
        }

        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.version = SaveData.CurrentVersion;
            data.lastSavedUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _current = data;

            var path = SaveFilePath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(data, true);
            var tempPath = path + TempSuffix;
            var backupPath = path + BackupSuffix;

            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                // Atomic replace keeps a backup of the previous good file.
                File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        public void DeleteSave()
        {
            TryDelete(SaveFilePath);
            TryDelete(SaveFilePath + BackupSuffix);
            TryDelete(SaveFilePath + TempSuffix);
            _current = CreateDefault();
        }

        private static bool TryLoad(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<SaveData>(json);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Save] Failed to read '{path}': {ex.Message}");
                return false;
            }
        }

        private static SaveData Migrate(SaveData data)
        {
            if (data.version < 1)
                data.version = 1;

            data.meta ??= new MetaProgressionData();
            data.settings ??= new SettingsData();
            data.meta.unlockedAbilityIds ??= new System.Collections.Generic.List<string>();
            data.meta.equippedAbilityIds ??= new System.Collections.Generic.List<string>();
            data.meta.unlockedCityIds ??= new System.Collections.Generic.List<string>();

            if (data.meta.unlockedAbilityIds.Count == 0)
                data.meta.unlockedAbilityIds.Add(ContentIdDefaults.AbilityBasicBolt);

            while (data.meta.equippedAbilityIds.Count < 4)
                data.meta.equippedAbilityIds.Add(string.Empty);

            // Strip equipped spells that are no longer unlocked (or were never unlocked).
            for (int i = 0; i < data.meta.equippedAbilityIds.Count; i++)
            {
                string id = data.meta.equippedAbilityIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!data.meta.unlockedAbilityIds.Contains(id))
                    data.meta.equippedAbilityIds[i] = string.Empty;
            }

            // Ensure at least slot 0 has the basic bolt when empty.
            if (string.IsNullOrEmpty(data.meta.equippedAbilityIds[0]) &&
                data.meta.unlockedAbilityIds.Contains(ContentIdDefaults.AbilityBasicBolt))
            {
                data.meta.equippedAbilityIds[0] = ContentIdDefaults.AbilityBasicBolt;
            }

            EnsureCity(data.meta.unlockedCityIds, ContentIdDefaults.CityStarter);
            EnsureCity(data.meta.unlockedCityIds, ContentIdDefaults.CityGildedWard);
            EnsureCity(data.meta.unlockedCityIds, ContentIdDefaults.CityAshenQuay);

            if (data.meta.preferredPreCapitalNodes < 1)
                data.meta.preferredPreCapitalNodes = 2;

            data.meta.decade = data.meta.year / 10;
            data.version = SaveData.CurrentVersion;
            return data;
        }

        private static void EnsureCity(System.Collections.Generic.List<string> list, string id)
        {
            if (!list.Contains(id))
                list.Add(id);
        }

        private static SaveData CreateDefault()
        {
            return Migrate(new SaveData());
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Save] Could not delete '{path}': {ex.Message}");
            }
        }
    }
}
