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
            StopMovementDrive();
            return;
        }

        Vector2 toPlayer = (playerTransform.position - transform.position);
        float dist = toPlayer.magnitude;

        Vector2 dir = toPlayer.normalized;

        // 距离修正
        float distanceFactor = Mathf.Clamp(dist - keepDistance, -1f, 1f);

        Vector2 radialDir = dir * distanceFactor;

        // 环绕扰动
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        float wave = Mathf.Sin(Time.time * waveFrequency + noiseOffset) * waveMagnitude;

        Vector2 finalDir = (radialDir + perpendicular * wave).normalized;

        Vector2 desiredVelocity = finalDir * moveSpeed;

        DriveVelocity(desiredVelocity, 1.2f);
    }
}
