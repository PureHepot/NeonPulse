using UnityEngine;
using DG.Tweening;

public class VocalistMeleeState : BossBaseState
{
    private VocalistBoss vocalistBoss;

    [Header("近战参数")]
    public float meleeRange = 3.0f;
    public float moveSpeed = 6f;
    public float attackCooldown = 1.5f;
    public float punchDistance = 3.5f;
    public float punchDuration = 0.15f;

    private int subPhase = 0;
    private Vector3[] startLocalPos = new Vector3[2];

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        vocalistBoss = context as VocalistBoss;
        subPhase = 0;
        stateTimer = 0f;

        CacheHandPositions();

        if (vocalistBoss != null && vocalistBoss.ShouldOpenWithDrillThrow())
        {
            vocalistBoss.TryLaunchDesignDrill();
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (vocalistBoss == null || vocalistBoss.playerTarget == null) return;

        RotateTowardsPlayer();

        if (vocalistBoss.GetHealthRatio() <= 0.8f)
        {
            vocalistBoss.TryLaunchDesignDrill();
        }

        if (TryHandleHeadgearRecall()) return;

        switch (subPhase)
        {
            case 0:
                HandleChase();
                break;
            case 1:
                HandleAttackAnimation();
                break;
            case 2:
                HandleCooldown();
                break;
            case 3:
                HandleHeadgearApproach();
                break;
        }
    }

    public override void Exit()
    {
        if (vocalistBoss == null || vocalistBoss.handAnchors == null) return;

        for (int i = 0; i < vocalistBoss.handAnchors.Length; i++)
        {
            if (vocalistBoss.handAnchors[i] != null) vocalistBoss.handAnchors[i].DOKill();
        }
    }

    private void HandleChase()
    {
        float dist = Vector2.Distance(vocalistBoss.transform.position, vocalistBoss.playerTarget.position);
        if (dist > meleeRange)
        {
            vocalistBoss.transform.position = Vector2.MoveTowards(
                vocalistBoss.transform.position,
                vocalistBoss.playerTarget.position,
                moveSpeed * Time.deltaTime
            );
        }
        else
        {
            subPhase = 1;
            stateTimer = 0f;
            PerformMeleeAttack();
        }
    }

    private void HandleAttackAnimation()
    {
        if (stateTimer >= (punchDuration * 2f) + 0.2f)
        {
            subPhase = 2;
            stateTimer = 0f;
        }
    }

    private void HandleCooldown()
    {
        if (stateTimer >= attackCooldown)
        {
            subPhase = 0;
            stateTimer = 0f;
        }
    }

    private bool TryHandleHeadgearRecall()
    {
        if (!vocalistBoss.HasDockedHeadgearReady())
        {
            if (subPhase == 3)
            {
                subPhase = 0;
                stateTimer = 0f;
            }

            return false;
        }

        float dist = Vector2.Distance(vocalistBoss.transform.position, vocalistBoss.GetHeadgearDockPosition());
        if (dist > vocalistBoss.headgearRecallDistance) return false;

        if (subPhase != 3)
        {
            subPhase = 3;
            stateTimer = 0f;
            KillHandTweens();
            ResetHands();
        }

        return false;
    }

    private void HandleHeadgearApproach()
    {
        Vector3 target = vocalistBoss.GetHeadgearDockPosition();
        vocalistBoss.transform.position = Vector2.MoveTowards(
            vocalistBoss.transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(vocalistBoss.transform.position, target) <= 0.35f)
        {
            vocalistBoss.ThrowHeadgearNow();
            subPhase = 0;
            stateTimer = 0f;
        }
    }

    private void RotateTowardsPlayer()
    {
        Vector3 dir = vocalistBoss.playerTarget.position - vocalistBoss.transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        vocalistBoss.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void PerformMeleeAttack()
    {
        KillHandTweens();

        Vector3 leftAttackTarget = startLocalPos[0] + Vector3.right * punchDistance;
        Vector3 rightAttackTarget = startLocalPos[1] + Vector3.right * punchDistance;

        if (vocalistBoss.handAnchors[0] != null)
        {
            vocalistBoss.handAnchors[0]
                .DOLocalMove(leftAttackTarget, punchDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => vocalistBoss.handAnchors[0].DOLocalMove(startLocalPos[0], punchDuration).SetEase(Ease.OutQuad));
        }

        if (vocalistBoss.handAnchors[1] != null)
        {
            vocalistBoss.handAnchors[1]
                .DOLocalMove(rightAttackTarget, punchDuration)
                .SetDelay(0.1f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => vocalistBoss.handAnchors[1].DOLocalMove(startLocalPos[1], punchDuration).SetEase(Ease.OutQuad));
        }
    }

    private void CacheHandPositions()
    {
        if (vocalistBoss == null || vocalistBoss.handAnchors == null) return;

        for (int i = 0; i < startLocalPos.Length; i++)
        {
            startLocalPos[i] = vocalistBoss.handAnchors.Length > i && vocalistBoss.handAnchors[i] != null
                ? vocalistBoss.handAnchors[i].localPosition
                : Vector3.zero;
        }
    }

    private void KillHandTweens()
    {
        if (vocalistBoss.handAnchors == null) return;

        for (int i = 0; i < vocalistBoss.handAnchors.Length; i++)
        {
            if (vocalistBoss.handAnchors[i] != null) vocalistBoss.handAnchors[i].DOKill();
        }
    }

    private void ResetHands()
    {
        if (vocalistBoss.handAnchors == null) return;

        for (int i = 0; i < startLocalPos.Length && i < vocalistBoss.handAnchors.Length; i++)
        {
            if (vocalistBoss.handAnchors[i] != null)
            {
                vocalistBoss.handAnchors[i].DOLocalMove(startLocalPos[i], 0.15f).SetEase(Ease.OutQuad);
            }
        }
    }
}
