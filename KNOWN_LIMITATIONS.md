# Known limitations (Stage 2)

- **Source sprite quality / hand-authored sheets** — processor is MVP (threshold + pad); final mage/enemy slices still need art pass.
- **Steamworks** — not integrated.
- **Final lore / naming** — decade champion variants use placeholder titles (Arcane / Gilded / Ashen).
- **Hub UI** is runtime placeholder uGUI (Legacy Text), not designed prefabs.
- **Local multiplayer** joins devices in Hub; shared ortho camera (no true split-screen).
- **Audio** — `AudioEventHub` stubs log/play hooks only; no shipped SFX bank yet.
- **ContentDatabase** uses an in-memory runtime catalog until a ScriptableObject asset is authored under `Assets/Game/Data/Progression/`.
- **Scenes** may be overwritten by `RealmShards/Setup CityRun Stage 2` — keep Build Settings order Bootstrap → Hub → CityRun → RunResults.
- **`.meta` for new scripts** left for Unity to generate on import.
- **JsonUtility** limitations apply (no Dictionaries; polymorphic types unsupported).
- **Projectile** layer (16) is a combat-compat alias alongside PlayerProjectile/EnemyProjectile.
- **Camera catch-up** soft-pulls outlier players — tune on `SharedOrthoCamera` if it feels aggressive in 3–4P.
