using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase3State : SingerBossBaseState
{
    private GameObject hairTop, hairBottom, hairLeft, hairRight;
    private Coroutine attackRoutine;
    private Coroutine ramRoutine;

    // 状态标记：是否已准备好开始攻击（等待转场结束）
    private bool isReady = false;
    // 状态标记：是否已处理退出逻辑（防止 P3->P4 重复触发）
    private bool handledExit = false;

    public BossPhase3State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> 进入 Phase 3");

        // 1. 初始化标记
        isReady = false;
        handledExit = false;

        // 【保险 1】进场立刻尝试停止干扰
        boss.StopScreenDisturb();

        // 2. 设置部件显隐
        if (boss.battleForm) boss.battleForm.SetActive(true);
        if (boss.speakerLeft) boss.speakerLeft.gameObject.SetActive(false);
        if (boss.speakerRight) boss.speakerRight.gameObject.SetActive(false);
        if (boss.hairLeft) boss.hairLeft.gameObject.SetActive(false);
        if (boss.hairRight) boss.hairRight.gameObject.SetActive(false);

        if (boss.faceAngry)
        {
            boss.faceAngry.gameObject.SetActive(true);
            boss.faceAngry.position = boss.faceTargetPos;
            var dmg = boss.faceAngry.GetComponent<ObjectContactDamage>();
            if (dmg == null) dmg = boss.faceAngry.gameObject.AddComponent<ObjectContactDamage>();
            dmg.damage = 1;
        }

        boss.ClearAllBullets();
        SpawnHairs();

        // 【核心修复】不要立刻启动攻击，而是启动“等待并修正”协程
        // 确保等白屏转场彻底结束，且屏幕回正后，再开始 P3 的疯狗模式
        boss.StartCoroutine(WaitAndStartAttacks());
    }

    IEnumerator WaitAndStartAttacks()
    {
        // 等待 BossSinger 中的 isTransitioning 变为 false
        // 这意味着 1.5s 的震动+白屏动画已经播放完毕
        while (boss.isTransitioning)
        {
            yield return null;
        }

        Debug.Log("P3 转场结束，强制回正屏幕并开始攻击");

        // 【保险 2】在白屏淡出后，再次强制停止屏幕干扰
        // 这能解决“转场期间屏幕卡在缩放/颠倒”的问题
        boss.StopScreenDisturb();

        // 标记就绪
        isReady = true;

        // 启动攻击循环
        attackRoutine = boss.StartCoroutine(RapidFireRoutine());
        ramRoutine = boss.StartCoroutine(RammingRoutine());
    }

    public override void Update()
    {
        base.Update();

        // 如果还没就绪（还在播放进场白屏），不执行任何逻辑
        if (!isReady) return;

        // P3 -> P4 转场检测
        // 如果 Boss 再次进入转场状态（说明血量空了，进 P4），强制停止 P3 的动作
        if (boss.isTransitioning)
        {
            if (!handledExit)
            {
                Debug.Log("P3 检测到 P4 转场信号，强制停止攻击！");
                ForceStopAttacks();
                handledExit = true;
            }
        }
    }

    // 强制停止所有攻击行为（用于 P3->P4 切换瞬间）
    void ForceStopAttacks()
    {
        if (attackRoutine != null) boss.StopCoroutine(attackRoutine);
        if (ramRoutine != null) boss.StopCoroutine(ramRoutine);

        // 杀掉 Face 的所有动画（冲刺、震动），防止它在转场时乱飞
        if (boss.faceAngry) boss.faceAngry.DOKill();
    }

    public override void Exit()
    {
        ForceStopAttacks();

        if (hairTop) Object.Destroy(hairTop);
        if (hairBottom) Object.Destroy(hairBottom);
        if (hairLeft) Object.Destroy(hairLeft);
        if (hairRight) Object.Destroy(hairRight);

        if (boss.faceAngry) boss.faceAngry.DOKill();
    }

    // ... (以下攻击逻辑保持不变) ...

    IEnumerator RammingRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        while (true)
        {
            if (boss.isTransitioning) yield break; // 双重检查
            if (boss.faceAngry == null) yield break;

            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Vector3 targetPos = player ? player.position : Vector3.zero;
            targetPos.x = Mathf.Clamp(targetPos.x, -8f, 8f);
            targetPos.y = Mathf.Clamp(targetPos.y, -4.5f, 4.5f);

            Vector3 dir = (targetPos - boss.faceAngry.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            boss.faceAngry.rotation = Quaternion.Euler(0, 0, angle - 90);

            // 预警
            boss.faceAngry.DOShakeScale(boss.p3ChargeAimTime, 0.2f, 10, 90);
            yield return new WaitForSeconds(boss.p3ChargeAimTime);

            if (boss.isTransitioning) yield break;

            // 冲刺
            Vector3 landPos = targetPos + dir * 2.5f;
            landPos.x = Mathf.Clamp(landPos.x, -8.5f, 8.5f);
            landPos.y = Mathf.Clamp(landPos.y, -4.5f, 4.5f);

            float dist = Vector3.Distance(boss.faceAngry.position, landPos);
            float duration = dist / boss.p3ChargeSpeed;

            boss.faceAngry.DOMove(landPos, duration).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(duration);
            yield return new WaitForSeconds(boss.p3BrakeDuration);
        }
    }

    IEnumerator RapidFireRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        List<GameObject> allHairs = new List<GameObject>();
        while (true)
        {
            if (boss.isTransitioning) yield break;

            allHairs.Clear();
            if (hairTop) allHairs.Add(hairTop);
            if (hairBottom) allHairs.Add(hairBottom);
            if (hairLeft) allHairs.Add(hairLeft);
            if (hairRight) allHairs.Add(hairRight);

            if (allHairs.Count == 0) yield break;
            Shuffle(allHairs);

            int shootCount = (Random.value > 0.4f) ? 1 : 2;
            shootCount = Mathf.Min(shootCount, allHairs.Count);

            for (int i = 0; i < shootCount; i++)
            {
                GameObject currentHair = allHairs[i];
                if (currentHair != null)
                {
                    TeleportOneHairToPlayer(currentHair);
                    Vector3 playerPos = GetPlayerPosition();
                    Vector3 dir = (playerPos - currentHair.transform.position).normalized;
                    float angleOffset = (currentHair == hairTop || currentHair == hairBottom) ? 90f : 0f;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    currentHair.transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
                    FireLaser(currentHair, dir);
                }
            }
            yield return new WaitForSeconds(boss.p3ShootInterval);
        }
    }

    void Shuffle<T>(List<T> list) { int n = list.Count; while (n > 1) { n--; int k = Random.Range(0, n + 1); T value = list[k]; list[k] = list[n]; list[n] = value; } }

    void SpawnHairs()
    {
        if (boss.shortLevelHairPrefab)
        {
            hairTop = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, boss.p3LevelY, 0), Quaternion.identity);
            hairTop.transform.SetParent(boss.transform);
            hairBottom = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, -boss.p3LevelY, 0), Quaternion.identity);
            hairBottom.transform.SetParent(boss.transform);
        }
        if (boss.shortVerticalHairPrefab)
        {
            hairLeft = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(-boss.p3VerticalX, 0, 0), Quaternion.identity);
            hairLeft.transform.SetParent(boss.transform);
            hairRight = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(boss.p3VerticalX, 0, 0), Quaternion.identity);
            hairRight.transform.SetParent(boss.transform);
        }
    }

    Vector3 GetPlayerPosition() { return GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero; }
    void TeleportOneHairToPlayer(GameObject hair) { Vector3 playerPos = GetPlayerPosition(); if (hair == hairTop) hair.transform.position = new Vector3(Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y), boss.p3LevelY, 0); else if (hair == hairBottom) hair.transform.position = new Vector3(Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y), -boss.p3LevelY, 0); else if (hair == hairLeft) hair.transform.position = new Vector3(-boss.p3VerticalX, Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y), 0); else if (hair == hairRight) hair.transform.position = new Vector3(boss.p3VerticalX, Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y), 0); }
    void FireLaser(GameObject hair, Vector3 direction) { if (!hair || !boss.laserBeamPrefab) return; Transform fp = hair.transform.Find("RayPoint"); if (!fp) fp = hair.transform.GetComponentInChildren<Transform>().Find("RayPoint"); if (fp) { GameObject l = Object.Instantiate(boss.laserBeamPrefab, fp.position, Quaternion.identity); LaserBeam beam = l.GetComponent<LaserBeam>(); if (beam) { beam.warningTime = boss.p3AimTime; beam.activeTime = boss.p3LaserActiveTime; beam.laserWidth = boss.p3LaserWidth; beam.Fire(fp.position, direction); } } }
}
