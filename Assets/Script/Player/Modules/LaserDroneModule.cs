using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserDroneModule : PlayerModule
{
    [Header("Drone Refs")]
    public List<Transform> droneTransforms;
    public GameObject laserPrefab;

    [Header("Movement Settings")]
    public float followSmoothTime = 0.6f;//滞后时间可以给大一点，现在完全独立了效果很明显
    public float rotateSpeed = 15f;

    [Header("Idle Ring Wander")]
    public float minIdleRadius;//内圈半径 (绝对不会进这个圈，避免重叠)
    public float maxIdleRadius;//外圈半径
    public float wanderSpeed = 0.5f;
    public float maxDistanceBeforeFollow = 8.0f;//掉队距离

    [Header("Attack Behavior")]
    public float attackForwardDist = 3.5f;
    public float attackSpreadRadius = 1.5f;

    [Header("Combat Stats")]
    public int droneCount = 1;
    public int damagePerTick = 1;
    public float damageInterval = 0.2f;
    public float laserRange = 15f;
    public float chargeTime = 0.6f;
    public float maxFireDuration = 5.0f;
    public float laserCD = 5f;

    public LayerMask enemyLayer;
    public LayerMask wallLayer;

    private enum DroneState { Idle, Charging, Firing, CD }
    private DroneState currentState = DroneState.Idle;

    private Vector3[] currentVelocities;
    private List<LineRenderer> activeLasers = new List<LineRenderer>();

    private float stateTimer = 0f;
    private float damageTimer = 0f;

    private float[] noiseOffsets; // 只需要一个偏移值来控制角度
    private float[] radiusOffsets; // 控制半径的偏移
    private Vector3[] attackRandomOffsets;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        int count = droneTransforms.Count;
        currentVelocities = new Vector3[count];
        noiseOffsets = new float[count];
        radiusOffsets = new float[count];
        attackRandomOffsets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform drone = droneTransforms[i];

            drone.SetParent(null);

            // 初始化激光
            GameObject laserObj = Instantiate(laserPrefab, drone);
            laserObj.transform.localPosition = Vector3.zero;
            LineRenderer lr = laserObj.GetComponent<LineRenderer>();
            lr.enabled = false;
            activeLasers.Add(lr);

            // 初始化随机数
            noiseOffsets[i] = Random.Range(0f, 1000f);
            radiusOffsets[i] = Random.Range(5000f, 6000f);
        }

        for(int i = 0; i < droneCount; i++)
        {
            droneTransforms[i].gameObject.SetActive(true);
            activeLasers[i].gameObject.SetActive(true);
        }

        for(int i = droneCount; i < count; i++)
        {
            droneTransforms[i].gameObject.SetActive(false);
            activeLasers[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach (var drone in droneTransforms)
        {
            if (drone != null) Destroy(drone.gameObject);
        }
    }

    public override void OnModuleUpdate()
    {
        // 如果玩家甚至被销毁了，模块停止运行
        if (player == null) return;

        HandleStateInput();
        HandleDroneMovement();

        if (currentState == DroneState.Firing)
        {
            HandleLaserFiring();
        }
        else
        {
            foreach (var lr in activeLasers) lr.enabled = false;
        }
    }

    void HandleStateInput()
    {
        if (player.IsStunned || player.IsDead)
        {
            currentState = DroneState.Idle;
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            if (currentState == DroneState.Idle)
            {
                currentState = DroneState.Charging;
                stateTimer = 0f;
                RandomizeAttackPositions();
            }
        }
        else
        {
            if (currentState == DroneState.Charging)
            {
                currentState = DroneState.Idle;
            }
            else if (currentState == DroneState.Firing)
            {
                currentState = DroneState.CD;
            }
        }

        if (currentState == DroneState.Charging)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= chargeTime)
            {
                currentState = DroneState.Firing;
                stateTimer = 0f;
            }
        }
        else if (currentState == DroneState.Firing)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= maxFireDuration)
            {
                currentState = DroneState.CD;
                stateTimer = 0f;
            }
        }
        else if (currentState == DroneState.CD)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= laserCD)
            {
                currentState = DroneState.Idle;
            }
        }
    }

    void RandomizeAttackPositions()
    {
        for (int i = 0; i < droneTransforms.Count; i++)
        {
            attackRandomOffsets[i] = (Vector3)Random.insideUnitCircle * attackSpreadRadius;
        }
    }

    void HandleDroneMovement()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 playerToMouseDir = (mousePos - player.transform.position).normalized;

        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            Vector3 targetPos = Vector3.zero;

            if (currentState == DroneState.Idle || currentState == DroneState.CD)
            {
                float distToPlayer = Vector3.Distance(drone.position, player.transform.position);

                //强制归位
                if (distToPlayer > maxDistanceBeforeFollow)
                {
                    Vector3 followTarget = player.transform.position;
                    //飞到玩家身后一点
                    if (player.Rigid2d.velocity.magnitude > 0.1f)
                        followTarget -= (Vector3)player.Rigid2d.velocity.normalized * minIdleRadius;

                    targetPos = followTarget;
                    RotateDrone(drone, player.transform.position - drone.position);
                }
                else
                {
                    //基于极坐标的圆环随机
                    //计算随机角度 使用柏林让角度平滑变化 (0 ~ 1) -> (0 ~ 360度)
                    // * 4f 让它转圈稍微快一点，也就是公转
                    float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[i]);
                    float targetAngleRad = noiseAngle * Mathf.PI * 4f;

                    //计算随机半径 (Radius)
                    //半径在 minIdleRadius 和 maxIdleRadius 之间浮动
                    float noiseRad = Mathf.PerlinNoise(Time.time * wanderSpeed, radiusOffsets[i]);
                    float currentRadius = Mathf.Lerp(minIdleRadius, maxIdleRadius, noiseRad);

                    //转换为坐标偏移
                    float offsetX = Mathf.Cos(targetAngleRad) * currentRadius;
                    float offsetY = Mathf.Sin(targetAngleRad) * currentRadius;
                    Vector3 ringOffset = new Vector3(offsetX, offsetY, 0);

                    targetPos = player.transform.position + ringOffset;

                    //闲置时看向鼠标
                    RotateDrone(drone, mousePos - drone.position);
                }
            }
            else
            {
                Vector3 baseAttackPos = player.transform.position + (playerToMouseDir * attackForwardDist);
                targetPos = baseAttackPos + attackRandomOffsets[i];
                RotateDrone(drone, mousePos - drone.position);
            }
            drone.position = Vector3.SmoothDamp(drone.position, targetPos, ref currentVelocities[i], followSmoothTime);
        }
    }

    void RotateDrone(Transform drone, Vector3 lookDir)
    {
        if (lookDir == Vector3.zero) return;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
        drone.rotation = Quaternion.Slerp(drone.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    void HandleLaserFiring()
    {
        damageTimer += Time.deltaTime;
        bool shouldDamage = damageTimer >= damageInterval;
        Vector3 mousePos = MUtils.GetMouseWorldPosition();

        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            LineRenderer lr = activeLasers[i];
            lr.enabled = true;
            lr.SetPosition(0, drone.position);
            Vector2 fireDir = (mousePos - drone.position).normalized;
            float actualDist = laserRange;
            RaycastHit2D wallHit = Physics2D.Raycast(drone.position, fireDir, laserRange, wallLayer);
            if (wallHit.collider != null)
            {
                actualDist = wallHit.distance;
                lr.SetPosition(1, wallHit.point);
            }
            else
            {
                lr.SetPosition(1, drone.position + (Vector3)(fireDir * laserRange));
            }
            if (shouldDamage)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(drone.position, fireDir, actualDist, enemyLayer);
                foreach (var hit in hits)
                {
                    if (hit.distance <= actualDist)
                    {
                        var damageable = hit.collider.GetComponent<IDamageable>();
                        if (damageable != null) damageable.TakeDamage(damagePerTick);
                    }
                }
            }
        }
        if (shouldDamage) damageTimer = 0f;
    }

    public override void OnActivate()
    {
        base.OnActivate();
        transform.gameObject.SetActive(true);
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        //OnDestroy();s
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        base.UpgradeModule(moduleType, statType);
        laserRange += 5f;
    }
}
