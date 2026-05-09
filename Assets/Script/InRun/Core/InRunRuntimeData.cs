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
