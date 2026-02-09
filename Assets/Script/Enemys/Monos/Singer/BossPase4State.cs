using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase4State : SingerBossBaseState
{
    private float stateTimer;
    private GameObject p4SpeakerLeft, p4SpeakerRight;
    private GameObject hairTop, hairBottom, hairLeft, hairRight;

    // 移除了 firingOrder，改用动态列表
    private Coroutine laserRoutine;
    private Coroutine barrageRoutineLeft, barrageRoutineRight;

    public BossPhase4State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> <color=red>进入 Phase 4: 最终处决</color>");

        boss.isInFinalPhase = true;
        boss.SetPhase4PartsActive();
        SpawnLoudspeakers();
        SpawnHairsSafe();

        stateTimer = boss.p4Duration;

        laserRoutine = boss.StartCoroutine(RapidFireRoutine());

        List<Transform> leftPoints = new List<Transform>();
        List<Transform> rightPoints = new List<Transform>();
        FindPointsOnSpeaker(p4SpeakerLeft, leftPoints);
        FindPointsOnSpeaker(p4SpeakerRight, rightPoints);

        if (leftPoints.Count > 0)
            barrageRoutineLeft = boss.StartCoroutine(boss.FireSequenceRoutine(leftPoints, new Vector3(0.5f, -1f, 0)));
        if (rightPoints.Count > 0)
            barrageRoutineRight = boss.StartCoroutine(boss.FireSequenceRoutine(rightPoints, new Vector3(-0.5f, -1f, 0)));
    }

    public override void Update()
    {
        base.Update();
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            Debug.Log("Boss 最终阶段结束，彻底死亡");
            boss.DieForReal();
        }
    }

    public override void Exit()
    {
        if (laserRoutine != null) boss.StopCoroutine(laserRoutine);
        if (barrageRoutineLeft != null) boss.StopCoroutine(barrageRoutineLeft);
        if (barrageRoutineRight != null) boss.StopCoroutine(barrageRoutineRight);

        if (p4SpeakerLeft) Object.Destroy(p4SpeakerLeft);
        if (p4SpeakerRight) Object.Destroy(p4SpeakerRight);
        if (hairTop) Object.Destroy(hairTop);
        if (hairBottom) Object.Destroy(hairBottom);
        if (hairLeft) Object.Destroy(hairLeft);
        if (hairRight) Object.Destroy(hairRight);
    }

    // 【修改】P4 随机射击逻辑
    IEnumerator RapidFireRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 进场缓冲
        List<GameObject> allHairs = new List<GameObject>();

        while (true)
        {
            allHairs.Clear();
            if (hairTop) allHairs.Add(hairTop);
            if (hairBottom) allHairs.Add(hairBottom);
            if (hairLeft) allHairs.Add(hairLeft);
            if (hairRight) allHairs.Add(hairRight);

            if (allHairs.Count == 0) yield break;

            // 1. 洗牌
            Shuffle(allHairs);

            // 2. 决定数量 (P4 节奏可以比 P3 更快更狠，这里设为 1到3根)
            // 50% 概率射1根，30% 概率射2根，20% 概率射3根
            float r = Random.value;
            int shootCount = 1;
            if (r > 0.5f) shootCount = 2;
            if (r > 0.8f) shootCount = 3;

            shootCount = Mathf.Min(shootCount, allHairs.Count);

            // 3. 发射
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

    // 洗牌算法 (直接复制 P3 的)
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

    void SpawnLoudspeakers()
    {
        Vector3 leftPos = new Vector3(-8.3f, 4, 0);
        Vector3 rightPos = new Vector3(8.3f, 4, 0);
        if (boss.loudspeakerLeftPrefab) p4SpeakerLeft = Object.Instantiate(boss.loudspeakerLeftPrefab, leftPos, Quaternion.identity);
        if (boss.loudspeakerRightPrefab) p4SpeakerRight = Object.Instantiate(boss.loudspeakerRightPrefab, rightPos, Quaternion.identity);
    }

    void FindPointsOnSpeaker(GameObject speaker, List<Transform> list)
    {
        if (speaker == null) return;
        for (int i = 1; i <= boss.maxSearchIndex; i++)
        {
            Transform t = boss.FindDeepChild(speaker.transform, "BulletPoint" + i);
            if (t != null) list.Add(t);
        }
        list.Sort((a, b) => string.Compare(a.name, b.name));
    }

    void SpawnHairsSafe()
    {
        GameObject sLevel = boss.shortLevelHairPrefab ? boss.shortLevelHairPrefab : boss.levelHairPrefab;
        GameObject sVert = boss.shortVerticalHairPrefab ? boss.shortVerticalHairPrefab : boss.verticalHairPrefab;
        if (sLevel == null || sVert == null) return;

        hairTop = Object.Instantiate(sLevel, new Vector3(0, boss.p3LevelY, 0), Quaternion.identity);
        hairBottom = Object.Instantiate(sLevel, new Vector3(0, -boss.p3LevelY, 0), Quaternion.identity);
        hairLeft = Object.Instantiate(sVert, new Vector3(-boss.p3VerticalX, 0, 0), Quaternion.identity);
        hairRight = Object.Instantiate(sVert, new Vector3(boss.p3VerticalX, 0, 0), Quaternion.identity);
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
