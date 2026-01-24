using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserDroneModule : PlayerModule
{
    [Header("Drone Refs")]
    public List<Transform> droneTransforms;

    // 【修改点】不再只用一个 laserPrefab，而是把特效预制体都引进来
    [Header("Visual Effects")]
    public GameObject laserLinePrefab;   // 激光线 (带 Shader Graph 的 LineRenderer)
    public GameObject muzzleVFXPrefab;   // 发射口特效
    public GameObject hitVFXPrefab;      // 击中特效

    [Header("Movement Settings")]
    public float followSmoothTime = 0.6f;
    public float rotateSpeed = 15f;

    [Header("Idle Ring Wander")]
    public float minIdleRadius;
    public float maxIdleRadius;
    public float wanderSpeed = 0.5f;
    public float maxDistanceBeforeFollow = 8.0f;

    [Header("Attack Behavior")]
    public float attackForwardDist = 3.5f;
    public float attackSpreadRadius = 1.5f;

    [Header("Combat Stats")]
    public int droneCount = 0;
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

    // 【修改点】使用一个内部类或结构体来管理单台无人机的所有视觉组件
    private class DroneVisuals
    {
        public LineRenderer line;
        public GameObject muzzleObj;
        public GameObject hitObj;
        public ParticleSystem[] muzzleParticles;
        public ParticleSystem[] hitParticles;

        // 控制粒子开关
        public void SetVFXActive(bool active, ParticleSystem[] particles, GameObject obj)
        {
            if (obj == null) return;
            if (active)
            {
                // 如果需要显示，且对象还没激活，或者粒子没发射
                // 这里我们简单处理：让Emission开启
                foreach (var p in particles)
                {
                    var em = p.emission;
                    em.enabled = true;
                    if (!p.isPlaying) p.Play();
                }
            }
            else
            {
                foreach (var p in particles)
                {
                    var em = p.emission;
                    em.enabled = false;
                }
            }
        }
    }

    private List<DroneVisuals> droneVisualsList = new List<DroneVisuals>();

    private float stateTimer = 0f;
    private float damageTimer = 0f;

    private float[] noiseOffsets;
    private float[] radiusOffsets;
    private Vector3[] attackRandomOffsets;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        int count = droneTransforms.Count;
        currentVelocities = new Vector3[count];
        noiseOffsets = new float[count];
        radiusOffsets = new float[count];
        attackRandomOffsets = new Vector3[count];
        droneVisualsList.Clear();

        for (int i = 0; i < count; i++)
        {
            Transform drone = droneTransforms[i];
            drone.SetParent(null);

            // --- 初始化视觉组件 ---
            DroneVisuals visuals = new DroneVisuals();

            // 激光线
            if (laserLinePrefab)
            {
                GameObject lObj = Instantiate(laserLinePrefab, Vector3.zero, Quaternion.identity);
                lObj.transform.position = Vector3.zero;
                visuals.line = lObj.GetComponent<LineRenderer>();
                visuals.line.enabled = false;
            }

            // 发射口特效 跟随无人机
            if (muzzleVFXPrefab)
            {
                GameObject mObj = Instantiate(muzzleVFXPrefab, drone.position, drone.rotation);
                mObj.transform.SetParent(drone);
                mObj.transform.localPosition = Vector3.right * 0.5f;

                visuals.muzzleObj = mObj;
                visuals.muzzleParticles = mObj.GetComponentsInChildren<ParticleSystem>();
                visuals.SetVFXActive(false, visuals.muzzleParticles, visuals.muzzleObj);
            }

            // 击中特效
            if (hitVFXPrefab)
            {
                GameObject hObj = Instantiate(hitVFXPrefab, Vector3.zero, Quaternion.identity);
                visuals.hitObj = hObj;
                visuals.hitParticles = hObj.GetComponentsInChildren<ParticleSystem>();
                visuals.SetVFXActive(false, visuals.hitParticles, visuals.hitObj);
            }

            droneVisualsList.Add(visuals);

            noiseOffsets[i] = Random.Range(0f, 1000f);
            radiusOffsets[i] = Random.Range(5000f, 6000f);
        }

        // 初始激活状态
        UpdateActiveDrones();
    }


    void UpdateActiveDrones()
    {
        for (int i = 0; i < droneTransforms.Count; i++)
        {
            bool isActive = i < droneCount;
            droneTransforms[i].gameObject.SetActive(isActive);
        }
    }

    private void OnDestroy()
    {
        foreach (var drone in droneTransforms) if (drone != null) Destroy(drone.gameObject);

        // 清理特效实例
        foreach (var v in droneVisualsList)
        {
            if (v.line) Destroy(v.line.gameObject);
            if (v.hitObj) Destroy(v.hitObj);
        }
    }

    public override void OnModuleUpdate()
    {
        if (player == null) return;

        HandleStateInput();
        HandleDroneMovement();

        // 状态分发
        if (currentState == DroneState.Firing)
        {
            HandleLaserFiring();
        }
        else
        {
            // 非开火状态，关闭所有特效
            DisableAllLasers();
        }
    }

    void DisableAllLasers()
    {
        for (int i = 0; i < droneCount; i++)
        {
            var v = droneVisualsList[i];
            if (v.line) v.line.enabled = false;
            v.SetVFXActive(false, v.muzzleParticles, v.muzzleObj);
            v.SetVFXActive(false, v.hitParticles, v.hitObj);
        }
    }

    void HandleStateInput()
    {
        if (player.IsStunned || player.IsDead)
        {
            currentState = DroneState.Idle;
            return;
        }

        if (InputManager.Instance.Run() && InputManager.Instance.Mouse0())
        {
            if (currentState == DroneState.Idle)
            {
                currentState = DroneState.Charging;
                stateTimer = 0f;
                //随即移动
                RandomizeAttackPositions();
            }
        }
        else
        {
            if (currentState == DroneState.Charging) currentState = DroneState.Idle;
            else if (currentState == DroneState.Firing) currentState = DroneState.CD;
        }

        // 状态计时器逻辑
        if (currentState == DroneState.Charging)
        {
            stateTimer += Time.deltaTime;
            // 可选：在充电阶段播放 Muzzle 的蓄力光效 (如果是单独的粒子)
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
            if (stateTimer >= laserCD) currentState = DroneState.Idle;
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
        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        Vector3 playerToMouseDir = (mousePos - player.transform.position).normalized;

        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            Vector3 targetPos = Vector3.zero;
            Vector3 lookTarget = mousePos; // 默认看向鼠标

            if (currentState == DroneState.Idle || currentState == DroneState.CD)
            {
                float distToPlayer = Vector3.Distance(drone.position, player.transform.position);

                if (distToPlayer > maxDistanceBeforeFollow)
                {
                    // 强制归位
                    Vector3 followTarget = player.transform.position;
                    if (player.Rigid2d.velocity.magnitude > 0.1f)
                        followTarget -= (Vector3)player.Rigid2d.velocity.normalized * minIdleRadius;

                    targetPos = followTarget;
                    lookTarget = player.transform.position; // 归位时看玩家或前方
                }
                else
                {
                    // 闲置游荡
                    float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[i]);
                    float targetAngleRad = noiseAngle * Mathf.PI * 4f;

                    float noiseRad = Mathf.PerlinNoise(Time.time * wanderSpeed, radiusOffsets[i]);
                    float currentRadius = Mathf.Lerp(minIdleRadius, maxIdleRadius, noiseRad);

                    float offsetX = Mathf.Cos(targetAngleRad) * currentRadius;
                    float offsetY = Mathf.Sin(targetAngleRad) * currentRadius;
                    Vector3 ringOffset = new Vector3(offsetX, offsetY, 0);

                    targetPos = player.transform.position + ringOffset;
                    // lookTarget 保持为 mousePos
                }
            }
            else
            {
                // 攻击站位
                Vector3 baseAttackPos = player.transform.position + (playerToMouseDir * attackForwardDist);
                targetPos = baseAttackPos + attackRandomOffsets[i];
            }

            // 移动
            drone.position = Vector3.SmoothDamp(drone.position, targetPos, ref currentVelocities[i], followSmoothTime);

            // 旋转
            RotateDrone(drone, lookTarget - drone.position);
        }
    }

    void RotateDrone(Transform drone, Vector3 lookDir)
    {
        if (lookDir == Vector3.zero) return;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
        drone.rotation = Quaternion.Slerp(drone.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    // 特效控制
    void HandleLaserFiring()
    {
        damageTimer += Time.deltaTime;
        bool shouldDamage = damageTimer >= damageInterval;
        Vector3 mousePos = MUtils.GetMouseWorldPosition();

        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            DroneVisuals v = droneVisualsList[i];

            v.SetVFXActive(true, v.muzzleParticles, v.muzzleObj);

            v.line.enabled = true;
            // 设置起点
            Vector3 startPos = drone.position;
            if (v.muzzleObj) startPos = v.muzzleObj.transform.position;

            v.line.SetPosition(0, startPos);

            Vector2 fireDir = (mousePos - drone.position).normalized;
            float actualDist = laserRange;
            Vector3 endPos;
            Vector3 hitNormal = -fireDir; // 默认反向

            // 3. 物理检测 (墙壁)
            RaycastHit2D wallHit = Physics2D.Raycast(startPos, fireDir, laserRange, wallLayer);
            if (wallHit.collider != null)
            {
                actualDist = wallHit.distance;
                endPos = wallHit.point;
                hitNormal = wallHit.normal;

                // --- 激活击中特效 ---
                v.SetVFXActive(true, v.hitParticles, v.hitObj);
                v.hitObj.transform.position = endPos;
                // 2D 中法线旋转处理: Z 轴朝外 (LookRotation 可能会让 Z 轴朝向法线，需要根据粒子形状调整)
                // 这里假设粒子是 Stretched Billboard，需要它在 XY 平面散射
                // 简单的做法是让 up 指向法线
                v.hitObj.transform.up = hitNormal;
            }
            else
            {
                // 没打中墙，射向最大距离
                endPos = startPos + (Vector3)(fireDir * laserRange);

                // --- 没打中东西，关闭击中特效 ---
                v.SetVFXActive(false, v.hitParticles, v.hitObj);
            }

            v.line.SetPosition(1, endPos);

            // 更新材质 Tiling (防止拉伸)
            // float lineLength = Vector3.Distance(startPos, endPos);
            // v.line.material.SetFloat("_Tiling", lineLength / 2f); // 如果 Shader 支持

            // 伤害判定
            if (shouldDamage)
            {
   
                RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, fireDir, actualDist, enemyLayer);
                foreach (var hit in hits)
                {
                    // 确保是在墙壁遮挡之前打中的
                    if (hit.distance <= actualDist)
                    {
                        var damageable = hit.collider.GetComponent<IDamageable>();
                        // 传递击中点和法线，触发敌人的受击特效
                        // 注意：这里的法线是射线反方向
                        if (damageable != null)
                            damageable.TakeDamage(damagePerTick, hit.point, -fireDir);
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
        droneCount = 1;
        UpdateActiveDrones();
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        base.UpgradeModule(moduleType, statType);
        // 如果升级了数量
        if (moduleType == ModuleType.LaserDrone && statType == StatType.BeamCount)
        {
            droneCount++;
            if (droneCount > droneTransforms.Count) droneCount = droneTransforms.Count;
            UpdateActiveDrones();
        }
        else if (statType == StatType.BeamRange) // 假设你有 Range 类型
        {
            laserRange += 5f;
        }
    }
}