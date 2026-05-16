using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour, IPoolable, IReflectableProjectile
{
    [Header("Basic Stats")]
    public float speed = 5f;
    public int damage = 1;
    public float lifeTime = 5f;

    [Header("Raycast Collision")]
    public LayerMask hitLayers;

    [Header("Reflection Settings")]
    public bool enableReflection = false;
    public int maxBounces = 2;
    public string reflectionTag = "Reflector";

    [Header("Homing Reflection (新增: 追踪反弹)")]
    [Tooltip("反弹导向修正：0为纯物理反弹，1为直接射向玩家，建议0.3左右")]
    [Range(0f, 1f)]
    public float reflectionHomingBias = 0.3f;

    private float timer;
    private bool isInitialized = false;
    private int currentBounceCount = 0;
    private bool isReflectedByPlayer = false;

    // 记录实际飞行方向
    public Vector3 direction { get; private set; }
    private Transform myTransform;

    private void Awake()
    {
        myTransform = transform;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Initialize(Vector3 dir)
    {
        timer = 0f;
        currentBounceCount = 0;
        isInitialized = true;
        isReflectedByPlayer = false;

        this.direction = dir.normalized;
        UpdateRotation();
    }

    public void OnSpawn() { }
    public void OnDespawn() { }

    private void Update()
    {
        if (!isInitialized) return;

        float moveDistance = speed * Time.deltaTime;

        // 射线检测
        RaycastHit2D hit = Physics2D.Raycast(myTransform.position, direction, moveDistance, hitLayers);

        if (hit.collider != null)
        {
            OnHitObject(hit.collider, hit.point, hit.normal);
        }
        else
        {
            myTransform.Translate(direction * moveDistance, Space.World);
        }

        timer += Time.deltaTime;
        if (timer >= lifeTime) RecycleSelf();
    }

    void OnHitObject(Collider2D other, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (!isReflectedByPlayer && other.CompareTag("Player"))
        {
            var health = other.GetComponentInChildren<HealthModule>();
            if (health != null) health.TakeDamage(damage, myTransform);
            RecycleSelf();
        }
        else if (isReflectedByPlayer && other.CompareTag("Enemy"))
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, hitPoint, direction.normalized);

            RecycleSelf();
        }
        else if (enableReflection && other.CompareTag(reflectionTag))
        {
            HandleReflection(hitPoint, hitNormal);
        }
        else
        {
            RecycleSelf();
        }
    }

    void HandleReflection(Vector2 hitPoint, Vector2 hitNormal)
    {
        if (currentBounceCount >= maxBounces)
        {
            RecycleSelf();
            return;
        }

        currentBounceCount++;

        // 1. 移动到碰撞点
        myTransform.position = hitPoint;

        // --- 核心修改：计算带有“杀意”的反弹方向 ---

        // A. 计算标准的物理反射方向
        Vector2 standardReflectDir = Vector2.Reflect(direction, hitNormal).normalized;

        // B. 计算指向玩家的方向
        Vector2 targetDir = standardReflectDir; // 默认回退
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            targetDir = (player.transform.position - (Vector3)hitPoint).normalized;
        }

        // C. 融合两个方向 (Vector3.Lerp)
        // Lerp 会在 A 和 B 之间插值。Bias = 0 是A，Bias = 1 是B。
        Vector2 finalDir = Vector3.Lerp(standardReflectDir, targetDir, reflectionHomingBias).normalized;

        // 2. 检查反弹角度安全性 (可选优化)
        // 确保新方向也是朝向“外侧”的，防止插值过度导致子弹反弹回墙里
        // 如果 finalDir 和 hitNormal 的夹角大于 90度 (点积 < 0)，说明反向穿墙了
        if (Vector2.Dot(finalDir, hitNormal) < 0)
        {
            // 如果计算出的方向会穿墙，强制使用物理反射保底
            finalDir = standardReflectDir;
        }

        // 3. 应用新方向
        this.direction = finalDir;
        UpdateRotation();

        // 4. 推离表面
        myTransform.Translate(direction * 0.1f, Space.World);
    }

    void UpdateRotation()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        myTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void RecycleSelf()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Return(this.gameObject);
        else
            Destroy(gameObject);
    }

    public bool TryReflect(Vector3 reflectorPosition, Vector3 preferredTargetPosition)
    {
        if (!isInitialized || isReflectedByPlayer)
            return false;

        isReflectedByPlayer = true;
        timer = 0f;
        myTransform.position = reflectorPosition;

        Vector2 targetDirection = preferredTargetPosition - myTransform.position;
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            targetDirection = -direction;

        direction = targetDirection.normalized;
        UpdateRotation();
        myTransform.Translate(direction * 0.1f, Space.World);
        return true;
    }
}
