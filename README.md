<img width="2560" height="1440" alt="EMP_preview" src="https://github.com/user-attachments/assets/312b317a-dbdf-4702-94ee-6b0dc5439287" />


# EMP Weapon Mod for Nuclear Option

An advanced electromagnetic pulse (EMP) weapon mod for the game **[Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)**.
Adds custom EMP missiles that disable electronics, scramble enemy missiles, and create spectacular visual effects.

## Features

- **New EMP missile variants** for `AShM2` and `AGM_heavy`  
  - Zero blast/pierce damage, purely energy‑based  
  - Automatically integrated into existing hardpoints and loadouts

- **EMP detonation mechanics**  
  - Expanding EMP wave disables:
    - Engines (turbine, turbofan, turbojet, ducted fan, rotor)
    - Radars and target detectors
    - Displays, MFDs, HUD screens
    - Power supplies
    - Ground vehicles & ships lose sensors and weapons
  - Scrambles hostile missiles:
    - Chance to self‑destruct
    - Chance to retarget a random unit (friendly‑fire configurable)
    - Otherwise flies blind until impact

- **Player UI disruption**  
  - When caught in the EMP, the HUD, map, tactical screen, and night vision are disabled
  - Automatically restored when the aircraft is respawned

- **Visual & audio effects**  
  - Expanding translucent EMP sphere
  - Animated arcs, lightning bolts, ground sparks, and shockwave decals
  - Static noise audio on affected units
  - ~~Water ring effect over ocean~~

- **Exclusion zone integration**  
  - Custom blue exclusion zone on the dynamic map for EMP warheads
  - Custom warning message in the chat

- **Debug tools**  
  - Decal monitor for tracking scene decal projectors
  - Optional diagnostic logging (`DebugLog` config setting)

## Configuration

All settings are adjustable via BepInEx config file (`BepInEx/config/com.wersal.empmod.cfg`):

| Setting | Default | Description |
|---------|---------|-------------|
| `Radius` | 900 | EMP effect radius (meters) |
| `DebugLog` | true | Enable detailed debug output |
| `ScrambleMissiles` | true | Enable missile scrambling |
| `ScrambleFriendlyFire` | true | Scrambled missiles can target friendly units |
| `ScrambleExplodeChance` | 0.5 | Chance a scrambled missile self‑destructs |
| `ScrambleRetargetChance` | 0.5 | Chance a scrambled missile retargets |

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) for Nuclear Option.
2. Download the latest release from the [Releases](https://github.com/wersal454/Nuclear-Option-EMP-mod/releases) page.
3. Extract the `plugins` folder into your `Nuclear Option/BepInEx/` directory.
   - The mod files should be inside `BepInEx/plugins/EMP mod NO/`.
4. Launch the game. EMP weapon variants will be available in the loadout editor.

## Mod Structure (for developers)

NuclearOptionEmpMod/
├── Plugin.cs – BepInEx plugin entry, config, UI restoration
├── DecalGlobalMonitor.cs – Debug monitor for decal projectors
├── Helpers.cs – Small utility MonoBehaviours
└── Patches/
├── EncyclopediaPatch.cs – Creates EMP weapon variants
├── HardpointSpawnPatch.cs – Ensures EMP mounts are active
├── WeaponManagerSpawnPatch.cs – Diagnostic logging for weapon spawns
├── MissileDetonatePatch.cs – Core EMP logic, effects, UI disable
├── ExclusionZonePatch.cs – Custom exclusion zone handling
├── MissileGuidancePatch.cs – Prevents scrambled missiles from seeking
└── UIPatch.cs – Additional UI suppression/restoration

## Building from Source

1. Clone the repository.
2. Reference the required game assemblies (from `Nuclear Option_Data/Managed`):
   - `Assembly-CSharp.dll`
   - `Mirage.dll`
   - `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, etc.
   - BepInEx libraries (`BepInEx.dll`, `0Harmony.dll`)
3. Compile with .NET Framework 4.8 or equivalent.
4. Place the output `.dll` and `.cs` files as shown in the installation section.

## License

[MIT](LICENSE) – feel free to fork, "steal" and modify.  
Credit to me (wersal) are appreciated, but not required.

## Known Issues / Limitations

- EMP‑disabling of some radar components may not persist after unit repair.
- Visual effects are client‑side; other players will not see the EMP sphere or arcs.
- The decal monitor is meant for debugging and can be safely ignored.

## Credits

- Thanks to the Nuclear Option modding community for reverse‑engineering guidance.
