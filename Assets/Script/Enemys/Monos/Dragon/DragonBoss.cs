using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DragonBoss : BossBase
{
    private enum DragonAttackType
    {
        None,
        Fireball,
        MouthLaser,
        HandPull,
        HandGrab
    }

    [Header("Target")]
    public Transform playerTarget;

    [Header("Dragon References")]
    public Transform head;
    public Transform mouth;
    [Tooltip("火球和激光优先使用的发射锚点。")]
    public Transform attackAnchor;
    public Transform leftClaw;
    public Transform rightClaw;
    public GameObject fireballPrefab;
    public LaserBeam laserPrefab;
    [Tooltip("左侧抓取攻击使用的传送门预制件。")] 
    public GameObject portalLPrefab;
    [Tooltip("右侧抓取攻击使用的传送门预制件。")] 
    public GameObject portalRPrefab;
    public List<GameObject> minionPrefabs = new List<GameObject>();

    [Header("Entrance")]
    public float entranceStartYOffset = 4f;
    public float entranceDuration = 1.5f;
    public float entranceHoldTime = 0.2f;

    [Header("Idle")]
    public Vector2 idleDurationRange = new Vector2(0.45f, 0.9f);

    [Header("Mouth Pose")]
    public float mouthOpenOffsetY = 1.35f;
    public float mouthOpenDuration = 0.18f;
    public float mouthCloseDuration = 0.15f;

    [Header("Fireball Attack")]
    public int fireballCountPerAttack = 5;
    public float fireballInterval = 0.16f;
    public float fireballSpreadAngle = 10f;
    public Vector2 fireballSpeedRange = new Vector2(5f, 7.5f);
    public Vector2 fireballSplitDelayRange = new Vector2(0.8f, 1.3f);
    public int fireballSplitCount = 8;
    public float fireballLifetime = 5f;
    public float splitFireballLifetime = 3f;
    public float splitFireballSpeed = 7.5f;
    public float splitFireballScale = 0.55f;
    public int fireballDamage = 1;
    public float postFireballRecover = 0.35f;

    [Header("Mouth Laser Attack")]
    public float laserWarningTime = 0.55f;
    public float laserSweepDuration = 1.25f;
    public float laserRecoverTime = 0.3f;
    public float laserSweepAngle = 55f;
    public float laserMaxDistance = 18f;
    public float laserWidth = 2.4f;
    [Range(0.1f, 1f)] public float laserHitboxScale = 0.2f;
    public int laserDamage = 1;
    public float laserDamageTickRate = 0.2f;
    public LayerMask laserHitLayer;
    public bool laserDebugHitbox = false;

    [Header("Hand Pull Attack")]
    public int handPullCount = 2;
    public float handPullReachDuration = 0.45f;
    public float handPullReturnDuration = 0.65f;
    public float handPullResetDuration = 0.35f;
    public float handPullCarryPause = 0.15f;
    public float handPullDropDelay = 0.15f;
    public float handPullBetweenDelay = 0.2f;
    public Vector2 leftPullOffscreenViewport = new Vector2(-0.18f, 0.62f);
    public Vector2 rightPullOffscreenViewport = new Vector2(1.18f, 0.62f);
    public Vector2 leftPullReleaseViewport = new Vector2(0.18f, 0.34f);
    public Vector2 rightPullReleaseViewport = new Vector2(0.82f, 0.34f);
    public Vector3 carriedMinionLocalOffset = new Vector3(0f, -0.6f, 0f);

    [Header("Hand Grab Attack")]
    [Tooltip("原手臂渐隐消失所需时间。")] public float handGrabClawFadeOutDuration = 0.28f;
    [Tooltip("原手臂渐隐出现所需时间。")] public float handGrabClawFadeInDuration = 0.32f;
    [Tooltip("传送门打开动画时长。")] public float portalOpenDuration = 0.22f;
    [Tooltip("传送门关闭动画时长。")] public float portalCloseDuration = 0.18f;
    [Tooltip("传送门生成在玩家左右两侧的水平距离。")] public float handGrabPortalHorizontalOffset = 2.1f;
    [Tooltip("传送门生成在玩家旁边时的垂直偏移。")] public float handGrabPortalVerticalOffset = 0f;
    [Tooltip("传送门打开后，抓手出现前的等待时间。")] public float handGrabPortalStayTime = 1f;
    [Tooltip("同一轮左右两次抓取之间的间隔。")] public float handGrabBetweenAttemptsDelay = 0.18f;
    [Tooltip("抓手从门里推出的动画时长。")] public float handGrabPushOutDuration = 0.22f;
    [Tooltip("抓手隐藏在门内时，相对原始位置保留的比例。")] [Range(0f, 1f)] public float handGrabHiddenLocalRatio = 0.08f;
    [Tooltip("抓手向前突刺时的移动速度。")] public float handGrabTravelSpeed = 12f;
    [Tooltip("抓手从门口向前突刺的固定距离。")] public float handGrabLungeDistance = 4.2f;
    [Tooltip("抓手失败后横向退回传送门时的速度。")] public float handGrabRetreatSpeed = 14f;
    [Tooltip("单次抓取最短移动时间。")] public float handGrabMinTravelTime = 0.12f;
    [Tooltip("单次抓取最长移动时间。")] public float handGrabMaxTravelTime = 0.45f;
    [Tooltip("抓取成功后额外维持玩家被控制的时间。")] public float handGrabHoldExtraTime = 0.15f;
    [Tooltip("玩家被抓住后，相对抓手的位置偏移。")] public Vector2 grabbedPlayerOffset = new Vector2(0f, -0.45f);
    [Tooltip("抓取成功后嘴部激光的前摇时间。")] public float handGrabBreathWarningTime = 0.2f;
    [Tooltip("抓取成功后嘴部激光的持续时间。")] public float handGrabBreathActiveTime = 0.6f;

    [Header("Attack Weights")]
    public float fireballWeight = 4f;
    public float mouthLaserWeight = 2f;
    public float handPullWeight = 2f;
    public float handGrabWeight = 2f;

    [Header("Cleanup")]
    public float cleanupInterval = 5f;
    public float cleanupViewportMargin = 0.22f;
    

    private DragonBossIdleState idleState;
    private DragonBossEntranceState entranceState;
    private DragonBossFireballState fireballState;
    private DragonBossLaserState laserState;
    private DragonBossHandPullState handPullState;
    private DragonBossHandGrabState handGrabState;

    private SpriteRenderer[] spriteRenderers;
    private Vector3 battleRootPosition;
    private Vector3 mouthClosedLocalPosition;
    private Vector3 leftClawHomeLocalPosition;
    private Vector3 rightClawHomeLocalPosition;
    private Quaternion leftClawHomeLocalRotation;
    private Quaternion rightClawHomeLocalRotation;
    private Transform[] leftClawBones;
    private Transform[] rightClawBones;
    private Quaternion[] leftClawBoneRestRotations;
    private Quaternion[] rightClawBoneRestRotations;

    private Coroutine runningAction;
    private bool actionFinished = true;
    private float cleanupTimer;
    private bool advancedAttacksUnlocked;
    private DragonAttackType lastAttackType = DragonAttackType.None;
    private bool portalGrabSucceeded;

    private readonly List<DragonFireballProjectile> activeProjectiles = new List<DragonFireballProjectile>();
    private readonly List<GameObject> transientObjects = new List<GameObject>();

    private sealed class PortalGrabInstance
    {
        public GameObject PortalObject;
        public Transform PortalTransform;
        public Vector3 PortalBaseScale = Vector3.one;
        public Transform GrappleTransform;
        public Collider2D GrappleCollider;
        public Vector3 GrappleShownLocalPosition;
        public Vector3 GrappleHiddenLocalPosition;
    }

    protected override void Awake()
    {
        base.Awake();

        if (head == null) head = FindChild("head");
        if (mouth == null) mouth = FindChild("mouth");
        if (attackAnchor == null) attackAnchor = FindChild("anchor");
        if (leftClaw == null) leftClaw = FindChild("dragonClawL");
        if (rightClaw == null) rightClaw = FindChild("dragonClawR");
        if (bodyRenderer == null && head != null) bodyRenderer = head.GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        if (laserHitLayer.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                laserHitLayer = 1 << playerLayer;
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        battleRootPosition = transform.position;
        mouthClosedLocalPosition = mouth != null ? mouth.localPosition : Vector3.zero;
        leftClawHomeLocalPosition = leftClaw != null ? leftClaw.localPosition : Vector3.zero;
        rightClawHomeLocalPosition = rightClaw != null ? rightClaw.localPosition : Vector3.zero;
        leftClawHomeLocalRotation = leftClaw != null ? leftClaw.localRotation : Quaternion.identity;
        rightClawHomeLocalRotation = rightClaw != null ? rightClaw.localRotation : Quaternion.identity;
        CacheClawBones();

        entranceState = new DragonBossEntranceState();
        idleState = new DragonBossIdleState();
        fireballState = new DragonBossFireballState();
        laserState = new DragonBossLaserState();
        handPullState = new DragonBossHandPullState();
        handGrabState = new DragonBossHandGrabState();

        SwitchState(entranceState);
    }

    protected override void Update()
    {
        base.Update();

        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= cleanupInterval)
        {
            cleanupTimer = 0f;
            CleanupProjectiles();
            CleanupTransientList();
        }
    }

    public override void SwitchState(BossBaseState newState)
    {
        StopRunningAction();
        base.SwitchState(newState);
    }

    protected override void CheckPhaseTransition()
    {
        if (!advancedAttacksUnlocked && maxHp > 0f && currentHp / maxHp <= 2f / 3f)
        {
            advancedAttacksUnlocked = true;
            Debug.Log("Dragon: advanced attacks unlocked.");
        }
    }

    protected override void Die()
    {
        StopRunningAction();
        CleanupProjectiles(true);
        base.Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
        }
    }

    public bool IsActionFinished => actionFinished;
    public BossBaseState IdleState => idleState;

    public float GetIdleDuration()
    {
        return Random.Range(idleDurationRange.x, idleDurationRange.y);
    }

    public BossBaseState ChooseNextAttackState()
    {
        if (!advancedAttacksUnlocked)
        {
            lastAttackType = DragonAttackType.Fireball;
            return fireballState;
        }

        List<DragonAttackType> attackTypes = new List<DragonAttackType>
        {
            DragonAttackType.Fireball,
            DragonAttackType.MouthLaser,
            DragonAttackType.HandPull,
            DragonAttackType.HandGrab
        };

        List<float> attackWeights = new List<float>
        {
            Mathf.Max(0f, fireballWeight),
            Mathf.Max(0f, mouthLaserWeight),
            Mathf.Max(0f, handPullWeight),
            Mathf.Max(0f, handGrabWeight)
        };

        int alternativeCount = 0;
        for (int i = 0; i < attackTypes.Count; i++)
        {
            if (attackWeights[i] > 0f && attackTypes[i] != lastAttackType)
                alternativeCount++;
        }

        float totalWeight = 0f;
        for (int i = 0; i < attackTypes.Count; i++)
        {
            if (attackWeights[i] <= 0f)
                continue;

            if (alternativeCount > 0 && attackTypes[i] == lastAttackType)
                continue;

            totalWeight += attackWeights[i];
        }

        if (totalWeight <= 0f)
        {
            lastAttackType = DragonAttackType.Fireball;
            return fireballState;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < attackTypes.Count; i++)
        {
            if (attackWeights[i] <= 0f)
                continue;

            if (alternativeCount > 0 && attackTypes[i] == lastAttackType)
                continue;

            roll -= attackWeights[i];
            if (roll <= 0f)
            {
                lastAttackType = attackTypes[i];
                return GetStateForAttack(attackTypes[i]);
            }
        }

        lastAttackType = DragonAttackType.Fireball;
        return fireballState;
    }

    public void BeginAction(IEnumerator routine)
    {
        StopRunningAction();
        actionFinished = false;
        runningAction = StartCoroutine(ActionWrapper(routine));
    }

    private IEnumerator ActionWrapper(IEnumerator routine)
    {
        yield return routine;
        runningAction = null;
        actionFinished = true;
    }

    private void StopRunningAction()
    {
        if (runningAction != null)
        {
            StopCoroutine(runningAction);
            runningAction = null;
        }

        actionFinished = true;
        ResetPoseImmediate();
        ClearTransientObjects();
    }

    public IEnumerator EntranceRoutine()
    {
        ResetPoseImmediate();
        transform.position = battleRootPosition + Vector3.up * entranceStartYOffset;
        SetAllRendererAlpha(0f);

        Tween moveTween = transform.DOMove(battleRootPosition, entranceDuration).SetEase(Ease.OutCubic);
        Tween fadeTween = DOTween.To(() => 0f, SetAllRendererAlpha, 1f, entranceDuration).SetEase(Ease.OutQuad);

        yield return moveTween.WaitForCompletion();
        yield return fadeTween.WaitForCompletion();

        yield return new WaitForSeconds(entranceHoldTime);
    }

    public IEnumerator FireballRoutine()
    {
        yield return OpenMouth().WaitForCompletion();

        int count = Mathf.Max(1, fireballCountPerAttack);
        for (int i = 0; i < count; i++)
        {
            Vector2 aimDir = GetAimDirection(GetAttackSpawnPosition());
            float angleOffset = count == 1 ? 0f : Mathf.Lerp(-fireballSpreadAngle, fireballSpreadAngle, i / (float)(count - 1));
            aimDir = (Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * (Vector3)aimDir);

            SpawnFireball(GetAttackSpawnPosition(), aimDir, Random.Range(fireballSpeedRange.x, fireballSpeedRange.y), fireballDamage, fireballLifetime, true, Random.Range(fireballSplitDelayRange.x, fireballSplitDelayRange.y), fireballSplitCount, splitFireballSpeed, splitFireballLifetime, splitFireballScale);
            yield return new WaitForSeconds(fireballInterval);
        }

        yield return new WaitForSeconds(postFireballRecover);
        yield return CloseMouth().WaitForCompletion();
    }

    public IEnumerator MouthLaserRoutine()
    {
        yield return OpenMouth().WaitForCompletion();
        yield return SweepMouthLaser(-laserSweepAngle, laserSweepAngle);
        yield return SweepMouthLaser(laserSweepAngle, -laserSweepAngle);
        yield return new WaitForSeconds(laserRecoverTime);
        yield return CloseMouth().WaitForCompletion();
    }

    public IEnumerator HandPullRoutine()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0)
            yield break;

        int count = Mathf.Max(1, handPullCount);
        for (int i = 0; i < count; i++)
        {
            bool useLeft = i % 2 == 0;
            Transform claw = useLeft ? leftClaw : rightClaw;
            if (claw == null)
                continue;

            Vector3 offscreen = GetViewportWorld(useLeft ? leftPullOffscreenViewport : rightPullOffscreenViewport);
            Vector3 releasePoint = GetViewportWorld(useLeft ? leftPullReleaseViewport : rightPullReleaseViewport);

            yield return MoveClawWorld(claw, offscreen, handPullReachDuration);

            GameObject carriedMinion = CreateCarriedMinion(GetRandomMinionPrefab(), claw);
            yield return new WaitForSeconds(handPullCarryPause);

            yield return MoveClawWorld(claw, releasePoint, handPullReturnDuration);

            if (carriedMinion != null)
            {
                ReleaseCarriedMinion(carriedMinion);
                yield return new WaitForSeconds(handPullDropDelay);
            }

            yield return ResetClaw(claw, handPullResetDuration);
            yield return new WaitForSeconds(handPullBetweenDelay);
        }
    }

    public IEnumerator HandGrabRoutine()
    {
        yield return FadeOriginalClaws(false, handGrabClawFadeOutDuration);

        yield return ExecutePortalHandGrab(true);
        yield return new WaitForSeconds(handGrabBetweenAttemptsDelay);
        yield return ExecutePortalHandGrab(false);

        yield return FadeOriginalClaws(true, handGrabClawFadeInDuration);
        ResetPoseImmediate();
    }

    private IEnumerator ExecutePortalHandGrab(bool useLeftPortal)
    {
        portalGrabSucceeded = false;

        PortalGrabInstance instance = SpawnPortalGrabInstance(useLeftPortal);
        if (instance == null || instance.PortalTransform == null || instance.GrappleTransform == null)
        {
            yield break;
        }

        yield return OpenPortal(instance);
        yield return new WaitForSeconds(handGrabPortalStayTime);
        yield return PushOutGrappleFromPortal(instance);

        bool success = false;
        yield return LungeGrappleAtPlayer(instance, useLeftPortal, value => success = value);
        portalGrabSucceeded = success;

        if (success)
        {
            yield return GrabPlayerAndBreath(instance.GrappleTransform, useLeftPortal);
        }

        yield return RetractGrapple(instance);
        yield return ClosePortal(instance, true);
    }

    private IEnumerator GrabPlayerAndBreath(Transform grapple, bool useLeftPortal)
    {
        if (playerTarget == null || grapple == null)
            yield break;

        PlayerController player = playerTarget.GetComponent<PlayerController>();
        if (player == null)
            yield break;

        float sideSign = useLeftPortal ? -1f : 1f;
        Vector3 holdOffset = new Vector3(grabbedPlayerOffset.x * sideSign, grabbedPlayerOffset.y, 0f);
        Transform grappleAnchor = FindChildRecursive(grapple, "anchor");
        yield return OpenMouth().WaitForCompletion();

        Vector3 graspPoint = GetGrappleHoldPosition(grapple, grappleAnchor, holdOffset);
        SnapPlayerToHoldPoint(player, graspPoint);
        LaserBeam breath = SpawnDirectedLaser(GetAttackSpawnPosition(), (graspPoint - GetAttackSpawnPosition()).normalized, handGrabBreathWarningTime, handGrabBreathActiveTime);

        float holdDuration = handGrabBreathWarningTime + handGrabBreathActiveTime + handGrabHoldExtraTime;
        yield return HoldPlayer(player, grapple, grappleAnchor, holdOffset, holdDuration);

        if (breath != null)
        {
            transientObjects.Remove(breath.gameObject);
        }

        yield return CloseMouth().WaitForCompletion();
    }

    private IEnumerator HoldPlayer(PlayerController player, Transform grapple, Transform grappleAnchor, Vector3 offset, float duration)
    {
        bool originalStun = player.IsStunned;
        float timer = 0f;

        player.IsStunned = true;
        while (timer < duration && player != null && !player.IsDead && grapple != null)
        {
            timer += Time.deltaTime;
            Vector3 holdPosition = GetGrappleHoldPosition(grapple, grappleAnchor, offset);
            SnapPlayerToHoldPoint(player, holdPosition);

            yield return null;
        }

        if (player != null)
        {
            player.IsStunned = originalStun;
        }
    }

    private Vector3 GetGrappleHoldPosition(Transform grapple, Transform grappleAnchor, Vector3 fallbackOffset)
    {
        if (grappleAnchor != null)
            return grappleAnchor.position;

        return grapple.position + fallbackOffset;
    }

    private void SnapPlayerToHoldPoint(PlayerController player, Vector3 holdPosition)
    {
        if (player == null)
            return;

        if (player.Rigid2d != null)
        {
            player.Rigid2d.velocity = Vector2.zero;
            player.Rigid2d.position = holdPosition;
            player.Rigid2d.MovePosition(holdPosition);
        }

        player.transform.position = holdPosition;
    }

    private IEnumerator SweepMouthLaser(float fromAngle, float toAngle)
    {
        if (laserPrefab == null)
            yield break;

        GameObject anchor = new GameObject("DragonLaserAnchor");
        transientObjects.Add(anchor);
        anchor.transform.position = GetAttackSpawnPosition();
        anchor.transform.rotation = Quaternion.Euler(0f, 0f, fromAngle);

        LaserBeam beam = Instantiate(laserPrefab, anchor.transform.position, anchor.transform.rotation);
        transientObjects.Add(beam.gameObject);
        beam.warningTime = laserWarningTime;
        beam.activeTime = laserSweepDuration;
        beam.maxDistance = laserMaxDistance;
        beam.laserWidth = laserWidth;
        beam.hitboxScale = laserHitboxScale;
        beam.damage = laserDamage;
        beam.damageTickRate = laserDamageTickRate;
        beam.hitLayer = laserHitLayer.value != 0 ? laserHitLayer : beam.hitLayer;
        beam.showDebugHitbox = laserDebugHitbox;
        beam.FireTracking(anchor.transform, 0f);

        float duration = laserWarningTime + laserSweepDuration;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            anchor.transform.position = GetAttackSpawnPosition();

            if (elapsed >= laserWarningTime)
            {
                float t = Mathf.Clamp01((elapsed - laserWarningTime) / Mathf.Max(0.01f, laserSweepDuration));
                float angle = Mathf.Lerp(fromAngle, toAngle, t);
                anchor.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }
    }

    private Tween OpenMouth()
    {
        if (mouth == null)
            return DOVirtual.DelayedCall(0f, () => { });

        mouth.DOKill();
        return mouth.DOLocalMoveY(mouthClosedLocalPosition.y - mouthOpenOffsetY, mouthOpenDuration).SetEase(Ease.OutQuad);
    }

    private Tween CloseMouth()
    {
        if (mouth == null)
            return DOVirtual.DelayedCall(0f, () => { });

        mouth.DOKill();
        return mouth.DOLocalMoveY(mouthClosedLocalPosition.y, mouthCloseDuration).SetEase(Ease.OutQuad);
    }

    private IEnumerator MoveClawWorld(Transform claw, Vector3 worldTarget, float duration)
    {
        if (claw == null)
            yield break;

        claw.DOKill();
        Tween tween = claw.DOMove(worldTarget, duration).SetEase(Ease.OutCubic);
        yield return tween.WaitForCompletion();
    }

    private IEnumerator ResetClaw(Transform claw, float duration)
    {
        if (claw == null)
            yield break;

        Vector3 localPos = claw == leftClaw ? leftClawHomeLocalPosition : rightClawHomeLocalPosition;
        Quaternion localRot = claw == leftClaw ? leftClawHomeLocalRotation : rightClawHomeLocalRotation;

        claw.DOKill();
        Tween moveTween = claw.DOLocalMove(localPos, duration).SetEase(Ease.InOutSine);
        Tween rotTween = claw.DOLocalRotateQuaternion(localRot, duration).SetEase(Ease.InOutSine);
        ResetClawBones(claw, duration);
        yield return moveTween.WaitForCompletion();
        yield return rotTween.WaitForCompletion();
    }

    private void ResetPoseImmediate()
    {
        if (transform != null)
        {
            transform.DOKill();
            transform.position = battleRootPosition;
        }

        if (mouth != null)
        {
            mouth.DOKill();
            mouth.localPosition = mouthClosedLocalPosition;
        }

        if (leftClaw != null)
        {
            leftClaw.DOKill();
            leftClaw.localPosition = leftClawHomeLocalPosition;
            leftClaw.localRotation = leftClawHomeLocalRotation;
            ResetClawBonesImmediate(leftClaw);
        }

        if (rightClaw != null)
        {
            rightClaw.DOKill();
            rightClaw.localPosition = rightClawHomeLocalPosition;
            rightClaw.localRotation = rightClawHomeLocalRotation;
            ResetClawBonesImmediate(rightClaw);
        }

        SetOriginalClawsVisible(true);
        SetClawRenderersAlpha(GetClawRenderers(), 1f);
    }

    private void CacheClawBones()
    {
        leftClawBones = CollectClawBones(leftClaw);
        rightClawBones = CollectClawBones(rightClaw);
        leftClawBoneRestRotations = CaptureBoneRotations(leftClawBones);
        rightClawBoneRestRotations = CaptureBoneRotations(rightClawBones);
    }

    private Transform[] CollectClawBones(Transform claw)
    {
        if (claw == null)
            return new Transform[0];

        List<Transform> bones = new List<Transform>();
        string prefix = claw.name + "_Bone_";
        foreach (Transform child in claw.GetComponentsInChildren<Transform>(true))
        {
            if (child != claw && child.name.StartsWith(prefix))
                bones.Add(child);
        }

        bones.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return bones.ToArray();
    }

    private Quaternion[] CaptureBoneRotations(Transform[] bones)
    {
        Quaternion[] rotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            rotations[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;
        return rotations;
    }

    private void ResetClawBones(Transform claw, float duration)
    {
        Transform[] bones = GetBonesForClaw(claw);
        Quaternion[] restRotations = GetBoneRestRotations(claw);

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;

            bones[i].DOKill();
            bones[i].DOLocalRotateQuaternion(restRotations[i], duration).SetEase(Ease.InOutSine);
        }
    }

    private void ResetClawBonesImmediate(Transform claw)
    {
        Transform[] bones = GetBonesForClaw(claw);
        Quaternion[] restRotations = GetBoneRestRotations(claw);

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;

            bones[i].DOKill();
            bones[i].localRotation = restRotations[i];
        }
    }

    private Transform[] GetBonesForClaw(Transform claw)
    {
        if (claw == leftClaw)
            return leftClawBones ?? new Transform[0];
        if (claw == rightClaw)
            return rightClawBones ?? new Transform[0];
        return new Transform[0];
    }

    private Quaternion[] GetBoneRestRotations(Transform claw)
    {
        if (claw == leftClaw)
            return leftClawBoneRestRotations ?? new Quaternion[0];
        if (claw == rightClaw)
            return rightClawBoneRestRotations ?? new Quaternion[0];
        return new Quaternion[0];
    }

    private LaserBeam SpawnDirectedLaser(Vector3 origin, Vector2 direction, float warningTime, float activeTime)
    {
        if (laserPrefab == null)
            return null;

        LaserBeam beam = Instantiate(laserPrefab, origin, Quaternion.identity);
        transientObjects.Add(beam.gameObject);
        beam.warningTime = warningTime;
        beam.activeTime = activeTime;
        beam.maxDistance = laserMaxDistance;
        beam.laserWidth = laserWidth;
        beam.hitboxScale = laserHitboxScale;
        beam.damage = laserDamage;
        beam.damageTickRate = laserDamageTickRate;
        beam.hitLayer = laserHitLayer.value != 0 ? laserHitLayer : beam.hitLayer;
        beam.showDebugHitbox = laserDebugHitbox;
        beam.Fire(origin, direction.normalized);
        return beam;
    }

    private DragonFireballProjectile SpawnFireball(Vector3 position, Vector2 direction, float speed, int damage, float lifeTime, bool canSplit, float splitDelay, int splitCount, float childSpeed, float childLifetime, float childScale)
    {
        if (fireballPrefab == null)
            return null;

        GameObject fireball = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(fireballPrefab, position, Quaternion.identity)
            : Instantiate(fireballPrefab, position, Quaternion.identity);

        PrepareFireballObject(fireball);

        DragonFireballProjectile projectile = fireball.GetComponent<DragonFireballProjectile>();
        if (projectile == null)
            projectile = fireball.AddComponent<DragonFireballProjectile>();

        projectile.Launch(this, direction, speed, damage, lifeTime, canSplit, splitDelay, splitCount, childSpeed, childLifetime, childScale);
        RegisterProjectile(projectile);
        return projectile;
    }

    public void SpawnSplitFireball(Vector3 position, Vector2 direction, float speed, int damage, float lifeTime, float scale)
    {
        SpawnFireball(position, direction, speed, damage, lifeTime, false, 0f, 0, 0f, 0f, scale);
    }

    private void PrepareFireballObject(GameObject fireball)
    {
        if (fireball == null)
            return;

        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer >= 0)
            fireball.layer = enemyBulletLayer;

        CircleCollider2D collider = fireball.GetComponent<CircleCollider2D>();
        if (collider == null)
            collider = fireball.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.35f;

        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = fireball.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
    }

    private PortalGrabInstance SpawnPortalGrabInstance(bool useLeftPortal)
    {
        GameObject portalPrefab = useLeftPortal ? portalLPrefab : portalRPrefab;
        if (portalPrefab == null)
            return null;

        Vector3 portalPosition = GetPortalSpawnWorld(useLeftPortal);
        GameObject portalObject = Instantiate(portalPrefab, portalPosition, Quaternion.identity);
        transientObjects.Add(portalObject);

        PortalGrabInstance instance = new PortalGrabInstance
        {
            PortalObject = portalObject,
            PortalTransform = portalObject.transform,
            PortalBaseScale = portalObject.transform.localScale
        };

        Transform grapple = FindPortalGrapple(portalObject.transform, useLeftPortal ? "dragonGrapL" : "dragonGrapR");
        if (grapple == null)
        {
            transientObjects.Remove(portalObject);
            Destroy(portalObject);
            return null;
        }

        instance.GrappleShownLocalPosition = grapple.localPosition;
        instance.GrappleHiddenLocalPosition = grapple.localPosition * handGrabHiddenLocalRatio;
        instance.GrappleCollider = grapple.GetComponent<Collider2D>();
        grapple.gameObject.SetActive(false);
        instance.GrappleTransform = grapple;
        return instance;
    }

    private Transform FindPortalGrapple(Transform portalRoot, string grappleName)
    {
        if (portalRoot == null)
            return null;

        foreach (Transform child in portalRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == portalRoot || child.name != grappleName)
                continue;

            return child;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == childName)
                return child;
        }

        return null;
    }

    private IEnumerator OpenPortal(PortalGrabInstance instance)
    {
        if (instance == null || instance.PortalTransform == null)
            yield break;

        instance.PortalTransform.DOKill();
        instance.PortalTransform.localScale = Vector3.zero;
        Tween tween = instance.PortalTransform.DOScale(instance.PortalBaseScale, portalOpenDuration).SetEase(Ease.OutBack);
        yield return tween.WaitForCompletion();
    }

    private IEnumerator ClosePortal(PortalGrabInstance instance, bool destroyGrapple)
    {
        if (instance != null && destroyGrapple && instance.GrappleTransform != null)
            instance.GrappleTransform.gameObject.SetActive(false);

        if (instance == null || instance.PortalTransform == null)
            yield break;

        instance.PortalTransform.DOKill();
        Tween tween = instance.PortalTransform.DOScale(Vector3.zero, portalCloseDuration).SetEase(Ease.InBack);
        yield return tween.WaitForCompletion();

        if (instance.PortalObject != null)
        {
            transientObjects.Remove(instance.PortalObject);
            Destroy(instance.PortalObject);
        }
    }

    private IEnumerator PushOutGrappleFromPortal(PortalGrabInstance instance)
    {
        if (instance == null || instance.GrappleTransform == null)
            yield break;

        instance.GrappleTransform.DOKill();
        instance.GrappleTransform.gameObject.SetActive(true);
        instance.GrappleTransform.localPosition = instance.GrappleHiddenLocalPosition;

        Tween tween = instance.GrappleTransform
            .DOLocalMove(instance.GrappleShownLocalPosition, handGrabPushOutDuration)
            .SetEase(Ease.OutCubic);

        yield return tween.WaitForCompletion();
    }

    private IEnumerator LungeGrappleAtPlayer(PortalGrabInstance instance, bool useLeftPortal, System.Action<bool> onComplete)
    {
        Transform grapple = instance != null ? instance.GrappleTransform : null;
        if (grapple == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        Vector3 start = grapple.position;
        Vector3 forward = useLeftPortal ? Vector3.right : Vector3.left;
        grapple.right = forward;
        Vector3 target = start + forward * handGrabLungeDistance;
        float distance = handGrabLungeDistance;
        float duration = handGrabTravelSpeed <= 0.01f
            ? handGrabMaxTravelTime
            : Mathf.Clamp(distance / handGrabTravelSpeed, handGrabMinTravelTime, handGrabMaxTravelTime);

        bool success = false;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            Vector3 next = Vector3.Lerp(start, target, EaseOutCubic(t));
            grapple.right = forward;
            grapple.position = next;

            if (IsGrappleTouchingPlayer(instance))
            {
                success = true;
                break;
            }

            yield return null;
        }

        onComplete?.Invoke(success);
    }

    private IEnumerator RetractGrapple(PortalGrabInstance instance)
    {
        if (instance == null || instance.GrappleTransform == null || instance.PortalTransform == null)
            yield break;

        Vector3 target = instance.PortalTransform.TransformPoint(instance.GrappleHiddenLocalPosition);
        float distance = Vector2.Distance(instance.GrappleTransform.position, target);
        float duration = handGrabRetreatSpeed <= 0.01f
            ? handGrabMaxTravelTime
            : Mathf.Clamp(distance / handGrabRetreatSpeed, handGrabMinTravelTime, handGrabMaxTravelTime);

        float elapsed = 0f;
        Vector3 start = instance.GrappleTransform.position;
        while (elapsed < duration && instance.GrappleTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            Vector3 next = Vector3.Lerp(start, target, t);
            Vector3 retractDir = (target - next).sqrMagnitude > 0.0001f ? (target - next).normalized : Vector3.left;
            instance.GrappleTransform.right = retractDir;
            instance.GrappleTransform.position = next;
            yield return null;
        }

        if (instance.GrappleTransform != null)
            instance.GrappleTransform.localPosition = instance.GrappleHiddenLocalPosition;
    }

    private float EaseOutCubic(float t)
    {
        float inv = 1f - Mathf.Clamp01(t);
        return 1f - inv * inv * inv;
    }

    private bool IsGrappleTouchingPlayer(PortalGrabInstance instance)
    {
        if (instance == null || instance.GrappleCollider == null || playerTarget == null)
            return false;

        Collider2D[] playerColliders = playerTarget.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled)
                continue;

            if (instance.GrappleCollider.bounds.Intersects(playerCollider.bounds))
                return true;
        }

        return false;
    }

    private IEnumerator FadeOriginalClaws(bool visible, float duration)
    {
        SetOriginalClawsVisible(true);

        SpriteRenderer[] renderers = GetClawRenderers();
        if (renderers.Length == 0)
        {
            SetOriginalClawsVisible(visible);
            yield break;
        }

        float startAlpha = renderers[0] != null ? renderers[0].color.a : (visible ? 0f : 1f);
        float endAlpha = visible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            SetClawRenderersAlpha(renderers, Mathf.Lerp(startAlpha, endAlpha, t));
            yield return null;
        }

        SetClawRenderersAlpha(renderers, endAlpha);

        if (!visible)
            SetOriginalClawsVisible(false);
    }

    private void SetOriginalClawsVisible(bool visible)
    {
        SetClawHierarchyVisible(leftClaw, visible);
        SetClawHierarchyVisible(rightClaw, visible);
    }

    private void SetClawHierarchyVisible(Transform claw, bool visible)
    {
        if (claw != null && claw.gameObject.activeSelf != visible)
            claw.gameObject.SetActive(visible);
    }

    private SpriteRenderer[] GetClawRenderers()
    {
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        CollectClawRenderers(leftClaw, renderers);
        CollectClawRenderers(rightClaw, renderers);
        return renderers.ToArray();
    }

    private void CollectClawRenderers(Transform claw, List<SpriteRenderer> renderers)
    {
        if (claw == null)
            return;

        SpriteRenderer[] localRenderers = claw.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < localRenderers.Length; i++)
        {
            if (localRenderers[i] != null)
                renderers.Add(localRenderers[i]);
        }
    }

    private void SetClawRenderersAlpha(SpriteRenderer[] renderers, float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color color = renderers[i].color;
            color.a = alpha;
            renderers[i].color = color;
        }
    }

    private GameObject CreateCarriedMinion(GameObject prefab, Transform claw)
    {
        if (prefab == null || claw == null)
            return null;

        GameObject minion = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(prefab, claw.position, Quaternion.identity)
            : Instantiate(prefab, claw.position, Quaternion.identity);

        EnemyBase enemy = minion.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.OnDespawn();
            enemy.enabled = false;
        }

        SetCarriedMinionState(minion, true);
        minion.transform.SetParent(claw, true);
        minion.transform.localPosition = carriedMinionLocalOffset;
        minion.transform.localRotation = Quaternion.identity;
        return minion;
    }

    private void ReleaseCarriedMinion(GameObject minion)
    {
        if (minion == null)
            return;

        minion.transform.SetParent(null, true);
        SetCarriedMinionState(minion, false);

        EnemyBase enemy = minion.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.enabled = true;
            enemy.OnSpawn();
        }
    }

    private void SetCarriedMinionState(GameObject minion, bool carried)
    {
        if (minion == null)
            return;

        Collider2D[] colliders = minion.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = !carried;

        Rigidbody2D rb = minion.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = !carried;
        }
    }

    private void RegisterProjectile(DragonFireballProjectile projectile)
    {
        if (projectile == null || activeProjectiles.Contains(projectile))
            return;

        activeProjectiles.Add(projectile);
    }

    public void UnregisterProjectile(DragonFireballProjectile projectile)
    {
        activeProjectiles.Remove(projectile);
    }

    private void CleanupProjectiles(bool force = false)
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            DragonFireballProjectile projectile = activeProjectiles[i];
            if (projectile == null)
            {
                activeProjectiles.RemoveAt(i);
                continue;
            }

            if (force || projectile.CanBeCulled(cleanupViewportMargin))
            {
                projectile.ForceRecycle();
            }
        }
    }

    private void ClearTransientObjects()
    {
        for (int i = 0; i < transientObjects.Count; i++)
        {
            if (transientObjects[i] != null)
                Destroy(transientObjects[i]);
        }
        transientObjects.Clear();
    }

    private void CleanupTransientList()
    {
        for (int i = transientObjects.Count - 1; i >= 0; i--)
        {
            if (transientObjects[i] == null)
                transientObjects.RemoveAt(i);
        }
    }

    private void SetAllRendererAlpha(float alpha)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            Color color = spriteRenderers[i].color;
            color.a = alpha;
            spriteRenderers[i].color = color;
        }
    }

    private Vector3 GetAttackSpawnPosition()
    {
        if (attackAnchor != null)
            return attackAnchor.position;
        if (mouth != null)
            return mouth.position;
        if (head != null)
            return head.position;
        return transform.position;
    }

    private Vector2 GetAimDirection(Vector3 origin)
    {
        if (playerTarget == null)
            return Vector2.down;

        Vector2 dir = playerTarget.position - origin;
        if (dir.sqrMagnitude <= 0.0001f)
            return Vector2.down;
        return dir.normalized;
    }

    private GameObject GetRandomMinionPrefab()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0)
            return null;

        return minionPrefabs[Random.Range(0, minionPrefabs.Count)];
    }

    private Vector3 GetPortalSpawnWorld(bool useLeftPortal)
    {
        Vector3 pivot = playerTarget != null ? playerTarget.position : GetViewportWorld(new Vector2(0.5f, 0.35f));
        float sideSign = useLeftPortal ? -1f : 1f;
        pivot += new Vector3(handGrabPortalHorizontalOffset * sideSign, handGrabPortalVerticalOffset, 0f);
        return pivot;
    }

    private Vector3 GetViewportWorld(Vector2 viewport)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return transform.position;

        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
        world.z = transform.position.z;
        return world;
    }

    private Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }
        return null;
    }

    private BossBaseState GetStateForAttack(DragonAttackType attackType)
    {
        switch (attackType)
        {
            case DragonAttackType.MouthLaser:
                return laserState;
            case DragonAttackType.HandPull:
                return handPullState;
            case DragonAttackType.HandGrab:
                return handGrabState;
            default:
                return fireballState;
        }
    }

}

