using UnityEngine;

public class SniperModule : PlayerModule
{
    private const string SnipeFireRateStatId = "weapon.snipefirerate";
    private const string SnipeDamageStatId = "weapon.snipedamage";
    private const string SnipePenetrationStatId = "weapon.snipepenetration";

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
    private bool hasFiredThisCycle;

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
        {
            cooldown -= DeltaTime;
            hasFiredThisCycle = false;
        }

        if (HasControl && Input.GetMouseButton(0) && cooldown <= 0f && !hasFiredThisCycle)
        {
            Fire();
            hasFiredThisCycle = true;
        }
    }

    private void RecalculateStats()
    {
        fireRate = GetStat(SnipeFireRateStatId, 1.8f);
        damage = Mathf.RoundToInt(GetStat(SnipeDamageStatId, 4f));
        penetration = Mathf.Max(1, Mathf.RoundToInt(GetStat(SnipePenetrationStatId, 2f)));
    }

    private void Fire()
    {
        if (muzzle == null)
            return;

        cooldown = fireRate;
        AudioManager.Instance.PlayEffect("SniperShoot");
        GameObject bullet = ObjectPoolManager.Instance.Get(sniperBulletPrefab, muzzle.position, muzzle.rotation);
        var bulletScript = bullet.GetComponent<PlayerSniperBullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
            bulletScript.penetrationCount = penetration;
        }
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
}
