using UnityEngine;

public class BouncingDrill : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 lastVelocity;
    private Transform returnAnchor;
    private bool isLaunched;
    private float flightTimer;

    public float speed = 15f;
    public int maxBounces = 5;
    public float maxFlightTime = 5f;
    public int damage = 1;

    private int currentBounces = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public void Launch(Vector2 direction, Transform anchor = null)
    {
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;

        returnAnchor = anchor;
        currentBounces = 0;
        flightTimer = 0f;
        isLaunched = true;
        transform.SetParent(null);
        rb.velocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (!isLaunched) return;

        flightTimer += Time.deltaTime;
        lastVelocity = rb.velocity;

        if (flightTimer >= maxFlightTime)
        {
            ReturnToAnchor();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLaunched) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(damage, transform);
            return;
        }

        if (collision.contactCount == 0 || lastVelocity.sqrMagnitude < 0.001f)
        {
            ReturnToAnchor();
            return;
        }

        float currentSpeed = lastVelocity.magnitude;
        Vector2 direction = Vector2.Reflect(lastVelocity.normalized, collision.contacts[0].normal);
        rb.velocity = direction * Mathf.Max(currentSpeed, speed * 0.65f);

        currentBounces++;
        if (currentBounces >= maxBounces)
        {
            ReturnToAnchor();
        }
    }

    public void ReturnToAnchor()
    {
        isLaunched = false;
        rb.velocity = Vector2.zero;

        if (returnAnchor == null) return;

        transform.SetParent(returnAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
