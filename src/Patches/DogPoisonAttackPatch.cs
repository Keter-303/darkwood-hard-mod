using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace DarkwoodMod.Patches;

[HarmonyPatch]
internal static class DogPoisonAttackPatch
{
    private const float PoisonTickDamage = 1f;
    private const float PoisonTickIntervalSeconds = 1f;
    private const float PoisonDurationSeconds = 8f;
    private const float MutatedPoisonTickDamage = 1.35f;
    private const float MutatedPoisonTickIntervalSeconds = 1f;
    private const float MutatedPoisonDurationSeconds = 10f;
    private const float PoisonCooldownSeconds = 0.75f;

    private static readonly Dictionary<int, float> lastPoisonByPlayer = new();

    [HarmonyPatch(typeof(MeleeSensor), "OnTriggerEnter", typeof(Collider))]
    [HarmonyPostfix]
    private static void MeleeSensorOnTriggerEnterPostfix(MeleeSensor __instance, Collider _collider)
    {
        if (__instance == null || _collider == null)
        {
            return;
        }

        Dog? dog = FindDog(__instance.attackerTransform);
        if (dog == null)
        {
            return;
        }

        Player player = _collider.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        TryApplyPoison(player, dog, $"MeleeSensor.OnTriggerEnter attacker={GetTransformPath(__instance.attackerTransform)} dog={dog.name}");
    }

    [HarmonyPatch(
        typeof(Player),
        nameof(Player.getHit),
        typeof(float),
        typeof(Transform),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool))]
    [HarmonyPostfix]
    private static void PlayerGetHitPostfix(Player __instance, Transform attackerTransform)
    {
        if (__instance == null || attackerTransform == null)
        {
            return;
        }

        Dog? dog = FindDog(attackerTransform);
        if (dog == null)
        {
            return;
        }

        TryApplyPoison(__instance, dog, $"Player.getHit attacker={GetTransformPath(attackerTransform)} dog={dog.name}");
    }

    private static void TryApplyPoison(Player player, Dog dog, string source)
    {
        if (player.hasEffect(CharacterEffectType.poison))
        {
            return;
        }

        int playerId = player.GetInstanceID();
        float currentTime = Time.time;

        if (lastPoisonByPlayer.TryGetValue(playerId, out float lastTime) && currentTime - lastTime < PoisonCooldownSeconds)
        {
            return;
        }

        lastPoisonByPlayer[playerId] = currentTime;
        bool mutated = DogAggressionPatch.IsMutatedDog(dog);
        DarkwoodPlugin.AppendVerboseDebug($"Dog poison applied via {source} to player={player.name}");
        player.effects.activate(
            CharacterEffectType.poison,
            mutated ? MutatedPoisonDurationSeconds : PoisonDurationSeconds,
            mutated ? MutatedPoisonTickDamage : PoisonTickDamage,
            mutated ? MutatedPoisonTickIntervalSeconds : PoisonTickIntervalSeconds,
            0f);
    }

    private static Dog? FindDog(Transform attackerTransform)
    {
        Dog dog = attackerTransform.GetComponentInParent<Dog>();
        if (dog != null)
        {
            return dog;
        }

        Transform root = attackerTransform.root;
        if (root != null)
        {
            dog = root.GetComponent<Dog>();
            if (dog != null)
            {
                return dog;
            }

            dog = root.GetComponentInChildren<Dog>();
            if (dog != null)
            {
                return dog;
            }
        }

        return null;
    }

    private static string GetTransformPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}
