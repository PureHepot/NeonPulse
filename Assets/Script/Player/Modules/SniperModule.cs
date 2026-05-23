using System.Collections.Generic;
using UnityEngine;

public class SniperModule : ProjectileWeaponModule
{
    private const string SnipeFireRateStatId = "weapon.attackspeed";
    private const string SnipeDamageStatId = "weapon.damage";
    private const string SnipePenetrationStatId = "weapon.snipepenetration";
    private const float DefaultProjectileSpeed = 40f;
    private const float DefaultProjectileLifetime = 3f;

    [Header("Refs")]
    public Transform muzzle;
    public Transform partToRotate;
    public GameObject sniperBulletPrefab;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    private readonly List<Transform> muzzleList = new();
    private float fireInterval;
    private int damage;
    private int penetration;
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
        fireInterval = Mathf.Max(0.01f, ApplyWeaponFireIntervalMultiplier(ResolveFireInterval()));
        damage = Mathf.RoundToInt(ApplyWeaponDamageMultiplier(GetStat(SnipeDamageStatId, 4f)));
        penetration = Mathf.Max(1, Mathf.RoundToInt(GetStat(SnipePenetrationStatId, 2f)));
    }

    private float ResolveFireInterval()
    {
        float shotsPerSecond = GetStat(SnipeFireRateStatId, 1.8f);
        if (shotsPerSecond <= 0f)
            return 1f / 1.8f;

        return 1f / shotsPerSecond;
    }

    private void Fire()
    {
        if (sniperBulletPrefab == null)
            return;

        SetCooldown(fireInterval);
        AudioManager.Instance?.PlayEffect("SniperShoot");
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

            var spawnData = new ProjectileSpawnData
            {
                prefab = sniperBulletPrefab,
                position = muzzlePlan[index].position,
                rotation = muzzlePlan[index].rotation,
                damage = damage,
                speed = projectileSpeed,
                lifeTime = projectileLifetime,
                hitLayer = projectileHitLayer,
                wallLayer = projectileWallLayer
            };

            GameObject bullet = SpawnProjectile(spawnData, fireContext);
            var bulletScript = bullet != null ? bullet.GetComponent<PlayerSniperBullet>() : null;
            if (bulletScript != null)
                bulletScript.penetrationCount = penetration;
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
        if (sniperBulletPrefab == null)
            return;

        var bulletDefaults = sniperBulletPrefab.GetComponent<PlayerSniperBullet>();
        if (bulletDefaults == null)
            return;

        projectileSpeed = bulletDefaults.speed;
        projectileLifetime = bulletDefaults.lifeTime;
        projectileHitLayer = bulletDefaults.hitLayer;
        projectileWallLayer = bulletDefaults.wallLayer;
    }
}
