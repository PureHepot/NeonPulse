using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChaser : EnemyBase
{
    [Header("Chaser Settings")]
    public float rotateSpeed = 200f; // 旋转速度

    [Header("Wiggle Settings")]
    public float waveFrequency = 2f; // 摆动频率
    public float waveMagnitude = 1f; // 摆动幅度

    private float noiseOffset;

    public override void OnSpawn()
    {
        base.OnSpawn();
        noiseOffset = Random.Range(0f, 100f);
    }


    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        Vector2 toPlayer = (playerTransform.position - transform.position).normalized;

        Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x);

        float wave = Mathf.Sin(Time.time * waveFrequency + noiseOffset) * waveMagnitude;

        Vector2 finalDir = (toPlayer + perpendicular * wave).normalized;

        rb.velocity = finalDir * moveSpeed;

        Vector2 lookDir = finalDir;

        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
    }
}
