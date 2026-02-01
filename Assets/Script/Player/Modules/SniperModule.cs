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

    private bool hasFiredThisCycle = false;

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
        {
            cooldown -= Time.deltaTime;
            hasFiredThisCycle = false;  
        }

        if (Input.GetMouseButton(0) && cooldown <= 0 && !hasFiredThisCycle)
        {
            Fire();
            hasFiredThisCycle = true;
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
        if (fireRate <= 0) fireRate = 1.8f;

        damage = (int)UpgradeManager.Instance.GetStat(ModuleType.Sniper, StatType.SnipeDamage);
        if (damage <= 0) damage = 4;

        penetration = (int)UpgradeManager.Instance.GetStat(ModuleType.Sniper, StatType.SnipePenetration);
        if (penetration <= 0) penetration = 2;
    }

    void Fire()
    {
        if (muzzle == null) return;

        cooldown = fireRate;

        GameObject bullet = ObjectPoolManager.Instance.Get(
            sniperBulletPrefab,
            muzzle.position,
            muzzle.rotation
        );

        var bulletScript = bullet.GetComponent<PlayerSniperBullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
            bulletScript.penetrationCount = penetration;
        }
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