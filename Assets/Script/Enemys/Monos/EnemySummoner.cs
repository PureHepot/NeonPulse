using UnityEngine;
using System.Collections.Generic;

// 召唤型敌人（尺寸0.8 + 仅生成召唤物 + 复用基类碰撞）
public class EnemySummoner : EnemyBase
{
    [Header("召唤配置")]
    public GameObject summonPrefab; // 拖入Patroller/Chaser等已有AI的敌人预制体
    public float summonCD = 2f;     // 召唤冷却（秒）
    public int maxSummonCount = 3;  // 最大同时召唤数
    public float fleeSpeed = 8f;  // 自身躲避速度

    [Header("尺寸配置")]
    public Vector3 enemyScale = new Vector3(0.8f, 0.8f, 1f); // 固定尺寸0.8

    private List<EnemyBase> summonedEnemies = new List<EnemyBase>(); // 仅防超上限
    private float summonTimer; // 基础计时器

    // 生成时初始化（应用尺寸）
    public override void OnSpawn()
    {
        base.OnSpawn();
        moveSpeed = fleeSpeed; // 召唤者只躲避
        transform.localScale = enemyScale; // 关键：设置尺寸为0.8
        summonedEnemies.Clear();
        summonTimer = 0;
    }

    // 核心行为：躲避玩家 + 定时召唤
    protected override void MoveBehavior()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 1. 召唤者向远离玩家的方向移动
        FleeFromPlayer();

        // 2. 计时召唤
        summonTimer += Time.deltaTime;
        if (summonTimer >= summonCD)
        {
            TrySummonEnemy();
            summonTimer = 0;
        }

        // 3. 仅清理空引用（避免内存泄漏）
        CleanNullSummonedEnemies();
    }

    #region 核心召唤逻辑（召唤物自动执行原有AI）
    private void TrySummonEnemy()
    {
        if (summonPrefab == null || summonedEnemies.Count >= maxSummonCount)
            return;

        // 随机召唤位置（自身周围2米）
        Vector2 randomOffset = Random.insideUnitCircle * 2f;
        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

        // 从对象池取敌人
        GameObject enemyObj = ObjectPoolManager.Instance.Get(summonPrefab, spawnPos, Quaternion.identity);
        EnemyBase summonedEnemy = enemyObj.GetComponent<EnemyBase>();
        if (summonedEnemy != null)
        {
            summonedEnemy.OnSpawn(); // 关键：触发召唤物原有AI初始化
            summonedEnemies.Add(summonedEnemy);
        }
        else
        {
            Debug.LogWarning("召唤预制体未挂载EnemyBase！");
            ObjectPoolManager.Instance.Return(enemyObj);
        }
    }

    // 仅清理空引用
    private void CleanNullSummonedEnemies()
    {
        for (int i = summonedEnemies.Count - 1; i >= 0; i--)
        {
            if (summonedEnemies[i] == null)
            {
                summonedEnemies.RemoveAt(i);
            }
        }
    }
    #endregion

    #region 召唤者躲避逻辑
    private void FleeFromPlayer()
    {
        if (playerTransform == null) return;

        Vector2 fleeDir = (transform.position - playerTransform.position).normalized;
        rb.velocity = fleeDir * moveSpeed;
    }
    #endregion

    // 归还对象池时清理
    public override void OnDespawn()
    {
        base.OnDespawn();
        rb.velocity = Vector2.zero;
        summonedEnemies.Clear();
        summonTimer = 0;
    }

    // Gizmos可视化召唤范围
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}