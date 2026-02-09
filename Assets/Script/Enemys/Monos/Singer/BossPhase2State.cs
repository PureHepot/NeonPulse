using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase2State : SingerBossBaseState
{
    private enum SubState { LaserAttack, BarrageAndDisturb }
    private SubState currentSubState;
    private float stateTimer;

    private GameObject activeLevelHair;
    private GameObject activeVerticalHair;
    private float laserCycleTimer;
    private bool laserFiring = false;
    private float barrageTimer;

    public BossPhase2State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log(">>> 进入 Phase 2: 激光起手 (无敌) -> 弹幕+干扰 (可受伤)");

        boss.SetPhase1PartsActive(false);
        boss.ClearAllBullets();

        currentSubState = SubState.LaserAttack;
        stateTimer = boss.p2LaserDuration;

        SpawnHairs();
        laserCycleTimer = 0f;
    }

    public override void Update()
    {
        base.Update();

        // 如果 Boss 正在转场，停止逻辑
        if (boss.isTransitioning) return;

        // 判定转阶段: Total - P1 - P2
        if (!boss.hasFinishedPhase3)
        {
            float p2Threshold = boss.MaxTotalHp - boss.hpPhase1 - boss.hpPhase2;

            if (boss.CurrentTotalHp <= p2Threshold)
            {
                Debug.Log($">>> P2 结束 (TotalHP: {boss.CurrentTotalHp}) -> 转 P3");

                // 【核心修复】在触发转场特效前，立即强制回正屏幕！
                // 防止在白屏震动期间屏幕保持颠倒，导致观感错误或逻辑卡死
                boss.StopScreenDisturb();

                boss.TriggerPhaseTransition(boss.Phase3);
                return;
            }
        }

        if (currentSubState == SubState.LaserAttack)
        {
            stateTimer -= Time.deltaTime;
            laserCycleTimer -= Time.deltaTime;

            if (laserCycleTimer <= 0)
            {
                if (!laserFiring)
                {
                    StartTeleport();
                    laserFiring = true;
                    laserCycleTimer = boss.p2StabilizeTime;
                }
                else
                {
                    FireLasers();
                    laserFiring = false;
                    laserCycleTimer = boss.p2PostFireDelay + 1.0f;
                }
            }

            if (stateTimer <= 0) SwitchToBarrageMode();
        }
        else if (currentSubState == SubState.BarrageAndDisturb)
        {
            boss.HandlePhase1HairMovement();
            boss.HandleFaceHover();

            barrageTimer -= Time.deltaTime;
            if (barrageTimer <= 0)
            {
                boss.StartCoroutine(boss.FirePhase1Barrage());
                barrageTimer = boss.p1AttackInterval;
            }
        }
    }

    void SwitchToBarrageMode()
    {
        currentSubState = SubState.BarrageAndDisturb;
        CleanUpHairs();
        boss.SetPhase1PartsActive(true);
        boss.StartScreenDisturb(); // 开始干扰
        boss.ResetHairMovementTime();
        barrageTimer = 1.0f;
    }

    public override void Exit()
    {
        boss.StopPhase1Attack();
        boss.ClearAllBullets();

        // 再次调用以防万一（StopScreenDisturb 内部有空检查，多次调用没问题）
        boss.StopScreenDisturb();

        CleanUpHairs();
        boss.SetPhase1PartsActive(false);

        boss.hasFinishedPhase2 = true;
    }

    void CleanUpHairs() { if (activeLevelHair) Object.Destroy(activeLevelHair); if (activeVerticalHair) Object.Destroy(activeVerticalHair); }
    void SpawnHairs()
    {
        if (boss.levelHairPrefab) { activeLevelHair = Object.Instantiate(boss.levelHairPrefab, boss.transform.position, Quaternion.identity); activeLevelHair.transform.SetParent(boss.transform); }
        if (boss.verticalHairPrefab) { activeVerticalHair = Object.Instantiate(boss.verticalHairPrefab, boss.transform.position, Quaternion.identity); activeVerticalHair.transform.SetParent(boss.transform); }
    }
    void StartTeleport() { if (activeLevelHair) { float x = Random.Range(boss.levelHairXRange.x, boss.levelHairXRange.y); activeLevelHair.transform.position = new Vector3(x, boss.levelHairY, 0); } if (activeVerticalHair) { float y = Random.Range(boss.verticalHairYRange.x, boss.verticalHairYRange.y); activeVerticalHair.transform.position = new Vector3(boss.verticalHairX, y, 0); } }
    void FireLasers() { Shoot(activeLevelHair, Vector3.down); Shoot(activeVerticalHair, Vector3.right); }
    void Shoot(GameObject hair, Vector3 dir) { if (!hair || !boss.laserBeamPrefab) return; Transform fp = hair.transform.Find("RayPoint"); if (!fp) fp = hair.transform.GetComponentInChildren<Transform>().Find("RayPoint"); if (fp) { GameObject l = Object.Instantiate(boss.laserBeamPrefab, fp.position, Quaternion.identity); LaserBeam beam = l.GetComponent<LaserBeam>(); if (beam != null) { beam.laserWidth = boss.p2LaserWidth; beam.Fire(fp.position, dir); } } }
}
