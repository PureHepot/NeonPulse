using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightFlightState : BossBaseState
{
    private KnightBoss knight;

    // 0-4: 变形入场动画阶段
    // 11: S型规避阶段
    // 15: 【新增】S型规避结束后的悬停瞄准阶段
    // 12: 直线冲刺阶段
    // 13: U型弯阶段
    // 20-24: 变形回收动画阶段
    private int subPhase = 0;

    private int currentAttackCount = 0;
    private bool isSCurveAttack;

    // 运动学缓存
    private Vector3 targetPos;
    private float turnDirection;
    private float sCurveTimer = 0f;

    // --- 冲刺锁定缓存 ---
    private Vector3 lockedDashDirection;
    private Vector3 lockedTargetPos;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;

        // 每次进入飞行状态，攻击计数器清零
        currentAttackCount = 0;

        // 删除了原来强制交替的 lastFlightWasSCurve 逻辑
        // 因为具体的攻击类型选择（直线还是S型）我们将统一放到 StartNextMove 中去判断

        // 进入飞行状态
        subPhase = 0;
        // ：进入飞行状态时确保特效是隐藏的
        knight.HideAllExParts();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate(); // 基类中自动累加 stateTimer

        // 在播放变形动画时，机头缓慢平滑地跟踪玩家
        if (subPhase < 10 || subPhase >= 20)
        {
            if (knight.playerTarget != null)
            {
                Vector3 dir = knight.playerTarget.position - knight.transform.position;
                Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dir);
                knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 5f * Time.deltaTime);
            }
        }

        switch (subPhase)
        {
            // --- 【入场变形动画】 ---
            case 0:
                float dur1 = knight.flightTransformDuration * 0.4f;
                knight.LeftBlade?.MoveToLocal(new Vector3(-0.8f, 0.5f, 0), new Vector3(0, 0, 45), dur1);
                knight.RightBlade?.MoveToLocal(new Vector3(0.8f, 0.5f, 0), new Vector3(0, 0, -45), dur1);
                subPhase = 1;
                break;
            case 1:
                if (knight.AreAllPartsStatic()) { subPhase = 2; stateTimer = 0; }
                break;
            case 2:
                if (stateTimer >= knight.flightTransformDuration * 0.2f) { subPhase = 3; }
                break;
            case 3:
                float dur2 = knight.flightTransformDuration * 0.4f;
                knight.LeftBlade?.MoveToLocal(new Vector3(-1.1f, 0.2f, 0), new Vector3(0, 0, 90), dur2);
                knight.RightBlade?.MoveToLocal(new Vector3(1.1f, 0.2f, 0), new Vector3(0, 0, -90), dur2);
                subPhase = 4;
                break;
            case 4:
                if (knight.AreAllPartsStatic())
                {
                    // ：飞行前置变形彻底完毕！如果处于狂暴，张开飞行光翼
                    if (knight.isEnraged)
                    {
                        knight.ShowExPart(knight.exFlight01, 0.4f);
                        knight.ShowExPart(knight.exFlight02, 0.4f);
                        knight.ShowExPart(knight.exFlight03, 0.4f);
                    }
                    StartNextMove();
                }
                break;

            // --- 【战斗核心逻辑】 ---
            case 11:
                HandleEvasiveSCurve();
                break;
            case 15:
                HandleAimingPause();
                break;
            case 12:
                HandleStraightDashLogic();
                break;
            case 13:
                HandleUTurnLogic();
                break;

            // --- 【退场变形动画】 ---
            case 20:
                knight.HideAllExParts();
                float dur3 = knight.flightTransformDuration * 0.4f;
                knight.LeftBlade?.MoveToLocal(new Vector3(-0.8f, 0.5f, 0), new Vector3(0, 0, 45), dur3);
                knight.RightBlade?.MoveToLocal(new Vector3(0.8f, 0.5f, 0), new Vector3(0, 0, -45), dur3);
                subPhase = 21;
                break;
            case 21:
                if (knight.AreAllPartsStatic()) { subPhase = 22; stateTimer = 0; }
                break;
            case 22:
                if (stateTimer >= knight.flightTransformDuration * 0.2f) { subPhase = 23; }
                break;
            case 23:
                float dur4 = knight.flightTransformDuration * 0.4f;
                knight.LeftBlade?.MoveToLocal(new Vector3(-0.35f, 1.3f, 0), Vector3.zero, dur4);
                knight.RightBlade?.MoveToLocal(new Vector3(0.35f, 1.3f, 0), Vector3.zero, dur4);
                subPhase = 24;
                break;
            case 24:
                if (knight.hasTriggeredFinalPhase && knight.endAttackState.currentStep <= 6)
                {
                    // 连招还在进行中，强制向指挥官状态报到，执行下一步
                    knight.SwitchState(knight.endAttackState);
                }
                else
                {
                    // 连招已经完全结束（步骤达到 7），恢复常规战斗循环
                    knight.SwitchState(knight.observeState);
                }
                break;
        }
    }

    private void StartNextMove()
    {
        stateTimer = 0;
        if (knight.playerTarget == null) return;

        // --- 【核心修改：攻击策略调度】 ---
        // 第 1 次攻击 (索引为 0)：无论什么情况，必须是直线盲冲，以此迅速拉开安全距离
        if (currentAttackCount == 0)
        {
            isSCurveAttack = false;
        }
        else
        {
            // 第 2、3 次攻击 (索引为 1、2)：此时经过U型弯，距离已经拉开，随机选择 S 型或直线
            isSCurveAttack = Random.value > 0.5f; // 50% 概率触发 S 型
        }

        if (isSCurveAttack)
        {
            subPhase = 11; // 启动S型规避
            sCurveTimer = 0f;
        }
        else
        {
            subPhase = 12; // 启动直线冲刺
            Vector3 dirToPlayer = (knight.playerTarget.position - knight.transform.position).normalized;
            targetPos = knight.playerTarget.position + dirToPlayer * knight.flightDashOvershoot;
        }
    }

    private void HandleEvasiveSCurve()
    {
        sCurveTimer += Time.deltaTime;
        if (knight.playerTarget == null) return;

        Vector3 dirToPlayer = (knight.playerTarget.position - knight.transform.position).normalized;
        if (dirToPlayer == Vector3.zero) dirToPlayer = -knight.transform.up;
        Vector3 rightDir = new Vector3(dirToPlayer.y, -dirToPlayer.x, 0).normalized;

        float waveOffset = Mathf.Sin(sCurveTimer * knight.flightSCurveFrequency) * knight.flightSCurveAmplitude;
        Vector3 forwardVelocity = dirToPlayer * knight.flightSCurveForwardSpeed;
        Vector3 lateralVelocity = rightDir * waveOffset;
        Vector3 currentVelocity = forwardVelocity + lateralVelocity;

        knight.transform.position += currentVelocity * Time.deltaTime;

        if (currentVelocity != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -currentVelocity.normalized);
            knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        float targetTime = (3f * Mathf.PI) / knight.flightSCurveFrequency;
        float distToPlayer = Vector3.Distance(knight.transform.position, knight.playerTarget.position);

        if (sCurveTimer >= targetTime || distToPlayer <= 4.0f)
        {
            subPhase = 15;
            stateTimer = 0;
        }
    }

    private void HandleAimingPause()
    {
        if (knight.playerTarget != null)
        {
            Vector3 dirToPlayer = knight.playerTarget.position - knight.transform.position;
            if (dirToPlayer != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dirToPlayer);
                knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 15f * Time.deltaTime);
            }
        }

        if (stateTimer >= 0.4f)
        {
            subPhase = 12;
            stateTimer = 0;

            Vector3 finalDir = -knight.transform.up;
            if (knight.playerTarget != null)
            {
                finalDir = (knight.playerTarget.position - knight.transform.position).normalized;
            }
            if (finalDir == Vector3.zero) finalDir = -knight.transform.up;

            targetPos = knight.transform.position + finalDir * knight.flightDashOvershoot;
        }
    }

    private void HandleStraightDashLogic()
    {
        knight.transform.position = Vector3.MoveTowards(knight.transform.position, targetPos, knight.flightStraightDashSpeed * Time.deltaTime);

        Vector3 dashDir = (targetPos - knight.transform.position).normalized;
        if (dashDir != Vector3.zero) knight.transform.up = -dashDir;

        if (Vector3.Distance(knight.transform.position, targetPos) < 0.1f || stateTimer > 1.5f)
        {
            TransitionToNextOrTurn();
        }
    }

    private void TransitionToNextOrTurn()
    {
        currentAttackCount++;
        if (currentAttackCount < knight.flightRepeatCount)
        {
            Vector3 toPlayer = knight.playerTarget.position - knight.transform.position;
            turnDirection = Vector3.Cross(-knight.transform.up, toPlayer).z >= 0 ? 1f : -1f;
            subPhase = 13;
            stateTimer = 0;
        }
        else
        {
            subPhase = 20;
        }
    }

    private void HandleUTurnLogic()
    {
        knight.transform.position += -knight.transform.up * knight.flightUTurnForwardSpeed * Time.deltaTime;
        knight.transform.Rotate(0, 0, turnDirection * knight.flightUTurnAngularSpeed * Time.deltaTime);

        if (stateTimer > 0.2f && knight.playerTarget != null)
        {
            float angle = Vector3.Angle(-knight.transform.up, (knight.playerTarget.position - knight.transform.position).normalized);
            if (angle < 15f || stateTimer > 3f)
            {
                StartNextMove();
            }
        }
    }
}
