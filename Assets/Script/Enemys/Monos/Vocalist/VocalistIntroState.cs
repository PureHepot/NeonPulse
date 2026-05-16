using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VocalistIntroState : BossBaseState
{
    private VocalistBoss vocalist;
    private Vector3[] combatLocalPositions = new Vector3[2];

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        vocalist = context as VocalistBoss;

        // 记录手部在编辑器中预设好的位置（即最终战斗时的两侧位置）
        combatLocalPositions[0] = vocalist.handAnchors[0].localPosition;
        combatLocalPositions[1] = vocalist.handAnchors[1].localPosition;

        PlayCorrectIntroAnimation();
    }

    private void PlayCorrectIntroAnimation()
    {
        // 1. 确保钻头初始在头发位置，旋转归零
        vocalist.leftDrill.position = vocalist.hairAnchors[0].position;
        vocalist.rightDrill.position = vocalist.hairAnchors[1].position;
        vocalist.leftDrill.rotation = Quaternion.identity;
        vocalist.rightDrill.rotation = Quaternion.identity;

        Sequence introSeq = DOTween.Sequence();

        // 第一步：手部（handAnchors）从当前位置平滑移动到钻头底部位置
        introSeq.Append(vocalist.handAnchors[0].DOMove(vocalist.leftDrill.position, 0.6f).SetEase(Ease.OutQuad));
        introSeq.Join(vocalist.handAnchors[1].DOMove(vocalist.rightDrill.position, 0.6f).SetEase(Ease.OutQuad));

        // 第二步：建立绑定关系（合体）
        introSeq.AppendCallback(() => {
            vocalist.leftDrill.SetParent(vocalist.handAnchors[0]);
            vocalist.rightDrill.SetParent(vocalist.handAnchors[1]);
            // 归一化本地坐标，确保手部捏住钻头底部
            vocalist.leftDrill.localPosition = Vector3.zero;
            vocalist.rightDrill.localPosition = Vector3.zero;
        });

        // 第三步：执行核心旋转（旋转 -180 度），带动钻头一起反转，使其头部朝上
        introSeq.Append(vocalist.handAnchors[0].DOLocalRotate(new Vector3(0, 0, -180), 0.6f).SetEase(Ease.InOutQuad));
        introSeq.Join(vocalist.handAnchors[1].DOLocalRotate(new Vector3(0, 0, -180), 0.6f).SetEase(Ease.InOutQuad));

        Vector3 leftFinalPos = new Vector3(-1.8f, -0.7f, 0);
        Vector3 rightFinalPos = new Vector3(1.8f, -0.7f, 0);

        introSeq.Append(vocalist.handAnchors[0].DOLocalMove(leftFinalPos, 0.6f).SetEase(Ease.OutCubic));
        introSeq.Join(vocalist.handAnchors[1].DOLocalMove(rightFinalPos, 0.6f).SetEase(Ease.OutCubic));

        // 完成动画并进入近战状态
        introSeq.OnComplete(() => {
            vocalist.SwitchState(vocalist.meleeState);
        });
    }
}
