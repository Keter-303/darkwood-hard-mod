using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace DarkwoodMod;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public sealed class DarkwoodPlugin : BaseUnityPlugin
{
    public const string ModGuid = "com.example.darkwoodmod";
    public const string ModName = "Darkwood Mod";
    public const string ModVersion = "0.1.0";
    public static readonly bool EnableVerboseDebug = false;
    public static readonly bool EnableDevHotkeys = true;

    internal static ManualLogSource Log = null!;

    private Harmony? harmony;

    private static string DebugLogPath => Path.Combine(Path.GetTempPath(), "DarkwoodMod-debug.log");

    private void Awake()
    {
        Log = Logger;
        AppendDebug("Darkwood Mod loaded");
        harmony = new Harmony(ModGuid);
        harmony.PatchAll();
        Logger.LogInfo($"{ModName} loaded");
    }

    private void OnDestroy()
    {
        if (harmony != null)
        {
            harmony.UnpatchSelf();
        }
    }

    private void Update()
    {
        if (!EnableDevHotkeys || !Input.GetKeyDown(KeyCode.F8))
        {
            return;
        }

        SpawnTestCentipede();
    }

    private static void SpawnTestCentipede()
    {
        if (Player.Instance == null || CharacterSpawner.Instance == null)
        {
            AppendDebug("F8 Centipede test failed: Player or CharacterSpawner missing");
            return;
        }

        Character centipede = CharacterSpawner.Instance.spawnCharacterAround(
            Player.Instance.gameObject,
            Vector3.zero,
            450f,
            CharacterType.Centipede.ToString(),
            false,
            true,
            false,
            true);

        if (centipede != null)
        {
            ConfigureTestCentipede(centipede);
        }

        AppendDebug(centipede != null
            ? $"F8 Centipede test spawned name={centipede.name} type={centipede.type}"
            : "F8 Centipede test failed: spawnCharacterAround returned null");
    }

    private static void ConfigureTestCentipede(Character centipede)
    {
        centipede.type = CharacterType.Centipede;
        centipede.nocturnal = false;
        centipede.temporarySpawned = false;
        centipede.wantToDespawn = false;
        centipede.spawnedManually = true;
        centipede.alwaysActive = true;
        centipede.relentlessPursuit = true;
        centipede.isActive = true;
        centipede.attackPlayer();
    }

    internal static void AppendDebug(string message)
    {
        try
        {
            File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    internal static void AppendVerboseDebug(string message)
    {
        if (!EnableVerboseDebug)
        {
            return;
        }

        AppendDebug(message);
    }
}
