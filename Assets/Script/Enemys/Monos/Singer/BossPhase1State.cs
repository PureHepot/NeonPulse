using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhase1State : SingerBossBaseState
{
    private float attackTimer;
    private bool isDeployed = false;
    private bool hasEnteredOnce = false;

    // 【新增】记录主攻击协程
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

        // 1. 头发正弦运动
        boss.HandlePhase1HairMovement();

        // 【新增】脸部悬浮运动
        boss.HandleFaceHover();

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            // 【修改】记录启动的协程，防止它变成“孤儿”
            mainAttackRoutine = boss.StartCoroutine(boss.FirePhase1Barrage());
            attackTimer = boss.p1AttackInterval;
        }

        if (!boss.hasFinishedPhase2 && boss.HpPercent <= 0.666f && boss.HpPercent > 0.334f)
        {
            boss.TransitionToState(boss.Phase2);
        }
    }

    public override void Exit()
    {
        isDeployed = false;
        boss.SetPhase1PartsActive(false); // 这会隐藏 BattleForm

        // 停止之前的 wrapper 协程
        if (mainAttackRoutine != null) boss.StopCoroutine(mainAttackRoutine);

        // 【必须调用】这会把 isP1Attacking 设为 false
        boss.StopPhase1Attack();
    }

    // ... (EntranceRoutine, RedeployRoutine, DoPartsMoveAnimation 等保持不变) ...
    // 下面是完整的 Deploy 代码，防止你丢失

    IEnumerator EntranceRoutine()
    {
        if (boss.idleForm) boss.idleForm.SetActive(true);
        boss.SetPhase1PartsActive(false);
        yield return new WaitForSeconds(1.0f);
        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);
        yield return new WaitForSeconds(1.0f);
        yield return boss.StartCoroutine(DoPartsMoveAnimation());
        FinishDeploy();
    }

    IEnumerator RedeployRoutine()
    {
        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);
        yield return boss.StartCoroutine(DoPartsMoveAnimation());
        FinishDeploy();
    }

    IEnumerator DoPartsMoveAnimation()
    {
        Sequence seq = DOTween.Sequence();
        float dur = boss.deployDuration;

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
        yield return seq.WaitForCompletion();
    }

    void FinishDeploy()
    {
        if (boss.hairLeft) { boss.savedHairLeftX = boss.hairLeftTargetPos.x; boss.savedHairLeftZ = boss.hairLeftTargetPos.z; }
        if (boss.hairRight) { boss.savedHairRightX = boss.hairRightTargetPos.x; boss.savedHairRightZ = boss.hairRightTargetPos.z; }

        boss.ResetHairMovementTime();
        isDeployed = true;
        // 部署完立即攻击
        attackTimer = 0f;
    }
}
