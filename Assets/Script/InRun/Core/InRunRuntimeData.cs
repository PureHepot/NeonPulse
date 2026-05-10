using System;
using System.Collections.Generic;

[Serializable]
public class InRunRuntimeSaveData
{
    // 本局运行的随机种子。
    public int runSeed;

    // 当前主题序号，0-based；-1 表示尚未进入主题。
    public int currentThemeIndex = -1;

    // 当前小循环序号，0-based；-1 表示当前不在小循环中。
    public int currentLoopIndex = -1;

    // 当前 InRun 主状态机阶段。
    public InRunPhase phase = InRunPhase.None;

    // 本局已选出的主题 ID 顺序。
    public List<string> selectedThemeIds = new();

    // 每个主题对应的运行期状态。
    public List<ThemeRuntimeSaveData> themes = new();

    // 本局当前可在商店消费的货币。
    public int runCurrency;

    // 本局累计总分，用于最终结算或调试观察。
    public int runScoreTotal;

    // 本局累计击杀数。
    public int lifetimeKillsThisRun;

    // 本局临时仓库数据。
    public WarehouseRuntimeSaveData warehouse = new();

    // 当前商店商品快照。
    public ShopInventoryRuntimeSaveData shopInventory = new();

    // 本局已获得但尚未转化为局外结果的运行期奖励列表。
    public List<RunRewardSaveData> pendingRewards = new();
}

[Serializable]
public class ThemeRuntimeSaveData
{
    // 当前主题的配置 ID。
    public string themeId;

    // 当前主题 Boss 是否已被击败。
    public bool bossDefeated;

    // 当前主题中已经正式登场过的敌人 ID。
    public List<string> introducedEnemyIds = new();

    // 当前主题下各小循环的存档数据。
    public List<CombatLoopRuntimeSaveData> loops = new();
}

[Serializable]
public class CombatLoopRuntimeSaveData
{
    // 小循环序号，0-based。
    public int loopIndex;

    // 当前小循环已进行的时间，单位秒。
    public float elapsedSeconds;

    // 本小循环原始得分，不受商店消费影响。
    public int loopScoreRaw;

    // 本小循环结算后给予的商店货币增量。
    public int loopCurrencyGain;

    // 本小循环击杀数。
    public int killCount;

    // 本小循环内达到过的最高连杀数。
    public int highestCombo;

    // 本小循环内达到过的最高倍率。
    public float highestMultiplier = 1f;

    // 本小循环结算评级。
    public CombatGrade grade = CombatGrade.F;

    // 本小循环是否已经触发过脉冲结算。
    public bool pulseUsed;

    // 本小循环奖励是否已经领取完成。
    public bool rewardClaimed;

    // 本小循环商店是否已经完成并离开。
    public bool shopCompleted;
}

[Serializable]
public class RunRewardSaveData
{
    // 奖励唯一 ID。
    public string rewardId;

    // 奖励显示名称。
    public string displayName;

    // 奖励说明文本。
    public string description;

    // 奖励来源，例如 LoopReward / Shop / BossReward。
    public string source;

    // 该奖励附带的额外货币收益。
    public int currencyBonus;

    // 奖励对应的局内物品类型。
    public InRunItemType itemType = InRunItemType.Misc;

    // 奖励对应的具体内容标识，例如 moduleId / coreId / pluginId。
    public string itemId;

    // 奖励带来的仓库容量变化。
    public int warehouseSlotsDelta;
}

[Serializable]
public class WarehouseRuntimeSaveData
{
    // 本局运行期仓库容量。
    public int capacity = 12;

    // 当前已入仓的运行期物品列表。
    public List<WarehouseItemSaveData> items = new();
}

[Serializable]
public class WarehouseItemSaveData
{
    // 仓库条目唯一标识。
    public string rewardId;

    // 物品类型。
    public InRunItemType itemType = InRunItemType.Misc;

    // 物品运行期标识，例如 moduleId / coreId / pluginId。
    public string itemId;

    // 显示名称。
    public string displayName;

    // 描述文本。
    public string description;

    // 来源，例如 LoopReward / Shop / BossReward。
    public string source;
}

[Serializable]
public class ShopInventoryRuntimeSaveData
{
    // 当前商店目录标识。
    public string catalogId;

    // 当前商店商品快照。
    public List<ShopOfferSaveData> offers = new();
}

[Serializable]
public class ShopOfferSaveData
{
    // 商品唯一 ID。
    public string offerId;

    // 商品显示名。
    public string displayName;

    // 商品说明。
    public string description;

    // 商品价格。
    public int cost;

    // 商品类型。
    public InRunItemType itemType = InRunItemType.Misc;

    // 商品内容标识。
    public string itemId;

    // 商品带来的仓库容量变化。
    public int warehouseSlotsDelta;

    // 是否已被购买。
    public bool purchased;
}
