using HarmonyLib;
using UnityEngine;

namespace DarkwoodMod.Patches;

[HarmonyPatch(typeof(CharacterSpawnPoint), "actuallySpawn")]
internal static class CentipedeEverywhereSpawnPatch
{
    private const float SpawnPointExtraCentipedeChance = 0.35f;
    private const float SpawnPointExtraCentipedeDistance = 350f;

    private static void Postfix(CharacterSpawnPoint __instance)
    {
        if (__instance == null || __instance.spawnedCharacter == null || CharacterSpawner.Instance == null)
        {
            return;
        }

        if (__instance.spawnedCharacter.type == CharacterType.Centipede)
        {
            return;
        }

        if (Random.value > SpawnPointExtraCentipedeChance)
        {
            return;
        }

        Character centipede = CharacterSpawner.Instance.spawnCharacterAround(
            __instance.spawnedCharacter.gameObject,
            Vector3.zero,
            SpawnPointExtraCentipedeDistance,
            CharacterType.Centipede.ToString(),
            false,
            false,
            false,
            true);

        if (centipede == null)
        {
            return;
        }

        ConfigureCentipede(centipede, nocturnal: false, attackPlayer: false);
        string locationName = __instance.location != null ? __instance.location.name : "unknown";
        DarkwoodPlugin.AppendVerboseDebug($"Extra Centipede spawned alongside {__instance.spawnedCharacter.type} at location={locationName}");
    }

    internal static void ConfigureCentipede(Character centipede, bool nocturnal, bool attackPlayer)
    {
        centipede.type = CharacterType.Centipede;
        centipede.nocturnal = nocturnal;
        centipede.wantToDespawn = false;
        centipede.temporarySpawned = nocturnal;
        centipede.spawnedManually = !nocturnal;
        centipede.alwaysActive = !nocturnal;
        centipede.relentlessPursuit = attackPlayer;
        centipede.isActive = true;

        if (attackPlayer)
        {
            centipede.attackPlayer();
        }
    }
}

[HarmonyPatch(typeof(CharacterSpawner), nameof(CharacterSpawner.spawnCharacterAround))]
internal static class CentipedeRuntimeSpawnPatch
{
    private const float RuntimeExtraCentipedeChance = 0.25f;

    private static bool spawningExtraCentipede;

    private static void Postfix(
        CharacterSpawner __instance,
        Character __result,
        GameObject destGO,
        Vector3 offset,
        float distance,
        string type,
        bool nocturnal,
        bool attackPlayer,
        bool relentlessPursuit,
        bool canSpawnInside)
    {
        if (spawningExtraCentipede || __instance == null || __result == null || destGO == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(type) || type == CharacterType.Centipede.ToString())
        {
            return;
        }

        if (Random.value > RuntimeExtraCentipedeChance)
        {
            return;
        }

        spawningExtraCentipede = true;
        try
        {
            Character centipede = __instance.spawnCharacterAround(
                destGO,
                offset,
                distance,
                CharacterType.Centipede.ToString(),
                nocturnal,
                attackPlayer,
                relentlessPursuit,
                canSpawnInside);

            if (centipede == null)
            {
                return;
            }

            CentipedeEverywhereSpawnPatch.ConfigureCentipede(centipede, nocturnal, attackPlayer);
            DarkwoodPlugin.AppendVerboseDebug($"Extra runtime Centipede spawned alongside {type}");
        }
        finally
        {
            spawningExtraCentipede = false;
        }
    }
}

[HarmonyPatch(typeof(CharacterSpawner), "spawnNightChar")]
internal static class CentipedeNightSpawnPatch
{
    private const float NightCentipedeExtraSpawnChance = 0.4f;
    private const float NightCentipedeMinIntervalSeconds = 45f;
    private const float NightCentipedeSpawnDistance = 1200f;

    private static float lastNightCentipedeSpawnTime = -9999f;

    private static void Postfix(CharacterSpawner __instance)
    {
        if (__instance == null || Player.Instance == null)
        {
            return;
        }

        if (Time.time - lastNightCentipedeSpawnTime < NightCentipedeMinIntervalSeconds)
        {
            return;
        }

        if (Random.value > NightCentipedeExtraSpawnChance)
        {
            return;
        }

        Character centipede = __instance.spawnCharacterAround(
            Player.Instance.gameObject,
            Vector3.zero,
            NightCentipedeSpawnDistance,
            CharacterType.Centipede.ToString(),
            true,
            true,
            false,
            false);

        if (centipede == null)
        {
            return;
        }

        CentipedeEverywhereSpawnPatch.ConfigureCentipede(centipede, nocturnal: true, attackPlayer: true);
        lastNightCentipedeSpawnTime = Time.time;
        DarkwoodPlugin.AppendVerboseDebug("Extra night Centipede spawned near player");
    }
}
