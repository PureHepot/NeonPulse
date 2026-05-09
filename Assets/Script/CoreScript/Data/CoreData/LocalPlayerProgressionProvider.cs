using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Local loadout provider backed by DataManager save data.
// Uses Meta loadout while in assembly/menu flow.
// Uses Run.loadout while in active gameplay flow.
// ============================================================

public class LocalLoadoutProvider : ILoadoutDataProvider
{
    public event Action<string> OnSlotChanged;
    public event Action<string> OnFrameChanged;

    private readonly Dictionary<string, FrameConfig> frameConfigMap = new();
    private readonly Dictionary<string, ModuleConfig> moduleConfigByIdMap = new();
    private readonly Dictionary<ModuleType, ModuleConfig> moduleConfigByTypeMap = new();
    private readonly Dictionary<string, CoreConfig> coreConfigMap = new();
    private readonly Dictionary<string, PluginConfig> pluginConfigMap = new();

    private RunLoadoutData RunLoadout => DataManager.Instance.Run?.loadout;
    private MetaProgressData Meta => DataManager.Instance.Meta;
    private GameConfigDatabase Database => GameConfigDatabase.Instance;
    private bool UseRunLoadout => DataManager.Instance.HasActiveRun && RunLoadout != null;

    public LocalLoadoutProvider(
        List<FrameConfig> frames,
        List<ModuleConfig> modules,
        List<CoreConfig> cores,
        List<PluginConfig> plugins)
    {
        if (frames != null)
        {
            foreach (var frame in frames)
                frameConfigMap[frame.frameId] = frame;
        }

        if (modules != null)
        {
            foreach (var module in modules)
            {
                moduleConfigByIdMap[module.ModuleId] = module;
                if (!moduleConfigByTypeMap.ContainsKey(module.moduleType))
                    moduleConfigByTypeMap[module.moduleType] = module;
            }
        }

        if (cores != null)
        {
            foreach (var core in cores)
                coreConfigMap[core.coreId] = core;
        }

        if (plugins != null)
        {
            foreach (var plugin in plugins)
                pluginConfigMap[plugin.pluginId] = plugin;
        }
    }

    public string FrameId
    {
        get
        {
            if (UseRunLoadout)
                return RunLoadout?.frameId ?? string.Empty;

            return Meta?.GetSelectedFrameId() ?? string.Empty;
        }
    }

    public ModuleType GetSlotModuleType(string slotId)
    {
        var runtimeSlotData = FindRuntimeSlotData(slotId);
        if (runtimeSlotData != null)
            return ResolveSelectedModuleConfig(runtimeSlotData)?.moduleType ?? runtimeSlotData.moduleType;

        var metaSlotData = FindMetaSlotData(slotId);
        return ResolveSelectedModuleConfig(metaSlotData)?.moduleType ?? metaSlotData?.moduleType ?? ModuleType.None;
    }

    public float GetFinalStat(string statId)
    {
        float total = 0f;
        if (GetFrameConfig(FrameId) == null)
            return total;

        if (UseRunLoadout)
        {
            if (RunLoadout?.slots == null)
                return total;

            foreach (var slotData in RunLoadout.slots)
            {
                if (!HasModuleSelection(slotData))
                    continue;

                total += GetSlotFinalStat(slotData, statId);
            }

            return total;
        }

        var metaLoadout = GetCurrentMetaLoadout(false);
        if (metaLoadout?.slots == null)
            return total;

        foreach (var slotData in metaLoadout.slots)
        {
            if (!HasModuleSelection(slotData))
                continue;

            total += GetSlotFinalStat(slotData, statId);
        }

        return total;
    }

    public string[] GetActivePluginEffectIds(string slotId)
    {
        var result = new List<string>();

        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            CollectPluginEffectIds(slotData?.plugins, result);
            return result.ToArray();
        }

