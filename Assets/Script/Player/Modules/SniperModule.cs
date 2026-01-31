using UnityEngine;

public class SniperModule : PlayerModule
{
    [Header("Refs")]
    public Transform muzzle;
    public Transform partToRotate;
    public GameObject sniperBulletPrefab;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    private float fireRate;
    private int damage;
    private int penetration;

    private float cooldown;

    public override void Initialize(PlayerController player)
    {
        base.Initialize(player);
        RecalculateStats();
    }

    public override void OnModuleUpdate()
    {
        HandleRotation();

        if (cooldown > 0)
            cooldown -= Time.deltaTime;

        if (InputManager.Instance.Mouse0() && cooldown <= 0)
        {
            Fire();
        }
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        if (moduleType != ModuleType.Sniper) return;
        RecalculateStats();
    }

    void RecalculateStats()
    {
        fireRate = UpgradeManager.Instance.GetStat(ModuleType.Sniper, StatType.SnipeFireRate);
        if (fireRate <= 0) fireRate = 1.2f;

        damage = (int)UpgradeManager.Instance.GetStat(ModuleType.Sniper, StatType.SnipeDamage);
        if (damage <= 0) damage = 10;

        penetration = (int)UpgradeManager.Instance.GetStat(ModuleType.Sniper, StatType.SnipePenetration);
    }

    void Fire()
    {
        cooldown = fireRate;

        GameObject bullet = ObjectPoolManager.Instance.Get(
            sniperBulletPrefab,
            muzzle.position,
            muzzle.rotation
        );

        var sniperBullet = bullet.GetComponent<PlayerSniperBullet>();
        sniperBullet.damage = damage;
        sniperBullet.penetrationCount = penetration;
    }

    void HandleRotation()
    {
        if (partToRotate == null) return;

        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        Vector2 dir = mousePos - partToRotate.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion target = Quaternion.AngleAxis(angle, Vector3.forward);

        partToRotate.rotation = Quaternion.Slerp(
            partToRotate.rotation,
            target,
            rotationSpeed * Time.deltaTime
        );
    }
}
