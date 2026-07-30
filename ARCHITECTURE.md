# Architecture (Stage 2)

## Composition root

`GameContext` (`RealmShards.Core`) is created via `RuntimeInitializeOnLoadMethod` and marked `DontDestroyOnLoad`.

It owns:

- `ISaveService` → `JsonSaveService`
- `ProgressionService` (year / decade / vestiges / unlocks)
- `IRunHost` → `RunHost` (world route → CityRun nodes → capital last → results)
- `ContentDatabase` (runtime stub catalog; optional SO later)
- `LocalCoopLobby` + binding overrides

Prefer `GameContext.Instance` over `FindObjectOfType` in `Update`.

## Scene flow

```
Bootstrap  --load save-->  Hub  --Start Run-->  CityRun  --EndRun-->  RunResults  --> Hub
```

| Scene | Role |
|-------|------|
| Bootstrap | Load/create save, jump to Hub |
| Hub | Local lobby UI (1–4 slots), loadout, Start Run |
| CityRun | Multi-room combat (`CityRunDirector`: 2–3 trash → champion) + HUD + pause |
| RunResults | Outcome UI; failure applies +10 year in `RunHost` |

Scene name constants: `RealmShards.Core.SceneNames`.

## CityRun multi-room

1. `CityRunBootstrap` builds arena, spawns players, starts `CityRunDirector`.
2. Director plans rooms from seed + node (`CityRoomPlanner`) — trash encounters then champion.
3. **First room clear does not end the run** — only `CityRunDirector.CityCompleted` (after champion room).
4. Champion death → Arcane Core → unlock UI → `CityRunMetaBridge` advances world route / ends run.
5. Decade-gated `ChampionSelector.Pick(seed, year)` chooses champion definition.

## Combat UX

- `CombatHud` — per-player HP, ability CD pips, inventory strip (1280×800 scaler)
- `PauseMenu` — Resume / Controls / Quit to Hub; uses `HitStop` so `timeScale` is restored across scenes
- `PlayerLocatePresenter` + `SharedOrthoCamera.PulseLocate` / catch-up pull
- `DamageNumberService` + `HitStop` on heavy hits
- `AudioEventHub` cast/hit/death stubs

## Cross-agent contracts

- **End a run:** `GameContext.Instance.Runs.EndRun(RunOutcome.Success/Failure(...))`
- **Begin a run:** `GameContext.Instance.Runs.BeginWorldRun(...)` / `BeginRun(...)`
- **Progression:** `GameContext.Instance.Progression`
- **Suppress CityRun stub:** implement `RealmShards.UI.ICityRunReady`
- **Physics / sorting layers:** `GameLayers` / `SortingLayers`

## Assemblies

- `RealmShards.Runtime` — gameplay scripts
- `RealmShards.Editor` — setup / art tools
- `RealmShards.EditModeTests` — EditMode NUnit tests under `Scripts/Tests/EditMode`

## Folder map

```
Assets/Game/
  Scripts/Runtime|Editor|Tests/EditMode
  Data/{Enemies,Champions,Encounters,Cities,...}
  Art/**/Generated|Processed
  Prefabs/{UI,Characters}
  Scenes/{Bootstrap,Hub,CityRun,RunResults}.unity
```

Do not overwrite `Assets/Characters/**` or `Assets/Tiles/**` source art.
