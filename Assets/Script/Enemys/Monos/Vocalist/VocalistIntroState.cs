using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VocalistIntroState : BossBaseState
{
    private VocalistBoss vocalist;
    private Vector3[] combatLocalPositions = new Vector3[2];
    private Sequence introSequence;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        vocalist = context as VocalistBoss;

        combatLocalPositions[0] = vocalist.handAnchors[0].localPosition;
        combatLocalPositions[1] = vocalist.handAnchors[1].localPosition;

        PlayCorrectIntroAnimation();
    }

    private void PlayCorrectIntroAnimation()
    {
        introSequence?.Kill();

        vocalist.leftDrill.position = vocalist.hairAnchors[0].position;
        vocalist.rightDrill.position = vocalist.hairAnchors[1].position;
        vocalist.leftDrill.rotation = Quaternion.identity;
        vocalist.rightDrill.rotation = Quaternion.identity;

        introSequence = DOTween.Sequence().SetLink(vocalist.gameObject);

        introSequence.Append(vocalist.handAnchors[0].DOMove(vocalist.leftDrill.position, 0.6f).SetEase(Ease.OutQuad));
        introSequence.Join(vocalist.handAnchors[1].DOMove(vocalist.rightDrill.position, 0.6f).SetEase(Ease.OutQuad));

        introSequence.AppendCallback(() =>
        {
            vocalist.leftDrill.SetParent(vocalist.handAnchors[0]);
            vocalist.rightDrill.SetParent(vocalist.handAnchors[1]);
            vocalist.leftDrill.localPosition = Vector3.zero;
            vocalist.rightDrill.localPosition = Vector3.zero;
        });

        introSequence.Append(vocalist.handAnchors[0].DOLocalRotate(new Vector3(0, 0, -180), 0.6f).SetEase(Ease.InOutQuad));
        introSequence.Join(vocalist.handAnchors[1].DOLocalRotate(new Vector3(0, 0, -180), 0.6f).SetEase(Ease.InOutQuad));

        Vector3 leftFinalPos = new Vector3(-1.8f, -0.7f, 0);
        Vector3 rightFinalPos = new Vector3(1.8f, -0.7f, 0);

        introSequence.Append(vocalist.handAnchors[0].DOLocalMove(leftFinalPos, 0.6f).SetEase(Ease.OutCubic));
        introSequence.Join(vocalist.handAnchors[1].DOLocalMove(rightFinalPos, 0.6f).SetEase(Ease.OutCubic));

        introSequence.OnComplete(() =>
        {
            introSequence = null;
            vocalist.SwitchState(vocalist.meleeState);
        });
    }

    public override void Exit()
    {
        introSequence?.Kill();
        introSequence = null;
        base.Exit();
    }
}
