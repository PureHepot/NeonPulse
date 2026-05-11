using UnityEngine;

public class VocalistDrillCloneProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private float timer;

    public float speed = 18f;
    public float lifeTime = 4f;
    public int damage = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public void Launch(Vector2 direction, float launchSpeed, float maxLifeTime, int hitDamage)
    {
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;

        speed = launchSpeed;
        lifeTime = maxLifeTime;
        damage = hitDamage;
        timer = 0f;
        transform.SetParent(null);
        rb.velocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(damage, transform);
        }

        Destroy(gameObject);
    }
}
