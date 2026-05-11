using UnityEngine;

public class EnemySaber : EnemyBase
{
    [Header("Movement")]
    public float followSmooth = 8f;
    public float stopDistance = 0.15f;
    public float followRotateSpeed = 540f;

    [Header("Attack")]
    public float attackSpinSpeed = 1080f;
    public float attackBrake = 20f;
    public float fallbackAttackRange = 2f;
    public float rangeExitBuffer = 0.25f;

    [Header("Range Detector (Child)")]
    public CircleCollider2D rangeTrigger;

    private bool isAttacking;
    private float spinRemainingAngle;
    private float cachedAttackRange;
    private bool spinArmed;

    public override void OnSpawn()
    {
        base.OnSpawn();
        TryAutoBindRangeTrigger();
        isAttacking = false;
        spinRemainingAngle = 0f;
        cachedAttackRange = GetAttackRange();
        spinArmed = true;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(playerTransform.position, transform.position);
        float enterRange = cachedAttackRange;
        float exitRange = cachedAttackRange + rangeExitBuffer;

        if (isAttacking)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * attackBrake);
            float step = attackSpinSpeed * Time.fixedDeltaTime;
            float delta = Mathf.Min(step, spinRemainingAngle);
            spinRemainingAngle -= delta;

            rb.MoveRotation(rb.rotation - delta);

            if (spinRemainingAngle <= 0.01f)
            {
                isAttacking = false;
            }
            return;
        }

        if (!spinArmed && distance >= exitRange)
        {
            spinArmed = true;
        }

        if (spinArmed && distance <= enterRange)
        {
            isAttacking = true;
            spinArmed = false;
            spinRemainingAngle = 360f;
            return;
        }

        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;
        Vector2 desiredVelocity = sqrDistance > stopDistance * stopDistance ? toPlayer.normalized * moveSpeed : Vector2.zero;

        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, Time.fixedDeltaTime * followSmooth);

        if (sqrDistance > 0.0001f)
        {
            // Follow state: keep saber hilt facing player. Sprite forward is assumed to be +Y.
            float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, followRotateSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(nextAngle);
        }
    }

    private float GetAttackRange()
    {
        if (rangeTrigger == null)
        {
            return fallbackAttackRange;
        }

        float scale = Mathf.Max(Mathf.Abs(rangeTrigger.transform.lossyScale.x), Mathf.Abs(rangeTrigger.transform.lossyScale.y));
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
        if (rangeTrigger == null) return;

        float rangeRadius = GetAttackRange();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeRadius);
    }
}
