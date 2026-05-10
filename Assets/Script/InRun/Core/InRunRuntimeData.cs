using System;
using System.Collections.Generic;

[Serializable]
public class InRunRuntimeSaveData
{
    // 本局运行的随机种子，用于主题抽取、奖励抽取等可复现随机流程。
    public int runSeed;

    // 当前进行到第几个主题，0-based；-1 表示还未进入任何主题。
    public int currentThemeIndex = -1;

    // 当前进行到主题内的第几个小循环，0-based；-1 表示当前不在小循环中。
    public int currentLoopIndex = -1;

    // 当前 In-Run 主状态机所处阶段。
    public InRunPhase phase = InRunPhase.None;

    // 本局已选出的主题 ID 列表，顺序对应主题流程顺序。
    public List<string> selectedThemeIds = new();

    // 每个主题对应的运行期存档数据。
    public List<ThemeRuntimeSaveData> themes = new();

    // 本局当前可在商店消费的货币。
    public int runCurrency;

    // 本局累计总分，用于最终结算或调试观察。
    public int runScoreTotal;

    // 本局累计击杀数。
    public int lifetimeKillsThisRun;

    // 本局已获得但尚未转化为局外结果的运行期奖励列表。
    public List<RunRewardSaveData> pendingRewards = new();
}

[Serializable]
public class ThemeRuntimeSaveData
{
    // 当前主题的配置 ID。
    public string themeId;

    // 当前主题的 Boss 是否已被击败。
    public bool bossDefeated;

    // 当前主题中已经“正式登场/解锁展示”过的敌人 ID。
    public List<string> introducedEnemyIds = new();

    // 当前主题下各个小循环的存档数据。
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
}
