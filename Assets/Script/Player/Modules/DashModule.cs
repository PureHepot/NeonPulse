using System.Collections;
using UnityEngine;

public class DashModule : PlayerModule
{
    [Header("Visuals")]
    public TrailRenderer dashTrail;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;

    private float dashCooldown;
    private float lastDashTime = -999f;

    private Vector2 dashDirection;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        RecalculateStats();

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
        dashCooldown =
            UpgradeManager.Instance.GetStat(ModuleType.Dash, StatType.DashCooldown);

        if (dashCooldown <= 1f)
            dashCooldown = 1f;
    }

    public bool IsReady()
    {
        return Time.time >= lastDashTime + dashCooldown;
    }

    public void OnDashStart()
    {
        lastDashTime = Time.time;
        if (dashTrail != null) dashTrail.emitting = true;
    }

    private void OnDashEnd()
    {
        if (dashTrail != null) dashTrail.emitting = false;
    }

    IEnumerator DashRoutine()
    {
        OnDashStart();

        bool oldState = player.IsDashing;
        player.IsDashing = true;

        dashDirection = new Vector2(
            InputManager.Instance.GetMoveX(),
            InputManager.Instance.GetMoveY()
        ).normalized;

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        player.SetVelocity(dashDirection * dashForce);

        yield return new WaitForSeconds(dashDuration);

        player.IsDashing = oldState;
        player.SetVelocity(Vector2.zero);

        OnDashEnd();
    }
}
