using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 核心配置 (ScriptableObject)
// 核心 = 纯数值加成，无特殊效果
// 每个模块只能装1个核心
// ============================================================

[CreateAssetMenu(fileName = "NewCoreConfig", menuName = "Game/Core Config")]
public class CoreConfig : ScriptableObject
{
    [Header("基本信息")]
    public string coreId;           // 如 "Core_DamageBoost"
    public string displayName;      // 如 "伤害增幅核心"
    [TextArea] public string description;
    public CoreType coreType;
    public Sprite icon;

    [Header("数值加成")]
    [Tooltip("插入此核心后提供的属性加成列表")]
    public List<CoreStatBonus> statBonuses = new List<CoreStatBonus>();

    [Header("适配限制")]
    [Tooltip("旧兼容字段：该核心只能插入这些模块类型（空=不限）")]
    public List<ModuleType> restrictedToModules = new List<ModuleType>();
    [Tooltip("新规则：该核心只能插入这些模块分类（None=不限）")]
    public ModuleCategory restrictedToCategories = ModuleCategory.None;

    [Header("掉落权重")]
    public int dropWeight = 100;    // 掉落权重，越高越常见

    // ==================== 运行时辅助 ====================

    /// <summary>
    /// 该核心是否可以插入指定模块
    /// </summary>
    public bool CanInsertInto(ModuleType moduleType)
    {
        if (restrictedToModules == null || restrictedToModules.Count == 0)
            return true;
        return restrictedToModules.Contains(moduleType);
    }

    public bool CanInsertInto(ModuleConfig moduleConfig)
    {
        if (moduleConfig == null)
            return false;

        if (restrictedToCategories != ModuleCategory.None &&
            (restrictedToCategories & moduleConfig.categories) == 0)
        {
            return false;
        }

        if (restrictedToModules == null || restrictedToModules.Count == 0)
            return true;

        return restrictedToModules.Contains(moduleConfig.moduleType);
    }
}

/// <summary>
/// 核心提供的属性加成（纯数值，无特效）
/// </summary>
[Serializable]
public struct CoreStatBonus
{
    public StatDefinition statDefinition;
    public float additiveBonus;       // 加法加成 (如 +10 伤害)
    public float multiplicativeBonus; // 乘法加成 (如 0.2 = +20%)

    public string StatId => statDefinition != null ? statDefinition.StatId : string.Empty;

    public bool Matches(string statId)
    {
        return statDefinition != null &&
               !string.IsNullOrWhiteSpace(statId) &&
               statDefinition.Matches(statId);
    }
}
