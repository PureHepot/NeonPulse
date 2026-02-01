using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StateEntrance : BossState
{
    public StateEntrance(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();

        Vector3 startPos = boss.targetEntryPosition + Vector3.up * 10f;
        boss.transform.position = startPos;

        boss.transform.DOMove(boss.targetEntryPosition, 2.5f)
            .SetEase(Ease.OutBack);
        Timer.Register(2.5f, OnEntranceFinished);
    }

    private void OnEntranceFinished()
    {
        boss.HoverAnchorPos = boss.transform.position;

        boss.ChangeState(boss.stateIdle);
    }

    public override void OnUpdate() { }

    public override void OnExit()
    {
        boss.transform.DOKill();
    }
}
