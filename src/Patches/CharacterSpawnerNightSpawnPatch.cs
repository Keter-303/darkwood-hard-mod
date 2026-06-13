using HarmonyLib;

namespace DarkwoodMod.Patches;

[HarmonyPatch(typeof(CharacterSpawner), "spawnNightChar")]
internal static class CharacterSpawnerNightSpawnPatch
{
    private static void Prefix(ref int ___spawnMax, out int __state)
    {
        __state = ___spawnMax;
        ___spawnMax *= 2;
    }

    private static void Postfix(ref int ___spawnMax, int __state)
    {
        ___spawnMax = __state;
    }
}