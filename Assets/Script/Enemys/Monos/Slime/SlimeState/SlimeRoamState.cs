using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeRoamState : BossBaseState
{
    private float moveDir; // 1 或 -1

    public override void Enter(BossSlimeController context)
    {
        base.Enter(context);
        // 随机一个初始方向
        moveDir = Random.Range(0, 2) == 0 ? 1f : -1f;

        ctx.PhysicsRuntime.enableGravity = true;
        // 清除之前的目标
        ctx.HasTarget = false;
    }

    public override void OnFixedUpdate()
    {
        Vector2 normal = ctx.SurfaceNormal;
        Vector2 tangent = new Vector2(normal.y, -normal.x);

        // 左右滚动 (Sin 曲线移动)
        float wave = Mathf.Sin(Time.time * 2f);
        ctx.CenterRB.AddForce(tangent * wave * ctx.moveSpeed);

        // 判断是否该跳跃了
        if (stateTimer > ctx.jumpInterval)
        {
            if (ctx.CheckGround(ctx.PhysicsRuntime.gravityDirection))
            {
                // 选择一个新的面作为冲撞目标
                ctx.PickNewTargetSurface();
                ctx.TransitionToState(ctx.ChargeState);
            }
        }
    }
}
