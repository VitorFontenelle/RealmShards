# RealmShards

**RealmShards** is a Unity 6 URP 2D local-co-op action roguelite for Windows desktop and Steam Deck. Assemble up to four local players, master distinct magic schools, clear procedural city rooms, defeat decade-gated champions, and spend the Arcane Cores they drop to expand future runs.

## Highlights

- **1–4 player local co-op** with keyboard/mouse and gamepads supported in the same lobby.
- **Procedural city runs** with multi-room encounters, escalating champions, and a capital finale.
- **Arcane progression**: champion rewards unlock spells and loadout choices between runs.
- **Readable combat UX**: per-player HUDs, cooldown indicators, damage numbers, pause controls, and player-location assistance.
- **Steam Deck-aware presentation** using a 1280×800 UI reference layout and gamepad-first controls.

## Download and install (Windows)

1. Download the current `RealmShards-Setup-*.exe` from this project's release assets.
2. Run the installer and accept the install location (or choose another folder).
3. Launch **RealmShards** from the Start menu or the optional desktop shortcut.
4. In the Hub, press **Space** to join with keyboard/mouse or **A / Start** on an unclaimed gamepad, then select **Start Run**.

Windows may show a SmartScreen prompt for an unsigned independent build. Use **More info → Run anyway** only when the installer was downloaded from a release you trust.

## Controls

| Action | Keyboard & mouse | Gamepad |
| --- | --- | --- |
| Move / aim | WASD or arrows / mouse | Left stick / right stick |
| Basic ability | Left mouse button | X (West) |
| Abilities 1–3 | Right mouse button or 1, 2, 3 | Y (North), B (East), RB |
| Dash / blink | Space | A (South) |
| Pause | Esc | Menu / Start |
| Join in Hub | Space / Enter | A / Start |

Gameplay button bindings can be changed from **Hub → Controls**. See the complete [input guide](Assets/Game/Docs/INPUT.md).

## Run locally in Unity

**Required:** Unity **6000.5.6f1**.

1. Open the project with Unity Hub or the matching Editor.
2. If generated content is missing after a fresh clone, run the setup menus in order:
   **RealmShards → Setup Player Content**, **CityRun Stage 2**, **Items Content**, then **Magic Schools**.
3. Open `Assets/Game/Scenes/Bootstrap.unity` and enter Play Mode.

For a single batch setup pass:

```powershell
& "D:\Unity\6000.5.6f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath $PWD `
  -executeMethod RealmShards.Editor.RealmShardsFullSetup.RunAll
```

## Build a Windows release

The project includes a repeatable Windows 64-bit build entry point:

```powershell
& "D:\Unity\6000.5.6f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath $PWD `
  -executeMethod RealmShards.Editor.RealmShardsFullSetup.BuildWindowsPlayer
```

The player is written to `Builds/Windows/RealmShards.exe`. To package it, install [Inno Setup 6](https://jrsoftware.org/isdl.php) and run:

```powershell
$iscc = @(
  "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
  "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
& $iscc Installer\RealmShards.iss
```

This creates `Builds/Installer/RealmShards-Setup-1.0.0.exe`. Build artifacts are intentionally excluded from source control.

## Project notes

- [Play guide](Assets/Game/Docs/PLAY_NOW.md)
- [Architecture](ARCHITECTURE.md)
- [Content pipeline](CONTENT_PIPELINE.md)
- [Save system](SAVE_SYSTEM.md)
- [Known limitations](KNOWN_LIMITATIONS.md)
