using UnityEngine;

public class EnemyBomber : EnemyBase
{
    public enum BomberState
    {
        Chase,
        Warning,
        Explode
    }

    [Header("检测参数")]
    public float detectRadius = 1f;
    public LayerMask playerLayer;

    [Header("引爆参数")]
    public float warningTime = 2f;
    public float explodeRadius = 2f;
    public int explodeDamage = 2;

    [Header("闪烁效果")]
    public float blinkSpeed = 15f;

    [Header("爆炸特效")]
    public GameObject explodeFxPrefab;

    private BomberState state;
    private float warningTimer;

    public override void OnSpawn()
    {
        base.OnSpawn();

        state = BomberState.Chase;
        warningTimer = 0;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDead) return;

        switch (state)
        {
            case BomberState.Chase:
                CheckPlayer();
                break;

            case BomberState.Warning:
                WarningState();
                break;
        }
    }

    protected override void MoveBehavior()
    {
        if (state != BomberState.Chase) return;
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;

        Vector2 targetVel = dir * moveSpeed;
        DriveVelocity(targetVel, 1.2f);
    }

    // 检测
    private void CheckPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerLayer);
        if (hit != null)
        {
            playerTransform = hit.transform;
            EnterWarning();
        }
    }

    // 预警
    private void EnterWarning()
    {
        state = BomberState.Warning;
        StopMovementDrive();
        warningTimer = warningTime;
    }

    private void WarningState()
    {
        warningTimer -= Time.deltaTime;

        float t = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        bodyRenderer.color = Color.Lerp(normalColor, Color.red, t);

        if (warningTimer <= 0f)
        {
            Explode();
        }
    }

    // 爆炸
    private void Explode()
    {
        state = BomberState.Explode;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, playerLayer);
        foreach (var hit in hits)
        {
            var health = hit.GetComponentInChildren<HealthModule>();
            if (health != null)
                health.TakeDamage(explodeDamage, transform);
        }

        PlayExplodeFX();

        Die();   
    }

    protected override void Die()
    {
        base.Die();
    }

    private void PlayExplodeFX()
    {
        if (explodeFxPrefab == null) return;

        GameObject fx = ObjectPoolManager.Instance.Get(explodeFxPrefab, transform.position, Quaternion.identity);

        Timer.Register(1.5f, () =>
        {
            ObjectPoolManager.Instance.Return(fx);
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
