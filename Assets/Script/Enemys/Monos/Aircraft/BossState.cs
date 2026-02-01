using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossState
{
    protected BossAirCraft boss;
    protected float stateTimer;

    public BossState(BossAirCraft _boss)
    {
        this.boss = _boss;
    }

    public virtual void OnEnter() { stateTimer = 0f; }
    public virtual void OnUpdate() { stateTimer += Time.deltaTime; }
    public virtual void OnFixedUpdate() { }
    public virtual void OnExit() { }
}
