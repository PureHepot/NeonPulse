using System.Collections;
using UnityEngine;

public class DashModule : PlayerModule
{
    private const string DashCooldownStatId = "move.dashcooldown";
    private const string DashForceStatId = "move.dashforce";
    private const string MoveSpeedStatId = "move.speed";

    [Header("Visuals")]
    public TrailRenderer dashTrail;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.3f;
    public AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

    [Header("Collision Layers")]
    public string playerLayerName = "Player";
    public string enemyLayerName = "Enemy";
    public string enemyBulletLayerName = "EnemyBullet";

    private float dashCooldown;
    private float lastDashTime = -999f;
    private Vector2 dashDirection;
    private int playerLayerID;
    private int enemyLayerID;
    private int enemyBulletLayerID;

    protected override void OnInitialize()
    {
        RecalculateStats();

        playerLayerID = LayerMask.NameToLayer(playerLayerName);
        enemyLayerID = LayerMask.NameToLayer(enemyLayerName);
        enemyBulletLayerID = LayerMask.NameToLayer(enemyBulletLayerName);

        if (dashTrail != null)
        {
            dashTrail.gameObject.SetActive(true);
            dashTrail.emitting = false;
        }
    }

    protected override void OnActivate()
    {
        RecalculateStats();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || player.IsStunned || player.IsDashing || !HasControl)
            return;

        if (InputManager.Instance.Space() && IsReady())
            StartCoroutine(DashRoutine());
    }

    public bool IsReady()
    {
        return Time.time >= lastDashTime + dashCooldown + dashDuration;
    }

    private void RecalculateStats()
    {
        dashCooldown = GetStat(DashCooldownStatId, dashCooldown);
        dashForce = GetStat(DashForceStatId, dashForce);
    }

    private void OnDashStart()
    {
        lastDashTime = Time.time;
        if (dashTrail != null)
            dashTrail.emitting = true;

        AudioManager.Instance.PlayEffect("Dash");
        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, true);

        if (enemyBulletLayerID != -1)
            Physics2D.IgnoreLayerCollision(playerLayerID, enemyBulletLayerID, true);
    }

    private void OnDashEnd()
    {
        if (dashTrail != null)
            dashTrail.emitting = false;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
        if (enemyBulletLayerID != -1)
            Physics2D.IgnoreLayerCollision(playerLayerID, enemyBulletLayerID, false);
    }

    private IEnumerator DashRoutine()
    {
        OnDashStart();

        bool oldState = player.IsDashing;
        player.IsDashing = true;

        dashDirection = new Vector2(InputManager.Instance.GetMoveX(), InputManager.Instance.GetMoveY()).normalized;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        if (player.Rigid2d != null)
            player.AddImpulse(dashForce * dashDirection);

        float targetSpeed = GetStat(MoveSpeedStatId, 5f);
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += DeltaTime;
            float progress = timer / dashDuration;
            float curveValue = speedCurve.Evaluate(progress);
            float currentSpeed = Mathf.Lerp(targetSpeed, dashForce, curveValue);
            player.SnapVelocity(dashDirection * currentSpeed);
            yield return null;
        }

        player.IsDashing = oldState;
        OnDashEnd();
    }
}
