using UnityEngine;

public class MechBuffer : MechBase
{
    [Header("Obstacle Settings")]
    public float pushSpeed = 4f;

    private Collider2D mainCollider;

    protected override void Awake()
    {
        base.Awake();
        mainCollider = GetComponent<Collider2D>();
        mainCollider.isTrigger = true;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            ObjectPoolManager.Instance.Return(bullet.gameObject);
            TakeDamage(bullet.damage);
            return;
        }
        EnemyProjectile projectile= other.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            ObjectPoolManager.Instance.Return(projectile.gameObject);
            TakeDamage(projectile.damage);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            Vector3 repelDir = (enemy.transform.position - transform.position).normalized;
            if (repelDir.sqrMagnitude < 0.01f)
                repelDir = Vector3.up;

            enemy.transform.position += repelDir * pushSpeed * Time.deltaTime;
        }
    }
}
