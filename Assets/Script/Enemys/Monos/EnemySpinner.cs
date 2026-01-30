using UnityEngine;

public class EnemySpinner : EnemyBase
{
    [Header("Rotation Settings")]
    public float rotateSpeed = 360f;

    [Header("追踪平滑")]
    public float followSmooth = 6f;

    [Header("Separation")]
    public float separationDistance = 1.5f;
    public float separationStrength = 2f;

    [Header("Wiggle")]
    public float waveFrequency = 2f;
    public float waveMagnitude = 0.5f;
    private float noiseOffset;

    public override void OnSpawn()
    {
        base.OnSpawn();
        noiseOffset = Random.Range(0f, 100f);
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;

        // 微摆动扰动
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        float wave = Mathf.Sin(Time.time * waveFrequency + noiseOffset) * waveMagnitude;

        Vector2 finalDir = (dir + perpendicular * wave).normalized;

        // 与同类保持距离
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationDistance, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            Vector2 away = (transform.position - hit.transform.position);
            float d = away.magnitude;
            if (d > 0 && d < separationDistance)
                finalDir += away.normalized * separationStrength * (1f - d / separationDistance);
        }

        // 平滑速度
        rb.velocity = Vector2.Lerp(rb.velocity, finalDir * moveSpeed, Time.fixedDeltaTime * followSmooth);

        // 自旋
        transform.Rotate(0, 0, rotateSpeed * Time.fixedDeltaTime);
    }
}
