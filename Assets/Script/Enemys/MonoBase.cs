using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class MonoBase : MonoBehaviour, IDamageable
{
    protected const float DefaultHitFlashStrength = 0.1f;
    protected static readonly int HitFlashStrengthId = Shader.PropertyToID("_HitFlashStrength");

    [Header("Base Stats")]
    public float maxHp = 10f;
    public float currentHp;
    protected bool isDead;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;
    public GameObject hitParticlePrefab;

    protected Vector3 baseScale;

    protected virtual void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        baseScale = transform.localScale;
        ResetHitFlashVisuals();
    }

    public virtual void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead)
            return;

        currentHp -= amount;
        PlayHitEffect(hitPoint, hitNormal);

        if (currentHp <= 0f)
        {
            Die();
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("EnemyHit1", 2f, 1f);
        }
    }

    public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        TakeDamage(amount, hitPoint, knockbackDir);
    }

    public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce, float customTorque)
    {
        TakeDamage(amount, hitPoint, knockbackDir, customForce);
    }

    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat(HitFlashStrengthId, 2f);
            bodyRenderer.material.DOFloat(DefaultHitFlashStrength, HitFlashStrengthId, 0.8f);

            transform.DOKill();
            transform.localScale = baseScale;
            transform.DOPunchScale(baseScale * 0.15f, 0.1f);
        }

        if (hitParticlePrefab == null)
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");

        if (hitParticlePrefab != null && ObjectPoolManager.Instance != null)
        {
            Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);
            Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = normalColor;
                ps.Play();
            }
        }
    }

    protected void ResetHitFlashVisuals()
    {
        if (bodyRenderer == null)
            return;

        bodyRenderer.DOKill();
        Material material = bodyRenderer.material;
        if (material == null)
            return;

        material.DOKill();
        if (material.HasProperty(HitFlashStrengthId))
            material.SetFloat(HitFlashStrengthId, DefaultHitFlashStrength);
    }

    protected virtual void Die()
    {
        isDead = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("EnemyDie");

        if (deathEffectPrefab == null)
            deathEffectPrefab = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");

        if (deathEffectPrefab != null && ObjectPoolManager.Instance != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = normalColor;
                ps.Play();
            }
        }

        BackgroundFXController.Instance?.TriggerDistortion(transform.position);
    }
}
