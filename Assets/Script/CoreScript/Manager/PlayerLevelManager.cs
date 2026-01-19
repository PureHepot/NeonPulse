using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家升级/经验系统
/// </summary>
public class PlayerLevelManager : MonoSingleton<PlayerLevelManager>
{
    [Header("升级配置")]
    public int baseExpToLevelUp = 100;  
    public float expScale = 1.5f;       // 每级经验增长倍率
    [SerializeField]
    private int currentLevel = 1;       
    [SerializeField]
    private int currentExp = 0;        

    public List<ModuleUnlockConfig> moduleUnlockConfigs;

    // 经验变化
    public Action<int, int, int> OnExpChanged;
    // 升级成功
    public Action<int, List<ModuleType>> OnPlayerLevelUp; 

    private PlayerManager playerManager;

    private void Awake()
    {
        playerManager = PlayerManager.Instance;
    }

    /// <summary>
    /// 添加经验（供EnemyBase调用）
    /// </summary>
    public void AddExperience(int expAmount)
    {
        if (!playerManager.IsPlayerAlive) return; 

        currentExp += expAmount;
        int expToLevelUp = GetExpToLevelUp();

        Debug.Log($"获得{expAmount}经验 | 当前等级：{currentLevel}");

        OnExpChanged?.Invoke(currentExp, expToLevelUp, currentLevel);

        while (currentExp >= expToLevelUp)
        {
            currentExp -= expToLevelUp;
            currentLevel++;
            expToLevelUp = GetExpToLevelUp();

            // 获取当前等级可解锁的模块
            List<ModuleType> unlockableModules = GetUnlockableModulesByLevel(currentLevel);

            Debug.Log($"等级提升到{currentLevel}级→可解锁模块：{string.Join(",", unlockableModules)}");

            OnPlayerLevelUp?.Invoke(currentLevel, unlockableModules);

            OnExpChanged?.Invoke(currentExp, expToLevelUp, currentLevel);
        }
    }

    /// <summary>
    /// 计算当前等级升级所需经验
    /// </summary>
    private int GetExpToLevelUp()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expScale, currentLevel - 1));
    }

    /// <summary>
    /// 根据等级获取可解锁的模块列表
    /// </summary>
    private List<ModuleType> GetUnlockableModulesByLevel(int level)
    {
        List<ModuleType> unlockableModules = new List<ModuleType>();
        foreach (var config in moduleUnlockConfigs)
        {
            if (config.unlockLevel == level && !playerManager.IsModuleUnlocked(config.moduleType))
            {
                unlockableModules.Add(config.moduleType);
            }
        }
        return unlockableModules;
    }

    /// <summary>
    /// 升级后选择解锁模块
    /// </summary>
    public void OnLevelUpChooseModule(ModuleType type)
    {
        playerManager.UnlockModuleData(type); // 复用PlayerManager的解锁逻辑
        Debug.Log($"升级解锁能力：{type}");
    }
}

// 根据等级进行模块解锁配置
[Serializable]
public class ModuleUnlockConfig
{
    public int unlockLevel; // 解锁等级
    public ModuleType moduleType; // 对应解锁的模块
}