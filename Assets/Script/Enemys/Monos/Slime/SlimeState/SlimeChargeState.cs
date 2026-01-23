using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeChargeState : BossBaseState
{
    public bool IsSwitchingSurface = false;
    private Vector2 launchDirection;
    private Vector2 targetPoint;

    public override void Enter(BossSlimeController context)
    {
        base.Enter(context);

        ctx.PhysicsRuntime.enableGravity = false;
    }

    public override void OnFixedUpdate()
    {
        // === 蓄力阶段 ===
        if (stateTimer < ctx.jumpChargeTime)
        {
            // 计算蓄力进度
            float t = stateTimer / ctx.jumpChargeTime;
            float currentSquish = Mathf.Lerp(0, ctx.squishForce, t);

            // 【核心需求】仅由自身的蓄力压力来提供"吸附"
            // 方向：当前表面的反方向 (即之前的重力方向)
            Vector2 squishDir = ctx.PhysicsRuntime.gravityDirection;

            // 施加力
            ctx.PhysicsRuntime.AddForceToAll(squishDir * currentSquish);

            // 剧烈抖动表现
            ctx.PhysicsRuntime.wobbleSpeed = 10f;
        }
        // === 发射阶段 ===
        else
        {
            PerformDash();
        }
    }

    void PerformDash()
    {
        // 恢复表现参数
        ctx.PhysicsRuntime.wobbleSpeed = 3f;

        // 如果没有目标，就沿法线跳 (容错)
        Vector2 launchDir = ctx.HasTarget ? ctx.TargetJumpDirection : -ctx.PhysicsRuntime.gravityDirection;

        // 施加冲撞力 (比普通跳大很多)
        float dashForce = ctx.jumpForce * 2.5f;

        // 推射
        ctx.CenterRB.AddForce(launchDir * dashForce, ForceMode2D.Impulse);
        ctx.PhysicsRuntime.AddForceToAll(launchDir * dashForce * 0.9f, ForceMode2D.Impulse);

        // 【关键逻辑】冲出去后，重力怎么办？
        // 方案A：保持重力关闭，直到撞到新墙 (像子弹一样飞)
        // 方案B：立刻把重力指向新墙 (像磁力鞋)

        // 这里采用方案 A (子弹模式)，因为你说的是"蓄力冲撞"
        // 我们保持 enableGravity = false，进入 FallState
        // FallState 检测到撞墙后，会重置重力并开启

        // 为了防止它永远飞在空中(如果没打中)，我们给 FallState 一个"如果太久没落地就开启重力"的保险
        ctx.TransitionToState(ctx.FallState);
    }

    //public override void OnFixedUpdate()
    //{
    //    // === 阶段 1: 蓄力 (Squash) ===
    //    if (stateTimer < ctx.jumpChargeTime)
    //    {
    //        // 沿着重力方向 (指向墙壁) 用力压
    //        Vector2 squishDir = ctx.PhysicsRuntime.gravityDirection;

    //        // 渐变力
    //        float t = stateTimer / ctx.jumpChargeTime;
    //        float force = Mathf.Lerp(0, ctx.squishForce, t);

    //        ctx.PhysicsRuntime.AddForceToAll(squishDir * force);

    //        // 剧烈抖动
    //        ctx.PhysicsRuntime.wobbleSpeed = 10f; // 临时加快抖动
    //    }
    //    // === 阶段 2: 起飞 (Launch) ===
    //    else
    //    {
    //        PerformJump();
    //    }
    //}

    //void PerformJump()
    //{
    //    // 恢复抖动速度
    //    ctx.PhysicsRuntime.wobbleSpeed = 3f;

    //    Vector2 jumpDir;
    //    float forceMultiplier = 1f;

    //    if (IsSwitchingSurface)
    //    {
    //        // === 换面跳 ===
    //        // 这里的逻辑：向 45度角大力飞出
    //        Vector2 normal = ctx.SurfaceNormal;
    //        Vector2 tangent = new Vector2(normal.y, -normal.x);
    //        // 随机向左或向右飞出
    //        float dir = Random.Range(0, 2) == 0 ? 1f : -1f;

    //        jumpDir = (normal + tangent * dir).normalized;
    //        forceMultiplier = 1.5f; // 跳得更远

    //        ctx.currentJumpCount = 0; // 重置计数

    //        // 跳出去后，进入 Fall 状态寻找新墙壁
    //        // 我们可以把重力暂时设为跳跃反方向，或者归零，让它飞一会
    //        ctx.PhysicsRuntime.gravityDirection = -jumpDir * 0.1f; // 微弱重力

    //        Launch(jumpDir, forceMultiplier);
    //        ctx.TransitionToState(ctx.FallState);
    //    }
    //    else
    //    {
    //        // === 原地跳 ===
    //        // 稍微偏一点点的垂直跳
    //        Vector2 normal = ctx.SurfaceNormal;
    //        jumpDir = (normal + new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f))).normalized;

    //        ctx.currentJumpCount++;

    //        Launch(jumpDir, 1f);
    //        // 跳完回到游走状态
    //        ctx.TransitionToState(ctx.RoamState);
    //    }
    //}

    //void Launch(Vector2 dir, float multiplier)
    //{
    //    float totalForce = ctx.jumpForce * multiplier;

    //    // 推中心
    //    ctx.CenterRB.AddForce(dir * totalForce, ForceMode2D.Impulse);

    //    // 推皮 (带皮起飞)
    //    ctx.PhysicsRuntime.AddForceToAll(dir * totalForce * 0.8f, ForceMode2D.Impulse);
    //}
}
