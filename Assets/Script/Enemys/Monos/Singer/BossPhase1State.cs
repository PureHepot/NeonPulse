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

        attackTimer = 999f;
    }

    public override void Update()
    {
        base.Update();

        // 必须等动画播完 (isDeployed = true) 才开始逻辑
        if (!isDeployed) return;

        boss.HandlePhase1HairMovement();
        boss.HandleFaceHover();

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            mainAttackRoutine = boss.StartCoroutine(boss.FirePhase1Barrage());
            attackTimer = boss.p1AttackInterval;
        }

        // 触发 P2 转场特效
        if (!boss.isTransitioning && !boss.hasFinishedPhase2 && boss.HpPercent <= 0.666f && boss.HpPercent > 0.334f)
        {
            Debug.Log(">>> P1 触发转场：进入 Phase 2 (抖动+白屏)");
            boss.TriggerPhaseTransition(boss.Phase2);
        }
    }

    public override void Exit()
    {
        isDeployed = false;
        boss.SetPhase1PartsActive(false);
        if (mainAttackRoutine != null) boss.StopCoroutine(mainAttackRoutine);
        boss.StopPhase1Attack();
    }

    IEnumerator EntranceRoutine()
    {
        float dur = boss.deployDuration;
        Sequence seq = DOTween.Sequence();

        if (boss.idleForm) boss.idleForm.SetActive(true);
        boss.SetPhase1PartsActive(false);
        yield return new WaitForSeconds(1.0f);

        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);
        yield return new WaitForSeconds(1.0f);

        // 设置动画
        if (boss.faceAngry) seq.Join(boss.faceAngry.DOMove(boss.faceTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerLeft) seq.Join(boss.speakerLeft.DOMove(boss.speakerLeftTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerRight) seq.Join(boss.speakerRight.DOMove(boss.speakerRightTargetPos, dur).SetEase(Ease.OutBack));

        float centerY = boss.HairCenterY;
        if (boss.hairLeft)
        {
            boss.hairLeft.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairLeftTargetPos.x, centerY, boss.hairLeftTargetPos.z);
            seq.Join(boss.hairLeft.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            seq.Join(boss.hairLeft.DORotate(boss.hairLeftTargetRot, dur).SetEase(Ease.OutBack));
        }
        if (boss.hairRight)
        {
            boss.hairRight.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairRightTargetPos.x, centerY, boss.hairRightTargetPos.z);
            seq.Join(boss.hairRight.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            seq.Join(boss.hairRight.DORotate(boss.hairRightTargetRot, dur).SetEase(Ease.OutBack));
        }

        // 【关键修复】不要依赖 seq.WaitForCompletion()，强制等待时间
        // 防止动画序列为空或瞬间完成导致攻击提前
        yield return new WaitForSeconds(dur);

        FinishDeploy();
    }

    IEnumerator RedeployRoutine()
    {
        float dur = boss.deployDuration;
        Sequence seq = DOTween.Sequence();

        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);

        if (boss.faceAngry) seq.Join(boss.faceAngry.DOMove(boss.faceTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerLeft) seq.Join(boss.speakerLeft.DOMove(boss.speakerLeftTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerRight) seq.Join(boss.speakerRight.DOMove(boss.speakerRightTargetPos, dur).SetEase(Ease.OutBack));

        float centerY = boss.HairCenterY;
        if (boss.hairLeft)
        {
            boss.hairLeft.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairLeftTargetPos.x, centerY, boss.hairLeftTargetPos.z);
            seq.Join(boss.hairLeft.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            seq.Join(boss.hairLeft.DORotate(boss.hairLeftTargetRot, dur).SetEase(Ease.OutBack));
        }
        if (boss.hairRight)
        {
            boss.hairRight.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairRightTargetPos.x, centerY, boss.hairRightTargetPos.z);
            seq.Join(boss.hairRight.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            seq.Join(boss.hairRight.DORotate(boss.hairRightTargetRot, dur).SetEase(Ease.OutBack));
        }

        // 【关键修复】同样强制等待
        yield return new WaitForSeconds(dur);

        FinishDeploy();
    }

    void FinishDeploy()
    {
        if (boss.hairLeft) { boss.savedHairLeftX = boss.hairLeftTargetPos.x; boss.savedHairLeftZ = boss.hairLeftTargetPos.z; }
        if (boss.hairRight) { boss.savedHairRightX = boss.hairRightTargetPos.x; boss.savedHairRightZ = boss.hairRightTargetPos.z; }

        boss.ResetHairMovementTime();
        isDeployed = true;
        // 部署完毕，立刻可以开始攻击计时（如果想让Boss停顿一下再打，可以把这里设为 1.0f 或 2.0f）
        attackTimer = 0f;
    }
}
