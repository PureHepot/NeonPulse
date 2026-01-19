using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    protected PlayerController Player;

    public BaseState(PlayerController player)
    {
        this.Player = player;
    }

    public abstract void Enter();

    // 每一帧执行update
    public abstract void LogicUpdate();

    // 物理计算fixedUpdate
    public abstract void PhysicsUpdate();

    public abstract void Exit();

    public virtual bool CanBeInterrupted()
    {
        return true;
    }
}
