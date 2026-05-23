using System;
using System.Collections.Generic;

[Serializable]
public class InRunRuntimeSaveData
{
    public int runSeed;
    public int currentThemeIndex = -1;
    public int currentLoopIndex = -1;
    public InRunPhase phase = InRunPhase.None;
    public List<string> selectedThemeIds = new();
    public List<ThemeRuntimeSaveData> themes = new();
    public int runCurrency;
    public int runScoreTotal;
    public int lifetimeKillsThisRun;
    public int bossDefeatCount;
    public WarehouseRuntimeSaveData warehouse = new();
    public ShopInventoryRuntimeSaveData shopInventory = new();
    public List<RunRewardSaveData> pendingRewards = new();
}

[Serializable]
public class ThemeRuntimeSaveData
{
    public string themeId;
    public bool bossDefeated;
    public List<string> introducedEnemyIds = new();
    public List<CombatLoopRuntimeSaveData> loops = new();
}

[Serializable]
public class CombatLoopRuntimeSaveData
{
    public int loopIndex;
    public float elapsedSeconds;
    public int loopScoreRaw;
    public int loopCurrencyGain;
    public int killCount;
    public int highestCombo;
    public float highestMultiplier = 1f;
    public CombatGrade grade = CombatGrade.F;
    public bool pulseUsed;
    public bool rewardClaimed;
    public bool shopCompleted;
}

[Serializable]
public class RunRewardSaveData
{
    public string rewardId;
    public string displayName;
    public string description;
    public string source;
    public int currencyBonus;
    public InRunItemType itemType = InRunItemType.Misc;
    public string itemId;
    public int warehouseSlotsDelta;
}

[Serializable]
public class WarehouseRuntimeSaveData
{
    public int capacity = 12;
    public List<WarehouseItemSaveData> items = new();
}

[Serializable]
public class WarehouseItemSaveData
{
    public string rewardId;
    public InRunItemType itemType = InRunItemType.Misc;
    public string itemId;
    public string displayName;
    public string description;
    public string source;
}

[Serializable]
public class ShopInventoryRuntimeSaveData
{
    public string catalogId;
    public List<ShopOfferSaveData> offers = new();
}

[Serializable]
public class ShopOfferSaveData
{
    public string offerId;
    public string displayName;
    public string description;
    public int cost;
    public InRunItemType itemType = InRunItemType.Misc;
    public string itemId;
    public int warehouseSlotsDelta;
    public bool purchased;
}
