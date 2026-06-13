# Darkwood Mod Handoff

Date: 2026-06-13

## Current State

- The mod is a BepInEx 5 + Harmony plugin for Darkwood.
- Darkwood is running as x64 on this machine, so the working loader is BepInEx x64.
- The plugin loads successfully and writes a simple debug log to `%TEMP%\DarkwoodMod-debug.log`.
- The built plugin is `build/DarkwoodMod.dll`.
- The game plugin path is `C:\Program Files (x86)\Steam\steamapps\common\Darkwood\BepInEx\plugins\DarkwoodMod.dll`.

## Build Notes

- `dotnet build` does not work in the current environment because the .NET SDK is not installed, even though `dotnet.exe` exists.
- The working compiler is Roslyn from Visual Studio Build Tools:
  `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe`
- `DarkwoodMod.csproj` points BepInEx/Harmony references at `lib\bepinex_x64\BepInEx\core`.
- Game references are loaded from:
  `C:\Program Files (x86)\Steam\steamapps\common\Darkwood\Darkwood_Data\Managed`

## Important Game Types

- `Player` derives from `CharBase`.
- `Character` derives from `CharBase`.
- `Dog` derives from `Character`.
- `CharacterEffectType` includes `poison`, `bleeding`, `damageContinuous`, `gas`, and many buff/debuff types.
- `CharBase` has HP and effect fields such as `Health`, `maxHealth`, `effects`, `poisoned`, `poisonValue`, `poisonInterval`, `poisonResistance`, and `poisonImmunity`.
- `CharBase.effects` is public and is a `CharacterEffects` instance.

## Effects and Poison

Prefer the game's normal effect system over calling low-level poison methods directly.

Useful APIs:

- `CharacterEffects.activate(CharacterEffectType type, float duration, float modifier)`
- `CharacterEffects.activate(CharacterEffectType type, float duration, float modifier, float interval, float timeElapsed)`
- `CharacterEffects.hasEffectType(CharacterEffectType type)`
- `CharBase.hasEffect(CharacterEffectType type)`
- `CharBase.startPoison(float strength, float interval)`
- `CharBase.stopPoison()`

Important finding:

- `CharBase.startPoison(strength, interval)` does not accept a duration. The second argument is the tick interval, not "how long poison lasts".
- Direct `startPoison(...)` can make HP tick down, but it does not reliably produce the normal timed effect/icon behavior.
- For a normal visible poison effect, use:

```csharp
player.effects.activate(
    CharacterEffectType.poison,
    durationSeconds,
    tickDamage,
    tickIntervalSeconds,
    0f);
```

## Dog Poison Patch Findings

The working dog poison implementation is in `src/Patches/DogPoisonAttackPatch.cs`.

Goal:

- When a dog's melee hit actually connects with the player, apply a timed poison effect.
- Do not apply poison just because the dog has aggro or is looking at the player.

False starts that should not be repeated:

- `Character.attackCharacter(Transform)` is not a real bite/hit event. It sets target/AI path/behavior. Hooking this applies poison when the dog notices or focuses the player.
- `Character.attackPlayer()` was not a reliable hit point for the dog attack flow.
- `Player.getHit(float, Transform, ...)` did not reliably identify dog hits in testing; in one iteration it produced no dog poison logs.
- Direct `player.startPoison(0.2f, 10f)` is wrong for a timed poison effect because `10f` is an interval, not duration.

Actual attack flow found by IL inspection:

- `Character.melee(Character.SensorType)` spawns a `MeleeSensor`.
- It sets:
  - `MeleeSensor.attackerTransform = Character._transform`
  - `MeleeSensor.damage = SensorType.damage`
  - `MeleeSensor.barricadeDamage = SensorType.barricadeDamage`
- The real contact point is `MeleeSensor.OnTriggerEnter(Collider)`.

Current correct hook:

- Patch `MeleeSensor.OnTriggerEnter(Collider)` with a Harmony postfix.
- Check `__instance.attackerTransform` belongs to a `Dog`.
- Check `_collider.GetComponentInParent<Player>()` returns the player.
- Then apply `CharacterEffectType.poison` through `player.effects.activate(...)`.
- Current poison constants in `DogPoisonAttackPatch.cs`:
  - tick damage: `1f`
  - tick interval: `1f`
  - duration: `8f`
  - cooldown guard: `0.75f`
- The patch also currently contains a `Player.getHit(float, Transform, ...)` fallback hook. Testing showed the confirmed working path is `MeleeSensor.OnTriggerEnter`; if duplicate poison logs ever appear from `Player.getHit`, remove that fallback first.

Expected debug log on success:

```text
Dog poison applied via MeleeSensor.OnTriggerEnter attacker=... dog=...
```

## Useful Reflection/IL Notes

If `Assembly.GetTypes()` throws `ReflectionTypeLoadException`, use the partial type list from `ex.Types` and ignore nulls. One common loader exception seen here was for `Newtonsoft.Json`; it did not prevent useful inspection if handled.

The local `_inspect_charactereffect.ps1` script is useful as a starting point for dumping IL and finding calls/fields. It can be adapted for other methods like `Character.melee`, `MeleeSensor.OnTriggerEnter`, or `CharacterEffects.activate`.

## Existing Patches

- `src/DarkwoodMod.cs`
  - BepInEx plugin entry point.
  - Calls `Harmony.PatchAll()`.
  - Writes debug lines with `DarkwoodPlugin.AppendDebug(...)`.

- `src/Patches/CharacterSpawnerNightSpawnPatch.cs`
  - Patches `CharacterSpawner.spawnNightChar`.
  - Temporarily doubles `___spawnMax` during the call and restores it in postfix.

- `src/Patches/DogPoisonAttackPatch.cs`
  - Patches `MeleeSensor.OnTriggerEnter(Collider)`.
  - Applies dog poison as a normal `CharacterEffectType.poison`.

## Quick Test Checklist

1. Build `build\DarkwoodMod.dll`.
2. Copy it to `C:\Program Files (x86)\Steam\steamapps\common\Darkwood\BepInEx\plugins\DarkwoodMod.dll`.
3. Fully restart Darkwood.
4. Confirm `%TEMP%\DarkwoodMod-debug.log` contains `Darkwood Mod loaded`.
5. Let a dog bite the player.
6. Confirm the log contains `Dog poison applied via MeleeSensor.OnTriggerEnter`.
7. Confirm the poison icon appears and HP ticks down for the configured duration only.
