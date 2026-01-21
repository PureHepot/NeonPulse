using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 玩家能力选择系统
/// </summary>
public class PlayerAbilitySelectManager : MonoSingleton<PlayerAbilitySelectManager>
{
    [Header("能力选择配置")]
    public int candidateCount = 3;

    public Action<int, List<ModuleType>> OnShowAbilitySelectUI;

    private PlayerManager _playerManager;
    private PlayerLevelManager _levelSystem;
    private System.Random _random = new System.Random();

    private void Awake()
    {
        _playerManager = PlayerManager.Instance;
        _levelSystem = PlayerLevelManager.Instance;

        // 订阅等级升级事件
        _levelSystem.OnPlayerLevelUp += HandleLevelUp;
    }

    private void HandleLevelUp(int level, List<ModuleType> unlockableModules)
    {
        List<ModuleType> candidateModules = GenerateCandidateModules(unlockableModules);
        OnShowAbilitySelectUI?.Invoke(level, candidateModules);
    }

    /// <summary>
    /// 生成候选模块列表
    /// </summary>
    private List<ModuleType> GenerateCandidateModules(List<ModuleType> unlockableModules)
    {
        List<ModuleType> candidates = new List<ModuleType>();

        // 优先添加当前等级可解锁的未解锁模块
        candidates.AddRange(unlockableModules
            .OrderBy(_ => _random.Next())
            .Take(candidateCount));

        // 如果还不够，补全所有<=当前等级的未解锁模块
        if (candidates.Count < candidateCount)
        {
            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                if (IsModuleLevelValid(type, _levelSystem.currentLevel)
                    && !_playerManager.IsModuleUnlocked(type)
                    && !candidates.Contains(type))
                {
                    candidates.Add(type);
                    if (candidates.Count >= candidateCount) break;
                }
            }
        }

        // 如果仍不够，再补已解锁模块用于升级
        if (candidates.Count < candidateCount)
        {
            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                if (_playerManager.IsModuleUnlocked(type) && !candidates.Contains(type))
                {
                    candidates.Add(type);
                    if (candidates.Count >= candidateCount) break;
                }
            }
        }

        candidates = candidates.Distinct().OrderBy(_ => _random.Next()).Take(candidateCount).ToList();

        return candidates;
    }

    private bool IsModuleLevelValid(ModuleType type, int currentLevel)
    {
        foreach (var kvp in PlayerLevelManager.Instance.levelUnlockCache)
        {
            if (kvp.Value.Contains(type))
            {
                Debug.Log($"模块{type}的解锁等级：{kvp.Key}，当前等级：{currentLevel}");
                return kvp.Key <= currentLevel;
            }
        }
        return false;
    }

    /// <summary>
    /// 处理玩家选择能力
    /// </summary>
    public void OnChooseModule(ModuleType type)
    {
        _levelSystem.OnLevelUpChooseModule(type);

        if (!_playerManager.IsModuleUnlocked(type))
        {
            Debug.Log($"成功解锁能力：{type}");
        }
        else
        {
            Debug.Log($"成功升级能力：{type}");
        }
    }

    /// <summary>
    /// 获取模块的显示配置
    /// </summary>
    public ModuleDescConfig GetModuleDesc(ModuleType type)
    {
        ModuleDescConfig defaultConfig = new ModuleDescConfig()
        {
            moduleName = type.ToString(),
            moduleDesc = _playerManager.IsModuleUnlocked(type) ? $"升级{type}模块" : $"解锁{type}模块"
        };

        // 遍历CSV行ID
        for (int idNum = 1001; idNum <= 1100; idNum++)
        {
            string rowId = idNum.ToString();
            var row = CSVManager.Instance.GetRow("UpgradeData", rowId);
            if (row == null) continue;

            // 读取CSV的Type列
            string csvType = CSVManager.Instance.GetValue("UpgradeData", rowId, "Type");
            if (csvType == type.ToString())
            {
                ModuleDescConfig config = new ModuleDescConfig();
                config.moduleType = type;
                config.moduleName = CSVManager.Instance.GetValue("UpgradeData", rowId, "Name");

                // 读取初始值
                string initialValueStr = CSVManager.Instance.GetValue("UpgradeData", rowId, "InitialValue");
                // 读取升级值
                string upgradeValueStr = CSVManager.Instance.GetValue("UpgradeData", rowId, "ModuleUpgrade");

                // 读取原始描述
                string desc = CSVManager.Instance.GetValue("UpgradeData", rowId, "Description");
                // 拆分解锁/升级描述（|分隔）
                string[] descSplit = desc.Split('|');

                if (_playerManager.IsModuleUnlocked(type))
                {
                    string upgradeDesc = descSplit.Length > 1 ? descSplit[1] : $"升级{type}模块";
                    upgradeDesc = upgradeDesc.Replace("{ModuleUpgrade}", upgradeValueStr);
                    upgradeDesc = upgradeDesc.Replace("{InitialValue}", initialValueStr);
                    config.moduleDesc = upgradeDesc;
                }
                else
                {
                    string unlockDesc = descSplit[0];
                    unlockDesc = unlockDesc.Replace("{InitialValue}", initialValueStr);
                    unlockDesc = unlockDesc.Replace("{ModuleUpgrade}", upgradeValueStr);
                    config.moduleDesc = unlockDesc;
                }

                return config;
            }
        }
        return defaultConfig;
    }

    private void OnDestroy()
    {
        _levelSystem.OnPlayerLevelUp -= HandleLevelUp;
        CSVManager.Instance.UnloadTable("UpgradeData");
    }
}

// 模块显示配置
[Serializable]
public class ModuleDescConfig
{
    public ModuleType moduleType;
    public string moduleName; // 显示名称
    [TextArea] public string moduleDesc; // 描述
    public Sprite moduleIcon; // 图标
}