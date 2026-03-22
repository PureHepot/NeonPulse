using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public float speed = 20f;
    public int damage = 2;
    public float lifeTime = 2f;
    public LayerMask hitLayer;
    private float timer;

    private float moveDistance;

    [Header("反弹设置")]
    public LayerMask WallLayer;

    public void OnSpawn()
    {
        timer = 0f;
        GetComponent<TrailRenderer>()?.Clear();
        transform.SetPositionZ(1f);
    }

    public void OnDespawn()
    {
        
    }

    void Update()
    {
        moveDistance = speed * Time.deltaTime;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, moveDistance, hitLayer);

        if (hit.collider != null)
        {
            OnHitObject(hit.collider, hit.point, hit.normal);
        }
        
            transform.Translate(Vector3.right * moveDistance);
        


        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }

    void OnHitObject(Collider2D other, Vector2 hitPoint, Vector2 hitNormal)
    {
        IDamageable target = other.GetComponent<IDamageable>();

        if (target != null)
        {
            target.TakeDamage(damage, hitPoint, transform.right);
            ObjectPoolManager.Instance.Return(this.gameObject);
        }
        if (((1 << other.gameObject.layer) & WallLayer) != 0)
        {
            // 计算反弹方向
            Vector2 reflectDir = Vector2.Reflect(transform.right, hitNormal);
            transform.right = reflectDir;
            transform.position = hitPoint;
        }
    }

}
