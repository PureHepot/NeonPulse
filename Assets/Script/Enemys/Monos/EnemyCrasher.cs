using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 需要 DoTween

public class EnemyCrasher : EnemyBase
{
    [Header("Dasher Settings")]
    public float aimDuration = 1.0f; // 瞄准/预警时间
    public float dashSpeed = 20f;    // 冲刺速度
    public float dashDistance = 30f; // 冲刺多远

    [Header("References")]
    public LineRenderer warningLine;

    // 状态
    private enum State { Spawning, Aiming, Dashing, Idle }
    private State currentState;
    private Vector2 dashDirection;
    private float stateTimer;

    public override void OnSpawn()
    {
        base.OnSpawn();

        currentState = State.Spawning;
        stateTimer = 0;

        if (warningLine) warningLine.enabled = false;

        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.5f).OnComplete(() => {
            StartAiming();
        });
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        if (warningLine) warningLine.enabled = false;
        transform.DOKill();
    }

    protected override void MoveBehavior()
    {
        // 状态机
        switch (currentState)
        {
            case State.Aiming:
                UpdateAiming();
                break;

            case State.Dashing:
                UpdateDashing();
                break;
        }
    }

    void StartAiming()
    {
        currentState = State.Aiming;
        stateTimer = aimDuration;

        if (warningLine)
        {
            warningLine.enabled = true;
            UpdateWarningLine();
        }

        // 可以在这里播放一个蓄力音效
    }

    void UpdateAiming()
    {
        stateTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            dashDirection = (playerTransform.position - transform.position).normalized;

            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); 
        }
        else
        {
            dashDirection = Vector2.left; // 没玩家就向左
        }

        UpdateWarningLine();

        if (stateTimer <= 0)
        {
            StartDash();
        }
    }

    void UpdateWarningLine()
    {
        if (warningLine == null) return;

        // 起点：自己
        warningLine.SetPosition(0, transform.position);

        // 终点：沿冲刺方向延伸 dashDistance 这么远
        Vector3 endPos = transform.position + (Vector3)dashDirection * dashDistance;
        warningLine.SetPosition(1, endPos);
    }

    void StartDash()
    {
        currentState = State.Dashing;

        if (warningLine) warningLine.enabled = false;

        // 播放冲刺动画/特效 (Squash and Stretch)
        transform.DOPunchScale(new Vector3(0.5f, -0.2f, 0), 0.2f, 10, 1);

        rb.velocity = dashDirection * dashSpeed;
        //冲两秒爆炸
        stateTimer = 2.0f;
    }

    void UpdateDashing()
    {
        // 保持速度 (防止阻力减速)
        rb.velocity = dashDirection * dashSpeed;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            Die();
        }
    }
}
