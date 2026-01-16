using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChaser : EnemyBase
{
    [Header("Chaser Settings")]
    public float rotateSpeed = 200f; // 旋转速度

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        Vector2 direction = (playerTransform.position - transform.position).normalized;

        rb.velocity = direction * moveSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
    }
}
