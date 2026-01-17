using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitedState : BaseState
{
    public override void Enter()
    {
        Timer.Register(Player.stunDuration,
            () =>
            {
                Player.ChangeState(Player.moveState);
            });

        Player.StartCoroutine(Player.InvincibilityRoutine());

        Player.PlayHurtVisuals();

        Vector2 knockbackDir = ((Vector2)Player.transform.position - Player.lastAttackerPos).normalized;

        Player.SetVelocity(Vector2.zero);

        Player.Rigid2d.AddForce(knockbackDir * Player.knockbackForce, ForceMode2D.Impulse);
    }

    public override void Exit()
    {

    }

    public override void LogicUpdate()
    {

    }

    public override void PhysicsUpdate()
    {
        Player.Rigid2d.drag = 1f;
    }
}
