using System.Collections.Generic;
using UnityEngine;

public class OriginShooterModule : RangedWeaponModule
{
    private const string WeaponDamageStatId = "weapon.damage";
    private const string WeaponShotSpeedStatId = "weapon.attackspeed";
    private const string WeaponCritChanceStatId = "weapon.critchance";
    private const string WeaponCritDamageStatId = "weapon.critdamage";
    private const string WeaponGunCountStatId = "weapon.weaponcount";
    private const string WeaponGunPierceCount = "weapon.piercecount";
    private const float DefaultFireInterval = 0.8f;
    private const int DefaultDamage = 2;
    private const float DefaultProjectileSpeed = 20f;
    private const float DefaultProjectileLifetime = 2f;
    private const float MuzzleAngleStep = 15f;
    private const float DefaultPrototypeRadius = 0.5f;

    [Header("Hierarchy Refs")]
    public Transform partToRotate;
    public List<Transform> muzzles;
    public GameObject bulletPrefab;

    [Header("Rotation Settings")]
    public float rotationSpeed = 15f;

    [Header("Combat Settings")]
    public float sequenceDelay = 0.01f;
    public float recoilImpulse = 0.55f;

    public int currentLevel = 1;

    private float fireInterval = DefaultFireInterval;
    private int damagePerShot = DefaultDamage;
    private int activeMuzzleCount = 1;
    private float critChance;
    private float critDamageMultiplier = 1f;
    private float projectileSpeed = DefaultProjectileSpeed;
    private float projectileLifetime = DefaultProjectileLifetime;
    private LayerMask projectileHitLayer;
    private LayerMask projectileWallLayer;
    private readonly List<Transform> generatedMuzzles = new();
    private float prototypeRadius = DefaultPrototypeRadius;

    protected override void OnWeaponInitialize()
    {
        enabled = false;
        CacheProjectileDefaults();
        CachePrototypeRadius();
        RefreshWeaponStats();
    }

    protected override void OnActivate()
    {
        RefreshWeaponStats();
        SyncGeneratedMuzzles();
        UpdateMuzzleVisuals();
        enabled = true;
    }

    protected override void OnDeactivate()
    {
        enabled = false;
        HideAllMuzzleVisuals();
    }

    private void OnDestroy()
    {
        ClearGeneratedMuzzleClones();
    }

    protected override void OnWeaponUpdate()
    {
        RotateTowardsAim(partToRotate != null ? partToRotate : transform, rotationSpeed);
        UpdateMuzzleVisuals();

        if (WantsPrimaryFire && CanFire)
            Fire();
    }

    private void RefreshWeaponStats()
    {
        damagePerShot = Mathf.Max(1, Mathf.RoundToInt(ApplyWeaponDamageMultiplier(GetStat(WeaponDamageStatId, DefaultDamage))));
        critChance = NormalizeChance(GetStat(WeaponCritChanceStatId, 0f));
        critDamageMultiplier = ResolveCritDamageMultiplier(GetStat(WeaponCritDamageStatId, 100f));

        fireInterval = Mathf.Max(0.01f, ApplyWeaponFireIntervalMultiplier(ResolveFireInterval()));

        activeMuzzleCount = ResolveMuzzleCount();
        currentLevel = activeMuzzleCount;
        SyncGeneratedMuzzles();
    }

    private void Fire()
    {
        SetCooldown(fireInterval);

        var fallbackOrigin = partToRotate != null ? partToRotate : transform;
        var aimTarget = ResolveAimTarget(fallbackOrigin);
        ApplyFireRecoil(aimTarget);
        var muzzlePlan = BuildShooterMuzzlePlan(aimTarget, activeMuzzleCount);
        var fireContext = new WeaponFireContext(this, muzzlePlan.Count);
        ApplyMuzzlePlanEffects(fireContext, muzzlePlan);
        fireContext.totalShots = muzzlePlan.Count;

        for (int index = 0; index < muzzlePlan.Count; index++)
        {
            fireContext.shotIndex = index;
            fireContext.currentMuzzle = muzzlePlan[index];
            SpawnBullet(fireContext);
        }
    }

    private void SpawnBullet(WeaponFireContext fireContext)
    {
        if (bulletPrefab == null || fireContext?.currentMuzzle == null)
            return;

        int finalDamage = RollCriticalDamage();
        AudioManager.Instance?.PlayEffect("Shootershoot", 0.4f, 1f);
        var spawnData = new ProjectileSpawnData
        {
            prefab = bulletPrefab,
            position = fireContext.currentMuzzle.position,
            rotation = fireContext.currentMuzzle.rotation,
            damage = finalDamage,
            speed = projectileSpeed,
            lifeTime = projectileLifetime,
            hitLayer = projectileHitLayer,
            wallLayer = projectileWallLayer
        };

        SpawnProjectile(spawnData, fireContext);
    }

    private void UpdateMuzzleVisuals()
    {
        if (activeMuzzleCount <= 0)
        {
            HideAllMuzzleVisuals();
            return;
        }

        SyncGeneratedMuzzles();

        var aimTarget = ResolveAimTarget(partToRotate != null ? partToRotate : transform);
        var muzzlePlan = BuildShooterMuzzlePlan(aimTarget, activeMuzzleCount);
        for (int index = 0; index < generatedMuzzles.Count; index++)
        {
            var muzzle = generatedMuzzles[index];
            if (muzzle == null)
                continue;

            bool active = index < muzzlePlan.Count;
            muzzle.gameObject.SetActive(active);
            if (!active)
                continue;

            muzzle.position = muzzlePlan[index].position;
            muzzle.rotation = muzzlePlan[index].rotation;
        }
    }

