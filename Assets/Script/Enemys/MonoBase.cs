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
        // �� Awake ʱ��¼�� Inspector �����õĴ�С
        baseScale = transform.localScale;
    }

    // --- IDamageable �ӿ�ʵ�� ---
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
            // ����ͨ���ܻ���Ч
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect("EnemyHit1", 2f, 1f);
        }
    }

    public virtual void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        // Ĭ��ʵ��������һ�£����л�����������ࣨ��EnemyBase������д�˷���
        TakeDamage(amount, hitPoint, knockbackDir);
    }

    // --- ͨ���Ӿ����� ---
    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // ���ʸ�����˸
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            // �ߴ�Q������
            transform.DOKill();
            // ���޸ĵ㡿��ʹ�ü�¼�� baseScale ��� Vector3.one
            transform.localScale = baseScale;
            // ���޸ĵ㡿���� Punch �����ȳ��� baseScale��������� Boss �ܴ󣬵��Եķ���Ҳ��ȱȷŴ�
            transform.DOPunchScale(baseScale * 0.15f, 0.1f);
        }

        // �����ܻ�����
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
                var mainModule = ps.main;
                
                mainModule.startColor = normalColor;

                ps.Play();
            }
        }

        if (BackgroundFXController.Instance != null)
            BackgroundFXController.Instance.TriggerDistortion(transform.position);
    }
}
