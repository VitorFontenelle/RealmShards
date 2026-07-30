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
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long lastSavedUnixUtc;
        public MetaProgressionData meta = new MetaProgressionData();
        public SettingsData settings = new SettingsData();
        public RunStateData activeRun;
    }

    [Serializable]
    public sealed class MetaProgressionData
    {
        /// <summary>Campaign calendar year (advances +10 on run failure).</summary>
        public int year = 1000;

        /// <summary>Decade index derived from year (year / 10). Cached for UI.</summary>
        public int decade = 100;

        /// <summary>Soft currency: Arcane Vestiges.</summary>
        public int arcaneVestiges;

        /// <summary>Unlocked ability / skill definition IDs.</summary>
        public List<string> unlockedAbilityIds = new List<string>
        {
            ContentIdDefaults.AbilityBasicBolt
        };

        /// <summary>Equipped loadout ability IDs (placeholders; 4 slots).</summary>
        public List<string> equippedAbilityIds = new List<string>
        {
            ContentIdDefaults.AbilityBasicBolt,
            string.Empty,
            string.Empty,
            string.Empty
        };

        /// <summary>Unlocked city / route IDs.</summary>
        public List<string> unlockedCityIds = new List<string>
        {
            ContentIdDefaults.CityStarter
        };

        public string selectedCityId = ContentIdDefaults.CityStarter;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public int localPlayerCount = 1;
    }

    /// <summary>
    /// Optional in-progress run snapshot. Cleared on run end.
    /// World/combat agents may extend via customJson.
    /// </summary>
    [Serializable]
    public sealed class RunStateData
    {
        public string cityId;
        public string routeId;
        public int seed;
        public int roomIndex;
        public bool isActive;
        public string customJson;
    }

    /// <summary>Stable content IDs shared with ContentDatabase stubs.</summary>
    public static class ContentIdDefaults
    {
        public const string AbilityBasicBolt = "ability.basic_bolt";
        public const string AbilityDash = "ability.dash";
        public const string CityStarter = "city.starter";
        public const string RouteStarterMain = "route.starter.main";
    }
}
