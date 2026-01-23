using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBaseState
{
    protected BossSlimeController ctx;
    protected float stateTimer;

    public virtual void Enter(BossSlimeController context)
    {
        ctx = context;
        stateTimer = 0f;
    }
    public virtual void OnUpdate() { stateTimer += Time.deltaTime; Debug.Log($"Updating in {this.ToString()}"); }
    public virtual void OnFixedUpdate() { Debug.Log($"FixedUpdating in {this.ToString()}"); }
    public virtual void Exit() { }
}