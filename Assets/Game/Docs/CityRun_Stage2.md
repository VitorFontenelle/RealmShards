# CityRun — Stage 2 playable combat room

## Play standalone

1. Open Unity (`6000.5.6f1`).
2. Wait for scripts to compile.
3. Optional but recommended: menu **RealmShards → Setup CityRun Stage 2**.
4. Also run **RealmShards → Setup Player Content** if the player prefab is missing.
5. Open `Assets/Game/Scenes/CityRun.unity` or Play from **Bootstrap**.
6. Press **Play**.

## Multi-room flow

`CityRunDirector` chains **2–3 trash rooms** then a **champion room** (seed + node deterministic). Clearing room 1 does **not** advance the world route — only city completion (after champion / Arcane Core) does.

## What spawns

`CityRunBootstrap` builds a 24×16 arena at runtime:

- Floor from Generated seam tile if present, else `Assets/Tiles/sample-tile.png`
- Layered Walls + Collision exit blockers
- Players from Hub lobby / prefab
- Trash waves then decade-gated champion (`ChampionSelector`)

## Combat UX

- Combat HUD (HP / CDs / inventory)
- Pause (Esc / Start)
- Locate player + camera catch-up
- Damage numbers + hit-stop

## Enemy behaviour

| Enemy | Behaviour |
|--------|-----------|
| **Golden Axe Warrior** | Chase → telegraph → active hitbox → cooldown |
| **Golden Archer** | Keep distance → aim → pooled projectile |
| **Arcane Core Champion** | Cleave / slam / phase-2 burst; Arcane Core on death |

## Gaps / art

See `CONTENT_PIPELINE.md` and `KNOWN_LIMITATIONS.md`.
