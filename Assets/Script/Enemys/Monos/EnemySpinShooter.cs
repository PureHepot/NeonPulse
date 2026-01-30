using UnityEngine;

public class EnemySpinShooter : EnemyBase
{
    [Header("Move & Follow")]
    public float chaseSpeed = 3f;
    public float keepDistance = 4f;
    public float followSmooth = 6f;

    [Header("Spin")]
    public float spinSpeed = 360f;

    [Header("Separation")]
    public float separationDistance = 1.5f;  
    public float separationStrength = 2f;    

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireInterval = 3f;
    public float bulletSpeed = 6f;

    [Header("Touch Damage")]
    public int touchDamage = 1;
    public float damageCD = 0.5f;

    private float shootTimer;
    private float damageTimer;

    public override void OnSpawn()
    {
        base.OnSpawn();

        shootTimer = fireInterval;
        damageTimer = 0;

        if (firePoint == null)
            firePoint = transform;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        // 自旋
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

        // 平滑追踪+与同类保持距离
        Vector2 desiredVel = GetDesiredVelocity();
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVel, Time.deltaTime * followSmooth);

        HandleShoot();

        HandleTouchDamage();
    }

    Vector2 GetDesiredVelocity()
    {
        Vector2 toPlayer = (playerTransform.position - transform.position);
        float dist = toPlayer.magnitude;
        Vector2 dir = toPlayer.normalized;

        Vector2 velocity = Vector2.zero;
        if (dist > keepDistance)
            velocity = dir * chaseSpeed;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationDistance, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            Vector2 away = (transform.position - hit.transform.position);
            float d = away.magnitude;
            if (d > 0 && d < separationDistance)
                velocity += away.normalized * separationStrength * (1f - d / separationDistance);
        }

        return velocity;
    }

    void HandleShoot()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer += fireInterval; 
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || playerTransform == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = (playerTransform.position - spawnPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);

        GameObject bullet = ObjectPoolManager.Instance.Get(bulletPrefab, spawnPos, rot);

        var spinBullet = bullet.GetComponent<EnemySpinBullet>();
        if (spinBullet != null)
            spinBullet.speed = bulletSpeed;
    }

    void HandleTouchDamage()
    {
        damageTimer -= Time.deltaTime;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (damageTimer > 0) return;

        if (collision.collider.CompareTag("Player"))
        {
            var health = collision.collider.GetComponent<HealthModule>();
            if (health != null)
            {
                health.TakeDamage(touchDamage, transform);
                damageTimer = damageCD;
            }
        }
    }
}
