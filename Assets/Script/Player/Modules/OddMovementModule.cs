using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class OddMovementModule : PlayerModule
{
    [Header("Move Settings")]
    public float smoothTime = 0.15f;

    private float baseMoveSpeed;
    private float speedMultiplier = 1f;

    private Vector2 currentVelocity;
    private Vector2 refVelocity;
    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        RecalculateStats();
    }
    public override void OnModuleUpdate()
    {
        if (player == null || player.IsStunned || player.IsDead || player.IsDashing || player.isPreview) return;

        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        Vector2 mouseWorld = MUtils.GetMouseWorldPosition();
        Vector2 lookDir = (mouseWorld - (Vector2)player.transform.position).normalized;
        Vector2 targetVelocity = lookDir * scrollDelta * 100f * GetFinalSpeed();
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref refVelocity,
            smoothTime
        );

        player.SetVelocity(currentVelocity);
    }
    public override void UpgradeModule(ModuleType ModuleType, StatType statType)
    {
        if (statType == StatType.OddMoveSpeed)
        {
            RecalculateStats();
            Debug.Log($"[MovementModule] ÒÆËÙÉý¼¶: {GetFinalSpeed():F2}");
        }
    }
    private void RecalculateStats()
    {
        baseMoveSpeed =
            UpgradeManager.Instance.GetStat(ModuleType.OddMovement, StatType.OddMoveSpeed);

        if (baseMoveSpeed <= 0) baseMoveSpeed = 5f;

        speedMultiplier = 1f;
    }

    private float GetFinalSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }
}
