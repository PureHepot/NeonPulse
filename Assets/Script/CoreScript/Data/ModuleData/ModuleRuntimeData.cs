using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleRuntimeData
{
    public ModuleConfig config;
    public int currentLevel = 1;

    private Dictionary<StatType, int> upgradeCounts = new Dictionary<StatType, int>();

    public ModuleRuntimeData(ModuleConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// 对指定属性进行一次升级
    /// </summary>
    public void AddStatUpgrade(StatType type)
    {
        if (!upgradeCounts.ContainsKey(type))
        {
            upgradeCounts[type] = 0;
        }

        var def = config.GetUpgradeDefinition(type);
        if (def == null)
        {
            Debug.LogWarning($"[{config.moduleName}] 找不到升级定义: {type}");
            return;
        }

        if (def.maxStacks != -1 && upgradeCounts[type] >= def.maxStacks)
        {
            Debug.LogWarning($"[{config.moduleName}] 属性 {type} 已达最大等级");
            return;
        }

        upgradeCounts[type]++;
        Debug.Log($"[{config.moduleName}] 属性 {type} 升级! 当前层数: {upgradeCounts[type]}");
    }

    /// <summary>
    /// 获取当前最终值
    /// </summary>
    public float GetCurrentStat(StatType type)
    {
        float finalValue = config.GetBaseStat(type);

        if (upgradeCounts.TryGetValue(type, out int count) && count > 0)
        {
            var def = config.GetUpgradeDefinition(type);
            if (def != null)
            {
                finalValue += count * def.valuePerUpgrade;
            }
        }

        return finalValue;
    }

    /// <summary>
    /// 获取某属性当前升了几级
    /// </summary>
    public int GetStatLevel(StatType type)
    {
        return upgradeCounts.TryGetValue(type, out int count) ? count : 0;
    }

    /// <summary>
    /// 是否已达到最大升级层数
    /// </summary>
    public bool IsStatMaxed(StatType type)
    {
        var def = config.GetUpgradeDefinition(type);
        if (def == null) return true;
        if (def.maxStacks < 0) return false;

        return GetStatLevel(type) >= def.maxStacks;
    }
}
