using System;

// ============================================================
// 装配系统数据接口
// 面向联机适配：读写分离，远程玩家只暴露只读接口
// ============================================================

// --------------------------------------------------------
// 只读接口：战斗系统、UI刷新只依赖此接口
// 联机意义：远程玩家数据从网络包同步，也实现此接口
// --------------------------------------------------------

/// <summary>
/// 装配数据只读接口：获取最终计算后的数值
/// </summary>
public interface ILoadoutReader
{
    /// <summary> 当前装备的框架ID </summary>
    string FrameId { get; }

    /// <summary> 获取指定插槽中模块的类型（None=空槽） </summary>
    ModuleType GetSlotModuleType(string slotId);

    /// <summary> 获取指定属性的最终值（模块基础 + 核心加成 + 插槽修正，全部叠加后） </summary>
    float GetFinalStat(string statId);

    /// <summary> 获取指定模块上所有配件的效果ID列表（用于战斗系统触发） </summary>
    string[] GetActivePluginEffectIds(string slotId);

    /// <summary> 获取框架所有固有特效 </summary>
    FrameInherentEffect[] GetFrameInherentEffects();

    /// <summary> 该插槽是否已装备模块 </summary>
    bool IsSlotOccupied(string slotId);
}

// --------------------------------------------------------
// 修改接口：只允许本地授权系统调用
// 联机意义：远程玩家不实现此接口，防止误改
// --------------------------------------------------------

/// <summary>
/// 装配数据修改接口：局内装配操作
/// </summary>
public interface ILoadoutMutator
{
    /// <summary> 选择框架（新局开始时） </summary>
    bool SelectFrame(string frameId);

    /// <summary> 将模块装备到指定插槽 </summary>
    bool EquipModule(string slotId, ModuleType moduleType);

    bool EquipModule(string slotId, string moduleId);

    /// <summary> 从指定插槽卸下模块 </summary>
    bool UnequipModule(string slotId);

    /// <summary> 为指定插槽的模块插入核心（替换已有核心） </summary>
    bool InsertCore(string slotId, string coreId);

    /// <summary> 移除指定插槽模块的核心 </summary>
    bool RemoveCore(string slotId);

    /// <summary> 为指定插槽的模块插入配件 </summary>
    bool InsertPlugin(string slotId, string pluginId, PluginRarity rarity);

    /// <summary> 移除指定插槽模块的指定配件 </summary>
    bool RemovePlugin(string slotId, int pluginIndex);

    /// <summary> 清空当前装配（死亡/重开时） </summary>
    void ClearLoadout();
}

// --------------------------------------------------------
// 综合数据提供者
// --------------------------------------------------------

/// <summary>
/// 装配数据完整提供者（只读 + 修改 + 事件）
/// </summary>
public interface ILoadoutDataProvider : ILoadoutReader, ILoadoutMutator
{
    /// <summary> 插槽内容变更时触发（装备/卸载/核心/配件变化） </summary>
    event Action<string> OnSlotChanged;

    /// <summary> 框架变更时触发 </summary>
    event Action<string> OnFrameChanged;
}
