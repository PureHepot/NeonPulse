using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Basic Stats")]
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 5f;

    [Header("Collision Settings")]
    // 【关键新增】在这里勾选你的墙壁/地面 Layer
    // 这样代码就会检测 Layer，而不是 Tag
    public LayerMask obstacleLayer;

    private Rigidbody2D rb;
    private float timer;
    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 确保使用动力学，完全由代码控制移动，不受重力影响
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// 初始化子弹（由 BossTurret 调用）
    /// </summary>
    public void Initialize(Vector2 direction)
    {
        timer = 0f;
        isInitialized = true;

        // 计算角度：让子弹的“右侧（Right）”指向目标方向
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 设定自动销毁时间
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 核心移动逻辑：始终沿着自身的右方移动
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 撞到玩家
        if (other.CompareTag("Player"))
        {
            var health = other.GetComponentInChildren<HealthModule>();
            if (health != null)
            {
                health.TakeDamage(damage, transform);
                Destroy(gameObject); // 造成伤害后销毁自己
            }
        }
        // 2. 【修改重点】撞到障碍物 (使用 LayerMask 检测)
        // (1 << other.gameObject.layer) 是将当前物体的 layer 索引转换为掩码
        // & obstacleLayer 运算如果不为 0，说明这个 layer 被勾选了
        else if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}