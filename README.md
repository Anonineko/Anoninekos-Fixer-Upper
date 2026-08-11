# Anonineko's Fixer Upper

> **Settings UI:** Pause → **Settings** shows a simple IMGUI panel on the right (not styled like the base game). You can also edit `BepInEx/config/com.anonineko.boobsrunnerfixes.cfg` or use ConfigurationManager.

BepInEx plugin for **[Boobs Runner](https://justcallmeneko001.itch.io/boobs-runner)** (Windows, Unity **6000.x Mono**).

Fixes broken photo-drone interaction and adds optional quality-of-life / spawn options (in-game Settings overlay + BepInEx config).

> **Content note:** the base game is NSFW. This mod only changes gameplay systems (interact, spawns, ledge drop); it does not add assets.

**License:** [MIT](./LICENSE) (this mod’s source only — the game remains property of its developer).

## Screenshots

<!-- Upload images to docs/screenshots/ on GitHub (or replace the paths below). -->

![Screenshot 1](docs/screenshots/01.png)

![Screenshot 2](docs/screenshots/02.png)

![Screenshot 3](docs/screenshots/03.png)

---

## Features

### Always-on fixes (can disable via config)

| Vanilla issue | What the mod does |
|---------------|-------------------|
| Recruit a photo drone → it follows you → **Interact does nothing** | Tracks the following drone so a second Interact still takes a photo even if the selector ring lost the target |
| Following drone steals Interact from a bench | **Selector target wins** when it is a bench (or any non-drone). Following drone is only used when nothing better is selected |
| Any nearby collider exit clears the selector | Selector only clears when the **currently selected** object exits |
| Pressing Interact in a bad pose can leave photo UI stuck | Failed photos no longer leave `outPhot` enabled; After Phot also hides it |
| Gallery detail view is **9:16** while photos are **square** | Detail `Image` uses preserve-aspect + square rect |
| Gallery grid **cannot scroll** to the last rows | Scroll content height is expanded after rearrange |

### Optional settings (pause menu → **Settings**)

| In-game label | Effect |
|---------------|--------|
| **No bench (full HP)** | Remove benches from newly spawned buildings while HP is full |
| **Bench++ (low HP)** | When low HP, keep benches at a high chance (default 100%) |
| **Force photo drone** | If you have no following photo drone, drone spawners always spawn one |
| **No drone if have one** | While a photo drone is following, skip new photo-drone spawns |
| **Down drops ledge** | Down / Slide releases ledge hang and ledge grab |
| **Show stage counter HUD** | Top-left `Stage: x/total` (stage = kills/10, starts at 0) |
| **Show gallery CG counter** | Top of gallery: `CGs: unlocked/total` (36 max: 4 poses × stages 0–8) |

During a run, the HUD shows **Stage: x/total** in the top-left (e.g. `Stage: 0/8`). Total auto-detects stage sprites when possible (`HUD` section in config).

Open **Gallery** to see **CGs: x/36** (unlocked photo CGs / total possible).

The same options (plus fine-tuning) live in:

```text
BepInEx/config/com.anonineko.boobsrunnerfixes.cfg
```

| Config key | Default | Notes |
|------------|---------|--------|
| `FixDroneInteract` | `true` | Master switch for drone/selector fixes |
| `NoBenchWhenFullHp` | `false` | |
| `BenchHighWhenLowHp` | `false` | |
| `ForceDroneWhenNone` | `false` | |
| `NoDroneWhenHaveOne` | `false` | |
| `DownDropsLedge` | `false` | |
| `LowHpRatio` | `0.5` | `lives / maxLives` at or below = “low HP” |
| `LowHpBenchChance` | `100` | % chance to keep each bench when low-HP boost is on |
| `NormalBenchChance` | `100` | % chance to keep each bench otherwise (when bench options apply) |

In-game toggles and the config file stay in sync when you flip options in Settings.

---

## Requirements

| Piece | Detail |
|-------|--------|
| Game | **[Boobs Runner](https://justcallmeneko001.itch.io/boobs-runner)** Windows build (tested on **V0.2.0qp1**) |
| Engine | Unity **6000.4** Mono (not IL2CPP) |
| Loader | **BepInEx 6 Unity Mono** bleeding edge (**be.785+** recommended) |

### Important: BepInEx version

This game **does not boot** with:

- BepInEx **5.x**
- BepInEx **6.0.0-pre.2** (and older pre builds)

Use a **Bleeding Edge** Unity Mono x64 pack, e.g. **6.0.0-be.785** or newer:

- Builds: https://builds.bepinex.dev/projects/bepinex_be  
- File name pattern: `BepInEx-Unity.Mono-win-x64-6.0.0-be.*+*.zip`

---

## Install (players)

1. Install **[Boobs Runner](https://justcallmeneko001.itch.io/boobs-runner)** (Windows) from itch.
2. Download **BepInEx Unity Mono win-x64** bleeding edge (see above).
3. Extract the zip so these sit **next to** `Boobs Runner.exe`:
   - `winhttp.dll`
   - `doorstop_config.ini`
   - `BepInEx\` folder
4. Download this mod’s `BoobsRunnerMod.dll` from [Releases](../../releases) (or [build it yourself](#build-from-source)).
5. Place the DLL here:

   ```text
   <game folder>/BepInEx/plugins/BoobsRunnerMod.dll
   ```

6. Launch the game once.
7. Confirm load in `BepInEx/LogOutput.log`:

   ```text
   Loading [Anonineko's Fixer Upper 1.0.0]
   Injected mod toggles into Settings.
   ```

8. Open **pause → Settings** for the mod toggles (under **Anonineko's Fixer Upper**).

To remove the mod, delete the DLL. To disable all BepInEx injection, set `enabled = false` in `doorstop_config.ini`.

---

## How photo drones work (with fixes)

Vanilla controls for this auto-runner:

| Action | Keyboard |
|--------|----------|
| Jump | `W` / `↑` / `Space` / `Enter` |
| Down / slide / fast-fall | `S` / `↓` |
| Attack | `D` / `→` |
| **Interact** | **`A` / `←`** |

### Photo steps

1. Find a **photo drone** (tag `Enemy Drone`). **Do not attack it** — a hit kills it and disables interact.
2. Get close and press **Interact** once → drone unparents and **follows** you.
3. Get into a valid pose, then press **Interact** again:
   - **Running** on ground  
   - **Sliding**  
   - **Right-wall hang** (the “ledge” gallery set)  
   - **Game over** (also auto-attempts if you die at 0 HP while a drone is still selected)
4. Time freezes; press any key to send the shot to the gallery (pause → Gallery).

Gallery variants scale with combat stage (`kills / 10`, photo index ≈ stage / 2, clamped 0–8).

### Interact priority (mod)

When you press Interact:

1. If the selector is on a **bench** (or other non-drone interactable) → use that.  
2. Else if the selector is on a **drone** → recruit / photo that drone.  
3. Else if you already have a **following** drone → photo that drone.  
4. Else → nothing.

---

## Troubleshooting

| Symptom | What to try |
|---------|-------------|
| Game crashes instantly after install | Wrong BepInEx pack. Use **Unity Mono win-x64 bleeding edge (be.785+)**, not 5.x / pre.2. |
| No `LogOutput.log` / no plugins | Doorstop not active: confirm `winhttp.dll` + `doorstop_config.ini` next to the exe; `enabled = true`. |
| Plugin missing from log | DLL not under `BepInEx/plugins/`, or wrong architecture. |
| No toggles in Settings | Check log for inject errors. Options still work via `com.anonineko.boobsrunnerfixes.cfg`. |
| Interact still does nothing | Confirm `FixDroneInteract = true`. You must **recruit once** first; photos only resolve in valid poses. |
| Settings layout looks odd | Toggles are cloned from the game’s UI; layout may vary by resolution. Function is what matters; open an issue with a screenshot if needed. |

---

## Build from source

### Prerequisites

| Tool | Why |
|------|-----|
| [.NET SDK](https://dotnet.microsoft.com/download) 6+ (or newer) | Builds the `netstandard2.1` class library (`dotnet build`) |
| [Boobs Runner](https://justcallmeneko001.itch.io/boobs-runner) (Windows) | Provides `Assembly-CSharp.dll` + Unity modules to reference |
| BepInEx 6 Unity Mono **be.785+** installed on that game copy | Provides `BepInEx.*.dll` and `0Harmony.dll` |

Optional: any editor that can open an SDK-style `.csproj` (VS, Rider, VS Code + C# extension).

### 1. Clone the repo

```powershell
git clone <your-repo-url> AnoninekosFixerUpper
cd AnoninekosFixerUpper
```

### 2. Install BepInEx on the game (if you have not already)

1. Download `BepInEx-Unity.Mono-win-x64-6.0.0-be.785+….zip` from  
   https://builds.bepinex.dev/projects/bepinex_be  
2. Extract **next to** `Boobs Runner.exe` (same folder as the executable).

### 3. Populate `libs/` (reference assemblies)

The project does **not** ship game or BepInEx binaries (see `.gitignore`). Create a `libs` folder in the repo root and copy:

**From** `<game>/BepInEx/core/`:

```text
0Harmony.dll
BepInEx.Core.dll
BepInEx.Unity.Common.dll
BepInEx.Unity.Mono.dll
```

**From** `<game>/Boobs Runner_Data/Managed/`:

```text
Assembly-CSharp.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
UnityEngine.IMGUIModule.dll
UnityEngine.Physics2DModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.AnimationModule.dll
Unity.InputSystem.dll
Unity.TextMeshPro.dll
```

PowerShell example (adjust paths):

```powershell
$game = "C:\Path\To\Boobs_Runner_Windows-V0.2.0qp1"
$libs = ".\libs"
New-Item -ItemType Directory -Force -Path $libs | Out-Null

$core = Join-Path $game "BepInEx\core"
$managed = Join-Path $game "Boobs Runner_Data\Managed"

@(
  "0Harmony.dll",
  "BepInEx.Core.dll",
  "BepInEx.Unity.Common.dll",
  "BepInEx.Unity.Mono.dll"
) | ForEach-Object { Copy-Item (Join-Path $core $_) $libs -Force }

@(
  "Assembly-CSharp.dll",
  "UnityEngine.dll",
  "UnityEngine.CoreModule.dll",
  "UnityEngine.UI.dll",
  "UnityEngine.UIModule.dll",
  "UnityEngine.IMGUIModule.dll",
  "UnityEngine.Physics2DModule.dll",
  "UnityEngine.TextRenderingModule.dll",
  "UnityEngine.AnimationModule.dll",
  "Unity.InputSystem.dll",
  "Unity.TextMeshPro.dll"
) | ForEach-Object { Copy-Item (Join-Path $managed $_) $libs -Force }
```

### 4. Build

From the repo root (where `BoobsRunnerMod.csproj` is):

```powershell
dotnet restore
dotnet build -c Release
```

Output:

```text
bin\BoobsRunnerMod.dll
```

### 5. Deploy to the game

```powershell
$game = "C:\Path\To\Boobs_Runner_Windows-V0.2.0qp1"
New-Item -ItemType Directory -Force -Path "$game\BepInEx\plugins" | Out-Null
Copy-Item .\bin\BoobsRunnerMod.dll "$game\BepInEx\plugins\" -Force
```

### 6. Verify

1. Start **Boobs Runner**.  
2. Open `BepInEx\LogOutput.log` and look for:

   ```text
   Loading [Anonineko's Fixer Upper 1.0.0]
   Anonineko's Fixer Upper v1.0.0 loaded.
   Injected mod toggles into Settings.
   ```

### Build notes

- **Target framework:** `netstandard2.1` (see `BoobsRunnerMod.csproj`).
- **Entry type:** `BoobsRunnerMod.Plugin` — BepInEx name **Anonineko's Fixer Upper**, GUID `com.anonineko.boobsrunnerfixes`.
- **Patching:** Harmony only; no game files are overwritten.
- Do **not** commit `libs/`, `bin/`, or `obj/` (ignored by `.gitignore`).

### Project layout

```text
src/
  Plugin.cs              # BepInEx entry, config bindings
  DroneState.cs          # Following-drone tracking + interact priority
  PlayerAccess.cs        # Reflection helpers for private player fields
  SettingsUi.cs          # Injects toggles into SettingsInit
  Patches/
    InteractPatches.cs   # Interact / TakePhot / selector exit / death cleanup
    SpawnPatches.cs      # EnemySpawner + building bench filtering
    LedgePatches.cs      # Down drops ledge
BoobsRunnerMod.csproj
LICENSE                  # MIT
README.md
.gitignore               # GitHub VisualStudio (C#/.NET) template + project extras
```

---

## Compatibility

- **Tested:** Windows x64, game **V0.2.0qp1**, Unity **6000.4.11f1**, BepInEx **6.0.0-be.785**
- **Not tested:** other OS packages, older game builds, IL2CPP (this build is Mono)
- May break if the developer renames types/methods (`PlayerScript`, `Interactable`, `EnemySpawner`, etc.) or reworks Settings UI

---

## Privacy / safety

- Writes **PlayerPrefs** keys prefixed with `BRMod_` for in-game toggles  
- Writes BepInEx config under `BepInEx/config/`  
- Does not network; photo unlocks only happen when you take photos through normal game flow  

---

## License

This repository’s code is released under the [MIT License](./LICENSE).

**Boobs Runner** and all of its assets remain the property of their developer. This is an unofficial fan project and is not affiliated with or endorsed by the game’s author. You are responsible for complying with the game’s license and itch.io terms.

---

## Credits

- Game: **[Boobs Runner](https://justcallmeneko001.itch.io/boobs-runner)** by JustCallMeNeko001  
- Mod: **Anonineko's Fixer Upper**  
- Loader: [BepInEx](https://github.com/BepInEx/BepInEx)  
- Patching: [Harmony](https://github.com/pardeike/Harmony)
