using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public enum BossPartType
{
    Invincible,  // 无敌型：挡子弹，不掉血 (供 Knight 刀刃使用)
    Destructible // 可破坏型：有独立血量，传导伤害给本体，可破坏 (供 AirCraft 炮塔机翼使用)
}

[RequireComponent(typeof(Collider2D))]
public class BossPart : MonoBehaviour, IDamageable
{
    [Header("基础配置")]
    public string partName;
    public BossPartType partType = BossPartType.Invincible;
    public int contactDamage = 1; // 对玩家造成的碰撞接触伤害

    [Header("可破坏设置 (Destructible专用)")]
    public int partMaxHp = 50;
    public bool passDamageToBoss = true; // 受击时是否将伤害传递给主Boss
    [Range(0f, 1f)] public float damageChain = 0.5f; // 传导给主Boss的伤害比例
    public GameObject explosionPrefab; // 部位被破坏时的爆炸特效

    [Header("视觉反馈")]
    public SpriteRenderer partRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;
    private GameObject hitParticlePrefab; // 缓存受击粒子预制件

    // --- 运行时数据 ---
    private int currentPartHp;
    private BossBase mainBoss;
    private Vector3 initialLocalPos;
    private Vector3 initialLocalRot;

    // --- DOTween 动画追踪器 ---
    private Tweener moveTween;
    private Tweener rotateTween;
    private Tweener scaleTween;

    // --- 事件与状态标志 ---
    public Action<BossPart> OnPartBroken; // 部位破坏时的回调委托
    public bool IsBroken => partType == BossPartType.Destructible && currentPartHp <= 0;

    // 动画锁：供本体检查当前部件是否还在移动，防止闪现
    public bool IsAnimating => (moveTween != null && moveTween.IsActive() && moveTween.IsPlaying()) ||
                               (rotateTween != null && rotateTween.IsActive() && rotateTween.IsPlaying());

    public void Initialize(BossBase boss)
    {
        mainBoss = boss;
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localEulerAngles;
        currentPartHp = partMaxHp;

        if (partRenderer == null) partRenderer = GetComponent<SpriteRenderer>();
        if (partRenderer != null) originalColor = partRenderer.color;

        // 优先尝试从主 Boss 获取受击粒子，如果没有则加载默认的
        if (mainBoss != null && mainBoss.hitParticlePrefab != null)
            hitParticlePrefab = mainBoss.hitParticlePrefab;
        else
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
    }

    // ==========================================
    // 运动与变形 API (使用 DOTween 序列)
    // ==========================================
    public void MoveToLocal(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        // 杀掉正在进行的动画，防止叠加冲突
        moveTween?.Kill();
        rotateTween?.Kill();

        moveTween = transform.DOLocalMove(targetPos, duration).SetEase(Ease.OutQuad);
        rotateTween = transform.DOLocalRotate(targetRot, duration).SetEase(Ease.OutQuad);
    }

    public void ResetToInitial(float duration)
    {
        MoveToLocal(initialLocalPos, initialLocalRot, duration);
    }

