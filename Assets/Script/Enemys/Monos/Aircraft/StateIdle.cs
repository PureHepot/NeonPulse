using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateIdle : BossState
{
    private float waitTime;

    private float xFreq = 0.5f; // 左右摆动频率
    private float xDist = 5.0f; // 左右摆动幅度
    private float yFreq = 1.0f; // 上下摆动频率
    private float yDist = 0.3f; // 上下摆动幅度

    private Vector2 currentVelocity;
    private float smoothTime = 0.8f;
    private float maxSpeed = 5.0f;

    public StateIdle(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();
        waitTime = Random.Range(0.5f, 1.5f);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        boss.PerformSmoothHover();

        if (stateTimer >= waitTime)
        {
            DecideNextAttack();
        }
    }

    private void DecideNextAttack()
    {
        float rng = Random.value;
        if (rng < 0.5f) boss.ChangeState(boss.stateSpawn);
        else boss.ChangeState(boss.stateBarrage);
    }
}
