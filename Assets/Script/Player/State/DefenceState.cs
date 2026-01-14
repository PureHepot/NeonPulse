using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenceState : BaseState
{
    private ShieldController shieldSystem;

    private Transform shieldPivot;

    private Vector2 currentVelocity;

    private Vector2 refVelocity;

    // 惯性值
    private float smoothTime = 0.15f;

    public override void Enter()
    {
        Debug.Log("Enter Defence State");

        if (shieldSystem == null)
        {
            shieldSystem = Player.GetComponentInChildren<ShieldController>();
        }

        shieldPivot = shieldSystem.transform;

        shieldSystem.SetDefend(true);

        currentVelocity = Player.Velocity;

        refVelocity = Vector2.zero;

        //Player.SetVelocity(Player.Velocity);

    }

    public override void Exit()
    {
        if (shieldSystem != null)
            shieldSystem.SetDefend(false);
    }

    public override void LogicUpdate()
    {
        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        Vector2 direction = (mousePos - Player.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        shieldPivot.rotation = Quaternion.Lerp(shieldPivot.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 20f);

        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 targetInput = Vector2.ClampMagnitude(new Vector2(x, y).normalized, 1f);

        Vector2 targetVelocity = targetInput * Player.defenceSpeed;

        currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref refVelocity, smoothTime);

        Player.SetVelocity(currentVelocity);

        //--- 状态切换 ---

        if (Player.CanDash() && Input.GetKeyDown(KeyCode.Space))
        {
            Player.ChangeState(Player.dashState);
            return;
        }

        if (!InputManager.Instance.Mouse1())
        {
            Player.ChangeState(Player.moveState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
    }
}
