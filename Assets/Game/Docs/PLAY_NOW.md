# RealmShards — Play Now (Stage 2)

Exact path to a playable Hub → CityRun fight.

## Prerequisites

- Unity **6000.5.6f1** (see `ProjectSettings/ProjectVersion.txt`)
- Open this project folder in the Unity Hub / Editor

## Steps

1. Open the project and **wait until scripts finish compiling** (bottom-right progress / Console clear of compile errors).
2. In the menu bar run **both** setup commands (order does not matter; safe to re-run):
   - **RealmShards → Setup Player Content**  
     Creates `Assets/Game/Prefabs/Characters/Player.prefab`, abilities, projectiles, Magus anim set, input actions wiring.
   - **RealmShards → Setup CityRun Stage 2**  
     Creates enemy/encounter ScriptableObjects and wires them onto `CityRunBootstrap` in `CityRun.unity`.
3. Open **`Assets/Game/Scenes/Bootstrap.unity`**.
4. Press **Play**.

## Expected flow

1. **Bootstrap** loads save → switches to **Hub**
2. Hub: **Start** → lobby → **Start Run**
3. **CityRun**: arena + Magus player + warriors/archers/champion
4. Clear the room (or use HUD **End: Win / End: Fail**) → **RunResults** → back toward Hub meta loop

## Controls (after Setup Player Content)

| Input | Action |
|--------|--------|
| WASD / Left stick | Move |
| Mouse / Right stick | Aim |
| Basic ability / Ability 1–3 / Dash | As bound in `RealmShards.inputactions` |
| Space / J | Only on the blue **placeholder** player if Magus prefab was missing |

## Fallback without menus

If you skip the setup menus, CityRun still boots with a blue placeholder player (WASD + Space/J melee) and default enemy wave. Prefer running both menus for Magus abilities and authored encounter data.

## Scenes in Build Settings

Enabled in order: `Bootstrap` → `Hub` → `CityRun` → `RunResults`  
(`Assets/Game/Scenes/*.unity`, names match `RealmShards.Core.SceneNames`)
