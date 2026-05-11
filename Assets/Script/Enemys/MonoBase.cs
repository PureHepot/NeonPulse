using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class MonoBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    public float maxHp = 10f;
    public float currentHp;
    protected bool isDead = false;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;
    public GameObject hitParticlePrefab;

    protected Vector3 baseScale;

    protected virtual void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        // 在 Awake 时记录下 Inspector 中设置的大小
        baseScale = transform.localScale;
    }

    // --- IDamageable 接口实现 ---
    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public virtual void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHp -= amount;
        PlayHitEffect(hitPoint, hitNormal);

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            // 播放通用受击音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect("EnemyHit1", 2f, 1f);
        }
    }

    public virtual void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        // 默认实现与上面一致，带有击退需求的子类（如EnemyBase）会重写此方法
        TakeDamage(amount, hitPoint, knockbackDir);
    }

    // --- 通用视觉表现 ---
    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // 材质高亮闪烁
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            // 尺寸Q弹反馈
            transform.DOKill();
            // 【修改点】：使用记录的 baseScale 替代 Vector3.one
            transform.localScale = baseScale;
            // 【修改点】：将 Punch 的力度乘以 baseScale，这样如果 Boss 很大，弹性的幅度也会等比放大
            transform.DOPunchScale(baseScale * 0.15f, 0.1f);
        }

        // 播放受击粒子
        if (hitParticlePrefab == null) hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        if (hitParticlePrefab != null && ObjectPoolManager.Instance != null)
        {
            Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);

            Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var mainModule = ps.main;
                mainModule.startColor = normalColor;
                ps.Play();
            }
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("EnemyDie");

        if (deathEffectPrefab == null) deathEffectPrefab = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");
        if (deathEffectPrefab != null && ObjectPoolManager.Instance != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 1. 先用变量把 main 模块缓存下来
                var mainModule = ps.main;

                // 2. 再修改变量的属性
                mainModule.startColor = normalColor;

                ps.Play();
            }
        }

        if (BackgroundFXController.Instance != null)
            BackgroundFXController.Instance.TriggerDistortion(transform.position);
    }
}
