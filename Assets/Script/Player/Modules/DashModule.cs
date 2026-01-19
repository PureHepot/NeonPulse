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

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (dashTrail != null)
        {
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


    public void UpgradeDashCooldown()
    {
        dashCooldown *= 0.8f; // 冷却缩减 20%
    }


    public override void OnModuleUpdate()
    {
        base.OnModuleUpdate();

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
