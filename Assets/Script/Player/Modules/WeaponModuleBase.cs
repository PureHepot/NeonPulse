using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponModuleBase : PlayerModule
{
    private const string WeaponSelfBackForceStatId = "weapon.selfbackforce";
    private const string DoubleWeaponDamageEffectId = PluginSpecialEffectUtility.DoubleWeaponDamageEffectId;
    private const string DoubleWeaponAttackSpeedEffectId = PluginSpecialEffectUtility.DoubleWeaponAttackSpeedEffectId;
    private readonly List<IWeaponModuleEffect> effects = new();
    private float cooldownRemaining;

    protected bool WantsPrimaryFire => HasControl && InputManager.Instance != null && InputManager.Instance.Mouse0();
    protected bool CanFire => cooldownRemaining <= 0f;
    protected float CooldownRemaining => cooldownRemaining;
    protected IReadOnlyList<IWeaponModuleEffect> Effects => effects;

    protected override void OnInitialize()
    {
        RebuildEffects();
        OnWeaponInitialize();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead)
            return;

        if (cooldownRemaining > 0f)
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - DeltaTime);

        OnWeaponUpdate();
    }

    protected void SetCooldown(float duration)
    {
        cooldownRemaining = Mathf.Max(0f, duration);
    }

    protected void RotateTowardsAim(Transform pivot, float rotationSpeed)
    {
        if (pivot == null)
            return;

        Vector3 aimTarget = HasControl ? MUtils.GetMouseWorldPosition() : pivot.position + pivot.right;
        Vector2 direction = aimTarget - pivot.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * DeltaTime);
    }

    protected void ApplyMuzzlePlanEffects(WeaponFireContext context, List<WeaponMuzzlePoint> muzzlePlan)
    {
        for (int index = 0; index < effects.Count; index++)
            effects[index].ModifyMuzzlePlan(context, muzzlePlan);
    }

    protected void ApplyProjectileEffects(WeaponFireContext context, ProjectileSpawnData spawnData)
    {
        for (int index = 0; index < effects.Count; index++)
            effects[index].ModifyProjectileSpawnData(context, spawnData);
    }

    protected void NotifyProjectileSpawned(WeaponFireContext context, GameObject projectileObject)
    {
        for (int index = 0; index < effects.Count; index++)
            effects[index].OnProjectileSpawned(context, projectileObject);
    }

    protected void ApplySelfBackForce(Vector2 shotDirection, float fallbackForce = 0f, float forceScale = 1f)
    {
        if (player == null)
            return;

        if (shotDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        float selfBackForce = GetStat(WeaponSelfBackForceStatId, fallbackForce);
        if (selfBackForce <= 0f)
            return;

        player.AddImpulse(-shotDirection.normalized * (selfBackForce * Mathf.Max(0f, forceScale)));
    }

    protected float ApplyWeaponDamageMultiplier(float baseDamage)
    {
        return baseDamage * ResolvePluginMultiplier(DoubleWeaponDamageEffectId);
    }

    protected float ApplyWeaponFireIntervalMultiplier(float baseFireInterval)
    {
        float multiplier = ResolvePluginMultiplier(DoubleWeaponAttackSpeedEffectId);
        if (multiplier <= 0f)
            return baseFireInterval;

        return baseFireInterval / multiplier;
    }

    private void RebuildEffects()
    {
        effects.Clear();
        var plugins = RuntimeData != null ? RuntimeData.Plugins : null;
        if (plugins == null)
            return;

        for (int index = 0; index < plugins.Count; index++)
        {
            var effect = WeaponModuleEffectFactory.Create(plugins[index]);
            if (effect == null)
                continue;

            effect.Initialize(this, plugins[index]);
            effects.Add(effect);
        }
    }

    private float ResolvePluginMultiplier(string effectId)
    {
        float multiplier = 1f;
        var plugins = RuntimeData != null ? RuntimeData.Plugins : null;
        if (plugins == null)
            return multiplier;

        for (int index = 0; index < plugins.Count; index++)
        {
            if (!PluginSpecialEffectUtility.MatchesEffect(plugins[index], effectId))
                continue;

            multiplier *= PluginSpecialEffectUtility.ResolveMultiplier(plugins[index]);
        }

        return multiplier;
    }

    protected abstract void OnWeaponInitialize();
    protected abstract void OnWeaponUpdate();
}

public interface IWeaponModuleEffect
{
    void Initialize(WeaponModuleBase owner, LoadoutPluginRuntimeData pluginRuntime);
    void ModifyMuzzlePlan(WeaponFireContext context, List<WeaponMuzzlePoint> muzzlePlan);
    void ModifyProjectileSpawnData(WeaponFireContext context, ProjectileSpawnData spawnData);
    void OnProjectileSpawned(WeaponFireContext context, GameObject projectileObject);
}

public static class WeaponModuleEffectFactory
{
    public static IWeaponModuleEffect Create(LoadoutPluginRuntimeData pluginRuntime)
    {
        if (pluginRuntime?.pluginConfig == null)
            return null;

        if (pluginRuntime.pluginConfig.pluginType == PluginType.Homing ||
            string.Equals(pluginRuntime.pluginConfig.effectId, "Homing", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pluginRuntime.pluginConfig.effectId, "Chase", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pluginRuntime.pluginConfig.effectId, "ChasePlugin", System.StringComparison.OrdinalIgnoreCase))
        {
            return new HomingWeaponModuleEffect();
        }

        return null;
    }
}

public sealed class HomingWeaponModuleEffect : IWeaponModuleEffect
{
    private const float DefaultTurnRate = 360f;
    private const float DefaultAcquireRadius = 6f;
    private const float DefaultRetargetInterval = 0.15f;

    private LoadoutPluginRuntimeData pluginRuntime;

    public void Initialize(WeaponModuleBase owner, LoadoutPluginRuntimeData pluginRuntimeData)
    {
        pluginRuntime = pluginRuntimeData;
    }

    public void ModifyMuzzlePlan(WeaponFireContext context, List<WeaponMuzzlePoint> muzzlePlan)
    {
    }

    public void ModifyProjectileSpawnData(WeaponFireContext context, ProjectileSpawnData spawnData)
    {
        if (spawnData == null)
            return;

        spawnData.homingEnabled = true;
        spawnData.homingTurnRate = pluginRuntime.effectParams.param1 > 0f
            ? pluginRuntime.effectParams.param1
            : DefaultTurnRate;
        spawnData.homingAcquireRadius = pluginRuntime.effectParams.param2 > 0f
            ? pluginRuntime.effectParams.param2
            : DefaultAcquireRadius;
        spawnData.homingRetargetInterval = pluginRuntime.effectParams.param3 > 0f
            ? pluginRuntime.effectParams.param3
            : DefaultRetargetInterval;
    }

    public void OnProjectileSpawned(WeaponFireContext context, GameObject projectileObject)
    {
    }
}
