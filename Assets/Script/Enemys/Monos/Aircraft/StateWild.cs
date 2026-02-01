using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateWild : BossState
{
    private float xFreq = 1.5f;
    private float xDist = 3.5f;
    private float yFreq = 2.0f;
    private float yDist = 1.0f;

    public StateWild(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        float newX = boss.HoverAnchorPos.x + Mathf.Cos(Time.time * xFreq) * xDist;
        float newY = boss.HoverAnchorPos.y + Mathf.Sin(Time.time * yFreq) * yDist;
        boss.GetComponent<Rigidbody2D>().MovePosition(new Vector2(newX, newY));
    }

    public override void OnExit()
    {
        base.OnExit();
        if (boss.leftTurret) boss.leftTurret.SetWildMode(false);
        if (boss.rightTurret) boss.rightTurret.SetWildMode(false);
    }
}
