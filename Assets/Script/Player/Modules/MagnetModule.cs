using DG.Tweening;
using UnityEngine;

public class MagnetModule : PlayerModule
{
    private const string MagnetRangeStatId = "magnet.range";
    private const string MagnetControlTimeStatId = "magnet.controltime";
    private const string MagnetCooldownStatId = "magnet.cooldown";

    [Header("Magnet References")]
    public GameObject magnetObject;

    [Header("扇形设置")]
    public float sectorAngle = 90f;

    [Header("基础配置")]
    public float MagnetRange = 8f;
    public float MagnetControlTime = 0.4f;
    public float MagnetCooldown = 2f;

    private float cooldownTimer;
    private float controlTimer;
    private LayerMask enemyLayerMask;
    private Collider2D[] currentEnemyList;
    public GameObject magnetSparks;
    private Vector3 mouseWorldPos;

    protected override void OnInitialize()
    {
        if (magnetObject != null)
            magnetObject.SetActive(false);

        RefreshStats();
        cooldownTimer = 0f;
        controlTimer = 0f;
        enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
    }

    protected override void OnActivate()
    {
        RefreshStats();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || !HasControl)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= DeltaTime;

        if (controlTimer > 0f)
        {
            controlTimer -= DeltaTime;
        }
        else if (currentEnemyList != null)
        {
            DeactivateMagnet();
        }

        if (InputManager.Instance.Mouse1Down() && cooldownTimer <= 0f)
        {
            mouseWorldPos = MUtils.GetMouseWorldPosition();
            StartMagnetSkill();
            KeepMagnetEffect();
            controlTimer = MagnetControlTime;
        }
    }

    private void StartMagnetSkill()
    {
        cooldownTimer = MagnetCooldown;
        if (magnetObject != null)
            magnetObject.SetActive(true);

        currentEnemyList = Physics2D.OverlapCircleAll(mouseWorldPos, MagnetRange, enemyLayerMask);
    }

    private void KeepMagnetEffect()
    {
        if (currentEnemyList == null)
            return;

        magnetSparks = Resources.Load<GameObject>("ParticleSystem/MagnetSparks");
        if (magnetSparks != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(magnetSparks, mouseWorldPos, Quaternion.identity);
            Timer.Register(MagnetControlTime, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj != null ? particleObj.GetComponent<ParticleSystem>() : null;
            if (ps != null)
            {
                var main = ps.main;
                main.startSize = MagnetRange;
                ps.Play();
            }
        }

        foreach (var hit in currentEnemyList)
        {
            if (hit == null)
                continue;

            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            enemy.scared = true;
            PullEnemyToFarEnd(enemy, enemy.transform.position, mouseWorldPos);
        }
    }

    private void PullEnemyToFarEnd(EnemyBase enemy, Vector2 enemyPos, Vector2 farEndPoint)
    {
        enemy.transform.DOMove(
            farEndPoint + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)),
            1f);
    }

    private void DeactivateMagnet()
    {
        if (magnetObject != null)
            magnetObject.SetActive(false);

        if (currentEnemyList != null)
        {
            foreach (var hit in currentEnemyList)
            {
                if (hit == null)
                    continue;

                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                    enemy.scared = false;
            }
        }

        currentEnemyList = null;
    }

    protected override void OnDeactivate()
    {
        DeactivateMagnet();
    }

    private void RefreshStats()
    {
        MagnetRange = GetStat(MagnetRangeStatId, MagnetRange);
        MagnetControlTime = GetStat(MagnetControlTimeStatId, MagnetControlTime);
        MagnetCooldown = GetStat(MagnetCooldownStatId, MagnetCooldown);
    }
}
