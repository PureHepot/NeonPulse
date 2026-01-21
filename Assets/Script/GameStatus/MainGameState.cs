using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameState : GameState
{
    private bool isSettingOpen = false;

    public override void OnEnter()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.PlayBGM("FightBGM");
        string Id = "1001";
        string name = CSVManager.Instance.GetValue("UpgradeData", Id, "Name");
        Debug.Log("Loaded Upgrade Name: " + name);

        StartGame();
        UIManager.Instance.OpenFullScreen<ExpBarUI>();
    }

    public override void OnExit()
    {
        if (PlayerAbilitySelectManager.Instance != null)
        {
            PlayerAbilitySelectManager.Instance.OnShowAbilitySelectUI -= HandleShowAbilityUI;
        }
    }

    public override void OnUpdate()
    {
        // 监听 ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingPanel();
        }
    }


    private void StartGame()
    {
        //玩家载入
        PlayerManager.Instance.SpawnPlayer();
        EventManager.AddListener<ModuleType, StatType>(GameEvent.ModuleUpgrade, OnModuleUpgrade);


        //设置事件
        WaveManager.Instance.OnWaveIncoming += (level, txt) =>
        {
            MessageUIArg arg = new MessageUIArg(level, txt);
            UIManager.Instance.OpenPopup<MessageUI>(arg);
        };

        UpgradeManager.Instance.SyncWithPlayerManager();

        PlayerAbilitySelectManager.Instance.OnShowAbilitySelectUI += HandleShowAbilityUI;

        GameManager.Instance.StartCoroutine(WaveManager.Instance.GameLoopRoutine());
    }

    private void HandleShowAbilityUI(int level, List<ModuleType> candidates)
    {
        // 封装参数，传给UI
        var uiArgs = Tuple.Create(level, candidates);
        // 打开升级选能力UI
        UIManager.Instance.OpenPopup<LevelUpAbilitySelectUI>(uiArgs);
    }

    private void ToggleSettingPanel()
    {
        if (!isSettingOpen)
        {
            // 打开设置面板
            UIManager.Instance.Open<SetVolumeUI>();
            Time.timeScale = 0f;   // 暂停游戏
            isSettingOpen = true;
        }
        else
        {
            // 关闭设置面板
            UIManager.Instance.CloseTopPanel();
            Time.timeScale = 1f;
            isSettingOpen = false;
        }
    }

    private void OnModuleUpgrade(ModuleType moduleType, StatType statType)
    {
        Debug.Log($"模块升级: {moduleType}, 属性: {statType}");
        PlayerManager.Instance.CurrentModules.GetModule<PlayerModule>(moduleType).UpgradeModule(moduleType, statType);
    }
}
