# Darkwood Mod Handoff

Date: 2026-06-13

## Current State

- The mod is a BepInEx 5 + Harmony plugin for Darkwood.
- Darkwood is running as x64 on this machine, so the working loader is BepInEx x64.
- The plugin loads successfully and writes a simple debug log to `%TEMP%\DarkwoodMod-debug.log`.
- Verbose patch logs are disabled by default. Set `DarkwoodPlugin.EnableVerboseDebug = true` in `src/DarkwoodMod.cs` to re-enable per-event diagnostics.
- Dev hotkeys are currently enabled. Set `DarkwoodPlugin.EnableDevHotkeys = false` before a public release.
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
  - mutated dog poison tick damage: `1.35f`
  - mutated dog poison tick interval: `1f`
  - mutated dog poison duration: `10f`
- The patch also currently contains a `Player.getHit(float, Transform, ...)` fallback hook. Testing showed the confirmed working path is `MeleeSensor.OnTriggerEnter`; if duplicate poison logs ever appear from `Player.getHit`, remove that fallback first.

Expected debug log on success:

```text
Dog poison applied via MeleeSensor.OnTriggerEnter attacker=... dog=...
```

## Dog / Huge Dog AI Findings

There is no separate C# class named `HugeDog` in `Assembly-CSharp.dll`.

Known dog-related types:

- `Dog : Character`
- `CharacterType.Dog = 201`
- `CharacterType.DogMutated = 250`

The huge dog is probably represented by the same `Dog` component with different prefab data and/or `CharBase.type == CharacterType.DogMutated`.

Important finding:

- `Dog.Start()` only calls `Character.Start()`.
- Dog AI is mostly inherited from `Character` and configured through serialized prefab fields.
- For dog-specific patches, filter by `character is Dog` and then split normal/huge dogs with `character.type == CharacterType.Dog` or `CharacterType.DogMutated`.

Important `Character` AI fields:

- Vision / detection:
  - `fieldOfViewRange`
  - `farViewDistance`
  - `nearViewDistance`
  - `sleepViewDistance`
  - `enemyInSightCounterTarget`
  - `enemyInSightCounterFade`
  - `aniSightRangeModifier`
  - `charactersInViewRange`
  - `charactersInSight`
  - `canSeeEnemyFar`
  - `canSeeEnemyNear`
  - `enemyInSight`
- Movement / chase:
  - `AIpath`
  - `target`
  - `superTarget`
  - `lastKnownTargetPosition`
  - `distanceToTarget`
  - `currentSpeed`
  - `chaseSpeed`
  - `idleWalkSpeed`
  - `running`
  - `walking`
  - `sleeping`
- Aggression / behavior:
  - `aggressiveness`
  - `attackType`
  - `aggressive`
  - `onlyAttackPlayer`
  - `constantlyAttackPlayer`
  - `returnToSleepAfterLosingTarget`
  - `attackClosestCharacerAfterWakingUp`
  - `afraidOfGunshots`
  - `afraidOfHideout`
  - `afraidOfForestSpiritWard`
- Attacks:
  - `hasMeleeAttack`
  - `hasRangedAttack`
  - `wantToAttackDistance`
  - `wantToRangedAttackDistance`
  - `attacks`
  - `attackIntervals`
  - `sensorTypes`
  - `currentAttack`
  - `recoveringFromAttack`
  - `dontMoveWhenAttacking`
  - `dontRecoverFromAttack`

Important enums:

- `Character.Behaviour`:
  - `idle = 100`
  - `walking = 200`
  - `running = 300`
  - `defensive = 400`
  - `chasingTarget = 500`
  - `escaping = 600`
  - `listening = 700`
  - `following = 800`
- `Aggressiveness`:
  - `attackOnSight = 100`
  - `defensive = 200`
  - `stalker = 201`
  - `neutral = 300`
  - `flee = 400`
  - `fleeAndDespawn = 500`
  - `follower = 600`
- `AttackType`:
  - `normal = 0`
  - `hitAndRun = 100`
- `Faction`:
  - `player = 0`
  - `mutant = 400`
  - `animalPassive = 500`
  - `animalAggressive = 600`

Useful hook points for future dog AI work:

