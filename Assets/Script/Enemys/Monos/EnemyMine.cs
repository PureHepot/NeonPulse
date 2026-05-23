using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMine : MonoBehaviour, IDamageable
{
    [Header("Mine Stats")]
    public float hp = 1f;
    public float armDelay = 0.2f;
    public float explodeRadius = 1.8f;
    public int explodeDamage = 2;
    public LayerMask playerLayer;

    [Header("FX")]
    public GameObject explodeFxPrefab;
    public string explodeFxResourcePath = "JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion 1";
    public float explodeFxLifeTime = 2f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isCarried;
    private bool isArmed;
    private bool isExploded;
    private float armTimer;
    private float currentHp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.gravityScale = 0f;

        EnsureEnemyTagAndLayer();
    }

    private void Update()
    {
        if (isCarried || isExploded) return;

        if (!isArmed)
        {
            armTimer -= Time.deltaTime;
            if (armTimer <= 0f)
            {
                isArmed = true;
            }
        }
    }

    public void InitializeAsCarried(Transform parent, Vector3 localPos, Quaternion localRot)
    {
        EnsureEnemyTagAndLayer();

        isCarried = true;
        isArmed = false;
        isExploded = false;
        armTimer = 0f;
        currentHp = hp;

        if (parent != null)
        {
            transform.SetParent(parent, false);
            transform.localPosition = localPos;
            transform.localRotation = localRot;
        }

        if (rb != null)
        {
            rb.simulated = false;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (col != null) col.enabled = false;
    }

    public void Deploy()
    {
        if (isExploded) return;

        EnsureEnemyTagAndLayer();

        isCarried = false;
        isArmed = false;
        armTimer = Mathf.Max(0f, armDelay);
        currentHp = hp;

        transform.SetParent(null, true);

        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (col != null) col.enabled = true;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isCarried || isExploded || amount <= 0) return;

        currentHp -= amount;
        if (currentHp <= 0)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isArmed || isCarried || isExploded) return;
        if (!other.CompareTag("Player")) return;

        Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isArmed || isCarried || isExploded) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        Explode();
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        DealExplosionDamageToPlayer();
        PlayExplosionFx();
        Destroy(gameObject);
    }

    private void DealExplosionDamageToPlayer()
    {
        HashSet<HealthModule> damagedTargets = new HashSet<HealthModule>();
        Collider2D[] hits = playerLayer.value == 0
            ? Physics2D.OverlapCircleAll(transform.position, explodeRadius)
            : Physics2D.OverlapCircleAll(transform.position, explodeRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.CompareTag("Player")) continue;

            HealthModule health = hit.GetComponentInChildren<HealthModule>();
            if (health == null || damagedTargets.Contains(health)) continue;

            health.TakeDamage(explodeDamage, transform);
            damagedTargets.Add(health);
        }
    }

    private void PlayExplosionFx()
    {
        if (explodeFxPrefab == null)
        {
            explodeFxPrefab = Resources.Load<GameObject>(explodeFxResourcePath);
        }

        if (explodeFxPrefab == null) return;

        GameObject fx = Instantiate(explodeFxPrefab, transform.position, Quaternion.identity);
        Destroy(fx, explodeFxLifeTime);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
