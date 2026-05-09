using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 装配管理器：统筹框架选择、模块装备、核心/配件插入
// 对外暴露 ILoadoutDataProvider，对内操作 DataManager 存档
// 战斗系统和 UI 只依赖 ILoadoutReader，不直接访问此 Manager
// ============================================================

public class LoadoutManager : MonoSingleton<LoadoutManager>
{
    // ==================== 数据提供者 ====================

    private ILoadoutDataProvider provider;

    /// <summary>
    /// 只读接口，供战斗系统、UI 使用
    /// </summary>
    public ILoadoutReader Reader => provider;

    /// <summary>
    /// 完整接口（仅本地授权系统使用）
    /// </summary>
    public ILoadoutDataProvider Provider => provider;

    // ==================== 配置缓存 ====================

    private Dictionary<string, FrameConfig> frameConfigs = new Dictionary<string, FrameConfig>();
    private Dictionary<ModuleType, ModuleConfig> moduleConfigs = new Dictionary<ModuleType, ModuleConfig>();
    private Dictionary<string, ModuleConfig> moduleConfigsById = new Dictionary<string, ModuleConfig>();
    private Dictionary<string, CoreConfig> coreConfigs = new Dictionary<string, CoreConfig>();
    private Dictionary<string, PluginConfig> pluginConfigs = new Dictionary<string, PluginConfig>();

    // ==================== 事件转发 ====================

    public event Action<string> OnSlotChanged
    {
        add { if (provider != null) provider.OnSlotChanged += value; }
        remove { if (provider != null) provider.OnSlotChanged -= value; }
    }

    public event Action<string> OnFrameChanged
    {
        add { if (provider != null) provider.OnFrameChanged += value; }
        remove { if (provider != null) provider.OnFrameChanged -= value; }
    }

    // ==================== 初始化 ====================

    private void Awake()
    {
        BuildConfigCaches();

        // 创建本地提供者
        var db = GameConfigDatabase.Instance;
        if (db != null)
        {
            provider = new LocalLoadoutProvider(
                db.allFrames,
                db.allModules,
                db.allCores,
                db.allPlugins
            );
        }
    }

    /// <summary>
    /// 从 GameConfigDatabase 缓存所有配置
    /// </summary>
    private void BuildConfigCaches()
    {
        var db = GameConfigDatabase.Instance;
        if (db == null)
        {
            Debug.LogError("[LoadoutManager] GameConfigDatabase 未找到！请确保 Resources/Configs 下存在配置数据库。");
            return;
        }

        frameConfigs.Clear();
        if (db.allFrames != null)
            foreach (var f in db.allFrames)
                frameConfigs[f.frameId] = f;

        moduleConfigs.Clear();
        moduleConfigsById.Clear();
        if (db.allModules != null)
            foreach (var m in db.allModules)
            {
                if (!moduleConfigs.ContainsKey(m.moduleType))
                    moduleConfigs[m.moduleType] = m;
                moduleConfigsById[m.ModuleId] = m;
            }

        coreConfigs.Clear();
        if (db.allCores != null)
            foreach (var c in db.allCores)
                coreConfigs[c.coreId] = c;

        pluginConfigs.Clear();
        if (db.allPlugins != null)
            foreach (var p in db.allPlugins)
                pluginConfigs[p.pluginId] = p;

        Debug.Log($"[LoadoutManager] 配置缓存完成: " +
                  $"{frameConfigs.Count} 框架, {moduleConfigs.Count} 模块, " +
                  $"{coreConfigs.Count} 核心, {pluginConfigs.Count} 配件");
    }

    // ============================================================
    // 框架操作
    // ============================================================

    /// <summary>
    /// 选择框架并开始新局装配
    /// </summary>
    public bool SelectFrame(string frameId)
    {
        if (provider == null) return false;
        return provider.SelectFrame(frameId);
    }