    // 执行复杂多段的序列动画
    public void ExecuteSequence(Sequence seq)
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        seq.Play();
    }

    // ==========================================
    // IDamageable 接口实现 (3个重载完整保留)
    // ==========================================
    public void TakeDamage(int amount) => TakeDamage(amount, transform.position, Vector3.zero);

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce) => TakeDamage(amount, hitPoint, knockbackDir);

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 如果已经被打爆了，就不再处理受击
        if (IsBroken) return;

        // 无论是哪种类型，只要没爆，命中部位都要播放视觉特效
        PlayHitEffect(hitPoint, hitNormal);

        // 如果是无敌护甲，处理到此为止 (挡子弹，不掉血)
        if (partType == BossPartType.Invincible) return;

        // --- 以下为可破坏类型（如 AirCraft）的血量扣除与破坏逻辑 ---
        currentPartHp -= amount;

        // 将伤害按比例传导给主Boss
        if (passDamageToBoss && mainBoss != null)
        {
            int chainDamage = Mathf.RoundToInt(amount * damageChain);
            if (chainDamage > 0)
            {
                mainBoss.TakeDamage(chainDamage, hitPoint, hitNormal);
            }
        }

        // 触发部位破坏
        if (currentPartHp <= 0)
        {
            BreakPart();
        }
    }

    // ==========================================
    // 视觉反馈与破坏特效
    // ==========================================
    private void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        // 1. 材质发红闪烁
        if (partRenderer != null)
        {
            partRenderer.DOKill();
            partRenderer.DOColor(hitColor, 0.05f).OnComplete(() => partRenderer.DOColor(originalColor, 0.1f));
        }

        // 2. Q弹缩放
        // 【核心修复】：绝对不能用 transform.DOKill()！
        // 我们只精准杀掉专属的 scaleTween，并传入 true 强制瞬间恢复 1:1:1 原比例
        scaleTween?.Kill(true);
        Vector3 currentScale = transform.localScale;
        // 先稍微放大（打击感），再恢复到 currentScale
        transform.DOScale(currentScale * 1.1f, 0.05f).OnComplete(() => {
            transform.DOScale(currentScale, 0.05f);
        });

        // 3. 使用对象池生成火花粒子特效
        if (hitParticlePrefab != null && ObjectPoolManager.Instance != null)
        {
            Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, rot);

            // 1秒后回收粒子
            Timer.Register(1f, () => {
                if (particleObj != null && ObjectPoolManager.Instance != null)
                    ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }
    }

    private void BreakPart()
    {
        // 生成爆炸特效
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(exp, 1.0f);
        }
        else
        {
            GameObject defaultExp = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");
            if (defaultExp)
            {
                GameObject exp = Instantiate(defaultExp, transform.position, Quaternion.identity);
                Destroy(exp, 1.0f);
            }
        }

        // 触发部位破坏的回调委托，通知状态机
        OnPartBroken?.Invoke(this);

        // 隐藏模型并关闭物理碰撞
        if (partRenderer != null) partRenderer.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // ==========================================
    // 对玩家造成接触伤害、破坏子弹
    // ==========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsBroken) return;

        // 【核心修复】：刀刃破坏子弹逻辑
        // 检查碰撞物体是否为玩家子弹
        if (collision.CompareTag("Bullet") )
        {
            // 如果该部件当前带有 shield 标签，则拦截并销毁子弹
            if (gameObject.CompareTag("Shield"))
            {
                // 播放一个受击火花特效
                PlayHitEffect(collision.transform.position, Vector3.zero);

                // 销毁子弹
                Destroy(collision.gameObject);
                return; // 拦截成功，不再执行后续伤害逻辑
            }
        }

        // 原有的对玩家接触伤害逻辑
        if (collision.CompareTag("Player"))
        {
            HealthModule playerHp = collision.GetComponentInChildren<HealthModule>();
            if (playerHp != null)
            {
                playerHp.TakeDamage((int)contactDamage, transform);
            }
        }
    }
    // 【你需要新增以下两个方法，实现实体物理碰撞伤害】：
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthModule playerHp = collision.gameObject.GetComponentInChildren<HealthModule>();
            if (playerHp != null)
            {
                playerHp.TakeDamage((int)contactDamage, transform);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        // 确保玩家如果一直被护盾挤在墙角，也会持续受到伤害
        if (col.gameObject.CompareTag("Player"))
        {
            HealthModule playerHp = col.gameObject.GetComponentInChildren<HealthModule>();
            if (playerHp != null)
            {
                // 注意：如果你的 TakeDamage 没有内置无敌帧(Invincibility Frames)，
                // 这里每秒会触发60次导致玩家瞬间被秒杀。
                // 如果出现秒杀情况，请确保 Player 的 HealthModule 里面有受击冷却(CD)逻辑！
                playerHp.TakeDamage((int)contactDamage, transform);
            }
        }
    }
}
