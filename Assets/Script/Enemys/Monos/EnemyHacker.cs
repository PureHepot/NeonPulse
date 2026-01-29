using UnityEngine;

public class EnemyHacker : EnemyBase
{
    [Header("Hacker Movement")]
    public float keepDistance = 1f;
    public float adjustSpeed = 1f;

    [Header("Wiggle")]
    public float waveFrequency = 1.5f;
    public float waveMagnitude = 0.6f;

    private float noiseOffset;

    public override void OnSpawn()
    {
        base.OnSpawn();
        noiseOffset = Random.Range(0f, 100f);
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (playerTransform.position - transform.position);
        float dist = toPlayer.magnitude;

        Vector2 dir = toPlayer.normalized;

        // æ‡¿Î–ﬁ’˝
        float distanceFactor = Mathf.Clamp(dist - keepDistance, -1f, 1f);

        Vector2 radialDir = dir * distanceFactor;

        // ª∑»∆»≈∂Ø
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        float wave = Mathf.Sin(Time.time * waveFrequency + noiseOffset) * waveMagnitude;

        Vector2 finalDir = (radialDir + perpendicular * wave).normalized;

        Vector2 desiredVelocity = finalDir * moveSpeed;

        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, Time.deltaTime * 6f);
    }
}
