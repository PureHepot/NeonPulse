using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase3State : SingerBossBaseState
{
    // 4 个短头发实例
    private GameObject hairTop;
    private GameObject hairBottom;
    private GameObject hairLeft;
    private GameObject hairRight;

    // 顺序列表
    private List<GameObject> firingOrder = new List<GameObject>();

    private float stateTimer;
    private Coroutine attackRoutine;

    public BossPhase3State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> <color=red>进入 Phase 3: 追踪瞬移狙击模式</color>");

        // 1. 清理场面
        boss.SetPhase1PartsActive(false);
        boss.ClearAllBullets();

        // 2. 生成 4 个头发
        SpawnHairs();

        // 3. 初始化计时器
        stateTimer = boss.p3Duration;

        // 4. 启动疯狂射击循环
        attackRoutine = boss.StartCoroutine(RapidFireRoutine());
    }

    public override void Update()
    {
        base.Update();

        // 检查总时间
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            Debug.Log("P3 结束，Boss 进入最终 P1 循环");
            boss.hasFinishedPhase3 = true;
            boss.TransitionToState(boss.Phase1);
        }
    }

    public override void Exit()
    {
        if (attackRoutine != null) boss.StopCoroutine(attackRoutine);

        if (hairTop) Object.Destroy(hairTop);
        if (hairBottom) Object.Destroy(hairBottom);
        if (hairLeft) Object.Destroy(hairLeft);
        if (hairRight) Object.Destroy(hairRight);
    }

    void SpawnHairs()
    {
        // 初始生成 (位置稍后会在攻击时更新)
        if (boss.shortLevelHairPrefab)
            hairTop = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, boss.p3LevelY, 0), Quaternion.identity);

        if (boss.shortLevelHairPrefab)
            hairBottom = Object.Instantiate(boss.shortLevelHairPrefab, new Vector3(0, -boss.p3LevelY, 0), Quaternion.identity);

        if (boss.shortVerticalHairPrefab)
            hairLeft = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(-boss.p3VerticalX, 0, 0), Quaternion.identity);

        if (boss.shortVerticalHairPrefab)
            hairRight = Object.Instantiate(boss.shortVerticalHairPrefab, new Vector3(boss.p3VerticalX, 0, 0), Quaternion.identity);

        // 设置射击顺序：左 -> 下 -> 右 -> 上
        firingOrder.Clear();
        if (hairLeft) firingOrder.Add(hairLeft);
        if (hairBottom) firingOrder.Add(hairBottom);
        if (hairRight) firingOrder.Add(hairRight);
        if (hairTop) firingOrder.Add(hairTop);
    }

    IEnumerator RapidFireRoutine()
    {
        int currentIndex = 0;
        yield return new WaitForSeconds(1.0f); // 进场缓冲

        while (true)
        {
            if (firingOrder.Count == 0) yield break;

            // 1. 获取当前要开火的头发
            GameObject currentHair = firingOrder[currentIndex];

            if (currentHair != null)
            {
                // 【核心逻辑】根据玩家位置瞬移
                TeleportOneHairToPlayer(currentHair);

                // 2. 瞄准玩家
                Vector3 playerPos = GetPlayerPosition();
                Vector3 hairPos = currentHair.transform.position;
                Vector3 dirToPlayer = (playerPos - hairPos).normalized;

                // 旋转朝向
                float angleOffset = 0f;
                // Level Hair (上下) 贴图通常需要旋转 90 度
                if (currentHair == hairTop || currentHair == hairBottom) angleOffset = 90f;
                else angleOffset = 0f;

                float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                currentHair.transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);

                // 3. 发射
                FireLaser(currentHair, dirToPlayer);
            }

            // 4. 切换下一个
            currentIndex++;
            if (currentIndex >= firingOrder.Count) currentIndex = 0;

            // 5. 等待下一发
            yield return new WaitForSeconds(boss.p3ShootInterval);
        }
    }

    // 获取玩家位置的辅助方法
    Vector3 GetPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position : Vector3.zero;
    }

    // 【核心修改】追踪瞬移逻辑
    void TeleportOneHairToPlayer(GameObject hair)
    {
        Vector3 playerPos = GetPlayerPosition();

        if (hair == hairTop)
        {
            // 上方头发：Y固定在顶部，X 追踪玩家 (限制在屏幕范围内)
            float targetX = Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y);
            hair.transform.position = new Vector3(targetX, boss.p3LevelY, 0);
        }
        else if (hair == hairBottom)
        {
            // 下方头发：Y固定在底部，X 追踪玩家
            float targetX = Mathf.Clamp(playerPos.x, boss.levelHairXRange.x, boss.levelHairXRange.y);
            hair.transform.position = new Vector3(targetX, -boss.p3LevelY, 0);
        }
        else if (hair == hairLeft)
        {
            // 左侧头发：X固定在左侧，Y 追踪玩家 (限制在屏幕范围内)
            float targetY = Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y);
            hair.transform.position = new Vector3(-boss.p3VerticalX, targetY, 0);
        }
        else if (hair == hairRight)
        {
            // 右侧头发：X固定在右侧，Y 追踪玩家
            float targetY = Mathf.Clamp(playerPos.y, boss.verticalHairYRange.x, boss.verticalHairYRange.y);
            hair.transform.position = new Vector3(boss.p3VerticalX, targetY, 0);
        }
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
