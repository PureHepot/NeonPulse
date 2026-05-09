using UnityEngine;

public class ShotgunModule : ShooterModuleBase
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
    private float force = 15f;
    private Vector2 recoilVelocity;
    private float recoilDamping = 10f;

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
        if (player == null || player.IsDead || player.isPreview) return;

        HandleRotation();

        if (cooldown > 0)
            cooldown -= Time.deltaTime;

        if (InputManager.Instance.Mouse0() && cooldown <= 0)
        {
            Fire();
        }
        if (recoilVelocity.magnitude > 0.01f)
        {
            player.Rigid2d.velocity += recoilVelocity;
            recoilVelocity = Vector2.Lerp(recoilVelocity, Vector2.zero, recoilDamping * Time.deltaTime);
        }
    }

    public override void UpgradeModule(ModuleType ModuleType, StatType statType)
    {
        if (ModuleType != ModuleType.Shotgun) return;
        RecalculateStats();
    }

    void RecalculateStats()
    {
        fireRate = UpgradeManager.Instance.GetStat(ModuleType.Shotgun, StatType.ShotgunFireRate);
        if (fireRate <= 0) fireRate = 1.5f;

        damage = (int)UpgradeManager.Instance.GetStat(ModuleType.Shotgun, StatType.ShotgunDamage);
        if (damage <= 0) damage = 2;

        pelletCount = (int)UpgradeManager.Instance.GetStat(ModuleType.Shotgun, StatType.ShotgunPelletCount);
        if (pelletCount <= 0) pelletCount = 6;

        spreadAngle = UpgradeManager.Instance.GetStat(ModuleType.Shotgun, StatType.ShotgunSpreadAngle);
        if (spreadAngle <= 0) spreadAngle = 30f;
    }

    void Fire()
    {
        cooldown = fireRate;

        float baseAngle = muzzle.eulerAngles.z;
        float startAngle = baseAngle - spreadAngle * 0.5f;
        float step = pelletCount > 1 ? spreadAngle / (pelletCount - 1) : 0f;

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + step * i;
            SpawnPellet(angle);
        }
        recoilVelocity += -(Vector2)muzzle.right * force;
    }

    void SpawnPellet(float angle)
    {
        Quaternion rot = Quaternion.Euler(0, 0, angle);
        AudioManager.Instance.PlayEffect("Shotgunnershoot", 0.1f, 1f);

        GameObject bullet = ObjectPoolManager.Instance.Get(
            bulletPrefab,
            muzzle.position,
            rot
        );

        PlayerShotgunBullet bulletScript = bullet.GetComponent<PlayerShotgunBullet>();
        if (bulletScript != null)
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