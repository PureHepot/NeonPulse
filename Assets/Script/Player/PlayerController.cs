using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float locomotionResponse = 18f;
    [SerializeField] private float angularDamping = 14f;

    public Rigidbody2D Rigid2d { get; private set; }
    public Collider2D Colli2d { get; private set; }
    public ModuleManager Modules { get; private set; }
    public SpriteRenderer BodyRenderer { get; private set; }

    public bool IsStunned { get; set; }
    public bool IsDashing { get; set; }
    public bool IsDead { get; set; }
    public bool AcceptsInput { get; private set; } = true;
    public bool UseUnscaledTime { get; private set; }
    public float ModuleDeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    public bool IsPrimaryRuntimePlayer => PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayerObj == gameObject;

    public Action OnDeath;

    private ContinuousPhysicsMotor2D motionMotor;

    private void Awake()
    {
        Rigid2d = GetComponent<Rigidbody2D>();
        Colli2d = GetComponent<Collider2D>();
        Modules = GetComponent<ModuleManager>();
        motionMotor = GetComponent<ContinuousPhysicsMotor2D>();
        if (motionMotor == null)
            motionMotor = gameObject.AddComponent<ContinuousPhysicsMotor2D>();

        motionMotor.Configure(locomotionResponse, angularDamping);

        var bodyTransform = transform.Find("Body");
        if (bodyTransform != null)
            BodyRenderer = bodyTransform.GetComponent<SpriteRenderer>();
    }

    public void ConfigureRuntime(bool acceptsInput, bool useUnscaledTime)
    {
        AcceptsInput = acceptsInput;
        UseUnscaledTime = useUnscaledTime;
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (IsStunned || motionMotor == null)
            return;

        motionMotor.SetDesiredVelocity(velocity);
    }

    public void SnapVelocity(Vector2 velocity)
    {
        if (motionMotor == null)
            return;

        motionMotor.SnapVelocity(velocity);
    }

    public void StopMovement(bool immediate = false)
    {
        if (motionMotor == null)
            return;

        motionMotor.StopDriving(immediate);
    }

    public void AddImpulse(Vector2 impulse, float angularImpulse = 0f)
    {
        if (motionMotor == null)
            return;

        motionMotor.AddImpulse(impulse, angularImpulse);
    }

    public void ResetMotion()
    {
        if (motionMotor == null)
            return;

        motionMotor.ResetMotion();
    }

    public void ClampToBounds(Vector2 min, Vector2 max)
    {
        if (motionMotor != null)
        {
            motionMotor.ClampPositionToBounds(min, max);
            return;
        }

        if (Rigid2d != null)
        {
            Rigid2d.position = new Vector2(
                Mathf.Clamp(Rigid2d.position.x, min.x, max.x),
                Mathf.Clamp(Rigid2d.position.y, min.y, max.y));
            return;
        }

        Vector3 position = transform.position;
        transform.position = new Vector3(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y),
            position.z);
    }

    public void SetInvincible(bool state)
    {
        var healthModule = Modules != null ? Modules.GetModule<HealthModule>(ModuleType.Health) : null;
        if (healthModule != null)
            healthModule.IsInvincible = state;
    }
}
