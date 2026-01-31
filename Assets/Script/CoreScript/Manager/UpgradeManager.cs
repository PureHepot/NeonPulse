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

    [Header("Level Settings")]
    public int baseExpToLevelUp = 100;
    public float expScale = 1.5f;
    public int pointsPerLevel = 2;

    public int UpgradePoints { get; private set; } = 0;

    private HashSet<ModuleType> unlockedModuleTypes = new HashSet<ModuleType>();
    public HashSet<ModuleType> UnlockedModuleTypes => unlockedModuleTypes;

    private Dictionary<ModuleType, ModuleRuntimeData> activeModules =
        new Dictionary<ModuleType, ModuleRuntimeData>();

    [Header("Default Loadout")]
    public List<ModuleType> startingModules;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; } = 0;

    public Action<int, int, int> OnExpChanged;
    public Action<int> OnUpgradePointsChanged;

    // 本轮升级面板排重池（核心）
    private HashSet<string> roundExclude = new();

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 每次打开升级面板前调用，清空排重池
    /// </summary>
    public void ClearRoundExclude()
    {
        roundExclude.Clear();
    }

    private string GetKey(UpgradeOption opt)
    {
        return $"{opt.moduleType}_{opt.statType}";
    }

    public void SyncWithPlayerManager()
    {
        foreach (var config in allModuleConfigs)
        {
            if (IsModuleUnlocked(config.moduleType))
            {
                InitializeRuntimeData(config);
            }
        }
    }

    public void ApplyModulesToPlayer()
    {
        var playerModules = PlayerManager.Instance.CurrentModules;
        if (playerModules == null) return;
        foreach (var module in startingModules)
        {
            UnlockModule(module);
            EventManager.Broadcast<ModuleType>(GameEvent.PlayerUIModelUnlock, module);
        }
    }

    public void ApplyModulesToPlayer(PlayerModuleManager modules)
    {
        foreach (var module in startingModules)
        {
            modules.UnlockModule(module);
        }

        modules.Initialize?.Invoke();
    }


    public void UnlockModule(ModuleType type)
    {
        if (!unlockedModuleTypes.Contains(type))
        {
            unlockedModuleTypes.Add(type);

            ModuleConfig config = GetConfig(type);
            if (config != null && !activeModules.ContainsKey(type))
            {
                activeModules.Add(type, new ModuleRuntimeData(config));
            }

            if (PlayerManager.Instance.CurrentModules != null)
            {
                PlayerManager.Instance.CurrentModules.UnlockModule(type);
            }
        }
    }

    public void LockModule(ModuleType type)
    {
        if (unlockedModuleTypes.Contains(type))
        {
            unlockedModuleTypes.Remove(type);
            //activeModules.Remove(type);
            if (PlayerManager.Instance.CurrentModules != null)
            {
                PlayerManager.Instance.CurrentModules.DisableModule(type);
            }
        }
    }

    public bool ConsumeUpgradePoint(int amount = 1)
    {
        if (UpgradePoints < amount) return false;
        UpgradePoints -= amount;
        return true;
    }

    public bool IsModuleUnlocked(ModuleType type) => unlockedModuleTypes.Contains(type);

    public void AddExperience(int amount)
    {
        CurrentExp += amount;

        int expToLevelUp = GetExpToLevelUp();
        OnExpChanged?.Invoke(CurrentExp, expToLevelUp, CurrentLevel);

        while (CurrentExp >= expToLevelUp)
        {
            CurrentExp -= expToLevelUp;
            CurrentLevel++;
            UpgradePoints += pointsPerLevel;
            OnUpgradePointsChanged?.Invoke(UpgradePoints);

            expToLevelUp = GetExpToLevelUp();
            AudioManager.Instance.PlayEffect("LevelUp");
            OnExpChanged?.Invoke(CurrentExp, expToLevelUp, CurrentLevel);
        }
    }

    public bool ConsumeUpgradePoint()
    {
        if (UpgradePoints <= 0) return false;
        UpgradePoints--;
        OnUpgradePointsChanged?.Invoke(UpgradePoints);
        return true;
    }

    //注意，判定成功后会直接扣费  
    public bool CanUpgrade(ModuleType moduleType, StatType statType)
    {
        return ConsumeUpgradePoint(GetCost(moduleType, statType));
    }

    private int GetExpToLevelUp()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expScale, CurrentLevel - 1));
    }

    //public void UnlockModule(ModuleType type)
    //{
    //    ModuleConfig config = GetConfig(type);
    //    if (config == null) return;

    //    if (activeModules.ContainsKey(type)) return;

    //    PlayerManager.Instance.UnlockModuleData(type);
    //    InitializeRuntimeData(config);
    //}

    public void UpgradeModuleStat(ModuleType moduleType, StatType statType)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            data.AddStatUpgrade(statType);
            EventManager.Broadcast<ModuleType, StatType>(GameEvent.ModuleUpgrade, moduleType, statType);
        }
    }

    private void InitializeRuntimeData(ModuleConfig config)
    {
        if (!activeModules.ContainsKey(config.moduleType))
        {
            activeModules.Add(config.moduleType, new ModuleRuntimeData(config));
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

    public int GetLevel(ModuleType moduleType, StatType statType)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            return data.GetStatLevel(statType);
        }
        return 0;
    }

    public List<StatType> GetUpgradedStats(ModuleType moduleType)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            return data.statTypes;
        }
        return null;
    }

    public int GetCost(ModuleType moduleType, StatType statType)
    {
        if (activeModules.TryGetValue(moduleType, out ModuleRuntimeData data))
        {
            return data.config.GetUpgradeDefinition(statType).pointCost + data.GetStatLevel(statType);
        }
        return -1;
    }

    /// <summary>
    /// 升级选项获取逻辑
    /// </summary>
    public List<UpgradeOption> GetUpgradeOptions(int count)
    {
        List<UpgradeOption> forcePool = new();
        List<UpgradeOption> normalPool = new();

        foreach (var config in allModuleConfigs)
        {
            bool unlocked = IsModuleUnlocked(config.moduleType);

            if (CurrentLevel < config.unlockLevel)
                continue;

            // 强制解锁
            if (!unlocked && config.unlockLevel == CurrentLevel)
            {
                forcePool.Add(new UpgradeOption
                {
                    moduleType = config.moduleType,
                    statType = StatType.None
                });
                continue;
            }

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
                if (activeModules.TryGetValue(config.moduleType, out var runtime))
                {
                    foreach (var stat in config.statUpgrades)
                    {
                        if (runtime.IsStatMaxed(stat.statType))
                            continue;

                        normalPool.Add(new UpgradeOption
                        {
                            moduleType = config.moduleType,
                            statType = stat.statType
                        });
                    }
                }
            }
        }

        Shuffle(normalPool);

        List<UpgradeOption> result = new();

        // 先塞强制池
        foreach (var opt in forcePool)
        {
            string key = GetKey(opt);
            if (roundExclude.Contains(key)) continue;

            roundExclude.Add(key);
            result.Add(opt);

            if (result.Count >= count) return result;
        }

        // 再塞普通池
        foreach (var opt in normalPool)
        {
            if (result.Count >= count) break;

            string key = GetKey(opt);
            if (roundExclude.Contains(key)) continue;

            roundExclude.Add(key);
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
        return null;
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance == this)
        {
            OnExpChanged = null;
            OnUpgradePointsChanged = null;
        }
        EventManager.Clear();
    }
}
