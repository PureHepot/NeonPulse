using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponModuleBase : PlayerModule
{
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

        if (pluginRuntime.pluginConfig.pluginType == PluginType.ExtraMuzzle ||
            string.Equals(pluginRuntime.pluginConfig.effectId, "ExtraMuzzle", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pluginRuntime.pluginConfig.effectId, "ExtraMuzzleCount", System.StringComparison.OrdinalIgnoreCase))
        {
            return new ExtraMuzzleWeaponModuleEffect();
        }

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

public sealed class ExtraMuzzleWeaponModuleEffect : IWeaponModuleEffect
{
    private const int DefaultExtraMuzzleCount = 1;
    private const float DefaultMuzzleSpacing = 0.18f;

    private LoadoutPluginRuntimeData pluginRuntime;

    public void Initialize(WeaponModuleBase owner, LoadoutPluginRuntimeData pluginRuntimeData)
    {
        pluginRuntime = pluginRuntimeData;
    }

    public void ModifyMuzzlePlan(WeaponFireContext context, List<WeaponMuzzlePoint> muzzlePlan)
    {
        if (muzzlePlan == null || muzzlePlan.Count == 0)
            return;

        int extraCount = Mathf.Max(
            0,
            Mathf.RoundToInt(pluginRuntime.effectParams.param1 > 0f
                ? pluginRuntime.effectParams.param1
                : DefaultExtraMuzzleCount));
        if (extraCount <= 0)
            return;

        float spacing = pluginRuntime.effectParams.param2 > 0f
            ? pluginRuntime.effectParams.param2
            : DefaultMuzzleSpacing;

        Quaternion rotation = muzzlePlan[0].rotation;
        Vector3 center = Vector3.zero;
        for (int index = 0; index < muzzlePlan.Count; index++)
            center += muzzlePlan[index].position;

        center /= muzzlePlan.Count;

        int originalCount = muzzlePlan.Count;
        int finalCount = originalCount + extraCount;
        var expandedPlan = new List<WeaponMuzzlePoint>(finalCount);

        for (int index = 0; index < finalCount; index++)
        {
            float centeredIndex = index - (finalCount - 1) * 0.5f;
            Vector3 offset = (rotation * Vector3.up) * (spacing * centeredIndex);
            expandedPlan.Add(new WeaponMuzzlePoint
            {
                position = center + offset,
                rotation = rotation,
                visualTransform = index < originalCount ? muzzlePlan[index].visualTransform : null,
                isVirtual = index >= originalCount
            });
        }

        muzzlePlan.Clear();
        muzzlePlan.AddRange(expandedPlan);
    }

    public void ModifyProjectileSpawnData(WeaponFireContext context, ProjectileSpawnData spawnData)
    {
    }

    public void OnProjectileSpawned(WeaponFireContext context, GameObject projectileObject)
    {
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
