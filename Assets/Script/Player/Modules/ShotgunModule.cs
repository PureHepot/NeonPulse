using UnityEngine;

public class ShotgunModule : PlayerModule
{
    private const string WeaponSelfBackForceStatId = "weapon.selfbackforce";
    private const string WeaponDoubleDamageEffectId = PluginSpecialEffectUtility.DoubleWeaponDamageEffectId;
    private const string WeaponDoubleAttackSpeedEffectId = PluginSpecialEffectUtility.DoubleWeaponAttackSpeedEffectId;
    private const string ShotgunFireRateStatId = "weapon.shotgunfirerate";
    private const string ShotgunDamageStatId = "weapon.shotgundamage";
    private const string ShotgunPelletCountStatId = "weapon.shotgunpelletcount";
    private const string ShotgunSpreadAngleStatId = "weapon.shotgunspreadangle";

    [Header("Refs")]
    public Transform muzzle;
    public Transform partToRotate;
    public GameObject bulletPrefab;

    [Header("Rotation")]
    public float rotationSpeed = 15f;

    private float fireRate;
    private int damage;
    private int pelletCount;
    private float spreadAngle;
    private float cooldown;

    protected override void OnInitialize()
    {
        if (muzzle != null)
            muzzle.gameObject.SetActive(false);

        RecalculateStats();
    }

    protected override void OnActivate()
    {
        if (muzzle != null)
            muzzle.gameObject.SetActive(true);
    }

    protected override void OnDeactivate()
    {
        if (muzzle != null)
            muzzle.gameObject.SetActive(false);
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead)
            return;

        HandleRotation();

        if (cooldown > 0f)
            cooldown -= DeltaTime;

        if (HasControl && InputManager.Instance.Mouse0() && cooldown <= 0f)
            Fire();
    }

    private void RecalculateStats()
    {
        fireRate = ApplyFireIntervalMultiplier(GetStat(ShotgunFireRateStatId, 1.5f));
        damage = Mathf.RoundToInt(ApplyDamageMultiplier(GetStat(ShotgunDamageStatId, 2f)));
        pelletCount = Mathf.Max(1, Mathf.RoundToInt(GetStat(ShotgunPelletCountStatId, 6f)));
        spreadAngle = GetStat(ShotgunSpreadAngleStatId, 30f);
    }

    private void Fire()
    {
        cooldown = fireRate;
        ApplyFireRecoil();

        float baseAngle = muzzle.eulerAngles.z;
        float startAngle = baseAngle - spreadAngle * 0.5f;
        float step = pelletCount > 1 ? spreadAngle / (pelletCount - 1) : 0f;

        for (int index = 0; index < pelletCount; index++)
            SpawnPellet(startAngle + step * index);
    }

    private void SpawnPellet(float angle)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        AudioManager.Instance.PlayEffect("Shotgunnershoot", 0.1f, 1f);
        GameObject bullet = ObjectPoolManager.Instance.Get(bulletPrefab, muzzle.position, rot);
        var bulletScript = bullet.GetComponent<PlayerShotgunBullet>();
        if (bulletScript != null)
            bulletScript.damage = damage;
    }

    private void HandleRotation()
    {
        if (partToRotate == null)
            return;

        Vector3 mousePos = HasControl ? MUtils.GetMouseWorldPosition() : partToRotate.position + partToRotate.right;
        Vector2 dir = mousePos - partToRotate.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion target = Quaternion.AngleAxis(angle, Vector3.forward);
        partToRotate.rotation = Quaternion.Slerp(partToRotate.rotation, target, rotationSpeed * DeltaTime);
    }

    private void ApplyFireRecoil()
    {
        if (player == null)
            return;

        Vector2 shotDirection = muzzle != null ? (Vector2)muzzle.right : (partToRotate != null ? (Vector2)partToRotate.right : (Vector2)transform.right);
        if (shotDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        float selfBackForce = GetStat(WeaponSelfBackForceStatId, 0f);
        if (selfBackForce <= 0f)
            return;

        player.AddImpulse(-shotDirection.normalized * selfBackForce);
    }

    private float ApplyDamageMultiplier(float baseDamage)
    {
        float multiplier = 1f;
        if (TryGetPlugin(WeaponDoubleDamageEffectId, out var pluginRuntime))
            multiplier *= PluginSpecialEffectUtility.ResolveMultiplier(pluginRuntime);

        return baseDamage * multiplier;
    }

    private float ApplyFireIntervalMultiplier(float baseFireInterval)
    {
        float multiplier = 1f;
        if (TryGetPlugin(WeaponDoubleAttackSpeedEffectId, out var pluginRuntime))
            multiplier *= PluginSpecialEffectUtility.ResolveMultiplier(pluginRuntime);

        if (multiplier <= 0f)
            return baseFireInterval;

        return baseFireInterval / multiplier;
    }
}
