# Save system

## Location

`Application.persistentDataPath/save.json`  
Backup: `save.json.bak`

## Format

Versioned JSON (`SaveData.version`, current = **1**) via `JsonUtility`.

### Payload

| Field | Notes |
|-------|--------|
| `meta.year` | Campaign year (default 1000) |
| `meta.decade` | `year / 10` |
| `meta.arcaneVestiges` | Soft currency |
| `meta.unlockedAbilityIds` | Stable string IDs |
| `meta.equippedAbilityIds` | 4 loadout slots (placeholders) |
| `meta.unlockedCityIds` / `selectedCityId` | City unlocks |
| `settings.*` | Volume + `localPlayerCount` |
| `activeRun` | Optional mid-run snapshot; cleared on run end |

**Never** persist Unity asset fileIDs — only string content IDs.

## Write safety

1. Serialize to `save.json.tmp`
2. `File.Replace` into `save.json` with backup to `save.json.bak`
3. On load failure of primary, attempt backup restore

## API

```csharp
GameContext.Instance.Save.LoadOrCreate();
GameContext.Instance.Save.Save();
GameContext.Instance.Progression.AdvanceDecadeOnFailure(); // also used by RunHost on failure
```

## Run failure rule

On `RunResultKind.Failure`, `RunHost` advances year by **+10**, saves, then loads `RunResults`.
