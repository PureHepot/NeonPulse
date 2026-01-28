using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public float speed = 20f;
    public int damage = 2;
    public float lifeTime = 2f;

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
            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);

                Vector3 hitNormal = transform.right;

                target.TakeDamage(damage, hitPoint, hitNormal);

                ObjectPoolManager.Instance.Return(this.gameObject);
            }
        }
    }
}
