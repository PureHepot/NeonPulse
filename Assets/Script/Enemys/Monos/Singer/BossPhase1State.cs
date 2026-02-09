using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase1State : SingerBossBaseState
{
    private float attackTimer;
    private bool isDeployed = false;
    private bool hasEnteredOnce = false;
    private Coroutine mainAttackRoutine;

    public BossPhase1State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        boss.SetPhase1PartsActive(true);
        if (boss.idleForm) boss.idleForm.SetActive(false);

        boss.FindBulletPoints();

        if (!hasEnteredOnce)
        {
            boss.StartCoroutine(EntranceRoutine());
            hasEnteredOnce = true;
        }
        else
        {
            boss.StartCoroutine(RedeployRoutine());
        }
        attackTimer = 999f; // 等待部署完成
    }

    public override void Update()
    {
        base.Update();
        if (!isDeployed) return;

        boss.HandlePhase1HairMovement();
        boss.HandleFaceHover();

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            mainAttackRoutine = boss.StartCoroutine(boss.FirePhase1Barrage());
            attackTimer = boss.p1AttackInterval;
        }

        // 基于总血量判定转阶段 (Total - P1)
        if (!boss.isTransitioning && !boss.hasFinishedPhase2)
        {
            // 【核心修复】使用 float 类型
            float p1Threshold = boss.MaxTotalHp - boss.hpPhase1;

            if (boss.CurrentTotalHp <= p1Threshold)
            {
                Debug.Log($">>> P1 结束 (TotalHP: {boss.CurrentTotalHp}) -> 转 P2");
                boss.TriggerPhaseTransition(boss.Phase2);
            }
        }
    }

    public override void Exit()
    {
        isDeployed = false;
        boss.SetPhase1PartsActive(false);
        if (mainAttackRoutine != null) boss.StopCoroutine(mainAttackRoutine);
        boss.StopPhase1Attack();
    }

    // ... (EntranceRoutine, RedeployRoutine, FinishDeploy 动画代码保持不变，直接复制即可) ...
    IEnumerator EntranceRoutine() { float dur = boss.deployDuration; Sequence seq = DOTween.Sequence(); if (boss.idleForm) boss.idleForm.SetActive(true); boss.SetPhase1PartsActive(false); yield return new WaitForSeconds(1.0f); if (boss.idleForm) boss.idleForm.SetActive(false); boss.SetPhase1PartsActive(true); yield return new WaitForSeconds(1.0f); if (boss.faceAngry) seq.Join(boss.faceAngry.DOMove(boss.faceTargetPos, dur).SetEase(Ease.OutBack)); if (boss.speakerLeft) seq.Join(boss.speakerLeft.DOMove(boss.speakerLeftTargetPos, dur).SetEase(Ease.OutBack)); if (boss.speakerRight) seq.Join(boss.speakerRight.DOMove(boss.speakerRightTargetPos, dur).SetEase(Ease.OutBack)); float centerY = boss.HairCenterY; if (boss.hairLeft) { boss.hairLeft.rotation = Quaternion.identity; Vector3 targetPos = new Vector3(boss.hairLeftTargetPos.x, centerY, boss.hairLeftTargetPos.z); seq.Join(boss.hairLeft.DOMove(targetPos, dur).SetEase(Ease.OutBack)); seq.Join(boss.hairLeft.DORotate(boss.hairLeftTargetRot, dur).SetEase(Ease.OutBack)); } if (boss.hairRight) { boss.hairRight.rotation = Quaternion.identity; Vector3 targetPos = new Vector3(boss.hairRightTargetPos.x, centerY, boss.hairRightTargetPos.z); seq.Join(boss.hairRight.DOMove(targetPos, dur).SetEase(Ease.OutBack)); seq.Join(boss.hairRight.DORotate(boss.hairRightTargetRot, dur).SetEase(Ease.OutBack)); } yield return new WaitForSeconds(dur); FinishDeploy(); }
    IEnumerator RedeployRoutine() { float dur = boss.deployDuration; Sequence seq = DOTween.Sequence(); if (boss.idleForm) boss.idleForm.SetActive(false); boss.SetPhase1PartsActive(true); if (boss.faceAngry) seq.Join(boss.faceAngry.DOMove(boss.faceTargetPos, dur).SetEase(Ease.OutBack)); if (boss.speakerLeft) seq.Join(boss.speakerLeft.DOMove(boss.speakerLeftTargetPos, dur).SetEase(Ease.OutBack)); if (boss.speakerRight) seq.Join(boss.speakerRight.DOMove(boss.speakerRightTargetPos, dur).SetEase(Ease.OutBack)); float centerY = boss.HairCenterY; if (boss.hairLeft) { boss.hairLeft.rotation = Quaternion.identity; Vector3 targetPos = new Vector3(boss.hairLeftTargetPos.x, centerY, boss.hairLeftTargetPos.z); seq.Join(boss.hairLeft.DOMove(targetPos, dur).SetEase(Ease.OutBack)); seq.Join(boss.hairLeft.DORotate(boss.hairLeftTargetRot, dur).SetEase(Ease.OutBack)); } if (boss.hairRight) { boss.hairRight.rotation = Quaternion.identity; Vector3 targetPos = new Vector3(boss.hairRightTargetPos.x, centerY, boss.hairRightTargetPos.z); seq.Join(boss.hairRight.DOMove(targetPos, dur).SetEase(Ease.OutBack)); seq.Join(boss.hairRight.DORotate(boss.hairRightTargetRot, dur).SetEase(Ease.OutBack)); } yield return new WaitForSeconds(dur); FinishDeploy(); }
    void FinishDeploy() { if (boss.hairLeft) { boss.savedHairLeftX = boss.hairLeftTargetPos.x; boss.savedHairLeftZ = boss.hairLeftTargetPos.z; } if (boss.hairRight) { boss.savedHairRightX = boss.hairRightTargetPos.x; boss.savedHairRightZ = boss.hairRightTargetPos.z; } boss.ResetHairMovementTime(); isDeployed = true; attackTimer = 0f; }
}
