using System.Collections.Generic;
using System;
using UnityEngine;

public class ModuleSelectManager : MonoSingleton<ModuleSelectManager>
{
    public int candidateCount = 3;

    public Action<int, List<UpgradeOption>> OnShowAbilitySelectUI;

    private List<UpgradeOption> cachedOptions = new();

    public List<UpgradeOption> GetOrCreateOptions()
    {
        if (cachedOptions == null || cachedOptions.Count == 0)
        {
            cachedOptions = UpgradeManager.Instance.GetUpgradeOptions(candidateCount);
        }

        return cachedOptions;
    }

    public UpgradeOption GetSingleRandomOption()
    {
        return UpgradeManager.Instance.GetUpgradeOptions(1)[0];
    }

    public void ReplaceOption(int index, UpgradeOption option)
    {
        if (index < 0 || index >= cachedOptions.Count) return;
        cachedOptions[index] = option;
    }

    public void OnChooseUpgrade(UpgradeOption option, int index)
    {
        if (!UpgradeManager.Instance.ConsumeUpgradePoint())
        {
            Debug.LogWarning("升级点数不足，无法升级！");
            return;
        }

        if (option.statType == StatType.None)
        {
            UpgradeManager.Instance.UnlockModule(option.moduleType);
        }
        else
        {
            UpgradeManager.Instance.UpgradeModuleStat(option.moduleType, option.statType);
        }

        if (UpgradeManager.Instance.UpgradePoints > 0)
        {
            ReplaceOption(index, GetSingleRandomOption());
        }
    }

    public ModuleDescConfig GetUpgradeDesc(UpgradeOption option)
    {
        ModuleConfig config = UpgradeManager.Instance.GetConfig(option.moduleType);

        if (option.statType == StatType.None)
        {
            return new ModuleDescConfig
            {
                moduleType = option.moduleType,
                moduleName = config.moduleName,
                moduleDesc = $"解锁新模块：{config.moduleName}\n当前升级点数：{UpgradeManager.Instance.UpgradePoints}"
            };
        }

        var def = config.GetUpgradeDefinition(option.statType);

        return new ModuleDescConfig
        {
            moduleType = option.moduleType,
            moduleName = def.upgradeName,
            moduleDesc = $"{def.description}\n当前升级点数：{UpgradeManager.Instance.UpgradePoints}"
        };
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