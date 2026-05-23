using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class MechBase : MonoBehaviour, IPoolable, IDamageable
{
    [Header("Base Stats")]
    public float maxHp = 30f;
    public float duration = 15f;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;

    protected float currentHp;
    protected Rigidbody2D rb;
    protected bool isDead;
    protected float timer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void OnSpawn()
    {
        currentHp = maxHp;
        isDead = false;
        timer = 0f;
        if (bodyRenderer != null) bodyRenderer.color = normalColor;
        transform.localScale = Vector3.one;
    }

    public virtual void OnDespawn()
    {
        transform.DOKill();
        if (bodyRenderer != null) bodyRenderer.DOKill();
        rb.velocity = Vector2.zero;
    }

    protected virtual void Update()
    {
        if (isDead) return;

        timer += Time.deltaTime;
        if (timer >= duration)
            Die();
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        PlayHitEffect();

        if (currentHp <= 0)
            Die();
    }

    public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHp -= amount;
        PlayHitEffect(hitPoint, hitNormal);

        if (currentHp <= 0)
            Die();
    }

    protected virtual void PlayHitEffect()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.DOColor(hitColor, 0.05f).OnComplete(() =>
            {
                bodyRenderer.DOColor(normalColor, 0.1f);
            });
            transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
        }
    }

    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.1f);
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        if (deathEffectPrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });
            ParticleSystem ps = particleObj != null ? particleObj.GetComponent<ParticleSystem>() : null;
            if (ps != null) ps.Play();
        }

        ObjectPoolManager.Instance.Return(gameObject);
    }
}
