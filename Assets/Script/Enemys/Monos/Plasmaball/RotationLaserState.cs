using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening; // 必须引入 DOTween 来实现机体修正平滑旋转

public class RotationLaserState : BossBaseState
{
    private PlasmaBallBoss plasmaball;
    private int subPhase = 0;

    

    private float currentSpinSpeed = 0f;
    private int spinDirection = 1;        // 1: 顺时针, -1: 逆时针

    // 存储你的激光预制件实例和发射基座
    private List<LaserBeam> activeLasers = new List<LaserBeam>();
    private List<Transform> emitters = new List<Transform>();

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        plasmaball = context as PlasmaBallBoss;
        subPhase = 0;
        stateTimer = 0;
        currentSpinSpeed = 0f;

        // 强行清理一阶段可能残留的动画，确保护甲在身上
        plasmaball.ReturnAllShields();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (subPhase)
        {
            case 0: // 【回归中心】
                Vector3 centerPos = Vector3.zero; // 场地中心
                plasmaball.transform.position = Vector3.MoveTowards(plasmaball.transform.position, centerPos, plasmaball.centerMoveSpeed * Time.deltaTime);

                if (Vector3.Distance(plasmaball.transform.position, centerPos) < 0.1f)
                {
                    subPhase = 1;
                    stateTimer = 0;
                }
                break;

            case 1: // 【弹出护甲并生成激光预警】
                for (int i = 0; i < 4; i++)
                {
                    plasmaball.PushShieldOutward(i, plasmaball.pushOutDistance, plasmaball.pushOutDuration);
                }

                // 此时实例化你的激光预制件！
                CreateLasers();

                subPhase = 2;
                stateTimer = 0;
                break;

            case 2: // 【渐进加速旋转】
                // 等待 pushOutDuration 时间（此时红线预警刚结束，激光正式喷发！）
                if (stateTimer >= plasmaball.pushOutDuration)
                {
                    currentSpinSpeed += plasmaball.spinAcceleration * Time.deltaTime;
                    if (currentSpinSpeed >= plasmaball.maxSpinSpeed)
                    {
                        currentSpinSpeed = plasmaball.maxSpinSpeed;
                        subPhase = 3;
                        stateTimer = 0;
                    }
                    plasmaball.transform.Rotate(0, 0, spinDirection * currentSpinSpeed * Time.deltaTime);
                }
                break;

            case 3: // 【最高速狂暴大风车】
                plasmaball.transform.Rotate(0, 0, spinDirection * plasmaball.maxSpinSpeed * Time.deltaTime);
                if (stateTimer >= plasmaball.maxSpinDuration)
                {
                    subPhase = 4;
                    stateTimer = 0;

                    DestroyLasers(); // 引擎过载，瞬间销毁所有激光，锁链掐断！

                    // 【核心细节：机体复位】：
                    // Boss 因为旋转现在角度肯定是乱的。趁着过载瘫痪的时间，让它平滑地转回 0 度！
                    plasmaball.transform.DORotate(Vector3.zero, plasmaball.overloadWaitTime).SetEase(Ease.InOutQuad);
                }
                break;

            case 4: // 【过载瘫痪期】
                if (stateTimer >= plasmaball.overloadWaitTime)
                {
                    plasmaball.ReturnAllShields(); // 瘫痪结束，无力地收回护甲
                    subPhase = 5;
                    stateTimer = 0;
                }
                break;

            case 5: // 【护甲收回，重置循环】
                if (stateTimer >= plasmaball.shieldReturnTime)
                {
                    spinDirection *= -1;
                    currentSpinSpeed = 0f;

                    // ==============================================
                    // 【核心连招机制】：如果已经解锁了三阶段，旋转结束后不休息，直接开始新一轮的切割！
                    if (plasmaball.isPhase3Unlocked)
                    {
                        plasmaball.SwitchState(plasmaball.gridCutState);
                        return; // 直接 return，跳出当前循环
                    }
                    // ==============================================

                    subPhase = 0;
                    stateTimer = 0;
                }
                break;
        }
    }

    private void CreateLasers()
    {
        DestroyLasers();

        if (plasmaball.laserPrefab == null)
        {
            Debug.LogError("PlasmaBallBoss 未分配 laserPrefab！请在面板中拖入预制件。");
            return;
        }
        float autoLaserDistance = 50f; // 默认给个安全值兜底
        if (Camera.main != null)
        {
            // orthographicSize 代表屏幕高度的一半
            float camHeight = Camera.main.orthographicSize;
            // 通过宽高比算出屏幕宽度的一半
            float camWidth = camHeight * Camera.main.aspect;
            // 勾股定理：最长距离(对角线) = √(宽? + 高?)，再额外加 5 个单位确保彻底飞出屏幕不露馅
            autoLaserDistance = Mathf.Sqrt(camHeight * camHeight + camWidth * camWidth) + 5f;
        }

        for (int i = 0; i < 4; i++)
        {
            if (plasmaball.shieldsTransforms[i] == null) continue;

            // 1. 创建空物体作为发射基座，挂载在 boss 根节点上跟着旋转
            GameObject emitterObj = new GameObject($"LaserEmitter_{i}");
            emitterObj.transform.SetParent(plasmaball.transform);
            emitterObj.transform.localPosition = Vector3.zero; // 放置在 Boss 中心

            // 2. 将发射基座瞄准护甲飞出的固定方向
            Vector3 fireDir = plasmaball.shieldInitialLocalPos[i].normalized;
            emitterObj.transform.up = fireDir;
            emitters.Add(emitterObj.transform);

            // 3. 生成你的激光预制件
            LaserBeam laser = Object.Instantiate(plasmaball.laserPrefab, emitterObj.transform.position, Quaternion.identity);

            // ========================================================
            // 【核心修改】：全自动覆盖参数，让激光自动“听命于”二阶段设置
            // ========================================================

            // A. 预警时间：严格等于护甲向外展开的时间 (推到位的瞬间开火)
            laser.warningTime = plasmaball.pushOutDuration;

            // B. 存活时间：预警时间 + 最高速旋转时间 + 冗余时间。
            // (其实状态机会在进入 case 3 时调用 DestroyLasers() 强行销毁它们，这里给个足够大的值防 Bug 即可)
            laser.activeTime = plasmaball.pushOutDuration + plasmaball.maxSpinDuration + 5f;

            // C. 伤害同步：让激光的伤害等于 Boss 本身的接触伤害
            // (注意：假设你的 LaserBeam 脚本里控制伤害的变量叫 damage，如果叫其他名字请自行修改)
            // laser.damage = plasmaball.contactDamage; 

            // D. 尺寸适配：如果你希望 Boss 变大时激光也跟着变粗，可以同步缩放
            // laser.transform.localScale = plasmaball.transform.localScale;
            laser.maxDistance = autoLaserDistance;
            // ========================================================

            // 4. 绑定到发射基座并追踪开火
            laser.FireTracking(emitterObj.transform, 0f, true);

            activeLasers.Add(laser);
        }
    }

        private void DestroyLasers()
    {
        // 彻底销毁所有激光实例
        foreach (var laser in activeLasers)
        {
            if (laser != null) Object.Destroy(laser.gameObject);
        }
        activeLasers.Clear();

        // 销毁所有辅助发射基座
        foreach (var em in emitters)
        {
            if (em != null) Object.Destroy(em.gameObject);
        }
        emitters.Clear();
    }

    public override void Exit()
    {
        base.Exit();
        DestroyLasers(); // 防止你在二阶段中途强行切到三阶段时导致激光残留满屏幕
    }
}