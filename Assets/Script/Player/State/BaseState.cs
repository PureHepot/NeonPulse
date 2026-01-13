using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    private PlayerController player;
    protected PlayerController Player
    {
        get
        {
            if (player == null)
            {
                player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            }
            return player;
        }

    }

    public abstract void Enter();

    // 每一帧执行update
    public abstract void LogicUpdate();

    // 物理计算fixedUpdate
    public abstract void PhysicsUpdate();

    public abstract void Exit();
}
