# Content Pipeline (Stage 2)

## Rules

- **Never overwrite** `Assets/Characters/**` or `Assets/Tiles/**` source art.
- Generated / processed outputs go under `Assets/Game/Art/**/Generated` or `**/Processed`.
- Prefer Point filter, no mipmaps, uncompressed (or none) for pixel art.

## Menus

| Menu | Purpose |
|------|---------|
| **RealmShards → Art → Process Floor Seam Tile** | Copies `Assets/Tiles/sample-tile.png` → `Assets/Game/Art/Tiles/Generated/sample-tile_seamed.png` with edge padding. Source untouched. `ArenaBuilder` prefers the Generated tile when present. |
| **RealmShards → Art → Sprite Sheet Processor** | MVP window: near-black→alpha threshold, padded grid re-slice, Point/no-mips/no-compression import. Outputs under `Assets/Game/Art/Characters/Processed`. |

## Sprite Sheet Processor — limitations

- Threshold matte only (near-black → transparent). Already-transparent pixels kept.
- Padding is edge-clone, **not** ML content-aware fill.
- Grid re-slice assumes uniform cells (e.g. mage **8×6**, enemy sheets often **6×4**).
- Complex sheets may still need hand slicing in the Sprite Editor.
- Does not rewrite original importer settings on source assets.

## Floor / room kit

`ArenaBuilder` builds layered roots:

- `Floor` — visual tiles
- `Walls` — visual + collision
- `Collision` — exit blockers

## Data content builders

- **Setup Player Content** — player prefab / abilities wiring
- **Setup CityRun Stage 2** — enemies, encounters, decade-gated champion defs
- **Setup Items Content** / **Setup Magic Schools** — ScriptableObjects + Generated icons

Re-run builders after pull if assets are missing. `.meta` GUIDs are left for Unity to generate on import.
