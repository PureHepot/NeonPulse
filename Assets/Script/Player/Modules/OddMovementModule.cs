using UnityEngine;

public class OddMovementModule : PlayerModule
{
    private const string OddMoveSpeedStatId = "move.speed";

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

        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scrollDelta, 0f))
            return;

        Vector2 mouseWorld = MUtils.GetMouseWorldPosition();
        Vector2 lookDir = (mouseWorld - (Vector2)player.transform.position).normalized;
        Vector2 targetVelocity = lookDir * scrollDelta * 100f * GetFinalSpeed();
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref refVelocity,
            smoothTime);

        player.SetVelocity(currentVelocity);
    }

    private void RecalculateStats()
    {
        baseMoveSpeed = GetStat(OddMoveSpeedStatId, 5f);
        if (baseMoveSpeed <= 0f)
            baseMoveSpeed = 5f;

        speedMultiplier = 1f;
    }

    private float GetFinalSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }
}