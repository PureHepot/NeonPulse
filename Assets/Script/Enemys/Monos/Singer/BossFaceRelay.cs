using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossFaceRelay : MonoBehaviour, IDamageable
{
    [Header("Boss")]
    public BossSinger mainBoss;

    [Header("Visuals")]
    public SpriteRenderer faceRenderer;
    public Color hitColor = Color.red;

    private Color originalColor;
    private GameObject hitParticlePrefab;

    private void Awake()
    {
        if (mainBoss == null)
            mainBoss = GetComponentInParent<BossSinger>();

        if (faceRenderer == null)
            faceRenderer = GetComponent<SpriteRenderer>();

        if (faceRenderer != null)
            originalColor = faceRenderer.color;

        hitParticlePrefab = mainBoss != null && mainBoss.hitParticlePrefab != null
            ? mainBoss.hitParticlePrefab
            : Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        PlayHitEffect(hitPoint, hitNormal);
        if (mainBoss != null)
            mainBoss.TakeDamage(amount, hitPoint, hitNormal);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        PlayHitEffect(hitPoint, knockbackDir);
        if (mainBoss != null)
            mainBoss.TakeDamage(amount, hitPoint, knockbackDir, customForce);
    }

    private void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (faceRenderer != null)
        {
            faceRenderer.DOKill();
            faceRenderer.DOColor(hitColor, 0.05f).OnComplete(() => faceRenderer.DOColor(originalColor, 0.1f));

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.1f);
        }

        if (hitParticlePrefab == null)
            return;

        Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
        if (ObjectPoolManager.Instance != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);
            Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));
            particleObj.GetComponent<ParticleSystem>()?.Play();
        }
        else
        {
            GameObject particleObj = Instantiate(hitParticlePrefab, pos, rot);
            Destroy(particleObj, 1f);
        }
    }
}
