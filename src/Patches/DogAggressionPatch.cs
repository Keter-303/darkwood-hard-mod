using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DarkwoodMod.Patches;

[HarmonyPatch(typeof(Dog), nameof(Dog.Start))]
internal static class DogAggressionPatch
{
    private const float DogChaseSpeedMultiplier = 1.2f;
    private const float MutatedDogChaseSpeedMultiplier = 1.45f;
    private const float DogAttackIntervalMultiplier = 0.55f;
    private const float MutatedDogAttackIntervalMultiplier = 0.38f;
    private const float DogRecoverTimeMultiplier = 0.65f;
    private const float MutatedDogRecoverTimeMultiplier = 0.35f;
    private const float RotationSpeedMultiplier = 2f;
    private const float MutatedRotationSpeedMultiplier = 2.5f;

    private static readonly FieldInfo? characterTurningSpeedField = AccessTools.Field(typeof(Character), "turningSpeed");
    private static readonly FieldInfo? characterMinimumTimeToReachRotationField = AccessTools.Field(typeof(Character), "minimumTimeToReachRotation");
    private static readonly FieldInfo? characterMaximumRotateSpeedField = AccessTools.Field(typeof(Character), "maximumRotateSpeed");

    private static void Postfix(Dog __instance)
    {
        if (__instance == null)
        {
            return;
        }

        bool mutated = IsMutatedDog(__instance);
        if (mutated)
        {
            __instance.type = CharacterType.DogMutated;
        }

        __instance.aggressiveness = Aggressiveness.attackOnSight;
        __instance.attackType = AttackType.normal;
        __instance.aggressive = true;
        __instance.onlyAttackPlayer = true;
        __instance.attackClosestCharacerAfterWakingUp = true;
        __instance.constantlyAttackPlayer = false;
        __instance.returnToSleepAfterLosingTarget = !mutated;
        __instance.afraidOfGunshots = !mutated && __instance.afraidOfGunshots;
        __instance.afraidOfHideout = !mutated && __instance.afraidOfHideout;
        __instance.afraidOfForestSpiritWard = !mutated && __instance.afraidOfForestSpiritWard;
        __instance.canAttackDoor = mutated || __instance.canAttackDoor;

        __instance.enemyInSightCounterTarget = Mathf.Min(__instance.enemyInSightCounterTarget, mutated ? 0.05f : 0.25f);
        __instance.enemyInSightCounterFade = Mathf.Max(__instance.enemyInSightCounterFade, mutated ? 6f : 2f);
        __instance.farViewDistance = Mathf.Max(__instance.farViewDistance, mutated ? 650 : 26);
        __instance.nearViewDistance = Mathf.Max(__instance.nearViewDistance, mutated ? 360 : 14);
        __instance.sleepViewDistance = Mathf.Max(__instance.sleepViewDistance, mutated ? 220 : 10);
        __instance.fieldOfViewRange = Mathf.Max(__instance.fieldOfViewRange, mutated ? 280 : __instance.fieldOfViewRange);

        __instance.chaseSpeed *= mutated ? MutatedDogChaseSpeedMultiplier : DogChaseSpeedMultiplier;
        __instance.idleWalkSpeed *= mutated ? 1.4f : 1f;

        if (mutated)
        {
            __instance.maxStamina = Mathf.Max(__instance.maxStamina, 300f);
            __instance.stamina = __instance.maxStamina;
            __instance.runSpeedModifier = Mathf.Max(__instance.runSpeedModifier, 1.15f);
            __instance.speedModifier = Mathf.Max(__instance.speedModifier, 1f);
        }

        TuneRotation(__instance, mutated);

        if (__instance.AIpath != null)
        {
            __instance.AIpath.speed = Mathf.Max(__instance.AIpath.speed, __instance.chaseSpeed);
            __instance.AIpath.turningSpeed *= mutated ? MutatedRotationSpeedMultiplier : RotationSpeedMultiplier;
            __instance.AIpath.maximumRotateSpeed *= mutated ? MutatedRotationSpeedMultiplier : RotationSpeedMultiplier;

            if (mutated)
            {
                __instance.AIpath.pickNextWaypointDist = Mathf.Max(__instance.AIpath.pickNextWaypointDist, 6f);
                __instance.AIpath.endReachedDistance = Mathf.Max(__instance.AIpath.endReachedDistance, 4f);
                __instance.AIpath.repathRate = Mathf.Min(__instance.AIpath.repathRate, 0.25f);
            }
        }

        TuneAttacks(__instance, mutated);

        DarkwoodPlugin.AppendVerboseDebug(
            $"Dog aggression tuned name={__instance.name} type={__instance.type} chaseSpeed={__instance.chaseSpeed:0.##} farView={__instance.farViewDistance} nearView={__instance.nearViewDistance}");
    }

    internal static bool IsMutatedDog(Dog dog)
    {
        if (dog.type == CharacterType.DogMutated)
        {
            return true;
        }

        string name = dog.name ?? string.Empty;
        return name.Contains("DogMutated") || name.Contains("Mutated");
    }

    private static void TuneRotation(Dog dog, bool mutated)
    {
        float multiplier = mutated ? MutatedRotationSpeedMultiplier : RotationSpeedMultiplier;
        MultiplyFloatField(characterTurningSpeedField, dog, multiplier);
        MultiplyFloatField(characterMaximumRotateSpeedField, dog, multiplier);
        MultiplyFloatField(characterMinimumTimeToReachRotationField, dog, 1f / multiplier);
    }

