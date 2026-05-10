using UnityEngine;

public class EnemyTeleportEvent : EnemyBoundaryReaction
{
    protected override bool React(in EnemyBoundaryService.EnemyBoundaryContext context)
    {
        if (enemy == null)
            return false;

        enemy.transform.position = -enemy.transform.position;
        return true;
    }
}
