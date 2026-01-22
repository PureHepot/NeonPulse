using UnityEngine;
using System.Collections.Generic;

// 召唤型敌人
public class EnemySummoner : EnemyBase
{
    [Header("Summoner Settings")]
    public GameObject summonPrefab;
    public float summonCD = 5f;
    public int maxSummonCount = 2;
    public Vector3 enemyScale = new Vector3(0.8f, 0.8f, 1f);

    [Header("移动参数")]
    public float moveSpeedToCenter = 2f;
    public float reachDistance = 4f;
    public float centerStayOffset = 4f;

    private List<EnemyBase> summonedEnemies = new List<EnemyBase>();
    private float summonTimer; // 召唤计时器
    private float camHalfWidth;
    private float camHalfHeight;

    // 移动到中心相关
    private Vector3 targetCenterPos;
    private bool isReachCenter = false;

    public override void OnSpawn()
    {
        base.OnSpawn();
        transform.localScale = enemyScale;
        rb.velocity = Vector2.zero;
        summonedEnemies.Clear();
        summonTimer = 0;
        isReachCenter = false;

        // 初始化相机边界
        InitCameraBounds();
        CalculateTargetCenterPos();
    }

    private void Update()
    {
        if (!isDead)
        {
            transform.localScale = enemyScale;
        }
    }

    // 先向中心移动，到达后静止并召唤
    protected override void MoveBehavior()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (!isReachCenter)
        {
            MoveToCenter();
            return;
        }

        rb.velocity = Vector2.zero;
        summonTimer += Time.deltaTime;
        if (summonTimer >= summonCD)
        {
            TrySummonEnemy();
            summonTimer = 0;
        }

        CleanNullSummonedEnemies();
    }

    private void CalculateTargetCenterPos()
    {

        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        float randomRadius = Random.Range(0f, centerStayOffset);

        float randomX = Mathf.Cos(randomAngle) * randomRadius;
        float randomY = Mathf.Sin(randomAngle) * randomRadius;

        randomX = Mathf.Clamp(randomX, -camHalfWidth + centerStayOffset, camHalfWidth - centerStayOffset);
        randomY = Mathf.Clamp(randomY, -camHalfHeight + centerStayOffset, camHalfHeight - centerStayOffset);
        targetCenterPos = new Vector3(randomX, randomY, 0);
    }

    // 向中心移动逻辑
    private void MoveToCenter()
    {
        Vector2 direction = (targetCenterPos - transform.position).normalized;
        rb.velocity = direction * moveSpeedToCenter;

        // 检测是否到达中心
        float distanceToCenter = Vector2.Distance(transform.position, targetCenterPos);
        if (distanceToCenter < reachDistance)
        {
            isReachCenter = true;
            rb.velocity = Vector2.zero; // 到达后立即静止
            transform.position = targetCenterPos;
        }
    }

    // 召唤逻辑
    private void TrySummonEnemy()
    {
        if (summonPrefab == null || summonedEnemies.Count >= maxSummonCount)
            return;

        Vector2 randomOffset = Random.insideUnitCircle * 2f;
        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject enemyObj = ObjectPoolManager.Instance.Get(summonPrefab, spawnPos, Quaternion.identity);
        EnemyBase summonedEnemy = enemyObj.GetComponent<EnemyBase>();
        if (summonedEnemy != null)
        {
            summonedEnemy.OnSpawn();
            summonedEnemies.Add(summonedEnemy);
        }
        else
        {
            Debug.LogWarning("召唤预制体未挂载EnemyBase");
            ObjectPoolManager.Instance.Return(enemyObj);
        }
    }

    // 清理空的召唤物
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

    private void InitCameraBounds()
    {
        Camera mainCam = Camera.main;
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        rb.velocity = Vector2.zero;
        summonedEnemies.Clear();
        summonTimer = 0;
        isReachCenter = false;
    }
}