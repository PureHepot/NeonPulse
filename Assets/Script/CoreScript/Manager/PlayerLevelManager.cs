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
    public float expScale = 1.5f;
    [SerializeField]
    public int currentLevel = 1;
    [SerializeField]
    private int currentExp = 0;

    public Dictionary<int, List<ModuleType>> levelUnlockCache = new Dictionary<int, List<ModuleType>>();
    // 缓存升级数值
    public Dictionary<ModuleType, int> moduleUpgradeValueCache = new Dictionary<ModuleType, int>();
    // 缓存初始数值
    public Dictionary<ModuleType, float> moduleInitialValueCache = new Dictionary<ModuleType, float>();

    // 经验变化
    public Action<int, int, int> OnExpChanged;
    // 升级成功
    public Action<int, List<ModuleType>> OnPlayerLevelUp;

    private PlayerManager playerManager;

    private void Awake()
    {
        playerManager = PlayerManager.Instance;
        LoadModuleUnlockFromCSV();
    }

    /// <summary>
    /// 从CSV加载解锁规则+升级数值
    /// </summary>
    private void LoadModuleUnlockFromCSV()
    {
        CSVManager.Instance.LoadTable("UpgradeData");
        levelUnlockCache.Clear();
        moduleUpgradeValueCache.Clear();
        moduleInitialValueCache.Clear();

        // 遍历CSV的行ID
        for (int idNum = 1001; idNum <= 1100; idNum++)
        {
            string rowId = idNum.ToString();
            // 用CSVManager的GetRow读取整行数据
            var row = CSVManager.Instance.GetRow("UpgradeData", rowId);
            if (row == null) continue;

            // 读取CSV的LimitLevel和Type
            int unlockLevel = CSVManager.Instance.GetInt("UpgradeData", rowId, "LimitLevel");
            string csvType = CSVManager.Instance.GetValue("UpgradeData", rowId, "Type");

            if (Enum.TryParse<ModuleType>(csvType, out ModuleType moduleType))
            {
                // 加载解锁等级缓存
                if (!levelUnlockCache.ContainsKey(unlockLevel))
                {
                    levelUnlockCache[unlockLevel] = new List<ModuleType>();
                }
                if (!levelUnlockCache[unlockLevel].Contains(moduleType))
                {
                    levelUnlockCache[unlockLevel].Add(moduleType);
                }

                // 读取初始值
                float initialVal = CSVManager.Instance.GetFloat("UpgradeData", rowId, "InitialValue");
                if (!moduleInitialValueCache.ContainsKey(moduleType))
                    moduleInitialValueCache.Add(moduleType, initialVal);

                // 升级数值缓存
                int upgradeValue = CSVManager.Instance.GetInt("UpgradeData", rowId, "ModuleUpgrade");
                if (!moduleUpgradeValueCache.ContainsKey(moduleType))
                {
                    moduleUpgradeValueCache.Add(moduleType, upgradeValue);
                }
            }
            else
            {
                Debug.LogWarning($"CSV行{rowId}的Type{csvType}无法转换为ModuleType");
            }
        }
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

            List<ModuleType> unlockableModules = GetUnlockableModulesByLevel(currentLevel);

            Debug.Log($"等级提升到{currentLevel}级→可操作模块：{string.Join(",", unlockableModules)}");

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
    /// 获取当前等级可操作的模块
    /// </summary>
    private List<ModuleType> GetUnlockableModulesByLevel(int level)
    {
        List<ModuleType> operableModules = new List<ModuleType>();

        // 先添加当前等级可解锁的未解锁模块
        if (levelUnlockCache.ContainsKey(level))
        {
            foreach (var moduleType in levelUnlockCache[level])
            {
                if (!playerManager.IsModuleUnlocked(moduleType) && !operableModules.Contains(moduleType))
                {
                    operableModules.Add(moduleType);
                }
            }
        }

        // 补充已解锁模块用于升级
        if (operableModules.Count < 3) 
        {
            foreach (var kvp in levelUnlockCache)
            {
                // 只补充<=当前等级的已解锁模块
                if (kvp.Key <= level)
                {
                    foreach (var moduleType in kvp.Value)
                    {
                        if (playerManager.IsModuleUnlocked(moduleType) && !operableModules.Contains(moduleType))
                        {
                            operableModules.Add(moduleType);
                            if (operableModules.Count >= 3) break;
                        }
                    }
                }
                if (operableModules.Count >= 3) break;
            }
        }

        return operableModules;
    }

    /// <summary>
    /// 升级后选择模块
    /// </summary>
    public void OnLevelUpChooseModule(ModuleType type)
    {
        if (!playerManager.IsModuleUnlocked(type))
        {
            playerManager.UnlockModuleData(type);
            Debug.Log($"解锁模块：{type}");

            InitModuleBaseValue(type);
        }
        else
        {
            if (playerManager.CurrentModules != null)
            {
                playerManager.CurrentModules.UpgradeModule(type);
                // 获取升级数值，打印日志
                int upgradeVal = moduleUpgradeValueCache.TryGetValue(type, out int val) ? val : 1;
                Debug.Log($"升级模块：{type} | 升级数值：{upgradeVal}");
            }
            else
            {
                Debug.LogWarning($"当前无玩家模块管理器，无法升级{type}");
            }
        }
    }

    /// <summary>
    /// 初始化模块基础属性
    /// </summary>
    public void InitModuleBaseValue(ModuleType type)
    {
        if (!moduleInitialValueCache.TryGetValue(type, out float initVal))
        {
            initVal = 1;
        }

        switch (type)
        {
            case ModuleType.Health:
                var healthModule = playerManager.CurrentModules?.GetModule<HealthModule>(ModuleType.Health);
                if (healthModule != null)
                {
                    healthModule.maxHp = (int)initVal; 
                    PlayerManager.Instance.MaxHealth = healthModule.maxHp;
                }
                break;
            case ModuleType.Shooter:
                var shooterModule = playerManager.CurrentModules?.GetModule<ShooterModule>(ModuleType.Shooter);
                if (shooterModule != null)
                {
                    shooterModule.fireRate = initVal; 
                }
                break;
                // 其他模块
        }
    }

    private void OnDestroy()
    {
        CSVManager.Instance.UnloadTable("UpgradeData");
    }

    public int GetModuleUpgradeValue(ModuleType type)
    {
        return moduleUpgradeValueCache.TryGetValue(type, out int val) ? val : 1;
    }
}