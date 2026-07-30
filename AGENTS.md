# AGENTS.md

## Cursor Cloud specific instructions

RealmShards is a **Unity 6 (`6000.5.6f1`) URP 2D local co-op roguelite** (see `README.md`,
`ARCHITECTURE.md`, `Assets/Game/Docs/PLAY_NOW.md`). There is no npm/pip/dotnet layer — the
Unity Editor drives everything (compile, tests, build, run).

### Environment baked into the VM snapshot

- Unity Editor `6000.5.6f1` is installed at `/opt/unity/Editor/Unity` (bundled
  `LinuxStandaloneSupport`, so Linux player builds work without extra modules).
- Linux system libraries the Editor/player need, plus `xvfb` + Mesa (software GL), are
  already installed via apt.
- A local, non-repo helper wraps the common flows: `~/realmshards-unity.sh`
  (`activate | status | setup | test | build | run`). Logs go to `~/unity-logs/`.

### Licensing (REQUIRED, not automatic)

The Editor will not compile, test, build, or run without an activated license — a bare launch
exits with code `198` and logs `No valid Unity Editor license found`. Activation needs a Unity
account (a free Personal account is enough). Provide ONE of these via secrets:

- `UNITY_EMAIL` + `UNITY_PASSWORD` (+ `UNITY_SERIAL` only for Plus/Pro seats), or
- `UNITY_LICENSE` = full contents of a `.ulf` file from manual activation
  (`https://license.unity3d.com/manual`; a `.alf` request file can be generated with
  `/opt/unity/Editor/Unity -batchmode -nographics -quit -createManualActivationFile`).

Then activate (`~/realmshards-unity.sh activate`, or run the raw command directly):

```bash
# email/password (Personal or Pro); add `-serial "$UNITY_SERIAL"` only for Plus/Pro
/opt/unity/Editor/Unity -batchmode -nographics -quit \
  -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -logFile ~/unity-logs/activate.log
# or, with a .ulf: echo "$UNITY_LICENSE" > ~/unity.ulf && \
#   /opt/unity/Editor/Unity -batchmode -nographics -quit -manualLicenseFile ~/unity.ulf -logFile ~/unity-logs/activate.log
```

Confirm with `~/realmshards-unity.sh status` (i.e.
`/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client --showEntitlements`);
it should list a Personal/Pro entitlement.

Note: an activated license persists in the VM snapshot, but Unity licenses are machine-bound, so
if a future VM reports a different machine id the persisted license can read as absent. Keep the
Unity secrets available and simply **re-run activation whenever `status` shows no license** — it
is idempotent. This is why activation is intentionally left out of the startup update script.

### Running things (all via batchmode; only the built player needs a display)

`~/realmshards-unity.sh` wraps these; the raw commands (in case the helper is missing) are:

```bash
U=/opt/unity/Editor/Unity; P=/workspace
# Content setup — run after a fresh pull / when generated assets are missing (CONTENT_PIPELINE.md)
$U -batchmode -nographics -quit -projectPath $P \
   -executeMethod RealmShards.Editor.RealmShardsFullSetup.RunAll -logFile ~/unity-logs/setup.log
# EditMode tests — fastest end-to-end check of core logic (world route, progression, save,
# room planner, champion selector). Results XML at ~/unity-logs/test-results.xml
$U -batchmode -nographics -runTests -projectPath $P \
   -testPlatform EditMode -testResults ~/unity-logs/test-results.xml -logFile ~/unity-logs/test.log
```

- `-batchmode -nographics` does NOT need Xvfb — it reaches the license/compile stage on its
  own. Only launching an actual **built standalone player** needs a display; run it under
  `xvfb-run -s "-screen 0 1280x800x24"` with Mesa software GL.
- The repo ships `RealmShards.Editor.RealmShardsFullSetup.BuildWindowsPlayer`
  (`StandaloneWindows64`), which cannot run natively on this Linux VM. To run the game here,
  build a `StandaloneLinux64` player instead (e.g. a throwaway `-executeMethod` build script);
  the `build`/`run` helper commands assume such a Linux build under `Builds/Linux/`.

### Gotchas

- Only one Unity Editor instance may hold the project (`Library/`) lock at a time — don't run
  `setup`/`test`/`build` concurrently against `/workspace`.
- `Library/`, `Temp/`, `Logs/`, and `Builds/` are gitignored and regenerated; the first
  Editor launch after a pull re-imports assets and can take several minutes.
- Editor menu items (`RealmShards → Setup ...`) are also exposed as batchmode entry points in
  `Assets/Game/Scripts/Editor/RealmShardsFullSetup.cs` — prefer those over the GUI menus.
