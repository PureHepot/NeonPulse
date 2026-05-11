using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenseSensor : MonoBehaviour
{
    [Header("设置检测层级")]
    [Tooltip("勾选需要触发 Boss 防御的子弹层级（例如 PlayerBullet）")]
    public LayerMask hitLayer;

    private KnightBoss knight;

    private void Start()
    {
        knight = GetComponentInParent<KnightBoss>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 使用位运算检查碰撞体的 Layer 是否在 hitLayer 的掩码中
        if (((1 << collision.gameObject.layer) & hitLayer) != 0)
        {
            // 确保 Knight 处于观察状态并触发旋转
            if (knight != null && knight.CurrentState is KnightObserveState observeState)
            {
                observeState.TriggerSingleSpinDefense();
            }
        }
    }
}