public class DragonFireballProjectile : MonoBehaviour, IPoolable
{
    private DragonBoss owner;
    private Vector2 moveDirection;
    private float speed;
    private int damage;
    private float lifeTime;
    private bool canSplit;
    private float splitDelay;
    private int splitCount;
    private float splitChildSpeed;
    private float splitChildLifetime;
    private float splitChildScale;

    private float elapsed;
    private bool splitTriggered;

    public void Launch(DragonBoss newOwner, Vector2 direction, float newSpeed, int newDamage, float newLifeTime, bool allowSplit, float newSplitDelay, int newSplitCount, float newChildSpeed, float newChildLifetime, float newChildScale)
    {
        owner = newOwner;
        moveDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.down : direction.normalized;
        speed = newSpeed;
        damage = newDamage;
        lifeTime = newLifeTime;
        canSplit = allowSplit;
        splitDelay = newSplitDelay;
        splitCount = Mathf.Max(1, newSplitCount);
        splitChildSpeed = newChildSpeed;
        splitChildLifetime = newChildLifetime;
        splitChildScale = newChildScale;

        elapsed = 0f;
        splitTriggered = false;

        transform.right = moveDirection;
        float scale = allowSplit ? 1f : newChildScale;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void OnSpawn()
    {
        elapsed = 0f;
        splitTriggered = false;
    }

    public void OnDespawn()
    {
        if (owner != null)
        {
            owner.UnregisterProjectile(this);
            owner = null;
        }
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
        elapsed += Time.deltaTime;

        if (canSplit && !splitTriggered && elapsed >= splitDelay)
        {
            Split();
            return;
        }

        if (elapsed >= lifeTime)
        {
            ForceRecycle();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        if (owner != null && other.GetComponent<DragonBoss>() == owner)
            return;

        if (other.GetComponent<ShieldController>() != null)
        {
            ForceRecycle();
            return;
        }

        if (other.CompareTag("Player"))
        {
            other.GetComponentInChildren<HealthModule>()?.TakeDamage(damage, transform);
            ForceRecycle();
        }
    }

    public bool CanBeCulled(float viewportMargin)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        return viewport.x < -viewportMargin
            || viewport.x > 1f + viewportMargin
            || viewport.y < -viewportMargin
            || viewport.y > 1f + viewportMargin
            || elapsed > lifeTime + 0.5f;
    }

    public void ForceRecycle()
    {
        if (owner != null)
        {
            owner.UnregisterProjectile(this);
            owner = null;
        }

        if (ObjectPoolManager.Instance != null && GetComponent<PoolObject>() != null)
            ObjectPoolManager.Instance.Return(gameObject);
        else
            Destroy(gameObject);
    }

    private void Split()
    {
        splitTriggered = true;

        if (owner != null)
        {
            float step = 360f / splitCount;
            for (int i = 0; i < splitCount; i++)
            {
                Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, step * i) * Vector3.right);
                owner.SpawnSplitFireball(transform.position, dir, splitChildSpeed, damage, splitChildLifetime, splitChildScale);
            }
        }

        ForceRecycle();
    }
}
