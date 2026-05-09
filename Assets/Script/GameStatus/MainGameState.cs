using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameState : GameState
{
    private readonly bool isContinue;

    public MainGameState(bool isContinue = false)
    {
        this.isContinue = isContinue;
    }

    public override void OnEnter()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.PlayBGM("FightBGM_2");

        if (isContinue)
        {
            // 继续游戏：从存档恢复各 Manager 状态
            UpgradeManager.Instance.InitFromSaveData();
            WaveManager.Instance.InitFromSaveData();
            MaskSystemManager.Instance.InitFromSaveData();
        }
        else
        {
            // 新游戏：创建新 Run
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            var startingModules = UpgradeManager.Instance.startingModules;
            DataManager.Instance.StartNewRun(seed, startingModules);
            UpgradeManager.Instance.InitNewRun();
        }

        StartGame();
        UIManager.Instance.OpenFullScreen<HUDUI>();
    }

    public override void OnExit()
    {
        // 存档：退出战斗状态前保存一次
        SaveAll();

        EventManager.RemoveListener<ModuleType, StatType>(GameEvent.ModuleUpgrade, OnModuleUpgrade);
        Time.timeScale = 1f;
    }

    public override void OnUpdate()
    {
        HandleUIEvent();
    }

    private void HandleUIEvent()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.Instance.CheckUIListEmpty())
            {
                // 暂停时存档
                SaveAll();
                UIManager.Instance.Open<PauseUI>();
            }
            else
            {
                UIManager.Instance.CloseTopPanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !UIManager.Instance.CheckUIOpen<LevelUpUI>())
        {
            UIManager.Instance.Open<LevelUpUI>();
            UIManager.Instance.ClosePopup();
        }
        else if (Input.GetKeyDown(KeyCode.Tab) && UIManager.Instance.CheckUIOpen<LevelUpUI>())
        {
            UIManager.Instance.CloseTopPanel();
        }
        if (Input.GetKeyDown(KeyCode.G) && !UIManager.Instance.CheckUIOpen<ModEquipUI>())
        {
            UIManager.Instance.Open<ModEquipUI>();
            UIManager.Instance.ClosePopup();
        }
        else if (Input.GetKeyDown(KeyCode.G) && UIManager.Instance.CheckUIOpen<ModEquipUI>())
        {
            UIManager.Instance.CloseTopPanel();
        }
    }

    private void StartGame()
    {
        PlayerManager.Instance.SpawnPlayer();
        EventManager.AddListener<ModuleType, StatType>(GameEvent.ModuleUpgrade, OnModuleUpgrade);

        WaveManager.Instance.OnWaveIncoming += OnWaveIncoming;
        WaveManager.Instance.OnAllWavesCleared += OnVictory;

        GameManager.Instance.StartCoroutine(WaveManager.Instance.GameLoopRoutine());
    }

    private void OnWaveIncoming(int level, string txt)
    {
        // 每次新波次开始时存档（上一波结束）
        SaveAll();

        if (txt != "")
        {
            MessageUIArg arg = new MessageUIArg(level, txt);
            UIManager.Instance.OpenPopup<MessageUI>(arg);
        }
    }

    private void OnVictory()
    {
        int waveReached = WaveManager.Instance.currentWaveIndex;
        DataManager.Instance.EndRun(true, waveReached);
    }

    private void OnModuleUpgrade(ModuleType ModuleType, StatType statType)
    {
        Debug.Log($"模块升级: {ModuleType}, 属性: {statType}");
        PlayerManager.Instance.CurrentModules.GetModule<PlayerModule>(ModuleType).UpgradeModule(ModuleType, statType);
    }

    /// <summary>
    /// 统一存档：收集所有 Manager 状态后写入磁盘
    /// </summary>
    private void SaveAll()
    {
        if (!DataManager.Instance.HasActiveRun) return;

        PlayerManager.Instance.SavePlayerState();
        UpgradeManager.Instance.SyncToSaveData();
        WaveManager.Instance.SyncToSaveData();
        DataManager.Instance.Save();
    }
}
