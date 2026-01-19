using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementModule : PlayerModule
{
    [Header("Move Settings")]
    public float baseMoveSpeed = 5f;

    private float speedMultiplier = 1.0f;

    private Vector2 currentVelocity;

    private Vector2 refVelocity;

    // 惯性值
    private float smoothTime = 0.15f;

    public override void OnModuleUpdate()
    {
        if (player.IsStunned || player.IsDead) return;

        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 targetInput = Vector2.ClampMagnitude(new Vector2(x, y).normalized, 1f);

        Vector2 targetVelocity = targetInput * baseMoveSpeed;

        currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref refVelocity, smoothTime);

        player.SetVelocity(currentVelocity);

    }

    public override void UpgradeModule()
    {
        speedMultiplier += 0.1f;
        Debug.Log($"速度升级！当前倍率: {speedMultiplier}");
    }
}
