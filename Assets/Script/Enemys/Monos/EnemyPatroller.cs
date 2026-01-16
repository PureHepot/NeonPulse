using UnityEngine;
using System.Collections.Generic; 

// 巡逻型敌人AI
public class EnemyPatroller : EnemyBase
{
    [Header("Patroller Settings")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float patrolSpeed;

    private Transform currentTargetPoint; 
    private int currentPointIndex = 0; // 当前巡逻点的索引
    public Vector3 parentScale = new Vector3(0.4f, 0.4f, 1f);

    // 添加修改尺寸逻辑
    public override void OnSpawn()
    {
        base.OnSpawn();

        currentTargetPoint = null;
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            currentTargetPoint = GetClosestPatrolPoint();
            currentPointIndex = patrolPoints.IndexOf(currentTargetPoint);
        }
        else
        {
            Debug.LogWarning("未配置巡逻点");
        }
        if (patrolSpeed > 0) moveSpeed = patrolSpeed;

        transform.localScale = parentScale;
    }

    protected override void MoveBehavior()
    {
        if (isDead || patrolPoints == null || patrolPoints.Count == 0)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        PatrolMovement();
    }

    // 移动逻辑
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
    }

    // Gizmos绘制
    private void OnDrawGizmos()
    {
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
    }
}