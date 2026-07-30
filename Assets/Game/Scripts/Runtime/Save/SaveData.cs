using System;
using System.Collections.Generic;

namespace RealmShards.Save
{
    /// <summary>
    /// Versioned persistent save payload. Stable string IDs only — never asset fileIDs.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public long lastSavedUnixUtc;
        public MetaProgressionData meta = new MetaProgressionData();
        public SettingsData settings = new SettingsData();
        public RunStateData activeRun;
    }

    [Serializable]
    public sealed class MetaProgressionData
    {
        public int year = 1000;
        public int decade = 100;
        public int arcaneVestiges;

        public List<string> unlockedAbilityIds = new List<string>
        {
            ContentIdDefaults.AbilityBasicBolt
        };

        /// <summary>Shared loadout slots (Stage 2: applied to all local players).</summary>
        public List<string> equippedAbilityIds = new List<string>
        {
            ContentIdDefaults.AbilityBasicBolt,
            string.Empty,
            string.Empty,
            string.Empty
        };

        public List<string> unlockedCityIds = new List<string>
        {
            ContentIdDefaults.CityStarter,
            ContentIdDefaults.CityGildedWard,
            ContentIdDefaults.CityAshenQuay
        };

        public string selectedCityId = ContentIdDefaults.CityStarter;
        public int preferredPreCapitalNodes = 2;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public int localPlayerCount = 1;
    }

    [Serializable]
    public sealed class RunStateData
    {
        public string cityId;
        public string routeId;
        public int seed;
        public int roomIndex;
        public int worldNodeIndex;
        public bool isActive;
        public bool isCapital;
        public string routePlanJson;
        public string customJson;
    }

    public static class ContentIdDefaults
    {
        public const string AbilityBasicBolt = "ability.basic_bolt";
        public const string AbilityArcanePulse = "ability.arcane_pulse";
        public const string AbilityBlinkStep = "ability.blink_step";
        public const string AbilityDash = "ability.dash";

        public const string AbilityGildedFlare = "ability.gilded_flare";
        public const string AbilityGildedSmite = "ability.gilded_smite";
        public const string AbilityAshenDrift = "ability.ashen_drift";
        public const string AbilityAshenCinder = "ability.ashen_cinder";
        public const string AbilityTideglassRipple = "ability.tideglass_ripple";
        public const string AbilityTideglassHarpoon = "ability.tideglass_harpoon";
        public const string AbilityContinuumSlip = "ability.continuum_slip";
        public const string AbilityContinuumEcho = "ability.continuum_echo";

        public const string SchoolNeutral = "school.neutral";
        public const string SchoolGilded = "school.gilded_ward";
        public const string SchoolAshen = "school.ashen_veil";
        public const string SchoolTideglass = "school.tideglass";
        public const string SchoolContinuum = "school.continuum";

        public const string CityStarter = "city.starter";
        public const string CityGildedWard = "city.gilded_ward";
        public const string CityAshenQuay = "city.ashen_quay";
        public const string CityCapital = "city.capital";

        public const string RouteStarterMain = "route.starter.main";
        public const string RouteWorldMain = "route.world.main";

        public const string ItemHeartward = "item.heartward_amulet";
        public const string ItemEmberSpark = "item.ember_spark_relic";
        public const string ItemMindthread = "item.mindthread_band";
        public const string ItemNeedleShard = "item.needle_shard";
        public const string ItemWideningHalo = "item.widening_halo";
        public const string ItemStridefeather = "item.stridefeather";
        public const string ItemLodestone = "item.lodestone_charm";
        public const string ItemChronoweave = "item.chronoweave_sand";
        public const string ItemIronvine = "item.ironvine_charm";
        public const string ItemGlassmarrow = "item.glassmarrow_phial";

        public const string AbilityGildedBastion = "ability.gilded_bastion";
        public const string AbilityAshenHowl = "ability.ashen_howl";
    }

    public static class UnlockCosts
    {
        public const int ArcanePulse = 15;
        public const int BlinkStep = 20;
        public const int DashLegacy = 25;
    }
}
