using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyBlade : EnemyBase
{
    [Header("Blade Settings")]
    public float rotationSpeedIdle = 180f; // 正常自转速度
    public float rotationSpeedAttack = 720f; // 攻击自转速度
    public float aggroRange = 1.0f; // 索敌范围

    [Header("Movement")]
    public float enterSpeed = 3f;
    public Vector2 centerAreaSize = new Vector2(10, 6); // 屏幕中心区域大小

    [Header("Attack Stats")]
    public float slashSpeed = 25f; // 冲刺速度
    public float turnRate = 5f;    // 转向率 (弧线弯曲程度)
    public float attackDuration = 1.5f; // 单次冲锋最大持续时间
    public float missRecoveryTime = 1.0f; // 未命中的迟缓时间
    public int attacksPerRound = 3; // 一轮攻击冲几次

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color aggroColor = new Color(1, 0, 0.2f) * 4f; // 攻击色 (红色高亮)

    // 内部状态
    private enum State { Entering, Prowling, AggroTrans, Slashing, Recovering, Bouncing }
    private State currentState;
    private float stateTimer;
    private int currentAttackCount;
    private Vector2 targetDir;

    // 引用
    private TrailRenderer trail;

    public override void OnSpawn()
    {
        base.OnSpawn();

        currentState = State.Entering;
        currentAttackCount = 0;

        if (!trail) trail = GetComponentInChildren<TrailRenderer>();
        if (trail)
        {
            trail.Clear();
            trail.emitting = false;
        }

        // 随机一个屏幕中心的目标点作为入场终点
        Vector2 randomCenter = new Vector2(
            Random.Range(-centerAreaSize.x / 2, centerAreaSize.x / 2),
            Random.Range(-centerAreaSize.y / 2, centerAreaSize.y / 2)
        );
        targetDir = (randomCenter - (Vector2)transform.position).normalized;
    }

    protected override void MoveBehavior()
    {
        // 持续自转 (根据状态改变速度)
        float currentRotSpeed = (currentState == State.Slashing || currentState == State.Bouncing)
                                ? rotationSpeedAttack : rotationSpeedIdle;
        transform.Rotate(0, 0, -currentRotSpeed * Time.deltaTime);

        // 状态机
        switch (currentState)
        {
            case State.Entering:
                HandleEntering();
                break;
            case State.Prowling:
                HandleProwling();
                break;
            case State.AggroTrans:
                break;
            case State.Slashing:
                HandleSlashing();
                break;
            case State.Recovering:
                HandleRecovering();
                break;
            case State.Bouncing:
                // 物理反弹中，仅等待速度衰减
                if (rb.velocity.magnitude < 5f)
                {
                    StartNextAttackOrReset();
                }
                break;
        }
    }

    void HandleEntering()
    {
        rb.velocity = targetDir * enterSpeed;

        // 检测是否到达中心区域 (简单距离判定，或者判断是否进入屏幕范围)
        if (Mathf.Abs(transform.position.x) < centerAreaSize.x / 2 + 2f &&
            Mathf.Abs(transform.position.y) < centerAreaSize.y / 2 + 2f)
        {
            currentState = State.Prowling;
            rb.velocity = rb.velocity.normalized * (enterSpeed * 0.5f); // 减速巡逻
        }
    }

    void HandleProwling()
    {
        // 简单的惯性游荡，碰到墙壁反弹由物理材质处理，这里只做索敌
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= aggroRange)
            {
                StartAggro();
            }
        }
    }

    void StartAggro()
    {
        currentState = State.AggroTrans;
        rb.velocity = Vector2.zero; // 停下蓄力

        // 颜色渐变 -> 变红
        if (bodyRenderer)
        {
            bodyRenderer.DOKill();
            bodyRenderer.DOColor(aggroColor, 0.5f);
        }

        // 蓄力抖动
        transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1).OnComplete(() =>
        {
            currentAttackCount = 0;
            StartSlashAttack();
        });
    }

    void StartSlashAttack()
    {
        currentState = State.Slashing;
        stateTimer = attackDuration;

        // 开启拖尾
        if (trail) trail.emitting = true;

        if (playerTransform != null)
        {
            // 初始冲锋方向：稍微预判一点点，或者直接指向玩家
            targetDir = (playerTransform.position - transform.position).normalized;
            rb.velocity = targetDir * slashSpeed;
        }
    }

    void HandleSlashing()
    {
        stateTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            // --- 核心：弧线运动逻辑 ---
            // 类似于导弹制导，不断修正速度方向指向玩家，但限制修正率(TurnRate)
            // 这样如果速度够快，它就会划出一道弧线而不是直线

            Vector2 desiredDir = (playerTransform.position - transform.position).normalized;

            // 使用 RotateTowards 平滑转向
            // 这里的 step 是弧度，TurnRate 越大拐弯越急
            Vector2 newDir = Vector3.RotateTowards(rb.velocity.normalized, desiredDir, turnRate * Time.deltaTime, 0f);

            rb.velocity = newDir * slashSpeed;
        }

        // 超时未命中 (Miss)
        if (stateTimer <= 0)
        {
            EnterMissRecovery();
        }
    }

    void EnterMissRecovery()
    {
        currentState = State.Recovering;
        stateTimer = missRecoveryTime;

        // 强力刹车
        rb.drag = 5f;

        if (trail) trail.emitting = false;

        // 视觉：颜色稍微暗淡一点表示喘息? (可选)
    }

    void HandleRecovering()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            rb.drag = 0f; // 恢复阻力
            StartNextAttackOrReset();
        }
    }

    void StartNextAttackOrReset()
    {
        currentAttackCount++;
        if (currentAttackCount < attacksPerRound)
        {
            // 继续下一次冲锋
            StartSlashAttack();
        }
        else
        {
            // 攻击轮次结束，回到正常状态
            currentState = State.Prowling;
            currentAttackCount = 0;

            // 颜色恢复
            if (bodyRenderer) bodyRenderer.DOColor(normalColor, 1f);

            // 稍微远离玩家一点，防止贴脸不动
            Vector2 retreatDir = (transform.position - playerTransform.position).normalized;
            rb.velocity = retreatDir * enterSpeed;
        }
    }


    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        var shield = collision.collider.gameObject.GetComponent<ShieldController>();
        if (shield != null)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            var health = collision.gameObject.GetComponentInChildren<HealthModule>();
            if (health != null) health.TakeDamage(contactDamage, transform);

            if (currentState == State.Slashing)
            {
                currentState = State.Bouncing;
                if (trail) trail.emitting = false;

                // 计算反弹方向：沿着法线反射
                Vector2 normal = collision.contacts[0].normal;
                Vector2 reflectDir = Vector2.Reflect(rb.velocity.normalized, normal);

                // 施加反弹力
                rb.velocity = reflectDir * (slashSpeed * 0.6f); // 稍微降速反弹
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("ArenaWall"))
        {
            if (currentState == State.Slashing)
            {
                EnterMissRecovery();
                rb.velocity = collision.contacts[0].normal * 5f;
            }
        }

        base.OnCollisionEnter2D(collision);
    }
}
