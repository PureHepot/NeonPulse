using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SlimePhysicsRuntime))]
public class BossSlimeController : EnemyBase
{
    // --- 核心引用 ---
    public SlimePhysicsRuntime PhysicsRuntime { get; private set; }
    public Rigidbody2D CenterRB => PhysicsRuntime.centerRB;

    // --- 状态机 ---
    private BossBaseState currentState;

    // 预加载状态实例
    public SlimeSpawnState SpawnState = new SlimeSpawnState();
    public SlimeFallState FallState = new SlimeFallState();
    public SlimeRoamState RoamState = new SlimeRoamState();
    public SlimeChargeState ChargeState = new SlimeChargeState();

    // --- 参数配置 (供状态读取) ---
    [Header("AI Settings")]
    public int jumpsBeforeSwitch = 3;  // 跳几次后换墙
    public float jumpInterval = 2f;    // 游走多久跳一次

    [Header("Jump Settings")]
    public float jumpChargeTime = 0.8f;
    public float jumpForce = 40f;
    public float squishForce = 120f;   // 蓄力压扁力度

    [Header("Detection")]
    public LayerMask terrainLayer;
    public float groundCheckDist = 3.5f;

    [Header("Debugging")]
    public bool showDebugGizmos = true;
    public Color predictColor = Color.cyan;

    // --- 运行时数据 ---
    [HideInInspector] public Vector2 SurfaceNormal = Vector2.up;
    [HideInInspector] public Vector2 TargetJumpDirection; // 下一次跳跃的方向
    [HideInInspector] public Vector2 PredictedLandingPoint; // 预测落点坐标
    [HideInInspector] public bool HasTarget = false; // 是否已经选好了目标

    void Start()
    {
        PhysicsRuntime = GetComponent<SlimePhysicsRuntime>();
        PhysicsRuntime.Initialize();

        // 启动状态机
        TransitionToState(SpawnState);
    }

    void Update()
    {
        currentState?.OnUpdate();
    }

    protected override void MoveBehavior()
    {
        currentState?.OnFixedUpdate();
    }

    public void TransitionToState(BossBaseState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        Debug.Log($"切换状态至{newState.ToString()}");
        currentState.Enter(this);
    }

    // 辅助：检测是否接触地面/墙壁
    public bool CheckGround(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, groundCheckDist, terrainLayer);
        if (hit.collider != null)
        {
            SurfaceNormal = hit.normal;
            return true;
        }
        return false;
    }

    public void PickNewTargetSurface()
    {
        // 1. 定义四个基本方向 (上、下、左、右)
        Vector2[] allDirs = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        // 2. 排除当前所在的面的法线方向 (比如在地面(0,1)，就不往上跳，那是普通跳，我们要冲撞)
        // 注意：我们其实是要"去"的方向。
        // 如果在地面(Normal=0,1)，我们不能去的方向其实是 (0,-1) 因为那是地底。
        // 但为了简单，我们逻辑是：从剩下3个方向里选一个作为"重力/落点"方向。

        List<Vector2> validDirs = new List<Vector2>();
        foreach (var dir in allDirs)
        {
            // 如果这个方向和当前表面法线点积 < 0.9f (不是同一个方向)，就可以去
            // 其实最简单的是：只要不是当前Normal (0,1)，那就是其他三个面对应的方向
            // 地面Normal是(0,1)。 
            // 左墙(-1,0), 右墙(1,0), 天花板(0,-1)。
            // 我们要去的方向是墙壁的"位置"。

            // 简单算法：排除当前法线方向
            if (Vector2.Distance(dir, SurfaceNormal) > 0.1f)
            {
                validDirs.Add(dir);
            }
        }

        // 3. 随机选一个
        if (validDirs.Count > 0)
        {
            Vector2 targetDir = validDirs[Random.Range(0, validDirs.Count)];

            // 4. 计算落点 (射线检测)
            // 从中心向该方向发射射线，找到落点
            RaycastHit2D hit = Physics2D.Raycast(transform.position, targetDir, 50f, terrainLayer);
            if (hit.collider != null)
            {
                TargetJumpDirection = targetDir; // 这将是我们跳跃冲刺的方向
                PredictedLandingPoint = hit.point;
                HasTarget = true;
            }
            else
            {
                // 如果没打到墙（可能是场景没封口），就默认飞远点
                TargetJumpDirection = targetDir;
                PredictedLandingPoint = (Vector2)transform.position + targetDir * 10f;
                HasTarget = true;
            }
        }
    }


    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 画出当前重力方向
        if (PhysicsRuntime)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, PhysicsRuntime.gravityDirection * 2f);
        }

        // 画出预测落点
        if (HasTarget)
        {
            Gizmos.color = predictColor;
            // 画虚线轨迹
            Gizmos.DrawLine(transform.position, PredictedLandingPoint);
            // 画落点球
            Gizmos.DrawWireSphere(PredictedLandingPoint, 1f);
            // 画落点法线模拟
            Gizmos.DrawRay(PredictedLandingPoint, -TargetJumpDirection * 1f);
        }
    }

    
}