using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ContinuousPhysicsMotor2D))]
public abstract class EnemyBase : MonoBase, IPoolable
{
    [Header("Enemy Specific")]
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public int contactDamage = 1;
    public int enemyExp = 10;

    [Header("Knockback Settings")]
    public bool canKnockback;
    protected bool isKnockbacking;
    public float knockbackForce = 8f;
    public float knockbackTorque = 20f;

    [Header("Movement Motor")]
    [SerializeField] protected float locomotionResponse = 12f;
    [SerializeField] protected float angularDamping = 10f;

    protected Rigidbody2D rb;
    protected ContinuousPhysicsMotor2D motionMotor;
    protected Transform playerTransform;
    public bool isInScene;
    public bool scared;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        motionMotor = GetComponent<ContinuousPhysicsMotor2D>();
        if (motionMotor == null)
            motionMotor = gameObject.AddComponent<ContinuousPhysicsMotor2D>();

        motionMotor.Configure(locomotionResponse, angularDamping);
        isInScene = false;
    }

    public virtual void OnSpawn()
    {
        currentHp = maxHp;
        isDead = false;
        isKnockbacking = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        ResetHitFlashVisuals();

        if (bodyRenderer != null)
            bodyRenderer.color = normalColor;

        transform.localScale = Vector3.one;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        rb.simulated = true;
        ResetMovementDrive();

        InRunDirector.ActiveInstance?.RegisterBoundaryEnemy(this);
        EnemyManager.Instance?.RegisterEnemy(this);
    }

    public virtual void OnDespawn()
    {
        ResetHitFlashVisuals();
        transform.DOKill();
        StopMovementDrive(true);
        EnemyManager.Instance?.UnRegisterEnemy(this);
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockbacking)
            return;

        MoveBehavior();
        CheckOutView();
    }

    protected virtual void MoveBehavior()
    {
    }

    public override void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);
        if (!isDead && canKnockback)
            ApplyKnockback(hitNormal, knockbackForce);
    }

    public override void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        base.TakeDamage(amount, hitPoint, knockbackDir, customForce);
        if (!isDead && canKnockback && customForce > 0f)
            ApplyKnockback(knockbackDir, customForce);
    }

    public override void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce, float customTorque)
    {
        base.TakeDamage(amount, hitPoint, knockbackDir, customForce, customTorque);
        if (!isDead && canKnockback && customForce > 0f)
            ApplyKnockback(knockbackDir, customForce, customTorque);
    }

    protected virtual void ApplyKnockback(Vector3 forceDir, float force)
    {
        isKnockbacking = true;
        StopMovementDrive(true);
        ApplyImpulse(forceDir.normalized * force, Random.Range(-knockbackTorque, knockbackTorque));
        Timer.Register(0.2f, () => isKnockbacking = false);
    }

    protected virtual void ApplyKnockback(Vector3 forceDir, float force, float angularImpulse)
    {
        isKnockbacking = true;
        StopMovementDrive(true);
        ApplyImpulse(forceDir.normalized * force, angularImpulse);
        Timer.Register(0.2f, () => isKnockbacking = false);
    }

    protected override void Die()
    {
        base.Die();
        StopMovementDrive(true);
        rb.simulated = false;
        InRunDirector.ActiveInstance?.NotifyEnemyKilled(this);
        ObjectPoolManager.Instance.Return(gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<ShieldController>() != null)
            return;

        if (collision.gameObject.CompareTag("Player"))
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }

    private void CheckOutView()
    {
        Vector2 p = Camera.main.WorldToViewportPoint(transform.position);
        isInScene = !(p.x < 0f || p.x > 1f || p.y < 0f || p.y > 1f);
    }

    protected void DriveVelocity(Vector2 velocity, float responseScale = 1f)
    {
        if (motionMotor != null)
        {
            motionMotor.SetDesiredVelocity(velocity, responseScale);
            return;
        }

        rb.velocity = velocity;
    }

    protected void SnapVelocity(Vector2 velocity)
    {
        if (motionMotor != null)
        {
            motionMotor.SnapVelocity(velocity);
            return;
        }

        rb.velocity = velocity;
    }

    protected void StopMovementDrive(bool immediate = false)
    {
        if (motionMotor != null)
        {
            motionMotor.StopDriving(immediate);
            if (immediate)
                rb.angularVelocity = 0f;
            return;
        }

        rb.velocity = Vector2.zero;
        if (immediate)
            rb.angularVelocity = 0f;
    }

    protected void ResetMovementDrive()
    {
        if (motionMotor != null)
        {
            motionMotor.ResetMotion();
            return;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    protected void ApplyImpulse(Vector2 impulse, float angularImpulse = 0f)
    {
        if (motionMotor != null)
        {
            motionMotor.AddImpulse(impulse, angularImpulse);
            return;
        }

        rb.AddForce(impulse, ForceMode2D.Impulse);
        if (!Mathf.Approximately(angularImpulse, 0f))
            rb.AddTorque(angularImpulse, ForceMode2D.Impulse);
    }
}
