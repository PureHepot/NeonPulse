using UnityEngine;

public abstract class DefenceModule : PlayerModule
{
    private const string DamageReductionStatId = "defence.damagereduction";
    private const string DamageReflectionStatId = "defence.damagereflectionefficiency";
    private const string DoubleDefenceReflectionEffectId = PluginSpecialEffectUtility.DoubleDefenceReflectionEffectId;

    protected float DamageReductionMultiplier { get; private set; } = 1f;
    protected float DamageReflectionPercent { get; private set; }

    protected override void OnInitialize()
    {
        RefreshDefenceModifiers();
    }

    protected override void OnActivate()
    {
        RefreshDefenceModifiers();
    }

    protected override void OnDeactivate()
    {
        RefreshDefenceModifiers();
    }

    protected void RefreshDefenceModifiers()
    {
        DamageReductionMultiplier = NormalizePercentMultiplier(ResolveAggregateStat(DamageReductionStatId, 100f), 1f);
        DamageReflectionPercent = NormalizePercentValue(ResolveAggregateStat(DamageReflectionStatId, 0f)) * ResolveReflectionMultiplier();

        var healthModule = player != null && player.Modules != null
            ? player.Modules.GetModule<HealthModule>(ModuleType.Health)
            : null;
        if (healthModule == null)
            return;

        healthModule.ConfigureDefenceModifiers(DamageReductionMultiplier, DamageReflectionPercent);
    }

    private float ResolveReflectionMultiplier()
    {
        if (!TryGetPlugin(DoubleDefenceReflectionEffectId, out var pluginRuntime))
            return 1f;

        return PluginSpecialEffectUtility.ResolveMultiplier(pluginRuntime);
    }

    private float ResolveAggregateStat(string statId, float fallbackValue)
    {
        var loadoutManager = GameMgr.Instance != null ? GameMgr.Instance.Loadout : null;
        if (loadoutManager == null || string.IsNullOrWhiteSpace(statId))
            return fallbackValue;

        float value = loadoutManager.GetFinalStat(statId);
        return Mathf.Approximately(value, 0f) ? fallbackValue : value;
    }

    private static float NormalizePercentMultiplier(float rawValue, float fallbackValue)
    {
        if (rawValue < 0f)
            return 0f;

        if (rawValue > 1f)
            return rawValue / 100f;

        return rawValue > 0f ? rawValue : fallbackValue;
    }

    private static float NormalizePercentValue(float rawValue)
    {
        if (rawValue <= 0f)
            return 0f;

        return rawValue > 1f ? rawValue / 100f : rawValue;
    }
}
