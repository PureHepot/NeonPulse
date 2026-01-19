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
    public List<ModuleDescConfig> moduleDescConfigs; 

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
        List<ModuleType> allModules = new List<ModuleType>();
        allModules.AddRange(unlockableModules);

        foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
        {
            if (!allModules.Contains(type))
            {
                allModules.Add(type);
            }
        }

        List<ModuleType> candidates = allModules
            .OrderBy(_ => _random.Next())
            .Take(candidateCount)
            .ToList();

        return candidates;
    }

    /// <summary>
    /// 处理玩家选择能力（
    /// </summary>
    public void OnChooseModule(ModuleType type)
    {
        if (!_playerManager.IsModuleUnlocked(type))
        {
            _playerManager.UnlockModuleData(type);
            Debug.Log($"成功解锁能力：{type}");
        }
        else
        {
            Debug.Log($"该能力已解锁：{type}");
        }
    }

    /// <summary>
    /// 获取模块的显示配置
    /// </summary>
    public ModuleDescConfig GetModuleDesc(ModuleType type)
    {
        return moduleDescConfigs.FirstOrDefault(x => x.moduleType == type);
    }

    private void OnDestroy()
    {
        _levelSystem.OnPlayerLevelUp -= HandleLevelUp;
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