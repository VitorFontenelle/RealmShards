#if UNITY_EDITOR
using RealmShards.Enemies;
using RealmShards.Rooms;
using RealmShards.Runs;
using RealmShards.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmShards.EditorTools
{
    /// <summary>
    /// Creates Stage 2 data assets and ensures CityRun bootstrap wiring.
    /// </summary>
    public static class CityRunStage2Setup
    {
        private const string ScenePath = "Assets/Game/Scenes/CityRun.unity";
        private const string MarkerPath = "Assets/Game/Data/.stage2_setup_done";

        [MenuItem("RealmShards/Setup CityRun Stage 2")]
        public static void RunSetup()
        {
            EnsurePlayerTag();
            var defs = CreateDataAssets();
            WireCityRunScene(defs);
            EnsureBuildSettings();
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Game/Data");
            System.IO.File.WriteAllText(Application.dataPath + "/Game/Data/.stage2_setup_done", "ok");
            AssetDatabase.Refresh();
            Debug.Log("[RealmShards] CityRun Stage 2 setup complete. Run Setup Player Content if needed, then Play from Bootstrap.");
        }

        [InitializeOnLoadMethod]
        private static void AutoCreateDataOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (System.IO.File.Exists(Application.dataPath + "/Game/Data/.stage2_setup_done"))
                    return;
                if (AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Game/Data/Enemies/GoldenAxeWarrior.asset") != null)
                    return;

                EnsurePlayerTag();
                CreateDataAssets();
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Game/Data");
                System.IO.File.WriteAllText(Application.dataPath + "/Game/Data/.stage2_setup_done", "ok");
                AssetDatabase.Refresh();
            };
        }

        private static void EnsurePlayerTag()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                return;
            var tagManager = new SerializedObject(assets[0]);
            var tags = tagManager.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == "Player")
                    return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Player";
            tagManager.ApplyModifiedProperties();
        }

        private struct DataBundle
        {
            public EnemyDefinition Warrior;
            public EnemyDefinition Archer;
            public EnemyDefinition Champion;
            public EncounterDefinition Encounter;
            public CoopScalingConfig Coop;
        }

        private static DataBundle CreateDataAssets()
        {
            EnsureFolder("Assets/Game/Data/Enemies");
            EnsureFolder("Assets/Game/Data/Encounters");
            EnsureFolder("Assets/Game/Data/Champions");
            EnsureFolder("Assets/Game/Data/Cities");
            EnsureFolder("Assets/Game/Data/Rooms");

            var warrior = LoadOrCreate<EnemyDefinition>("Assets/Game/Data/Enemies/GoldenAxeWarrior.asset");
            warrior.ApplyRuntimeDefaults("Golden Axe Warrior", EnemyArchetype.Warrior, 45f, 2.5f, EnemyFactory.KnightSheet, new Color(1f, 0.78f, 0.2f));
            warrior.ConfigureCombat(0, 6, 20, 6, 1.35f, 0f, 10f, 1.1f, 0.45f);
            EditorUtility.SetDirty(warrior);

            var archer = LoadOrCreate<EnemyDefinition>("Assets/Game/Data/Enemies/GoldenArcher.asset");
            archer.ApplyRuntimeDefaults("Golden Archer", EnemyArchetype.Archer, 28f, 2.1f, EnemyFactory.ArcherSheet, new Color(1f, 0.82f, 0.25f));
            archer.ConfigureCombat(0, 6, 24, 6, 9f, 5.5f, 7f, 1.4f, 0.55f);
            EditorUtility.SetDirty(archer);

            var champion = LoadOrCreate<EnemyDefinition>("Assets/Game/Data/Enemies/ArcaneCoreChampion.asset");
            champion.ApplyRuntimeDefaults("Arcane Core Champion", EnemyArchetype.Champion, 160f, 2.0f, EnemyFactory.KnightSheet, new Color(0.75f, 0.45f, 1f));
            champion.ConfigureCombat(0, 6, 20, 6, 1.5f, 0f, 14f, 1.0f, 0.55f, 1.1f);
            EditorUtility.SetDirty(champion);

            var champDef = LoadOrCreate<ChampionDefinition>("Assets/Game/Data/Champions/ArcaneCoreChampion.asset");
            var champSo = new SerializedObject(champDef);
            champSo.FindProperty("enemyDefinition").objectReferenceValue = champion;
            champSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(champDef);

            var encounter = LoadOrCreate<EncounterDefinition>("Assets/Game/Data/Encounters/CityRunSample.asset");
            encounter.SetRuntime(
                "cityrun-sample",
                new[]
                {
                    new EncounterDefinition.EnemySpawnEntry { definition = warrior, archetypeFallback = EnemyArchetype.Warrior, count = 2 },
                    new EncounterDefinition.EnemySpawnEntry { definition = archer, archetypeFallback = EnemyArchetype.Archer, count = 2 }
                },
                champion,
                true,
                "cityrun-clear");
            EditorUtility.SetDirty(encounter);

            var coop = LoadOrCreate<CoopScalingConfig>("Assets/Game/Data/Enemies/CoopScalingConfig.asset");
            EditorUtility.SetDirty(coop);

            var city = LoadOrCreate<CityDefinition>("Assets/Game/Data/Cities/SampleCity.asset");
            EditorUtility.SetDirty(city);

            AssetDatabase.SaveAssets();
            return new DataBundle
            {
                Warrior = warrior,
                Archer = archer,
                Champion = champion,
                Encounter = encounter,
                Coop = coop
            };
        }

        private static void WireCityRunScene(DataBundle defs)
        {
            if (!System.IO.File.Exists(Application.dataPath + "/Game/Scenes/CityRun.unity"))
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<CityRunBootstrap>();
            if (bootstrap == null)
            {
                var go = new GameObject("CityRunBootstrap");
                bootstrap = go.AddComponent<CityRunBootstrap>();
            }

            var so = new SerializedObject(bootstrap);
            so.FindProperty("encounterOverride").objectReferenceValue = defs.Encounter;
            so.FindProperty("coopScalingOverride").objectReferenceValue = defs.Coop;
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Player.prefab");
            if (player != null)
            {
                so.FindProperty("playerPrefab").objectReferenceValue = player;
            }
            else
            {
                Debug.LogWarning("[RealmShards] Player.prefab missing — run RealmShards → Setup Player Content, then re-run Setup CityRun Stage 2.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<RealmShards.CameraSystem.SharedOrthoCamera>() == null)
                cam.gameObject.AddComponent<RealmShards.CameraSystem.SharedOrthoCamera>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath)
                    return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
