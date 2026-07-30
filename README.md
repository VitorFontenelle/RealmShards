# RealmShards

Unity 6 (6000.5.6f1) URP 2D local co-op action game.

## Play mode (Stage 2 meta loop)

1. Open the project in Unity 6.
2. Open **`Assets/Game/Scenes/Bootstrap.unity`** (or press Play with Bootstrap as the first Build Settings scene).
3. Flow: **Bootstrap** → loads save → **Hub** → **Start Run** → **CityRun** (stub) → Win/Fail → **RunResults** → Hub.

Build Settings scene order: Bootstrap, Hub, CityRun, RunResults.

## Ownership (Stage 2)

| Area | Owner |
|------|--------|
| Foundation / meta / UI / save | This pass (`Assets/Game/Scripts/Runtime/Core|Save|UI|Progression|Runs`) |
| Player / combat | Other agent — expected prefab: `Assets/Game/Prefabs/Characters/Player.prefab` |
| World / enemies / real CityRun | Other agent — suppress stub UI via `ICityRunReady` |

## Namespaces

`RealmShards.Core`, `RealmShards.Save`, `RealmShards.UI`, `RealmShards.Progression`, `RealmShards.Runs`

## Docs

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [SAVE_SYSTEM.md](SAVE_SYSTEM.md)
- [KNOWN_LIMITATIONS.md](KNOWN_LIMITATIONS.md)
