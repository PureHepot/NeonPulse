using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class PlasmaBallBoss : BossBase
{
    public Transform playerTarget;

    [Header("部位引用 (Parts References)")]

    public Transform shieldsContainer; // 护甲的旋转父节点
    public Transform[] shieldsTransforms = new Transform[4]; // 0:上, 1:下, 2:左, 3:右

    [Header("战斗参数")]
    public float shieldPopSpeed = 15f;    // 护甲弹射速度
    public float shieldReturnTime = 0.5f; // 护甲收回时间
    [Header("武器预制件")]
    public LaserBeam laserPrefab;

    [Header("一阶段攻击参数")]
    public float attackCooldown = 2.5f; // 两次攻击的间隔
    public float stayTime = 1.0f;       // 护甲砸中玩家后停留的时间
    public float moveSpeed = 3.5f;       // Boss 寻找准星时的移动速度
    [Tooltip("判定范围")]
    public float pathWidth = 1.5f;       // 十字准星的宽度（玩家走进这个宽度就会触发开火）
    [Tooltip("护盾飞出距离")]
    public float popDistance = 10f;   // 护甲弹射的保底飞行距离

    [Header("二阶段参数")]
    public float centerMoveSpeed = 8f;
    public float pushOutDistance = 5f;    // 锁链展开长度
    public float pushOutDuration = 1.0f;  // 展开耗时
    public float maxSpinSpeed = 250f;     // 最高旋转速度 (度/秒)
    public float spinAcceleration = 80f;  // 旋转加速度
    public float maxSpinDuration = 6.0f;  // 最高速狂暴持续时间
    [Tooltip("过载时间")]
    public float overloadWaitTime = 2.0f; // 过载瘫痪发呆时间

    [Header("三阶段参数")]
    public float sweepSpeed = 12f;       // 扫荡移动速度
    public float gridCutDuration = 8.0f; // 十字网格切割持续时间
    public Vector2 arenaBounds = new Vector2(15f, 9f); // 屏幕/场地边缘坐标
   

    // --- 状态实例声明 (等下我们写具体状态时取消注释) ---
    public ShieldPunchState shieldPunchState;
    public RotationLaserState rotationLaserState;
    public GridCutState gridCutState;

    public Vector3[] shieldInitialLocalPos = new Vector3[4];
    private Vector3[] shieldInitialScale = new Vector3[4];

    // 转阶段标志位
    private bool isPhase2Unlocked = false;
    public bool isPhase3Unlocked = false;

    protected override void Start()
    {
        // 1. 调用基类的 Start，这会自动初始化挂载在 Boss Parts 列表里的所有部位（包括设为无敌的护盾）
        base.Start();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
        // 2. 记录 4 块护甲拼合时的初始位置
        for (int i = 0; i < 4; i++)
        {
            if (shieldsTransforms[i] != null)
            {
                shieldInitialLocalPos[i] = shieldsTransforms[i].localPosition;
                shieldInitialScale[i] = shieldsTransforms[i].localScale;
            }
        }

        // 3. 初始化状态机实例
        shieldPunchState = new ShieldPunchState();
        rotationLaserState = new RotationLaserState();
        gridCutState = new GridCutState();
        // ...

        // 4. 初始状态进入一阶段
        SwitchState(shieldPunchState);
    }

    // 【必须实现】：基类 BossBase 的抽象方法，每次受伤都会调用
    protected override void CheckPhaseTransition()
    {
        float healthRatio = currentHp / maxHp;

        if (healthRatio <= 0.666f && !isPhase2Unlocked)
        {
            isPhase2Unlocked = true;
            Debug.Log("PlasmaBall: 血量降至 2/3，二阶段[旋转锁链]启动！");

            // 强行打断当前的一阶段状态，切换为二阶段
            SwitchState(rotationLaserState);
        }
        if (healthRatio <= 0.5f && !isPhase3Unlocked)
        {
            isPhase3Unlocked = true;
            Debug.Log("PlasmaBall: 血量降至 1/2，三阶段[正交网格切割]启动！");

            // 强行打断当前状态，进入三阶段
            SwitchState(gridCutState);
            return;
        }
    }


    // ==========================================
    // 护甲运动底层 API (供各个状态调用)
    // ==========================================

    /// <summary>
    /// 将指定的护甲作为“独立飞行物”弹出到世界坐标目标点 (用于一阶段攻击)
    /// </summary>
    public void PopShieldToWorldPos(int index, Vector3 targetWorldPos)
    {
        if (shieldsTransforms[index] == null) return;

        Transform shield = shieldsTransforms[index];

        // 【关键】：从容器中解绑，使其在世界空间中独立移动，不受本体移动影响
        shield.SetParent(null);

        // 计算飞行时间 (距离 / 速度)
        float dist = Vector3.Distance(shield.position, targetWorldPos);
        float duration = dist / shieldPopSpeed;

        shield.DOMove(targetWorldPos, duration).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 将指定的护甲向外推开一段距离，但仍保持跟随本体 (用于二阶、三阶的锁链形态)
    /// </summary>
    public void PushShieldOutward(int index, float distance, float duration)
    {
        if (shieldsTransforms[index] == null) return;

        Transform shield = shieldsTransforms[index];

        // 获取护盾向外的方向向量
        Vector3 pushDir = shieldInitialLocalPos[index].normalized;
        Vector3 targetLocalPos = shieldInitialLocalPos[index] + pushDir * distance;

        shield.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 强制收回指定的护甲
    /// </summary>
    public void ReturnShield(int index)
    {
        if (shieldsTransforms[index] == null) return;

        Transform shield = shieldsTransforms[index];
        shield.DOKill(); // 杀掉正在进行的飞行或外推前摇

        // 【关键】：重新认父，回到旋转容器中
        shield.SetParent(shieldsContainer);

        // 飞回初始的拼合位置，并将旋转归零
        shield.DOLocalMove(shieldInitialLocalPos[index], shieldReturnTime).SetEase(Ease.InQuad);
        shield.DOLocalRotate(Vector3.zero, shieldReturnTime);
        shield.DOScale(shieldInitialScale[index], shieldReturnTime);
    }

    /// <summary>
    /// 一键收回所有护甲（用于阶段切换前的强制清理）
    /// </summary>
    public void ReturnAllShields()
    {
        for (int i = 0; i < 4; i++)
        {
            ReturnShield(i);
        }
    }

    protected override void CleanupBossArtifacts()
    {
        transform.DOKill();

        if (shieldsContainer != null)
            shieldsContainer.localRotation = Quaternion.identity;

        for (int i = 0; i < shieldsTransforms.Length; i++)
        {
            var shield = shieldsTransforms[i];
            if (shield == null)
                continue;

            shield.DOKill();
            shield.SetParent(shieldsContainer);
            shield.localPosition = i < shieldInitialLocalPos.Length ? shieldInitialLocalPos[i] : Vector3.zero;
            shield.localRotation = Quaternion.identity;
            shield.localScale = i < shieldInitialScale.Length ? shieldInitialScale[i] : Vector3.one;
            shield.gameObject.SetActive(true);
        }
    }
}
