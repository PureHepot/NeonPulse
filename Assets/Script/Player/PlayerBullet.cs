using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerBullet : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    public float speed = 20f;
    public int damage = 2;
    public float lifeTime = 2f;
    public LayerMask hitLayer;
    private float timer;

    private float moveDistance;
    private int penetrateCount = 2;
    private int currentPenetrate;
    private float explodeRadius=1;
    private int explodeDamage=1;
    public GameObject explodePS;

    [Header("反弹设置")]
    public LayerMask WallLayer;

    private static Transform currentTarget;

    public float detectionRadius = 15f;
    public float turnSpeed = 5f;

    public bool isReflect=false;
    public bool isPenetrate=false;
    public bool isChase=false;
    public bool isExplode=false;

    public void OnSpawn()
    {
        timer = 0f;
        GetComponent<TrailRenderer>()?.Clear();
        transform.SetPositionZ(1f);
        currentPenetrate = penetrateCount;
    }

    public void OnDespawn()
    {
        
    }

    void Update()
    {
        UpdateSharedTarget();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, moveDistance, hitLayer);

        if (hit.collider != null)
        {
            OnHitObject(hit.collider, hit.point, hit.normal);
        }
        // 如果有目标，转向
        if (currentTarget != null && isChase)
        {
            Vector2 dir = (currentTarget.position - transform.position).normalized;
            transform.Translate(dir * speed * Time.deltaTime, Space.World);
        }
        else
        {
            moveDistance = speed * Time.deltaTime;
            transform.Translate(Vector3.right * moveDistance);
        }
        
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
            if (isExplode)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, hitLayer);
                foreach (var hit in hits) 
                {
                    IDamageable enemy = hit.GetComponent<IDamageable>();
                    if (enemy != null)
                    {
                        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
                        enemy.TakeDamage(explodeDamage, hit.transform.position, (hit.transform.position-player.position).normalized);
                        explodePS = Resources.Load<GameObject>("ParticleSystem/ExplodePS");
                        GameObject particleObj = ObjectPoolManager.Instance.Get(explodePS, hitPoint, Quaternion.identity);
                        Timer.Register(0.5f, onComplete: () =>
                        {
                            ObjectPoolManager.Instance.Return(particleObj);
                        });
                        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            var main = ps.main;

                            main.startSize = explodeRadius;

                            ps.Play();
                        }
                    }
                }
            }
            if (isPenetrate)
            {
                target.TakeDamage(damage, hitPoint, transform.right);
                currentPenetrate--;
                if (currentPenetrate <= 0)
                {
                    ObjectPoolManager.Instance.Return(this.gameObject);
                }
                
            }
            else
            {
                target.TakeDamage(damage, hitPoint, transform.right);
                ObjectPoolManager.Instance.Return(this.gameObject);
            }
        }
        if (((1 << other.gameObject.layer) & WallLayer) != 0 && isReflect)
        {
            // 计算反弹方向
            Vector2 reflectDir = Vector2.Reflect(transform.right, hitNormal);
            transform.right = reflectDir;
            transform.position = hitPoint;
        }
    }
    void UpdateSharedTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            // 寻找场景中最近的敌人，这里假设玩家位置已知，可用静态引用或查找
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;

            EnemyBase[]enemies=FindObjectsOfType<EnemyBase>();
            Transform nearest = null;
            float minDist = Mathf.Infinity;
            foreach (var hit in enemies)
            {
                float d = (hit.transform.position - player.position).sqrMagnitude;
                if (d < minDist)
                {
                    minDist = d;
                    nearest = hit.transform;
                }
            }
            currentTarget = nearest;
        }
    }
}
