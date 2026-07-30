# Architecture (Stage 2)

## Composition root

`GameContext` (`RealmShards.Core`) is created via `RuntimeInitializeOnLoadMethod` and marked `DontDestroyOnLoad`.

It owns:

- `ISaveService` → `JsonSaveService`
- `ProgressionService` (year / decade / vestiges / unlocks)
- `IRunHost` → `RunHost` (begin/end city runs)
- `ContentDatabase` (runtime stub catalog; optional SO later)

Prefer `GameContext.Instance` over `FindObjectOfType` in `Update`.

## Scene flow

```
Bootstrap  --load save-->  Hub  --Start Run-->  CityRun  --EndRun-->  RunResults  --> Hub
```

| Scene | Role |
|-------|------|
| Bootstrap | Load/create save, jump to Hub |
| Hub | Local lobby UI (1–4 slots), loadout placeholders, Start Run |
| CityRun | Level (world agent). Meta ships a Win/Fail stub until then |
| RunResults | Outcome UI; failure already applied +10 year in `RunHost` |

Scene name constants: `RealmShards.Core.SceneNames`.

## Cross-agent contracts

- **End a run:** `GameContext.Instance.Runs.EndRun(RunOutcome.Success/Failure(...))`
- **Begin a run:** `GameContext.Instance.Runs.BeginRun(cityId, routeId, playerCount)`
- **Progression:** `GameContext.Instance.Progression` (`Year`, `Decade`, `AdvanceDecadeOnFailure`, `AddArcaneVestiges`, `UnlockAbility`)
- **Content IDs:** stable strings (`ability.basic_bolt`, `city.starter`, …) via `ContentDatabase` / `ContentIdDefaults`
- **City run glue:** `CityRunMetaBridge` ensures `CityRunBootstrap`, shows End Win/Fail HUD, auto-wins on `EncounterRoom.Cleared`
- **Suppress full-screen stub:** implement `RealmShards.UI.ICityRunReady` (meta bridge already does)
- **Player prefab path:** `Assets/Game/Prefabs/Characters/Player.prefab`
- **Physics layers:** `RealmShards.Core.GameLayers` (6–15 required; 16 = `Projectile` combat alias)
- **Sorting layers:** `RealmShards.Core.SortingLayers`
- **City SO:** `RealmShards.Runs.CityDefinition` (world/route agent)

## Folder map

```
Assets/Game/
  Scripts/Runtime/{Core,Save,Progression,UI,Runs}
  Data/{Progression,Cities}
  Prefabs/{UI,Characters}
  Scenes/{Bootstrap,Hub,CityRun,RunResults}.unity
  Settings/
```

Do not overwrite `Assets/Characters/**` or `Assets/Tiles/**` source art.