    /// <summary>
    /// 获取当前框架配置
    /// </summary>
    public FrameConfig GetCurrentFrame()
    {
        string id = provider?.FrameId;
        if (string.IsNullOrEmpty(id)) return null;
        frameConfigs.TryGetValue(id, out var config);
        return config;
    }

    /// <summary>
    /// 获取所有框架配置
    /// </summary>
    public IEnumerable<FrameConfig> GetAllFrames() => frameConfigs.Values;

    // ============================================================
    // 模块操作
    // ============================================================

    /// <summary>
    /// 将模块装备到指定插槽
    /// </summary>
    public bool EquipModule(string slotId, ModuleType moduleType)
    {
        if (provider == null) return false;
        bool success = provider.EquipModule(slotId, moduleType);
        if (success) DataManager.Instance.Save();
        return success;
    }

    public bool EquipModule(string slotId, string moduleId)
    {
        if (provider == null) return false;
        bool success = provider.EquipModule(slotId, moduleId);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 从指定插槽卸下模块
    /// </summary>
    public bool UnequipModule(string slotId)
    {
        if (provider == null) return false;
        bool success = provider.UnequipModule(slotId);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 获取指定模块的配置
    /// </summary>
    public ModuleConfig GetModuleConfig(ModuleType type)
    {
        moduleConfigs.TryGetValue(type, out var config);
        return config;
    }

    public ModuleConfig GetModuleConfig(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return null;

        moduleConfigsById.TryGetValue(moduleId, out var config);
        return config;
    }

    /// <summary>
    /// 获取所有模块配置
    /// </summary>
    public IEnumerable<ModuleConfig> GetAllModules() => moduleConfigsById.Values;

    public LoadoutModuleRuntimeData GetEquippedModuleRuntime(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
            return null;

        var database = GameConfigDatabase.Instance;
        if (database == null)
            return null;

        if (DataManager.Instance.HasActiveRun)
        {
            var loadout = DataManager.Instance.CurrentLoadout;
            if (loadout?.slots == null)
                return null;

            foreach (var slot in loadout.slots)
            {
                if (slot.slotId == slotId)
                    return LoadoutModuleRuntimeBuilder.Build(slot, database);
            }

            return null;
        }

        var frameId = DataManager.Instance.Meta.GetSelectedFrameId();
        if (string.IsNullOrEmpty(frameId))
            return null;

        var loadoutMeta = DataManager.Instance.Meta.frameLoadouts.Find(loadout => loadout.frameId == frameId);
        if (loadoutMeta?.slots == null)
            return null;

        foreach (var slot in loadoutMeta.slots)
        {
            if (slot.slotId == slotId)
                return LoadoutModuleRuntimeBuilder.Build(slot, database);
        }

        return null;
    }

    public AssemblyLoadoutSnapshot BuildCurrentAssemblySnapshot()
    {
        var snapshot = new AssemblyLoadoutSnapshot();
        var database = GameConfigDatabase.Instance;
        if (database == null)
            return snapshot;

        if (DataManager.Instance.HasActiveRun)
        {
            var loadout = DataManager.Instance.CurrentLoadout;
            snapshot.frameId = loadout?.frameId ?? string.Empty;

            if (loadout?.slots == null)
                return snapshot;

            foreach (var slot in loadout.slots)
            {
                var runtimeData = LoadoutModuleRuntimeBuilder.Build(slot, database);
                if (runtimeData == null || !runtimeData.HasModule)
                    continue;

                snapshot.slots.Add(new AssemblyLoadoutSlotSnapshot
                {
                    slotId = slot.slotId,
                    moduleId = slot.moduleId,
                    moduleType = slot.moduleType,
                    moduleRarity = slot.moduleRarity,
                    coreId = slot.coreId,
                    runtimeData = runtimeData
                });
            }

            return snapshot;
        }

        string frameId = DataManager.Instance.Meta.GetSelectedFrameId();
        snapshot.frameId = frameId ?? string.Empty;
        if (string.IsNullOrEmpty(frameId))
            return snapshot;

        var metaLoadout = DataManager.Instance.Meta.frameLoadouts.Find(loadout => loadout.frameId == frameId);
        if (metaLoadout?.slots == null)
            return snapshot;

        foreach (var slot in metaLoadout.slots)
        {
            var runtimeData = LoadoutModuleRuntimeBuilder.Build(slot, database);
            if (runtimeData == null || !runtimeData.HasModule)
                continue;

            snapshot.slots.Add(new AssemblyLoadoutSlotSnapshot
            {
                slotId = slot.slotId,
                moduleId = slot.moduleId,
                moduleType = slot.moduleType,
                moduleRarity = slot.moduleRarity,
                coreId = slot.coreId,
                runtimeData = runtimeData
            });
        }

        return snapshot;
    }

    // ============================================================
    // 核心操作
    // ============================================================

    /// <summary>
    /// 为指定插槽的模块插入核心（替换已有）
    /// </summary>
    public bool InsertCore(string slotId, string coreId)
    {
        if (provider == null) return false;
        bool success = provider.InsertCore(slotId, coreId);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 移除指定插槽模块的核心
    /// </summary>
    public bool RemoveCore(string slotId)
    {
        if (provider == null) return false;
        bool success = provider.RemoveCore(slotId);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 获取指定核心的配置
    /// </summary>
    public CoreConfig GetCoreConfig(string coreId)
    {
        if (string.IsNullOrEmpty(coreId)) return null;
        coreConfigs.TryGetValue(coreId, out var config);
        return config;
    }

    /// <summary>
    /// 获取所有核心配置
    /// </summary>
    public IEnumerable<CoreConfig> GetAllCores() => coreConfigs.Values;

    // ============================================================
    // 配件操作
    // ============================================================

    /// <summary>
    /// 为指定插槽的模块插入配件
    /// </summary>
    public bool InsertPlugin(string slotId, string pluginId, PluginRarity rarity)
    {
        if (provider == null) return false;
        bool success = provider.InsertPlugin(slotId, pluginId, rarity);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 移除指定插槽模块的指定配件
    /// </summary>
    public bool RemovePlugin(string slotId, int pluginIndex)
    {
        if (provider == null) return false;
        bool success = provider.RemovePlugin(slotId, pluginIndex);
        if (success) DataManager.Instance.Save();
        return success;
    }

    /// <summary>
    /// 获取指定配件的配置
    /// </summary>
    public PluginConfig GetPluginConfig(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId)) return null;
        pluginConfigs.TryGetValue(pluginId, out var config);
        return config;
    }

    /// <summary>
    /// 获取所有配件配置
    /// </summary>
    public IEnumerable<PluginConfig> GetAllPlugins() => pluginConfigs.Values;

    // ============================================================
    // 数值查询（便捷方法，委托给 Provider）
    // ============================================================

    /// <summary>
    /// 获取指定属性的最终值（模块基础 + 核心 + 插槽修正，全部叠加）
    /// </summary>
    public float GetFinalStat(string statId)
    {
        return provider?.GetFinalStat(statId) ?? 0f;
    }

    /// <summary>
    /// 清空当前装配
    /// </summary>
    public void ClearLoadout()
    {
        provider?.ClearLoadout();
        DataManager.Instance.Save();
    }

    // ============================================================
    // 从存档恢复（继续游戏时调用）
    // ============================================================

    /// <summary>
    /// 从 DataManager 的 Run.loadout 恢复运行时状态
    /// </summary>
    public void RestoreFromSave()
    {
        var loadout = DataManager.Instance.CurrentLoadout;
        if (loadout == null || string.IsNullOrEmpty(loadout.frameId))
        {
            Debug.LogWarning("[LoadoutManager] 存档中无装配数据，跳过恢复");
            return;
        }

        // LocalLoadoutProvider 直接读取 DataManager 的存档数据
        // 无需额外恢复操作，只需确认 provider 能正确读取
        Debug.Log($"[LoadoutManager] 从存档恢复装配: 框架={loadout.frameId}, 插槽={loadout.slots.Count}");
    }
}
