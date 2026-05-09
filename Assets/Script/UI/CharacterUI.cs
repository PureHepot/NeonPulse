using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// TODO: 等待重写为基于 FrameConfig 的装配 UI
public class CharacterUI : UIBase
{
    [Header("UI References")]
    public Transform personListContent;
    public Transform moduleListContent;
    public Transform detailPanel;
    public Transform detailContent;
    public Transform previewContainer;

    public Text loadCapacityText;
    public Text softCurrencyText;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
    }
}
