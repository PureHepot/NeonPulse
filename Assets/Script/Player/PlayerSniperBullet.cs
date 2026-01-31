using System.Collections.Generic;
using UnityEngine;

public class PlayerSniperBullet : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public float speed = 40f;
    public int damage = 10;
    public float lifeTime = 3f;
    public int penetrationCount = 2;

    private float timer;
    private int penetrationLeft;

    // 防止同一个敌人被多次命中
    private HashSet<Collider2D> hitTargets = new();

    public void OnSpawn()
    {
        timer = 0f;
        penetrationLeft = penetrationCount;
        hitTargets.Clear();

        GetComponent<TrailRenderer>()?.Clear();
    }

    public void OnDespawn()
    {
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (hitTargets.Contains(other)) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) return;

        hitTargets.Add(other);

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = transform.right;

        target.TakeDamage(damage, hitPoint, hitNormal);

        penetrationLeft--;

        if (penetrationLeft < 0)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
    }
}
