using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;

namespace DarkwoodMod;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public sealed class DarkwoodPlugin : BaseUnityPlugin
{
    public const string ModGuid = "com.example.darkwoodmod";
    public const string ModName = "Darkwood Mod";
    public const string ModVersion = "0.1.0";

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
}
