using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ContinuousPhysicsMotor2D : MonoBehaviour
{
    [SerializeField] private float baseLinearResponse = 12f;
    [SerializeField] private float angularDamping = 10f;

    private Rigidbody2D body;
    private Vector2 desiredVelocity;
    private float responseMultiplier = 1f;

    public Vector2 DesiredVelocity => desiredVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Configure(float linearResponse, float angularDampingValue)
    {
        baseLinearResponse = Mathf.Max(0f, linearResponse);
        angularDamping = Mathf.Max(0f, angularDampingValue);
    }

    public void SetDesiredVelocity(Vector2 velocity, float responseScale = 1f)
    {
        desiredVelocity = velocity;
        responseMultiplier = Mathf.Max(0f, responseScale);
    }

    public void StopDriving(bool immediate = false)
    {
        desiredVelocity = Vector2.zero;
        responseMultiplier = 1f;

        if (immediate && body != null)
            body.velocity = Vector2.zero;
    }

    public void SnapVelocity(Vector2 velocity)
    {
        desiredVelocity = velocity;
        responseMultiplier = 1f;

        if (body != null)
            body.velocity = velocity;
    }

    public void AddImpulse(Vector2 impulse, float angularImpulse = 0f)
    {
        if (body == null)
            return;

        body.AddForce(impulse, ForceMode2D.Impulse);
        if (!Mathf.Approximately(angularImpulse, 0f))
            body.AddTorque(angularImpulse, ForceMode2D.Impulse);
    }

    public void ResetMotion()
    {
        desiredVelocity = Vector2.zero;
        responseMultiplier = 1f;

        if (body == null)
            return;

        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    public void ClampPositionToBounds(Vector2 min, Vector2 max)
    {
        if (body == null || !body.simulated)
            return;

        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(body.position.x, min.x, max.x),
            Mathf.Clamp(body.position.y, min.y, max.y));

        Vector2 adjustedVelocity = body.velocity;
        Vector2 adjustedDesiredVelocity = desiredVelocity;
        const float epsilon = 0.0001f;

        if (clampedPosition.x <= min.x + epsilon)
        {
            if (adjustedVelocity.x < 0f)
                adjustedVelocity.x = 0f;
            if (adjustedDesiredVelocity.x < 0f)
                adjustedDesiredVelocity.x = 0f;
        }
        else if (clampedPosition.x >= max.x - epsilon)
        {
            if (adjustedVelocity.x > 0f)
                adjustedVelocity.x = 0f;
            if (adjustedDesiredVelocity.x > 0f)
                adjustedDesiredVelocity.x = 0f;
        }

        if (clampedPosition.y <= min.y + epsilon)
        {
            if (adjustedVelocity.y < 0f)
                adjustedVelocity.y = 0f;
            if (adjustedDesiredVelocity.y < 0f)
                adjustedDesiredVelocity.y = 0f;
        }
        else if (clampedPosition.y >= max.y - epsilon)
        {
            if (adjustedVelocity.y > 0f)
                adjustedVelocity.y = 0f;
            if (adjustedDesiredVelocity.y > 0f)
                adjustedDesiredVelocity.y = 0f;
        }

        body.position = clampedPosition;
        body.velocity = adjustedVelocity;
        desiredVelocity = adjustedDesiredVelocity;
    }

    private void FixedUpdate()
    {
        if (body == null || !body.simulated)
            return;

        float dt = Time.fixedDeltaTime;
        float linearResponse = Mathf.Max(0f, baseLinearResponse * responseMultiplier);
        if (linearResponse > 0f)
        {
            float lerpFactor = 1f - Mathf.Exp(-linearResponse * dt);
            body.velocity = Vector2.Lerp(body.velocity, desiredVelocity, lerpFactor);
        }
        else
        {
            body.velocity = desiredVelocity;
        }

        if (angularDamping > 0f && !Mathf.Approximately(body.angularVelocity, 0f))
        {
            float angularLerp = 1f - Mathf.Exp(-angularDamping * dt);
            body.angularVelocity = Mathf.Lerp(body.angularVelocity, 0f, angularLerp);
        }
    }
}
