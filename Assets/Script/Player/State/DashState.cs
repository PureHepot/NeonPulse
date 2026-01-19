using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseState
{
    DashModule dashModule;
    private Vector2 dashDirection;

    public DashState(PlayerController player) : base(player)
    {
    }

    public override bool CanBeInterrupted()
    {
        return false;
    }

    public override void Enter()
    {
        dashModule = Player.Modules.GetModule<DashModule>(ModuleType.Dash);

        if (dashModule == null)
        {
            //Player.ChangeState(Player.moveState);
            return;
        }

        dashModule.OnDashStart();

        dashDirection = new Vector2(InputManager.Instance.GetMoveX(), InputManager.Instance.GetMoveY()).normalized;

        if(dashDirection.magnitude < 0.1f)
        {
            Vector3 dir = MUtils.GetMouseWorldPosition() - Player.transform.position;
            dashDirection = new Vector2(dir.x, dir.y).normalized;
        }

        Player.SetVelocity(dashDirection.normalized * dashModule.dashForce);

        //Player.SetColor(Color.red);
        //Player.StartGhostEffect();

        //Timer.Register(dashTimer,
        //    onComplete: () =>
        //    {
        //        Player.ChangeState(Player.moveState); 
        //    },
        //    onUpdate: (float t) =>
        //    {
        //        float currentSpeed = Mathf.Lerp(Player.dashSpeed, Player.moveSpeed, t);

        //        Player.SetVelocity(dashDirection * currentSpeed);
        //    });
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
