using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour, IPoolable
{
    [Header("Basic Stats")]
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 5f;

    [Header("Reflection Stats")]
    public float reflectedSpeedMultiplier = 1.5f;
    public Color reflectedColor = Color.cyan;
    public int reflectedDamage = 2;

    private Rigidbody2D rb;
    private TrailRenderer trail;
    private float timer;
    private Vector2 moveDir;
    private bool isReflected = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void OnSpawn()
    {
        timer = 0f;
        isReflected = false;
        if (trail) trail.Clear();

        GetComponent<SpriteRenderer>().color = Color.yellow;
        if (trail) trail.startColor = Color.yellow;

        moveDir = transform.right;
    }

    public void OnDespawn()
    {
        rb.velocity = Vector2.zero;
    }

    void Update()
    {
        // 简单的位移
        // 注意：反弹后我们会修改 transform.right，所以始终沿着 right 走即可
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isReflected)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponentInChildren<HealthModule>();
                if (health != null)
                {
                    health.TakeDamage(damage, transform);
                    ObjectPoolManager.Instance.Return(this.gameObject);
                }
            }
            else if (other.GetComponent<ShieldController>())
            {
                Reflect(other.transform.position);
            }
        }
        else
        {
            if (other.CompareTag("Enemy"))
            {
                var enemy = other.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    Vector3 hitPoint = other.ClosestPoint(transform.position);
                    Vector3 hitNormal = (transform.position - hitPoint).normalized;

                    enemy.TakeDamage(reflectedDamage, hitPoint, hitNormal);

                    ObjectPoolManager.Instance.Return(this.gameObject);
                }
            }
        }
    }

    void Reflect(Vector3 shieldPos)
    {
        isReflected = true;

        Vector2 normal = (transform.position - shieldPos).normalized;

        Vector2 incomingDir = transform.right;

        Vector2 reflectDir = Vector2.Reflect(incomingDir, normal).normalized;

        float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        speed *= reflectedSpeedMultiplier;
        timer = 0f; // 重置计时，让它能飞得更远

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = reflectedColor;
        if (trail) trail.startColor = reflectedColor;

        this.gameObject.layer = LayerMask.NameToLayer("PlayerBullet"); 
    }
}