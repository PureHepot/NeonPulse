using System.Collections;
using DG.Tweening;
using UnityEngine;

public class HealthModule : PlayerModule
{
    private const string MaxHpStatId = "health.addtionalhp";
    private const string HealthRegenStatId = "health.healthregen";
    private const string InvincibilityDurationStatId = "health.invinciduration";

    public float MaxHp { get; private set; }
    public float CurrentHp { get; private set; }
    public float RegenPerSecond { get; private set; }
    public float RegenMultiplier { get; private set; } = 1f;
    public float DamageReductionMultiplier { get; private set; } = 1f;
    public float DamageReflectionPercent { get; private set; }

    [Header("Hurt Settings")]
    public float knockbackForce = 15f;
    public float stunDuration = 0.2f;
    public float invincibilityDuration = 0.4f;
    public Color hurtColor = Color.red;
    public Color normalColor = Color.white;

    public bool IsInvincible { get; set; }
    private float regenAccumulator;

    protected override void OnInitialize()
    {
        RecalculateStats();
        CurrentHp = ResolveInitialCurrentHp();
        SyncUI();
    }

    public override void OnModuleUpdate()
    {
        if (player == null)
            return;

        HandleRegen();
    }

    public void TakeDamage(float amount, Transform attacker)
    {
        if (IsInvincible || player == null || player.IsDead)
            return;

        float finalDamage = ResolveIncomingDamage(amount);
        CurrentHp = Mathf.Clamp(CurrentHp - finalDamage, 0, MaxHp);
        AudioManager.Instance.PlayEffect("PlayerHit");
        ReflectDamageToAttacker(attacker, finalDamage);
        SyncUI();

        if (CurrentHp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(attacker));
    }

    public void RefreshFromLoadout()
    {
        float previousMaxHp = MaxHp;
        float previousCurrentHp = CurrentHp;

        RecalculateStats();

        if (previousMaxHp <= 0f)
        {
            CurrentHp = MaxHp;
        }
        else if (MaxHp >= previousMaxHp)
        {
            CurrentHp = Mathf.Clamp(previousCurrentHp + (MaxHp - previousMaxHp), 0f, MaxHp);
        }
        else
        {
            CurrentHp = Mathf.Clamp(previousCurrentHp, 0f, MaxHp);
        }

        SyncUI();
    }

    public void ConfigureDefenceModifiers(float reductionMultiplier, float reflectionPercent)
    {
        DamageReductionMultiplier = Mathf.Max(0f, reductionMultiplier);
        DamageReflectionPercent = Mathf.Max(0f, reflectionPercent);
    }

    public void ConfigureHealthModifiers(float regenMultiplier)
    {
        RegenMultiplier = Mathf.Max(0f, regenMultiplier);
    }

    private void HandleRegen()
    {
        float effectiveRegen = RegenPerSecond * RegenMultiplier;
        if (effectiveRegen <= 0f || CurrentHp >= MaxHp)
            return;

        regenAccumulator += effectiveRegen * DeltaTime;
        if (regenAccumulator <= 0f)
            return;

        float heal = regenAccumulator;
        regenAccumulator = 0f;
        CurrentHp = Mathf.Min(CurrentHp + heal, MaxHp);
        SyncUI();
    }

    private void SyncUI()
    {
        if (!IsPrimaryPlayer || PlayerManager.Instance == null)
            return;

        float displayHp = CurrentHp <= 0f ? 0f : Mathf.Max(1f, CurrentHp);
        PlayerManager.Instance.SyncHp(displayHp, MaxHp);
    }

