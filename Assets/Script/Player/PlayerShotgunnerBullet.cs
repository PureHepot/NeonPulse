using UnityEngine;

public class PlayerShotgunBullet : MonoBehaviour, IPoolable
{
    [Header("Base")]
    public float speed = 18f;
    public int damage = 1;
    public float lifeTime = 3f;

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
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = transform.right;

        target.TakeDamage(damage, hitPoint, hitNormal);

        ObjectPoolManager.Instance.Return(gameObject);
    }
}
