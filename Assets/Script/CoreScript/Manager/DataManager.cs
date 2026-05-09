using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoSingleton<DataManager>
{
    private SaveRoot saveRoot;

    // ==================== 快捷访问器 ====================

    public MetaProgressData Meta => saveRoot.meta;
    public RunSaveData Run => saveRoot.currentRun;
    public SettingsData Settings => saveRoot.settings;
    public bool HasActiveRun => Run != null && Run.hasActiveRun;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);
        saveRoot = SaveService.Load() ?? new SaveRoot();
    }

    // ==================== Run 生命周期 ====================

    /// <summary>
    /// 开始新一局游戏
    /// </summary>
    public void StartNewRun(int seed, List<ModuleType> startingModules)
    {
        saveRoot.currentRun = new RunSaveData
        {
            hasActiveRun = true,
            runSeed = seed,
            player = new PlayerRunData(),
            progression = new ProgressionRunData { level = 1 },
            build = new BuildRunData
            {
                startingModules = new List<ModuleType>(startingModules)
            },
            wave = new WaveRunData(),
            world = new WorldRunData()
        };
        ModManager.Instance.AddMod(ModType.PenetrateMod);
        ModManager.Instance.AddMod(ModType.PenetrateMod);
        ModManager.Instance.AddMod(ModType.ReflectMod);
        Meta.totalRunsPlayed++;
        Save();
    }

    /// <summary>
    /// 结束当前局（死亡或胜利），结算奖励并清除 Run
    /// </summary>
    public void EndRun(bool victory, int waveReached)
    {
        if (Run == null) return;

        // 结算货币：基于波次给奖励，胜利额外加成
        int reward = waveReached * 10;
        if (victory) reward *= 2;
        Meta.softCurrency += reward;
        ModManager.Instance.AddMod(ModType.ReflectMod);

        // 更新最高波次记录
        if (waveReached > Meta.bestWaveReached)
            Meta.bestWaveReached = waveReached;

        // 清除当前 Run
        saveRoot.currentRun = null;
        Save();
    }

    // ==================== Score 管理 ====================

    public int Score
    {
        get => Run != null ? Run.progression.score : 0;
        set
        {
            if (Run == null) return;
            Run.progression.score = value;
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        EventManager.Broadcast(GameEvent.PlayerScore, Score);
    }

    // ==================== IsGameOver 兼容 ====================

    public bool IsGameOver
    {
        get => Run != null && Run.world.isGameOver;
        set
        {
            if (Run == null) return;
            Run.world.isGameOver = value;
        }
    }

    // ==================== 存档 API ====================

    public void Save()
    {
        SaveService.Save(saveRoot);
    }

    public void ResetAll()
    {
        saveRoot = new SaveRoot();
        Save();
    }

    // ==================== 自动保存 ====================

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
