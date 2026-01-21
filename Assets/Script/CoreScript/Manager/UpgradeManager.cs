using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoSingleton<UpgradeManager>
{
    [Header("Database")]
    public List<ModuleConfig> allModuleConfigs;

    private Dictionary<ModuleType, ModuleRuntimeData> activeModules = new Dictionary<ModuleType, ModuleRuntimeData>();

    /// <summary>
    /// 同步PlayerManager中的解锁数据到UpgradeManager
    /// </summary>
    public void SyncWithPlayerManager()
    {
        foreach (var config in allModuleConfigs)
        {
            if (PlayerManager.Instance.IsModuleUnlocked(config.moduleType))
            {
                InitializeRuntimeData(config);
            }
        }
    }

    /// <summary>
    /// 解锁模块的统一入口
    /// </summary>
    public void UnlockModule(ModuleType type)
    {
        ModuleConfig config = GetConfig(type);
        if (config == null) return;

        //已经解锁并初始化过直接返回
        if (activeModules.ContainsKey(type))
        {
            return;
        }

        PlayerManager.Instance.UnlockModuleData(type);

        InitializeRuntimeData(config);
    }

    //初始化数据
    private void InitializeRuntimeData(ModuleConfig config)
    {
        if (!activeModules.ContainsKey(config.moduleType))
        {
            ModuleRuntimeData newData = new ModuleRuntimeData(config);
            activeModules.Add(config.moduleType, newData);
            Debug.Log($"[UpgradeManager] 初始化数据: {config.moduleName}");
        }
    }

    // --- 数据查询接口 ---

    //获取模块某个属性的当前值
    public float GetStat(ModuleType moduleType, StatType statType, float defaultValue = 0f)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            return data.GetCurrentStat(statType);
        }
        return defaultValue;
    }

    public void UpgradeModuleStat(ModuleType moduleType, StatType statType)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            data.AddStatUpgrade(statType);

            EventManager.Broadcast(GameEvent.ModuleUpgrade, moduleType, statType);
        }
        else
        {
            Debug.LogWarning($"试图升级未解锁的模块: {moduleType}");
        }
    }

    // --- 辅助方法 ---

    public bool IsModuleUnlocked(ModuleType type)
    {
        return activeModules.ContainsKey(type);
    }

    private ModuleConfig GetConfig(ModuleType type)
    {
        foreach (var config in allModuleConfigs)
        {
            if (config.moduleType == type) return config;
        }
        Debug.LogError($"配置表中找不到类型为 {type} 的 ModuleConfig，请检查 UpgradeManager!");
        return null;
    }
}