- `Character.canSeeEnemy()`
  - Handles perception.
  - Uses `charactersInViewRange`, `charactersInSight`, FOV/range fields, raycasts, faction checks, wake-up logic, and target assignment.
  - Good for making dogs detect the player differently.
- `Character.setBehaviour(Character.Behaviour targetBehaviour, bool force)`
  - Central state transition method.
  - `chasingTarget` makes the character run, growl, set target on `AIpath`, and add itself to the player's attackers.
  - Good for changing what dogs do after detecting/losing the player.
- `Character.canAttackTarget()`
  - Checks target distance, attack distance, FOV angle, and line-of-sight raycast before attack.
  - Good for modifying "can bite now" rules.
- `Character.chooseAttack(int id)`
  - Chooses a valid attack from `attacks` by cooldown, min/max distance, and walkability.
  - Sets `currentAttack`, `attackAni`, attack interval timestamps, and `attacking`.
  - Good for changing attack selection and cadence.
- `Character.doAttack(string sensorType)`
  - Dispatches to ranged attack or melee.
- `Character.melee(Character.SensorType)`
  - Spawns `MeleeSensor` and writes `attackerTransform`, `damage`, and `barricadeDamage`.
  - Good for modifying melee sensor properties at spawn time.
  - Dog accuracy issue: if the player circles around the dog, the bite sensor can spawn in the old facing direction and miss. The current mod patches `Character.melee` and, for `Dog`, snaps the dog transform's yaw toward `target` just before the sensor is spawned.
- `MeleeSensor.OnTriggerEnter(Collider)`
  - Real melee contact point.
  - Best hook for effects that should happen only when a bite actually connects.

Useful attack data classes:

- `Character.Attack`:
  - `name`
  - `disabled`
  - `minimumDifficulty`
  - `minAttackDistance`
  - `maxAttackDistance`
  - `recoverTime`
  - `intervalType`
  - `attackInterval`
  - `dontMove`
  - `alignWithTarget`
  - `runAwayAfterAttacking`
  - `runAwayChance`
  - `groundAttackType`
  - `targetGroundAttackType`
- `Character.SensorType`:
  - `name`
  - `isRanged`
  - `damage`
  - `barricadeDamage`
  - `throwStrength`
  - `targetDistanceModifier`
  - `strengthIsRelativeToTargetDist`
  - `sensor`

Useful pathfinding fields:

- `Character.AIpath` is an `AIPath`.
- `AIPath` has useful public fields:
  - `target`
  - `canSearch`
  - `canMove`
  - `speed`
  - `turningSpeed`
  - `slowdownDistance`
  - `pickNextWaypointDist`
  - `endReachedDistance`
  - `maximumRotateSpeed`

Current dog aggression patch:

- File: `src/Patches/DogAggressionPatch.cs`
- Hook: `Dog.Start()` postfix.
- Applies to `Dog` and treats `CharacterType.DogMutated` as the stronger huge/mutated dog variant.
- Mutated dogs may appear with `type=0` in logs before being normalized, so the patch detects them by either `type == CharacterType.DogMutated` or prefab/name containing `DogMutated` / `Mutated`.
- Makes dogs react and attack faster by changing runtime prefab fields:
  - sets `aggressiveness = Aggressiveness.attackOnSight`
  - sets `attackType = AttackType.normal`
  - sets `aggressive = true`
  - sets `onlyAttackPlayer = true`
  - sets `attackClosestCharacerAfterWakingUp = true`
  - lowers `enemyInSightCounterTarget`
  - raises `enemyInSightCounterFade`
  - raises `farViewDistance`, `nearViewDistance`, and `sleepViewDistance`
  - raises `chaseSpeed`
  - raises `AIPath.speed`, `AIPath.turningSpeed`, and `AIPath.maximumRotateSpeed`
  - lowers `Character.Attack.attackInterval`
  - lowers `Character.Attack.recoverTime`
  - slightly expands `Character.Attack.maxAttackDistance`
  - doubles dog rotation speed by changing `AIPath.turningSpeed`, `AIPath.maximumRotateSpeed`, and private `Character` fields through Harmony `AccessTools`:
    - `turningSpeed`
    - `maximumRotateSpeed`
    - `minimumTimeToReachRotation` is halved
  - patches `Character.melee(...)` to face the current target immediately before spawning a dog bite sensor
  - patches `MeleeSensor.FixedUpdate()` to make dog bite sensors slightly larger and longer-lived:
    - normal dog sensor scale: `1.35x`
    - mutated dog sensor scale: `2.1x`
    - sensor longevity: `1.25x`
    - mutated sensor longevity: `1.8x`
    - mutated bite damage is not modified; only the poison effect is stronger
  - mutated dog movement was adjusted to avoid stop-go jitter:
    - chase speed multiplier is `1.45x`, not extreme
    - rotation multiplier is `2.5x`
    - `AIPath.pickNextWaypointDist` is at least `6f`
    - `AIPath.endReachedDistance` is at least `4f`
    - `AIPath.repathRate` is at most `0.25f`
    - stamina is kept full in a `Character.Update` postfix for mutated dogs
