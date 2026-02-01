using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 需要 DoTween 做受击闪烁

[RequireComponent(typeof(Collider2D))]
public class BossPart : MonoBehaviour, IDamageable
{
    public float damageChain = 0.5f;

    [Header("Part Settings")]
    [Tooltip("部位独立血量")]
    public int partMaxHp = 50;

    [Tooltip("受击时是否将伤害传递给主Boss（扣总血量）")]
    public bool passDamageToBoss = true;

    [Tooltip("爆炸特效预制件")]
    public GameObject explosionPrefab;

    [Header("Visuals")]
    public SpriteRenderer partRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;

    // 运行时数据
    private int currentPartHp;
    private EnemyBase mainBoss;
    private GameObject hitParticlePrefab; // 缓存受击粒子预制件

    //部位破坏回调
    public Action<BossPart> OnPartBroken;
    public bool IsBroken => currentPartHp <= 0;

    private void Awake()
    {
        currentPartHp = partMaxHp;

        if (partRenderer == null) partRenderer = GetComponent<SpriteRenderer>();
        if (partRenderer != null) originalColor = partRenderer.color;

        mainBoss = GetComponentInParent<EnemyBase>();

        // 【新增】尝试从主 Boss 获取受击粒子，保持风格一致
        if (mainBoss != null)
        {
            hitParticlePrefab = mainBoss.hitParticlePrefab;
        }
        // 如果 Boss 没配，就加载默认的
        if (hitParticlePrefab == null)
        {
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsBroken) return; // 已经坏了就不处理了

        currentPartHp -= amount;
        PlayHitEffect(hitPoint, hitNormal);

        if (passDamageToBoss && mainBoss != null)
        {
            mainBoss.TakeDamage((int)(amount * damageChain), hitPoint, hitNormal);
        }

        if (currentPartHp <= 0)
        {
            BreakPart();
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    private void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        // --- 视觉反馈：闪烁 & 抖动 ---
        if (partRenderer != null)
        {
            partRenderer.DOKill();
            partRenderer.DOColor(hitColor, 0.05f).OnComplete(() =>
            {
                partRenderer.DOColor(originalColor, 0.1f);
            });

            // 抖动：增强了力度 (0.1 -> 0.2)，让小部件抖动更明显
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
        }

        // --- 视觉反馈：粒子特效 (之前缺少的) ---
        if (hitParticlePrefab != null)
        {
            // 使用对象池生成火花
            // 注意：这里需要 Quaternion.LookRotation 来让火花朝向正确的法线方向飞溅
            Quaternion rot = (normal != Vector3.zero) ? Quaternion.LookRotation(normal) : Quaternion.identity;

            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);

            // 1秒后回收
            Timer.Register(1f, () =>
            {
                if (particleObj != null) ObjectPoolManager.Instance.Return(particleObj);
            });

            // 播放粒子
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }
    }

    private void BreakPart()
    {
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(exp, 1.0f);
        }
        else
        {
            GameObject defaultExp = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");
            if (defaultExp) Instantiate(defaultExp, transform.position, Quaternion.identity);
        }

        OnPartBroken?.Invoke(this);

        if (partRenderer) partRenderer.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        //gameObject.SetActive(false);
    }
}
