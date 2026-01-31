using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase2State : SingerBossBaseState
{
    private GameObject activeLevelHair;
    private GameObject activeVerticalHair;

    // 大阶段控制
    private enum StageState { Entering, Looping, Exiting }
    private StageState currentStage;
    private float stageTimer;

    // 攻击循环控制 (在 Looping 阶段使用)
    private enum AttackCycle { Teleporting, Stabilizing, Firing, Recovering }
    private AttackCycle currentCycle;
    private float cycleTimer;

    public BossPhase2State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> 进入 Phase 2流程");

        // 1. 隐藏 P1 部件
        boss.SetPhase1PartsActive(false);

        // 2. 清理全屏弹幕
        boss.ClearAllBullets();

        // 3. 进入 "Entering" 缓冲阶段
        currentStage = StageState.Entering;
        stageTimer = boss.p2EnterDelay; // 等待缓冲时间
    }

    public override void Update()
    {
        base.Update();

        switch (currentStage)
        {
            // === 阶段 A: 进场缓冲 ===
            case StageState.Entering:
                stageTimer -= Time.deltaTime;
                if (stageTimer <= 0)
                {
                    // 缓冲结束，生成头发，开始攻击循环
                    SpawnHairs();
                    StartTeleport(); // 第一次瞬移

                    currentStage = StageState.Looping;
                    stageTimer = boss.p2AttackDuration; // 设置攻击总时长
                }
                break;

            // === 阶段 B: 攻击循环 ===
            case StageState.Looping:
                // 1. 检查总时间
                stageTimer -= Time.deltaTime;
                if (stageTimer <= 0)
                {
                    // 时间到，进入退场阶段
                    PrepareExit();
                    return;
                }

                // 2. 执行瞬移射击循环
                UpdateAttackCycle();
                break;

            // === 阶段 C: 退场缓冲 ===
            case StageState.Exiting:
                stageTimer -= Time.deltaTime;
                if (stageTimer <= 0)
                {
                    // 彻底结束，销毁头发，切回 P1
                    CleanUpHairs();
                    boss.hasFinishedPhase2 = true;
                    boss.TransitionToState(boss.Phase1);
                }
                break;
        }
    }

    // --- 攻击循环逻辑 ---
    void UpdateAttackCycle()
    {
        cycleTimer -= Time.deltaTime;

        switch (currentCycle)
        {
            case AttackCycle.Teleporting:
                // 瞬移已在 StartTeleport 执行，直接切到稳定状态
                currentCycle = AttackCycle.Stabilizing;
                cycleTimer = boss.p2StabilizeTime; // 停顿，让玩家看清位置
                break;

            case AttackCycle.Stabilizing:
                if (cycleTimer <= 0)
                {
                    FireLasers();
                    currentCycle = AttackCycle.Firing;
                    // 等待激光生命周期 (假设预警1s + 伤害0.5s = 1.5s)
                    // 额外加一点点缓冲防止瞬移穿帮
                    cycleTimer = 1.6f;
                }
                break;

            case AttackCycle.Firing:
                if (cycleTimer <= 0)
                {
                    currentCycle = AttackCycle.Recovering;
                    cycleTimer = boss.p2PostFireDelay; // 射完歇一会儿
                }
                break;

            case AttackCycle.Recovering:
                if (cycleTimer <= 0)
                {
                    StartTeleport(); // 下一次循环
                }
                break;
        }
    }

    // --- 辅助方法 ---

    void PrepareExit()
    {
        Debug.Log("P2 攻击结束，进入退场缓冲...");
        currentStage = StageState.Exiting;

        // 销毁头发 (或者你可以选择保留头发但不发射，看你想怎么演出)
        // 这里建议先保留头发，等退场缓冲结束再销毁，避免突兀消失
        // 如果想让它立刻消失，就调用 CleanUpHairs();

        // 设置等待时间 (等待最后的激光消失 + 额外的发呆时间)
        stageTimer = boss.p2ExitDelay;
    }

    void CleanUpHairs()
    {
        if (activeLevelHair) Object.Destroy(activeLevelHair);
        if (activeVerticalHair) Object.Destroy(activeVerticalHair);
    }

    public override void Exit()
    {
        CleanUpHairs();
    }

    void SpawnHairs()
    {
        if (boss.levelHairPrefab) activeLevelHair = Object.Instantiate(boss.levelHairPrefab, boss.transform.position, Quaternion.identity);
        if (boss.verticalHairPrefab) activeVerticalHair = Object.Instantiate(boss.verticalHairPrefab, boss.transform.position, Quaternion.identity);
    }

    void StartTeleport()
    {
        currentCycle = AttackCycle.Teleporting;

        if (activeLevelHair)
        {
            float x = Random.Range(boss.levelHairXRange.x, boss.levelHairXRange.y);
            activeLevelHair.transform.position = new Vector3(x, boss.levelHairY, 0);
        }
        if (activeVerticalHair)
        {
            float y = Random.Range(boss.verticalHairYRange.x, boss.verticalHairYRange.y);
            activeVerticalHair.transform.position = new Vector3(boss.verticalHairX, y, 0);
        }
    }

    void FireLasers()
    {
        Shoot(activeLevelHair, Vector3.down);
        Shoot(activeVerticalHair, Vector3.right);
    }

    void Shoot(GameObject hair, Vector3 dir)
    {
        if (!hair || !boss.laserBeamPrefab) return;

        Transform fp = hair.transform.Find("RayPoint");
        if (!fp) fp = hair.transform.GetComponentInChildren<Transform>().Find("RayPoint");

        if (fp)
        {
            GameObject l = Object.Instantiate(boss.laserBeamPrefab, fp.position, Quaternion.identity);

            LaserBeam beam = l.GetComponent<LaserBeam>();
            if (beam != null)
            {
                // 【新增】应用 BossSinger 设置的 P2 宽度
                beam.laserWidth = boss.p2LaserWidth;

                beam.Fire(fp.position, dir);
            }
        }
    }
}
