using UnityEngine;

public class EnemyMiner : EnemyBase
{
    [Header("Wander Around Player")]
    public float wanderInnerRadius = 3f;
    public float wanderOuterRadius = 6f;
    public float arriveDistance = 0.35f;
    public float moveSmooth = 8f;
    public float repathInterval = 1.2f;
    public float edgeMargin = 0.8f;

    [Header("Mine Placement")]
    public Transform mineAnchor;
    public float placeInterval = 1.2f;
    public float mineRegenerateDelay = 2.5f;

    private EnemyMine carriedMine;
    private EnemyMine runtimeMineTemplate;
    private Vector3 mineLocalPos;
    private Quaternion mineLocalRot;

    private Vector2 wanderTarget;
    private float repathTimer;
    private float placeTimer;
    private float regenerateTimer;
    private bool isRegeneratingMine;

    protected override void Awake()
    {
        base.Awake();
        AutoBindMineAnchor();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AutoBindMineAnchor();
        CacheOrCreateMineTemplate();
        EnsureCarriedMine();

        placeTimer = placeInterval;
        regenerateTimer = 0f;
        isRegeneratingMine = false;
        repathTimer = 0f;

        PickNewWanderTarget(true);
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        UpdateWanderMovement();
        UpdateMineLoop();
    }

    private void UpdateWanderMovement()
    {
        repathTimer -= Time.fixedDeltaTime;

        if (repathTimer <= 0f || Vector2.Distance(rb.position, wanderTarget) <= arriveDistance)
        {
            PickNewWanderTarget(false);
        }

        Vector2 toTarget = wanderTarget - rb.position;
        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, moveSmooth * Time.fixedDeltaTime);
            return;
        }

        Vector2 desiredVel = toTarget.normalized * moveSpeed;
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVel, moveSmooth * Time.fixedDeltaTime);
    }

    private void UpdateMineLoop()
    {
        if (carriedMine != null)
        {
            placeTimer -= Time.fixedDeltaTime;
            if (placeTimer <= 0f)
            {
                PlaceMine();
                placeTimer = placeInterval;
            }

            return;
        }

        if (!isRegeneratingMine) return;

        regenerateTimer -= Time.fixedDeltaTime;
        if (regenerateTimer <= 0f)
        {
            RegenerateMine();
        }
    }

    private void PlaceMine()
    {
        if (carriedMine == null) return;

        Transform anchor = mineAnchor != null ? mineAnchor : transform;
        carriedMine.transform.position = anchor.position;
        carriedMine.transform.rotation = anchor.rotation;
        carriedMine.Deploy();
        carriedMine = null;

        isRegeneratingMine = true;
        regenerateTimer = mineRegenerateDelay;
    }

    private void RegenerateMine()
    {
        isRegeneratingMine = false;
        carriedMine = SpawnMineFromTemplate();

        if (carriedMine != null)
        {
            placeTimer = placeInterval;
        }
    }

    private EnemyMine SpawnMineFromTemplate()
    {
        if (runtimeMineTemplate == null) return null;

        Transform anchor = mineAnchor != null ? mineAnchor : transform;
        GameObject mineObj = Instantiate(runtimeMineTemplate.gameObject, anchor);
        mineObj.SetActive(true);

        EnemyMine mine = mineObj.GetComponent<EnemyMine>();
        if (mine != null)
        {
            mine.InitializeAsCarried(anchor, mineLocalPos, mineLocalRot);
        }

        return mine;
    }

    private void EnsureCarriedMine()
    {
        if (mineAnchor == null)
        {
            mineAnchor = transform;
        }

        carriedMine = null;
        EnemyMine[] mines = mineAnchor.GetComponentsInChildren<EnemyMine>(true);
        EnemyMine existing = null;
        for (int i = 0; i < mines.Length; i++)
        {
            if (mines[i] == null || mines[i] == runtimeMineTemplate) continue;
            existing = mines[i];
            break;
        }

        if (existing != null)
        {
            existing.InitializeAsCarried(mineAnchor, mineLocalPos, mineLocalRot);
            carriedMine = existing;
        }

        if (carriedMine == null)
        {
            carriedMine = SpawnMineFromTemplate();
        }
    }

    private void CacheOrCreateMineTemplate()
    {
        Transform anchor = mineAnchor != null ? mineAnchor : transform;

        EnemyMine[] mines = anchor.GetComponentsInChildren<EnemyMine>(true);
        EnemyMine sourceMine = null;
        for (int i = 0; i < mines.Length; i++)
        {
            if (mines[i] == null || mines[i] == runtimeMineTemplate) continue;
            sourceMine = mines[i];
            break;
        }

        if (sourceMine == null)
        {
            return;
        }

        mineLocalPos = sourceMine.transform.localPosition;
        mineLocalRot = sourceMine.transform.localRotation;

        if (runtimeMineTemplate != null) return;

        GameObject templateObj = Instantiate(sourceMine.gameObject, transform);
        templateObj.name = sourceMine.gameObject.name + "_TemplateRuntime";
        templateObj.SetActive(false);
        runtimeMineTemplate = templateObj.GetComponent<EnemyMine>();
    }

    private void AutoBindMineAnchor()
    {
        if (mineAnchor != null) return;

        Transform t = transform.Find("mineAnchor");
        if (t == null) t = transform.Find("MineAnchor");
        if (t == null) t = transform.Find("mine");
        if (t == null) t = transform;

        mineAnchor = t;
    }

    private void PickNewWanderTarget(bool forceImmediate)
    {
        if (playerTransform == null)
        {
            wanderTarget = rb.position;
            return;
        }

        float inner = Mathf.Min(wanderInnerRadius, wanderOuterRadius);
        float outer = Mathf.Max(wanderInnerRadius, wanderOuterRadius);

        Vector2 chosen = rb.position;
        for (int i = 0; i < 8; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(inner, outer);

            Vector2 rawTarget = (Vector2)playerTransform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            chosen = ClampToCamera(rawTarget);

            if (Vector2.Distance(chosen, rb.position) > arriveDistance * 1.5f)
            {
                break;
            }
        }

        wanderTarget = chosen;
        repathTimer = forceImmediate ? 0.05f : repathInterval;
    }

    private Vector2 ClampToCamera(Vector2 pos)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = cam.transform.position.x - halfW + edgeMargin;
        float maxX = cam.transform.position.x + halfW - edgeMargin;
        float minY = cam.transform.position.y - halfH + edgeMargin;
        float maxY = cam.transform.position.y + halfH - edgeMargin;

        return new Vector2(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY)
        );
    }
}
