using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldPunchState : BossBaseState
{
    private PlasmaBallBoss plasmaball;

    // 0: 智能移动对准, 1: 弹射护甲, 2: 护甲停留, 3: 护甲收回
    private int subPhase = 0;

    

    // 记录本次攻击丢出去了哪几个护甲
    private List<int> poppedShields = new List<int>();

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        plasmaball = context as PlasmaBallBoss;
        subPhase = 0;
        stateTimer = 0;
        poppedShields.Clear();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (plasmaball.playerTarget == null) return;

        switch (subPhase)
        {
            case 0: // 【智能追踪与十字路径对准】
                Vector3 dirToPlayer = plasmaball.playerTarget.position - plasmaball.transform.position;

                // 检测玩家是否处于 Boss 的水平(X)或垂直(Y)攻击走廊内
                bool alignedY = Mathf.Abs(dirToPlayer.x) < plasmaball.pathWidth; // 玩家在正上方或正下方
                bool alignedX = Mathf.Abs(dirToPlayer.y) < plasmaball.pathWidth; // 玩家在正左方或正右方

                int mainShieldIndex = -1;
                Vector3 targetDir = Vector3.zero;

                // 确定我们需要开火的世界方向
                if (alignedY) targetDir = new Vector3(0, Mathf.Sign(dirToPlayer.y), 0);
                else if (alignedX) targetDir = new Vector3(Mathf.Sign(dirToPlayer.x), 0, 0);

                // 【核心修复1：使用点乘寻找物理上真正朝向玩家的护盾，无视任何旋转和顺序错误】
                if (targetDir != Vector3.zero)
                {
                    float maxDot = -2f;
                    for (int i = 0; i < 4; i++)
                    {
                        if (plasmaball.shieldsTransforms[i] != null)
                        {
                            Vector3 shieldWorldDir = (plasmaball.shieldsTransforms[i].position - plasmaball.transform.position).normalized;
                            float dot = Vector3.Dot(shieldWorldDir, targetDir);
                            if (dot > maxDot)
                            {
                                maxDot = dot;
                                mainShieldIndex = i;
                            }
                        }
                    }
                    if (maxDot < 0.5f) mainShieldIndex = -1; // 防错：如果找出的护甲根本没朝向玩家，放弃锁定
                }

                // --- 移动与攻击逻辑分流 ---
                if (mainShieldIndex != -1 && stateTimer >= plasmaball.attackCooldown)
                {
                    // 1. 如果完美对准了，且攻击冷却完毕，开火！
                    poppedShields.Clear();
                    poppedShields.Add(mainShieldIndex); // 必定发射对准玩家的护甲

                    // 随机附带发射 0~2 块其他护甲
                    int popCount = Random.Range(1, 4);
                    List<int> available = new List<int> { 0, 1, 2, 3 };
                    available.Remove(mainShieldIndex);

                    while (poppedShields.Count < popCount && available.Count > 0)
                    {
                        int randIdx = Random.Range(0, available.Count);
                        poppedShields.Add(available[randIdx]);
                        available.RemoveAt(randIdx);
                    }

                    subPhase = 1;
                    stateTimer = 0;
                }
                else
                {
                    // 2. 还没有对准/还在冷却，执行智能移动
                    Vector3 targetOffset = Vector3.zero;

                    if (mainShieldIndex == -1)
                    {
                        // 未对准：寻找最短的距离去对准十字轴线
                        if (Mathf.Abs(dirToPlayer.y) < Mathf.Abs(dirToPlayer.x))
                            targetOffset = new Vector3(0, dirToPlayer.y, 0);
                        else
                            targetOffset = new Vector3(dirToPlayer.x, 0, 0);
                    }
                    else
                    {
                        // 已对准但在冷却中：顺着准星轴线压迫靠近玩家
                        if (alignedY) targetOffset = new Vector3(0, dirToPlayer.y, 0);
                        else if (alignedX) targetOffset = new Vector3(dirToPlayer.x, 0, 0);
                    }

                    // 【核心修复2：利用 MoveTowards 防止抖动和超调抽搐】
                    if (targetOffset != Vector3.zero)
                    {
                        Vector3 targetPos = plasmaball.transform.position + targetOffset;
                        plasmaball.transform.position = Vector3.MoveTowards(plasmaball.transform.position, targetPos, plasmaball.moveSpeed * Time.deltaTime);
                    }
                }
                break;

            case 1: // 【严格按绝对世界轴线向外弹射】
                foreach (int index in poppedShields)
                {
                    if (plasmaball.shieldsTransforms[index] == null) continue;

                    // 【核心修复3：使用物理世界坐标计算完美直线，杜绝斜眼偏移】
                    Vector3 shieldWorldPos = plasmaball.shieldsTransforms[index].position;
                    Vector3 fireDir = (shieldWorldPos - plasmaball.transform.position).normalized;

                    // 目标点锁定在此向外延展的直线上
                    Vector3 targetPos = plasmaball.transform.position + fireDir * plasmaball.popDistance;

                    plasmaball.PopShieldToWorldPos(index, targetPos);
                }

                subPhase = 2;
                stateTimer = 0;
                break;

            case 2: // 【护甲停留等待】
                if (stateTimer >= 1.0f + plasmaball.stayTime)
                {
                    subPhase = 3;
                    stateTimer = 0;

                    foreach (int index in poppedShields)
                    {
                        plasmaball.ReturnShield(index);
                    }
                }
                break;

            case 3: // 【护甲收回本体】
                if (stateTimer >= plasmaball.shieldReturnTime)
                {
                    poppedShields.Clear();
                    subPhase = 0;
                    stateTimer = 0;
                }
                break;
        }
    }
}
