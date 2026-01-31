using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BossAirCraft : EnemyBase
{
    [Header("Spawner Settings")]
    public GameObject drifterPrefab;

    [Tooltip("大生成间隔：两波大生成之间的等待时间")]
    public float majorSpawnInterval = 5.0f;

    [Tooltip("小生成间隔：一次大生成内部，连续生成小怪的间隔")]
    public float minorSpawnInterval = 1.0f;

    [Tooltip("每次大生成包含几次小生成")]
    public int wavesPerMajor = 3;

    // 最大小怪存在数量限制
    public int maxMinions = 6;

    [Header("Entrance Settings")]
    public Vector3 targetEntryPosition = new Vector3(0, 11, 0);
    public float enterSpeed = 3.0f;

    // 【新增】悬浮设置
    [Header("Hover Settings")]
    [Tooltip("悬浮速度：数值越大上下动得越快")]
    public float hoverSpeed = 1.0f;

    [Tooltip("悬浮幅度：上下移动的最大距离")]
    public float hoverDistance = 0.5f;

    // 内部变量
    private Transform leftSpawnPoint;
    private Transform rightSpawnPoint;
    private float majorTimer;
    private bool isEntering = true;
    private bool isSpawningWave = false;

    // 【新增】记录悬浮的中心点位置
    private Vector3 hoverAnchorPos;

    private List<GameObject> activeMinions = new List<GameObject>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        leftSpawnPoint = transform.FindDeepChild("LeftPoint");
        rightSpawnPoint = transform.FindDeepChild("RightPoint");
    }

    public override void OnSpawn()
    {
        base.OnSpawn();

        isEntering = true;
        isSpawningWave = false;
        majorTimer = majorSpawnInterval;

        activeMinions.Clear();
    }

    protected override void MoveBehavior()
    {
        if (isEntering)
        {
            EntranceMovement();
        }
        else
        {
            // 阶段二：悬浮 + 生成逻辑
            HoverMovement(); // 【新增】调用悬浮
            HandleSpawningTimer();
        }
    }

    void EntranceMovement()
    {
        Vector3 currentPos = transform.position;
        Vector3 target = new Vector3(targetEntryPosition.x, targetEntryPosition.y, currentPos.z);
        Vector3 nextPos = Vector3.MoveTowards(currentPos, target, enterSpeed * Time.deltaTime);
        rb.MovePosition(nextPos);

        if (Vector3.Distance(currentPos, target) < 0.05f)
        {
            FinishEntrance();
        }
    }

    void FinishEntrance()
    {
        isEntering = false;
        rb.velocity = Vector2.zero;

        // 【新增】入场结束后，记录当前位置作为悬浮的中心点
        hoverAnchorPos = transform.position;

        majorTimer = majorSpawnInterval;
    }

    // 【新增】悬浮运动逻辑
    void HoverMovement()
    {
        if (isDead) return;

        // 使用 Sin 函数计算当前的 Y 轴偏移量
        // Time.time * hoverSpeed 控制频率
        // * hoverDistance 控制幅度
        float newY = hoverAnchorPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;

        // 保持 X 轴位置不变（即 hoverAnchorPos.x），只改变 Y
        Vector2 targetPos = new Vector2(hoverAnchorPos.x, newY);

        // 使用 MovePosition 移动刚体
        rb.MovePosition(targetPos);
    }

    // --- 生成逻辑控制 ---
    void HandleSpawningTimer()
    {
        if (isSpawningWave) return;

        activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

        if (activeMinions.Count >= maxMinions)
        {
            return;
        }

        majorTimer -= Time.deltaTime;

        if (majorTimer <= 0)
        {
            StartCoroutine(MajorSpawnRoutine());
        }
    }

    IEnumerator MajorSpawnRoutine()
    {
        isSpawningWave = true;

        for (int i = 0; i < wavesPerMajor; i++)
        {
            SpawnDrifters();

            if (i < wavesPerMajor - 1)
            {
                yield return new WaitForSeconds(minorSpawnInterval);
            }
        }

        isSpawningWave = false;
        majorTimer = majorSpawnInterval;
    }

    void SpawnDrifters()
    {
        if (drifterPrefab == null) return;

        activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

        // 检查翅膀是否存活 + 数量限制
        if (leftSpawnPoint != null && leftSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
            CreateMinion(leftSpawnPoint.position);

        if (rightSpawnPoint != null && rightSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
            CreateMinion(rightSpawnPoint.position);
    }

    void CreateMinion(Vector3 spawnPos)
    {
        GameObject minionObj = Instantiate(drifterPrefab, spawnPos, Quaternion.identity);
        activeMinions.Add(minionObj);

        EnemyDrifter drifterScript = minionObj.GetComponent<EnemyDrifter>();
        if (drifterScript != null)
        {
            drifterScript.OnSpawn();
        }
    }
}

public static class TransformDeepChildExtension
{
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName) return c;
            foreach (Transform t in c) queue.Enqueue(t);
        }
        return null;
    }
}
