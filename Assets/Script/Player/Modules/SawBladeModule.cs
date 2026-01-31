using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SawBladeModule : PlayerModule
{
    [Header("Visual Refs")]
    public Transform bladeVisual;      // 刀片视觉物体
    public TrailRenderer dashTrail;    // 拖尾

    [Header("Charge Settings (Staged)")]
    public float maxChargeTime = 1.5f;
    public float minSpinSpeed = 360f;
    public float maxSpinSpeed = 1800f;
    public float scaleStage1 = 0.7f;
    public float scaleStage2 = 0.9f;
    public float scaleStage3 = 1.3f;

    [Header("Dash Settings")]
    public float minDashSpeed = 15f;
    public float maxDashSpeed = 40f;
    public float dashDurationBase = 0.25f;

    [Header("Combat Settings")]
    public int baseDamage = 10;
    public float attackRadius = 1.5f;
    public int hitCount = 3;               // 攻击段数
    public float hitInterval = 0.2f;      // 每段攻击间隔
    public float knockbackForce = 10f;     // 击退力度
    public float attackRadiusRatio = 1.2f; // 判定范围是视觉大小的多少倍
    public LayerMask enemyLayer;

    // --- 内部状态 ---
    private enum State { Idle, Charging, Dashing, Settling }
    private State currentState = State.Idle;

    private float currentChargeTime = 0f;
    private float currentSpinSpeed = 0f;
    private int currentStage = 0;

    private Vector3 dashDirection;
    private float currentDashSpeed;

    // 记录冲刺期间碰到的敌人 (去重)
    private HashSet<EnemyBase> hitTargets = new HashSet<EnemyBase>();

    private HealthModule healthModule;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        healthModule = _player.Modules.GetModule<HealthModule>(ModuleType.Health);

        if (bladeVisual)
        {
            bladeVisual.gameObject.SetActive(false);
            bladeVisual.localScale = Vector3.one * 0.1f;
        }
        if (dashTrail) dashTrail.emitting = false;

        RecalculateStats();
    }

    private void RecalculateStats()
    {
        baseDamage = (int)UpgradeManager.Instance.GetStat(ModuleType.SawBlade, StatType.BladeBaseDamage);
        maxChargeTime = UpgradeManager.Instance.GetStat(ModuleType.SawBlade, StatType.BladeChargeTime);
        hitCount = (int)UpgradeManager.Instance.GetStat(ModuleType.SawBlade, StatType.BladeHitCount); 
    }

    public override void OnModuleUpdate()
    {
        if (player.IsDead) return;

        // 只要不是 Idle，刀片都在转
        if (currentState != State.Idle && bladeVisual)
        {
            bladeVisual.Rotate(Vector3.forward, currentSpinSpeed * Time.deltaTime);
        }

        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Charging:
                HandleCharging();
                break;
            case State.Dashing:
                HandleDashing();
                break;
            case State.Settling:
                break;
        }
    }

    void HandleIdle()
    {
        if (InputManager.Instance.Mouse0())
        {
            StartCharging();
        }
    }

    void HandleCharging()
    {
        currentChargeTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentChargeTime / maxChargeTime);

        // 转速
        currentSpinSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, progress);

        // 阶段判断
        int newStage = 0;
        float targetScale = 0.1f;

        if (progress >= 1f)
        {
            newStage = 3; targetScale = scaleStage3;
        }
        else if (progress >= 0.66f)
        {
            newStage = 2; targetScale = scaleStage2;
        }
        else if (progress >= 0.33f)
        {
            newStage = 1; targetScale = scaleStage1;
        }
        else
        {
            newStage = 0; targetScale = scaleStage1 * 0.5f;
        }

        // 升阶动画
        if (newStage != currentStage)
        {
            currentStage = newStage;
            if (bladeVisual && currentStage > 0)
            {
                bladeVisual.DOKill(true); // 杀掉之前的动画
                bladeVisual.localScale = Vector3.one * targetScale;
                bladeVisual.DOPunchScale(Vector3.one * 0.4f, 0.3f, 10, 1);
                // 这里可以播放 "ChargeUp" 音效
                // AudioManager.Instance.PlayEffect("ChargeUp");
            }
        }

        // 保持目标大小
        if (!DOTween.IsTweening(bladeVisual))
        {
            bladeVisual.localScale = Vector3.Lerp(bladeVisual.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);
        }

        // 松开鼠标
        if (!InputManager.Instance.Mouse0())
        {
            if (currentStage == 0) CancelCharge();
            else StartDash(progress);
        }
    }

    void HandleDashing()
    {
        player.Rigid2d.velocity = dashDirection * currentDashSpeed;

        DetectEnemies();
    }

    // --- 核心逻辑 ---

    void StartCharging()
    {
        currentState = State.Charging;
        currentChargeTime = 0f;
        currentStage = 0;
        currentSpinSpeed = minSpinSpeed;

        // 清理可能存在的"消失动画"，强制显示
        if (bladeVisual)
        {
            bladeVisual.DOKill();
            bladeVisual.gameObject.SetActive(true);
            bladeVisual.localScale = Vector3.zero;
            bladeVisual.DOScale(scaleStage1 * 0.5f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    void CancelCharge()
    {
        currentState = State.Idle;
        if (bladeVisual)
        {
            bladeVisual.DOScale(0, 0.2f).OnComplete(() => bladeVisual.gameObject.SetActive(false));
        }
    }

    void StartDash(float chargeProgress)
    {
        currentState = State.Dashing;

        hitTargets.Clear(); // 清空受击列表
        currentDashSpeed = Mathf.Lerp(minDashSpeed, maxDashSpeed, chargeProgress);

        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        dashDirection = (mousePos - player.transform.position).normalized;

        if (healthModule) healthModule.IsInvincible = true;
        if (dashTrail) dashTrail.emitting = true;

        CameraManager.Instance.Shake("Blade");

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        // 冲刺时间
        float duration = dashDurationBase + (currentStage * 0.05f);
        yield return new WaitForSeconds(duration);
        EndDash();
    }

    void DetectEnemies()
    {
        // 判定范围比视觉稍大
        float detectRadius = (bladeVisual ? bladeVisual.localScale.x : 1f) * attackRadius * 0.5f * attackRadiusRatio;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, detectRadius, enemyLayer);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead())
            {
                hitTargets.Add(enemy);
            }
        }
    }

    void EndDash()
    {
        // 停止无敌
        if (healthModule) healthModule.IsInvincible = false;
        if (dashTrail) dashTrail.emitting = false;

        // 保留一点惯性
        player.Rigid2d.velocity = dashDirection * (currentDashSpeed * 0.2f);

        // 进入结算阶段
        StartCoroutine(SettlementRoutine());
    }

    IEnumerator SettlementRoutine()
    {
        currentState = State.Settling;

        // 复制一份列表，防止协程执行时 modify
        List<EnemyBase> targets = new List<EnemyBase>(hitTargets);

        // 计算最终参数
        int finalDamage = Mathf.RoundToInt(baseDamage * currentStage);
        float finalKnockback = knockbackForce * currentStage;

        // 多段攻击循环
        for (int i = 0; i < hitCount; i++)
        {
            bool isLastHit = (i == hitCount - 1);

            foreach (var enemy in targets)
            {
                if (enemy == null || enemy.gameObject == null) continue; // 敌人可能已经死了

                // 只有最后一击才带强力击退
                float force = isLastHit ? finalKnockback : 0f;
                // 击退方向：从玩家推向敌人
                Vector3 dir = (enemy.transform.position - player.transform.position).normalized;

                // 调用我们在 EnemyBase 新加的重载方法
                enemy.TakeDamage(finalDamage, enemy.transform.position, dir, force);

                // 每一击都震一点屏，增加打击感
                if (i > 0) CameraManager.Instance.Shake("BladeLight");
            }

            yield return new WaitForSeconds(hitInterval);
        }

        // 结算完毕，检查输入进行连招
        CheckInputAndReset();
    }

    void CheckInputAndReset()
    {
        if (InputManager.Instance.Mouse0())
        {
            StartCharging();
        }
        else
        {
            currentState = State.Idle;
            if (bladeVisual)
            {
                bladeVisual.DOScale(0, 0.15f).OnComplete(() =>
                {
                    // 再次检查防止 tween 回调时已经开始新一轮蓄力了
                    if (currentState == State.Idle)
                        bladeVisual.gameObject.SetActive(false);
                });
            }
        }
    }

    public override void OnActivate()
    {
        base.OnActivate();
        RecalculateStats();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (healthModule) healthModule.IsInvincible = false;
        if (bladeVisual) bladeVisual.gameObject.SetActive(false);
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        if (moduleType == ModuleType.SawBlade)
        {
            switch (statType)
            {
                case StatType.BladeBaseDamage:
                    baseDamage = (int)UpgradeManager.Instance.GetStat(moduleType, statType);
                    break;
                case StatType.BladeChargeTime:
                    maxChargeTime = UpgradeManager.Instance.GetStat(moduleType, statType);
                    break;
            }
        }

        maxDashSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed) + 5f;
    }
}

public static class EnemyExtensions
{
    public static bool IsDead(this EnemyBase enemy)
    {
        // 根据你的 EnemyBase 逻辑判断，这里假设 currentHp > 0
        // 或者 EnemyBase 应该公开一个 IsDead 属性
        return enemy == null || !enemy.gameObject.activeSelf;
    }
}
