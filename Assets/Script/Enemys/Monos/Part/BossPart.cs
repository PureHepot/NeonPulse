using System;
using DG.Tweening;
using UnityEngine;

public enum BossPartType
{
    Invincible,
    Destructible
}

[RequireComponent(typeof(Collider2D))]
public class BossPart : MonoBehaviour, IDamageable
{
    [Header("Part")]
    public string partName;
    public BossPartType partType = BossPartType.Invincible;
    public float contactDamage = 1f;

    [Header("Destructible")]
    public float partMaxHp = 50f;
    public bool passDamageToBoss = true;
    [Range(0f, 1f)] public float damageChain = 0.5f;
    public GameObject explosionPrefab;

    [Header("Visuals")]
    public SpriteRenderer partRenderer;
    public Color hitColor = Color.red;

    private Color originalColor;
    private GameObject hitParticlePrefab;
    private float currentPartHp;
    private MonoBase hostBoss;
    private BossBase mainBoss;
    private Vector3 initialLocalPos;
    private Vector3 initialLocalRot;
    private Tweener moveTween;
    private Tweener rotateTween;
    private Tweener scaleTween;

    public Action<BossPart> OnPartBroken;
    public bool IsBroken => partType == BossPartType.Destructible && currentPartHp <= 0f;
    public bool IsAnimating =>
        (moveTween != null && moveTween.IsActive() && moveTween.IsPlaying()) ||
        (rotateTween != null && rotateTween.IsActive() && rotateTween.IsPlaying());

    public void Initialize(BossBase boss)
    {
        Initialize((MonoBase)boss);
        mainBoss = boss;
    }

    public void Initialize(MonoBase boss)
    {
        hostBoss = boss;
        mainBoss = boss as BossBase;
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localEulerAngles;
        currentPartHp = partMaxHp;

        if (partRenderer == null)
            partRenderer = GetComponent<SpriteRenderer>();

        if (partRenderer != null)
        {
            originalColor = partRenderer.color;
            partRenderer.enabled = true;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        hitParticlePrefab = hostBoss != null && hostBoss.hitParticlePrefab != null
            ? hostBoss.hitParticlePrefab
            : Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
    }

    public void MoveToLocal(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        moveTween = transform.DOLocalMove(targetPos, duration).SetEase(Ease.OutQuad);
        rotateTween = transform.DOLocalRotate(targetRot, duration).SetEase(Ease.OutQuad);
    }

    public void ResetToInitial(float duration)
    {
        MoveToLocal(initialLocalPos, initialLocalRot, duration);
    }

    public void ExecuteSequence(Sequence seq)
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        seq.Play();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsBroken)
            return;

        PlayHitEffect(hitPoint, hitNormal);
        if (partType == BossPartType.Invincible)
            return;

        currentPartHp -= amount;
        if (passDamageToBoss && hostBoss != null)
        {
            float chainDamage = amount * damageChain;
            if (chainDamage > 0f)
                hostBoss.TakeDamage(chainDamage, hitPoint, hitNormal);
        }

        if (currentPartHp <= 0f)
            BreakPart();
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        TakeDamage(amount, hitPoint, knockbackDir);
    }

    private void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (partRenderer != null)
        {
            partRenderer.DOKill();
            partRenderer.DOColor(hitColor, 0.05f).OnComplete(() => partRenderer.DOColor(originalColor, 0.1f));
        }

        scaleTween?.Kill(true);
        Vector3 currentScale = transform.localScale;
        scaleTween = transform.DOScale(currentScale * 1.1f, 0.05f).OnComplete(() =>
        {
            transform.DOScale(currentScale, 0.05f);
        });

        if (hitParticlePrefab != null && ObjectPoolManager.Instance != null)
        {
            Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);
            Timer.Register(1f, () =>
            {
                if (particleObj != null && ObjectPoolManager.Instance != null)
                    ObjectPoolManager.Instance.Return(particleObj);
            });
            particleObj.GetComponent<ParticleSystem>()?.Play();
        }
    }

    private void BreakPart()
    {
        GameObject fxPrefab = explosionPrefab != null
            ? explosionPrefab
            : Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");

        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1f);
        }

        OnPartBroken?.Invoke(this);

        if (partRenderer != null)
            partRenderer.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsBroken)
            return;

        if (collision.CompareTag("Bullet") && gameObject.CompareTag("Shield"))
        {
            PlayHitEffect(collision.transform.position, Vector3.zero);
            Destroy(collision.gameObject);
            return;
        }

        if (collision.CompareTag("Player"))
            collision.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }
}
