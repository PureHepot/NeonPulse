using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase3State : SingerBossBaseState
{
    private GameObject hairTop, hairBottom, hairLeft, hairRight;
    // 移除了 firingOrder，改为每次动态构建列表
    private Coroutine attackRoutine;
    private Coroutine ramRoutine;

    public BossPhase3State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> 进入 Phase 3: 随机交叉激光 + 疯牛冲撞");

        boss.StopScreenDisturb();

        // 1. 设置部件
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

        // 2. 启动循环
        attackRoutine = boss.StartCoroutine(RapidFireRoutine());
        ramRoutine = boss.StartCoroutine(RammingRoutine());
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        if (attackRoutine != null) boss.StopCoroutine(attackRoutine);
        if (ramRoutine != null) boss.StopCoroutine(ramRoutine);

        if (hairTop) Object.Destroy(hairTop);
        if (hairBottom) Object.Destroy(hairBottom);
        if (hairLeft) Object.Destroy(hairLeft);
        if (hairRight) Object.Destroy(hairRight);

        if (boss.faceAngry) boss.faceAngry.DOKill();
    }

    IEnumerator RammingRoutine()
    {
        // ... (保持之前的疯牛冲撞逻辑不变，完全复用) ...
        yield return new WaitForSeconds(1.0f);
        while (true)
        {
            if (boss.faceAngry == null) yield break;
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Vector3 targetPos = player ? player.position : Vector3.zero;
            targetPos.x = Mathf.Clamp(targetPos.x, -8f, 8f);
            targetPos.y = Mathf.Clamp(targetPos.y, -4.5f, 4.5f);

            Vector3 dir = (targetPos - boss.faceAngry.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            boss.faceAngry.rotation = Quaternion.Euler(0, 0, angle - 90);

            boss.faceAngry.DOShakeScale(boss.p3ChargeAimTime, 0.2f, 10, 90);
            yield return new WaitForSeconds(boss.p3ChargeAimTime);

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

    // 【修改】新的随机射击逻辑
    IEnumerator RapidFireRoutine()
    {
        yield return new WaitForSeconds(1.0f); // 进场缓冲

        // 构建可用头发池
        List<GameObject> allHairs = new List<GameObject>();

        while (true)
        {
            // 1. 重置列表
            allHairs.Clear();
            if (hairTop) allHairs.Add(hairTop);
            if (hairBottom) allHairs.Add(hairBottom);
            if (hairLeft) allHairs.Add(hairLeft);
            if (hairRight) allHairs.Add(hairRight);

            if (allHairs.Count == 0) yield break;

            // 2. 随机洗牌 (Shuffle)
            Shuffle(allHairs);

            // 3. 决定本次发射几根 (1根 或 2根，增加不确定性)
            // 60% 几率单发，40% 几率双发 (你可以调整这个概率)
            int shootCount = (Random.value > 0.4f) ? 1 : 2;

            // 确保不超出剩余头发数量
            shootCount = Mathf.Min(shootCount, allHairs.Count);

            // 4. 执行发射
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

            // 5. 等待间隔
            yield return new WaitForSeconds(boss.p3ShootInterval);
        }
    }

    // Fisher-Yates 洗牌算法
    void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    void SpawnHairs()
    {
        if (boss.shortLevelHairPrefab) hairTop = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, boss.p3LevelY, 0), Quaternion.identity);
        if (boss.shortLevelHairPrefab) hairBottom = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, -boss.p3LevelY, 0), Quaternion.identity);
        if (boss.shortVerticalHairPrefab) hairLeft = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(-boss.p3VerticalX, 0, 0), Quaternion.identity);
        if (boss.shortVerticalHairPrefab) hairRight = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(boss.p3VerticalX, 0, 0), Quaternion.identity);
    }

    Vector3 GetPlayerPosition() { return GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero; }
    void TeleportOneHairToPlayer(GameObject hair)
    {
        Vector3 playerPos = GetPlayerPosition();
        if (hair == hairTop) hair.transform.position = new Vector3(Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y), boss.p3LevelY, 0);
        else if (hair == hairBottom) hair.transform.position = new Vector3(Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y), -boss.p3LevelY, 0);
        else if (hair == hairLeft) hair.transform.position = new Vector3(-boss.p3VerticalX, Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y), 0);
        else if (hair == hairRight) hair.transform.position = new Vector3(boss.p3VerticalX, Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y), 0);
    }
    void FireLaser(GameObject hair, Vector3 direction)
    {
        if (!hair || !boss.laserBeamPrefab) return;
        Transform fp = hair.transform.Find("RayPoint");
        if (!fp) fp = hair.transform.GetComponentInChildren<Transform>().Find("RayPoint");
        if (fp)
        {
            GameObject l = Object.Instantiate(boss.laserBeamPrefab, fp.position, Quaternion.identity);
            LaserBeam beam = l.GetComponent<LaserBeam>();
            if (beam)
            {
                beam.warningTime = boss.p3AimTime;
                beam.activeTime = boss.p3LaserActiveTime;
                beam.laserWidth = boss.p3LaserWidth;
                beam.Fire(fp.position, direction);
            }
        }
    }
}