        var metaSlotData = FindMetaSlotData(slotId);
        CollectPluginEffectIds(metaSlotData?.plugins, result);
        return result.ToArray();
    }

    public FrameInherentEffect[] GetFrameInherentEffects()
    {
        var frameConfig = GetFrameConfig(FrameId);
        if (frameConfig == null)
            return Array.Empty<FrameInherentEffect>();

        return frameConfig.inherentEffects.ToArray();
    }

    public bool IsSlotOccupied(string slotId)
    {
        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            return HasModuleSelection(slotData);
        }

        var metaSlotData = FindMetaSlotData(slotId);
        return HasModuleSelection(metaSlotData);
    }

    public bool SelectFrame(string frameId)
    {
        if (GetFrameConfig(frameId) == null)
            return false;

        if (UseRunLoadout)
        {
            if (RunLoadout == null)
                return false;

            RunLoadout.frameId = frameId;
            RunLoadout.slots.Clear();
            OnFrameChanged?.Invoke(frameId);
            return true;
        }

        Meta.SetSelectedFrame(frameId);
        OnFrameChanged?.Invoke(frameId);
        return true;
    }

    public bool EquipModule(string slotId, ModuleType moduleType)
    {
        var moduleConfig = GetModuleConfig(moduleType);
        return EquipModule(slotId, moduleConfig);
    }

    public bool EquipModule(string slotId, string moduleId)
    {
        var moduleConfig = GetModuleConfig(moduleId);
        return EquipModule(slotId, moduleConfig);
    }

    private bool EquipModule(string slotId, ModuleConfig moduleConfig)
    {
        if (moduleConfig == null)
            return false;

        if (UseRunLoadout)
        {
            var slotData = GetOrCreateRuntimeSlotData(slotId);
            if (slotData == null)
                return false;

            ApplyModuleSelection(slotData, moduleConfig);
            OnSlotChanged?.Invoke(slotId);
            return true;
        }

        var metaSlotData = GetOrCreateMetaSlotData(slotId);
        if (metaSlotData == null)
            return false;

        ApplyModuleSelection(metaSlotData, moduleConfig);
        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    public bool UnequipModule(string slotId)
    {
        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            if (slotData == null || slotData.moduleType == ModuleType.None)
                return false;

            ClearModuleSelection(slotData);
            OnSlotChanged?.Invoke(slotId);
            return true;
        }

        var metaSlotData = FindMetaSlotData(slotId);
        if (metaSlotData == null || metaSlotData.moduleType == ModuleType.None)
            return false;

        ClearModuleSelection(metaSlotData);
        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    public bool InsertCore(string slotId, string coreId)
    {
        var coreConfig = GetCoreConfig(coreId);
        if (coreConfig == null)
            return false;

        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            var moduleConfig = ResolveSelectedModuleConfig(slotData);
            if (slotData == null || moduleConfig == null)
                return false;

            if (!coreConfig.CanInsertInto(moduleConfig))
            {
                Debug.LogWarning($"[Loadout] Core {coreId} cannot be inserted into {moduleConfig.ModuleId}");
                return false;
            }

            slotData.coreId = coreId;
            OnSlotChanged?.Invoke(slotId);
            return true;
        }

        var metaSlotData = FindMetaSlotData(slotId);
        var metaModuleConfig = ResolveSelectedModuleConfig(metaSlotData);
        if (metaSlotData == null || metaModuleConfig == null)
            return false;

        if (!coreConfig.CanInsertInto(metaModuleConfig))
        {
            Debug.LogWarning($"[Loadout] Core {coreId} cannot be inserted into {metaModuleConfig.ModuleId}");
            return false;
        }

        metaSlotData.coreId = coreId;
        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    public bool RemoveCore(string slotId)
    {
        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            if (slotData == null || string.IsNullOrEmpty(slotData.coreId))
                return false;

            slotData.coreId = null;
            OnSlotChanged?.Invoke(slotId);
            return true;
        }

        var metaSlotData = FindMetaSlotData(slotId);
        if (metaSlotData == null || string.IsNullOrEmpty(metaSlotData.coreId))
            return false;

        metaSlotData.coreId = null;
        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    public bool InsertPlugin(string slotId, string pluginId, PluginRarity rarity)
    {
        var pluginConfig = GetPluginConfig(pluginId);
        if (pluginConfig == null)
            return false;

        if (UseRunLoadout)
            return InsertPluginToRuntimeSlot(slotId, pluginConfig, rarity);

        return InsertPluginToMetaSlot(slotId, pluginConfig, rarity);
    }

    public bool RemovePlugin(string slotId, int pluginIndex)
    {
        if (UseRunLoadout)
        {
            var slotData = FindRuntimeSlotData(slotId);
            if (slotData == null || pluginIndex < 0 || pluginIndex >= slotData.plugins.Count)
                return false;

            slotData.plugins.RemoveAt(pluginIndex);
            OnSlotChanged?.Invoke(slotId);
            return true;
        }

        var metaSlotData = FindMetaSlotData(slotId);
        if (metaSlotData == null || pluginIndex < 0 || pluginIndex >= metaSlotData.plugins.Count)
            return false;

        metaSlotData.plugins.RemoveAt(pluginIndex);
        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    public void ClearLoadout()
    {
        if (UseRunLoadout)
        {
            if (RunLoadout == null)
                return;

            RunLoadout.frameId = null;
            RunLoadout.slots.Clear();
            OnFrameChanged?.Invoke(string.Empty);
            return;
        }

        var metaLoadout = GetCurrentMetaLoadout(false);
        if (metaLoadout == null)
            return;

        metaLoadout.slots.Clear();
        OnFrameChanged?.Invoke(FrameId);
    }

    private bool InsertPluginToRuntimeSlot(string slotId, PluginConfig pluginConfig, PluginRarity rarity)
    {
        var slotData = FindRuntimeSlotData(slotId);
        var moduleConfig = ResolveSelectedModuleConfig(slotData);
        if (slotData == null || moduleConfig == null)
            return false;

        if (!pluginConfig.CanInsertInto(moduleConfig))
        {
            Debug.LogWarning($"[Loadout] Plugin {pluginConfig.pluginId} cannot be inserted into {moduleConfig.ModuleId}");
            return false;
        }

        var runtimeData = LoadoutModuleRuntimeBuilder.Build(slotData, Database);
        int maxSlots = runtimeData != null ? runtimeData.GetPluginCapacity() : 0;
        if (slotData.plugins.Count >= maxSlots)
        {
            Debug.LogWarning($"[Loadout] Plugin slots are full for {moduleConfig.ModuleId} ({maxSlots})");
            return false;
        }

        slotData.plugins.Add(new PluginInstanceSaveData
        {
            pluginId = pluginConfig.pluginId,
            rarity = rarity
        });

        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    private bool InsertPluginToMetaSlot(string slotId, PluginConfig pluginConfig, PluginRarity rarity)
    {
        var slotData = FindMetaSlotData(slotId);
        var moduleConfig = ResolveSelectedModuleConfig(slotData);
        if (slotData == null || moduleConfig == null)
            return false;

        if (!pluginConfig.CanInsertInto(moduleConfig))
        {
            Debug.LogWarning($"[Loadout] Plugin {pluginConfig.pluginId} cannot be inserted into {moduleConfig.ModuleId}");
            return false;
        }

        var runtimeData = LoadoutModuleRuntimeBuilder.Build(slotData, Database);
        int maxSlots = runtimeData != null ? runtimeData.GetPluginCapacity() : 0;
        if (slotData.plugins.Count >= maxSlots)
        {
            Debug.LogWarning($"[Loadout] Plugin slots are full for {moduleConfig.ModuleId} ({maxSlots})");
            return false;
        }

        slotData.plugins.Add(new PluginInstanceSaveData
        {
            pluginId = pluginConfig.pluginId,
            rarity = rarity
        });

        OnSlotChanged?.Invoke(slotId);
        return true;
    }

    private void CollectPluginEffectIds(List<PluginInstanceSaveData> plugins, List<string> result)
    {
        if (plugins == null)
            return;

        foreach (var plugin in plugins)
        {
            var config = GetPluginConfig(plugin.pluginId);
            if (config != null && !string.IsNullOrEmpty(config.effectId))
                result.Add(config.effectId);
        }
    }

    private float GetSlotFinalStat(SlotRuntimeSaveData slotData, string statId)
    {
        var runtimeData = LoadoutModuleRuntimeBuilder.Build(slotData, Database);
        if (runtimeData == null || !runtimeData.HasModule)
            return 0f;

        return runtimeData.GetFinalStat(statId);
    }

    private float GetSlotFinalStat(SlotSaveData slotData, string statId)
    {
        var runtimeData = LoadoutModuleRuntimeBuilder.Build(slotData, Database);
        if (runtimeData == null || !runtimeData.HasModule)
            return 0f;

        return runtimeData.GetFinalStat(statId);
    }

    private SlotRuntimeSaveData FindRuntimeSlotData(string slotId)
    {
        if (!UseRunLoadout || string.IsNullOrEmpty(slotId) || RunLoadout?.slots == null)
            return null;

        foreach (var slot in RunLoadout.slots)
        {
            if (slot.slotId == slotId)
                return slot;
        }

        return null;
    }

    private SlotRuntimeSaveData GetOrCreateRuntimeSlotData(string slotId)
    {
        var slotData = FindRuntimeSlotData(slotId);
        if (slotData != null)
            return slotData;

        if (!UseRunLoadout || string.IsNullOrEmpty(slotId) || RunLoadout == null)
            return null;

        slotData = new SlotRuntimeSaveData { slotId = slotId };
        RunLoadout.slots.Add(slotData);
        return slotData;
    }

    private FrameLoadoutSaveData GetCurrentMetaLoadout(bool createIfMissing)
    {
        if (Meta == null)
            return null;

        var frameId = FrameId;
        if (string.IsNullOrEmpty(frameId))
            return null;

        return createIfMissing
            ? Meta.GetOrInitFrameLoadout(frameId)
            : Meta.frameLoadouts.Find(loadout => loadout.frameId == frameId);
    }

    private SlotSaveData FindMetaSlotData(string slotId)
    {
        var loadout = GetCurrentMetaLoadout(false);
        if (loadout?.slots == null || string.IsNullOrEmpty(slotId))
            return null;

        foreach (var slot in loadout.slots)
        {
            if (slot.slotId == slotId)
                return slot;
        }

        return null;
    }

    private SlotSaveData GetOrCreateMetaSlotData(string slotId)
    {
        var slotData = FindMetaSlotData(slotId);
        if (slotData != null)
            return slotData;

        var loadout = GetCurrentMetaLoadout(true);
        if (loadout == null || string.IsNullOrEmpty(slotId))
            return null;

        slotData = new SlotSaveData { slotId = slotId };
        loadout.slots.Add(slotData);
        return slotData;
    }

    private static void ApplyModuleSelection(SlotRuntimeSaveData slotData, ModuleConfig moduleConfig)
    {
        slotData.moduleId = moduleConfig.ModuleId;
        slotData.moduleType = moduleConfig.moduleType;
        slotData.moduleRarity = moduleConfig.defaultRarity;
        slotData.coreId = null;
        slotData.plugins.Clear();
    }

    private static void ApplyModuleSelection(SlotSaveData slotData, ModuleConfig moduleConfig)
    {
        slotData.moduleId = moduleConfig.ModuleId;
        slotData.moduleType = moduleConfig.moduleType;
        slotData.moduleRarity = moduleConfig.defaultRarity;
        slotData.coreId = null;
        slotData.plugins.Clear();
    }

    private static void ClearModuleSelection(SlotRuntimeSaveData slotData)
    {
        slotData.moduleId = null;
        slotData.moduleType = ModuleType.None;
        slotData.moduleRarity = ModuleRarity.Common;
        slotData.coreId = null;
        slotData.plugins.Clear();
    }

    private static void ClearModuleSelection(SlotSaveData slotData)
    {
        slotData.moduleId = null;
        slotData.moduleType = ModuleType.None;
        slotData.moduleRarity = ModuleRarity.Common;
        slotData.coreId = null;
        slotData.plugins.Clear();
    }

    private FrameConfig GetFrameConfig(string frameId)
    {
        if (string.IsNullOrEmpty(frameId))
            return null;

        frameConfigMap.TryGetValue(frameId, out var config);
        return config;
    }

    private ModuleConfig GetModuleConfig(ModuleType type)
    {
        moduleConfigByTypeMap.TryGetValue(type, out var config);
        return config;
    }

    private ModuleConfig GetModuleConfig(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return null;

        moduleConfigByIdMap.TryGetValue(moduleId, out var config);
        return config;
    }

    private CoreConfig GetCoreConfig(string coreId)
    {
        if (string.IsNullOrEmpty(coreId))
            return null;

        coreConfigMap.TryGetValue(coreId, out var config);
        return config;
    }

    private PluginConfig GetPluginConfig(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId))
            return null;

        pluginConfigMap.TryGetValue(pluginId, out var config);
        return config;
    }

    private ModuleConfig ResolveSelectedModuleConfig(SlotRuntimeSaveData slotData)
    {
        if (slotData == null)
            return null;

        var config = GetModuleConfig(slotData.moduleId);
        return config ?? GetModuleConfig(slotData.moduleType);
    }

    private ModuleConfig ResolveSelectedModuleConfig(SlotSaveData slotData)
    {
        if (slotData == null)
            return null;

        var config = GetModuleConfig(slotData.moduleId);
        return config ?? GetModuleConfig(slotData.moduleType);
    }

    private bool HasModuleSelection(SlotRuntimeSaveData slotData)
    {
        return ResolveSelectedModuleConfig(slotData) != null;
    }

    private bool HasModuleSelection(SlotSaveData slotData)
    {
        return ResolveSelectedModuleConfig(slotData) != null;
    }
}
