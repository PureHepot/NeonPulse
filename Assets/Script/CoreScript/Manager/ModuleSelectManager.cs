using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家能力选择
/// </summary>
public class ModuleSelectManager : MonoSingleton<ModuleSelectManager>
{
    [Header("能力选择配置")]
    public int candidateCount = 3;

    public Action<int, List<UpgradeOption>> OnShowAbilitySelectUI;

    private void Awake()
    {
        UpgradeManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnLevelUp -= HandleLevelUp;
        }
    }

    /// <summary>
    /// 等级提升,弹出升级UI
    /// </summary>
    private void HandleLevelUp(int level, List<UpgradeOption> options)
    {
        OnShowAbilitySelectUI?.Invoke(level, options);
    }

    /// <summary>
    /// 玩家点击某个模块
    /// </summary>
    public void OnChooseUpgrade(UpgradeOption option)
    {
        if (option.statType == StatType.None)
        {
            UpgradeManager.Instance.UnlockModule(option.moduleType);
            Debug.Log($"解锁模块: {option.moduleType}");
        }
        else
        {
            UpgradeManager.Instance.UpgradeModuleStat(option.moduleType, option.statType);
            Debug.Log($"升级属性: {option.moduleType} -> {option.statType}");
        }
    }

    /// <summary>
    /// 获取模块显示信息
    /// </summary>
    public ModuleDescConfig GetUpgradeDesc(UpgradeOption option)
    {
        ModuleConfig config = UpgradeManager.Instance.GetConfig(option.moduleType);

        if (option.statType == StatType.None)
        {
            return new ModuleDescConfig
            {
                moduleType = option.moduleType,
                moduleName = config.moduleName,
                moduleDesc = $"解锁新模块：{config.moduleName}"
            };
        }
        else
        {
            var def = config.GetUpgradeDefinition(option.statType);
            return new ModuleDescConfig
            {
                moduleType = option.moduleType,
                moduleName = def.upgradeName,
                moduleDesc = def.description
            };
        }
    }
}

[Serializable]
public class ModuleDescConfig
{
    public ModuleType moduleType;
    public string moduleName;
    [TextArea] public string moduleDesc;
    public Sprite moduleIcon;
}