    private static void MultiplyFloatField(FieldInfo? field, Dog dog, float multiplier)
    {
        if (field == null)
        {
            return;
        }

        object? value = field.GetValue(dog);
        if (value is not float current)
        {
            return;
        }

        field.SetValue(dog, current * multiplier);
    }

    private static void TuneAttacks(Dog dog, bool mutated)
    {
        if (dog.attacks != null)
        {
            float intervalMultiplier = mutated ? MutatedDogAttackIntervalMultiplier : DogAttackIntervalMultiplier;
            float recoverMultiplier = mutated ? MutatedDogRecoverTimeMultiplier : DogRecoverTimeMultiplier;

            foreach (Character.Attack attack in dog.attacks)
            {
                if (attack == null)
                {
                    continue;
                }

                attack.attackInterval = Mathf.Max(0.35f, attack.attackInterval * intervalMultiplier);
                attack.recoverTime = Mathf.Max(0.15f, attack.recoverTime * recoverMultiplier);
                attack.minAttackDistance = Mathf.Max(0, attack.minAttackDistance - 1);
                attack.maxAttackDistance += mutated ? 2 : 1;
            }
        }

        if (dog.attackIntervals != null)
        {
            float intervalMultiplier = mutated ? MutatedDogAttackIntervalMultiplier : DogAttackIntervalMultiplier;

            foreach (Character.Attack.Interval interval in dog.attackIntervals)
            {
                if (interval == null)
                {
                    continue;
                }

                interval.interval = Mathf.Max(0.35f, interval.interval * intervalMultiplier);
            }
        }

        dog.wantToAttackDistance = GetMaxAttackDistance(dog);
    }

    private static int GetMaxAttackDistance(Dog dog)
    {
        int distance = dog.wantToAttackDistance;

        if (dog.attacks == null)
        {
            return distance;
        }

        foreach (Character.Attack attack in dog.attacks)
        {
            if (attack != null && attack.maxAttackDistance > distance)
            {
                distance = attack.maxAttackDistance;
            }
        }

        return distance;
    }
}

[HarmonyPatch(typeof(Character), "melee")]
internal static class DogMeleeAimPatch
{
    private static void Prefix(Character __instance)
    {
        if (__instance is not Dog dog || dog.target == null)
        {
            return;
        }

        Transform dogTransform = dog.transform;
        Vector3 direction = dog.target.position - dogTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 eulerAngles = dogTransform.eulerAngles;
        eulerAngles.y = Quaternion.LookRotation(direction).eulerAngles.y;
        dogTransform.eulerAngles = eulerAngles;
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Update))]
internal static class MutatedDogMovementSustainPatch
{
    private static void Postfix(Character __instance)
    {
        if (__instance is not Dog dog || !DogAggressionPatch.IsMutatedDog(dog))
        {
            return;
        }

        dog.stamina = dog.maxStamina;
        dog.wantToDespawn = false;
    }
}

[HarmonyPatch(typeof(MeleeSensor), "FixedUpdate")]
internal static class DogMeleeSensorPatch
{
    private const float DogSensorScaleMultiplier = 1.35f;
    private const float MutatedDogSensorScaleMultiplier = 2.1f;
    private const float DogSensorLongevityMultiplier = 1.25f;
    private const float MutatedDogSensorLongevityMultiplier = 1.8f;

    private static readonly Dictionary<int, Vector3> originalScaleBySensor = new();
    private static readonly Dictionary<int, float> originalLongevityBySensor = new();

    private static void Prefix(MeleeSensor __instance)
    {
        if (__instance == null)
        {
            return;
        }

        int id = __instance.GetInstanceID();
        Transform transform = __instance.transform;

        if (!originalScaleBySensor.ContainsKey(id))
        {
            originalScaleBySensor[id] = transform.localScale;
            originalLongevityBySensor[id] = __instance.longevity;
        }

        Vector3 originalScale = originalScaleBySensor[id];
        float originalLongevity = originalLongevityBySensor[id];
        Dog? dog = FindDog(__instance.attackerTransform);

        if (dog == null)
        {
            transform.localScale = originalScale;
            __instance.longevity = originalLongevity;
            return;
        }

        bool mutated = DogAggressionPatch.IsMutatedDog(dog);
        float scaleMultiplier = mutated ? MutatedDogSensorScaleMultiplier : DogSensorScaleMultiplier;
        float longevityMultiplier = mutated ? MutatedDogSensorLongevityMultiplier : DogSensorLongevityMultiplier;

        transform.localScale = originalScale * scaleMultiplier;
        __instance.longevity = Mathf.Max(__instance.longevity, originalLongevity * longevityMultiplier);
    }

    private static Dog? FindDog(Transform attackerTransform)
    {
        if (attackerTransform == null)
        {
            return null;
        }

        Dog dog = attackerTransform.GetComponentInParent<Dog>();
        if (dog != null)
        {
            return dog;
        }

        Transform root = attackerTransform.root;
        if (root == null)
        {
            return null;
        }

        dog = root.GetComponent<Dog>();
        return dog != null ? dog : root.GetComponentInChildren<Dog>();
    }
}
