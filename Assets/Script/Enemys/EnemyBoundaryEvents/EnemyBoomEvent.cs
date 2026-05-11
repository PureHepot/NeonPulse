using UnityEngine;

public class EnemyBoomEvent : EnemyBoundaryReaction
{
    protected override bool React(in EnemyBoundaryService.EnemyBoundaryContext context)
    {
        if (enemy == null)
            return false;

        enemy.TakeDamage(int.MaxValue);
        return true;
    }
}
