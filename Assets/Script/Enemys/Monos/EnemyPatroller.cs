using UnityEngine;
using System.Collections.Generic;

// 巡逻型敌人AI
public class EnemyPatroller : EnemyBase
{
    [Header("Patroller Settings")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float patrolSpeed;
    [Header("平滑路径参数")]
    public float curveSmoothness = 0.5f; // 曲线平滑度
    public int curveSampleCount = 20;    // 每个路径段的插值点数

    private Transform currentTargetPoint;
    private int currentPointIndex = 0; // 当前巡逻点的索引
    public Vector3 enemyScale = new Vector3(0.4f, 0.4f, 1f);

    private List<Vector2> smoothPathPoints; // 存储插值后的平滑路径点
    private int currentPathIndex = 0;      

    public override void OnSpawn()
    {
        base.OnSpawn();

        currentTargetPoint = null;
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            currentTargetPoint = GetClosestPatrolPoint();
            currentPointIndex = patrolPoints.IndexOf(currentTargetPoint);
            GenerateSmoothPath(); // 生成平滑路径
        }
        else
        {
            Debug.LogWarning("未配置巡逻点");
        }
        if (patrolSpeed > 0) moveSpeed = patrolSpeed;

        SetEnemyScale();
    }

    private void Update()
    {
        if (!isDead && transform.localScale != enemyScale)
        {
            SetEnemyScale();
        }
    }

    private void SetEnemyScale()
    {
        transform.localScale = enemyScale;
    }

    protected override void MoveBehavior()
    {
        if (isDead || patrolPoints == null || patrolPoints.Count == 0)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        SmoothPatrolMovement(); 
    }

    // 生成Catmull-Rom平滑路径
    private void GenerateSmoothPath()
    {
        smoothPathPoints = new List<Vector2>();
        if (patrolPoints.Count < 2) return;

        // 遍历所有巡逻点段
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            Transform p0 = patrolPoints[(i - 1 + patrolPoints.Count) % patrolPoints.Count]; 
            Transform p1 = patrolPoints[i]; 
            Transform p2 = patrolPoints[(i + 1) % patrolPoints.Count]; 
            Transform p3 = patrolPoints[(i + 2) % patrolPoints.Count]; 

            if (p0 == null || p1 == null || p2 == null || p3 == null) continue;

            // 对每个段生成插值点，形成平滑曲线
            for (int j = 0; j < curveSampleCount; j++)
            {
                float t = j / (float)curveSampleCount;
                Vector2 point = CatmullRomInterpolation(
                    new Vector2(p0.position.x, p0.position.y),
                    new Vector2(p1.position.x, p1.position.y),
                    new Vector2(p2.position.x, p2.position.y),
                    new Vector2(p3.position.x, p3.position.y),
                    t
                );
                smoothPathPoints.Add(point);
            }
        }
    }

    // Catmull-Rom曲线插值计算
    private Vector2 CatmullRomInterpolation(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        // Catmull-Rom公式
        Vector2 a = 0.5f * (2f * p1);
        Vector2 b = 0.5f * (p2 - p0);
        Vector2 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector2 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

        return a + (b * t) + (c * t2) + (d * t3);
    }

    // 平滑巡逻移动逻辑
    private void SmoothPatrolMovement()
    {
        if (smoothPathPoints == null || smoothPathPoints.Count == 0)
        {
            PatrolMovement();
            return;
        }

        Vector2 targetPos = smoothPathPoints[currentPathIndex];
        float distanceToTarget = Vector2.Distance(transform.position, targetPos);

        Vector2 directionToTarget = (targetPos - (Vector2)transform.position).normalized;
        rb.velocity = directionToTarget * moveSpeed;

        if (distanceToTarget < 0.1f)
        {
            currentPathIndex = (currentPathIndex + 1) % smoothPathPoints.Count;

            if (currentPathIndex == 0)
            {
                GenerateSmoothPath();
            }
        }
    }

    // 直线巡逻逻辑
    private void PatrolMovement()
    {
        if (currentTargetPoint == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 计算朝向当前目标点的方向
        Vector2 directionToTarget = (currentTargetPoint.position - transform.position).normalized;

        rb.velocity = directionToTarget * moveSpeed;

        float distanceToTarget = Vector2.Distance(transform.position, currentTargetPoint.position);
        if (distanceToTarget < 0.1f)
        {
            // 遍历循环列表，切换到下一个巡逻点
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Count;
            currentTargetPoint = patrolPoints[currentPointIndex];
        }
    }

    // 找最近巡逻点
    private Transform GetClosestPatrolPoint()
    {
        Transform closestPoint = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            float distance = Vector2.Distance(transform.position, point.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }
        return closestPoint;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        rb.velocity = Vector2.zero;
        currentPointIndex = 0;
        currentPathIndex = 0; // 重置路径索引
    }

    // Gizmos绘制
    private void OnDrawGizmos()
    {
        // 绘制直线巡逻点连线（红色）
        if (patrolPoints == null || patrolPoints.Count < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            Transform current = patrolPoints[i];
            Transform next = patrolPoints[(i + 1) % patrolPoints.Count];

            if (current != null && next != null)
            {
                Gizmos.DrawLine(current.position, next.position);
            }
        }

        // 绘制平滑路径（绿色）
        if (smoothPathPoints != null && smoothPathPoints.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < smoothPathPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(smoothPathPoints[i], smoothPathPoints[i + 1]);
            }
        }
    }
}