using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    [Header("激光基础设置")]
    public float warningTime = 1.0f;
    public float activeTime = 0.5f;
    public float maxDistance = 25f;

    [Tooltip("激光的视觉宽度 (LineRenderer渲染宽度)")]
    public float laserWidth = 1.5f;

    [Header("伤害判定优化")]
    [Tooltip("判定框宽度缩放系数 (0.1~1.0)\n调小这个值，让伤害判定只覆盖激光中心实心部分")]
    [Range(0.1f, 1f)]
    public float hitboxScale = 0.5f;

    public int damage = 1;
    [Tooltip("伤害频率：每隔多少秒造成一次伤害（防止一秒扣60次血）")]
    public float damageTickRate = 0.1f;
    public LayerMask hitLayer;

    [Header("调试")]
    public bool showDebugHitbox = true;

    private LineRenderer lr;
    private bool isFiring = false;
    private bool isActivePhase = false; // 是否处于真实伤害阶段

    // --- 追踪追踪变量 ---
    private Transform shooterTransform; // 发射者(Boss)
    private float dirMultiplier = 1f;
    private float fireOffset = 1.5f;    // 炮口偏移量
    private float nextDamageTime = 0f;  // 伤害计时器

    // 原版开火方法 (静态方向)
    public void Fire(Vector3 startPos, Vector3 direction)
    {
        transform.position = startPos;
        transform.up = -direction;
        InitLaser();
        StartCoroutine(Routine());
    }

    public void FireTracking(Transform shooter, float offset, bool reverseDirection = false)
    {
        shooterTransform = shooter;
        fireOffset = offset;
        // 如果开启反向，方向倍率设为 -1
        dirMultiplier = reverseDirection ? -1f : 1f;
        InitLaser();
        StartCoroutine(Routine());
    }

    private void InitLaser()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        isFiring = true;
        isActivePhase = false;

        // 预警特效 (细线)
        lr.widthMultiplier = 0.1f;
    }

    private void Update()
    {
        if (!isFiring || lr == null) return;

        Vector3 startPos = transform.position;
        Vector3 direction = transform.up * -1f;

        if (shooterTransform != null)
        {
            // 【核心修改】：乘上 dirMultiplier。正常是 -up，反向发射时变成 +up
            direction = shooterTransform.up * -1f * dirMultiplier;
            startPos = shooterTransform.position + direction * fireOffset;

            transform.position = startPos;
            transform.up = -direction;
        }

        Vector3 endPos = startPos + direction * maxDistance;

        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);

        if (isActivePhase)
        {
            CheckHit(startPos, direction);
        }
    }

    IEnumerator Routine()
    {
        // --- 预警阶段 ---
        float t = 0;
        while (t < warningTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // --- 激活爆发阶段 ---
        isActivePhase = true;
        lr.widthMultiplier = laserWidth; // 激光瞬间变粗

        t = 0;
        while (t < activeTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // --- 结束 ---
        isFiring = false;
        Destroy(gameObject);
    }

    private void CheckHit(Vector3 startPos, Vector3 dir)
    {
        // 频率控制：还没到下次扣血时间，直接跳过检测
        if (Time.time < nextDamageTime) return;

        float actualHitboxWidth = laserWidth * hitboxScale;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 center = (Vector2)startPos + (Vector2)dir * (maxDistance * 0.5f);
        Vector2 size = new Vector2(maxDistance, actualHitboxWidth);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, hitLayer);

        if (showDebugHitbox)
            DebugDrawBox(center, size, angle, Color.cyan, 0.05f);

        bool hitPlayer = false;
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit != null)
                {
                    var hp = hit.GetComponentInChildren<HealthModule>();
                    if (hp != null)
                    {
                        hp.TakeDamage(damage, transform);
                        hitPlayer = true;
                    }
                }
            }
        }

        // 如果扫到了玩家，重置冷却计时器
        if (hitPlayer)
        {
            nextDamageTime = Time.time + damageTickRate;
        }
    }
    void DebugDrawBox(Vector2 center, Vector2 size, float angle, Color color, float duration)
    {
        Vector2 halfSize = size / 2f;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector3 p1 = center + (Vector2)(rot * new Vector2(-halfSize.x, -halfSize.y));
        Vector3 p2 = center + (Vector2)(rot * new Vector2(-halfSize.x, halfSize.y));
        Vector3 p3 = center + (Vector2)(rot * new Vector2(halfSize.x, halfSize.y));
        Vector3 p4 = center + (Vector2)(rot * new Vector2(halfSize.x, -halfSize.y));

        Debug.DrawLine(p1, p2, color, duration);
        Debug.DrawLine(p2, p3, color, duration);
        Debug.DrawLine(p3, p4, color, duration);
        Debug.DrawLine(p4, p1, color, duration);
    }
}
