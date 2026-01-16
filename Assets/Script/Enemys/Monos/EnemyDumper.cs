using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDumper : EnemyBase
{
    [Header("Dumper Settings")]
    public float prepareTime = 2f;
    public float dashForce = 20f;

    private float timer;
    private bool isDashing;

    public override void OnSpawn()
    {
        base.OnSpawn(); 
        timer = prepareTime;
        isDashing = false;
    }

    protected override void MoveBehavior()
    {
        if (isDashing)
        {
            if (rb.velocity.magnitude < 1f)
            {
                isDashing = false;
                timer = prepareTime;
            }
            return;
        }

        rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 5f);

        if (playerTransform != null)
        {
            Vector2 dir = playerTransform.position - transform.position;
            transform.up = dir;
        }

        timer -= Time.fixedDeltaTime;
        if (timer <= 0 && playerTransform != null)
        {
            Dash();
        }
    }

    void Dash()
    {
        isDashing = true;
        Vector2 dir = (playerTransform.position - transform.position).normalized;

        // 瞬间施加力
        rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        //TODO: 添加冲刺特效或音效
    }
}
