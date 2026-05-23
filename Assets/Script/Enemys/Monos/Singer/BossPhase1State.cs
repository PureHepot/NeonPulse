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
    private Coroutine deployRoutine;
    private Sequence deploySequence;

    public BossPhase1State(BossSinger boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        deploySequence?.Kill();
        deploySequence = null;
        if (deployRoutine != null)
        {
            boss.StopCoroutine(deployRoutine);
            deployRoutine = null;
        }

        boss.SetPhase1PartsActive(true);
        if (boss.idleForm) boss.idleForm.SetActive(false);

        boss.FindBulletPoints();

        if (!hasEnteredOnce)
        {
            deployRoutine = boss.StartCoroutine(EntranceRoutine());
            hasEnteredOnce = true;
        }
        else
        {
            deployRoutine = boss.StartCoroutine(RedeployRoutine());
        }
        attackTimer = 999f;
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

        if (!boss.isTransitioning && !boss.hasFinishedPhase2)
        {
            float p1Threshold = boss.MaxTotalHp - boss.hpPhase1;

            if (boss.CurrentTotalHp <= p1Threshold)
            {
                Debug.Log($">>> P1 end (TotalHP: {boss.CurrentTotalHp}) -> P2");
                boss.TriggerPhaseTransition(boss.Phase2);
            }
        }
    }

    public override void Exit()
    {
        isDeployed = false;
        deploySequence?.Kill();
        deploySequence = null;
        if (deployRoutine != null)
        {
            boss.StopCoroutine(deployRoutine);
            deployRoutine = null;
        }

        boss.SetPhase1PartsActive(false);
        if (mainAttackRoutine != null) boss.StopCoroutine(mainAttackRoutine);
        boss.StopPhase1Attack();
    }

    private IEnumerator EntranceRoutine()
    {
        float dur = boss.deployDuration;
        deploySequence?.Kill();
        deploySequence = DOTween.Sequence().SetLink(boss.gameObject);

        if (boss.idleForm) boss.idleForm.SetActive(true);
        boss.SetPhase1PartsActive(false);
        yield return new WaitForSeconds(1.0f);
        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);
        yield return new WaitForSeconds(1.0f);

        BuildDeploySequence(dur);
        yield return new WaitForSeconds(dur);
        FinishDeploy();
    }

    private IEnumerator RedeployRoutine()
    {
        float dur = boss.deployDuration;
        deploySequence?.Kill();
        deploySequence = DOTween.Sequence().SetLink(boss.gameObject);

        if (boss.idleForm) boss.idleForm.SetActive(false);
        boss.SetPhase1PartsActive(true);

        BuildDeploySequence(dur);
        yield return new WaitForSeconds(dur);
        FinishDeploy();
    }

    private void BuildDeploySequence(float dur)
    {
        if (boss.faceAngry) deploySequence.Join(boss.faceAngry.DOMove(boss.faceTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerLeft) deploySequence.Join(boss.speakerLeft.DOMove(boss.speakerLeftTargetPos, dur).SetEase(Ease.OutBack));
        if (boss.speakerRight) deploySequence.Join(boss.speakerRight.DOMove(boss.speakerRightTargetPos, dur).SetEase(Ease.OutBack));

        float centerY = boss.HairCenterY;
        if (boss.hairLeft)
        {
            boss.hairLeft.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairLeftTargetPos.x, centerY, boss.hairLeftTargetPos.z);
            deploySequence.Join(boss.hairLeft.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            deploySequence.Join(boss.hairLeft.DORotate(boss.hairLeftTargetRot, dur).SetEase(Ease.OutBack));
        }

        if (boss.hairRight)
        {
            boss.hairRight.rotation = Quaternion.identity;
            Vector3 targetPos = new Vector3(boss.hairRightTargetPos.x, centerY, boss.hairRightTargetPos.z);
            deploySequence.Join(boss.hairRight.DOMove(targetPos, dur).SetEase(Ease.OutBack));
            deploySequence.Join(boss.hairRight.DORotate(boss.hairRightTargetRot, dur).SetEase(Ease.OutBack));
        }
    }

    private void FinishDeploy()
    {
        deployRoutine = null;
        deploySequence = null;

        if (boss.hairLeft)
        {
            boss.savedHairLeftX = boss.hairLeftTargetPos.x;
            boss.savedHairLeftZ = boss.hairLeftTargetPos.z;
        }

        if (boss.hairRight)
        {
            boss.savedHairRightX = boss.hairRightTargetPos.x;
            boss.savedHairRightZ = boss.hairRightTargetPos.z;
        }

        boss.ResetHairMovementTime();
        isDeployed = true;
        attackTimer = 0f;
    }
}
