#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RealmShards.Core;
using UnityEditor;
using UnityEngine;

namespace RealmShards.Editor
{
    /// <summary>
    /// Builds Resources/GameContent so standalone players can load sprites/prefabs without AssetDatabase.
    /// </summary>
    public static class RuntimeContentCatalogBuilder
    {
        private const string ResourcesDir = "Assets/Game/Resources";
        private const string CatalogPath = ResourcesDir + "/GameContent.asset";

        [MenuItem("RealmShards/Setup Runtime Content Catalog")]
        public static void BuildMenu() => Build();

        public static RuntimeContentCatalog Build()
        {
            EnsureFolder(ResourcesDir);

            var catalog = AssetDatabase.LoadAssetAtPath<RuntimeContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RuntimeContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Player.prefab");
            var floor = FirstSprite("Assets/Game/Art/Tiles/Generated/sample-tile_seamed.png")
                        ?? FirstSprite("Assets/Tiles/sample-tile.png");
            var knight = LoadSprites("Assets/Characters/Enemies/knight-spritesheet.png");
            var archer = LoadSprites("Assets/Characters/Enemies/archer-spritesheet.png");
            var mageRun = LoadSprites("Assets/Characters/Magus/running-spritesheet.png");
            var mageCast = LoadSprites("Assets/Characters/Magus/attacking-spritesheet.png");
            var mageIdle = FirstSprite("Assets/Characters/Magus/standing.png");
            var arrow = PickArrowSprite(archer);
            var abilities = LoadAbilityDefinitions();

            catalog.EditorAssign(player, floor, arrow, knight, archer, mageRun, mageCast, mageIdle, abilities);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[RealmShards] RuntimeContentCatalog ready at {CatalogPath}. " +
                $"Player={(player != null)}, Floor={(floor != null)}, Arrow={(arrow != null)}, " +
                $"Knight={knight.Length}, Archer={archer.Length}, MageRun={mageRun.Length}, Abilities={abilities.Length}.");

            if (player == null)
                Debug.LogWarning("[RealmShards] Player prefab missing — run Setup Player Content first.");
            if (floor == null)
                Debug.LogWarning("[RealmShards] Floor tile sprite missing — check Assets/Tiles/sample-tile.png import as Sprite.");
            if (knight.Length == 0 || archer.Length == 0)
                Debug.LogWarning("[RealmShards] Enemy sheets returned 0 sprites — ensure Sprite Mode Multiple.");

            return catalog;
        }

        private static AbilityDefinition[] LoadAbilityDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:AbilityDefinition", new[] { "Assets/Game/Data/Abilities" });
            var list = new List<AbilityDefinition>();
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var def = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
                if (def != null)
                    list.Add(def);
            }

            return list.OrderBy(a => a.ContentId, System.StringComparer.Ordinal).ToArray();
        }

        private static Sprite PickArrowSprite(Sprite[] archer)
        {
            if (archer == null || archer.Length == 0)
                return null;

            // Fired-arrow cells sit at column 9 of each 12-wide archer row (indices 9,21,33,...).
            int[] preferred = { 9, 21, 33, 45, 57, 69, 81, 93 };
            for (int i = 0; i < preferred.Length; i++)
            {
                int idx = preferred[i];
                if (idx >= 0 && idx < archer.Length && archer[idx] != null)
                    return archer[idx];
            }

            // Fallback: pick the most elongated small sprite in the sheet.
            Sprite best = archer[Mathf.Min(9, archer.Length - 1)];
            float bestScore = -1f;
            for (int i = 0; i < archer.Length; i++)
            {
                var s = archer[i];
                if (s == null) continue;
                float w = s.rect.width;
                float h = Mathf.Max(1f, s.rect.height);
                float aspect = w / h;
                float area = w * h;
                if (aspect < 1.4f || area > 4000f)
                    continue;
                float score = aspect / Mathf.Sqrt(area);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }

            return best;
        }

        private static Sprite FirstSprite(string path)
        {
            var all = LoadSprites(path);
            return all.Length > 0 ? all[0] : null;
        }

        private static Sprite[] LoadSprites(string texturePath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            if (assets == null || assets.Length == 0)
                return System.Array.Empty<Sprite>();

            var list = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite s)
                    list.Add(s);
            }

            return list
                .OrderBy(s => ExtractTrailingNumber(s.name))
                .ThenBy(s => s.name, System.StringComparer.Ordinal)
                .ToArray();
        }

        private static int ExtractTrailingNumber(string name)
        {
            if (string.IsNullOrEmpty(name))
                return int.MaxValue;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;
            if (i == name.Length - 1)
                return int.MaxValue;
            if (int.TryParse(name.Substring(i + 1), out int n))
                return n;
            return int.MaxValue;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
