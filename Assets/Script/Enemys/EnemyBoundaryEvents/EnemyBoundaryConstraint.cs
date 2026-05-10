using UnityEngine;

public abstract class EnemyBoundaryConstraint : MonoBehaviour
{
    protected EnemyBase enemy;
    protected Rigidbody2D enemyRb;

    protected virtual void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        enemyRb = GetComponent<Rigidbody2D>();
    }

    internal void TickConstraint(in EnemyBoundaryService.EnemyBoundaryContext context)
    {
        ApplyConstraint(context);
    }

    protected abstract void ApplyConstraint(in EnemyBoundaryService.EnemyBoundaryContext context);
}
