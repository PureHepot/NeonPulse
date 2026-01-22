using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UpgradeOption
{
    public ModuleType moduleType;
    public StatType statType; 
}

public class UpgradeManager : MonoSingleton<UpgradeManager>
{
    [Header("Database")]
    public List<ModuleConfig> allModuleConfigs;

    private Dictionary<ModuleType, ModuleRuntimeData> activeModules =
        new Dictionary<ModuleType, ModuleRuntimeData>();

    [Header("Level Settings")]
    public int baseExpToLevelUp = 100;
    public float expScale = 1.5f;

    private HashSet<ModuleType> forcedHistory = new();

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; } = 0;

    public Action<int, int, int> OnExpChanged;
    public Action<int, List<UpgradeOption>> OnLevelUp;

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 同步PlayerManager中的已解锁模块
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

    public void AddExperience(int amount)
    {
        CurrentExp += amount;

        int expToLevelUp = GetExpToLevelUp();
        OnExpChanged?.Invoke(CurrentExp, expToLevelUp, CurrentLevel);

        while (CurrentExp >= expToLevelUp)
        {
            CurrentExp -= expToLevelUp;
            CurrentLevel++;

            expToLevelUp = GetExpToLevelUp();

            var options = GetUpgradeOptions(3);
            OnLevelUp?.Invoke(CurrentLevel, options);

            OnExpChanged?.Invoke(CurrentExp, expToLevelUp, CurrentLevel);
        }
    }

    private int GetExpToLevelUp()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expScale, CurrentLevel - 1));
    }

    /// <summary>
    /// 解锁或升级模块统一入口
    /// </summary>
    public void UnlockOrUpgradeModule(ModuleType type)
    {
        Debug.Log($"[Upgrade] 请求升级模块: {type}");

        if (!IsModuleUnlocked(type))
        {
            Debug.Log($"[Upgrade] 解锁模块: {type}");
            UnlockModule(type);
        }
        else
        {
            ModuleConfig config = GetConfig(type);
            if (config != null && config.statUpgrades.Count > 0)
            {
                Debug.Log($"[Upgrade] 升级模块 {type}, 属性: {config.statUpgrades[0].statType}");
                UpgradeModuleStat(type, config.statUpgrades[0].statType);
            }
            else
            {
                Debug.LogWarning($"模块{type}无可用升级属性");
            }
        }
    }


    /// <summary>
    /// 解锁模块统一入口
    /// </summary>
    public void UnlockModule(ModuleType type)
    {
        ModuleConfig config = GetConfig(type);
        if (config == null) return;

        if (activeModules.ContainsKey(type))
            return;

        PlayerManager.Instance.UnlockModuleData(type);
        InitializeRuntimeData(config);
    }

    /// <summary>
    /// 升级模块属性
    /// </summary>
    public void UpgradeModuleStat(ModuleType moduleType, StatType statType)
    {
        Debug.Log($"[Upgrade] 应用升级: {moduleType} -> {statType}");

        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            data.AddStatUpgrade(statType);

            float value = data.GetCurrentStat(statType);
            Debug.Log($"[Upgrade] 当前 {statType} 数值: {value}");

            EventManager.Broadcast<ModuleType, StatType>(GameEvent.ModuleUpgrade, moduleType, statType);
        }
    }

    private void InitializeRuntimeData(ModuleConfig config)
    {
        if (!activeModules.ContainsKey(config.moduleType))
        {
            activeModules.Add(
                config.moduleType,
                new ModuleRuntimeData(config)
            );

            Debug.Log($"[UpgradeManager]初始化模块: {config.moduleName}");
        }
    }

    public float GetStat(ModuleType moduleType, StatType statType, float defaultValue = 0f)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            return data.GetCurrentStat(statType);
        }
        return defaultValue;
    }

    public bool IsModuleUnlocked(ModuleType type)
    {
        return activeModules.ContainsKey(type);
    }

    // 升级获取模块逻辑：优先当前等级解锁模块，其余都放入普通池
    public List<UpgradeOption> GetUpgradeOptions(int count)
    {
        List<UpgradeOption> forcePool = new();
        List<UpgradeOption> normalPool = new();

        foreach (var config in allModuleConfigs)
        {
            bool unlocked = IsModuleUnlocked(config.moduleType);

            // 未达到解锁等级->完全不加入池
            if (CurrentLevel < config.unlockLevel)
                continue;

            // 到达该模块解锁等级->必出一次
            if (!unlocked && config.unlockLevel == CurrentLevel)
            {
                forcePool.Add(new UpgradeOption
                {
                    moduleType = config.moduleType,
                    statType = StatType.None
                });
                continue;
            }

            // 达到解锁等级但未解锁->普通随机池
            if (!unlocked)
            {
                normalPool.Add(new UpgradeOption
                {
                    moduleType = config.moduleType,
                    statType = StatType.None
                });
            }
            else
            {
                // 已解锁 → 随机属性升级
                foreach (var stat in config.statUpgrades)
                {
                    normalPool.Add(new UpgradeOption
                    {
                        moduleType = config.moduleType,
                        statType = stat.statType
                    });
                }
            }
        }

        Shuffle(normalPool);

        List<UpgradeOption> result = new();

        // 先放强制池
        result.AddRange(forcePool);

        // 再补随机池
        foreach (var opt in normalPool)
        {
            if (result.Count >= count) break;
            if (!result.Exists(o => o.moduleType == opt.moduleType && o.statType == opt.statType))
                result.Add(opt);
        }

        return result;
    }


    public ModuleConfig GetConfig(ModuleType type)
    {
        foreach (var config in allModuleConfigs)
        {
            if (config.moduleType == type)
                return config;
        }

        Debug.LogError($"找不到ModuleConfig: {type}");
        return null;
    }

    private void OnDestroy()
    {
        EventManager.Clear();
    }
}