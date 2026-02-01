using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSniperBullet : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public float speed = 40f;
    public int damage = 4;
    public float lifeTime = 3f;

    [Tooltip("最大穿透数量 (例如 2 表示最多击中 2 个敌人)")]
    public int penetrationCount = 2;

    [Header("Detection")]
    public Vector2 boxSize = new Vector2(1f, 0.2f);
    public LayerMask hitLayer;

    private float timer;
    private int penetrationLeft;

    private HashSet<Collider2D> hitTargets = new();

    public void OnSpawn()
    {
        timer = 0f;
        penetrationLeft = penetrationCount;
        hitTargets.Clear();

        GetComponent<TrailRenderer>()?.Clear();
    }

    public void OnDespawn()
    {
    }

    void Update()
    {
        float moveDistance = speed * Time.deltaTime;

        CheckCollision(moveDistance);

        if (gameObject.activeInHierarchy)
        {
            transform.Translate(Vector3.right * moveDistance);
        }

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
    }

    void CheckCollision(float distance)
    {
        // 使用 BoxCastAll 穿透检测
        // origin: 当前位置
        // size: 判定盒大小
        // angle: 随子弹旋转
        // direction: 前方 (transform.right)
        // distance: 本帧移动距离
        // layerMask: 攻击目标层级
        RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, boxSize, transform.eulerAngles.z, transform.right, distance, hitLayer);

        // 关键：按距离排序，确保先打中近的
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            Collider2D other = hit.collider;

            // 过滤掉 Trigger (除非它是敌人的受击框) 和 已经打过的目标
            if (other.isTrigger && !other.CompareTag("Enemy")) continue;
            if (hitTargets.Contains(other)) continue;

            // 1. 处理撞墙 (非 Enemy 且在 hitLayer 中的物体，视为阻挡物)
            if (!other.CompareTag("Enemy"))
            {
                // 可以在这里生成撞墙特效
                // transform.position = hit.point; // 可选：把子弹瞬移到墙面上再销毁
                ObjectPoolManager.Instance.Return(gameObject);
                return; // 撞墙后直接结束，不再处理后面的穿透
            }

            // 2. 处理撞怪
            if (other.CompareTag("Enemy"))
            {
                IDamageable target = other.GetComponent<IDamageable>();
                if (target != null)
                {
                    hitTargets.Add(other); // 记录命中

                    // 造成伤害
                    // 优先尝试获取 EnemyBase 以应用带击退的高级伤害
                    EnemyBase enemy = other.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        Vector3 knockbackDir = transform.right;
                        // 5f 是击退力度示例，可视需求提取为变量
                        enemy.TakeDamage(damage, hit.point, knockbackDir, 5f);
                    }
                    else
                    {
                        // 普通伤害
                        target.TakeDamage(damage);
                    }

                    // 扣除穿透次数
                    penetrationLeft--;

                    // 次数用尽则销毁
                    if (penetrationLeft <= 0)
                    {
                        ObjectPoolManager.Instance.Return(gameObject);
                        return;
                    }
                }
            }
        }
    }

    // 在编辑器中绘制判定框，方便调试大小
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Vector3.zero, Vector3.right * speed * 0.1f); // 示意飞行方向
    }

    //[Header("Settings")]
    //public float speed = 40f;
    //public int damage = 4;
    //public float lifeTime = 3f;
    //public int penetrationCount = 2;

    //private float timer;
    //private int penetrationLeft;

    //// 防止同一个敌人被多次命中
    //private HashSet<Collider2D> hitTargets = new();

    //public void OnSpawn()
    //{
    //    timer = 0f;
    //    penetrationLeft = penetrationCount;
    //    hitTargets.Clear();

    //    GetComponent<TrailRenderer>()?.Clear();
    //}

    //public void OnDespawn()
    //{
    //}

    //void Update()
    //{
    //    transform.Translate(Vector3.right * speed * Time.deltaTime);

    //    timer += Time.deltaTime;
    //    if (timer >= lifeTime)
    //    {
    //        ObjectPoolManager.Instance.Return(gameObject);
    //    }
    //}

    //void OnTriggerEnter2D(Collider2D other)
    //{
    //    if (!other.CompareTag("Enemy")) return;
    //    if (hitTargets.Contains(other)) return;

    //    IDamageable target = other.GetComponent<IDamageable>();
    //    if (target == null) return;

    //    hitTargets.Add(other);

    //    Vector3 hitPoint = other.ClosestPoint(transform.position);
    //    Vector3 hitNormal = transform.right;

    //    target.TakeDamage(damage, hitPoint, hitNormal);

    //    penetrationLeft--;

    //    if (penetrationLeft < 0)
    //    {
    //        ObjectPoolManager.Instance.Return(gameObject);
    //    }
    //}
}
