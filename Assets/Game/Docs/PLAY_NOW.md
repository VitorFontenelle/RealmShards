# RealmShards — Play Now

Unity **6000.5.6f1** · URP 2D · local co-op (Deck + desktop).

## Setup menus (run once / after pull)

1. **RealmShards → Setup Player Content**
2. **RealmShards → Setup CityRun Stage 2**
3. **RealmShards → Setup Items Content**
4. **RealmShards → Setup Magic Schools**
5. Optional: **RealmShards → Art → Process Floor Seam Tile** / **Sprite Sheet Processor**

Then open `Assets/Game/Scenes/Bootstrap.unity` → Play.

## Loop

1. **Bootstrap** → load save → **Hub**
2. Hub lobby: **Space / A** join (KBM and/or pads), cycle loadout slots, set cities-before-capital, optional **Controls** rebind
3. **Start Run** → procedural city nodes → capital last
4. **CityRun** — clear **2–3 trash rooms**, then **champion** (telegraphed multi-attack, phase at ~50% HP)
5. Champion drops **Arcane Core** (spend Vestiges) → next city or finish
6. **RunResults** → Hub

First room clear no longer ends the whole run.

## In-run UX

| Feature | How |
|---------|-----|
| Combat HUD | Bottom-left HP / CD / items per player; room counter top-right |
| Pause | **Esc / P / Start** → Resume · Controls · Quit to Hub |
| Locate ally | **LocatePlayer** action → camera pulse + edge arrow |
| Damage numbers | Float on hits; brief hit-stop on heavy hits |

## Controls quick ref

See [INPUT.md](INPUT.md) for Deck vs desktop defaults and rebinding.

## Mixed 2P test

1. Hub: join Keyboard with Space (P1 purple)
2. Connect a gamepad → press A (P2 green)
3. Start Run — both should spawn with exclusive devices
4. Split far apart — catch-up pull / locate should help framing

## Colors

| Player | Robe recolor |
|--------|----------------|
| P1 | Purple |
| P2 | Green |
| P3 | Yellow |
| P4 | Red |

## Magic schools

| School | Sample spells |
|--------|----------------|
| Neutral Arcana | Bolt, Pulse, Blink |
| Gilded Ward | Gilded Flare (burn), Gilded Smite |
| Ashen Veil | Ashen Drift, Ashen Cinder (slow) |
| Tideglass | Tideglass Ripple (slow), Harpoon |
| Continuum | Continuum Slip, Continuum Echo (**self ward**) |

Unlock via Arcane Core UI after champions; equip in Hub loadout when unlocked.

## EditMode tests

Window → General → Test Runner → EditMode → run `RealmShards.EditModeTests`.
