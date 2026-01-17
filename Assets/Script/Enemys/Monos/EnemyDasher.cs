using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDasher : EnemyBase
{
    [Header("Orbit Settings (巡航设置)")]
    public float orbitDistance = 5f; 
    public float distanceCorrectionSpeed = 2f;

    [Header("Dash Settings (冲刺设置)")]
    public float dashInterval = 3f;
    public float dashForce = 25f;
    public float dashDrag = 2f;

    private float timer;
    private bool isDashing;

    public override void OnSpawn()
    {
        base.OnSpawn();
        timer = dashInterval;
        isDashing = false;
    }

    protected override void MoveBehavior()
    {
        // 始终让怪物朝向玩家 (几何风格常见设定)
        if (playerTransform != null)
        {
            Vector2 aimDir = playerTransform.position - transform.position;
            transform.up = aimDir;
        }

        if (isDashing)
        {
            HandleDashState();
        }
        else
        {
            HandleOrbitState();
        }
    }

    private void HandleOrbitState()
    {
        if (playerTransform == null) return;

        Vector2 toPlayer = playerTransform.position - transform.position;
        float currentDist = toPlayer.magnitude;
        Vector2 dirToPlayer = toPlayer.normalized;

        Vector2 tangentDir = new Vector2(-dirToPlayer.y, dirToPlayer.x);

        Vector2 radialDir = Vector2.zero;

        float distDiff = currentDist - orbitDistance;

        float correctionFactor = Mathf.Clamp(distDiff, -1f, 1f);
        radialDir = dirToPlayer * correctionFactor;

        Vector2 targetVelocity = (tangentDir * moveSpeed) + (radialDir * distanceCorrectionSpeed);

        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 5f);

        timer -= Time.fixedDeltaTime;
        if (timer <= 0)
        {
            StartDash();
        }
    }

    private void HandleDashState()
    {
        rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * dashDrag);

        // 当速度降到一定程度以下
        if (rb.velocity.magnitude < 2f)
        {
            isDashing = false;
            timer = dashInterval;
        }
    }

    void StartDash()
    {
        if (playerTransform == null) return;

        isDashing = true;

        Vector2 dir = (playerTransform.position - transform.position).normalized;

        rb.velocity = Vector2.zero;

        rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        // 可以在这里播放音效或粒子
        // ObjectPoolManager.Instance.Get(dashEffect, transform.position...);
    }
}
