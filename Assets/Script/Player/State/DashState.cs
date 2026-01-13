using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseState
{
    private float dashTimer = 0.15f;
    private Vector2 dashDirection;

    private float dashCD = 0.1f;

    public override void Enter()
    {

        Debug.Log("Enter Dash State");
        dashDirection = new Vector2(InputManager.Instance.GetMoveX(), InputManager.Instance.GetMoveY()).normalized;

        if(dashDirection.magnitude < 0.1f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - Player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        Player.SetVelocity(dashDirection.normalized * Player.dashSpeed);

        //Player.SetColor(Color.red);
        //Player.StartGhostEffect();

        Timer.Register(dashTimer,
            onComplete: () =>
            {
                if (InputManager.Instance.Mouse1())
                {
                    Player.ChangeState(Player.defenceState);
                }
                else
                {
                    Player.ChangeState(Player.moveState);
                }  
            },
            onUpdate: (float t) =>
            {
                float currentSpeed = Mathf.Lerp(Player.dashSpeed, Player.moveSpeed, t);

                Player.SetVelocity(dashDirection * currentSpeed);
            });
    }

    public override void Exit()
    {

    }

    public override void LogicUpdate()
    {

    }

    public override void PhysicsUpdate()
    {

    }
}
