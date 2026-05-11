using UnityEngine;

public class EnemyCollisionEvent : EnemyBoundaryConstraint
{
    protected override void ApplyConstraint(in EnemyBoundaryService.EnemyBoundaryContext context)
    {
        if (!context.HasArmedBoundaryEvents || context.Rigidbody == null)
            return;

        Vector2 currentPosition = context.Rigidbody.position;
        float clampedX = Mathf.Clamp(currentPosition.x, context.MinX, context.MaxX);
        float clampedY = Mathf.Clamp(currentPosition.y, context.MinY, context.MaxY);
        context.Rigidbody.position = new Vector2(clampedX, clampedY);
    }

}
