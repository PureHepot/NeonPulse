using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerModulePrefabResolver
{
    private static readonly Dictionary<string, string> FixedResourcePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["base_health_module"] = "Prefabs/Module/Health",
        ["defense_module_base"] = "Prefabs/Module/Defence_Base",
        ["defense_module_carapace"] = "Prefabs/Module/Defence_Carapace",
        ["defense_module_energy"] = "Prefabs/Module/Defence_Energy",
        ["defense_module_light"] = "Prefabs/Module/Defence_Light",
        ["defense_module_spike"] = "Prefabs/Module/Defence_Spike"
    };

    public static GameObject Resolve(LoadoutModuleRuntimeData runtimeData)
    {
        var moduleConfig = runtimeData != null ? runtimeData.moduleConfig : null;
        if (moduleConfig == null)
            return null;

        var prefab = moduleConfig.GetRuntimePrefab();
        if (prefab != null)
            return prefab;

        if (FixedResourcePaths.TryGetValue(moduleConfig.ModuleId, out var resourcePath))
            return Resources.Load<GameObject>(resourcePath);

        return null;
    }
}
