using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GridCutState : BossBaseState
{
    private PlasmaBallBoss plasmaball;
    private int subPhase = 0;

    public float shieldPopSpeed = 60f;   // 护盾弹射到屏幕边缘的极速速度

    // 【全新移动逻辑变量】
    private Vector3 targetPos;           // 下一次冲刺的精确目标点
    private bool movingOnX = true;       // 当前是否在 X 轴上移动
    private float gridCutTimer = 0f;

    // 记录已经派发到屏幕边缘的护盾
    private List<int> deployedShields = new List<int>();
    private List<LaserBeam> activeLasers = new List<LaserBeam>();
    private List<Transform> emitters = new List<Transform>();

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        plasmaball = context as PlasmaBallBoss;
        subPhase = 0;
        stateTimer = 0;
        gridCutTimer = 0f;
        deployedShields.Clear();

        // 强行杀掉二阶段残留的大风车旋转，正骨复位
        plasmaball.transform.DOKill();
        plasmaball.transform.DORotate(Vector3.zero, plasmaball.shieldReturnTime).SetEase(Ease.InOutQuad);

        // 强制收回所有护盾
        plasmaball.ReturnAllShields();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 【核心神级逻辑：二维扫描仪边缘滑轨更新】
        // 每一帧都会执行，强制让护盾吸附在屏幕边缘，并与 Boss 形成十字扫描线
        UpdateScannerShieldsAndLasers();

        switch (subPhase)
        {
            case 0: // 等待复位与收回
                if (stateTimer >= plasmaball.shieldReturnTime)
                {
                    Vector3 dirToPlayer = plasmaball.playerTarget.position - plasmaball.transform.position;
                    List<int> firstPopShields = new List<int>();

                    // 决定起手轴向
                    movingOnX = Mathf.Abs(dirToPlayer.x) > Mathf.Abs(dirToPlayer.y);
                    targetPos = plasmaball.transform.position;

                    if (movingOnX)
                    {
                        firstPopShields.Add(0); // 上
                        firstPopShields.Add(1); // 下
                        // 第一次起手，强行横向贯穿全场大冲刺！
                        targetPos.x = (dirToPlayer.x > 0) ? plasmaball.arenaBounds.x : -plasmaball.arenaBounds.x;
                    }
                    else
                    {
                        firstPopShields.Add(2); // 左
                        firstPopShields.Add(3); // 右
                        // 第一次起手，强行纵向贯穿全场大冲刺！
                        targetPos.y = (dirToPlayer.y > 0) ? plasmaball.arenaBounds.y : -plasmaball.arenaBounds.y;
                    }

                    DeployShieldsToEdges(firstPopShields);
                    subPhase = 1;
                    stateTimer = 0;
                }
                break;

            case 1: // 等待第一组护盾飞到边缘，然后带出激光逼近玩家
                if (stateTimer >= 0.5f)
                {
                    // 【核心修改】：使用 MoveTowards 精准冲刺到目标点
                    plasmaball.transform.position = Vector3.MoveTowards(plasmaball.transform.position, targetPos, plasmaball.sweepSpeed * Time.deltaTime);

                    if (Vector3.Distance(plasmaball.transform.position, targetPos) < 0.1f)
                    {
                        subPhase = 2;
                        stateTimer = 0;
                    }
                }
                break;

            case 2: // 展开剩余十字扫描线
                List<int> remainingShields = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    if (!deployedShields.Contains(i)) remainingShields.Add(i);
                }
                DeployShieldsToEdges(remainingShields);

                PickNextGridTarget(); // 计算下一个网格大跨度目标

                subPhase = 3;
                stateTimer = 0;
                break;

            case 3: // 完整的十字扫描仪在场内疯狂大范围切割！
                if (stateTimer >= 0.5f)
                {
                    gridCutTimer += Time.deltaTime;

                    // 高速冲向计算好的目标点
                    plasmaball.transform.position = Vector3.MoveTowards(plasmaball.transform.position, targetPos, plasmaball.sweepSpeed * Time.deltaTime);

                    // 到达目标点后，瞬间切换轴向，计算下一个大范围目标点
                    if (Vector3.Distance(plasmaball.transform.position, targetPos) < 0.1f)
                    {
                        PickNextGridTarget();
                    }

                    if (gridCutTimer >= plasmaball.gridCutDuration)
                    {
                        subPhase = 4;
                    }
                }
                break;

            case 4: // 结束扫描切割，收回护盾切入旋转大风车
                DestroyLasers();
                plasmaball.SwitchState(plasmaball.rotationLaserState);
                break;
        }
    }

    /// <summary>
    /// 【二维扫描仪核心算法】
    /// 无论 Boss 怎么动，强行覆盖护盾的世界坐标，使其在屏幕边缘纯平移滑动
    /// </summary>
    private void UpdateScannerShieldsAndLasers()
    {
        for (int i = 0; i < deployedShields.Count; i++)
        {
            int idx = deployedShields[i];
            Transform s = plasmaball.shieldsTransforms[idx];
            if (s == null) continue;

            // 1. 计算护盾绝对滑轨目标位置
            Vector3 targetPos = s.position;
            if (idx == 0) targetPos = new Vector3(plasmaball.transform.position.x, plasmaball.arenaBounds.y, 0);       // 上：Y锁死上边缘，X永远与Boss对齐
            else if (idx == 1) targetPos = new Vector3(plasmaball.transform.position.x, -plasmaball.arenaBounds.y, 0); // 下：Y锁死下边缘，X永远与Boss对齐
            else if (idx == 2) targetPos = new Vector3(-plasmaball.arenaBounds.x, plasmaball.transform.position.y, 0); // 左：X锁死左边缘，Y永远与Boss对齐
            else if (idx == 3) targetPos = new Vector3(plasmaball.arenaBounds.x, plasmaball.transform.position.y, 0);  // 右：X锁死右边缘，Y永远与Boss对齐

            // 2. 护盾瞬间平滑移动过去（初始弹出时有极快的飞行过程，到位后就是死死咬合）
            s.position = Vector3.MoveTowards(s.position, targetPos, shieldPopSpeed * Time.deltaTime);

            // 3. 动态伸缩激光：距离实时计算
            if (i < activeLasers.Count && activeLasers[i] != null)
            {
                float dist = Vector3.Distance(plasmaball.transform.position, s.position);
                activeLasers[i].maxDistance = dist;
            }
        }
    }

    /// <summary>
    /// 将护盾解绑并派发到屏幕边缘，同时生成发射基座
    /// </summary>
    private void DeployShieldsToEdges(List<int> shieldIndices)
    {
        if (plasmaball.laserPrefab == null) return;

        foreach (int i in shieldIndices)
        {
            if (plasmaball.shieldsTransforms[i] == null) continue;

            // 【关键】：解开父子层级关系，使得护盾彻底独立于 Boss 本体的运动
            plasmaball.shieldsTransforms[i].SetParent(null);
            deployedShields.Add(i);

            // 生成追踪发射基座
            GameObject emitterObj = new GameObject($"GridLaserEmitter_{i}");
            emitterObj.transform.SetParent(plasmaball.transform);
            emitterObj.transform.localPosition = Vector3.zero;

            // 强制绝对朝向，绝不跟随残留的旋转
            Vector3 absoluteDir = Vector3.up;
            if (i == 1) absoluteDir = Vector3.down;
            if (i == 2) absoluteDir = Vector3.left;
            if (i == 3) absoluteDir = Vector3.right;
            emitterObj.transform.up = absoluteDir;

            emitters.Add(emitterObj.transform);

            // 生成激光预制件
            LaserBeam laser = Object.Instantiate(plasmaball.laserPrefab, emitterObj.transform.position, Quaternion.identity);
            laser.warningTime = 0.5f;
            laser.activeTime = plasmaball.gridCutDuration + 10f;
            laser.FireTracking(emitterObj.transform, 0f, true);

            activeLasers.Add(laser);
        }
    }
    /*
    private bool CheckHitBoundsAndClamp()
    {
        bool hitX = Mathf.Abs(plasmaball.transform.position.x) >= plasmaball.arenaBounds.x;
        bool hitY = Mathf.Abs(plasmaball.transform.position.y) >= plasmaball.arenaBounds.y;

        if (hitX || hitY)
        {
            Vector3 clampedPos = plasmaball.transform.position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, -plasmaball.arenaBounds.x, plasmaball.arenaBounds.x);
            clampedPos.y = Mathf.Clamp(clampedPos.y, -plasmaball.arenaBounds.y, plasmaball.arenaBounds.y);
            plasmaball.transform.position = clampedPos;
            return true;
        }
        return false;
    }*/

   /// <summary>
    /// 【全新核心大脑】：计算下一次的正交冲刺目标，彻底杜绝“小碎步”
    /// </summary>
    private void PickNextGridTarget()
    {
        movingOnX = !movingOnX; // 强行 90 度转角，保证十字正交
        targetPos = plasmaball.transform.position;

        if (movingOnX)
        {
            float distToPlayerX = Mathf.Abs(plasmaball.playerTarget.position.x - targetPos.x);

            // 【防碎步机制】：如果玩家离得很远（大于4），有 70% 概率精准对齐玩家 X 轴
            if (distToPlayerX > 4f && Random.value > 0.3f)
            {
                targetPos.x = plasmaball.playerTarget.position.x;
            }
            else
            {
                // 否则，直接往更远的那一端发起“贯穿全屏”的切割大冲刺！
                targetPos.x = (targetPos.x > 0) ? -plasmaball.arenaBounds.x : plasmaball.arenaBounds.x;
            }
        }
        else
        {
            float distToPlayerY = Mathf.Abs(plasmaball.playerTarget.position.y - targetPos.y);

            // 同理，计算 Y 轴的大跨度移动
            if (distToPlayerY > 4f && Random.value > 0.3f)
            {
                targetPos.y = plasmaball.playerTarget.position.y;
            }
            else
            {
                targetPos.y = (targetPos.y > 0) ? -plasmaball.arenaBounds.y : plasmaball.arenaBounds.y;
            }
        }

        // 终极安全锁：如果算出来的目标点离自己还是太近（例如刚好被逼在角落），强制反弹全屏！
        if (Vector3.Distance(plasmaball.transform.position, targetPos) < 2f)
        {
            if (movingOnX) targetPos.x = (targetPos.x > 0) ? -plasmaball.arenaBounds.x : plasmaball.arenaBounds.x;
            else targetPos.y = (targetPos.y > 0) ? -plasmaball.arenaBounds.y : plasmaball.arenaBounds.y;
        }
    }

    private void DestroyLasers()
    {
        foreach (var laser in activeLasers) if (laser != null) Object.Destroy(laser.gameObject);
        activeLasers.Clear();
        foreach (var em in emitters) if (em != null) Object.Destroy(em.gameObject);
        emitters.Clear();
    }

    public override void Exit()
    {
        base.Exit();
        DestroyLasers();

        // 离开状态时，主脚本的 ReturnAllShields 会自动将被 SetParent(null) 的护盾全部抓回来并重新连接到容器上
    }
}
