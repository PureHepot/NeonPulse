using UnityEngine;

public class EnemyTrap : EnemyBase
{
    private enum TrapState
    {
        Chasing,
        Opening,
        Advancing,
        Closing,
        LatchedClosed
    }

    [Header("References")]
    public Transform head;
    public Transform trapLeft;
    public Transform trapRight;
    public CircleCollider2D rangeTrigger;

    [Header("Chase")]
    public float followSmooth = 8f;
    public float rotateSpeed = 540f;
    public float stopDistance = 0.25f;

    [Header("Trap Attack")]
    public float trapMoveSpeed = 10f;
    public float advanceSpeed = 14f;
    public float openRadiusScale = 1f;
    public float rangeExitBuffer = 0.3f;
    public float fallbackRange = 1.5f;
    public bool freezeAfterClose = true;

    [Header("Lock Condition")]
    public Vector2 lockCheckSize = new Vector2(1.2f, 2.2f);
    public Vector2 lockCheckOffset = Vector2.zero;

    [Header("Child Hitbox Sync")]
    public bool syncChildLayerWithRoot = true;
    public bool forceChildEnemyTag = true;

    private TrapState state = TrapState.Chasing;
    private bool attackArmed = true;

    private Vector3 trapLeftClosedLocalPos;
    private Vector3 trapRightClosedLocalPos;
    private Vector3 trapLeftOpenLocalPos;
    private Vector3 trapRightOpenLocalPos;
    private Vector3 attackTargetWorldPos;
    private bool cachedClosedPose;

    protected override void Awake()
    {
        base.Awake();
        AutoBindReferences();
        CacheClosedPose();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AutoBindReferences();
        CacheClosedPose();
        EnsureChildHitboxSetup();

        RestoreTrapClosedPose();
        state = TrapState.Chasing;
        attackArmed = true;
        rb.velocity = Vector2.zero;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        switch (state)
        {
            case TrapState.Chasing:
                UpdateChasing();
                break;
            case TrapState.Opening:
                UpdateOpening();
                break;
            case TrapState.Advancing:
                UpdateAdvancing();
                break;
            case TrapState.Closing:
                UpdateClosing();
                break;
            case TrapState.LatchedClosed:
                UpdateLatchedClosed();
                break;
        }
    }

    private void UpdateChasing()
    {
        FacePlayer();
        FollowPlayer();

        float distanceToRangeCenter = Vector2.Distance(GetRangeCenter(), playerTransform.position);
        float enterRange = GetAttackRange();
        float exitRange = enterRange + rangeExitBuffer;

        if (!attackArmed && distanceToRangeCenter >= exitRange)
        {
            attackArmed = true;
        }

        if (attackArmed && distanceToRangeCenter <= enterRange)
        {
            StartTrapAttack();
        }
    }

    private void StartTrapAttack()
    {
        state = TrapState.Opening;
        attackArmed = false;
        rb.velocity = Vector2.zero;

        // Lock target position at trigger moment.
        attackTargetWorldPos = playerTransform.position;

        Vector3 rangeCenterLocal = transform.InverseTransformPoint(GetRangeCenter());
        float span = GetAttackRange() * openRadiusScale;

        trapLeftOpenLocalPos = rangeCenterLocal + Vector3.left * span;
        trapRightOpenLocalPos = rangeCenterLocal + Vector3.right * span;
    }

    private void UpdateOpening()
    {
        rb.velocity = Vector2.zero;

        bool leftReached = MovePartToLocal(trapLeft, trapLeftOpenLocalPos);
        bool rightReached = MovePartToLocal(trapRight, trapRightOpenLocalPos);

        if (leftReached && rightReached)
        {
            state = TrapState.Advancing;
        }
    }

    private void UpdateAdvancing()
    {
        // Keep claws open while advancing forward to the locked target.
        MovePartToLocal(trapLeft, trapLeftOpenLocalPos);
        MovePartToLocal(trapRight, trapRightOpenLocalPos);

        Vector2 nextPos = Vector2.MoveTowards(rb.position, attackTargetWorldPos, advanceSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);

        if (((Vector2)attackTargetWorldPos - rb.position).sqrMagnitude <= 0.01f)
        {
            state = TrapState.Closing;
        }
    }

    private void UpdateClosing()
    {
        rb.velocity = Vector2.zero;

        bool leftReached = MovePartToLocal(trapLeft, trapLeftClosedLocalPos);
        bool rightReached = MovePartToLocal(trapRight, trapRightClosedLocalPos);

        if (leftReached && rightReached)
        {
            bool trapped = IsPlayerTrapped();
            state = (freezeAfterClose && trapped) ? TrapState.LatchedClosed : TrapState.Chasing;
        }
    }

    private void UpdateLatchedClosed()
    {
        rb.velocity = Vector2.zero;
    }

