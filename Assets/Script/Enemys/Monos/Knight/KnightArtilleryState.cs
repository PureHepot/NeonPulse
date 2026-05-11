using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightArtilleryState : BossBaseState
{
    private KnightBoss knight;
    private int subPhase = 0;
    public LaserBeam activeLaser; // 引用当前生成的激光实例
    private List<LaserBeam> activeLasers = new List<LaserBeam>();


    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;
        subPhase = 0;
        knight.HideAllExParts();

        float dur = knight.flightTransformDuration;

        // 【坐标修正】：让 BladeL 去到右边(0.4)，BladeR 去到左边(-0.4)
        // 这样刀刃会交叉合拢形成炮管
        knight.LeftBlade?.MoveToLocal(new Vector3(0.4f, -0.6f, 0), new Vector3(0, 0, 180), dur);
        knight.RightBlade?.MoveToLocal(new Vector3(-0.4f, -0.6f, 0), new Vector3(0, 0, 180), dur);

    }

    public override void Exit()
    {
        base.Exit();

        // 1. 强制清理所有还在发射的激光
        foreach (var laser in activeLasers)
        {
            if (laser != null) Object.Destroy(laser.gameObject);
        }
        activeLasers.Clear();

        

        Debug.Log("炮击形态被强行中断，已清理残留激光。");
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (subPhase)
        {
            case 0: // 变形阶段
                if (knight.playerTarget != null)
                {
                    Vector3 dir = knight.playerTarget.position - knight.transform.position;
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dir);
                    knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 5f * Time.deltaTime);
                }

                if (knight.AreAllPartsStatic()) subPhase = 1;
                break;

            case 1: // 开火阶段
                if (knight.laserPrefab != null)
                {
                    activeLaser = Object.Instantiate(knight.laserPrefab, knight.transform.position, Quaternion.identity);
                    activeLasers.Add(activeLaser);
                    activeLaser.gameObject.SetActive(true);
                    activeLaser.FireTracking(knight.transform, 1.5f);
                }
                subPhase = 2;
                stateTimer = 0;
                break;

            case 2: // 【核心同步修复】：扫射与等待激光结束
                if (knight.playerTarget != null)
                {
                    Vector3 dir = knight.playerTarget.position - knight.transform.position;
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dir);
                    knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 2f * Time.deltaTime);
                }

                // 不再使用 stateTimer 判定
                // 只有当激光脚本执行完 Destroy(gameObject) 后，activeLaser 才会变为 null
                if (activeLaser == null)
                {
                    subPhase = 3;
                    stateTimer = 0;

                    // 激光消失后，立即开始回收刀刃
                    float dur = knight.flightTransformDuration;
                    knight.LeftBlade?.ResetToInitial(dur);
                    knight.RightBlade?.ResetToInitial(dur);
                }
                break;

            case 3: // 回收阶段
                if (knight.AreAllPartsStatic())
                {
                    if (knight.hasTriggeredFinalPhase && knight.endAttackState.currentStep <= 6)
                    {
                        // 连招还在进行中，强制向指挥官状态报到，执行下一步
                        knight.SwitchState(knight.endAttackState);
                    }
                    else
                    {
                        if (Random.value > 0.5f) knight.SwitchState(knight.observeState);
                        else knight.SwitchState(knight.flightState);
                    }
                }
                break;
        }
    }
}
