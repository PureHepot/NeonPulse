using System.Collections.Generic;
using UnityEngine;

public class LaserDroneModule : PlayerModule
{
    private const string BeamPerTickStatId = "weapon.beampertick";
    private const string BeamCooldownStatId = "weapon.beamcooldown";
    private const string BeamCountStatId = "weapon.beamcount";
    private const string BeamRangeStatId = "weapon.beamrange";

    [Header("Drone Refs")]
    public List<Transform> droneTransforms;

    [Header("Visual Effects")]
    public GameObject laserLinePrefab;
    public GameObject muzzleVFXPrefab;
    public GameObject hitVFXPrefab;

    [Header("Movement Settings")]
    public float followSmoothTime = 0.6f;
    public float rotateSpeed = 15f;
    public float minIdleRadius = 1.2f;
    public float maxIdleRadius = 2.4f;
    public float wanderSpeed = 0.5f;
    public float maxDistanceBeforeFollow = 8f;

    [Header("Auto Attack Settings")]
    public float detectionRadius = 10f;
    public float laserWidth = 0.5f;
    public int droneCount = 1;
    public int damagePerTick = 1;
    public float damageInterval = 0.2f;
    public float laserRange = 15f;
    public float chargeTime = 0.6f;
    public float maxFireDuration = 5f;
    public float laserCD = 5f;
    public LayerMask enemyLayer;
    public LayerMask wallLayer;

    private enum DroneState
    {
        Idle,
        Charging,
        Firing,
        Cooldown
    }

    private sealed class DroneVisuals
    {
        public LineRenderer line;
        public GameObject muzzleObject;
        public GameObject hitObject;
        public ParticleSystem[] muzzleParticles;
        public ParticleSystem[] hitParticles;

        public void SetParticles(bool active, ParticleSystem[] particles)
        {
            if (particles == null)
                return;

            foreach (var particle in particles)
            {
                if (particle == null)
                    continue;

                var emission = particle.emission;
                emission.enabled = active;
                if (active && !particle.isPlaying)
                    particle.Play();
            }
        }
    }

    private readonly List<DroneVisuals> droneVisualsList = new();
    private Transform[] currentTargets;
    private Vector3[] currentVelocities;
    private float[] noiseOffsets;
    private float[] radiusOffsets;
    private GameObject debrisHolder;
    private DroneState currentState = DroneState.Idle;
    private float stateTimer;
    private float damageTimer;

    protected override void OnInitialize()
    {
        RecalculateStats();

        if (debrisHolder != null)
            Destroy(debrisHolder);

        debrisHolder = new GameObject($"[{name}]_DroneHolder");
        int count = droneTransforms != null ? droneTransforms.Count : 0;
        currentTargets = new Transform[count];
        currentVelocities = new Vector3[count];
        noiseOffsets = new float[count];
        radiusOffsets = new float[count];
        droneVisualsList.Clear();

        for (int index = 0; index < count; index++)
        {
            var drone = droneTransforms[index];
            if (drone == null)
                continue;

            drone.SetParent(debrisHolder.transform);
            noiseOffsets[index] = Random.Range(0f, 1000f);
            radiusOffsets[index] = Random.Range(5000f, 6000f);
            droneVisualsList.Add(BuildVisuals(drone));
        }

        UpdateActiveDrones();
        if (player != null && player.UseUnscaledTime)
            PreviewManager.Instance.SetLayerRecursively(debrisHolder, LayerMask.NameToLayer("UI_Model"));
    }

    protected override void OnActivate()
    {
        currentState = DroneState.Idle;
        UpdateActiveDrones();
    }

