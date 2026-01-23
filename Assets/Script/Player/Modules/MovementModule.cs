using UnityEngine;

public class MovementModule : PlayerModule
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
        if (player.IsStunned || player.IsDead || player.IsDashing) return;

        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 input = new Vector2(x, y);

        Vector2 targetVelocity = input.normalized * GetFinalSpeed();

        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref refVelocity,
            smoothTime
        );

        player.SetVelocity(currentVelocity);
    }

    public override void UpgradeModule(ModuleType moduleType, StatType statType)
    {
        if (statType == StatType.MoveSpeed)
        {
            RecalculateStats();
            Debug.Log($"[MovementModule] 移速升级: {GetFinalSpeed():F2}");
        }
    }

    private void RecalculateStats()
    {
        baseMoveSpeed =
            UpgradeManager.Instance.GetStat(ModuleType.Movement, StatType.MoveSpeed);

        if (baseMoveSpeed <= 0) baseMoveSpeed = 5f;

        speedMultiplier = 1f;
    }

    private float GetFinalSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }
}
