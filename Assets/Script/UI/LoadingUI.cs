using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : UIBase
{
    private float transTime = 1f;

    private Action action;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        action = args as Action;

        Get<Image>("BG").DOKill();

        Get<Image>("BG").DOColor(new Color(0, 0, 0, 1), transTime).SetUpdate(true).OnComplete(() =>
        {
            action?.Invoke();

            Get<Image>("BG").DOColor(new Color(0, 0, 0, 0), transTime).SetUpdate(true).OnComplete(() =>
            {
                UIManager.Instance.CloseUI(this);
            });
        });

    }

    public override void OnClose()
    {
        base.OnClose();
    }
}
