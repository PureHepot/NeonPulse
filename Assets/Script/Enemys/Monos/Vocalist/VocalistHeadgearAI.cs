using UnityEngine;

public class VocalistHeadgearAI : MonoBehaviour
{
    private enum HeadgearState
    {
        Held,
        Flying,
        Docked
    }

    private Rigidbody2D rb;
    private VocalistBoss owner;
    private HeadgearState state = HeadgearState.Held;
    private float stateTimer;
    private Vector2 arenaHalfSize;

    public int damage = 1;
    public float speed = 9f;
    public float flightDuration = 5f;
    public float dockWaitTime = 4f;
    public float spinSpeed = 540f;
    public Vector2 fallbackArenaHalfSize = new Vector2(9f, 5f);

    public bool IsDocked => state == HeadgearState.Docked;
    public Vector3 DockPosition => transform.position;
    public float DockProgress => dockWaitTime <= 0f ? 1f : Mathf.Clamp01(stateTimer / dockWaitTime);
    public bool CanBeRecalledEarly => IsDocked && DockProgress >= 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public void Initialize(VocalistBoss boss)
    {
        owner = boss;
        arenaHalfSize = GetArenaHalfSize();
        Hold(owner != null ? owner.headgearHome : null);
    }

    public void Hold(Transform parent)
    {
        state = HeadgearState.Held;
        stateTimer = 0f;
        rb.velocity = Vector2.zero;

        if (parent != null)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public void Throw(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;

        transform.SetParent(null);
        arenaHalfSize = GetArenaHalfSize();
        state = HeadgearState.Flying;
        stateTimer = 0f;
        rb.velocity = direction.normalized * speed;
    }

    private void Update()
    {
        switch (state)
        {
            case HeadgearState.Flying:
                UpdateFlying();
                break;
            case HeadgearState.Docked:
                UpdateDocked();
                break;
        }
    }

    private void UpdateFlying()
    {
        stateTimer += Time.deltaTime;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        Vector3 pos = transform.position;
        Vector2 velocity = rb.velocity;

        if (Mathf.Abs(pos.x) >= arenaHalfSize.x)
        {
            velocity.x = -Mathf.Sign(pos.x) * Mathf.Abs(velocity.x);
            pos.x = Mathf.Sign(pos.x) * arenaHalfSize.x;
        }

        if (Mathf.Abs(pos.y) >= arenaHalfSize.y)
        {
            velocity.y = -Mathf.Sign(pos.y) * Mathf.Abs(velocity.y);
            pos.y = Mathf.Sign(pos.y) * arenaHalfSize.y;
        }

        transform.position = pos;
        rb.velocity = velocity.sqrMagnitude < 0.001f ? Random.insideUnitCircle.normalized * speed : velocity.normalized * speed;

        if (stateTimer >= flightDuration)
        {
            DockAtRandomEdge();
        }
    }

    private void UpdateDocked()
    {
        stateTimer += Time.deltaTime;
        transform.Rotate(0f, 0f, spinSpeed * 0.25f * Time.deltaTime);

        if (stateTimer >= dockWaitTime)
        {
            Throw(Random.insideUnitCircle.normalized);
        }
    }

    private void DockAtRandomEdge()
    {
        state = HeadgearState.Docked;
        stateTimer = 0f;
        rb.velocity = Vector2.zero;

        int edge = Random.Range(0, 4);
        Vector3 pos = transform.position;
        switch (edge)
        {
            case 0:
                pos = new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x), arenaHalfSize.y, 0f);
                break;
            case 1:
                pos = new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x), -arenaHalfSize.y, 0f);
                break;
            case 2:
                pos = new Vector3(-arenaHalfSize.x, Random.Range(-arenaHalfSize.y, arenaHalfSize.y), 0f);
                break;
            default:
                pos = new Vector3(arenaHalfSize.x, Random.Range(-arenaHalfSize.y, arenaHalfSize.y), 0f);
                break;
        }

        transform.position = pos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != HeadgearState.Flying) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(damage, transform);
            return;
        }

        if (collision.contactCount > 0)
        {
            rb.velocity = Vector2.Reflect(rb.velocity.normalized, collision.contacts[0].normal) * speed;
        }
    }

    private Vector2 GetArenaHalfSize()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return fallbackArenaHalfSize;

        float y = cam.orthographicSize;
        float x = y * cam.aspect;
        return new Vector2(Mathf.Max(1f, x - 0.5f), Mathf.Max(1f, y - 0.5f));
    }
}
