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
    [SerializeField]
    private float currentDashSpeed;

    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    private HealthModule healthModule;

    private int playerLayerID;
    private int enemyLayerID;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        healthModule = _player.Modules.GetModule<HealthModule>(ModuleType.Health);

        playerLayerID = LayerMask.NameToLayer("Player");
        enemyLayerID = LayerMask.NameToLayer("Enemy");

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
        minDashSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed) * 4;
        maxDashSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed)* 4 + 5f;
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || player.isPreview) return;

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

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, true);

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

    float GetCurrentStageScale()
    {
        switch (currentStage)
        {
            case 3: return scaleStage3;
            case 2: return scaleStage2;
            case 1: return scaleStage1;
            default: return scaleStage1 * 0.5f;
        }
    }

    void DetectEnemies()
    {
        float currentScale = GetCurrentStageScale();

        float detectRadius = currentScale * attackRadius * attackRadiusRatio;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, detectRadius, enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitTargets.Add(hit);
            }
        }
    }

    void EndDash()
    {
        // 停止无敌
        if (healthModule) healthModule.IsInvincible = false;
        if (dashTrail) dashTrail.emitting = false;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
        player.Rigid2d.velocity = dashDirection * (currentDashSpeed * 0.2f);

        // 进入结算阶段
        StartCoroutine(SettlementRoutine());
    }

    IEnumerator SettlementRoutine()
    {
        currentState = State.Settling;

        // 复制列表防止修改
        List<Collider2D> targets = new List<Collider2D>(hitTargets);

        int finalDamage = Mathf.RoundToInt(baseDamage * currentStage);
        float finalKnockback = knockbackForce * currentStage;

        for (int i = 0; i < hitCount; i++)
        {
            bool isLastHit = (i == hitCount - 1);

            foreach (var col in targets)
            {
                // 判空 (可能怪已经死了被 Destroy 了)
                if (col == null || col.gameObject == null) continue;

                // 计算击退方向
                Vector3 dir = (col.transform.position - player.transform.position).normalized;

                // 【修改 6】伤害类型分流处理
                EnemyBase enemy = col.GetComponent<EnemyBase>();

                if (enemy != null)
                {
                    // A. 如果是 EnemyBase (小怪/本体)，使用带击退的高级伤害
                    float force = isLastHit ? finalKnockback : 0f;
                    enemy.TakeDamage(finalDamage, col.transform.position, dir, force);
                }
                else
                {
                    // B. 如果只是 IDamageable (比如 BossPart)，使用普通伤害接口
                    IDamageable part = col.GetComponent<IDamageable>();
                    if (part != null)
                    {
                        // 普通受击（通常 Boss 部位不吃物理击退）
                        part.TakeDamage(finalDamage, col.transform.position, dir);
                    }
                }

                if (i > 0) CameraManager.Instance.Shake("BladeLight");
            }

            yield return new WaitForSeconds(hitInterval);
        }

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
        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
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
        minDashSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed) * 4;
        maxDashSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed) * 4 + 5f;
    }

    private void OnDrawGizmosSelected()
    {
        // 如果判定参数还没设置，就不画
        if (attackRadius <= 0) return;

        Vector3 center = transform.position;

        // 1. 绘制基础参考圆 (白色) - 这是 attackRadius 的原始大小
        // Gizmos.color = new Color(1, 1, 1, 0.2f);
        // Gizmos.DrawWireSphere(center, attackRadius);

        // 2. 预计算各个阶段的实际判定半径
        // 公式必须与 DetectEnemies 保持完全一致: scale * attackRadius * attackRadiusRatio
        float r1 = scaleStage1 * attackRadius * attackRadiusRatio;
        float r2 = scaleStage2 * attackRadius * attackRadiusRatio;
        float r3 = scaleStage3 * attackRadius * attackRadiusRatio;

        // 3. 绘制 Stage 1 (绿色 - 最小判定)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, r1);

        // 4. 绘制 Stage 2 (黄色 - 中等判定)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, r2);

        // 5. 绘制 Stage 3 (红色 - 最大判定)
        Gizmos.color = new Color(1f, 0.3f, 0.3f); // 浅红
        Gizmos.DrawWireSphere(center, r3);

        // 6. [运行时] 绘制当前生效的判定范围
        if (Application.isPlaying)
        {
            float currentR = GetCurrentStageScale() * attackRadius * attackRadiusRatio;
            Gizmos.color = Color.cyan;

            // 稍微画粗一点 (多画几圈)
            Gizmos.DrawWireSphere(center, currentR);
            Gizmos.DrawWireSphere(center, currentR * 0.99f);
        }
    }
}

public static class EnemyExtensions
{
    public static bool IsDead(this EnemyBase enemy)
    {
        return enemy == null || !enemy.gameObject.activeSelf;
    }
}
