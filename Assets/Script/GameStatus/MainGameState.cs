using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameState : GameState
{
    private bool isSettingOpen = false;
    private bool isModuleSelectOpen = false;
    private bool isLevelUpOpen = false;

    private LevelUpUI levelUpUI;

    public override void OnEnter()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.PlayBGM("FightBGM_2");
        StartGame();
        UIManager.Instance.OpenFullScreen<HUDUI>();
    }

    public override void OnExit()
    {
        // 清理事件
        if (ModuleSelectManager.Instance != null)
        {
            ModuleSelectManager.Instance.OnShowAbilitySelectUI -= HandleShowAbilityUI;
        }
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
        else if(Input.GetKeyDown(KeyCode.Tab) && UIManager.Instance.CheckUIOpen<LevelUpUI>())
        {
            UIManager.Instance.CloseTopPanel();
        }
    }


    private void StartGame()
    {
        PlayerManager.Instance.SpawnPlayer();
        EventManager.AddListener<ModuleType, StatType>(GameEvent.ModuleUpgrade, OnModuleUpgrade);

        WaveManager.Instance.OnWaveIncoming += (level, txt) =>
        {
            if (txt != "")
            {
                MessageUIArg arg = new MessageUIArg(level, txt);
                UIManager.Instance.OpenPopup<MessageUI>(arg);
            }
        };

        //UpgradeManager.Instance.SyncWithPlayerManager();
        ModuleSelectManager.Instance.OnShowAbilitySelectUI += HandleShowAbilityUI;
        GameManager.Instance.StartCoroutine(WaveManager.Instance.GameLoopRoutine());
    }

    private void HandleShowAbilityUI(int level, List<UpgradeOption> candidates)
    {
        var uiArgs = Tuple.Create(level, candidates);
        if (!isModuleSelectOpen)
        {
            UIManager.Instance.OpenPopup<ModuleSelectUI>(uiArgs);
        }
        isModuleSelectOpen = true;
    }

    private void ToggleLevelUpPanel()
    {
        if (isSettingOpen) return;
        if (!isLevelUpOpen)
        {
            levelUpUI = UIManager.Instance.Open<LevelUpUI>();
            Time.timeScale = 0f;
            isLevelUpOpen = true;
        }
        else
        {
            UIManager.Instance.CloseUI(levelUpUI);
            Time.timeScale = 1f;
            isLevelUpOpen = false;
        }
    }

    //private void ToggleModuleSelectPanel()
    //{
    //    if (isSettingOpen) return;

    //    if (!isModuleSelectOpen)
    //    {
    //        if (UpgradeManager.Instance.UpgradePoints <= 0)
    //        {
    //            Debug.LogWarning("没有可用的升级点数！");
    //            return;
    //        }

    //        var options = ModuleSelectManager.Instance.GetOrCreateOptions();
    //        ModuleSelectManager.Instance.OnShowAbilitySelectUI?.Invoke(
    //            UpgradeManager.Instance.CurrentLevel,
    //            options
    //        );

    //        Time.timeScale = 0f;
    //        isModuleSelectOpen = true;
    //    }
    //    else
    //    {
    //        ModuleSelectUI moduleSelectUI = FindModuleSelectUIInPopupLayer();
    //        if (moduleSelectUI != null)
    //        {
    //            UIManager.Instance.CloseUI(moduleSelectUI);
    //        }

    //        Time.timeScale = 1f;
    //        isModuleSelectOpen = false;
    //    }
    //}


    //private ModuleSelectUI FindModuleSelectUIInPopupLayer()
    //{
    //    GameObject canvasObj = GameObject.Find("Canvas");
    //    if (canvasObj == null) return null;

    //    Transform layerPopup = canvasObj.transform.Find("Layer_Popup");
    //    if (layerPopup == null) return null;

    //    foreach (Transform child in layerPopup)
    //    {
    //        ModuleSelectUI ui = child.GetComponent<ModuleSelectUI>();
    //        if (ui != null)
    //        {
    //            return ui;
    //        }
    //    }

    //    return null;
    //}

    private bool isPauseOpen = false;

    private void TogglePausePanel()
    {
        if (!isPauseOpen)
        {
            UIManager.Instance.Open<PauseUI>();
            Time.timeScale = 0f;
            isPauseOpen = true;
        }
        else
        {
            UIManager.Instance.CloseTopPanel();
            Time.timeScale = 1f;
            isPauseOpen = false;
        }
    }

    private void OnModuleUpgrade(ModuleType moduleType, StatType statType)
    {
        Debug.Log($"模块升级: {moduleType}, 属性: {statType}");
        PlayerManager.Instance.CurrentModules.GetModule<PlayerModule>(moduleType).UpgradeModule(moduleType, statType);
    }
}