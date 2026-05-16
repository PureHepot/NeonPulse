using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightMeleeState : BossBaseState
{
    private KnightBoss knight;
    private Vector3 targetPos;
    private int currentAttackCount = 0;

    // 0: 瞄准, 1: 冲刺, 2: 快速旋转, 3: 最终恢复
    private int subPhase = 0;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;

        currentAttackCount = 0;
        PrepareNextAttack();
    }

    private void PrepareNextAttack()
    {
        subPhase = 0; // 回到瞄准阶段
        stateTimer = 0;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (subPhase)
        {
            case 0: // 阶段 0：瞄准 (解决“直接平移”的关键)
                if (knight.playerTarget != null)
                {
                    // 1. 实时锁定玩家位置
                    targetPos = knight.playerTarget.position;

                    // 2. 平滑旋转面向玩家
                    Vector3 dir = targetPos - knight.transform.position;
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dir);
                    knight.transform.rotation = Quaternion.Slerp(
                        knight.transform.rotation,
                        targetRot,
                        15f * Time.deltaTime // 旋转速度可以稍微快一点
                    );
                }

                // 瞄准时间结束，开始冲刺
                if (stateTimer >= knight.meleeAimDuration)
                {
                    subPhase = 1;
                    stateTimer = 0;
                }
                break;

            case 1: // 阶段 1：高速冲刺
                knight.transform.position = Vector3.MoveTowards(
                    knight.transform.position,
                    targetPos,
                    knight.meleeDashSpeed * Time.deltaTime
                );

                if (Vector3.Distance(knight.transform.position, targetPos) < 0.1f)
                {
                    subPhase = 2;
                    stateTimer = 0;
                }
                break;

            case 2: // 阶段 2：快速旋转一圈
                float rotateSpeed = 360f / knight.meleeSpinDuration;
                knight.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

                if (stateTimer >= knight.meleeSpinDuration)
                {
                    currentAttackCount++;

                    if (currentAttackCount < knight.meleeRepeatCount)
                    {
                        // 衔接：回到瞄准阶段进行下一次攻击
                        PrepareNextAttack();
                    }
                    else
                    {
                        // 结束：进入最终恢复阶段
                        subPhase = 3;
                        stateTimer = 0;
                    }
                }
                break;

            case 3: // 阶段 3：最终恢复
                if (knight.playerTarget != null)
                {
                    Vector3 dir = knight.playerTarget.position - knight.transform.position;
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dir);
                    knight.transform.rotation = Quaternion.Slerp(
                        knight.transform.rotation,
                        targetRot,
                        10f * Time.deltaTime
                    );
                }

                if (stateTimer >= knight.meleeRecoveryDuration)
                {
                    knight.SwitchState(knight.observeState);
                }
                break;
        }
    }
}
