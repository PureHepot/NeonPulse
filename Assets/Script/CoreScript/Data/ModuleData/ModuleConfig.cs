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
    Damage,         // 伤害
    FireRate,       // 射速
    ShooterCount,   // 射口数量

    //BeamModule
    BeamRange,      // 射程
    BeamCount,
    BeamCooldown,

    //Dash
    DashCooldown    // 突进冷却

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
    public int maxStacks = -1;//-1为无限升级

    [Header("UI Info")]
    public string upgradeName;//名称
    public string description;//描述
}

[CreateAssetMenu(fileName = "NewModuleConfig", menuName = "Game/Module Config")]
public class ModuleConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string moduleName;
    public ModuleType moduleType;
    //public Sprite icon;

    [Header("Base Stats")]
    public List<StatData> baseStats;

    [Header("Unlock Settings")]
    public int unlockLevel = 1;

    [Header("Upgrade Definitions")]
    public List<StatUpgradeDefinition> statUpgrades;

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
}
