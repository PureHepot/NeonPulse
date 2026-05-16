// ============================================================
// 装配系统枚举定义
// 框架(具名插槽) → 模块(核心+配件) 的层级体系
// 核心 = 数值加成（每模块1个）
// 配件 = 特殊效果（每模块可多个）
// ============================================================

/// <summary>
/// 模块分类标签：一个模块可属于多个分类
/// 决定了哪些框架插槽能装备此模块
/// </summary>
[System.Flags]
public enum ModuleCategory
{
    None      = 0,
    Weapon    = 1 << 0,   // 武器类
    Ranged    = 1 << 1,   // 远程
    Melee     = 1 << 2,   // 近战
    Mech      = 1 << 3,   // 机械
    Defense   = 1 << 4,   // 防御类（护盾、血量等）
    Utility   = 1 << 5,   // 辅助类
    Movement  = 1 << 6,   // 移动类
    Health    = 1 << 7    // 生命类
}

/// <summary>
/// 模块品质：影响配件插槽数量（核心始终只有1个）
/// </summary>
public enum ModuleRarity
{
    Common      = 0,   // 1 配件槽
    Uncommon    = 1,   // 2 配件槽
    Rare        = 2,   // 3 配件槽
    Epic        = 3,   // 4 配件槽
    Legendary   = 4,   // 5 配件槽
}

/// <summary>
/// 核心类型：核心只做数值加成，无特殊效果
/// 每个模块只能装1个核心
/// </summary>
public enum CoreType
{
    None = 0,

    // === 攻击类 ===
    DamageBoost,       // 伤害提升
    FireRateBoost,     // 射速提升
    CritChance,        // 暴击率
    CritDamage,        // 暴击伤害
    Penetration,       // 穿透

    // === 防御类 ===
    HPBoost,           // 生命值提升
    ShieldBoost,       // 护盾容量提升
    DamageReduction,   // 伤害减免
    RegenBoost,        // 回复提升

    // === 功能类 ===
    SpeedBoost,        // 移速提升
    DashBoost,         // 冲刺增强
    CooldownReduction, // 冷却缩减
}

/// <summary>
/// 配件（插件）类型：配件实现特殊效果
/// 每个模块可装多个配件（数量由品质决定）
/// </summary>
public enum PluginType
{
    None = 0,

    // === 攻击特效 ===
    ExtraMuzzle,       // 增加额外枪口/额外发射点
    LifeSteal,         // 生命偷取
    ChainLightning,    // 连锁闪电（攻击附带弹射）
    ExplosiveHit,      // 爆破打击（攻击附带范围伤害）
    FrostSlow,         // 冰霜减速（攻击附带减速）
    Homing,            // 追踪（投射物自动追踪）
    Ricochet,          // 弹射（投射物弹射到附近敌人）

    // === 防御特效 ===
    Thorns,            // 荆棘（受击反弹伤害）
    ShieldOnHit,       // 受击生成临时护盾
    DodgeChance,       // 闪避概率

    // === 功能特效 ===
    AreaOfEffect,      // 效果范围扩大
    OnKillExplosion,   // 击杀爆炸
    OnKillHeal,        // 击杀回血
    OnCritStun,        // 暴击眩晕
    ReflectProjectiles // 反弹敌方投射物（优先给近战/防御模块使用）
}

/// <summary>
/// 配件品质：影响特效参数的强度
/// </summary>
public enum PluginRarity
{
    Common      = 0,
    Uncommon    = 1,
    Rare        = 2,
    Epic        = 3,
    Legendary   = 4,
}
