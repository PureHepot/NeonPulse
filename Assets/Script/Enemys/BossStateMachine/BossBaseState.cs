using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBaseState
{
    protected BossBase vocalist;
    protected float stateTimer;

    public virtual void Enter(BossBase context)
    {
        vocalist = context;
        stateTimer = 0f;
    }

    // 建议改名为 LogicUpdate，避免和 Unity 的 MonoBehaviour.Update 混淆
    public virtual void LogicUpdate()
    {
        stateTimer += Time.deltaTime;
    }

    public virtual void PhysicsUpdate()
    {
    }

    public virtual void Exit()
    {
    }
}