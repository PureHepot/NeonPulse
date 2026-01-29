using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemySpinBullet : MonoBehaviour, IPoolable
{
    [Header("Basic Stats")]
    public float speed = 6f;
    public int damage = 1;
    public float lifeTime = 3f;

    [Header("Spin Visual")]
    public Transform visual;
    public float spinSpeed = 360f;

    [Header("Reflection Stats")]
    public float reflectedSpeedMultiplier = 1.5f;
    public Color reflectedColor = Color.cyan;
    public int reflectedDamage = 2;

    private float timer;
    private bool isReflected;

    private SpriteRenderer sr;
    private TrailRenderer trail;

    void Awake()
    {
        if (visual == null)
            visual = transform.Find("Visual");

        if (visual != null)
        {
            sr = visual.GetComponent<SpriteRenderer>();
            trail = visual.GetComponent<TrailRenderer>();
        }
    }

    public void OnSpawn()
    {
        timer = 0;
        isReflected = false;

        if (trail) trail.Clear();
        if (sr) sr.color = Color.yellow;
        if (trail) trail.startColor = Color.yellow;
    }

    public void OnDespawn() { }

    void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;

        if (visual != null)
            visual.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            ObjectPoolManager.Instance.Return(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isReflected)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponentInChildren<HealthModule>()
                    ?.TakeDamage(damage, transform);

                ObjectPoolManager.Instance.Return(gameObject);
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
                other.GetComponent<IDamageable>()
                    ?.TakeDamage(reflectedDamage, transform.position, transform.right);

                ObjectPoolManager.Instance.Return(gameObject);
            }
        }
    }

    void Reflect(Vector3 shieldPos)
    {
        isReflected = true;

        Vector2 normal = (transform.position - shieldPos).normalized;
        Vector2 reflectDir = Vector2.Reflect(transform.right, normal);

        float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        speed *= reflectedSpeedMultiplier;
        timer = 0f;

        if (sr) sr.color = reflectedColor;
        if (trail) trail.startColor = reflectedColor;

        gameObject.layer = LayerMask.NameToLayer("PlayerBullet");
    }
}
