using DG.Tweening; // 闇€瑕?DoTween
using UnityEngine;

public class EnemyCrasher : EnemyBase
{
    [Header("Movement Settings")]
    public float enterSpeed = 5f;    // 鍏ュ満閫熷害
    public Vector2 safeAreaSize = new Vector2(8, 5);

    [Header("Dasher Settings")]
    public float aimDuration = 1.0f; // 鐬勫噯/棰勮鏃堕棿
    public float dashSpeed = 20f;    // 鍐插埡閫熷害
    public float dashDistance = 30f; // 鍐插埡澶氳繙
    public float dashDuration = 2.0f;

    [Header("Homing Settings")]
    public float homingStrength = 2.0f;

    [Header("References")]
    public LineRenderer warningLine;

    // 鐘舵€?
    private enum State { Entering, Spawning, Aiming, Dashing, Idle }
    private State currentState;
    private Vector2 dashDirection;
    private float stateTimer;

    public override void OnSpawn()
    {
        base.OnSpawn();

        currentState = State.Spawning;
        stateTimer = 0;

        if (warningLine) warningLine.enabled = false;

        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.5f).OnComplete(StartEntering);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        if (warningLine) warningLine.enabled = false;
        transform.DOKill();
    }

    protected override void MoveBehavior()
    {
        switch (currentState)
        {
            case State.Entering:
                break;

            case State.Aiming:
                UpdateAiming();
                break;

            case State.Dashing:
                UpdateDashing();
                break;
        }
    }

    void StartAiming()
    {
        currentState = State.Aiming;
        stateTimer = aimDuration;

        if (warningLine)
        {
            warningLine.enabled = true;
            UpdateWarningLine();
        }

        // 鍙互鍦ㄨ繖閲屾挱鏀句竴涓搫鍔涢煶鏁?
    }

    void StartEntering()
    {
        currentState = State.Entering;

        if (playerTransform != null)
        {
            Vector2 lookdir = (playerTransform.position - transform.position).normalized;

            float angle = Mathf.Atan2(lookdir.y, lookdir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        float halfW = safeAreaSize.x / 2f;
        float halfH = safeAreaSize.y / 2f;

        float targetX = Mathf.Clamp(transform.position.x, -halfW, halfW);
        float targetY = Mathf.Clamp(transform.position.y, -halfH, halfH);
        Vector3 targetPos = new Vector3(targetX, targetY, 0);

        float distance = Vector3.Distance(transform.position, targetPos);
        float duration = distance / enterSpeed;

        transform.DOMove(targetPos, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(StartAiming);
    }

    void UpdateAiming()
    {
        stateTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            dashDirection = (playerTransform.position - transform.position).normalized;

            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); 
        }
        else
        {
            dashDirection = Vector2.left; // 娌＄帺瀹跺氨鍚戝乏
        }

        UpdateWarningLine();

        if (stateTimer <= 0)
        {
            StartDash();
        }
    }

    void UpdateWarningLine()
    {
        if (warningLine == null) return;

        // 璧风偣锛氳嚜宸?
        warningLine.SetPosition(0, transform.position);

        // 缁堢偣锛氭部鍐插埡鏂瑰悜寤朵几 dashDistance 杩欎箞杩?
        Vector3 endPos = transform.position + (Vector3)dashDirection * dashDistance;
        warningLine.SetPosition(1, endPos);
    }

    void StartDash()
    {
        currentState = State.Dashing;
        stateTimer = dashDuration;

        if (warningLine) warningLine.enabled = false;

        // 鎾斁鍐插埡鍔ㄧ敾
        transform.DOPunchScale(new Vector3(0.5f, -0.2f, 0), 0.2f, 10, 1); 
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        int hitLayer = collision.gameObject.layer;
        int wallLayer = LayerMask.NameToLayer("ArenaWall");

        if (currentState == State.Dashing)
        {
            if (hitLayer == wallLayer)
            {
                Die();
            }
        }
    }

    void UpdateDashing()
    {
        stateTimer -= Time.deltaTime;

        // 淇鏂瑰悜
        if (playerTransform != null && homingStrength > 0)
        {
            // 璁＄畻鐞嗘兂鐨勭洰鏍囨柟鍚?
            Vector2 targetDir = (playerTransform.position - transform.position).normalized;

            float maxRadiansDelta = homingStrength * Time.deltaTime;
            Vector3 newDir = Vector3.RotateTowards(dashDirection, targetDir, homingStrength * Time.deltaTime, maxRadiansDelta);

            dashDirection = newDir.normalized;
        }

        DriveVelocity(dashDirection * dashSpeed, 3f);

        RotateTowards(dashDirection);

        if (stateTimer <= 0)
        {
            Die();
        }
    }
    void RotateTowards(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
