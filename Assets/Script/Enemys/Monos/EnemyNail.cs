using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyNail : MonoBehaviour
{
    [Header("Nail Stats")]
    public float speed = 12f;
    public int damage = 1;
    public float lifeTime = 6f;
    public float outOfViewMargin = 0.2f;

    private Rigidbody2D rb;
    private float lifeTimer;
    private bool isLaunched;
    private Transform ownerRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    private void OnEnable()
    {
        ResetProjectile();
    }

    public void ResetProjectile()
    {
        lifeTimer = 0f;
        isLaunched = false;
        ownerRoot = null;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void Launch(float launchSpeed, int launchDamage, float launchLifeTime, Transform owner)
    {
        speed = launchSpeed;
        damage = launchDamage;
        lifeTime = launchLifeTime;
        ownerRoot = owner;

        isLaunched = true;
        lifeTimer = 0f;
        transform.SetParent(null, true);
        rb.velocity = transform.up * speed;
    }

    private void Update()
    {
        if (!isLaunched) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime || IsOutOfView())
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject target)
    {
        if (!isLaunched || target == null) return;

        if (ownerRoot != null && target.transform.root == ownerRoot)
        {
            return;
        }

        if (target.CompareTag("Player"))
        {
            target.GetComponentInChildren<HealthModule>()?.TakeDamage(damage, transform);
        }

        Destroy(gameObject);
    }

    private bool IsOutOfView()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        return viewport.z < 0f
            || viewport.x < -outOfViewMargin
            || viewport.x > 1f + outOfViewMargin
            || viewport.y < -outOfViewMargin
            || viewport.y > 1f + outOfViewMargin;
    }
}
