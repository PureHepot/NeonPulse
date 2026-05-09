using System;
using System.Collections.Generic;

// ============================================================
// 存档数据结构 — 所有可序列化的游戏状态都定义在此文件中
// DataManager 持有 SaveRoot，通过 SaveService 读写 JSON
// ============================================================

/// <summary>
/// 存档根节点
/// </summary>
[Serializable]
public class SaveRoot
{
    public int version = 1;
    public SettingsData settings = new();
    public MetaProgressData meta = new();
    public RunSaveData currentRun; // null = 无进行中的 Run
}

// ======================== 设置 ========================

[Serializable]
public class SettingsData
{
    public float bgmVolume = 1f;
    public float effectVolume = 1f;
    public bool isBgmMuted;
    public bool isEffectMuted;
}

// ======================== 场外进度 ========================

[Serializable]
public class MetaProgressData
{
    public List<ModuleType> unlockedModules = new();
    public List<ModData> mods = new();
    public int softCurrency;
    public int totalRunsPlayed;
    public int bestWaveReached;
}

// ======================== 单局存档 ========================

[Serializable]
public class RunSaveData
{
    public bool hasActiveRun;
    public int runSeed;

    public PlayerRunData player = new();
    public ProgressionRunData progression = new();
    public BuildRunData build = new();
    public WaveRunData wave = new();
    public WorldRunData world = new();
}

[Serializable]
public class PlayerRunData
{
    public int currentHp;
    public int maxHp;
    public float posX;
    public float posY;
}

[Serializable]
public class ProgressionRunData
{
    public int level = 1;
    public int exp;
    public int upgradePoints;
    public int score;
}

[Serializable]
public class BuildRunData
{
    public List<ModuleType> startingModules = new();
    public List<OwnedModuleRunData> ownedModules = new();
    public string currentMaskName;
}

[Serializable]
public class OwnedModuleRunData
{
    public ModuleType ModuleType;
    public List<StatLevelData> statLevels = new();
}

[Serializable]
public class StatLevelData
{
    public StatType statType;
    public int level;
}

[Serializable]
public class WaveRunData
{
    public int currentWaveIndex;
    public float elapsedTimeInWave;
}

[Serializable]
public class WorldRunData
{
    public string backgroundThemeId;
    public bool isGameOver;
    public bool isVictory;
}
public class ModData
{
    public ModType modType;
    public int modCount;
}