- Expected debug log:

```text
Dog aggression tuned name=... type=Dog chaseSpeed=... farView=... nearView=...
```

## Useful Reflection/IL Notes

If `Assembly.GetTypes()` throws `ReflectionTypeLoadException`, use the partial type list from `ex.Types` and ignore nulls. One common loader exception seen here was for `Newtonsoft.Json`; it did not prevent useful inspection if handled.

The local `_inspect_charactereffect.ps1` script is useful as a starting point for dumping IL and finding calls/fields. It can be adapted for other methods like `Character.melee`, `MeleeSensor.OnTriggerEnter`, or `CharacterEffects.activate`.

## Existing Patches

- `src/DarkwoodMod.cs`
  - BepInEx plugin entry point.
  - Calls `Harmony.PatchAll()`.
  - Writes debug lines with `DarkwoodPlugin.AppendDebug(...)`.
  - `AppendVerboseDebug(...)` writes only when `EnableVerboseDebug` is `true`.
  - `F8` dev hotkey spawns a test `Centipede` near the player and writes a non-verbose log result.
  - `spawnCharacterAround(...)` marks spawned characters as temporary. The F8 test explicitly sets the centipede to `type = CharacterType.Centipede`, `nocturnal = false`, `temporarySpawned = false`, `spawnedManually = true`, `alwaysActive = true`, `relentlessPursuit = true`, and then calls `attackPlayer()` so it persists long enough for testing.

- `src/Patches/CharacterSpawnerNightSpawnPatch.cs`
  - Patches `CharacterSpawner.spawnNightChar`.
  - Temporarily doubles `___spawnMax` during the call and restores it in postfix.

- `src/Patches/DogPoisonAttackPatch.cs`
  - Patches `MeleeSensor.OnTriggerEnter(Collider)`.
  - Applies dog poison as a normal `CharacterEffectType.poison`.

- `src/Patches/CentipedeEverywhereSpawnPatch.cs`
  - Patches `CharacterSpawnPoint.actuallySpawn()`.
  - After the normal spawn succeeds, has a `35%` chance to spawn an extra `Centipede` near the spawned character.
  - Patches `CharacterSpawner.spawnCharacterAround(...)`.
  - After the normal runtime spawn succeeds, has a `25%` chance to spawn an extra `Centipede` using the same destination/flags.
  - Patches `CharacterSpawner.spawnNightChar()`.
  - Has a `40%` chance, with a `45s` cooldown, to spawn an extra nocturnal centipede around the player.
  - This makes centipedes possible from normal character spawn points, runtime spawns, and night spawns without replacing the original mob and without permanently rewriting location/prefab data.
  - Expected debug log when it happens:

```text
Centipede spawned from ... spawn point at location=...
```

## Quick Test Checklist

1. Build `build\DarkwoodMod.dll`.
2. Copy it to `C:\Program Files (x86)\Steam\steamapps\common\Darkwood\BepInEx\plugins\DarkwoodMod.dll`.
3. Fully restart Darkwood.
4. Confirm `%TEMP%\DarkwoodMod-debug.log` contains `Darkwood Mod loaded`.
5. Let a dog bite the player.
6. Confirm the log contains `Dog poison applied via MeleeSensor.OnTriggerEnter`.
7. Confirm the poison icon appears and HP ticks down for the configured duration only.
