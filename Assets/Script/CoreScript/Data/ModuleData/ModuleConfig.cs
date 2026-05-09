using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    None,

    //HealthModule
    MaxHP,          // 最大生命
    HealthRegen,    // 生命恢复

    //MovementModule
    MoveSpeed,      // 移动速度

    //ShieldModule
    ShieldCapacity, // 护盾容量
    ShiledRegen,    // 护盾恢复速度
    ShieldKnockback,// 击退力度

    //ShooterModule
    BaseDamage,         // 伤害
    DamageRateMultiplier,
    BaseFireRate,       // 射速
    FireRateMultiplier,
    ShooterCount,   // 射口数量

    //LaserDroneModule
    BeamRange,      // 射程
    BeamCount,
    BeamCooldown,
    BeamPerTick,

    //Dash
    DashCooldown,   // 突进冷却
    DashForce,

    //SniperModule
    SnipeDamage,    // 伤害
    SnipeFireRate,  // 射速
    SnipePenetration, // 穿透次数

    //ShotgunModule
    ShotgunDamage,
    ShotgunFireRate,
    ShotgunPelletCount,   // 弹丸数量
    ShotgunSpreadAngle,   // 扩散角度

    //SawBladeModule
    BladeBaseDamage,
    BladeChargeTime,
    BladeHitCount,

    InvinciDuration,


    //MagnetModule
    MagnetRange,
    MagnetControlTime,
    MagnetCooldown,
    //OddMovementModule
    OddMoveSpeed,
    //诸如此类
}

[Serializable]
public struct StatData
{
    public StatType type;
    public float value;
}

[Serializable]
public class StatUpgradeDefinition
{
    public StatType statType;//属性
    public float valuePerUpgrade;//数值
    public int pointCost = 1;
    public int maxStacks = -1;//-1为无限升级

    [Header("UI Info")]
    public string upgradeName;//名称
    public string description;//描述
}

[CreateAssetMenu(fileName = "NewModuleConfig", menuName = "Game/Module Config")]
public class ModuleConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string ModuleName;
    public ModuleType ModuleType;
    //public Sprite icon;

    [Header("Module Prefab")]
    [Tooltip("该模块对应的预制体，根节点需挂有 PlayerModule 组件")]
    public GameObject prefab;
    public GameObject uiPreviewPrefab; // 可选：用于 UI 预览的附加物体（如无人机）
    public bool hasVisualEffectInUI => uiPreviewPrefab != null;

    [Header("Base Stats")]
    public List<StatData> baseStats;

    [Header("Unlock Settings")]
    public int unlockLevel = 1;

    [Header("Upgrade Definitions")]
    public List<StatUpgradeDefinition> statUpgrades;

    [Header("Visual Settings")]
    public Color themeColor = Color.cyan;

    public float GetBaseStat(StatType type)
    {
        foreach (var stat in baseStats)
        {
            if (stat.type == type) return stat.value;
        }
        return 0f;
    }

    public StatUpgradeDefinition GetUpgradeDefinition(StatType type)
    {
        foreach (var def in statUpgrades)
        {
            if (def.statType == type) return def;
        }
        return null;
    }

    public string GetDescription(StatType type)
    {
        var def = GetUpgradeDefinition(type);
        if (def != null)
        {
            return def.description;
        }
        return string.Empty;
    }
}
