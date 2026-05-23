using UnityEngine;

public class EnemyArcher : EnemyBase
{
    private enum ArcherState
    {
        Shooting,
        SeekingArrow
    }

    [Header("References")]
    public Transform arrowAnchor;
    public GameObject arrowPrefab;
    public string arrowResourcePath = "Prefabs/Mono/Enemys/arrow";

    [Header("Charge & Warning")]
    public float chargeDuration = 1.1f;
    public float aimRotateSpeed = 720f;
    public float warningLength = 20f;
    public GameObject warningLinePrefab;
    public string warningLineResourcePath = "ParticleSystem/VFX_WarningLine";

    [Header("Attack Range")]
    public CircleCollider2D rangeTrigger;
    public float fallbackAttackRange = 4f;

    [Header("Arrow Launch")]
    public float arrowSpeed = 22f;
    public float arrowStopDistanceFromPlayer = 3.2f;
    public float arrowMinFlightTime = 0.15f;
    public float arrowMaxFlightTime = 4.0f;
    public int arrowContactDamage = 1;

    [Header("Seek Arrow")]
    public float seekArrowSpeed = 10f;
    public float combineDistance = 0.45f;

    private ArcherState state;
    private float chargeTimer;
    private Vector2 cachedAimDir;
    private float cachedAttackRange;

    private EnemyArcherArrow currentArrow;
    private EnemyArcherArrow runtimeArrowTemplate;
    private Vector3 arrowLocalPos;
    private Quaternion arrowLocalRot;

    private LineRenderer warningLine;
    private GameObject warningLineObj;

    public Transform PlayerTransform => playerTransform;

    protected override void Awake()
    {
        base.Awake();
        AutoBindArrowAnchor();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AutoBindArrowAnchor();

        CacheArrowTemplate();
        EnsureArrowAvailable();
        EnsureWarningLine();
        TryAutoBindRangeTrigger();
        cachedAttackRange = GetAttackRange();

        state = ArcherState.Shooting;
        chargeTimer = chargeDuration;
        cachedAimDir = transform.up;
        rb.velocity = Vector2.zero;

        SetWarningVisible(false);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        rb.velocity = Vector2.zero;

        SetWarningVisible(false);
        CleanupWarningLine();
        CleanupArrowObjects();
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null)
        {
            rb.velocity = Vector2.zero;
            SetWarningVisible(false);
            return;
        }

