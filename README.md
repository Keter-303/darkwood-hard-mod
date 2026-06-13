# Darkwood Mod

Base mod workspace for Darkwood.

## Layout
- `src/` - source code for the mod
- `src/Patches/` - Harmony patches and game hooks
- `assets/` - optional mod assets
- `build/` - compiled output
- `lib/` - external modding DLLs such as BepInEx and Harmony
- `install/BepInEx/plugins/` - drop-in location for the built DLL
- `docs/` - notes, research, and reverse-engineering findings

## Target stack
- Unity Mono game
- BepInEx plugin
- Harmony patches

## Why the references matter
Darkwood is a Unity Mono game, so the mod needs compile-time references to the game's managed DLLs in `Darkwood_Data\Managed`. Those references let the project understand types like `Player`, `Inventory`, and `WorldGenerator` before we write patches against them.

BepInEx and Harmony are also required because they provide the mod entry point and patching API. Without them, the compiler would not know what `BaseUnityPlugin`, `BepInPlugin`, or `Harmony` are, and the mod could not load into the game cleanly.

## Next steps
1. Add a C# project file.
2. Add the BepInEx entry plugin class.
3. Add the first gameplay patch once we pick a target system.

## Handoff notes
- `docs/DarkwoodMod-handoff.md` records current reverse-engineering findings and known-good hook points.
