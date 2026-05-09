using System;
using System.Collections.Generic;

public sealed class LoadoutPluginRuntimeData
{
    public string pluginId;
    public PluginRarity rarity = PluginRarity.Common;
    public PluginConfig pluginConfig;
    public PluginEffectParams effectParams;

    public bool Matches(string effectId)
    {
        return pluginConfig != null &&
               string.Equals(pluginConfig.effectId, effectId, StringComparison.OrdinalIgnoreCase);
    }

    public bool Matches(PluginType pluginType)
    {
        return pluginConfig != null && pluginConfig.pluginType == pluginType;
    }
}

public sealed class LoadoutModuleRuntimeData
{
    public string slotId;
    public string moduleId;
    public ModuleType moduleType = ModuleType.None;
    public ModuleRarity moduleRarity = ModuleRarity.Common;
    public string coreId;
    public GameConfigDatabase database;

    public ModuleConfig moduleConfig;
    public CoreConfig coreConfig;
    public readonly List<LoadoutPluginRuntimeData> pluginRuntimes = new();
    public LoadoutStatGraph statGraph;

    public bool HasModule => moduleConfig != null;
    public bool HasCore => coreConfig != null;
    public IReadOnlyList<LoadoutPluginRuntimeData> Plugins => pluginRuntimes;

    public float GetFinalStat(StatDefinition statDefinition)
    {
        return HasModule ? statGraph.GetFinalStat(statDefinition) : 0f;
    }

    public float GetFinalStat(string statId)
    {
        if (!HasModule || string.IsNullOrWhiteSpace(statId))
            return 0f;

        return statGraph.GetFinalStat(statId);
    }

    public int GetPluginCapacity()
    {
        return HasModule ? moduleConfig.GetPluginSlots(moduleRarity) : 0;
    }

    public int GetLoadCost()
    {
        return HasModule ? moduleConfig.GetLoadCost(moduleRarity) : 0;
    }

    public bool HasPlugin(string effectId)
    {
        return TryGetPlugin(effectId, out _);
    }

    public bool HasPlugin(PluginType pluginType)
    {
        return TryGetPlugin(pluginType, out _);
    }

    public bool TryGetPlugin(string effectId, out LoadoutPluginRuntimeData pluginRuntime)
    {
        for (int index = 0; index < pluginRuntimes.Count; index++)
        {
            if (pluginRuntimes[index].Matches(effectId))
            {
                pluginRuntime = pluginRuntimes[index];
                return true;
            }
        }

        pluginRuntime = null;
        return false;
    }

    public bool TryGetPlugin(PluginType pluginType, out LoadoutPluginRuntimeData pluginRuntime)
    {
        for (int index = 0; index < pluginRuntimes.Count; index++)
        {
            if (pluginRuntimes[index].Matches(pluginType))
            {
                pluginRuntime = pluginRuntimes[index];
                return true;
            }
        }

        pluginRuntime = null;
        return false;
    }
}

public static class LoadoutModuleRuntimeBuilder
{
    public static LoadoutModuleRuntimeData Build(SlotRuntimeSaveData slotData, GameConfigDatabase database)
    {
        if (slotData == null)
            return null;

        return BuildInternal(
            slotData.slotId,
            slotData.moduleId,
            slotData.moduleType,
            slotData.moduleRarity,
            slotData.coreId,
            slotData.plugins,
            database);
    }

    public static LoadoutModuleRuntimeData Build(SlotSaveData slotData, GameConfigDatabase database)
    {
        if (slotData == null)
            return null;

        return BuildInternal(
            slotData.slotId,
            slotData.moduleId,
            slotData.moduleType,
            slotData.moduleRarity,
            slotData.coreId,
            slotData.plugins,
            database);
    }

    public static ModuleConfig ResolveModuleConfig(GameConfigDatabase database, string moduleId, ModuleType moduleType)
    {
        if (database?.allModules == null)
            return null;

        if (!string.IsNullOrEmpty(moduleId))
        {
            foreach (var module in database.allModules)
            {
                if (module.ModuleId == moduleId)
                    return module;
            }
        }

        foreach (var module in database.allModules)
        {
            if (module.moduleType == moduleType)
                return module;
        }

        return null;
    }

    public static CoreConfig ResolveCoreConfig(GameConfigDatabase database, string coreId)
    {
        if (string.IsNullOrEmpty(coreId) || database?.allCores == null)
            return null;

        foreach (var core in database.allCores)
        {
            if (core.coreId == coreId)
                return core;
        }

        return null;
    }

    private static LoadoutModuleRuntimeData BuildInternal(
        string slotId,
        string moduleId,
        ModuleType moduleType,
        ModuleRarity moduleRarity,
        string coreId,
        List<PluginInstanceSaveData> plugins,
        GameConfigDatabase database)
    {
        var runtimeData = new LoadoutModuleRuntimeData
        {
            slotId = slotId,
            moduleId = moduleId,
            moduleType = moduleType,
            moduleRarity = moduleRarity,
            coreId = coreId,
            database = database,
            moduleConfig = ResolveModuleConfig(database, moduleId, moduleType),
            coreConfig = ResolveCoreConfig(database, coreId)
        };

        if (runtimeData.moduleConfig != null)
            runtimeData.moduleType = runtimeData.moduleConfig.moduleType;

        runtimeData.statGraph = new LoadoutStatGraph(
            runtimeData.moduleConfig,
            runtimeData.moduleRarity,
            runtimeData.coreConfig);

        if (database?.allPlugins != null && plugins != null)
        {
            foreach (var plugin in plugins)
            {
                foreach (var config in database.allPlugins)
                {
                    if (config.pluginId != plugin.pluginId)
                        continue;

                    runtimeData.pluginRuntimes.Add(new LoadoutPluginRuntimeData
                    {
                        pluginId = plugin.pluginId,
                        rarity = plugin.rarity,
                        pluginConfig = config,
                        effectParams = config.GetEffectParams(plugin.rarity)
                    });
                    break;
                }
            }
        }

        return runtimeData;
    }
}
