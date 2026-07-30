# Known limitations (Stage 2 foundation)

- **CityRunMetaBridge** auto-spawns world `CityRunBootstrap` and ends the run on first encounter clear — replace with proper multi-room route flow when ready.
- **Hub UI is runtime placeholder uGUI** (Legacy Text), not designed prefabs.
- **Local multiplayer slots** only set player count (1–4); no device pairing / split-screen yet.
- **Loadout** shows equipped ability IDs only — combat agent must bind them to `AbilityCaster`.
- **ContentDatabase** uses an in-memory runtime catalog until a ScriptableObject asset is authored under `Assets/Game/Data/Progression/`.
- **Ability combat definitions** live in `RealmShards.AbilityDefinition` (combat agent), not Progression.
- **City definitions** live in `RealmShards.Runs.CityDefinition`.
- **Scenes** may be overwritten by `RealmShards/Setup CityRun Stage 2` editor menu — keep Build Settings order Bootstrap → Hub → CityRun → RunResults.
- **`.meta` for scripts** left for Unity to generate on import.
- **JsonUtility** limitations apply (no Dictionaries; polymorphic types unsupported).
- **Projectile** layer (16) is a combat-compat alias alongside PlayerProjectile/EnemyProjectile.
