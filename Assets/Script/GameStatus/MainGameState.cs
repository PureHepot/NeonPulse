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
        StartGame();
    }

    public override void OnExit()
    {
        
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

        //设置事件
        WaveManager.Instance.OnWaveIncoming += (level, txt) =>
        {
            MessageUIArg arg = new MessageUIArg(level, txt);
            UIManager.Instance.OpenPopup<MessageUI>(arg);
        };

        GameManager.Instance.StartCoroutine(WaveManager.Instance.GameLoopRoutine());
    }

    private void ToggleSettingPanel()
    {
        if (!isSettingOpen)
        {
            // 打开设置面板
            UIManager.Instance.Open<SetVolumeUI>();
            Time.timeScale = 0f;   // 可选：暂停游戏
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
}
