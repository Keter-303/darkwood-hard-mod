# Darkwood Hard Mod

Gameplay mod for Darkwood built with BepInEx 5 and Harmony.

The goal is to make the world more dangerous without replacing the core feel of the game: dogs are more threatening, mutated dogs are faster and nastier, and centipedes can appear in more places.

## Features

### Dogs

- Regular dogs react faster and attack more reliably.
- Dog bite hitboxes are slightly more forgiving, so dogs miss less often when the player circles around them.
- Dogs turn faster toward their target before biting.
- Dog bites apply poison:
  - `1` damage per tick
  - `1s` tick interval
  - `8s` duration

### Mutated Dogs

- Mutated dogs are treated as a stronger dog variant even when the game reports their type incorrectly.
- They detect the player faster and from farther away.
- They rotate faster and recover from attacks faster.
- Their movement was tuned to avoid stop-go jitter.
- Mutated dog bite damage is not directly increased.
- Mutated dog poison is stronger:
  - `1.35` damage per tick
  - `1s` tick interval
  - `10s` duration

### Centipedes

- Centipedes can appear alongside normal enemy spawns across biomes and locations.
- They are added as extra spawns, not replacements, so original enemies still appear.
- Spawn hooks:
  - regular character spawn points
  - runtime spawns around the player
  - night waves near the hideout

### Night Spawns

- Night character spawn capacity is doubled.

## Installation

1. Install BepInEx 5 x64 into your Darkwood folder.

   Darkwood is expected at:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Darkwood
   ```

2. Make sure the BepInEx folder exists:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Darkwood\BepInEx
   ```

3. Copy the built mod DLL:

   ```text
   build\DarkwoodMod.dll
   ```

   into:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Darkwood\BepInEx\plugins\DarkwoodMod.dll
   ```

4. Start Darkwood.

5. Check the debug log if needed:

   ```text
   %TEMP%\DarkwoodMod-debug.log
   ```

   A successful load writes:

   ```text
   Darkwood Mod loaded
   ```

## Developer Notes

- Current dev hotkey: `F8`
  - Spawns a test centipede near the player.
  - This is useful for verifying that `Characters/Centipede` can spawn.
  - Disable before public release by setting `EnableDevHotkeys = false` in `src/DarkwoodMod.cs`.

- Verbose logs are disabled by default.
  - Enable them by setting `EnableVerboseDebug = true` in `src/DarkwoodMod.cs`.

## Building

This project targets:

- Unity Mono
- .NET Framework `net472`
- BepInEx 5
- Harmony

The project expects Darkwood managed assemblies at:

```text
C:\Program Files (x86)\Steam\steamapps\common\Darkwood\Darkwood_Data\Managed
```

The project references BepInEx/Harmony from:

```text
lib\bepinex_x64\BepInEx\core
```

If `dotnet build` is unavailable, build with Roslyn `csc.exe` from Visual Studio Build Tools.

## Repository Layout

- `src/` - plugin source code
- `src/Patches/` - Harmony patches
- `docs/` - reverse-engineering notes and handoff documentation
- `lib/README.md` - notes for local dependency placement
- `build/` - local compiled output, ignored by git
- `install/` - local staging folder, ignored by git

## Research Notes

See:

```text
docs/DarkwoodMod-handoff.md
```

It records known hook points, game types, effect behavior, dog AI findings, and spawn pipeline notes.