    private void CacheProjectileDefaults()
    {
        if (bulletPrefab == null)
            return;

        var bulletDefaults = bulletPrefab.GetComponent<PlayerBullet>();
        if (bulletDefaults == null)
            return;

        projectileSpeed = bulletDefaults.speed;
        projectileLifetime = bulletDefaults.lifeTime;
        projectileHitLayer = bulletDefaults.hitLayer;
        projectileWallLayer = bulletDefaults.WallLayer;
    }

    private int ResolveMuzzleCount()
    {
        int statCount = GetIntStat(WeaponGunCountStatId, 1);
        if (statCount > 0)
            return Mathf.Max(1, statCount);

        return 1;
    }

    private float ResolveFireInterval()
    {
        float defaultInterval = DefaultFireInterval;
        float shotSpeedValue = GetStat(WeaponShotSpeedStatId, 0f);
        if (shotSpeedValue <= 0f)
            return defaultInterval;

        if (shotSpeedValue > 10f)
            return defaultInterval * (100f / shotSpeedValue);

        return 1f / shotSpeedValue;
    }

    private Vector3 ResolveAimTarget(Transform fallbackOrigin)
    {
        if (HasControl)
            return MUtils.GetMouseWorldPosition();

        var origin = fallbackOrigin != null ? fallbackOrigin : transform;
        return origin.position + origin.right * 10f;
    }

    private List<WeaponMuzzlePoint> BuildShooterMuzzlePlan(Vector3 aimTarget, int requestedCount)
    {
        requestedCount = Mathf.Max(1, requestedCount);

        Vector3 center = player != null ? player.transform.position : transform.position;
        Vector3 aimDirection = aimTarget - center;
        if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            aimDirection = partToRotate != null ? partToRotate.right : transform.right;

        aimDirection.Normalize();
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion aimRotation = Quaternion.AngleAxis(aimAngle, Vector3.forward);
        var muzzlePlan = new List<WeaponMuzzlePoint>(requestedCount);

        for (int index = 0; index < requestedCount; index++)
        {
            float centeredIndex = index - (requestedCount - 1) * 0.5f;
            float angleOffset = centeredIndex * MuzzleAngleStep;
            Vector3 muzzleOffset = aimRotation * (Quaternion.AngleAxis(angleOffset, Vector3.forward) * (Vector3.right * prototypeRadius));
            Vector3 muzzlePosition = center + muzzleOffset;
            muzzlePlan.Add(new WeaponMuzzlePoint
            {
                position = muzzlePosition,
                rotation = aimRotation,
                visualTransform = index < generatedMuzzles.Count ? generatedMuzzles[index] : null,
                isVirtual = index != 0
            });
        }

        return muzzlePlan;
    }

    private Transform GetPrototypeMuzzle()
    {
        if (muzzles == null)
            return null;

        for (int index = 0; index < muzzles.Count; index++)
        {
            if (muzzles[index] != null)
                return muzzles[index];
        }

        return null;
    }

    private void CachePrototypeRadius()
    {
        var prototypeMuzzle = GetPrototypeMuzzle();
        if (prototypeMuzzle == null)
        {
            prototypeRadius = DefaultPrototypeRadius;
            return;
        }

        Vector3 center = player != null ? player.transform.position : transform.position;
        float radius = Vector3.Distance(center, prototypeMuzzle.position);
        prototypeRadius = radius > 0.001f ? radius : DefaultPrototypeRadius;
    }

    private int RollCriticalDamage()
    {
        if (critChance <= 0f || Random.value > critChance)
            return damagePerShot;

        return Mathf.Max(1, Mathf.RoundToInt(damagePerShot * critDamageMultiplier));
    }

    private static float NormalizeChance(float rawChance)
    {
        if (rawChance <= 0f)
            return 0f;

        return rawChance > 1f ? rawChance / 100f : rawChance;
    }

    private static float ResolveCritDamageMultiplier(float rawValue)
    {
        if (rawValue <= 0f)
            return 1f;

        if (rawValue > 10f)
            return rawValue / 100f;

        return rawValue;
    }

    private void ApplyFireRecoil(Vector3 aimTarget)
    {
        if (player == null)
            return;

        Vector2 aimDirection = aimTarget - player.transform.position;
        if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        float recoilScale = 1f + Mathf.Max(0, activeMuzzleCount - 1) * 0.2f;
        ApplySelfBackForce(aimDirection, recoilImpulse, recoilScale);
    }

    private void SyncGeneratedMuzzles()
    {
        var prototypeMuzzle = GetPrototypeMuzzle();
        if (prototypeMuzzle == null)
            return;

        if (generatedMuzzles.Count == 0)
            generatedMuzzles.Add(prototypeMuzzle);

        while (generatedMuzzles.Count < activeMuzzleCount)
        {
            var cloneObject = Instantiate(prototypeMuzzle.gameObject, prototypeMuzzle.parent);
            cloneObject.name = $"{prototypeMuzzle.name}_RuntimeClone_{generatedMuzzles.Count}";
            generatedMuzzles.Add(cloneObject.transform);
        }

        for (int index = 0; index < generatedMuzzles.Count; index++)
        {
            var muzzle = generatedMuzzles[index];
            if (muzzle == null)
                continue;

            muzzle.gameObject.SetActive(index < activeMuzzleCount);
        }
    }

    private void HideAllMuzzleVisuals()
    {
        for (int index = 0; index < generatedMuzzles.Count; index++)
        {
            if (generatedMuzzles[index] != null)
                generatedMuzzles[index].gameObject.SetActive(false);
        }
    }

    private void ClearGeneratedMuzzleClones()
    {
        for (int index = generatedMuzzles.Count - 1; index >= 1; index--)
        {
            if (generatedMuzzles[index] != null)
                Destroy(generatedMuzzles[index].gameObject);
        }

        generatedMuzzles.Clear();
    }
}
