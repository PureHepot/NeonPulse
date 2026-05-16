using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferModule : PlayerModule
{
    private const string BufferBaseHealthID = "weapon.bufferbasehealth";
    private const string BufferCooldownStatId = "utility.buffercooldown";

    [Header("Combat Settings")]
    public float bufferCooldown = 8f;

    [Header("Obstacle Settings")]
    public GameObject bufferObstaclePrefab;
    public float bufferRange = 2.5f;
    public float bufferSpreadAngle = 25f;

    private float cooldownTimer;

    protected override void OnInitialize()
    {
        cooldownTimer = 0f;
        RecalculateStats();
    }

    protected override void OnActivate()
    {
        RecalculateStats();
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || !HasControl)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= DeltaTime;

        if (InputManager.Instance.Mouse1Down() && cooldownTimer <= 0f)
        {
            SpawnObstacles();
            cooldownTimer = bufferCooldown;
        }
    }

    private void SpawnObstacles()
    {
        if (bufferObstaclePrefab == null)
            return;

        Vector3 playerPos = player.transform.position;
        Vector2 facingDir = (MUtils.GetMouseWorldPosition() - playerPos).normalized;
        float baseAngle = Mathf.Atan2(facingDir.y, facingDir.x) * Mathf.Rad2Deg;

        for (int i = -1; i <= 1; i++)
        {
            float angle = baseAngle + i * bufferSpreadAngle;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * bufferRange;
            ObjectPoolManager.Instance.Get(bufferObstaclePrefab, playerPos + offset, Quaternion.identity);
        }
    }

    private void RecalculateStats()
    {
        bufferCooldown = GetStat(BufferCooldownStatId, bufferCooldown);
    }
}
