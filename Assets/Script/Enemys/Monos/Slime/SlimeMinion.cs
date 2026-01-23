using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeMinion : EnemyBase
{
    [Header("Minion Settings")]
    public float chaseForce = 5f;

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        rb.AddForce(dir * chaseForce);
    }
}
