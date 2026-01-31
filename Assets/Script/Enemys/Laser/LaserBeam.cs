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
    public LayerMask hitLayer;

    [Header("调试")]
    public bool showDebugHitbox = true;

    private LineRenderer lr;

    public void Fire(Vector3 startPos, Vector3 direction)
    {
        transform.position = startPos;

        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        Vector3 endPos = startPos + direction * maxDistance;

        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);

        StartCoroutine(Routine(startPos, endPos, direction));
    }

    IEnumerator Routine(Vector3 start, Vector3 end, Vector3 dir)
    {
        // === 预警阶段 ===
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.startColor = new Color(1, 0, 0, 0.4f);
        lr.endColor = new Color(1, 0, 0, 0.4f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        yield return new WaitForSeconds(warningTime);

        // === 伤害阶段 (视觉) ===
        // 这里使用完整的视觉宽度
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        lr.startColor = Color.red;
        lr.endColor = Color.white;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        // === 伤害判定 (物理) ===
        CheckDamage(start, end, dir);

        yield return new WaitForSeconds(activeTime);
        Destroy(gameObject);
    }

    void CheckDamage(Vector3 start, Vector3 end, Vector3 dir)
    {
        Vector3 center = start + dir * (maxDistance / 2f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 【核心修改】计算实际判定宽度
        // 使用 视觉宽度 * 缩放系数
        float actualHitboxWidth = laserWidth * hitboxScale;
        Vector2 size = new Vector2(maxDistance, actualHitboxWidth);

        // 发射检测
        RaycastHit2D[] hits = Physics2D.BoxCastAll(center, size, angle, dir, 0f, hitLayer);

        // 画出判定框 (青色) 方便你在 Scene 窗口对比
        if (showDebugHitbox)
        {
            DebugDrawBox(center, size, angle, Color.cyan, 1.0f);
        }

        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    var hp = hit.collider.GetComponentInChildren<HealthModule>();
                    if (hp != null)
                    {
                        hp.TakeDamage(damage, transform);
                    }
                }
            }
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
