using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 保留受击动画需要

[RequireComponent(typeof(Collider2D))]
public class BossFaceRelay : MonoBehaviour, IDamageable
{
    [Header("绑定 Boss 本体")]
    [Tooltip("如果不填，会自动在父物体中查找")]
    public BossSinger mainBoss;

    [Header("视觉反馈")]
    public SpriteRenderer faceRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;

    // 受击特效（火花）
    private GameObject hitParticlePrefab;

    private void Awake()
    {
        // 1. 自动寻找 Boss 本体
        if (mainBoss == null)
        {
            mainBoss = GetComponentInParent<BossSinger>();
        }

        // 2. 初始化渲染器颜色
        if (faceRenderer == null) faceRenderer = GetComponent<SpriteRenderer>();
        if (faceRenderer != null) originalColor = faceRenderer.color;

        // 3. 尝试从 Boss 那里获取通用的受击特效（保持风格统一）
        if (mainBoss != null && mainBoss.hitParticlePrefab != null)
        {
            hitParticlePrefab = mainBoss.hitParticlePrefab;
        }
        else
        {
            // 如果没找到，加载默认的
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }
    }

    // --- IDamageable 接口实现 ---

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 1. 播放 Face 自己的受击表现（闪光、抖动）
        PlayHitEffect(hitPoint, hitNormal);

        // 2. 【核心】直接把伤害转发给 Boss 本体
        if (mainBoss != null)
        {
            // 直接调用 BossSinger 的 TakeDamage
            // 这样就会触发 BossSinger 里的分阶段扣血、转阶段等所有逻辑
            mainBoss.TakeDamage(amount, hitPoint, hitNormal);
        }
    }

    // 重载方法兼容
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        // 转发带击退参数的伤害（虽然Boss可能不吃击退，但保持接口完整）
        PlayHitEffect(hitPoint, knockbackDir);
        if (mainBoss != null)
        {
            mainBoss.TakeDamage(amount, hitPoint, knockbackDir, customForce);
        }
    }

    // --- 视觉表现 (从 BossPart 简化而来) ---
    private void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        // 1. 变色闪烁 + Q弹抖动
        if (faceRenderer != null)
        {
            faceRenderer.DOKill();
            faceRenderer.DOColor(hitColor, 0.05f).OnComplete(() =>
            {
                faceRenderer.DOColor(originalColor, 0.1f);
            });

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.1f);
        }

        // 2. 播放粒子特效
        if (hitParticlePrefab != null)
        {
            Quaternion rot = (normal != Vector3.zero) ? Quaternion.LookRotation(normal) : Quaternion.identity;

            // 使用对象池生成
            if (ObjectPoolManager.Instance != null)
            {
                GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);
                Timer.Register(1f, () => ObjectPoolManager.Instance.Return(particleObj));
                particleObj.GetComponent<ParticleSystem>()?.Play();
            }
            else
            {
                // 如果没有对象池，直接实例化销毁（防备用）
                GameObject p = Instantiate(hitParticlePrefab, pos, rot);
                Destroy(p, 1f);
            }
        }
    }
}
