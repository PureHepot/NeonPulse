using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeHitboxProxy : MonoBehaviour
{
    private EnemyBase mainBoss;

    void Start()
    {
        mainBoss = GetComponentInParent<EnemyBase>();
    }

    // 皮撞到玩家 -> 玩家扣血
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && mainBoss != null)
        {
            var playerHealth = collision.gameObject.GetComponentInChildren<HealthModule>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(mainBoss.contactDamage, mainBoss.transform);
            }
        }
    }
}
