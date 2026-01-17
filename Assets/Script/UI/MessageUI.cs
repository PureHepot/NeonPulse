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

        Get<TextMeshProUGUI>("Text").text = arg.txt;

        Timer.Register(2f,

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
    public MessageUIArg(int level, string txt)
    {
        this.level = level;
        this.txt = txt;
    }
}