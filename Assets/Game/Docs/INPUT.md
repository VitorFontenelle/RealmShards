# RealmShards — Input (Steam Deck + Desktop)

## Design goals

- **Gamepad-first** on Steam Deck (1280×800), still fully usable with mouse/keyboard on Windows.
- **Mixed local multiplayer**: Player 1 can be Keyboard+Mouse **or** a gamepad; P2–P4 join on other gamepads. Devices are exclusive (no shared pad).
- **Rebindable** gameplay buttons via Hub → Controls (overrides saved under `persistentDataPath/input_bindings.json`).

## UI scale

Runtime canvases use reference resolution **1280×800** with width/height match 0.5 and a small safe-area inset (`UiScaleConfig`). This keeps Hub / Controls / Arcane Core readable on Deck and desktop.

## Default bindings

### Keyboard & Mouse

| Action | Default |
|--------|---------|
| Move | WASD / Arrows |
| Aim | Mouse position |
| Basic Ability | LMB |
| Ability 1 | RMB / 1 |
| Ability 2 | 2 |
| Ability 3 | 3 |
| Dash / Blink | Space |
| Interact | E |
| Drop Item | G |
| Pause | Esc |
| Join (Hub) | Space / Enter |
| Confirm | Enter |
| Cancel | Esc |
| Locate Player | Tab |

### Gamepad (Steam Deck / Xbox layout)

| Action | Default |
|--------|---------|
| Move | Left Stick |
| Aim | Right Stick |
| Basic Ability | X (West) |
| Ability 1 | Y (North) |
| Ability 2 | B (East) |
| Ability 3 | RB |
| Dash / Blink | A (South) |
| Interact | LB |
| Drop Item | D-Pad Down |
| Pause | Menu / Start |
| Join (Hub) | A / Start |
| Confirm | A |
| Cancel | B |
| Locate Player | View / Select |

**Note:** Stick **Move** and **Aim** axes are not offered as a single rebind block in the Controls UI (composites / pointer). Button actions are rebindable for both Keyboard&Mouse and Gamepad schemes. Conflict replace: binding a control already used by another action clears the old binding.

## Hub join / leave

- **Join:** Space (KBM) or A (gamepad) on an unclaimed device.
- **Leave:** Backspace (KBM slot) or B (that pad).
- Keyboard join also claims the mouse for that player.
- Disconnect removes the player from the lobby.

## Player Settings (Deck-like)

- `Application.runInBackground = true` at boot (also `runInBackground: 1` in Project Settings) so alt-tab / Deck overlay does not freeze sim hard.
- Prefer borderless window / native 1280×800 when testing Deck resolution on desktop.

## Rebind persistence

`RealmShards.Input.BindingOverridesService` uses Input System `SaveBindingOverridesAsJson` / `LoadBindingOverridesFromJson` at:

`Application.persistentDataPath/input_bindings.json`

Reset Defaults clears overrides and deletes that file.
