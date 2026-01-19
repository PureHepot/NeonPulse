using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveState : BaseState
{
    private Vector2 currentVelocity;

    private Vector2 refVelocity;

    // 惯性值
    private float smoothTime = 0.15f;

    public MoveState(PlayerController player) : base(player){}

    public override void Enter()
    {
        Debug.Log("Enter move State");

        //currentVelocity = Player.Velocity;

        refVelocity = Vector2.zero;
    }

    public override void Exit()
    {
        
    }

    public override void LogicUpdate()
    {

    }

    public override void PhysicsUpdate()
    {
        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 targetInput = Vector2.ClampMagnitude(new Vector2(x, y).normalized, 1f);

        //Vector2 targetVelocity = targetInput * Player.moveSpeed;

        //currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref refVelocity, smoothTime);

        Player.SetVelocity(currentVelocity);
    }
}