    private IEnumerator HurtRoutine(Transform attacker)
    {
        player.IsStunned = true;
        IsInvincible = true;
        PlayHurtVisuals();

        if (attacker != null && player.Colli2d != null && player.Rigid2d != null)
        {
            Vector2 knockbackDir = (player.transform.position - attacker.position).normalized;
            player.Colli2d.enabled = false;
            player.AddImpulse(knockbackDir * knockbackForce);
        }

        yield return new WaitForSeconds(stunDuration);
        player.IsStunned = false;

        yield return new WaitForSeconds(invincibilityDuration - stunDuration);

        if (player.Colli2d != null)
            player.Colli2d.enabled = true;

        IsInvincible = false;
        if (player.BodyRenderer != null)
            player.BodyRenderer.color = normalColor;
    }

    private void PlayHurtVisuals()
    {
        if (player.BodyRenderer == null)
            return;

        player.BodyRenderer.DOKill();
        player.BodyRenderer.DOColor(hurtColor, 0.05f).OnComplete(() =>
        {
            player.BodyRenderer.DOColor(normalColor, 0.2f);
        });

        player.BodyRenderer.DOFade(0.5f, 0.1f).SetLoops(5, LoopType.Yoyo);
        player.transform.DOPunchScale(new Vector3(-0.2f, 0.2f, 0f), 0.2f, 10, 1f);
    }

    private void Die()
    {
        if (player == null)
            return;

        player.IsDead = true;
        player.StopMovement(true);
        player.OnDeath?.Invoke();
        AudioManager.Instance.PlayEffect("PlayerDie");

        if (IsPrimaryPlayer)
        {
            Time.timeScale = 0f;
            UIManager.Instance.Open<GameOverUI>();
        }
    }

    private void RecalculateStats()
    {
        float frameBaseHp = ResolveFrameBaseHp();
        float additionalHp = ResolveAggregateStat(MaxHpStatId, 0f);
        MaxHp = frameBaseHp + additionalHp;
        RegenPerSecond = ResolveAggregateStat(HealthRegenStatId, 0f);
        invincibilityDuration = ResolveAggregateStat(InvincibilityDurationStatId, invincibilityDuration);
        MaxHp = Mathf.Max(MaxHp, 1);
    }

    private float ResolveInitialCurrentHp()
    {
        var run = DataManager.Instance != null ? DataManager.Instance.Run : null;
        if (run == null)
            return MaxHp;

        if (run.player.maxHp > 0 && run.player.currentHp > 0)
            return Mathf.Clamp(run.player.currentHp, 1f, MaxHp);

        return MaxHp;
    }

    private float ResolveFrameBaseHp()
    {
        var loadoutManager = GameMgr.Instance != null ? GameMgr.Instance.Loadout : null;
        var frameConfig = loadoutManager != null ? loadoutManager.GetCurrentFrame() : null;
        return frameConfig != null ? Mathf.Max(0f, frameConfig.baseMaxHP) : 0f;
    }

    private float ResolveAggregateStat(string statId, float fallbackValue)
    {
        var loadoutManager = GameMgr.Instance != null ? GameMgr.Instance.Loadout : null;
        if (loadoutManager == null || string.IsNullOrWhiteSpace(statId))
            return fallbackValue;

        float value = loadoutManager.GetFinalStat(statId);
        return Mathf.Approximately(value, 0f) ? fallbackValue : value;
    }

    private float ResolveIncomingDamage(float incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        float finalDamage = incomingDamage * DamageReductionMultiplier;
        return Mathf.Max(0f, finalDamage);
    }

    private void ReflectDamageToAttacker(Transform attacker, float finalDamage)
    {
        if (attacker == null || finalDamage <= 0)
            return;

        if (DamageReflectionPercent <= 0f)
            return;

        float reflectedDamage = finalDamage * DamageReflectionPercent;
        if (reflectedDamage <= 0)
            return;

        var damageable = attacker.GetComponent<IDamageable>()
                         ?? attacker.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        Vector3 hitPoint = attacker.position;
        Vector3 hitNormal = (attacker.position - transform.position).normalized;
        if (hitNormal.sqrMagnitude <= Mathf.Epsilon)
            hitNormal = Vector3.up;

        damageable.TakeDamage(reflectedDamage, hitPoint, hitNormal);
    }
}
