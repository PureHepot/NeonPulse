using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IPoolable, IDamageable
{
    [Header("Base Stats")]
    public float maxHp = 10f;
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public int contactDamage = 1;
    public int enemyExp = 10;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;
    public GameObject hitParticlePrefab;

    [Header("Knockback Settings")]
    public bool canKnockback = false;
    protected bool isKnockbacking;
    public float knockbackForce = 8f;
    public float knockbackTorque = 20f;

    protected float currentHp;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void OnSpawn()
    {
        currentHp = maxHp;
        isDead = false;
        this.gameObject.layer = LayerMask.NameToLayer("EnemySpawning");

        if (bodyRenderer != null) bodyRenderer.color = normalColor;
        transform.localScale = Vector3.one;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        rb.simulated = true;
    }

    public virtual void OnDespawn()
    {
        Debug.Log("我被OnDespawn了");
        transform.DOKill();
        if (bodyRenderer != null) bodyRenderer.DOKill();

        rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockbacking) return;
        MoveBehavior();
    }

    protected abstract void MoveBehavior();

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;

        PlayHitEffect();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void PlayHitEffect()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.DOColor(hitColor, 0.05f).OnComplete(() =>
            {
                bodyRenderer.DOColor(normalColor, 0.1f);
            });

            // 简单的受击缩放（Q弹的感觉）
            transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.simulated = false;
        AudioManager.Instance.PlayEffect("EnemyDie");

        if (deathEffectPrefab == null)
        {
            deathEffectPrefab = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");
        }

        if (deathEffectPrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }

        BackgroundFXController.Instance.TriggerDistortion(transform.position);

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.AddExperience(enemyExp);
        }

        WaveManager.Instance.RegisterEnemyDeath();
        ObjectPoolManager.Instance.Return(this.gameObject);
    }


    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        var shield = collision.collider.gameObject.GetComponent<ShieldController>();
        if (shield != null)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHp -= amount;

        PlayHitEffect(hitPoint, hitNormal);

        if (canKnockback)
        {
            ApplyKnockback(hitNormal);
        }

        if (currentHp <= 0) Die();
        else AudioManager.Instance.PlayEffect("EnemyHit");
    }

    protected virtual void ApplyKnockback(Vector3 hitNormal)
    {
        isKnockbacking = true;

        rb.velocity = Vector2.zero;

        Vector2 forceDir = hitNormal.normalized;
        rb.AddForce(forceDir * knockbackForce, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-knockbackTorque, knockbackTorque), ForceMode2D.Impulse);

        Timer.Register(0.2f, () =>
        {
            isKnockbacking = false;
        });
    }

    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // 假设我们在Shader里定义了 "_HitFlashStrength"
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.1f);
        }

        if (hitParticlePrefab == null)
        {
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }

        if (hitParticlePrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }
    }
}
