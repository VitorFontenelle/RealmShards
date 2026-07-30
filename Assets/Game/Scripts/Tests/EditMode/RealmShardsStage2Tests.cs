using System.Collections.Generic;
using NUnit.Framework;
using RealmShards.Progression;
using RealmShards.Rooms;
using RealmShards.Runs;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.Tests.EditMode
{
    public sealed class WorldRouteGeneratorTests
    {
        [Test]
        public void Capital_Is_Always_Last()
        {
            for (int seed = 1; seed < 40; seed++)
            {
                var plan = WorldRouteGenerator.Generate(seed, preCapitalCount: 3);
                Assert.GreaterOrEqual(plan.NodeCount, 2);
                Assert.AreEqual(WorldNodeKind.Capital, plan.nodes[plan.NodeCount - 1].kind);
                Assert.AreEqual(ContentIdDefaults.CityCapital, plan.nodes[plan.NodeCount - 1].cityId);
                for (int i = 0; i < plan.NodeCount - 1; i++)
                    Assert.AreNotEqual(WorldNodeKind.Capital, plan.nodes[i].kind);
            }
        }

        [Test]
        public void No_Duplicate_Cities_When_Pool_Allows()
        {
            var plan = WorldRouteGenerator.Generate(42, preCapitalCount: 3);
            var seen = new HashSet<string>();
            for (int i = 0; i < plan.NodeCount - 1; i++)
            {
                Assert.IsTrue(seen.Add(plan.nodes[i].cityId), $"Duplicate city {plan.nodes[i].cityId}");
            }
        }

        [Test]
        public void Same_Seed_Is_Deterministic()
        {
            var a = WorldRouteGenerator.Generate(12345, 2);
            var b = WorldRouteGenerator.Generate(12345, 2);
            Assert.AreEqual(a.NodeCount, b.NodeCount);
            for (int i = 0; i < a.NodeCount; i++)
            {
                Assert.AreEqual(a.nodes[i].cityId, b.nodes[i].cityId);
                Assert.AreEqual(a.nodes[i].kind, b.nodes[i].kind);
            }
        }
    }

    public sealed class DecadeAndUnlockTests
    {
        [Test]
        public void AdvanceDecade_Adds_Ten_Years()
        {
            var save = new JsonSaveService("test_decade_save.json");
            save.DeleteSave();
            save.LoadOrCreate();
            int start = save.Current.meta.year;
            var progression = new ProgressionService(save);
            progression.AdvanceDecadeOnFailure(saveImmediately: false);
            Assert.AreEqual(start + 10, progression.Year);
            Assert.AreEqual(progression.Year / 10, progression.Decade);
            save.DeleteSave();
        }

        [Test]
        public void Unlock_Prevents_Duplicate_Spend()
        {
            var save = new JsonSaveService("test_unlock_save.json");
            save.DeleteSave();
            save.LoadOrCreate();
            save.Current.meta.arcaneVestiges = 100;
            save.Save();
            var progression = new ProgressionService(save);

            Assert.IsTrue(progression.TryPurchaseAbilityUnlock(ContentIdDefaults.AbilityGildedFlare, 18, out _));
            Assert.IsTrue(progression.IsAbilityUnlocked(ContentIdDefaults.AbilityGildedFlare));
            int vestiges = progression.ArcaneVestiges;
            Assert.IsFalse(progression.TryPurchaseAbilityUnlock(ContentIdDefaults.AbilityGildedFlare, 18, out var reason));
            Assert.AreEqual("Already unlocked.", reason);
            Assert.AreEqual(vestiges, progression.ArcaneVestiges);
            save.DeleteSave();
        }
    }

    public sealed class SaveRoundtripTests
    {
        [Test]
        public void Save_Load_Roundtrip_Preserves_Meta()
        {
            var save = new JsonSaveService("test_roundtrip_save.json");
            save.DeleteSave();
            var data = save.LoadOrCreate();
            data.meta.year = 1040;
            data.meta.arcaneVestiges = 33;
            data.meta.unlockedAbilityIds.Add(ContentIdDefaults.AbilityAshenCinder);
            save.Save(data);

            var save2 = new JsonSaveService("test_roundtrip_save.json");
            var loaded = save2.LoadOrCreate();
            Assert.AreEqual(1040, loaded.meta.year);
            Assert.AreEqual(104, loaded.meta.decade);
            Assert.AreEqual(33, loaded.meta.arcaneVestiges);
            Assert.Contains(ContentIdDefaults.AbilityAshenCinder, loaded.meta.unlockedAbilityIds);
            save2.DeleteSave();
        }
    }

    public sealed class CityRoomPlannerTests
    {
        [Test]
        public void Trash_Then_Champion_Counts()
        {
            var plan = CityRoomPlanner.Build(99, 0, isCapital: false);
            Assert.GreaterOrEqual(plan.TrashRoomCount, 2);
            Assert.LessOrEqual(plan.TrashRoomCount, 3);
            Assert.AreEqual(plan.TrashRoomCount + 1, plan.TotalRooms);
            Assert.IsTrue(plan.IsChampionRoom(plan.ChampionRoomIndex));
            Assert.IsFalse(plan.IsChampionRoom(0));
        }

        [Test]
        public void Capital_Uses_Min_Trash()
        {
            var plan = CityRoomPlanner.Build(1, 0, isCapital: true);
            Assert.AreEqual(CityRoomPlanner.MinTrashRooms, plan.TrashRoomCount);
        }

        [Test]
        public void Planner_Is_Deterministic()
        {
            var a = CityRoomPlanner.Build(777, 2, false);
            var b = CityRoomPlanner.Build(777, 2, false);
            Assert.AreEqual(a.TotalRooms, b.TotalRooms);
            Assert.AreEqual(a.TrashRoomCount, b.TrashRoomCount);
        }
    }

    public sealed class ChampionSelectorTests
    {
        [Test]
        public void Pick_Is_Deterministic_For_Seed_And_Year()
        {
            ChampionSelector.ClearRuntimePool();
            var e1 = ScriptableObject.CreateInstance<Enemies.EnemyDefinition>();
            e1.ApplyRuntimeDefaults("A", Enemies.EnemyArchetype.Champion, 100, 2, null, Color.white);
            var c1 = ScriptableObject.CreateInstance<Enemies.ChampionDefinition>();
            c1.ConfigureRuntime("c.a", "A", e1, true, 0, 9999, 1f);
            var e2 = ScriptableObject.CreateInstance<Enemies.EnemyDefinition>();
            e2.ApplyRuntimeDefaults("B", Enemies.EnemyArchetype.Champion, 120, 2, null, Color.red);
            var c2 = ScriptableObject.CreateInstance<Enemies.ChampionDefinition>();
            c2.ConfigureRuntime("c.b", "B", e2, true, 0, 9999, 1f);
            ChampionSelector.RegisterRuntime(c1);
            ChampionSelector.RegisterRuntime(c2);

            var a = ChampionSelector.Pick(42, 1000);
            var b = ChampionSelector.Pick(42, 1000);
            Assert.AreEqual(a.ChampionId, b.ChampionId);

            ChampionSelector.ClearRuntimePool();
            Object.DestroyImmediate(e1);
            Object.DestroyImmediate(e2);
            Object.DestroyImmediate(c1);
            Object.DestroyImmediate(c2);
        }
    }
}
