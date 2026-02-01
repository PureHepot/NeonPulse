using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StateLaser : BossState
{
    private bool isFiring = false;
    private LineRenderer laserLine;
    private float damageTimer = 0f;
    private float spawnTimer = 0f;

    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();

    public StateLaser(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();
        boss.transform.Find("Symbol").gameObject.SetActive(true);
        boss.CleanMinionList();

        if (boss.laserBeamObj)
        {
            laserLine = boss.laserBeamObj.GetComponent<LineRenderer>();
            boss.laserBeamObj.SetActive(false);
        }

        boss.StartCoroutine(LaserLoop());
    }

    public override void OnUpdate()
    {
        PerformFastHover();

        if (isFiring)
        {
            HandleLaserLogic();
        }

        HandleLaserPhaseSpawning();
    }

    // 激光模式专用的快速移动
    private void PerformFastHover()
    {
        // 可以在这里改变频率，让它动得比 Idle 快
        float xFreq = 2.0f;
        float yFreq = 3.0f;
        float xDist = 9.0f;

        float targetX = boss.HoverAnchorPos.x + Mathf.Cos(Time.time * xFreq) * xDist;
        float targetY = boss.HoverAnchorPos.y + Mathf.Sin(Time.time * yFreq) * 0.5f; // 上下幅度小一点
        Vector2 targetPos = new Vector2(targetX, targetY);

        // 使用 Boss 的 CurrentVelocity 保持惯性
        Vector2 nextPos = Vector2.SmoothDamp(boss.transform.position, targetPos, ref boss.CurrentVelocity, 0.5f, 10f);
        boss.GetComponent<Rigidbody2D>().MovePosition(nextPos);
    }

    private void HandleLaserLogic()
    {
        if (!boss.laserBeamObj) return;

        Vector3 startPos = boss.transform.position;
        Vector2 fireDir = Vector2.down;

        // 视觉：强制画到最大距离 (穿透效果，不被阻挡)
        if (laserLine)
        {
            // 起点
            laserLine.SetPosition(0, startPos);

            // 终点：直接延伸到最大射程
            Vector3 endPos = startPos + (Vector3)(fireDir * boss.laserMaxDist);
            laserLine.SetPosition(1, endPos);
        }

        damageTimer += Time.deltaTime;
        if (damageTimer >= boss.laserTickRate)
        {
            hitObjects.Clear();

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                startPos,
                new Vector2(boss.laserWidth, 0.1f),
                0f,
                fireDir,
                boss.laserMaxDist,
                boss.laserHitLayer
            );

            foreach (var hit in hits)
            {
                GameObject hitObj = hit.collider.gameObject;

                // 简单的去重：防止同一个怪身上的身体和头两个Collider导致受双倍伤害
                if (hitObjects.Contains(hitObj)) continue;
                hitObjects.Add(hitObj);

                // --- 情况 A: 打中玩家 (使用 HealthModule) ---
                if (hitObj.CompareTag("Player"))
                {
                    // 尝试在自身或父物体上找 HealthModule
                    var health = hitObj.GetComponentInChildren<HealthModule>();
                    if (health == null) health = hitObj.GetComponentInParent<HealthModule>();

                    if (health != null)
                    {
                        // 传入 boss.transform 作为攻击者，方便计算击退方向
                        health.TakeDamage(boss.laserDamage, boss.transform);
                    }
                }
                // --- 情况 B: 打中其他东西 (使用通用 IDamageable) ---
                else
                {
                    IDamageable target = hitObj.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        // 怪物受击
                        target.TakeDamage(boss.laserDamage, hit.point, Vector2.down);
                    }
                }
            }

            // 重置计时器
            damageTimer = 0f;
        }
    }
    private void HandleLaserPhaseSpawning()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= boss.laserModeSpawnInterval)
        {
            spawnTimer = 0f;

            // 依然检查翅膀是否完好，如果完好就疯狂出怪
            if (boss.leftWing != null && !boss.leftWing.IsBroken)
                boss.SpawnSingleMinion(boss.leftSpawnPoint);

            if (boss.rightWing != null && !boss.rightWing.IsBroken)
                boss.SpawnSingleMinion(boss.rightSpawnPoint);
        }
    }

    IEnumerator LaserLoop()
    {
        while (true)
        {
            isFiring = false;
            if (boss.laserBeamObj) boss.laserBeamObj.SetActive(false);

            yield return new WaitForSeconds(2.0f);

            boss.transform.DOShakePosition(1.0f, 0.5f);
            yield return new WaitForSeconds(1.0f);

            if (boss.laserBeamObj)
            {
                if (laserLine == null) laserLine = boss.laserBeamObj.GetComponent<LineRenderer>();

                if (laserLine != null)
                {
                    Vector3 startPos = boss.transform.position;
                    
                    laserLine.SetPosition(0, startPos);
                    laserLine.SetPosition(1, startPos + (Vector3)(Vector2.down * boss.laserMaxDist));
                }

                boss.laserBeamObj.SetActive(true);
            }

            isFiring = true;
            CameraManager.Instance.Shake("Explosion");

            yield return new WaitForSeconds(3.0f);

            if (boss.laserBeamObj) boss.laserBeamObj.SetActive(false);
            isFiring = false;
        }
    }
}
