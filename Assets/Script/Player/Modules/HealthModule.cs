using System.Collections;
using DG.Tweening;
using UnityEngine;

public class HealthModule : PlayerModule
{
    private const string MaxHpStatId = "health.addtionalhp";
    private const string HealthRegenStatId = "health.healthregen";
    private const string InvincibilityDurationStatId = "health.invinciduration";

    public int MaxHp { get; private set; }
    public float CurrentHp { get; private set; }
    public float RegenPerSecond { get; private set; }

    [Header("Hurt Settings")]
    public float knockbackForce = 15f;
    public float stunDuration = 0.2f;
    public float invincibilityDuration = 1.0f;
    public Color hurtColor = Color.red;
    public Color normalColor = Color.white;

    public bool IsInvincible { get; set; }
    private float regenAccumulator;

    protected override void OnInitialize()
    {
        RecalculateStats();
        CurrentHp = MaxHp;
        SyncUI();
    }

    public override void OnModuleUpdate()
    {
        if (player == null)
            return;

        HandleRegen();
    }

    public void TakeDamage(int amount, Transform attacker)
    {
        if (IsInvincible || player == null || player.IsDead)
            return;

        CurrentHp = Mathf.Clamp(CurrentHp - amount, 0, MaxHp);
        AudioManager.Instance.PlayEffect("PlayerHit");
        SyncUI();

        if (CurrentHp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(attacker));
    }

    private void HandleRegen()
    {
        if (RegenPerSecond <= 0f || CurrentHp >= MaxHp)
            return;

        regenAccumulator += RegenPerSecond * DeltaTime;
        if (regenAccumulator < 1f)
            return;

        int heal = Mathf.FloorToInt(regenAccumulator);
        regenAccumulator -= heal;
        CurrentHp = Mathf.Min(CurrentHp + heal, MaxHp);
        SyncUI();
    }

    private void SyncUI()
    {
        if (!IsPrimaryPlayer || PlayerManager.Instance == null)
            return;

        int displayHp = CurrentHp <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(CurrentHp));
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
        MaxHp = Mathf.RoundToInt(GetStat(MaxHpStatId, 10f));
        RegenPerSecond = GetStat(HealthRegenStatId, 0f);
        invincibilityDuration = GetStat(InvincibilityDurationStatId, invincibilityDuration);
        MaxHp = Mathf.Max(MaxHp, 1);
    }
}
