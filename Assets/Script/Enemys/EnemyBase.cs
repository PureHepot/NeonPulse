using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IPoolable, IDamageable
{
    [Header("Base Stats")]
    public float maxHp = 10f;
    public float moveSpeed = 5f;
    public int scoreValue = 10;
    public int contactDamage = 1;
    public int enemyExp = 10;

    [Header("Visuals")]
    public SpriteRenderer bodyRenderer;
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public GameObject deathEffectPrefab;
    public GameObject hitParticlePrefab;

    [Header("Knockback Settings")]
    public bool canKnockback = false;
    protected bool isKnockbacking;
    public float knockbackForce = 8f;
    public float knockbackTorque = 20f;

    [Header("Physics Motion")]
    public float locomotionResponse = 10f;
    public float angularDamping = 12f;

    public float currentHp;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected bool isDead = false;
    protected ContinuousPhysicsMotor2D motionMotor;

    public bool isInScene;
    public bool scared;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        motionMotor = GetComponent<ContinuousPhysicsMotor2D>();
        if (motionMotor == null)
            motionMotor = gameObject.AddComponent<ContinuousPhysicsMotor2D>();

        motionMotor.Configure(locomotionResponse, angularDamping);
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        isInScene = false;
    }

    public virtual void OnSpawn()
    {
        currentHp = maxHp;
        isDead = false;
        this.gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (bodyRenderer != null) bodyRenderer.color = normalColor;
        transform.localScale = Vector3.one;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        rb.simulated = true;
        motionMotor?.ResetMotion();
        
        if (InRunDirector.ActiveInstance != null)
            InRunDirector.ActiveInstance.RegisterBoundaryEnemy(this);
        else if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(this);
    }

    public virtual void OnDespawn()
    {

        transform.DOKill();
        if (bodyRenderer != null) bodyRenderer.DOKill();

        if (motionMotor != null)
            motionMotor.ResetMotion();
        else
            rb.velocity = Vector2.zero;
        
        if (InRunDirector.ActiveInstance != null)
            InRunDirector.ActiveInstance.UnregisterBoundaryEnemy(this);
        else if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnRegisterEnemy(this);
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockbacking) return;
        MoveBehavior();
        CheckOutView();
    }

    protected virtual void MoveBehavior()
    {
        
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;

        PlayHitEffect();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void PlayHitEffect()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.DOColor(hitColor, 0.05f).OnComplete(() =>
            {
                bodyRenderer.DOColor(normalColor, 0.1f);
            });

            // 绠€鍗曠殑鍙楀嚮缂╂斁锛圦寮圭殑鎰熻锛?
            transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.simulated = false;
        AudioManager.Instance.PlayEffect("EnemyDie");

        if (deathEffectPrefab == null)
        {
            deathEffectPrefab = Resources.Load<GameObject>("ParticleSystem/PS_DeathSparks");
        }

        if (deathEffectPrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }

        BackgroundFXController.Instance.TriggerDistortion(transform.position);

        if (InRunDirector.ActiveInstance != null)
            InRunDirector.ActiveInstance.NotifyEnemyKilled(this);
        
        ObjectPoolManager.Instance.Return(this.gameObject);
    }


    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        var shield = collision.collider.gameObject.GetComponent<ShieldController>();
        if (shield != null)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHp -= amount;

        PlayHitEffect(hitPoint, hitNormal);

        if (canKnockback)
        {
            ApplyKnockback(hitNormal);
        }

        if (currentHp <= 0) Die();
        else AudioManager.Instance.PlayEffect("EnemyHit1", 2f, 1f);
    }

    protected virtual void ApplyKnockback(Vector3 hitNormal)
    {
        isKnockbacking = true;

        StopMovementDrive();

        Vector2 forceDir = hitNormal.normalized;
        ApplyImpulse(forceDir * knockbackForce, Random.Range(-knockbackTorque, knockbackTorque));

        Timer.Register(0.2f, () =>
        {
            isKnockbacking = false;
        });
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        TakeDamage(amount, hitPoint, knockbackDir, customForce, float.NaN);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce, float customTorque)
    {
        if (isDead) return;

        currentHp -= amount;

        PlayHitEffect(hitPoint, knockbackDir); // 鎾斁鐗规晥

        if (canKnockback && customForce > 0)
        {
            ApplyCustomKnockback(knockbackDir, customForce, customTorque);
        }

        if (currentHp <= 0) Die();
        else AudioManager.Instance.PlayEffect("EnemyHit1");
    }

    protected virtual void ApplyCustomKnockback(Vector3 forceDir, float force, float customTorque = float.NaN)
    {
        isKnockbacking = true;
        StopMovementDrive();

        float torque = float.IsNaN(customTorque)
            ? Random.Range(-knockbackTorque, knockbackTorque)
            : customTorque;

        ApplyImpulse(forceDir * force, torque);

        Timer.Register(0.2f, () =>
        {
            isKnockbacking = false;
        });
    }



    protected virtual void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // 鍋囪鎴戜滑鍦⊿hader閲屽畾涔変簡 "_HitFlashStrength"
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.1f);
        }

        if (hitParticlePrefab == null)
        {
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }

        if (hitParticlePrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }
    }
    
    
    private void CheckOutView()
    {
        Vector2 p = Camera.main.WorldToViewportPoint(transform.position);
        isInScene = !(p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1);
    }

    protected void DriveVelocity(Vector2 velocity, float responseScale = 1f)
    {
        if (motionMotor != null)
            motionMotor.SetDesiredVelocity(velocity, responseScale);
        else
            rb.velocity = velocity;
    }

    protected void StopMovementDrive(bool immediate = false)
    {
        if (motionMotor != null)
            motionMotor.StopDriving(immediate);
        else if (immediate)
            rb.velocity = Vector2.zero;
    }

    protected void SnapVelocity(Vector2 velocity)
    {
        if (motionMotor != null)
            motionMotor.SnapVelocity(velocity);
        else
            rb.velocity = velocity;
    }

    protected void ApplyImpulse(Vector2 impulse, float angularImpulse = 0f)
    {
        if (motionMotor != null)
            motionMotor.AddImpulse(impulse, angularImpulse);
        else
        {
            rb.AddForce(impulse, ForceMode2D.Impulse);
            if (!Mathf.Approximately(angularImpulse, 0f))
                rb.AddTorque(angularImpulse, ForceMode2D.Impulse);
        }
    }

  
}
