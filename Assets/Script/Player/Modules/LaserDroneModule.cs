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

    [Header("Auto Attack Settings")]
    public float detectionRadius = 10f; // 索敌半径
    public float laserWidth = 0.5f;

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
    private Transform[] currentTargets;

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
    //private Vector3[] attackRandomOffsets;

    private GameObject _debrisHolder;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (_debrisHolder != null) Destroy(_debrisHolder);
        _debrisHolder = new GameObject($"[{player.name}]_DroneHolder");

        int count = droneTransforms.Count;
        currentVelocities = new Vector3[count];
        currentTargets = new Transform[count];
        noiseOffsets = new float[count];
        radiusOffsets = new float[count];
        droneVisualsList.Clear();

        for (int i = 0; i < count; i++)
        {
            Transform drone = droneTransforms[i];
            drone.SetParent(_debrisHolder.transform);

            // --- 初始化视觉组件 ---
            DroneVisuals visuals = new DroneVisuals();

            // 激光线
            if (laserLinePrefab)
            {
                GameObject lObj = Instantiate(laserLinePrefab, Vector3.zero, Quaternion.identity);
                lObj.transform.position = Vector3.zero;
                visuals.line = lObj.GetComponent<LineRenderer>();
                if (visuals.line)
                {
                    visuals.line.transform.SetParent(_debrisHolder.transform);
                    visuals.line.startWidth = laserWidth*2;
                    visuals.line.endWidth = laserWidth*2;
                    visuals.line.enabled = false;
                }
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
                if (visuals.hitObj) visuals.hitObj.transform.SetParent(_debrisHolder.transform);
                visuals.hitParticles = hObj.GetComponentsInChildren<ParticleSystem>();
                visuals.SetVFXActive(false, visuals.hitParticles, visuals.hitObj);
            }

            droneVisualsList.Add(visuals);

            noiseOffsets[i] = Random.Range(0f, 1000f);
            radiusOffsets[i] = Random.Range(5000f, 6000f);
        }

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
        for (int i = 0; i < droneTransforms.Count; i++)
        {
            droneTransforms[i].gameObject.SetActive(false);
        }

        if (_debrisHolder != null)
        {
            Destroy(_debrisHolder);
        }
    }

    public override void OnModuleUpdate()
    {
        if (player == null) return;

        //HandleStateInput();
        HandleAutoState();
        HandleDroneMovement();

        // 状态分发
        if (currentState == DroneState.Firing)
        {
            HandleLaserFiring();
        }
        else
        {
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



    void HandleAutoState()
    {
        if (player.IsDead)
        {
            currentState = DroneState.Idle;
            return;
        }

        switch (currentState)
        {
            case DroneState.Idle:
                // 闲置时，尝试索敌
                if (TryFindTargets())
                {
                    currentState = DroneState.Charging;
                    stateTimer = 0f;
                }
                break;

            case DroneState.Charging:
                stateTimer += Time.deltaTime;
                UpdateTargets();
                if (!HasAnyTarget())
                {
                    currentState = DroneState.Idle;
                    stateTimer = 0f;
                    return;
                }
                if (stateTimer >= chargeTime)
                {
                    currentState = DroneState.Firing;
                    stateTimer = 0f;
                    damageTimer = damageInterval; // 确保第一次伤害立即触发
                }
                break;

            case DroneState.Firing:
                stateTimer += Time.deltaTime;
                UpdateTargets();
                if (!HasAnyTarget())
                {
                    currentState = DroneState.CD; // 或者直接 Idle，看你想不想触发CD
                    stateTimer = 0f;
                    return;
                }
                if (stateTimer >= maxFireDuration)
                {
                    currentState = DroneState.CD;
                    stateTimer = 0f;
                }
                break;

            case DroneState.CD:
                stateTimer += Time.deltaTime;
                if (stateTimer >= laserCD)
                {
                    currentState = DroneState.Idle;
                }
                break;
        }
    }
    bool HasAnyTarget()
    {
        for (int i = 0; i < droneCount; i++)
        {
            if (currentTargets[i] != null && currentTargets[i].gameObject.activeInHierarchy) return true;
        }

        return TryFindTargets();
    }

    bool TryFindTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, detectionRadius, enemyLayer);
        if (hits.Length == 0) return false;

        // 收集并去重
        List<Transform> enemies = new List<Transform>();
        foreach (var hit in hits)
        {
            if (hit != null && hit.transform != null && !enemies.Contains(hit.transform))
            {
                enemies.Add(hit.transform);
            }
        }

        if (enemies.Count == 0) return false;

        // 威胁度排序：优先把离玩家最近的敌人排在前面
        Vector3 playerPos = player.transform.position;
        enemies.Sort((a, b) =>
        {
            float d1 = (a.position - playerPos).sqrMagnitude;
            float d2 = (b.position - playerPos).sqrMagnitude;
            return d1.CompareTo(d2);
        });

        //取模来分配目标
        for (int i = 0; i < droneCount; i++)
        {
            currentTargets[i] = enemies[i % enemies.Count];
        }

        return true;
    }

    void UpdateTargets()
    {
        for (int i = 0; i < droneCount; i++)
        {
            // 如果目标不存在或已失活
            if (currentTargets[i] == null || !currentTargets[i].gameObject.activeInHierarchy)
            {
                // 尝试重新找一个最近的
                currentTargets[i] = GetNearestEnemy(droneTransforms[i].position);
            }
        }
    }

    Transform GetNearestEnemy(Vector3 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, detectionRadius, enemyLayer);
        return hit ? hit.transform : null;
    }

    void HandleDroneMovement()
    {
        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            Vector3 targetPos;
            Vector3 lookTarget; 

            // 如果有目标且处于战斗状态则看向目标
            if (currentState == DroneState.Charging || currentState == DroneState.Firing)
            {
                // 攻击时，移动到目标附近的一个点，或者稍微散开
                // 这里为了简单，保持环绕玩家，但朝向敌人

                if (currentTargets[i] != null)
                {
                    lookTarget = currentTargets[i].position;
                }
                else
                {
                    lookTarget = drone.position + drone.right;
                }

                // 计算环绕位置
                float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[i]);
                float targetAngleRad = noiseAngle * Mathf.PI * 4f;
                Vector3 ringOffset = new Vector3(Mathf.Cos(targetAngleRad), Mathf.Sin(targetAngleRad), 0) * minIdleRadius;
                
                targetPos = player.transform.position + ringOffset;
            }
            else
            {
                // 闲置或CD，环绕玩家，看向前方或移动方向
                float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[i]);
                float targetAngleRad = noiseAngle * Mathf.PI * 4f;
                float noiseRad = Mathf.PerlinNoise(Time.time * wanderSpeed, radiusOffsets[i]);
                float currentRadius = Mathf.Lerp(minIdleRadius, maxIdleRadius, noiseRad);

                Vector3 ringOffset = new Vector3(Mathf.Cos(targetAngleRad), Mathf.Sin(targetAngleRad), 0) * currentRadius;
                targetPos = player.transform.position + ringOffset;
                lookTarget = targetPos;
                
                // 远距离归位
                if (Vector3.Distance(drone.position, player.transform.position) > maxDistanceBeforeFollow)
                {
                    targetPos = player.transform.position;
                }
            }

            drone.position = Vector3.SmoothDamp(drone.position, targetPos, ref currentVelocities[i], followSmoothTime);
            Vector3 dirToLook = lookTarget - drone.position;
            RotateDrone(drone, dirToLook);
        }
    }


    #region 弃用
    //void HandleStateInput()
    //{
    //    if (player.IsStunned || player.IsDead)
    //    {
    //        currentState = DroneState.Idle;
    //        return;
    //    }

    //    if (InputManager.Instance.Run() && InputManager.Instance.Mouse0())
    //    {
    //        if (currentState == DroneState.Idle)
    //        {
    //            currentState = DroneState.Charging;
    //            stateTimer = 0f;
    //            //随即移动
    //            RandomizeAttackPositions();
    //        }
    //    }
    //    else
    //    {
    //        if (currentState == DroneState.Charging) currentState = DroneState.Idle;
    //        else if (currentState == DroneState.Firing) currentState = DroneState.CD;
    //    }

    //    // 状态计时器逻辑
    //    if (currentState == DroneState.Charging)
    //    {
    //        stateTimer += Time.deltaTime;
    //        // 可选：在充电阶段播放 Muzzle 的蓄力光效 (如果是单独的粒子)
    //        if (stateTimer >= chargeTime)
    //        {
    //            currentState = DroneState.Firing;
    //            stateTimer = 0f;
    //        }
    //    }
    //    else if (currentState == DroneState.Firing)
    //    {
    //        stateTimer += Time.deltaTime;
    //        if (stateTimer >= maxFireDuration)
    //        {
    //            currentState = DroneState.CD;
    //            stateTimer = 0f;
    //        }
    //    }
    //    else if (currentState == DroneState.CD)
    //    {
    //        stateTimer += Time.deltaTime;
    //        if (stateTimer >= laserCD) currentState = DroneState.Idle;
    //    }
    //}

    //void RandomizeAttackPositions()
    //{
    //    for (int i = 0; i < droneTransforms.Count; i++)
    //    {
    //        attackRandomOffsets[i] = (Vector3)Random.insideUnitCircle * attackSpreadRadius;
    //    }
    //}

    //void HandleDroneMovement()
    //{
    //    Vector3 mousePos = MUtils.GetMouseWorldPosition();
    //    Vector3 playerToMouseDir = (mousePos - player.transform.position).normalized;

    //    for (int i = 0; i < droneCount; i++)
    //    {
    //        Transform drone = droneTransforms[i];
    //        Vector3 targetPos = Vector3.zero;
    //        Vector3 lookTarget = mousePos; // 默认看向鼠标

    //        if (currentState == DroneState.Idle || currentState == DroneState.CD)
    //        {
    //            float distToPlayer = Vector3.Distance(drone.position, player.transform.position);

    //            if (distToPlayer > maxDistanceBeforeFollow)
    //            {
    //                // 强制归位
    //                Vector3 followTarget = player.transform.position;
    //                if (player.Rigid2d.velocity.magnitude > 0.1f)
    //                    followTarget -= (Vector3)player.Rigid2d.velocity.normalized * minIdleRadius;

    //                targetPos = followTarget;
    //                lookTarget = player.transform.position; // 归位时看玩家或前方
    //            }
    //            else
    //            {
    //                // 闲置游荡
    //                float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[i]);
    //                float targetAngleRad = noiseAngle * Mathf.PI * 4f;

    //                float noiseRad = Mathf.PerlinNoise(Time.time * wanderSpeed, radiusOffsets[i]);
    //                float currentRadius = Mathf.Lerp(minIdleRadius, maxIdleRadius, noiseRad);

    //                float offsetX = Mathf.Cos(targetAngleRad) * currentRadius;
    //                float offsetY = Mathf.Sin(targetAngleRad) * currentRadius;
    //                Vector3 ringOffset = new Vector3(offsetX, offsetY, 0);

    //                targetPos = player.transform.position + ringOffset;
    //                // lookTarget 保持为 mousePos
    //            }
    //        }
    //        else
    //        {
    //            // 攻击站位
    //            Vector3 baseAttackPos = player.transform.position + (playerToMouseDir * attackForwardDist);
    //            targetPos = baseAttackPos + attackRandomOffsets[i];
    //        }

    //        // 移动
    //        drone.position = Vector3.SmoothDamp(drone.position, targetPos, ref currentVelocities[i], followSmoothTime);

    //        // 旋转
    //        RotateDrone(drone, lookTarget - drone.position);
    //    }
    //}
    #endregion

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

        for (int i = 0; i < droneCount; i++)
        {
            Transform drone = droneTransforms[i];
            DroneVisuals v = droneVisualsList[i];

            v.SetVFXActive(true, v.muzzleParticles, v.muzzleObj);
            v.line.enabled = true;

            Vector3 fireDir = drone.right;

            // 起点
            Vector3 startPos = drone.position;
            if (v.muzzleObj) startPos = v.muzzleObj.transform.position;

            v.line.SetPosition(0, startPos);

            // 1. 墙壁检测
            float actualDist = laserRange;
            Vector3 endPos;
            Vector3 hitNormal = -fireDir;

            RaycastHit2D wallHit = Physics2D.Raycast(startPos, fireDir, laserRange, wallLayer);
            if (wallHit.collider != null)
            {
                actualDist = wallHit.distance;
                endPos = wallHit.point;
                hitNormal = wallHit.normal;

                v.SetVFXActive(true, v.hitParticles, v.hitObj);
                v.hitObj.transform.position = endPos;
                v.hitObj.transform.up = hitNormal;
            }
            else
            {
                endPos = startPos + fireDir * laserRange;
                v.SetVFXActive(false, v.hitParticles, v.hitObj);
            }

            v.line.SetPosition(1, endPos);

            // 2. 伤害检测 - BoxCast
            if (shouldDamage)
            {
                Vector2 boxCenter = (Vector2)startPos + (Vector2)fireDir * (actualDist * 0.5f);
                Vector2 boxSize = new Vector2(actualDist, laserWidth);

                // 角度就是无人机当前的旋转角度
                float angle = drone.eulerAngles.z;

                RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCenter, boxSize, angle, Vector2.zero, 0f, enemyLayer);

                foreach (var hit in hits)
                {
                    var damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damagePerTick, hit.point, fireDir);
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
        currentState = DroneState.Idle;
        UpdateActiveDrones();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        for (int i = 0; i < droneTransforms.Count; i++)
        {
            droneTransforms[i].gameObject.SetActive(false);
        }
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        base.UpgradeModule(moduleType, statType);
        if (moduleType == ModuleType.LaserDrone)
        {
            switch (statType)
            {
                case StatType.BeamPerTick:
                    damagePerTick = (int)UpgradeManager.Instance.GetStat(moduleType, statType);
                    break;
                case StatType.BeamCooldown:
                    laserCD = UpgradeManager.Instance.GetStat(moduleType, statType);
                    break;
                case StatType.BeamCount:
                    droneCount = (int)UpgradeManager.Instance.GetStat(moduleType, statType);
                    break;
                case StatType.BeamRange:
                    laserRange = (int)UpgradeManager.Instance.GetStat(moduleType, statType);
                    detectionRadius = laserRange + 0.2f;
                    break;
            }
        }

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

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.transform.position, detectionRadius);
        }
    }
}