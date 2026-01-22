using UnityEngine;

public class SlimeFallState : BossBaseState
{
    public override void Enter(BossSlimeController context)
    {
        base.Enter(context);
        // 飞行时确保重力是关闭的，这样才是直线飞行
        ctx.PhysicsRuntime.enableGravity = false;
    }

    public override void OnFixedUpdate()
    {
        // 这里的检测方向很关键
        // 如果我们在冲刺，应该检测速度方向
        Vector2 checkDir = ctx.CenterRB.velocity.normalized;
        if (checkDir == Vector2.zero) checkDir = ctx.PhysicsRuntime.gravityDirection;

        if (ctx.CheckGround(checkDir))
        {
            // 撞到了新墙！

            // 1. 设置新重力方向
            ctx.PhysicsRuntime.gravityDirection = -ctx.SurfaceNormal;

            // 2. 【核心】重新开启重力
            ctx.PhysicsRuntime.enableGravity = true;

            // 3. 吸附缓冲
            ctx.CenterRB.velocity = Vector2.zero; // 撞墙停下
            ctx.CenterRB.AddForce(-ctx.SurfaceNormal * 20f, ForceMode2D.Impulse); // 拍在墙上

            // 4. 切回游走
            ctx.TransitionToState(ctx.RoamState);
        }
    }
}