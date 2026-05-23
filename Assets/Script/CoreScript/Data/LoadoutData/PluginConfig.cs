using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 配件/插件配置 (ScriptableObject)
// 配件 = 特殊效果，不做纯数值加成
// 每个模块可装多个配件（数量由模块品质决定）
// ============================================================

[CreateAssetMenu(fileName = "NewPluginConfig", menuName = "Game/Plugin Config")]
public class PluginConfig : ScriptableObject
{
    [Header("基本信息")]
    public string pluginId;        // 如 "Plugin_ChainLightning"
    public string displayName;     // 如 "连锁闪电"
    [TextArea] public string description;
    public PluginType pluginType;
    public Sprite icon;
    public int load;

    [Header("特殊效果")]
    [Tooltip("效果标识符，用于战斗系统触发（如 ChainLightning, LifeSteal）")]
    public string effectId;
    [Tooltip("效果在各品质下的参数")]
    public PluginEffectRarityValues effectRarityValues;
    
    [Header("数值修正")]
    [Tooltip("插件可按品质提供基础数值修正，供新 loadout/runtime 系统直接消费")]
    public List<PluginStatModifierProfile> statModifierProfiles = new List<PluginStatModifierProfile>();

    [Header("适配限制")]
    [Tooltip("旧兼容字段：该配件只能插入这些模块类型（空=不限）")]
    public List<ModuleType> restrictedToModules = new List<ModuleType>();
    [Tooltip("新规则：该配件只能插入这些模块分类（None=不限）")]
    public ModuleCategory restrictedToCategories = ModuleCategory.None;

    [Header("掉落权重")]
    public int dropWeight = 100;

    // ==================== 运行时辅助 ====================

    /// <summary>
    /// 获取指定品质下的效果参数
    /// </summary>
    public PluginEffectParams GetEffectParams(PluginRarity rarity)
    {
        return effectRarityValues.GetParams(rarity);
    }

    public int GetLoadCost()
    {
        return Mathf.Max(0, load);
    }

    public List<PluginStatModifier> GetStatModifiers(PluginRarity rarity)
    {
        if (statModifierProfiles == null)
            return null;

        foreach (var profile in statModifierProfiles)
        {
            if (profile != null && profile.rarity == rarity)
                return profile.statModifiers;
        }

        return null;
    }

    /// <summary>
    /// 该配件是否可以插入指定模块
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
/// 配件效果在各品质下的参数定义
/// 设计师可以为每个品质单独调参
/// </summary>
[Serializable]
public class PluginEffectRarityValues
{
    public PluginEffectParams @common;
    public PluginEffectParams uncommon;
    public PluginEffectParams rare;
    public PluginEffectParams epic;
    public PluginEffectParams legendary;

    public PluginEffectParams GetParams(PluginRarity rarity)
    {
        return rarity switch
        {
            PluginRarity.Common    => @common,
            PluginRarity.Uncommon  => uncommon,
            PluginRarity.Rare      => rare,
            PluginRarity.Epic      => epic,
            PluginRarity.Legendary => legendary,
            _ => @common
        };
    }
}

/// <summary>
/// 配件效果参数（不同配件含义不同，由战斗系统解读）
/// 例：连锁闪电 → param1=弹射次数, param2=伤害衰减比例
/// 例：生命偷取 → param1=偷取比例, param2=0
/// </summary>
[Serializable]
public struct PluginEffectParams
{
    public float param1;
    public float param2;
    public float param3;  // 预留第三个参数
}

[Serializable]
public class PluginStatModifierProfile
{
    public PluginRarity rarity;
    public List<PluginStatModifier> statModifiers = new List<PluginStatModifier>();
}

[Serializable]
public struct PluginStatModifier
{
    public StatDefinition statDefinition;
    public float additiveBonus;
    public float multiplicativeBonus;

    public string StatId => statDefinition != null ? statDefinition.StatId : string.Empty;

    public bool Matches(string statId)
    {
        return statDefinition != null &&
               !string.IsNullOrWhiteSpace(statId) &&
               statDefinition.Matches(statId);
    }
}
