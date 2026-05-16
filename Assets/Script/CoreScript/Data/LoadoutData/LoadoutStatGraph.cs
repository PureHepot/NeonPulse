using System.Collections.Generic;
using GameDataTool;
using UnityEngine;

public sealed class LoadoutStatGraph
{
    private const string CoreAddGroupId = "core_add";
    private const string CoreMultiplierGroupId = "core_multiplier";
    private const string PluginAddGroupId = "plugin_add";
    private const string PluginMultiplierGroupId = "plugin_multiplier";

    private readonly ModuleConfig moduleConfig;
    private readonly ModuleRarity moduleRarity;
    private readonly CoreConfig coreConfig;
    private readonly IReadOnlyList<LoadoutPluginRuntimeData> pluginRuntimes;

    private readonly Dictionary<string, SealedValue<float>> statValuesById = new Dictionary<string, SealedValue<float>>();

    public LoadoutStatGraph(ModuleConfig moduleConfig, ModuleRarity moduleRarity, CoreConfig coreConfig, IReadOnlyList<LoadoutPluginRuntimeData> pluginRuntimes = null)
    {
        this.moduleConfig = moduleConfig;
        this.moduleRarity = moduleRarity;
        this.coreConfig = coreConfig;
        this.pluginRuntimes = pluginRuntimes;
    }

    public float GetFinalStat(StatDefinition statDefinition)
    {
        if (moduleConfig == null || statDefinition == null)
            return 0f;

        return GetFinalStat(statDefinition.StatId);
    }

    public float GetFinalStat(string statId)
    {
        if (moduleConfig == null || string.IsNullOrWhiteSpace(statId))
            return 0f;

        string normalizedStatId = statId.Trim().ToLowerInvariant();
        if (!statValuesById.TryGetValue(normalizedStatId, out var sealedValue))
        {
            sealedValue = CreateGraph(moduleConfig.GetBaseStat(normalizedStatId, moduleRarity), normalizedStatId);
            statValuesById.Add(normalizedStatId, sealedValue);
        }

        return sealedValue.Value;
    }

    private SealedValue<float> CreateGraph(float baseValue, string statId)
    {
        var sealedValue = new SealedValue<float>(baseValue)
            .AddModification(new FloatAddGroup(CoreAddGroupId))
            .AddModification(new FloatMultipleMulGroup(CoreMultiplierGroupId))
            .AddModification(new FloatAddGroup(PluginAddGroupId))
            .AddModification(new FloatMultipleMulGroup(PluginMultiplierGroupId));

        ApplyCoreBonuses(sealedValue, statId);
        ApplyPluginBonuses(sealedValue, statId);
        return sealedValue;
    }

    private void ApplyCoreBonuses(SealedValue<float> sealedValue, string statId)
    {
        if (coreConfig?.statBonuses == null || string.IsNullOrWhiteSpace(statId))
            return;

        foreach (var bonus in coreConfig.statBonuses)
        {
            if (!bonus.Matches(statId))
                continue;

            if (!Mathf.Approximately(bonus.additiveBonus, 0f))
                sealedValue.TryAddModifier(CoreAddGroupId, new Modifier<float>(bonus.additiveBonus));

            if (!Mathf.Approximately(bonus.multiplicativeBonus, 0f))
                sealedValue.TryAddModifier(
                    CoreMultiplierGroupId,
                    new Modifier<float>(1f + bonus.multiplicativeBonus));
        }
    }

    private void ApplyPluginBonuses(SealedValue<float> sealedValue, string statId)
    {
        if (pluginRuntimes == null || string.IsNullOrWhiteSpace(statId))
            return;

        for (int runtimeIndex = 0; runtimeIndex < pluginRuntimes.Count; runtimeIndex++)
        {
            var pluginRuntime = pluginRuntimes[runtimeIndex];
            var modifiers = pluginRuntime?.pluginConfig?.GetStatModifiers(pluginRuntime.rarity);
            if (modifiers == null)
                continue;

            for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++)
            {
                var modifier = modifiers[modifierIndex];
                if (!modifier.Matches(statId))
                    continue;

                if (!Mathf.Approximately(modifier.additiveBonus, 0f))
                    sealedValue.TryAddModifier(PluginAddGroupId, new Modifier<float>(modifier.additiveBonus));

                if (!Mathf.Approximately(modifier.multiplicativeBonus, 0f))
                {
                    sealedValue.TryAddModifier(
                        PluginMultiplierGroupId,
                        new Modifier<float>(1f + modifier.multiplicativeBonus));
                }
            }
        }
    }
}
