using UnityEngine;

public class ShotgunModule : PlayerModule
{
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

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (muzzle != null)
            muzzle.gameObject.SetActive(false);

        RecalculateStats();
    }

    public override void OnActivate()
    {
        base.OnActivate();
        if (muzzle != null)
            muzzle.gameObject.SetActive(true);
    }

    public override void OnDeactivate()
    {
        if (muzzle != null)
            muzzle.gameObject.SetActive(false);

        base.OnDeactivate();
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
        if (moduleType != ModuleType.Shotgun) return;
        RecalculateStats();
    }

    void RecalculateStats()
    {
        fireRate = UpgradeManager.Instance.GetStat(
            ModuleType.Shotgun, StatType.ShotgunFireRate);
        if (fireRate <= 0) fireRate = 3.0f;

        damage = (int)UpgradeManager.Instance.GetStat(
            ModuleType.Shotgun, StatType.ShotgunDamage);
        if (damage <= 0) damage = 2;

        pelletCount = (int)UpgradeManager.Instance.GetStat(
            ModuleType.Shotgun, StatType.ShotgunPelletCount);
        if (pelletCount <= 0) pelletCount = 6;

        spreadAngle = UpgradeManager.Instance.GetStat(
            ModuleType.Shotgun, StatType.ShotgunSpreadAngle);
        if (spreadAngle <= 0) spreadAngle = 30f;
    }

    void Fire()
    {
        cooldown = fireRate;

        float baseAngle = muzzle.eulerAngles.z;
        float startAngle = baseAngle - spreadAngle * 0.5f;

        float step = pelletCount > 1
            ? spreadAngle / (pelletCount - 1)
            : 0f;

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + step * i;
            SpawnPellet(angle);
        }
    }

    void SpawnPellet(float angle)
    {
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        GameObject bullet = ObjectPoolManager.Instance.Get(
            bulletPrefab,
            muzzle.position,
            rot
        );

        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        if (bulletScript)
            bulletScript.damage = damage;
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

