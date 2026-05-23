using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyArcherArrow : MonoBehaviour, IDamageable
{
    [Header("Arrow Damage")]
    public int contactDamage = 1;

    private EnemyArcher owner;
    private Rigidbody2D rb;

    private bool isAttached;
    private bool isFlying;
    private bool isStopped;

    private Vector2 launchDir;
    private float flySpeed;
    private float stopDistanceFromPlayer;
    private float minFlightTime;
    private float maxFlightTime;
    private float flightTimer;

    public bool IsAttached => isAttached;
    public bool IsFlying => isFlying;
    public bool IsStopped => isStopped;
    public EnemyArcher Owner => owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.zero;
    }

    public void InitializeOwner(EnemyArcher newOwner)
    {
        owner = newOwner;
        EnsureEnemyTagAndLayer();
    }

    public void AttachTo(Transform parent, Vector3 localPos, Quaternion localRot)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        isAttached = true;
        isFlying = false;
        isStopped = false;
        flightTimer = 0f;

        if (parent != null)
        {
            transform.SetParent(parent, false);
            transform.localPosition = localPos;
            transform.localRotation = localRot;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;
    }

    public void Launch(Vector2 dir, float speed, float stopDistance, float minTime, float maxTime)
    {
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector2.up;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        isAttached = false;
        isFlying = true;
        isStopped = false;
        flightTimer = 0f;

        launchDir = dir.normalized;
        flySpeed = Mathf.Max(0f, speed);
        stopDistanceFromPlayer = Mathf.Max(0.1f, stopDistance);
        minFlightTime = Mathf.Max(0f, minTime);
        maxFlightTime = Mathf.Max(minFlightTime + 0.1f, maxTime);

        transform.SetParent(null, true);
        transform.up = launchDir;
        rb.simulated = true;
        rb.velocity = Vector2.zero;
    }

    public void ForceStop()
    {
        isAttached = false;
        isFlying = false;
        isStopped = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;
    }

    private void Update()
    {
        if (!isFlying) return;

        flightTimer += Time.deltaTime;

        Transform player = owner != null ? owner.PlayerTransform : null;
        if (player != null && flightTimer >= minFlightTime)
        {
            Vector2 toPlayer = (Vector2)transform.position - (Vector2)player.position;
            float forwardDot = Vector2.Dot(toPlayer, launchDir);
            float distance = toPlayer.magnitude;

            // Arrow stops after it has passed the player and is far enough away.
            if (forwardDot > 0f && distance >= stopDistanceFromPlayer)
            {
                ForceStop();
                return;
            }
        }

        if (flightTimer >= maxFlightTime)
        {
            ForceStop();
        }
    }

    private void FixedUpdate()
    {
        if (!isFlying) return;
        rb.MovePosition(rb.position + launchDir * flySpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (owner == null) return;
        owner.ApplySharedDamageFromArrow(amount, transform.position, -transform.up);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (owner == null) return;
        owner.ApplySharedDamageFromArrow(amount, hitPoint, hitNormal);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (target == null) return;

        if (owner != null && target.transform.root == owner.transform.root)
        {
            return;
        }

        if (!target.CompareTag("Player")) return;

        target.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }

    private void EnsureEnemyTagAndLayer()
    {
        gameObject.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            gameObject.layer = enemyLayer;
        }
    }
}
