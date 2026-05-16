using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LaserSlashState : BossBaseState
{
    private KnightBoss knight;

    // 0: 刀刃平移到位, 1: 激光爆发并保持观察移动, 2: 高速旋转斩击, 3: 回收刀刃
    private int subPhase = 0;
    private float currentSpinAngle = 0f;
    private LaserBeam leftLaser;
    private LaserBeam rightLaser;
    public BossBaseState nextStateAfterSlash;

    // 蓄力停留时间
    private const float LaserStayDuration = 2.0f;

    [Header("移动参数 (同步自观察状态)")]
    private float orbitSpeed = 60f;
    private float targetOrbitRadius = 6f;
    private float radiusAdjustSpeed = 3f;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        knight = context as KnightBoss;
        subPhase = 0;
        currentSpinAngle = 0f;
        knight.HideAllExParts();

        // 步骤 1：刀刃向两侧平移打开
        float dur = 0.5f;
        knight.LeftBlade?.MoveToLocal(new Vector3(-0.6f, 1.2f, 0), Vector3.zero, dur);
        knight.RightBlade?.MoveToLocal(new Vector3(0.6f, 1.2f, 0), Vector3.zero, dur);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (subPhase)
        {
            case 0: // 等待刀刃平移到位
                if (knight.AreAllPartsStatic())
                {
                    subPhase = 1;
                    stateTimer = 0;
                    FireFullLasers();
                }
                break;

            case 1: // 【阶段 1：激光静止锁定 + 观察移动】
                // 在蓄力期间，保持像观察状态一样的螺旋移动
                if (knight.playerTarget != null)
                {
                    UpdateSpiralMovement();

                    // 姿态控制：机头死死盯住玩家
                    // 此时激光是“反向”射出的，所以激光会指向玩家的相反方向
                    Vector3 dirToPlayer = (knight.playerTarget.position - knight.transform.position).normalized;
                    knight.transform.up = -dirToPlayer;
                }

                // 1.5 秒蓄力时间到
                if (stateTimer >= LaserStayDuration)
                {
                    subPhase = 2;
                    stateTimer = 0;
                }
                break;

            case 2: // 【阶段 2：维持激光并开始高速旋转】
                HandleSpinAttack();
                break;

            case 3: // 旋转结束，回收并切换形态
                if (knight.AreAllPartsStatic())
                {
                    knight.SwitchState(nextStateAfterSlash);
                }
                break;
        }
    }

    // --- 核心移动逻辑：复刻自观察状态 ---
    private void UpdateSpiralMovement()
    {
        Vector3 offset = knight.transform.position - knight.playerTarget.position;
        float currentRadius = offset.magnitude;

        if (currentRadius < 0.01f) { offset = Vector3.up * 0.01f; currentRadius = 0.01f; }

        float currentAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        float newAngle = currentAngle + orbitSpeed * Time.deltaTime;
        float newRadius = Mathf.Lerp(currentRadius, targetOrbitRadius, radiusAdjustSpeed * Time.deltaTime);

        float rad = newAngle * Mathf.Deg2Rad;
        Vector3 newOffset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * newRadius;

        knight.transform.position = knight.playerTarget.position + newOffset;
    }

    private void FireFullLasers()
    {
        if (knight.laserPrefab == null) return;

        leftLaser = Object.Instantiate(knight.laserPrefab, knight.LeftBlade.transform.position, Quaternion.identity);
        rightLaser = Object.Instantiate(knight.laserPrefab, knight.RightBlade.transform.position, Quaternion.identity);

        // 瞬间生成粗激光
        leftLaser.warningTime = 0.05f;
        rightLaser.warningTime = 0.05f;

        // 确保激光寿命覆盖：1.5s 移动 + 约0.3s 旋转
        leftLaser.activeTime = 3.0f;
        rightLaser.activeTime = 3.0f;

        leftLaser.gameObject.SetActive(true);
        rightLaser.gameObject.SetActive(true);

        // 使用反向参数，激光从刀尖向后（三角形底边）射出
        leftLaser.FireTracking(knight.LeftBlade.transform, 0f, true);
        rightLaser.FireTracking(knight.RightBlade.transform, 0f, true);
    }

    private void HandleSpinAttack()
    {
        float rotateSpeed = 1440f;
        float angleStep = rotateSpeed * Time.deltaTime;

        knight.transform.Rotate(0, 0, angleStep);
        currentSpinAngle += angleStep;

        if (currentSpinAngle >= 360f)
        {
            subPhase = 3;

            if (leftLaser != null) Object.Destroy(leftLaser.gameObject);
            if (rightLaser != null) Object.Destroy(rightLaser.gameObject);

            knight.LeftBlade?.ResetToInitial(0.2f);
            knight.RightBlade?.ResetToInitial(0.2f);
        }
    }
}