    private void FacePlayer()
    {
        Vector2 toPlayer = playerTransform.position - transform.position;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotateSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextAngle);
    }

    private void FollowPlayer()
    {
        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;
        float stopSqr = stopDistance * stopDistance;

        Vector2 desiredVelocity = sqrDistance > stopSqr ? toPlayer.normalized * moveSpeed : Vector2.zero;
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, followSmooth * Time.fixedDeltaTime);
    }

    private bool MovePartToLocal(Transform part, Vector3 targetLocal)
    {
        if (part == null) return true;

        part.localPosition = Vector3.MoveTowards(part.localPosition, targetLocal, trapMoveSpeed * Time.fixedDeltaTime);
        return (part.localPosition - targetLocal).sqrMagnitude <= 0.0004f;
    }

    private float GetAttackRange()
    {
        if (rangeTrigger == null) return fallbackRange;

        float scale = Mathf.Max(
            Mathf.Abs(rangeTrigger.transform.lossyScale.x),
            Mathf.Abs(rangeTrigger.transform.lossyScale.y)
        );
        return rangeTrigger.radius * scale;
    }

    private Vector3 GetRangeCenter()
    {
        if (rangeTrigger == null) return transform.position;
        return rangeTrigger.transform.TransformPoint(rangeTrigger.offset);
    }

    private void AutoBindReferences()
    {
        if (head == null)
        {
            Transform t = transform.Find("head");
            if (t != null) head = t;
        }

        if (trapLeft == null)
        {
            Transform t = transform.Find("trapL");
            if (t != null) trapLeft = t;
        }

        if (trapRight == null)
        {
            Transform rightA = transform.Find("trapR");
            Transform rightB = transform.Find("tarpR");
            trapRight = rightA != null ? rightA : rightB;
        }

        if (rangeTrigger == null)
        {
            Transform rangeNode = transform.Find("range");
            if (rangeNode != null) rangeTrigger = rangeNode.GetComponent<CircleCollider2D>();
            if (rangeTrigger == null) rangeTrigger = GetComponentInChildren<CircleCollider2D>();
        }
    }

    private void CacheClosedPose()
    {
        if (cachedClosedPose) return;
        if (trapLeft == null || trapRight == null) return;

        trapLeftClosedLocalPos = trapLeft.localPosition;
        trapRightClosedLocalPos = trapRight.localPosition;
        cachedClosedPose = true;
    }

    private void RestoreTrapClosedPose()
    {
        if (!cachedClosedPose) return;
        if (trapLeft != null) trapLeft.localPosition = trapLeftClosedLocalPos;
        if (trapRight != null) trapRight.localPosition = trapRightClosedLocalPos;
    }

    private void EnsureChildHitboxSetup()
    {
        ApplyChildHitboxSetup(trapLeft);
        ApplyChildHitboxSetup(trapRight);
    }

    private void ApplyChildHitboxSetup(Transform part)
    {
        if (part == null) return;
        if (syncChildLayerWithRoot)
        {
            part.gameObject.layer = gameObject.layer;
        }
        if (forceChildEnemyTag)
        {
            part.gameObject.tag = "Enemy";
        }
    }

    private bool IsPlayerTrapped()
    {
        if (playerTransform == null) return false;

        int playerLayer = LayerMask.NameToLayer("Player");
        int playerMask = playerLayer >= 0 ? (1 << playerLayer) : ~0;

        Vector2 center = transform.TransformPoint(lockCheckOffset);
        float angle = transform.eulerAngles.z;

        Collider2D hit = Physics2D.OverlapBox(center, lockCheckSize, angle, playerMask);
        if (hit != null && hit.CompareTag("Player"))
        {
            return true;
        }

        Vector3 localPlayer = transform.InverseTransformPoint(playerTransform.position);
        return Mathf.Abs(localPlayer.x - lockCheckOffset.x) <= lockCheckSize.x * 0.5f
            && Mathf.Abs(localPlayer.y - lockCheckOffset.y) <= lockCheckSize.y * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        AutoBindReferences();
        float r = GetAttackRange();
        Vector3 c = GetRangeCenter();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(c, r);

        float span = r * openRadiusScale;
        Vector3 leftOpenWorld = transform.TransformPoint(transform.InverseTransformPoint(c) + Vector3.left * span);
        Vector3 rightOpenWorld = transform.TransformPoint(transform.InverseTransformPoint(c) + Vector3.right * span);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftOpenWorld, 0.06f);
        Gizmos.DrawSphere(rightOpenWorld, 0.06f);

        Gizmos.color = Color.red;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(lockCheckOffset), Quaternion.Euler(0f, 0f, transform.eulerAngles.z), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, lockCheckSize);
        Gizmos.matrix = old;
    }
}
