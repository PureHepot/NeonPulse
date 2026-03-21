using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StartUI : UIBase
{
    private bool hasSave;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        hasSave = args is bool b && b;
        InitBtn();
    }

    private void InitBtn()
    {
        // 继续游戏按钮（有存档时显示）
        var continueBtn = Get<Button>("Continue");
        if (continueBtn != null)
        {
            continueBtn.gameObject.SetActive(hasSave);
            continueBtn.onClick.SetListener(() =>
            {
                Debug.Log("继续游戏");
                Action action = () =>
                {
                    GameManager.Instance.ChangeState(new MainGameState(isContinue: true));
                };
                UIManager.Instance.Open<LoadingUI>(action);
            });
        }

        Get<Button>("Start").onClick.SetListener(() =>
        {
            Debug.Log("开始游戏");
            Action action = () =>
            {
                GameManager.Instance.ChangeState(new MainGameState(isContinue: false));
            };
            UIManager.Instance.Open<LoadingUI>(action);
        });

        Get<Button>("Setting").onClick.SetListener(() =>
        {
            Debug.Log("打开设置界面");
            UIManager.Instance.Open<SetVolumeUI>();
        });

        Get<Button>("Exit").onClick.SetListener(() =>
        {
            Application.Quit();
        });

        DOVirtual.Float(0f, 1f, 3f, (value) =>
        {
            Color rainbowColor = Color.HSVToRGB(value, 0.8f, 1f);
            Get<Image>("Start").color = rainbowColor;
            Get<Image>("Start").GetComponentInChildren<Text>().color = rainbowColor;
        })
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    public override void OnClose()
    {
        base.OnClose();
    }
}
