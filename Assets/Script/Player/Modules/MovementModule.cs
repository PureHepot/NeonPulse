using UnityEngine;

public class MovementModule : PlayerModule
{
    private const string MoveSpeedStatId = "move.speed";

    [Header("Move Settings")]
    public float smoothTime = 0.15f;

    private float baseMoveSpeed;
    private float speedMultiplier = 1f;
    private Vector2 currentVelocity;
    private Vector2 refVelocity;

    protected override void OnInitialize()
    {
        RecalculateStats();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsStunned || player.IsDead || player.IsDashing || !HasControl)
            return;

        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 input = new Vector2(x, y);

        Vector2 targetVelocity = input.normalized * GetFinalSpeed();
        currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref refVelocity, smoothTime);
        player.SetVelocity(currentVelocity);
    }

    private void RecalculateStats()
    {
        baseMoveSpeed = GetStat(MoveSpeedStatId, 5f);
        speedMultiplier = 1f;
    }

    private float GetFinalSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }
}
