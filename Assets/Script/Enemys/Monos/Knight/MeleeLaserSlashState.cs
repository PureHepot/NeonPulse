using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MeleeLaserSlashState : BossBaseState
{
    private KnightBoss knight;
    private int subPhase = 0; // 0: 瞄准, 1: 冲刺, 2: 旋转+激光
    private Vector3 dashTarget;
    private float currentSpinAngle = 0f;
    private LaserBeam leftL, rightL;

    // 【新增】：用于记录当前已经连击了多少次
    private int currentAttackCount = 0;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;
        subPhase = 0;
        currentSpinAngle = 0f;
        currentAttackCount = 0; // 每次进入状态时清零

        // 刀刃打开进入斩击姿态
        knight.LeftBlade?.MoveToLocal(new Vector3(-0.6f, 1.2f, 0), Vector3.zero, 0.3f);
        knight.RightBlade?.MoveToLocal(new Vector3(0.6f, 1.2f, 0), Vector3.zero, 0.3f);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (subPhase)
        {
            case 0: // 【极速瞄准阶段】
                if (stateTimer < 0.3f && knight.playerTarget != null)
                {
                    // 机头死死盯住玩家，并预测冲刺落点（越过玩家 5 米）
                    Vector3 dir = (knight.playerTarget.position - knight.transform.position).normalized;
                    knight.transform.up = -dir;
                    dashTarget = knight.playerTarget.position + dir * 5f;
                }
                else
                {
                    subPhase = 1;
                    stateTimer = 0;
                }
                break;

            case 1: // 【冲刺靠近玩家】
                knight.transform.position = Vector3.MoveTowards(knight.transform.position, dashTarget, 40f * Time.deltaTime);

                // 到达目标点，或者冲刺超时（防卡死），立刻触发旋转斩击
                if (Vector3.Distance(knight.transform.position, dashTarget) < 0.1f || stateTimer > 0.5f)
                {
                    subPhase = 2;
                    currentSpinAngle = 0f; // 确保每次旋转前角度归零
                    FireLasers();
                }
                break;

            case 2: // 【旋转斩击 + 激光爆发】
                float rotateSpeed = 1500f; // 超高转速
                float angleStep = rotateSpeed * Time.deltaTime;

                knight.transform.Rotate(0, 0, angleStep);
                currentSpinAngle += angleStep;

                // 刚好转完一圈 (360度)
                if (currentSpinAngle >= 360f)
                {
                    
                    // 立刻掐断当前激光
                    /*
                    if (leftL) Object.Destroy(leftL.gameObject);
                    if (rightL) Object.Destroy(rightL.gameObject);
                    */
                    currentAttackCount++; // 连击次数 +1

                    // 判断是否打满了预设的近战连击数 (读取自预制件，通常是 3 次)
                    if (currentAttackCount < knight.meleeRepeatCount)
                    {
                        // --- 【核心修改：继续连击】 ---
                        // 没有打完 3 次，回到阶段 0 重新瞄准玩家！
                        subPhase = 0;
                        stateTimer = 0;
                        currentSpinAngle = 0f;
                    }
                    else
                    {
                        // --- 连击彻底结束，执行统一的归队/退出逻辑 ---
                        if (knight.hasTriggeredFinalPhase && knight.endAttackState.currentStep <= 6)
                        {
                            knight.SwitchState(knight.endAttackState);
                        }
                        else
                        {
                            knight.SwitchState(knight.observeState);
                        }
                    }
                }
                break;
        }
    }

    private void FireLasers()
    {
        leftL = Object.Instantiate(knight.laserPrefab, knight.LeftBlade.transform.position, Quaternion.identity);
        rightL = Object.Instantiate(knight.laserPrefab, knight.RightBlade.transform.position, Quaternion.identity);

        // 压秒取消预警线，实现瞬间爆发
        leftL.warningTime = 0.01f;
        rightL.warningTime = 0.01f;

        // 给一个足够长的存活时间，确保旋转完毕前激光不会消失
        leftL.activeTime = 2.0f;
        rightL.activeTime = 2.0f;

        // 反向追踪发射
        leftL.FireTracking(knight.LeftBlade.transform, 0f, true);
        rightL.FireTracking(knight.RightBlade.transform, 0f, true);
    }
}
