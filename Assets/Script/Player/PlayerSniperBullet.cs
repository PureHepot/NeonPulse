using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSniperBullet : MonoBehaviour, IPoolable, IProjectileSpawnReceiver
{
    [Header("Settings")]
    public float speed = 40f;
    public int damage = 4;
    public float lifeTime = 3f;
    public int penetrationCount = 2;

    [Header("Detection")]
    public Vector2 boxSize = new Vector2(1f, 0.2f);
    public LayerMask hitLayer;
    public LayerMask wallLayer;

    [Header("Runtime Homing")]
    public bool homingEnabled;
    public float homingTurnRate;
    public float homingAcquireRadius;
    public float homingRetargetInterval = 0.15f;

    private float timer;
    private int penetrationLeft;
    private Transform homingTarget;
    private float homingRetargetTimer;
    private readonly HashSet<Collider2D> hitTargets = new();

    public void OnSpawn()
    {
        timer = 0f;
        penetrationLeft = penetrationCount;
        hitTargets.Clear();
        homingTarget = null;
        homingRetargetTimer = 0f;
        GetComponent<TrailRenderer>()?.Clear();
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
        wallLayer = spawnData.wallLayer;
        homingEnabled = spawnData.homingEnabled;
        homingTurnRate = spawnData.homingTurnRate;
        homingAcquireRadius = spawnData.homingAcquireRadius;
        homingRetargetInterval = spawnData.homingRetargetInterval;
        homingTarget = null;
        homingRetargetTimer = 0f;
    }

    private void Update()
    {
        float moveDistance = speed * Time.deltaTime;
        UpdateHoming();
        CheckCollision(moveDistance);

        if (gameObject.activeInHierarchy)
            transform.Translate(Vector3.right * moveDistance);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            ObjectPoolManager.Instance.Return(gameObject);
    }

    private void CheckCollision(float distance)
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            transform.position,
            boxSize,
            transform.eulerAngles.z,
            transform.right,
            distance,
            hitLayer);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            Collider2D other = hit.collider;
            if (other == null)
                continue;

            if (other.isTrigger && !other.CompareTag("Enemy"))
                continue;

            if (hitTargets.Contains(other))
                continue;

            if (!other.CompareTag("Enemy"))
            {
                if (((1 << other.gameObject.layer) & wallLayer) != 0)
                {
                    ObjectPoolManager.Instance.Return(gameObject);
                    return;
                }

                continue;
            }

            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null)
                continue;

            hitTargets.Add(other);

            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                Vector3 knockbackDir = transform.right;
                enemy.TakeDamage(damage, hit.point, knockbackDir, 5f);
            }
            else
            {
                target.TakeDamage(damage);
            }

            penetrationLeft--;
            if (penetrationLeft <= 0)
            {
                ObjectPoolManager.Instance.Return(gameObject);
                return;
            }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Vector3.zero, Vector3.right * speed * 0.1f);
    }
}