    protected override void OnDeactivate()
    {
        DisableAllLasers();
        if (droneTransforms == null)
            return;

        for (int index = 0; index < droneTransforms.Count; index++)
        {
            if (droneTransforms[index] != null)
                droneTransforms[index].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (debrisHolder != null)
            Destroy(debrisHolder);
    }

    public override void OnModuleUpdate()
    {
        if (player == null || droneTransforms == null)
            return;

        HandleAutoState();
        HandleDroneMovement();

        if (currentState == DroneState.Firing)
            HandleLaserFiring();
        else
            DisableAllLasers();
    }

    private DroneVisuals BuildVisuals(Transform drone)
    {
        var visuals = new DroneVisuals();

        if (laserLinePrefab != null)
        {
            GameObject lineObject = Instantiate(laserLinePrefab, Vector3.zero, Quaternion.identity, debrisHolder.transform);
            visuals.line = lineObject.GetComponent<LineRenderer>();
            if (visuals.line != null)
            {
                visuals.line.startWidth = laserWidth * 2f;
                visuals.line.endWidth = laserWidth * 2f;
                visuals.line.enabled = false;
            }
        }

        if (muzzleVFXPrefab != null)
        {
            visuals.muzzleObject = Instantiate(muzzleVFXPrefab, drone.position, drone.rotation, drone);
            visuals.muzzleObject.transform.localPosition = Vector3.right * 0.5f;
            visuals.muzzleParticles = visuals.muzzleObject.GetComponentsInChildren<ParticleSystem>(true);
            visuals.SetParticles(false, visuals.muzzleParticles);
        }

        if (hitVFXPrefab != null)
        {
            visuals.hitObject = Instantiate(hitVFXPrefab, Vector3.zero, Quaternion.identity, debrisHolder.transform);
            visuals.hitParticles = visuals.hitObject.GetComponentsInChildren<ParticleSystem>(true);
            visuals.SetParticles(false, visuals.hitParticles);
        }

        return visuals;
    }

    private void RecalculateStats()
    {
        int maxDroneCount = droneTransforms != null ? droneTransforms.Count : 0;
        damagePerTick = Mathf.RoundToInt(GetStat(BeamPerTickStatId, damagePerTick));
        laserCD = GetStat(BeamCooldownStatId, laserCD);
        droneCount = maxDroneCount > 0
            ? Mathf.Clamp(Mathf.RoundToInt(GetStat(BeamCountStatId, droneCount)), 1, maxDroneCount)
            : 0;
        laserRange = GetStat(BeamRangeStatId, laserRange);
        detectionRadius = laserRange + 0.2f;
    }

    private void UpdateActiveDrones()
    {
        if (droneTransforms == null)
            return;

        for (int index = 0; index < droneTransforms.Count; index++)
        {
            if (droneTransforms[index] != null)
                droneTransforms[index].gameObject.SetActive(index < droneCount);
        }
    }

    private void HandleAutoState()
    {
        if (player.IsDead)
        {
            currentState = DroneState.Idle;
            return;
        }

        switch (currentState)
        {
            case DroneState.Idle:
                if (TryFindTargets())
                {
                    currentState = DroneState.Charging;
                    stateTimer = 0f;
                }
                break;
            case DroneState.Charging:
                stateTimer += DeltaTime;
                UpdateTargets();
                if (!HasAnyTarget())
                {
                    currentState = DroneState.Idle;
                    stateTimer = 0f;
                }
                else if (stateTimer >= chargeTime)
                {
                    currentState = DroneState.Firing;
                    stateTimer = 0f;
                    damageTimer = damageInterval;
                }
                break;
            case DroneState.Firing:
                stateTimer += DeltaTime;
                UpdateTargets();
                if (!HasAnyTarget())
                {
                    currentState = DroneState.Cooldown;
                    stateTimer = 0f;
                }
                else if (stateTimer >= maxFireDuration)
                {
                    currentState = DroneState.Cooldown;
                    stateTimer = 0f;
                }
                break;
            case DroneState.Cooldown:
                stateTimer += DeltaTime;
                if (stateTimer >= laserCD)
                    currentState = DroneState.Idle;
                break;
        }
    }

    private bool HasAnyTarget()
    {
        for (int index = 0; index < droneCount; index++)
        {
            if (currentTargets[index] != null && currentTargets[index].gameObject.activeInHierarchy)
                return true;
        }

        return TryFindTargets();
    }

    private bool TryFindTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, detectionRadius, enemyLayer);
        if (hits.Length == 0)
            return false;

