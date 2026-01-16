using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartUI : UIBase
{
    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        InitBtn();
    }

    private void InitBtn()
    {
        Get<Button>("Start").onClick.SetListener(() =>
        {
           // 开始游戏逻辑
            Debug.Log("开始游戏");

            Action action = () =>
            {
                // 在加载完成后切换游戏状态
                GameManager.Instance.ChangeState(new MainGameState());
            };  

            UIManager.Instance.Open<LoadingUI>(action);
        });

        Get<Button>("Setting").onClick.SetListener(() =>
        {
            // 打开设置界面逻辑
            Debug.Log("打开设置界面");
            UIManager.Instance.Open<SetVolumeUI>();
        });

        Get<Button>("Exit").onClick.SetListener(() =>
        {
            // 退出游戏逻辑
            Debug.Log("退出游戏");
        }); 
    }


    public override void OnClose()
    {
        base.OnClose();
    }
}
