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
        AudioManager.Instance.PlayBGM("FightBGM_2");
        StartGame();
        UIManager.Instance.Open<ExpBarUI>();
        UIManager.Instance.Open<HpBarUI>();
    }

    public override void OnExit()
    {
        if (ModuleSelectManager.Instance != null)
        {
            ModuleSelectManager.Instance.OnShowAbilitySelectUI -= HandleShowAbilityUI;
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

        ModuleSelectManager.Instance.OnShowAbilitySelectUI += HandleShowAbilityUI;

        GameManager.Instance.StartCoroutine(WaveManager.Instance.GameLoopRoutine());
    }

    private void HandleShowAbilityUI(int level, List<UpgradeOption> candidates)
    {
        var uiArgs = Tuple.Create(level, candidates);
        // 打开升级选能力UI
        UIManager.Instance.OpenPopup<ModuleSelectUI>(uiArgs);
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
