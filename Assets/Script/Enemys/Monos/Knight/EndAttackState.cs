using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndAttackState : BossBaseState
{
    private KnightBoss knight;
    public int currentStep = 0;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;
        
        ExecuteNextStep();
    }

    public void ExecuteNextStep()
    {
        currentStep++;

        switch (currentStep)
        {
            case 1: // 1. 激光斩击
                Debug.Log("这是最终阶段的第一次激光斩击");
                knight.laserSlashState.nextStateAfterSlash = this;
                knight.SwitchState(knight.laserSlashState);
                break;

            case 2: // 2. 飞行形态直线冲刺
                // 临时修改次数为 1，确保冲刺一次后就回来
                knight.flightRepeatCount = 1;
                // 修改飞行状态的退出指向 (需在 FlightState 加入逻辑支持，详见下文)
                knight.SwitchState(knight.flightState);
                break;

            case 3: // 3. 再次激光斩击
                knight.laserSlashState.nextStateAfterSlash = this;
                knight.SwitchState(knight.laserSlashState);
                break;

            case 4: // 4. 炮击形态
                knight.SwitchState(knight.artilleryState);
                break;

            case 5: // 5. 再次飞行形态
                knight.flightRepeatCount = 1;
                knight.SwitchState(knight.flightState);
                break;

            case 6: // 6. 最终：特殊激光斩击
                knight.SwitchState(knight.meleeLaserSlashState);
                break;

            default:
                // 【核心修改】：终极连招彻底打完，能量耗尽
                if (!knight.isExhausted)
                {
                    // 触发参数削弱与视觉变化
                    knight.TriggerExhaustedState();
                }

                // 带着虚弱的身体，回到观察状态
                knight.SwitchState(knight.observeState);
                break;
        }
    }

    // 注意：在 KnightFlightState 和 ArtilleryState 结束时，
    // 需要检查当前是否处于 EndAttack 序列，如果是，则调用此方法。
}
