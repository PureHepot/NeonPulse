using System.Collections;
using UnityEngine;

public class DashModule : PlayerModule
{
    [Header("Visuals")]
    public TrailRenderer dashTrail;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.3f;
    [Tooltip("速度衰减曲线")]
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

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        RecalculateStats();

        // 获取Layer ID
        playerLayerID = LayerMask.NameToLayer(playerLayerName);
        enemyLayerID = LayerMask.NameToLayer(enemyLayerName);
        enemyBulletLayerID = LayerMask.NameToLayer(enemyBulletLayerName);

        if (dashTrail != null)
        {
            dashTrail.gameObject.SetActive(true);
            dashTrail.emitting = false;
        }

        Debug.Log($"[DashModule] 初始化 CD={dashCooldown:F2}s");
    }

    public override void OnModuleUpdate()
    {
        if (player.IsDead || player.IsStunned || player.IsDashing) return;

        if (InputManager.Instance.Space() && IsReady())
        {
            StartCoroutine(DashRoutine());
        }
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        if (statType == StatType.DashCooldown)
        {
            RecalculateStats();
            Debug.Log($"[DashModule] 冷却升级 → {dashCooldown:F2}s");
        }
    }

    private void RecalculateStats()
    {
        dashCooldown = UpgradeManager.Instance.GetStat(ModuleType.Dash, StatType.DashCooldown);
        dashForce = UpgradeManager.Instance.GetStat(ModuleType.Dash, StatType.DashForce);
    }

    public bool IsReady()
    {
        return Time.time >= lastDashTime + dashCooldown;
    }

    public void OnDashStart()
    {
        lastDashTime = Time.time;
        if (dashTrail != null) dashTrail.emitting = true;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, true);

        if (enemyBulletLayerID != -1)
            Physics2D.IgnoreLayerCollision(playerLayerID, enemyBulletLayerID, true);
    }

    private void OnDashEnd()
    {
        if (dashTrail != null) dashTrail.emitting = false;

        Physics2D.IgnoreLayerCollision(playerLayerID, enemyLayerID, false);
        if (enemyBulletLayerID != -1)
            Physics2D.IgnoreLayerCollision(playerLayerID, enemyBulletLayerID, false);
    }

    IEnumerator DashRoutine()
    {
        OnDashStart();

        bool oldState = player.IsDashing;
        player.IsDashing = true;

        // 确定方向
        dashDirection = new Vector2(
            InputManager.Instance.GetMoveX(),
            InputManager.Instance.GetMoveY()
        ).normalized;

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        // 获取当前正常移速
        float targetSpeed = UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed);
        if (targetSpeed <= 0) targetSpeed = 5f; // 防止取不到数据导致不动

        // 开始平滑衰减循环
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / dashDuration; // 0 到 1

            float curveValue = speedCurve.Evaluate(progress);

            float currentSpeed = Mathf.Lerp(targetSpeed, dashForce, curveValue);

            player.SetVelocity(dashDirection * currentSpeed);

            yield return null;
        }

        player.IsDashing = oldState; // 解锁输入

        OnDashEnd();
    }
}
