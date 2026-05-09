using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StartUI : UIBase
{
    private bool hasSave;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        hasSave = args is bool value && value;
        InitButtons();
    }

    private void InitButtons()
    {
        var continueButton = Get<Button>("Continue");
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
            continueButton.onClick.SetListener(() =>
            {
                Action action = () => GameMgr.Instance.Game.ChangeState(new MainGameState(true));
                GameMgr.Instance.UI.Open<LoadingUI>(action);
            });
        }

        Get<Button>("Start").onClick.SetListener(() =>
        {
            Action action = () => GameMgr.Instance.Game.ChangeState(new AssembleGameState());
            GameMgr.Instance.UI.Open<LoadingUI>(action);
        });

        Get<Button>("Setting").onClick.SetListener(() =>
        {
            GameMgr.Instance.UI.Open<SetVolumeUI>();
        });

        Get<Button>("Exit").onClick.SetListener(Application.Quit);

        DOVirtual.Float(0f, 1f, 3f, value =>
        {
            Color rainbowColor = Color.HSVToRGB(value, 0.8f, 1f);
            Get<Image>("Start").color = rainbowColor;
            Get<Image>("Start").GetComponentInChildren<Text>().color = rainbowColor;
        })
        .SetLoops(-1, LoopType.Restart)
        .SetEase(Ease.Linear)
        .SetUpdate(true);
    }
}
