using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class EndUI : UIBase
{
    [Header("UI Refs")]
    public Image edgeImage;

    [Header("Settings")]
    [Tooltip("颜色循环一圈的时间 (秒)")]
    public float cycleDuration = 3.0f;

    [Tooltip("发光强度 (配合 Bloom，>1 才会发光)")]
    public float brightness = 2.5f;

    // 内部用来做动画的中间变量
    private float _hue = 0f;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        DOTween.To(() => _hue, x => _hue = x, 1f, cycleDuration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(edgeImage.gameObject)
            .OnUpdate(() =>
            {
                Color targetColor = Color.HSVToRGB(_hue, 1f, 1f);
                edgeImage.color = targetColor * brightness;
            });
    }
}
