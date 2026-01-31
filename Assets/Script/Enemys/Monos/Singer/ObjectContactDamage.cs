using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectContactDamage : MonoBehaviour
{
    [Header("伤害设置")]
    [Tooltip("碰到玩家造成的伤害值")]
    public int damage = 1;

    // 处理物理碰撞 (当 Collider 没有勾选 Is Trigger 时)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamage(collision.gameObject);
        }
    }

    // 处理触发器碰撞 (当 Collider 勾选了 Is Trigger 时，或者玩家是 Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DealDamage(other.gameObject);
        }
    }

    void DealDamage(GameObject playerObj)
    {
        // 尝试获取玩家的血量组件
        // 根据你的项目结构，可能是 HealthModule
        var health = playerObj.GetComponentInChildren<HealthModule>();

        if (health != null)
        {
            // 造成伤害
            health.TakeDamage(damage, transform);

            // 可选：在这里播放一个撞击音效
            // AudioManager.Instance.PlayEffect("Hit");
        }
    }
}
