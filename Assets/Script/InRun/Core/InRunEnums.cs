public enum InRunPhase
{
    None,
    Bootstrap,
    ThemeSelecting,
    ThemeIntro,
    CombatLoopPreparing,
    CombatLoopActive,
    CombatLoopComplete,
    PulseReady,
    PulseResolving,
    LoopReward,
    Shop,
    BossPreparing,
    BossActive,
    BossReward,
    NextTheme,
    FinalSettlement,
    RunEnded
}

public enum CombatGrade
{
    F,
    D,
    C,
    B,
    A,
    S,
    SS,
    SSS
}

public enum InRunItemType
{
    Currency,
    Module,
    Plugin,
    Core,
    Frame,
    Consumable,
    WarehouseExpansion,
    MapExpansion,
    Repair,
    Misc
}
