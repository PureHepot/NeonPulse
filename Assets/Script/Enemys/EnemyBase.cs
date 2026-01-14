using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IPoolable
{
    [Header("Base Stats")]
    public float maxHp = 100f;
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public float contactDamage = 10f;

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


    public void OnDespawn()
    {
        currentHp = maxHp;
        isDead = false;

        if (bodyRenderer != null) bodyRenderer.color = normalColor;
        transform.localScale = Vector3.one;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        rb.simulated = true;
    }

    public void OnSpawn()
    {
        transform.DOKill();
        if (bodyRenderer != null) bodyRenderer.DOKill();

        rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        PhysicsUpdate();
    }

    protected abstract void PhysicsUpdate();

    public void TakeDamage(float amount)
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
        rb.simulated = false; // 死亡瞬间关闭物理，防止诈尸

        if (deathEffectPrefab != null)
        {
            ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        ObjectPoolManager.Instance.Return(this.gameObject);
    }


    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            Die();
        }
    }

}
