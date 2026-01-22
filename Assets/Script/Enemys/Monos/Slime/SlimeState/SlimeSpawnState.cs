using UnityEngine;

public class SlimeSpawnState : BossBaseState
{
    private float duration = 1.0f;
    private Vector3 startScale = Vector3.one * 0.1f;
    private Vector3 targetScale = Vector3.one;

    public override void Enter(BossSlimeController context)
    {
        base.Enter(context);

        ctx.transform.position = Vector3.zero;
        ctx.transform.localScale = startScale;

        ctx.PhysicsRuntime.gravityDirection = Vector2.down;
        ctx.PhysicsRuntime.enablePhysicsSimulation = false;
        ctx.PhysicsRuntime.enableGravity = false;

    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        // 2. 动画插值
        float t = stateTimer / duration;
        // 使用简单的 EaseOutBack 曲线效果
        float curve = 1f + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
        // 或者简单的 Lerp: float curve = t;

        if (t >= 1f)
        {
            ctx.transform.localScale = targetScale;
            ctx.PhysicsRuntime.enablePhysicsSimulation = true;
            ctx.PhysicsRuntime.enableGravity = true;
            // 动画结束，切到下落状态
            //ctx.TransitionToState(ctx.FallState);

            if (ctx.CheckGround(Vector2.down))
            {
                ctx.TransitionToState(ctx.FallState);
            }
        }
        else
        {
            ctx.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curve);
        }

        
    }
}
