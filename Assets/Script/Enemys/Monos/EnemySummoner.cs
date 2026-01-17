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

    public float offsetToCenter = 2f; // 向屏幕中心偏移的距离

    private List<EnemyBase> summonedEnemies = new List<EnemyBase>();
    private float summonTimer; // 召唤计时器
    private float camHalfWidth; 
    private float camHalfHeight; 

    public override void OnSpawn()
    {
        base.OnSpawn();
        transform.localScale = enemyScale;
        rb.velocity = Vector2.zero;
        summonedEnemies.Clear();
        summonTimer = 0;
        InitCameraBounds();
        AdjustPositionToCenter();
    }

    // 定时召唤，无任何移动
    protected override void MoveBehavior()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
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

    // 初始化屏幕边界
    private void InitCameraBounds()
    {
        Camera mainCam = Camera.main;
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
    }

    // 调整召唤者生成位置
    private void AdjustPositionToCenter()
    {
        Vector3 newPos = transform.position;

        if (Mathf.Abs(newPos.x) >= camHalfWidth)
        {
            newPos.x = Mathf.Sign(newPos.x) * (camHalfWidth - offsetToCenter);
        }

        if (Mathf.Abs(newPos.y) >= camHalfHeight)
        {
            newPos.y = Mathf.Sign(newPos.y) * (camHalfHeight - offsetToCenter);
        }

        transform.position = newPos;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        rb.velocity = Vector2.zero;
        summonedEnemies.Clear();
        summonTimer = 0;
    }
}