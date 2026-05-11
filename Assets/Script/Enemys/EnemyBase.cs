using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBase, IPoolable
{
    [Header("Enemy Specific")]
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public int contactDamage = 1;
    public int enemyExp = 10;

    [Header("Knockback Settings")]
    public bool canKnockback = false;
    protected bool isKnockbacking;
    public float knockbackForce = 8f;
    public float knockbackTorque = 20f;

    protected Rigidbody2D rb;
    protected Transform playerTransform;
    public bool isInScene;
    public bool scared;

    protected override void Awake()
    {
        base.Awake(); // 调用 EntityBase 的 Awake
        rb = GetComponent<Rigidbody2D>();
        isInScene = false;
    }

    public virtual void OnSpawn()
    {
        currentHp = maxHp;
        isDead = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (bodyRenderer != null) bodyRenderer.color = normalColor;
        transform.localScale = Vector3.one;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        rb.simulated = true;
if (InRunDirector.ActiveInstance != null)
            InRunDirector.ActiveInstance.RegisterBoundaryEnemy(this);if (WaveManager.Instance != null) WaveManager.Instance.RegisterEnemy(this);
if (EnemyManager.Instance != null) EnemyManager.Instance.RegisterEnemy(this);    }

    public virtual void OnDespawn()
    {
        Debug.Log("我被OnDespawn了");
        transform.DOKill();
        if (bodyRenderer != null) { bodyRenderer.DOKill(); bodyRenderer.material.DOKill(); }
rb.velocity = Vector2.zero;

        if (WaveManager.Instance != null) WaveManager.Instance.UnregisterEnemy(this);
        if (EnemyManager.Instance != null) EnemyManager.Instance.UnRegisterEnemy(this);    }

    private void FixedUpdate()
    {
        if (isDead || isKnockbacking) return;
        MoveBehavior();
        CheckOutView();
    }

    protected virtual void MoveBehavior()
    {
        
    }

    // 覆写击退相关的方法
    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);
        if (!isDead && canKnockback) ApplyKnockback(hitNormal, knockbackForce);
    }

    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        base.TakeDamage(amount, hitPoint, knockbackDir, customForce);
        if (!isDead && canKnockback && customForce > 0) ApplyKnockback(knockbackDir, customForce);
    }

    protected virtual void ApplyKnockback(Vector3 forceDir, float force)
    {
isKnockbacking = true;
        rb.velocity = Vector2.zero;
        rb.AddForce(forceDir.normalized * force, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-knockbackTorque, knockbackTorque), ForceMode2D.Impulse);
        Timer.Register(0.2f, () => isKnockbacking = false);    }

    
    protected override void Die()
    {
        base.Die(); // 播放特效和音效
        rb.simulated = false;

if (UpgradeManager.Instance != null) UpgradeManager.Instance.AddExperience(enemyExp);
        ObjectPoolManager.Instance.Return(gameObject); // 普通敌人使用对象池回收    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<ShieldController>() != null) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
        }
    }

    private void CheckOutView()
    {
        Vector2 p = Camera.main.WorldToViewportPoint(transform.position);
        isInScene = !(p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1);
    }
  
}
