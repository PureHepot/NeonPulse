using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashModule : PlayerModule
{
    [Header("Visuals")]
    public TrailRenderer dashTrail;

    [Header("Dash Stats")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.0f;

    private float lastDashTime = -999f;

    private Vector2 dashDirection;


    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (dashTrail != null)
        {
            dashTrail.gameObject.SetActive(true);
            dashTrail.emitting = false;
        }
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

    public void OnDashEnd()
    {
        if (dashTrail != null) dashTrail.emitting = false;
    }


    public override void UpgradeModule()
    {
        dashCooldown *= 0.8f; // 冷却缩减 20%
    }


    public override void OnModuleUpdate()
    {
        base.OnModuleUpdate();
        if (InputManager.Instance.Space() && IsReady())
        {
            StartCoroutine(DashRoutine());
        }
    }


    IEnumerator DashRoutine()
    {
        OnDashStart();

        bool oldStunState = player.IsDashing;
        player.IsDashing = true;

        // 施加冲刺力
        dashDirection = new Vector2(InputManager.Instance.GetMoveX(), InputManager.Instance.GetMoveY()).normalized;

        if (dashDirection.magnitude < 0.1f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        player.SetVelocity(dashDirection.normalized * dashForce);

        yield return new WaitForSeconds(dashDuration);

        // 冲刺结束，恢复控制
        player.IsDashing = oldStunState;
        player.SetVelocity(Vector2.zero);

        OnDashEnd();
    }
    public override void OnActivate()
    {
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
    }
}
