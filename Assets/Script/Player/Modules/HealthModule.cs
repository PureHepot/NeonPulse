using System.Collections;
using UnityEngine;
using DG.Tweening;

public class HealthModule : PlayerModule
{
    public int MaxHp { get; private set; }
    public float CurrentHp { get; private set; }
    public float RegenPerSecond { get; private set; }

    [Header("Hurt Settings")]
    public float knockbackForce = 15f;
    public float stunDuration = 0.2f;
    public float invincibilityDuration = 1.0f;
    public Color hurtColor = Color.red;
    public Color normalColor = Color.white;

    private bool isInvincible = false;
    private float regenTimer;
    private float regenAccumulator = 0f;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        RecalculateStats();

        CurrentHp = MaxHp;

        int displayHp = CurrentHp <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(CurrentHp));
        PlayerManager.Instance.SyncHp(displayHp, MaxHp);

        Debug.Log($"[HealthModule] 初始化 HP={CurrentHp}/{MaxHp} Regen={RegenPerSecond}/s");
    }

    public override void OnModuleUpdate()
    {
        HandleRegen();
    }

    private void HandleRegen()
    {
        if (RegenPerSecond <= 0f) return;
        if (CurrentHp >= MaxHp) return;

        regenAccumulator += RegenPerSecond * Time.deltaTime;

        if (regenAccumulator < 1f) return;

        int heal = Mathf.FloorToInt(regenAccumulator);
        regenAccumulator -= heal;

        CurrentHp = Mathf.Min(CurrentHp + heal, MaxHp);

        SyncUI();

        Debug.Log($"[HealthModule] 回血 +{heal} => {CurrentHp}/{MaxHp}");
    }
    private void SyncUI()
    {
        int displayHp = CurrentHp <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(CurrentHp));
        PlayerManager.Instance.SyncHp(displayHp, MaxHp);
    }

    public void TakeDamage(int amount, Transform attacker)
    {
        if (isInvincible || player.IsDead) return;

        CurrentHp -= amount;
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);

        PlayerManager.Instance.SyncHp(Mathf.RoundToInt(CurrentHp), MaxHp);

        Debug.Log($"[HealthModule] 受伤 -{amount} => {CurrentHp}/{MaxHp}");

        if (CurrentHp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(attacker));
    }

    IEnumerator HurtRoutine(Transform attacker)
    {
        player.IsStunned = true;
        isInvincible = true;

        PlayHurtVisuals();

        if (attacker != null)
        {
            Vector2 knockbackDir = (player.transform.position - attacker.position).normalized;
            player.Colli2d.enabled = false;
            player.Rigid2d.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(stunDuration);
        player.IsStunned = false;

        yield return new WaitForSeconds(invincibilityDuration - stunDuration);

        player.Colli2d.enabled = true;
        isInvincible = false;
        player.BodyRenderer.color = normalColor;
    }

    void PlayHurtVisuals()
    {
        player.BodyRenderer.DOKill();
        player.BodyRenderer.DOColor(hurtColor, 0.05f).OnComplete(() =>
        {
            player.BodyRenderer.DOColor(normalColor, 0.2f);
        });

        player.BodyRenderer.DOFade(0.5f, 0.1f).SetLoops(5, LoopType.Yoyo);
        player.transform.DOPunchScale(new Vector3(-0.2f, 0.2f, 0), 0.2f, 10, 1);
    }

    void Die()
    {
        player.IsDead = true;
        player.SetVelocity(Vector2.zero);
        player.OnDeath?.Invoke();
        Debug.Log("Player Died");
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        RecalculateStats();

        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        PlayerManager.Instance.SyncHp(Mathf.RoundToInt(CurrentHp), MaxHp);

        Debug.Log($"[HealthModule] 升级刷新 HP={CurrentHp}/{MaxHp} Regen={RegenPerSecond}/s");
    }

    private void RecalculateStats()
    {
        MaxHp = Mathf.RoundToInt(
            UpgradeManager.Instance.GetStat(ModuleType.Health, StatType.MaxHP)
        );

        RegenPerSecond =
            UpgradeManager.Instance.GetStat(ModuleType.Health, StatType.HealthRegen);

        if (MaxHp <= 0) MaxHp = 10;

        Debug.Log($"[HealthModule] Recalc MaxHp={MaxHp} Regen={RegenPerSecond}/s");
    }
}
