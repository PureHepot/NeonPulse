using System.Collections.Generic;
using UnityEngine;

public class MachineGunModule : ProjectileWeaponModule
{
    private const string WeaponDamageStatId = "weapon.damage";
    private const string WeaponAttackSpeedStatId = "weapon.attackspeed";
    private const string WeaponProjectileSpeedStatId = "weapon.projectilespeed";
    private const float DefaultFireInterval = 0.12f;
    private const int DefaultDamage = 1;
    private const float DefaultProjectileSpeed = 22f;
    private const float DefaultProjectileLifetime = 2f;
    private const float DefaultSpreadAngle = 52f;

    [Header("Refs")]
    public Transform muzzle;
    public Transform partToRotate;
    public GameObject bulletPrefab;

    [Header("Rotation")]
    public float rotationSpeed = 18f;

    [Header("Spread")]
    public float spreadAngle = DefaultSpreadAngle;

    private readonly List<Transform> muzzleList = new();
    private float fireInterval = DefaultFireInterval;
    private int damage = DefaultDamage;
    private float projectileSpeed = DefaultProjectileSpeed;
    private float projectileLifetime = DefaultProjectileLifetime;
    private LayerMask projectileHitLayer;
    private LayerMask projectileWallLayer;

    protected override void OnWeaponInitialize()
    {
        muzzleList.Clear();
        if (muzzle != null)
        {
            muzzleList.Add(muzzle);
            muzzle.gameObject.SetActive(false);
        }

        CacheProjectileDefaults();
        RecalculateStats();
    }

    protected override void OnActivate()
    {
        RecalculateStats();
        if (muzzle != null)
            muzzle.gameObject.SetActive(true);
    }

    protected override void OnDeactivate()
    {
        if (muzzle != null)
            muzzle.gameObject.SetActive(false);
    }

    protected override void OnWeaponUpdate()
    {
        HandleRotation();

        if (WantsPrimaryFire && CanFire)
            Fire();
    }

    private void RecalculateStats()
    {
        damage = Mathf.Max(1, Mathf.RoundToInt(ApplyWeaponDamageMultiplier(GetStat(WeaponDamageStatId, DefaultDamage))));
        fireInterval = Mathf.Max(0.01f, ApplyWeaponFireIntervalMultiplier(ResolveFireInterval()));
        projectileSpeed = Mathf.Max(0.01f, GetStat(WeaponProjectileSpeedStatId, projectileSpeed > 0f ? projectileSpeed : DefaultProjectileSpeed));
    }

    private float ResolveFireInterval()
    {
        float shotSpeedValue = GetStat(WeaponAttackSpeedStatId, 0f);
        if (shotSpeedValue <= 0f)
            return DefaultFireInterval;

        if (shotSpeedValue > 10f)
            return DefaultFireInterval * (100f / shotSpeedValue);

        return 1f / shotSpeedValue;
    }

    private void Fire()
    {
        if (bulletPrefab == null)
            return;

        SetCooldown(fireInterval);
        ApplySelfBackForce((muzzle != null ? muzzle.right : (partToRotate != null ? partToRotate.right : transform.right)));

        var fallbackOrigin = muzzle != null ? muzzle : (partToRotate != null ? partToRotate : transform);
        var muzzlePlan = BuildMuzzlePlan(muzzleList, 1, fallbackOrigin);
        var fireContext = new WeaponFireContext(this, muzzlePlan.Count);
        ApplyMuzzlePlanEffects(fireContext, muzzlePlan);
        fireContext.totalShots = muzzlePlan.Count;

        for (int index = 0; index < muzzlePlan.Count; index++)
        {
            fireContext.shotIndex = index;
            fireContext.currentMuzzle = muzzlePlan[index];

            Quaternion spreadRotation = muzzlePlan[index].rotation * Quaternion.Euler(0f, 0f, Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f));
            var spawnData = new ProjectileSpawnData
            {
                prefab = bulletPrefab,
                position = muzzlePlan[index].position,
                rotation = spreadRotation,
                damage = damage,
                speed = projectileSpeed,
                lifeTime = projectileLifetime,
                hitLayer = projectileHitLayer,
                wallLayer = projectileWallLayer
            };

            SpawnProjectile(spawnData, fireContext);
        }
    }

    private void HandleRotation()
    {
        if (partToRotate == null)
            return;

        RotateTowardsAim(partToRotate, rotationSpeed);
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
}
