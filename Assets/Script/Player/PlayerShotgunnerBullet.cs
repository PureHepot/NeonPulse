using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerShotgunBullet : MonoBehaviour, IPoolable
{
    [Header("Base")]
    public float speed = 18f;
    public int damage = 1;
    public float lifeTime = 3f;

    private static readonly Dictionary<Collider2D, float> recentHits = new Dictionary<Collider2D, float>();
    private static float lastClearTime = 0f;
    private const float CLEAR_INTERVAL = 5f;     
    private const float HIT_WINDOW = 0.1f;      

    private float timer;

    public void OnSpawn()
    {
        timer = 0f;
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
            return;
        }

        if (Time.time - lastClearTime > CLEAR_INTERVAL)
        {
            lastClearTime = Time.time;
            var keysToRemove = recentHits.Where(kvp => Time.time - kvp.Value > 2f).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                recentHits.Remove(key);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        // 检查是否已被本波散弹命中过
        if (recentHits.TryGetValue(other, out float lastHitTime) && Time.time - lastHitTime < HIT_WINDOW)
        {
            ObjectPoolManager.Instance.Return(gameObject);
            return;
        }

        recentHits[other] = Time.time;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null)
        {
            ObjectPoolManager.Instance.Return(gameObject);
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = transform.right;
        target.TakeDamage(damage, hitPoint, hitNormal);

        ObjectPoolManager.Instance.Return(gameObject);
    }
}