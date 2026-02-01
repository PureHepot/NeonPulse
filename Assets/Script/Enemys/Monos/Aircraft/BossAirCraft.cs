using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BossAirCraft : EnemyBase
{
    [Header("Parts References")]
    public BossPart bodyPart;
    public BossPart leftWing;
    public BossPart rightWing;
    public BossPart leftTurretPart;
    public BossPart rightTurretPart;

    [Header("Weapon Components")]
    public BossTurret leftTurret;
    public BossTurret rightTurret;

    [Header("Spawner Settings")]
    public List<GameObject> minionPrefabs;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public int spawnCountPerWave = 3;
    public int maxMinions = 18;
    [HideInInspector] public List<GameObject> activeMinions = new List<GameObject>();

    [Header("Movement Settings")]
    public Vector3 targetEntryPosition = new Vector3(0, 11, 0);
    public float enterSpeed = 3.0f;

    [Header("Laser Settings")]
    public GameObject laserBeamObj;
    public float laserWidth = 2.0f;       // 激光判定宽度
    public float laserMaxDist = 20.0f;    // 激光最大长度
    public int laserDamage = 5;           // 激光伤害
    public float laserTickRate = 0.1f;    // 伤害频率
    public LayerMask laserHitLayer;
    public float laserModeSpawnInterval = 1.5f;

    [Header("Smooth Hover Settings")]
    public float smoothTime = 0.8f;
    public float maxSpeed = 5.0f;
    public float xFreq = 0.6f;
    public float xDist = 6f;
    public float yFreq = 1.0f;
    public float yDist = 1f;

    public Vector2 CurrentVelocity;

    [Tooltip("悬浮速度：数值越大上下动得越快")]
    public float hoverSpeed = 1.0f;
    [Tooltip("悬浮幅度：上下移动的最大距离")]
    public float hoverDistance = 0.5f;

    [Tooltip("大生成间隔：两波大生成之间的等待时间")]
    public float majorSpawnInterval = 5.0f;

    [Tooltip("小生成间隔：一次大生成内部，连续生成小怪的间隔")]
    public float minorSpawnInterval = 1.0f;

    [Tooltip("每次大生成包含几次小生成")]
    public int wavesPerMajor = 3;

    // --- 状态机 ---
    private BossState currentState;
    public Vector3 HoverAnchorPos { get; set; } // 悬浮的基准点

    // 状态实例
    public StateEntrance stateEntrance;
    public StateIdle stateIdle;
    public StateSpawn stateSpawn;
    public StateBarrage stateBarrage;
    public StateWild stateWild;
    public StateLaser stateLaser;


    private float majorTimer;
    private bool isEntering = true;
    private bool isSpawningWave = false;

    private int brokenTurretCount = 0;
    private bool bodyShieldBroken = false;

    // 【新增】记录悬浮的中心点位置
    private Vector3 hoverAnchorPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (!bodyPart) bodyPart = transform.FindDeepChild("aircraft_body")?.GetComponent<BossPart>();
        if (!leftWing) leftWing = transform.FindDeepChild("aircraftLeft")?.GetComponent<BossPart>();
        if (!rightWing) rightWing = transform.FindDeepChild("aircraftRight")?.GetComponent<BossPart>();

        if (!leftTurretPart) leftTurretPart = transform.FindDeepChild("canotower_left")?.GetComponent<BossPart>();
        if (!rightTurretPart) rightTurretPart = transform.FindDeepChild("canotower_right")?.GetComponent<BossPart>();

        if (!leftTurret) leftTurret = transform.FindDeepChild("canotower_left")?.GetComponent<BossTurret>();
        if (!rightTurret) rightTurret = transform.FindDeepChild("canotower_right")?.GetComponent<BossTurret>();

        // 监听部位破坏（可选，用于状态切换判断）
        if (leftWing) leftWing.OnPartBroken += OnWingBroken;
        if (rightWing) rightWing.OnPartBroken += OnWingBroken;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        activeMinions.Clear();

        if (leftTurretPart) { leftTurretPart.OnPartBroken -= OnTurretPartBroken; leftTurretPart.OnPartBroken += OnTurretPartBroken; }
        if (rightTurretPart) { rightTurretPart.OnPartBroken -= OnTurretPartBroken; rightTurretPart.OnPartBroken += OnTurretPartBroken; }

        brokenTurretCount = 0;
        bodyShieldBroken = false;
        if (laserBeamObj) laserBeamObj.SetActive(false);

        // 初始化状态
        stateEntrance = new StateEntrance(this);
        stateIdle = new StateIdle(this);
        stateSpawn = new StateSpawn(this);
        stateBarrage = new StateBarrage(this);
        stateWild = new StateWild(this);
        stateLaser = new StateLaser(this);

        // 初始状态：入场
        ChangeState(stateEntrance);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        ChangeState(null);
    }

    private void Update()
    {
        if (isDead) return;
        currentState?.OnUpdate();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        currentState?.OnFixedUpdate();
    }

    public void ChangeState(BossState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();
    }


    protected override void MoveBehavior()
    {
        
    }

    public void PerformSmoothHover()
    {
        float targetX = HoverAnchorPos.x + Mathf.Cos(Time.time * xFreq) * xDist;
        float targetY = HoverAnchorPos.y + Mathf.Sin(Time.time * yFreq) * yDist;
        Vector2 targetPos = new Vector2(targetX, targetY);

        Vector2 nextPos = Vector2.SmoothDamp(transform.position, targetPos, ref CurrentVelocity, smoothTime, maxSpeed);

        rb.MovePosition(nextPos);
    }


    // --- 部位破坏回调 ---
    private void OnWingBroken(BossPart part)
    {
        Debug.Log($"Boss Wing Broken: {part.name}");
        // 可以在这里播放特定的断裂特效
        BackgroundFXController.Instance.TriggerDistortion(part.transform.position);
    }

    private void OnTurretPartBroken(BossPart brokenPart)
    {
        brokenTurretCount++;

        bool isLeftBroken = (brokenPart == leftTurretPart);

        Debug.Log($"炮塔破坏: {(isLeftBroken ? "左" : "右")}, 当前破坏数: {brokenTurretCount}");

        if (brokenTurretCount == 1)
        {
            if (isLeftBroken && rightTurret != null)
            {
                rightTurret.SetWildMode(true);
            }
            else if (!isLeftBroken && leftTurret != null)
            {
                leftTurret.SetWildMode(true);
            }

            ChangeState(stateWild);
        }
        else if (brokenTurretCount >= 2)
        {
            rightTurret.SetWildMode(false);
            leftTurret.SetWildMode(false);

            EnterLaserMode();
        }
    }

    private void EnterLaserMode()
    {
        if (bodyPart != null && !bodyShieldBroken)
        {
            bodyPart.TakeDamage(99999);
            bodyShieldBroken = true;
        }

        ChangeState(stateLaser);
        Debug.Log("Boss 胸甲破碎，进入激光终极模式！");

        //BackgroundFXController.Instance.TriggerDistortion(transform.position);
        BackgroundFXController.Instance.SwitchToTheme("Boss");
    }

    protected override void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // 假设我们在Shader里定义了 "_HitFlashStrength"
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0), 0.1f);
        }

        if (hitParticlePrefab == null)
        {
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }

        if (hitParticlePrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }
    }

    public GameObject GetRandomMinionPrefab()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return null;
        return minionPrefabs[Random.Range(0, minionPrefabs.Count)];
    }

    // --- 辅助方法：生成一只小怪 ---
    public void SpawnSingleMinion(Transform spawnPoint)
    {
        activeMinions.RemoveAll(x => x == null || !x.activeInHierarchy);

        // 然后再判断数量
        if (activeMinions.Count >= maxMinions) return;

        // 检查部位破坏
        if (spawnPoint == null || !spawnPoint.gameObject.activeInHierarchy) return;

        GameObject prefab = GetRandomMinionPrefab();
        if (prefab == null) return;

        GameObject minion = ObjectPoolManager.Instance.Get(prefab, spawnPoint.position, Quaternion.identity);
        activeMinions.Add(minion);

        var enemy = minion.GetComponent<EnemyBase>();
        if (enemy) enemy.OnSpawn();
    }

    public void CleanMinionList()
    {
        activeMinions.RemoveAll(x => x == null || !x.activeInHierarchy);
    }

    private void OnDrawGizmosSelected()
    {
        // 如果没有开启激光相关的引用或参数，就不画
        if (laserWidth <= 0 || laserMaxDist <= 0) return;

        Gizmos.color = Color.red;

        // 1. 获取对应 StateLaser 中 BoxCast 的参数
        Vector3 startPos = transform.position; // 起点
        Vector2 boxSize = new Vector2(laserWidth, 0.1f); // 判定盒大小 (StateLaser里写的是0.1高)
        Vector3 direction = Vector3.down; // 方向
        float distance = laserMaxDist; // 最大距离

        // 为了演示实际击中位置，我们可以尝试进行一次真实的射线检测 (仅在编辑器模式下)
        // 注意：这可能会稍微影响编辑器性能，但能看到实际挡在哪了
        /*
        RaycastHit2D hit = Physics2D.BoxCast(startPos, boxSize, 0f, direction, distance, laserHitLayer);
        if (hit.collider != null)
        {
            distance = hit.distance; // 如果打中东西，只画到打中点
            Gizmos.color = Color.yellow; // 打中时变黄
        }
        */

        // 2. 绘制起点盒子
        Gizmos.DrawWireCube(startPos, boxSize);

        // 3. 绘制终点盒子
        Vector3 endPos = startPos + direction * distance;
        Gizmos.DrawWireCube(endPos, boxSize);

        // 4. 绘制连接线 (模拟扫过的区域)
        Vector3 halfWidth = Vector3.right * (laserWidth * 0.5f);
        Gizmos.DrawLine(startPos - halfWidth, endPos - halfWidth); // 左边缘
        Gizmos.DrawLine(startPos + halfWidth, endPos + halfWidth); // 右边缘

        // 5. 绘制中心线
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawLine(startPos, endPos);
    }

    //void EntranceMovement()
    //{
    //    Vector3 currentPos = transform.position;
    //    Vector3 target = new Vector3(targetEntryPosition.x, targetEntryPosition.y, currentPos.z);
    //    Vector3 nextPos = Vector3.MoveTowards(currentPos, target, enterSpeed * Time.deltaTime);
    //    rb.MovePosition(nextPos);

    //    if (Vector3.Distance(currentPos, target) < 0.05f)
    //    {
    //        FinishEntrance();
    //    }
    //}

    //void FinishEntrance()
    //{
    //    isEntering = false;
    //    rb.velocity = Vector2.zero;

    //    hoverAnchorPos = transform.position;

    //    majorTimer = majorSpawnInterval;
    //}

    //void HoverMovement()
    //{
    //    if (isDead) return;

    //    // 使用 Sin 函数计算当前的 Y 轴偏移量
    //    // Time.time * hoverSpeed 控制频率
    //    // * hoverDistance 控制幅度
    //    float newY = hoverAnchorPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;

    //    // 保持 X 轴位置不变（即 hoverAnchorPos.x），只改变 Y
    //    Vector2 targetPos = new Vector2(hoverAnchorPos.x, newY);

    //    // 使用 MovePosition 移动刚体
    //    rb.MovePosition(targetPos);
    //}

    //// --- 生成逻辑控制 ---
    //void HandleSpawningTimer()
    //{
    //    if (isSpawningWave) return;

    //    activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

    //    if (activeMinions.Count >= maxMinions)
    //    {
    //        return;
    //    }

    //    majorTimer -= Time.deltaTime;

    //    if (majorTimer <= 0)
    //    {
    //        StartCoroutine(MajorSpawnRoutine());
    //    }
    //}

    //IEnumerator MajorSpawnRoutine()
    //{
    //    isSpawningWave = true;

    //    for (int i = 0; i < wavesPerMajor; i++)
    //    {
    //        SpawnDrifters();

    //        if (i < wavesPerMajor - 1)
    //        {
    //            yield return new WaitForSeconds(minorSpawnInterval);
    //        }
    //    }

    //    isSpawningWave = false;
    //    majorTimer = majorSpawnInterval;
    //}

    //void SpawnDrifters()
    //{
    //    if (drifterPrefab == null) return;

    //    activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

    //    // 检查翅膀是否存活 + 数量限制
    //    if (leftSpawnPoint != null && leftSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
    //        CreateMinion(leftSpawnPoint.position);

    //    if (rightSpawnPoint != null && rightSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
    //        CreateMinion(rightSpawnPoint.position);
    //}

    //void CreateMinion(Vector3 spawnPos)
    //{
    //    GameObject minionObj = Instantiate(drifterPrefab, spawnPos, Quaternion.identity);
    //    activeMinions.Add(minionObj);

    //    EnemyDrifter drifterScript = minionObj.GetComponent<EnemyDrifter>();
    //    if (drifterScript != null)
    //    {
    //        drifterScript.OnSpawn();
    //    }
    //}
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
