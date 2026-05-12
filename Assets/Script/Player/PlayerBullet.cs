using UnityEngine;

public class PlayerBullet : MonoBehaviour, IPoolable, IProjectileSpawnReceiver
{
    [Header("Settings")]
    public float speed = 20f;
    public int damage = 2;
    public float lifeTime = 2f;
    public LayerMask hitLayer;

    [Header("Bounce Settings")]
    public LayerMask WallLayer;

    [Header("Impact Physics")]
    public float impactForce = 3.5f;
    public float impactTorque = 10f;

    [Header("Runtime Homing")]
    public bool homingEnabled;
    public float homingTurnRate;
    public float homingAcquireRadius;
    public float homingRetargetInterval = 0.15f;

    private float timer;
    private float moveDistance;
    private Transform homingTarget;
    private float homingRetargetTimer;

    public void OnSpawn()
    {
        timer = 0f;
        homingTarget = null;
        homingRetargetTimer = 0f;
        GetComponent<TrailRenderer>()?.Clear();
        transform.SetPositionZ(1f);
    }

    public void OnDespawn()
    {
        homingTarget = null;
        homingRetargetTimer = 0f;
    }

    public void ApplySpawnData(ProjectileSpawnData spawnData)
    {
        if (spawnData == null)
            return;

        damage = spawnData.damage;
        speed = spawnData.speed;
        lifeTime = spawnData.lifeTime;
        hitLayer = spawnData.hitLayer;
        WallLayer = spawnData.wallLayer;
        homingEnabled = spawnData.homingEnabled;
        homingTurnRate = spawnData.homingTurnRate;
        homingAcquireRadius = spawnData.homingAcquireRadius;
        homingRetargetInterval = spawnData.homingRetargetInterval;
        homingTarget = null;
        homingRetargetTimer = 0f;
    }

    private void Update()
    {
        moveDistance = speed * Time.deltaTime;
        UpdateHoming();

        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, moveDistance, hitLayer);
        if (hit.collider != null)
            OnHitObject(hit.collider, hit.point, hit.normal);

        transform.Translate(Vector3.right * moveDistance);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            ObjectPoolManager.Instance.Return(gameObject);
    }

    private void OnHitObject(Collider2D other, Vector2 hitPoint, Vector2 hitNormal)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                //enemy.TakeDamage(damage, hitPoint, transform.right, impactForce, impactTorque);
            }
                
            else
                target.TakeDamage(damage, hitPoint, transform.right);

            ObjectPoolManager.Instance.Return(gameObject);
        }

        if (((1 << other.gameObject.layer) & WallLayer) != 0)
        {
            Vector2 reflectDir = Vector2.Reflect(transform.right, hitNormal);
            transform.right = reflectDir;
            transform.position = hitPoint;
        }
    }

    private void UpdateHoming()
    {
        if (!homingEnabled)
            return;

        homingRetargetTimer -= Time.deltaTime;
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy || homingRetargetTimer <= 0f)
        {
            homingTarget = AcquireHomingTarget();
            homingRetargetTimer = homingRetargetInterval > 0f ? homingRetargetInterval : 0.15f;
        }

        if (homingTarget == null)
            return;

        Vector2 targetDirection = homingTarget.position - transform.position;
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        float maxRadiansDelta = Mathf.Deg2Rad * homingTurnRate * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(transform.right, targetDirection.normalized, maxRadiansDelta, 0f);
        transform.right = newDirection.normalized;
    }

    private Transform AcquireHomingTarget()
    {
        float searchRadius = homingAcquireRadius > 0f ? homingAcquireRadius : 6f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, hitLayer);

        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        for (int index = 0; index < hits.Length; index++)
        {
            var hit = hits[index];
            if (hit == null || !hit.gameObject.activeInHierarchy)
                continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
                continue;

            float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance >= nearestDistance)
                continue;

            nearestDistance = sqrDistance;
            nearestTarget = hit.transform;
        }

        return nearestTarget;
    }
}