        UpdateTargets();
        return HasAssignedTarget();
    }

    private bool HasAssignedTarget()
    {
        for (int index = 0; index < droneCount; index++)
        {
            if (currentTargets[index] != null)
                return true;
        }

        return false;
    }

    private void UpdateTargets()
    {
        HashSet<Transform> usedTargets = new();
        for (int index = 0; index < droneCount; index++)
            UpdateDroneTarget(index, usedTargets);
    }

    private void UpdateDroneTarget(int index, HashSet<Transform> usedTargets)
    {
        var drone = droneTransforms[index];
        if (drone == null)
            return;

        Transform currentTarget = currentTargets[index];
        if (currentTarget != null &&
            currentTarget.gameObject.activeInHierarchy &&
            Vector3.Distance(drone.position, currentTarget.position) <= detectionRadius * 1.2f &&
            !usedTargets.Contains(currentTarget))
        {
            usedTargets.Add(currentTarget);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(drone.position, detectionRadius, enemyLayer);
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
        {
            var candidate = hits[hitIndex].transform;
            if (candidate == null || !candidate.gameObject.activeInHierarchy || usedTargets.Contains(candidate))
                continue;

            float distance = (candidate.position - drone.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = candidate;
            }
        }

        currentTargets[index] = nearestTarget;
        if (nearestTarget != null)
            usedTargets.Add(nearestTarget);
    }

    private void HandleDroneMovement()
    {
        for (int index = 0; index < droneCount; index++)
        {
            Transform drone = droneTransforms[index];
            if (drone == null)
                continue;

            Vector3 targetPosition;
            Vector3 lookTarget;

            if (currentState == DroneState.Charging || currentState == DroneState.Firing)
            {
                lookTarget = currentTargets[index] != null ? currentTargets[index].position : drone.position + drone.right;
                float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[index]);
                float angleRad = noiseAngle * Mathf.PI * 4f;
                Vector3 ringOffset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * minIdleRadius;
                targetPosition = player.transform.position + ringOffset;
            }
            else
            {
                float noiseAngle = Mathf.PerlinNoise(Time.time * wanderSpeed, noiseOffsets[index]);
                float angleRad = noiseAngle * Mathf.PI * 4f;
                float noiseRadius = Mathf.PerlinNoise(Time.time * wanderSpeed, radiusOffsets[index]);
                float currentRadius = Mathf.Lerp(minIdleRadius, maxIdleRadius, noiseRadius);
                Vector3 ringOffset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * currentRadius;
                targetPosition = player.transform.position + ringOffset;
                lookTarget = targetPosition;

                if (Vector3.Distance(drone.position, player.transform.position) > maxDistanceBeforeFollow)
                    targetPosition = player.transform.position;
            }

            drone.position = Vector3.SmoothDamp(drone.position, targetPosition, ref currentVelocities[index], followSmoothTime);
            RotateDrone(drone, lookTarget - drone.position);
        }
    }

    private void RotateDrone(Transform drone, Vector3 lookDirection)
    {
        if (lookDirection == Vector3.zero)
            return;

        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        drone.rotation = Quaternion.Slerp(drone.rotation, targetRotation, rotateSpeed * DeltaTime);
    }

    private void DisableAllLasers()
    {
        for (int index = 0; index < Mathf.Min(droneCount, droneVisualsList.Count); index++)
        {
            var visuals = droneVisualsList[index];
            if (visuals.line != null)
                visuals.line.enabled = false;

            visuals.SetParticles(false, visuals.muzzleParticles);
            visuals.SetParticles(false, visuals.hitParticles);
        }
    }

    private void HandleLaserFiring()
    {
        damageTimer += DeltaTime;
        bool shouldDamage = damageTimer >= damageInterval;

        for (int index = 0; index < droneCount; index++)
        {
            if (index >= droneVisualsList.Count || droneTransforms[index] == null)
                continue;

            Transform drone = droneTransforms[index];
            var visuals = droneVisualsList[index];
            visuals.SetParticles(true, visuals.muzzleParticles);
            if (visuals.line != null)
                visuals.line.enabled = true;

            Vector3 fireDirection = drone.right;
            Vector3 startPosition = visuals.muzzleObject != null ? visuals.muzzleObject.transform.position : drone.position;
            Vector3 endPosition = startPosition + fireDirection * laserRange;
            float actualDistance = laserRange;
            Vector3 hitNormal = -fireDirection;

            RaycastHit2D wallHit = Physics2D.Raycast(startPosition, fireDirection, laserRange, wallLayer);
            if (wallHit.collider != null)
            {
                actualDistance = wallHit.distance;
                endPosition = wallHit.point;
                hitNormal = wallHit.normal;
                if (visuals.hitObject != null)
                {
                    visuals.hitObject.transform.position = endPosition;
                    visuals.hitObject.transform.up = hitNormal;
                }
                visuals.SetParticles(true, visuals.hitParticles);
            }
            else
            {
                visuals.SetParticles(false, visuals.hitParticles);
            }

            if (visuals.line != null)
            {
                visuals.line.SetPosition(0, startPosition);
                visuals.line.SetPosition(1, endPosition);
            }

            if (!shouldDamage)
                continue;

            Vector2 boxCenter = (Vector2)startPosition + (Vector2)fireDirection * (actualDistance * 0.5f);
            Vector2 boxSize = new Vector2(actualDistance, laserWidth);
            float angle = drone.eulerAngles.z;
            RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCenter, boxSize, angle, Vector2.zero, 0f, enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damagePerTick, hit.point, fireDirection);
            }
        }

        if (shouldDamage)
            damageTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.transform.position, detectionRadius);
    }
}
