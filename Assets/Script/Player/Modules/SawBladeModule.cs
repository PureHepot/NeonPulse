using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SawBladeModule : PlayerModule
{
    private const string BladeBaseDamageStatId = "weapon.bladebasedamage";
    private const string BladeChargeTimeStatId = "weapon.bladechargetime";
    private const string BladeHitCountStatId = "weapon.bladehitcount";
    private const string MoveSpeedStatId = "move.speed";

    [Header("Visual Refs")]
    public Transform bladeVisual;
    public TrailRenderer dashTrail;

    [Header("Charge Settings")]
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
    public int hitCount = 3;
    public float hitInterval = 0.2f;
    public float knockbackForce = 10f;
    public float attackRadiusRatio = 1.2f;
    public LayerMask enemyLayer;

    private enum BladeState
    {
        Idle,
        Charging,
        Dashing,
        Settling
    }

    private BladeState currentState = BladeState.Idle;
    private float currentChargeTime;
    private float currentSpinSpeed;
    private int currentStage;
    private Vector3 dashDirection;
    private float currentDashSpeed;
    private readonly HashSet<Collider2D> hitTargets = new();
    private HealthModule healthModule;
    private int playerLayerID;
    private int enemyLayerID;

    protected override void OnInitialize()
    {
        healthModule = player != null && player.Modules != null
            ? player.Modules.GetModule<HealthModule>(ModuleType.Health)
            : null;

        playerLayerID = LayerMask.NameToLayer("Player");
        enemyLayerID = LayerMask.NameToLayer("Enemy");

        if (bladeVisual != null)
        {
            bladeVisual.gameObject.SetActive(false);
            bladeVisual.localScale = Vector3.one * 0.1f;
        }

        if (dashTrail != null)
            dashTrail.emitting = false;

        RecalculateStats();
    }

    protected override void OnActivate()
    {
        RecalculateStats();
    }

    protected override void OnDeactivate()
    {
        var runtimeHealthModule = EnsureHealthModule();
        if (runtimeHealthModule != null)
            runtimeHealthModule.IsInvincible = false;

        if (bladeVisual != null)
            bladeVisual.gameObject.SetActive(false);

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead)
            return;

        if (currentState != BladeState.Idle && bladeVisual != null)
            bladeVisual.Rotate(Vector3.forward, currentSpinSpeed * DeltaTime);

        switch (currentState)
        {
            case BladeState.Idle:
                HandleIdle();
                break;
            case BladeState.Charging:
                HandleCharging();
                break;
            case BladeState.Dashing:
                HandleDashing();
                break;
        }
    }

    private void RecalculateStats()
    {
        baseDamage = Mathf.RoundToInt(GetStat(BladeBaseDamageStatId, baseDamage));
        maxChargeTime = GetStat(BladeChargeTimeStatId, maxChargeTime);
        hitCount = Mathf.Max(1, Mathf.RoundToInt(GetStat(BladeHitCountStatId, hitCount)));
        float moveSpeed = GetStat(MoveSpeedStatId, 5f);
        minDashSpeed = moveSpeed * 4f;
        maxDashSpeed = moveSpeed * 4f + 5f;
    }

    private HealthModule EnsureHealthModule()
    {
        if (healthModule == null && player != null && player.Modules != null)
            healthModule = player.Modules.GetModule<HealthModule>(ModuleType.Health);

        return healthModule;
    }

    private void HandleIdle()
    {
        if (HasControl && InputManager.Instance.Mouse0())
            StartCharging();
    }

    private void HandleCharging()
    {
        currentChargeTime += DeltaTime;
        float progress = Mathf.Clamp01(currentChargeTime / maxChargeTime);
        currentSpinSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, progress);

        int newStage = progress >= 1f ? 3 : progress >= 0.66f ? 2 : progress >= 0.33f ? 1 : 0;
        float targetScale = newStage switch
        {
            3 => scaleStage3,
            2 => scaleStage2,
            1 => scaleStage1,
            _ => scaleStage1 * 0.5f
        };

        if (newStage != currentStage)
        {
            currentStage = newStage;
            if (bladeVisual != null && currentStage > 0)
            {
                bladeVisual.DOKill(true);
                bladeVisual.localScale = Vector3.one * targetScale;
                bladeVisual.DOPunchScale(Vector3.one * 0.4f, 0.3f, 10, 1f);
            }
        }

        if (bladeVisual != null && !DOTween.IsTweening(bladeVisual))
            bladeVisual.localScale = Vector3.Lerp(bladeVisual.localScale, Vector3.one * targetScale, DeltaTime * 10f);

        if (HasControl && !InputManager.Instance.Mouse0())
        {
            if (currentStage == 0)
                CancelCharge();
            else
                StartDash(progress);
        }
    }

    private void HandleDashing()
    {
        if (player.Rigid2d != null)
            player.SnapVelocity(dashDirection * currentDashSpeed);

        DetectEnemies();
    }

    private void StartCharging()
    {
        currentState = BladeState.Charging;
        currentChargeTime = 0f;
        currentStage = 0;
        currentSpinSpeed = minSpinSpeed;

        if (bladeVisual == null)
            return;

        bladeVisual.DOKill();
        bladeVisual.gameObject.SetActive(true);
        bladeVisual.localScale = Vector3.zero;
        bladeVisual.DOScale(scaleStage1 * 0.5f, 0.2f).SetEase(Ease.OutBack);
    }

    private void CancelCharge()
    {
        currentState = BladeState.Idle;
        if (bladeVisual != null)
            bladeVisual.DOScale(0f, 0.2f).OnComplete(() => bladeVisual.gameObject.SetActive(false));
    }

    private void StartDash(float chargeProgress)
    {
        currentState = BladeState.Dashing;
        hitTargets.Clear();
        currentDashSpeed = Mathf.Lerp(minDashSpeed, maxDashSpeed, chargeProgress);

        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        dashDirection = (mousePos - player.transform.position).normalized;

        if (healthModule != null)
            healthModule.IsInvincible = true;

        if (dashTrail != null)
            dashTrail.emitting = true;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, true);
        CameraManager.Instance.Shake("Blade");
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        float duration = dashDurationBase + currentStage * 0.05f;
        yield return new WaitForSeconds(duration);
        EndDash();
    }

    private void DetectEnemies()
    {
        float detectRadius = GetCurrentStageScale() * attackRadius * attackRadiusRatio;
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, detectRadius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<IDamageable>() != null)
                hitTargets.Add(hit);
        }
    }

    private void EndDash()
    {
        if (healthModule != null)
            healthModule.IsInvincible = false;

        if (dashTrail != null)
            dashTrail.emitting = false;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
        if (player.Rigid2d != null)
            player.SnapVelocity(dashDirection * (currentDashSpeed * 0.2f));

        StartCoroutine(SettlementRoutine());
    }

    private IEnumerator SettlementRoutine()
    {
        currentState = BladeState.Settling;
        List<Collider2D> targets = new(hitTargets);
        int finalDamage = Mathf.RoundToInt(baseDamage * Mathf.Max(1, currentStage));
        float finalKnockback = knockbackForce * Mathf.Max(1, currentStage);

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            bool isLastHit = hitIndex == hitCount - 1;
            foreach (var collider in targets)
            {
                if (collider == null)
                    continue;

                Vector3 dir = (collider.transform.position - player.transform.position).normalized;
                EnemyBase enemy = collider.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    float force = isLastHit ? finalKnockback : 0f;
                    enemy.TakeDamage(finalDamage, collider.transform.position, dir, force);
                }
                else
                {
                    var damageable = collider.GetComponent<IDamageable>();
                    if (damageable != null)
                        damageable.TakeDamage(finalDamage, collider.transform.position, dir);
                }
            }

            yield return new WaitForSeconds(hitInterval);
        }

        ResetAfterSettlement();
    }

    private void ResetAfterSettlement()
    {
        if (HasControl && InputManager.Instance.Mouse0())
        {
            StartCharging();
            return;
        }

        currentState = BladeState.Idle;
        if (bladeVisual != null)
        {
            bladeVisual.DOScale(0f, 0.15f).OnComplete(() =>
            {
                if (currentState == BladeState.Idle)
                    bladeVisual.gameObject.SetActive(false);
            });
        }
    }

    private float GetCurrentStageScale()
    {
        return currentStage switch
        {
            3 => scaleStage3,
            2 => scaleStage2,
            1 => scaleStage1,
            _ => scaleStage1 * 0.5f
        };
    }
}

public static class EnemyExtensions
{
    public static bool IsDead(this EnemyBase enemy)
    {
        return enemy == null || !enemy.gameObject.activeSelf;
    }
}