        switch (state)
        {
            case ArcherState.Shooting:
                UpdateShootingState();
                break;
            case ArcherState.SeekingArrow:
                UpdateSeekingArrowState();
                break;
        }
    }

    public void ApplySharedDamageFromArrow(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;
        TakeDamage(amount, hitPoint, hitNormal);
    }

    protected override void Die()
    {
        CleanupArrowObjects();
        CleanupWarningLine();
        base.Die();
    }

    private void UpdateShootingState()
    {
        if (currentArrow == null)
        {
            state = ArcherState.SeekingArrow;
            SetWarningVisible(false);
            return;
        }

        if (!currentArrow.IsAttached)
        {
            state = ArcherState.SeekingArrow;
            SetWarningVisible(false);
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        bool inRange = toPlayer.magnitude <= cachedAttackRange;

        if (!inRange)
        {
            Vector2 chaseDir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.up;
            Vector2 desiredVelocity = chaseDir * moveSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, 8f * Time.fixedDeltaTime);
            FaceDirection(rb.velocity.sqrMagnitude > 0.001f ? rb.velocity : chaseDir, aimRotateSpeed);

            chargeTimer = chargeDuration;
            SetWarningVisible(false);
            return;
        }

        rb.velocity = Vector2.zero;

        cachedAimDir = toPlayer.normalized;
        if (cachedAimDir.sqrMagnitude < 0.0001f) cachedAimDir = transform.up;

        FaceDirection(cachedAimDir, aimRotateSpeed);

        chargeTimer -= Time.fixedDeltaTime;
        SetWarningVisible(true);
        UpdateWarningLine(cachedAimDir);

        if (chargeTimer > 0f) return;

        FireArrow(transform.up);
        SetWarningVisible(false);
        state = ArcherState.SeekingArrow;
    }

    private void UpdateSeekingArrowState()
    {
        SetWarningVisible(false);

        if (currentArrow == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toArrow = (Vector2)currentArrow.transform.position - rb.position;
        float distance = toArrow.magnitude;

        if (!currentArrow.IsFlying && distance <= combineDistance)
        {
            CombineWithArrow();
            return;
        }

        Vector2 dir = distance > 0.0001f ? toArrow / distance : Vector2.zero;
        rb.velocity = dir * seekArrowSpeed;
        FaceDirection(dir, aimRotateSpeed);
    }

    private void FireArrow(Vector2 dir)
    {
        if (currentArrow == null) return;

        currentArrow.contactDamage = arrowContactDamage;
        currentArrow.Launch(dir, arrowSpeed, arrowStopDistanceFromPlayer, arrowMinFlightTime, arrowMaxFlightTime);
    }

    private void CombineWithArrow()
    {
        if (currentArrow == null) return;

        Transform anchor = arrowAnchor != null ? arrowAnchor : transform;
        currentArrow.InitializeOwner(this);
        currentArrow.contactDamage = arrowContactDamage;
        currentArrow.AttachTo(anchor, arrowLocalPos, arrowLocalRot);

        rb.velocity = Vector2.zero;
        chargeTimer = chargeDuration;
        state = ArcherState.Shooting;
    }

    private void FaceDirection(Vector2 dir, float rotateSpeed)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotateSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextAngle);
    }

    private void AutoBindArrowAnchor()
    {
        if (arrowAnchor != null) return;

        Transform t = transform.Find("arrowAnchor");
        if (t == null) t = transform.Find("ArrowAnchor");
        if (t == null) t = transform.Find("arrow");
        if (t == null) t = transform;
        arrowAnchor = t;
    }

    private void CacheArrowTemplate()
    {
        if (runtimeArrowTemplate != null) return;

        Transform anchor = arrowAnchor != null ? arrowAnchor : transform;
        EnemyArcherArrow sourceArrow = anchor.GetComponentInChildren<EnemyArcherArrow>(true);

        if (sourceArrow == null && arrowPrefab == null)
        {
            arrowPrefab = Resources.Load<GameObject>(arrowResourcePath);
        }

        if (sourceArrow == null && arrowPrefab != null)
        {
            GameObject temp = Instantiate(arrowPrefab, anchor);
            sourceArrow = temp.GetComponent<EnemyArcherArrow>();
            if (sourceArrow == null)
            {
                Destroy(temp);
                return;
            }

            sourceArrow.transform.localPosition = Vector3.zero;
            sourceArrow.transform.localRotation = Quaternion.identity;
        }

        if (sourceArrow == null) return;

        arrowLocalPos = sourceArrow.transform.localPosition;
        arrowLocalRot = sourceArrow.transform.localRotation;

        GameObject templateObj = Instantiate(sourceArrow.gameObject, transform);
        templateObj.name = sourceArrow.gameObject.name + "_TemplateRuntime";
        templateObj.SetActive(false);
        runtimeArrowTemplate = templateObj.GetComponent<EnemyArcherArrow>();
        if (runtimeArrowTemplate == null)
        {
            Destroy(templateObj);
            return;
        }
        runtimeArrowTemplate.InitializeOwner(this);
        runtimeArrowTemplate.contactDamage = arrowContactDamage;
    }

    private void EnsureArrowAvailable()
    {
        Transform anchor = arrowAnchor != null ? arrowAnchor : transform;
        currentArrow = null;

        EnemyArcherArrow[] arrows = anchor.GetComponentsInChildren<EnemyArcherArrow>(true);
        for (int i = 0; i < arrows.Length; i++)
        {
            EnemyArcherArrow candidate = arrows[i];
            if (candidate == null || candidate == runtimeArrowTemplate) continue;

            currentArrow = candidate;
            break;
        }

        if (currentArrow == null && runtimeArrowTemplate != null)
        {
            GameObject arrowObj = Instantiate(runtimeArrowTemplate.gameObject, anchor);
            arrowObj.SetActive(true);
            currentArrow = arrowObj.GetComponent<EnemyArcherArrow>();
        }

        if (currentArrow != null)
        {
            currentArrow.InitializeOwner(this);
            currentArrow.contactDamage = arrowContactDamage;
            currentArrow.AttachTo(anchor, arrowLocalPos, arrowLocalRot);
        }
    }

    private void EnsureWarningLine()
    {
        if (warningLineObj != null && warningLine != null) return;

        if (warningLinePrefab == null)
        {
            warningLinePrefab = Resources.Load<GameObject>(warningLineResourcePath);
        }

        if (warningLinePrefab != null)
        {
            warningLineObj = Instantiate(warningLinePrefab, transform.position, Quaternion.identity, transform);
            warningLineObj.name = "WarningLineRuntime";
            warningLine = warningLineObj.GetComponent<LineRenderer>();
        }

        if (warningLine == null)
        {
            warningLineObj = new GameObject("WarningLineRuntime");
            warningLineObj.transform.SetParent(transform, false);
            warningLine = warningLineObj.AddComponent<LineRenderer>();
            warningLine.positionCount = 2;
        }

        warningLine.useWorldSpace = true;
        warningLine.positionCount = 2;
        warningLine.enabled = false;
    }

    private void SetWarningVisible(bool visible)
    {
        if (warningLine == null) return;
        warningLine.enabled = visible;
    }

    private void UpdateWarningLine(Vector2 dir)
    {
        if (warningLine == null) return;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(dir.normalized * warningLength);
        warningLine.SetPosition(0, start);
        warningLine.SetPosition(1, end);
    }

    private void CleanupWarningLine()
    {
        if (warningLineObj != null)
        {
            Destroy(warningLineObj);
        }

        warningLineObj = null;
        warningLine = null;
    }

    private void CleanupArrowObjects()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow.gameObject);
        }

        currentArrow = null;

        if (runtimeArrowTemplate != null)
        {
            Destroy(runtimeArrowTemplate.gameObject);
        }

        runtimeArrowTemplate = null;
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

    private float GetAttackRange()
    {
        if (rangeTrigger == null) return fallbackAttackRange;

        float scale = Mathf.Max(
            Mathf.Abs(rangeTrigger.transform.lossyScale.x),
            Mathf.Abs(rangeTrigger.transform.lossyScale.y)
        );

        return rangeTrigger.radius * scale;
    }
}
