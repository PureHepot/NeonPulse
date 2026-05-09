using System;
using UnityEngine;

/// <summary>
/// 本地玩家进度提供者：桥接 DataManager 的局内与局外存档数据
/// </summary>
public class LocalPlayerProgressionProvider : IProgressionDataProvider
{
    public event Action<ModuleType, StatType> OnStatUpgraded;

    // ==================== IStatReader 实现 ====================

    public int GetMetaBaseLevel(ModuleType Module, StatType stat)
    {
        // 假设你在 MetaProgressData 中加了一个字典或列表来存永久升级
        // return DataManager.Instance.Meta.GetModuleLevel(Module, stat);

        // 占位返回，需配合 DataManager 修改
        return 0;
    }

    public int GetRunLevel(ModuleType Module, StatType stat)
    {
        var runData = DataManager.Instance.Run;
        if (runData == null) return 0;

        // 遍历找到对应的模块并返回局内等级 (从你之前的 OwnedModuleRunData 提取)
        var owned = runData.build.ownedModules.Find(m => m.ModuleType == Module);
        if (owned != null)
        {
            var statData = owned.statLevels.Find(s => s.statType == stat);
            if (statData != null) return statData.level;
        }
        return 0;
    }

    public int GetTotalLevel(ModuleType Module, StatType stat)
    {
        // 核心公式：总等级 = 局外基础 + 局内临时
        return GetMetaBaseLevel(Module, stat) + GetRunLevel(Module, stat);
    }

    public int GetRunMaxLevelCap(ModuleType Module, StatType stat)
    {
        //TODO: 改相关内容

        int baseCap = 5;
        // int capBonus = DataManager.Instance.Meta.GetCapBonus(Module, stat);
        return baseCap; // + capBonus;
    }

    // ==================== IProgressionMutator 实现 ====================

    public bool TryUpgradeMeta(ModuleType Module, StatType stat, int cost)
    {
        var meta = DataManager.Instance.Meta;
        if (meta.softCurrency >= cost)
        {
            meta.softCurrency -= cost;
            // 执行 Meta 写入逻辑...
            // meta.SetModuleLevel(Module, stat, GetMetaBaseLevel(...) + 1);

            DataManager.Instance.Save();
            OnStatUpgraded?.Invoke(Module, stat);
            return true;
        }
        return false;
    }

    public bool TryUpgradeRun(ModuleType Module, StatType stat)
    {
        int currentRunLevel = GetRunLevel(Module, stat);
        int cap = GetRunMaxLevelCap(Module, stat);

        if (currentRunLevel >= cap)
        {
            Debug.Log($"[Progression] 模块 {Module} 的 {stat} 属性已达局内上限");
            return false;
        }

        // 写入 RunData 的逻辑 (类似你之前 UpgradeManager 里的 AddStatUpgrade)
        var runData = DataManager.Instance.Run;
        var owned = runData.build.ownedModules.Find(m => m.ModuleType == Module);
        if (owned == null)
        {
            // 如果还没拥有，初始化
            owned = new OwnedModuleRunData { ModuleType = Module, statLevels = new() };
            runData.build.ownedModules.Add(owned);
        }

        var statData = owned.statLevels.Find(s => s.statType == stat);
        if (statData == null)
        {
            owned.statLevels.Add(new StatLevelData { statType = stat, level = 1 });
        }
        else
        {
            statData.level++;
        }

        OnStatUpgraded?.Invoke(Module, stat);
        return true;
    }
}
