using UnityEngine;

public class EnemyBoomerange : EnemyBase
{
    private enum BoomerangeState
    {
        SeekPlayer,
        SAttack
    }

    [Header("Seek")]
    public float seekSmooth = 8f;
    public float seekRotateSpeed = 720f;

    [Header("Attack Range")]
    public CircleCollider2D rangeTrigger;
    public float fallbackAttackRange = 3f;

    [Header("S Attack")]
    public float attackMoveSpeed = 10f;
    public float sAmplitude = 1.2f;
    public float attackSpinSpeed = 1080f;
    public float minSegmentDuration = 0.25f;

    private BoomerangeState state;
    private float cachedAttackRange;

    private Vector2 segmentStart;
    private Vector2 segmentEnd;
    private float segmentDuration;
    private float segmentTimer;
    private int sMirrorSign = 1;

    public override void OnSpawn()
    {
        base.OnSpawn();
        TryAutoBindRangeTrigger();
        cachedAttackRange = GetAttackRange();
        state = BoomerangeState.SeekPlayer;
        segmentTimer = 0f;
        rb.velocity = Vector2.zero;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        switch (state)
        {
            case BoomerangeState.SeekPlayer:
                UpdateSeek();
                break;
            case BoomerangeState.SAttack:
                UpdateSAttack();
                break;
        }
    }

    private void UpdateSeek()
    {
        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqr = toPlayer.sqrMagnitude;
        if (sqr > 0.0001f)
        {
            Vector2 desiredVelocity = toPlayer.normalized * moveSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, seekSmooth * Time.fixedDeltaTime);
            FaceDirection(rb.velocity.sqrMagnitude > 0.001f ? rb.velocity : toPlayer);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, seekSmooth * Time.fixedDeltaTime);
        }

        if (IsPlayerInAttackRange())
        {
            sMirrorSign = 1;
            StartSegmentAroundPlayer(rb.position);
            state = BoomerangeState.SAttack;
        }
    }

    private void UpdateSAttack()
    {
        segmentTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(segmentTimer / segmentDuration);

        Vector2 axis = (segmentEnd - segmentStart).normalized;
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = transform.up;
        }

        Vector2 perpendicular = new Vector2(-axis.y, axis.x) * sMirrorSign;
        Vector2 basePos = Vector2.Lerp(segmentStart, segmentEnd, t);
        float wave = Mathf.Sin(t * Mathf.PI * 2f) * sAmplitude;
        Vector2 nextPos = basePos + perpendicular * wave;

        rb.velocity = Vector2.zero;
        rb.MovePosition(nextPos);
        rb.MoveRotation(rb.rotation - attackSpinSpeed * Time.fixedDeltaTime);

        if (t >= 1f)
        {
            rb.MovePosition(segmentEnd);

            if (IsPlayerInAttackRange())
            {
                sMirrorSign *= -1;
                StartSegmentAroundPlayer(segmentEnd);
            }
            else
            {
                rb.velocity = Vector2.zero;
                state = BoomerangeState.SeekPlayer;
            }
        }
    }

    private void StartSegmentAroundPlayer(Vector2 startPoint)
    {
        segmentStart = startPoint;
        Vector2 playerCenter = playerTransform.position;

        // The player is the midpoint of each S segment.
        segmentEnd = playerCenter * 2f - segmentStart;

        float len = Vector2.Distance(segmentStart, segmentEnd);
        segmentDuration = Mathf.Max(minSegmentDuration, len / Mathf.Max(attackMoveSpeed, 0.01f));
        segmentTimer = 0f;
    }

    private bool IsPlayerInAttackRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= cachedAttackRange;
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, seekRotateSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextAngle);
    }

    private float GetAttackRange()
    {
        if (rangeTrigger == null) return fallbackAttackRange;

        float scale = Mathf.Max(
            Mathf.Abs(rangeTrigger.transform.lossyScale.x),
            Mathf.Abs(rangeTrigger.transform.lossyScale.y)
        );

        return rangeTrigger.radius * scale;
    }

    private void TryAutoBindRangeTrigger()
    {
        if (rangeTrigger != null) return;

        Transform rangeNode = transform.Find("range");
        if (rangeNode != null)
        {
            rangeTrigger = rangeNode.GetComponent<CircleCollider2D>();
        }

        if (rangeTrigger == null)
        {
            rangeTrigger = GetComponentInChildren<CircleCollider2D>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        float range = rangeTrigger != null ? GetAttackRange() : fallbackAttackRange;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
