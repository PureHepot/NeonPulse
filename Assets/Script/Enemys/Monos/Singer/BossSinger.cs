using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSinger : EnemyBase
{
    [Header("=== 状态机核心 ===")]
    private SingerBossBaseState currentState;
    public BossPhase1State Phase1 { get; private set; }
    public BossPhase2State Phase2 { get; private set; }
    public BossPhase3State Phase3 { get; private set; }

    [Header("=== Phase 1: 引用 ===")]
    public GameObject idleForm;
    public GameObject battleForm;
    public Transform faceAngry;
    public Transform speakerLeft;
    public Transform speakerRight;
    public Transform hairLeft;
    public Transform hairRight;

    [Header("=== Phase 1: 部署位置 ===")]
    public Vector3 faceTargetPos = new Vector3(0, 4, 0);
    public Vector3 speakerLeftTargetPos = new Vector3(-7, 4, 0);
    public Vector3 speakerRightTargetPos = new Vector3(7, 4, 0);
    public Vector3 hairLeftTargetPos = new Vector3(-8.5f, 0, 0);
    public Vector3 hairRightTargetPos = new Vector3(8.5f, 0, 0);
    public float deployDuration = 2.0f;

    [Header("=== Phase 1: 头发运动 ===")]
    public Vector3 hairLeftTargetRot = new Vector3(0, 0, 90);
    public Vector3 hairRightTargetRot = new Vector3(0, 0, -90);
    public float hairMoveSpeed = 2.0f;
    public float hairTopY = 2.0f;
    public float hairBottomY = -4.0f;

    // 【新增】脸部悬浮参数
    [Header("=== Phase 1: 脸部悬浮参数 ===")]
    [Tooltip("悬浮幅度 (上下移动距离)")]
    public float faceHoverAmplitude = 0.5f;
    [Tooltip("悬浮速度 (数值越大越快)")]
    public float faceHoverSpeed = 1.5f;

    [Header("=== Phase 1: 弹幕攻击 ===")]
    public GameObject redBulletPrefab;
    public float p1AttackInterval = 3.5f;
    [Tooltip("连发轮数")] public int roundsPerAttack = 3;
    [Tooltip("单发间隔")] public float pointStaggerDelay = 0.15f;
    [Tooltip("散射角度")] public float showerSpreadAngle = 15f;
    [Tooltip("子弹速度范围")] public Vector2 bulletSpeedRange = new Vector2(3f, 5f);
    public int maxSearchIndex = 5;

    [Header("=== Phase 2: 激光阶段设置 ===")]
    public GameObject levelHairPrefab;
    public GameObject verticalHairPrefab;
    public GameObject laserBeamPrefab;

    [Tooltip("二阶段攻击持续时间")]
    public float p2AttackDuration = 15.0f;

    [Tooltip("进入二阶段前的缓冲时间 (清屏后发呆多久)")]
    public float p2EnterDelay = 1.5f;

    [Tooltip("退出二阶段后的缓冲时间 (激光射完后发呆多久才变回P1)")]
    public float p2ExitDelay = 1.5f;

    [Tooltip("瞬移后停顿多久才发射 (稳定时间)")]
    public float p2StabilizeTime = 1.0f; // 建议设大一点，给玩家反应

    [Tooltip("发射完后停留多久再瞬移")]
    public float p2PostFireDelay = 0.5f;

    // 【新增】P2 激光宽度
    [Tooltip("P2 阶段激光的粗细")]
    public float p2LaserWidth = 1.5f;

    // 生成范围
    public float levelHairY = 4.5f;
    public Vector2 levelHairXRange = new Vector2(-6f, 6f);
    public float verticalHairX = -8f;
    public Vector2 verticalHairYRange = new Vector2(-3f, 3f);

    [Header("=== 运行时状态 ===")]
    [SerializeField] private string debugStateName;

    // 【关键新增】标记二阶段是否已完成
    public bool hasFinishedPhase2 = false;

    [Header("=== Phase 3: 终极狙击阶段 ===")]
    public GameObject shortLevelHairPrefab;    // shortbar_level
    public GameObject shortVerticalHairPrefab; // shortbar_vertical

    [Tooltip("P3 持续时间")]
    public float p3Duration = 20.0f;

    [Tooltip("P3 每一发激光的射击间隔 (越小越快)")]
    public float p3ShootInterval = 0.8f;

    [Tooltip("P3 瞄准时间 (激光预警时间)")]
    public float p3AimTime = 0.4f;

    [Tooltip("P3 激光伤害生效时间")]
    public float p3LaserActiveTime = 0.2f;
    // 【新增】P3 激光宽度 (通常可以细一点，因为频率高)
    [Tooltip("P3 阶段激光的粗细")]
    public float p3LaserWidth = 1.0f;

    // P3 生成位置参数 (复用 P2 的边界值，也可单独设)
    public float p3LevelY = 4.5f;   // 上下的 Y 绝对值
    public float p3VerticalX = 8.5f; // 左右的 X 绝对值

    // 【新增】标记 P3 是否已完成
    public bool hasFinishedPhase3 = false;

    // 内部变量
    public float HairCenterY => (hairTopY + hairBottomY) / 2f;
    public float HairAmplitude => (hairTopY - hairBottomY) / 2f;
    public List<Transform> leftBulletPoints = new List<Transform>();
    public List<Transform> rightBulletPoints = new List<Transform>();
    [HideInInspector] public float savedHairLeftX, savedHairLeftZ;
    [HideInInspector] public float savedHairRightX, savedHairRightZ;
    private float moveStartTime;

    // 使用 EnemyBase 的变量
    public float HpPercent => (float)currentHp / maxHp;
    // 【新增】用来存储正在运行的攻击协程引用
    private Coroutine attackCoroutineMain;
    private Coroutine attackCoroutineLeft;
    private Coroutine attackCoroutineRight;
    // 【新增】攻击开关：控制 P1 弹幕是否允许运行
    private bool isP1Attacking = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        Phase1 = new BossPhase1State(this);
        Phase2 = new BossPhase2State(this);
        Phase3 = new BossPhase3State(this);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        hasFinishedPhase2 = false; // 重置标记
        TransitionToState(Phase1);
    }

    private void Update()
    {
        currentState?.Update();
        debugStateName = currentState?.GetType().Name;

        // 全局检测：血量低于1/3 强制进 P3
        // 优先级最高，随时打断 P1 或 P2
        if (HpPercent <= 0.334f && currentState != Phase3)
        {
            TransitionToState(Phase3);
        }
    }
    void CheckGlobalTransitions()
    {
        // 优先级最高：血量 <= 1/3，且从未进过 P3
        if (!hasFinishedPhase3 && HpPercent <= 0.334f && currentState != Phase3)
        {
            TransitionToState(Phase3);
        }
    }

    public void TransitionToState(SingerBossBaseState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }

    // ================= 功能函数库 =================

    public void SetPhase1PartsActive(bool isActive)
    {
        if (battleForm) battleForm.SetActive(isActive);
        if (faceAngry) faceAngry.gameObject.SetActive(isActive);
        if (speakerLeft) speakerLeft.gameObject.SetActive(isActive);
        if (speakerRight) speakerRight.gameObject.SetActive(isActive);
        if (hairLeft) hairLeft.gameObject.SetActive(isActive);
        if (hairRight) hairRight.gameObject.SetActive(isActive);
    }

    public void FindBulletPoints()
    {
        leftBulletPoints.Clear();
        rightBulletPoints.Clear();
        for (int i = 1; i <= maxSearchIndex; i++)
        {
            string pName = "BulletPoint" + i;
            if (speakerLeft) { Transform t = FindDeepChild(speakerLeft, pName); if (t) leftBulletPoints.Add(t); }
            if (speakerRight) { Transform t = FindDeepChild(speakerRight, pName); if (t) rightBulletPoints.Add(t); }
        }
    }

    public void ResetHairMovementTime() { moveStartTime = Time.time; }

    public void HandlePhase1HairMovement()
    {
        float t = Time.time - moveStartTime;
        float offset = Mathf.Sin(t * hairMoveSpeed) * HairAmplitude;
        if (hairLeft) hairLeft.position = new Vector3(savedHairLeftX, HairCenterY + offset, savedHairLeftZ);
        if (hairRight) hairRight.position = new Vector3(savedHairRightX, HairCenterY - offset, savedHairRightZ);
    }

    // 1. 发射入口
    public IEnumerator FirePhase1Barrage()
    {
        // 打开开关
        isP1Attacking = true;

        // 开启左右两个独立的协程
        // 注意：这里不需要再记录 Coroutine 变量了，我们用 bool 控制
        Coroutine leftRoutine = StartCoroutine(FireSequenceRoutine(leftBulletPoints, new Vector3(0.5f, -1f, 0)));
        Coroutine rightRoutine = StartCoroutine(FireSequenceRoutine(rightBulletPoints, new Vector3(-0.5f, -1f, 0)));

        // 等待它们自然执行完 (或者被 bool 强行打断)
        yield return leftRoutine;
        yield return rightRoutine;
    }


    // 2. 紧急停止 (在 BossPhase1State.Exit 调用)
    public void StopPhase1Attack()
    {
        // 【核心修复】关掉开关！
        // 正在运行的 FireSequenceRoutine 检测到这个变量变 false 后，会在下一发子弹前自动自杀
        isP1Attacking = false;

        // 双重保险：停止所有协程 (虽然有点暴力，但对 BossSinger 来说通常没问题)
        // 如果你有其他不想停止的协程(比如移动)，就只依赖上面的 bool
        // StopAllCoroutines(); 

        Debug.Log("P1 弹幕已通过开关强制切断");
    }
    // 3. 具体的发射逻辑 (带检测)
    IEnumerator FireSequenceRoutine(List<Transform> points, Vector3 baseDirection)
    {
        if (redBulletPrefab == null) yield break;

        for (int r = 0; r < roundsPerAttack; r++)
        {
            foreach (var point in points)
            {
                // ========================================================
                // 【核心修复】每次发射前，先检查开关！
                // 如果开关关了，或者 BattleForm 被隐藏了，立刻停止协程
                // ========================================================
                if (!isP1Attacking || !battleForm.activeInHierarchy)
                {
                    yield break; // 彻底退出协程
                }

                if (point == null) continue;

                // 生成子弹
                GameObject bullet = Instantiate(redBulletPrefab, point.position, Quaternion.identity);
                // ObjectPoolManager.Instance.Get(...)

                Vector3 targetDir = baseDirection.normalized;
                float angle = Random.Range(-showerSpreadAngle, showerSpreadAngle);
                Vector3 finalDir = Quaternion.Euler(0, 0, angle) * targetDir;
                float speed = Random.Range(bulletSpeedRange.x, bulletSpeedRange.y);

                var ep = bullet.GetComponent<EnemyProjectile>();
                if (ep != null) { ep.speed = speed; ep.Initialize(finalDir); }

                yield return new WaitForSeconds(pointStaggerDelay);
            }
        }
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform res = FindDeepChild(child, name);
            if (res != null) return res;
        }
        return null;
    }
    public void ClearAllBullets()
    {
        // 查找所有挂载了 EnemyProjectile 的物体
        EnemyProjectile[] bullets = FindObjectsOfType<EnemyProjectile>();
        foreach (var bullet in bullets)
        {
            // 播放一个消失特效(可选)
            // Instantiate(despawnVFX, bullet.transform.position, Quaternion.identity);

            // 销毁或回收
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Return(bullet.gameObject);
            else
                Destroy(bullet.gameObject);
        }
        Debug.Log($"<color=yellow>Boss P2 进场：清理了 {bullets.Length} 个弹幕</color>");
    }

    // 【新增】处理脸部上下悬浮
    public void HandleFaceHover()
    {
        if (faceAngry == null) return;

        // 计算时间流逝 (基于 moveStartTime，保证动画连贯)
        float timeElapsed = Time.time - moveStartTime;

        // 计算 Y 轴偏移量
        float yOffset = Mathf.Sin(timeElapsed * faceHoverSpeed) * faceHoverAmplitude;

        // 应用位置：基准位置 (faceTargetPos) + 偏移量
        // 注意：我们只改变 Y 轴，保持 X 和 Z 轴在基准位置
        faceAngry.position = new Vector3(
            faceTargetPos.x,
            faceTargetPos.y + yOffset,
            faceTargetPos.z
        );
    }
    protected override void MoveBehavior() { }
}
