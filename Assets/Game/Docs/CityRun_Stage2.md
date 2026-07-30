# CityRun — Stage 2 playable combat room

## Play standalone

1. Open Unity (`6000.5.6f1`).
2. Wait for scripts to compile.
3. Optional but recommended: menu **RealmShards → Setup CityRun Stage 2** (creates enemy/encounter ScriptableObjects and wires references).
4. Also run **RealmShards → Setup Player Content** if `Assets/Game/Prefabs/Characters/Player.prefab` is missing.
5. Open `Assets/Game/Scenes/CityRun.unity`.
6. Press **Play**.

### Controls (placeholder / Magus player)

- **WASD / Arrows**: move  
- Player abilities come from the Characters agent prefab when present.

### From Hub

Hub loads scene name `CityRun` (`RealmShards.Core.SceneNames.CityRun`). Build Settings already include `Assets/Game/Scenes/CityRun.unity`.

## What spawns

`CityRunBootstrap` builds a 24×16 arena at runtime:

- Floor quads from `Assets/Tiles/sample-tile.png` (scaled; seams OK)
- Environment-layer walls + exit blockers (locked until clear)
- Player at south spawn (real prefab if found, else blue placeholder)
- Encounter: **2 Golden Axe Warriors**, **2 Golden Archers**, **1 Arcane Core Champion**
- Coop HP/count curves via `CoopScalingConfig`

## Enemy behaviour

| Enemy | Behaviour |
|--------|-----------|
| **Golden Axe Warrior** | Chase → telegraph (no damage) → **active hitbox window only** → cooldown. Uses knight sheet frames when available. |
| **Golden Archer** | Keep distance / strafe → aim telegraph → pooled projectile (procedural bullet, not painted arrow collider). |
| **Arcane Core Champion** | High-HP warrior variant; on death spawns **Arcane Core** stub trigger. |

Targeting: closest living player, retarget on interval / death / leave aggro (not every frame).

## Gaps / art workarounds

- Knight/archer sheets are messy auto-slices — walk/attack use index ranges; fallback tinted placeholders if frames fail.
- 4-dir facing via `flipX` (not true 8-dir).
- Floor tiling may show seams.
- Damage uses configured colliders (`EnemyHitbox` / pooled projectile), never sprite pixel shapes.
