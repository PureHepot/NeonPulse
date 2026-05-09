using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MagnetModule : PlayerModule
{
    [Header("Magnet References")]
    public GameObject magnetObject;

    [Header("扇形设置")]
    public float sectorAngle = 90f;

    [Header("固定超大吸力(不升级)")]
    private readonly float FixedPullPower = 15f;

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

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        if (magnetObject) magnetObject.SetActive(false);

        cooldownTimer = 0f;
        controlTimer = 0f;
        enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
    }

    public override void OnActivate()
    {
        base.OnActivate();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || player.isPreview) return;

        // 冷却计时
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        if (controlTimer > 0)
        {
            controlTimer -= Time.deltaTime;
        }
        else if (controlTimer <= 0 && currentEnemyList != null)
        {
            DeactivateMagnet();
        }
        // 鼠标按下瞬间触发瞬发
        if (InputManager.Instance.Mouse1Down() && cooldownTimer <= 0)
        {
            mouseWorldPos = MUtils.GetMouseWorldPosition();

            StartMagnetSkill();
            KeepMagnetEffect();
            controlTimer = MagnetControlTime;
        }
    }

    void StartMagnetSkill()
    {
        cooldownTimer = MagnetCooldown;
        if (magnetObject) magnetObject.SetActive(true);
        currentEnemyList = Physics2D.OverlapCircleAll(mouseWorldPos, MagnetRange, enemyLayerMask);
    }

    void KeepMagnetEffect()
    {
        if (currentEnemyList == null) return;

        Vector3 mouseWorldPos = MUtils.GetMouseWorldPosition();
        Vector2 skillDir = (mouseWorldPos - player.transform.position).normalized;
        Vector2 playerPos = player.transform.position;
        magnetSparks = Resources.Load<GameObject>("ParticleSystem/MagnetSparks");
        GameObject particleObj = ObjectPoolManager.Instance.Get(magnetSparks, mouseWorldPos, Quaternion.identity);
        Timer.Register(MagnetControlTime, onComplete: () =>
        {
            ObjectPoolManager.Instance.Return(particleObj);
        });
        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;

            main.startSize= MagnetRange;

            ps.Play();
        }

        foreach (var hit in currentEnemyList)
        {
            if (hit == null) continue;
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;
            enemy.scared = true;
            // 传入远端目标点
            PullEnemyToFarEnd(enemy, enemy.transform.position, mouseWorldPos);
        }
    }


    void PullEnemyToFarEnd(EnemyBase enemy, Vector2 enemyPos, Vector2 farEndPoint)
    {
        Vector2 moveDir = (farEndPoint - enemyPos).normalized;
        enemy.transform.DOMove(farEndPoint+new Vector2(Random.Range(-0.5f,0.5f),Random.Range(-0.5f,0.5f)), 1f);
    }

    void DeactivateMagnet()
    {
        if (magnetObject) magnetObject.SetActive(false);

        if (currentEnemyList != null)
        {
            foreach (var hit in currentEnemyList)
            {
                if (hit == null) continue;
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null) enemy.scared = false;
            }
        }
        currentEnemyList = null;
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        DeactivateMagnet();
    }

    // 升级只改：范围 / 控制时长 / 冷却
    public override void UpgradeModule(ModuleType ModuleType, StatType statType)
    {
        base.UpgradeModule(ModuleType, statType);
        if (ModuleType == ModuleType.Magnet)
        {
            switch (statType)
            {
                case StatType.MagnetRange:
                    MagnetRange = UpgradeManager.Instance.GetStat(ModuleType, statType);
                    break;
                case StatType.MagnetControlTime:
                    MagnetControlTime = UpgradeManager.Instance.GetStat(ModuleType, statType);
                    break;
                case StatType.MagnetCooldown:
                    MagnetCooldown = UpgradeManager.Instance.GetStat(ModuleType, statType);
                    break;
            }
        }
    }
}