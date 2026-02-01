using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateBarrage : BossState
{
    public StateBarrage(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();
        if (boss.leftTurret) boss.leftTurret.FireBurst();
        if (boss.rightTurret) boss.rightTurret.FireBurst();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        boss.PerformSmoothHover();

        // 射击硬直
        if (stateTimer > 2.5f)
        {
            boss.ChangeState(boss.stateIdle);
        }
    }
}
