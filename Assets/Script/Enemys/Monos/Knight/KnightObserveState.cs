using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class KnightObserveState : BossBaseState, IAttackReactable
{
    private KnightBoss knight;

    [Header("观察期环绕参数")]
    public float observeDuration = 3f;
    public float orbitSpeed = 60f;        // 环绕的角速度 (度/秒)
    public float targetOrbitRadius = 6f;  // 理想的压迫距离 (建议6~8)
    public float radiusAdjustSpeed = 3f;  // 平滑靠近的速度系数

    // 防御状态控制
    private bool isSpinning = false;
    private float currentSpinAngle = 0f;

    // ：防御次数控制 (每次进入状态只有 1 次)
    private int defenseCharges = 0;
    // ：用于标记特效是否已经展开过
    private bool exShown = false;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;

        isSpinning = false;
        currentSpinAngle = 0f;

        // 每次进入观察状态，重置防御次数为 1
        defenseCharges = 1;

        exShown = false;

        // ：刚进入状态时，先隐藏所有特效
        knight.HideAllExParts();

        // 进入观察状态，确保刀刃在标准的悬浮位置
        if (knight.AreAllPartsStatic())
        {
            knight.LeftBlade?.MoveToLocal(new Vector3(-0.3f, 1.2f, 0), Vector3.zero, 0.5f);
            knight.RightBlade?.MoveToLocal(new Vector3(0.3f, 1.2f, 0), Vector3.zero, 0.5f);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // ：检测刀刃是否就位。一旦就位且处于狂暴，就展开 EXmelee 特效！
        if (!exShown && knight.AreAllPartsStatic())
        {
            exShown = true;
            if (knight.isEnraged) // 假设你的狂暴判断变量叫 isEnraged
            {
                // 用 0.4 秒的时间，让能量刃从中间向两边张开
                knight.ShowExPart(knight.exMelee, 0.4f);
            }
        }
        if (knight.playerTarget != null)
        {
            // 1. 螺旋环绕位移逻辑 (拒绝闪现，平滑逼近玩家)
            UpdateSpiralMovement();

            // 2. 姿态控制：普通盯防 vs 预判格挡旋转
            if (isSpinning)
            {
                HandleSingleSpin();
            }
            else
            {
                //【核心修复】：将瞬间转身改为平滑转身(Slerp)
                Vector3 dirToPlayer = knight.playerTarget.position - knight.transform.position;
                if (dirToPlayer != Vector3.zero)
                {
                    // 计算出目标旋转角度 (因为机头是 -up 方向，所以这里传 -dirToPlayer)
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, -dirToPlayer);

                    // 使用 Slerp 平滑插值。8f 是转身速度，可以根据你想要的手感微调 (建议 5f ~ 12f 之间)
                    knight.transform.rotation = Quaternion.Slerp(knight.transform.rotation, targetRot, 8f * Time.deltaTime);
                }
            }
        }

        // 观察时间结束，准备发起下一次攻击 (仅在非防御姿态下才能切换状态)
        if (stateTimer >= observeDuration && !isSpinning)
        {
            ChooseNextAttack();
        }
    }

    // ==========================================
    // 防御与旋转逻辑
    // ==========================================

    // 处理单次 360 度旋转 (完美复刻 MeleeState 的大风车)
    private void HandleSingleSpin()
    {
        // 读取你在 Inspector 里配好的近战旋转速度
        float rotateSpeed = 360f / knight.meleeSpinDuration;
        float angleStep = rotateSpeed * Time.deltaTime;

        // 本体带着处于悬浮位的刀刃一起高速自转，切割子弹
        knight.transform.Rotate(0, 0, angleStep);
        currentSpinAngle += angleStep;

        // 如果转满了一圈（360度），停止旋转，恢复普通盯防
        if (currentSpinAngle >= 360f)
        {
            isSpinning = false;
            currentSpinAngle = 0f;
        }
    }

    // 由挂载在感应区上的 DefenseSensor 脚本调用的外部接口
    public void TriggerSingleSpinDefense()
    {
        // 【核心修复】：增加次数检查
        if (!isSpinning && defenseCharges > 0)
        {
            defenseCharges--; // 消耗掉唯一的防御次数
            isSpinning = true;
            currentSpinAngle = 0f;
            Debug.Log("Knight: 触发格挡旋转！剩余次数: " + defenseCharges);
        }
    }

    // 保留受击接口防止报错（既然已经使用前置感应区预判，这里可以留空，或者作为兜底）
    public void OnBossAttacked()
    {
        // 受击时也尝试触发，同样受 defenseCharges 限制
        TriggerSingleSpinDefense();
    }

    // ==========================================
    // 运动与状态切换逻辑
    // ==========================================

    private void UpdateSpiralMovement()
    {
        Vector3 offset = knight.transform.position - knight.playerTarget.position;
        float currentRadius = offset.magnitude;

        // 防止重合导致的除零错误
        if (currentRadius < 0.01f) { offset = Vector3.up * 0.01f; currentRadius = 0.01f; }

        float currentAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        float newAngle = currentAngle + orbitSpeed * Time.deltaTime;
        float newRadius = Mathf.Lerp(currentRadius, targetOrbitRadius, radiusAdjustSpeed * Time.deltaTime);

        float rad = newAngle * Mathf.Deg2Rad;
        Vector3 newOffset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * newRadius;

        // 实时更新位置
        knight.transform.position = knight.playerTarget.position + newOffset;
    }

    private void ChooseNextAttack()
    {
        
        // 一阶段或者虚弱状态
        if (!knight.IsPhase2Unlocked || knight.isExhausted)
        {
            
            knight.SwitchState(knight.meleeState);
            return;
        }

        // --- 进入二阶段后的常规随机逻辑 (不再包含 LaserSlash) ---
        BossBaseState nextState;

        // 防重复近战逻辑
        if (knight.lastAttackState == knight.meleeState)
        {
            float total = knight.flightWeight + knight.artilleryWeight;
            nextState = (Random.Range(0, total) < knight.flightWeight) ? knight.flightState : knight.artilleryState;
        }
        else
        {
            float total = knight.meleeWeight + knight.flightWeight + knight.artilleryWeight;
            float rand = Random.Range(0, total);

            if (rand < knight.meleeWeight) nextState = knight.meleeState;
            else if (rand < knight.meleeWeight + knight.flightWeight) nextState = knight.flightState;
            else nextState = knight.artilleryState;
        }

        knight.lastAttackState = nextState;
        knight.SwitchState(nextState);
    }
}
