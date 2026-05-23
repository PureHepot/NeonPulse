using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageUI : UIBase
{
    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        MessageUIArg arg = args as MessageUIArg;

        Get<Text>("Text").text = arg.txt;

        Timer.Register(arg != null ? arg.durationSeconds : 2f,

            onComplete: () =>
            {
                OnClose();
            });


    }

    public override void OnClose()
    {
        base.OnClose();
    }
}

public class MessageUIArg
{
    public int level;
    public string txt;
    public float durationSeconds;

    public MessageUIArg(int level, string txt, float durationSeconds = 2f)
    {
        this.level = level;
        this.txt = txt;
        this.durationSeconds = durationSeconds;
    }
}
