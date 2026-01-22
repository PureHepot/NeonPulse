using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IPoolable, IDamageable
{
    [Header("Base Stats")]
    public float maxHp = 100f;
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public int contactDamage = 10;
    public int enemyExp = 10;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;

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
        if (isDead) return;
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
            bodyRenderer.DOColor(hitColor, 0.05f).OnComplete(() => {
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

        if (deathEffectPrefab != null)
        {
            ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
        }

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
}
