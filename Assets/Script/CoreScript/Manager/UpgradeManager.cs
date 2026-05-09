using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UpgradeOption
{
    public ModuleType ModuleType;
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

    [Header("Default Loadout")]
    public List<ModuleType> startingModules;

    // 运行时缓存（不序列化，从 DataManager 重建）
    private HashSet<ModuleType> unlockedModuleTypes = new HashSet<ModuleType>();
    public HashSet<ModuleType> UnlockedModuleTypes => unlockedModuleTypes;

    private Dictionary<ModuleType, ModuleRuntimeData> activeModules =
        new Dictionary<ModuleType, ModuleRuntimeData>();

    public Action<int, int, int> OnExpChanged;
    public Action<int> OnUpgradePointsChanged;

    // 本轮升级面板排重池
    private HashSet<string> roundExclude = new();

    // ==================== 数据代理（通过 DataManager）====================

    public int CurrentLevel
    {
        get => DataManager.Instance.Run != null ? DataManager.Instance.Run.progression.level : 1;
        private set { if (DataManager.Instance.Run != null) DataManager.Instance.Run.progression.level = value; }
    }

    public int CurrentExp
    {
        get => DataManager.Instance.Run != null ? DataManager.Instance.Run.progression.exp : 0;
        private set { if (DataManager.Instance.Run != null) DataManager.Instance.Run.progression.exp = value; }
    }

    public int UpgradePoints
    {
        get => DataManager.Instance.Run != null ? DataManager.Instance.Run.progression.upgradePoints : 0;
        private set { if (DataManager.Instance.Run != null) DataManager.Instance.Run.progression.upgradePoints = value; }
    }

    // ==================== 存档 ↔ 运行时 同步 ====================

    /// <summary>
    /// 从 DataManager 的 Run.build.ownedModules 重建运行时缓存
    /// 用于继续游戏（读档）
    /// </summary>
    public void InitFromSaveData()
    {
        unlockedModuleTypes.Clear();
        activeModules.Clear();

        var run = DataManager.Instance.Run;
        if (run == null) return;

        foreach (var owned in run.build.ownedModules)
        {
            unlockedModuleTypes.Add(owned.ModuleType);

            ModuleConfig config = GetConfig(owned.ModuleType);
            if (config == null) continue;

            var runtime = new ModuleRuntimeData(config);
            // 恢复每个 stat 的升级层数
            foreach (var sl in owned.statLevels)
            {
                for (int i = 0; i < sl.level; i++)
                    runtime.AddStatUpgrade(sl.statType);
            }
            activeModules[owned.ModuleType] = runtime;
        }
    }

    /// <summary>
    /// 将当前运行时缓存写回 DataManager 的 Run.build
    /// 用于存档前调用
    /// </summary>
    public void SyncToSaveData()
    {
        var run = DataManager.Instance.Run;
        if (run == null) return;

        run.build.ownedModules.Clear();
        foreach (var kvp in activeModules)
        {
            var owned = new OwnedModuleRunData
            {
                ModuleType = kvp.Key,
                statLevels = new List<StatLevelData>()
            };

            var runtime = kvp.Value;
            foreach (var statType in runtime.statTypes)
            {
                int level = runtime.GetStatLevel(statType);
                if (level > 0)
                {
                    owned.statLevels.Add(new StatLevelData
                    {
                        statType = statType,
                        level = level
                    });
                }
            }

            run.build.ownedModules.Add(owned);
        }
    }

    /// <summary>
    /// 新局开始时初始化：清空缓存，写入 startingModules
    /// </summary>
    public void InitNewRun()
    {
        unlockedModuleTypes.Clear();
        activeModules.Clear();
        roundExclude.Clear();
    }

    // ==================== 原有逻辑 ====================

    public void ClearRoundExclude()
    {
        roundExclude.Clear();
    }


    #region 模块操作
    public void ApplyModulesToPlayer()
    {
        var playerModules = PlayerManager.Instance.CurrentModules;
        if (playerModules == null) return;
        foreach (var Module in startingModules)
        {
            UnlockModule(Module);
            EventManager.Broadcast<ModuleType>(GameEvent.PlayerUIModelUnlock, Module);
        }
    }

    public void ApplyModulesToPlayer(PlayerModuleManager Modules)
    {
        foreach (var Module in startingModules)
        {
            Modules.UnlockModule(Module);
        }

        Modules.Initialize?.Invoke();
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

            SyncToSaveData();
        }
    }

    public void LockModule(ModuleType type)
    {
        if (unlockedModuleTypes.Contains(type))
        {
            unlockedModuleTypes.Remove(type);
            if (PlayerManager.Instance.CurrentModules != null)
            {
                PlayerManager.Instance.CurrentModules.DisableModule(type);
            }

            SyncToSaveData();
        }
    }

    public void ResetLevel(ModuleType ModuleType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            data.ResetAllStatLevel();
            SyncToSaveData();
        }
    }

    public bool ConsumeUpgradePoint(int amount = 1)
    {
        if (UpgradePoints < amount) return false;
        UpgradePoints -= amount;
        return true;
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
            UpgradePoints += pointsPerLevel;
            OnUpgradePointsChanged?.Invoke(UpgradePoints);

            expToLevelUp = GetExpToLevelUp();
            AudioManager.Instance.PlayEffect("LevelUp");
            OnExpChanged?.Invoke(CurrentExp, expToLevelUp, CurrentLevel);
        }
    }

    private int GetExpToLevelUp()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expScale, CurrentLevel - 1));
    }


    public void UpgradeModuleStat(ModuleType ModuleType, StatType statType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            if (data.AddStatUpgrade(statType))
            {
                SyncToSaveData();
                EventManager.Broadcast<ModuleType, StatType>(GameEvent.ModuleUpgrade, ModuleType, statType);
            }
        }
    }

    #endregion

    #region 获取数据的接口
    public bool IsModuleUnlocked(ModuleType type) => unlockedModuleTypes.Contains(type);
    public void GainUpgradePointByModule(ModuleType type)
    {
        if (activeModules.TryGetValue(type, out ModuleRuntimeData data))
        {
            int amount = 0;
            foreach (var stat in data.statTypes)
            {
                amount += data.GetStatLevel(stat) * data.config.GetUpgradeDefinition(stat).pointCost;
            }
            UpgradePoints += amount;
        }
    }

    public bool CanUpgrade(ModuleType ModuleType, StatType statType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            if (data.IsStatMaxed(statType))
            {
                return false;
            }
        }

        return ConsumeUpgradePoint(GetCost(ModuleType, statType));
    }

    public float GetStat(ModuleType ModuleType, StatType statType, float defaultValue = 0f)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            return data.GetCurrentStat(statType);
        }
        return defaultValue;
    }

    public int GetLevel(ModuleType ModuleType, StatType statType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            return data.GetStatLevel(statType);
        }
        return 0;
    }

    public List<StatType> GetUpgradedStats(ModuleType ModuleType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            return data.statTypes;
        }
        return null;
    }

    public int GetCost(ModuleType ModuleType, StatType statType)
    {
        if (activeModules.TryGetValue(ModuleType, out ModuleRuntimeData data))
        {
            return data.config.GetUpgradeDefinition(statType).pointCost;
        }
        return -1;
    }

    public ModuleConfig GetConfig(ModuleType type)
    {
        foreach (var config in allModuleConfigs)
        {
            if (config.ModuleType == type)
                return config;
        }
        return null;
    }

    #endregion

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
