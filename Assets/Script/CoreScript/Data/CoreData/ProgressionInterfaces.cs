using System;

// --------------------------------------------------------
// 数据读取接口 (IStatReader)
// 作用：战斗系统（伤害计算、血量计算）和 UI 刷新只依赖这个接口。
// 联机意义：不管是本地玩家，还是通过网络同步过来的远程玩家，
// 他们的 PlayerManager 都持有这个接口。战斗逻辑不需要关心数据是从存档读的还是从网络包解出来的。
// --------------------------------------------------------
public interface IStatReader
{
    /// <summary> 获取局外永久强化的基础等级 </summary>
    int GetMetaBaseLevel(ModuleType Module, StatType stat);

    /// <summary> 获取局内获得的临时等级 </summary>
    int GetRunLevel(ModuleType Module, StatType stat);

    /// <summary> 获取最终计算的总等级 (Meta + Run) </summary>
    int GetTotalLevel(ModuleType Module, StatType stat);

    /// <summary> 获取该属性在局内的最高可升等级（由局外科技树决定上限） </summary>
    int GetRunMaxLevelCap(ModuleType Module, StatType stat);
}

// --------------------------------------------------------
// 数据修改接口 (IProgressionMutator)
// 作用：只允许拥有授权的系统（如本地的 UpgradeManager）调用。
// 联机意义：远程玩家的实例不会实现或暴露这个接口，防止本地误改远程玩家的数据。
// --------------------------------------------------------
public interface IProgressionMutator
{
    /// <summary> 尝试在局外升级（消耗 Meta 货币） </summary>
    bool TryUpgradeMeta(ModuleType Module, StatType stat, int cost);

    /// <summary> 尝试在局内升级（消耗局内拾取物/经验） </summary>
    bool TryUpgradeRun(ModuleType Module, StatType stat);
}

// --------------------------------------------------------
// 综合数据提供者 (服务定位器或依赖注入使用)
// --------------------------------------------------------
public interface IProgressionDataProvider : IStatReader, IProgressionMutator
{
    // 可以添加一些事件用于 UI 响应式刷新
    event Action<ModuleType, StatType> OnStatUpgraded;
}
