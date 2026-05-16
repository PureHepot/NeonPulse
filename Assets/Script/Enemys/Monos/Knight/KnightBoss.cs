using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightBoss : BossBase
{
    [Header("Knight Specific")]
    
    public Transform playerTarget;
    public LaserBeam laserPrefab;

    [Header("近战冲刺攻击设置")]
    public float meleeDashSpeed = 20f;
    public float meleeSpinDuration = 0.3f;
    public int meleeRepeatCount = 3;
    public float meleeAimDuration = 0.2f;
    public float meleeRecoveryDuration = 0.6f;

    [Header("飞行形态基础设置")]
    public float flightTransformDuration = 0.8f;
    public int flightRepeatCount = 3;

    [Header("S型盘旋攻击")]
    public float flightSCurveForwardSpeed = 12f;
    [Tooltip("S型幅度")]
    public float flightSCurveAmplitude = 10f;
    [Tooltip("S型频率")]
    public float flightSCurveFrequency = 5f;
    public float flightSCurveDuration = 1.5f;

    [HideInInspector]
    public bool lastFlightWasSCurve = false;

    [Header("直线折返冲刺攻击")]
    public float flightStraightAimDuration = 0.3f;
    public float flightStraightDashSpeed = 30f;
    [Tooltip("冲刺越过距离")]
    public float flightDashOvershoot = 5f;
    [Tooltip("U型拐弯速度")]
    public float flightUTurnForwardSpeed = 15f;
    [Tooltip("U型拐弯旋转角速度")]
    public float flightUTurnAngularSpeed = 180f;

    [Header("二阶段概率权重")]
    public float meleeWeight = 20f;
    public float flightWeight = 40f;
    public float artilleryWeight = 40f;

    [Header("狂暴状态")]
    public bool isEnraged = false;
    private bool hasTriggeredEnrage = false; // 确保狂暴只触发一次
    [Header("狂暴特效部件")]
    public SpriteRenderer exMelee;
    public SpriteRenderer exFlight01;
    public SpriteRenderer exFlight02;
    public SpriteRenderer exFlight03;
    [Header("面具破损表现")]
    public GameObject normalMask;   // 完好的面具
    public GameObject brokenMask;   // 破损的面具
    [Header("虚弱状态")]
    public bool isExhausted = false; // 标记是否进入最后的挣扎阶段

    [HideInInspector] public BossBaseState lastAttackState;
    [HideInInspector] public Vector3 targetDashPos;

    public KnightObserveState observeState;
    public KnightMeleeState meleeState;
    public KnightFlightState flightState;
    public KnightArtilleryState artilleryState;
    public LaserSlashState laserSlashState;
    public EndAttackState endAttackState;
    public MeleeLaserSlashState meleeLaserSlashState; // 特殊复合斩击

    private bool hasTriggeredPhase2Entry = false;//狂热阶段
    public bool hasTriggeredFinalPhase = false;//最后阶段
    public bool IsPhase2Unlocked { get; private set; } = false;
    public BossPart LeftBlade { get; private set; }
    public BossPart RightBlade { get; private set; }

    public int contactDamage = 1;

    public BossBaseState CurrentState => currentState;
    
    protected override void Start()
    {
        DOTween.Init();
        DOTween.defaultUpdateType = UpdateType.Fixed;
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;

        LeftBlade = GetPart("BladeL");
        RightBlade = GetPart("BladeR");

        observeState = new KnightObserveState();
        meleeState = new KnightMeleeState();
        flightState = new KnightFlightState();
        artilleryState = new KnightArtilleryState();
        laserSlashState = new LaserSlashState();
        endAttackState = new EndAttackState();
        meleeLaserSlashState = new MeleeLaserSlashState();

        SwitchState(observeState);
    }



    protected override void CheckPhaseTransition()
    {
        
        float healthRatio = currentHp / maxHp;

        // 血量低于 2/3 且尚未触发过二阶段转场
        if (healthRatio <= 0.66f && !hasTriggeredPhase2Entry)
        {
            
            IsPhase2Unlocked = true;
            hasTriggeredPhase2Entry = true;

            Debug.Log("Knight: 触发二阶段转场激光斩击！");

            // 设置斩击结束后的下一个状态为观察状态
            laserSlashState.nextStateAfterSlash = observeState;
            SwitchState(laserSlashState);
        }
        // ：半血狂暴逻辑 (1/2 血量触发)
        if (healthRatio <= 0.5f && !hasTriggeredEnrage)
        {
            TriggerEnrage();
        }
        // 1/6血最后攻击
        if (healthRatio <= 0.166f && !hasTriggeredFinalPhase)
        {
            hasTriggeredFinalPhase = true;
            BreakMask();
            endAttackState.currentStep = 0;
            SwitchState(endAttackState);
        }
    }

    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);

        if (currentState is IAttackReactable reactableState)
        {
            reactableState.OnBossAttacked();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var shield = collision.collider.gameObject.GetComponent<ShieldController>();
        if (shield != null) return;

        if (collision.gameObject.CompareTag("Player") && !isDead)
        {
            collision.gameObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
        }
    }

    public bool AreAllPartsStatic()
    {
        if (bossParts == null) return true;
        foreach (var part in bossParts)
        {
            if (part != null && part.IsAnimating) return false;
        }
        return true;
    }
    // 狂暴相关

    // 集中处理狂暴时的参数强化
    private void TriggerEnrage()
    {
        isEnraged = true;
        hasTriggeredEnrage = true;
        Debug.Log("Knight: 血量低于50%，进入狂暴状态！");

        // === 核心参数调整（数值你可以根据手感微调） ===

        // 1. 飞行形态强化：冲刺速度提升 50%，冲刺次数 +1
        flightStraightDashSpeed *= 1.5f;
        flightRepeatCount += 1;

        // 2. 近战形态强化：大风车旋转时间缩短（视觉上转速大幅加快）
        meleeSpinDuration *= 0.6f;

        // 3. 观察形态强化：发呆时间减半，盘旋移动速度加快
        if (observeState != null)
        {
            observeState.observeDuration = 1.0f; // 攻击频率极高！
            observeState.orbitSpeed *= 1.5f;     // 盘旋压迫感更强
        }

        // === 视觉与听觉反馈 ===
        
        

        // 如果有屏幕震动或狂暴音效，可以写在这里：
        // CameraShake.Shake(0.5f, 2f);
        // AudioManager.Play("BossEnrageRoar");
    }
    /// <summary>
    /// 瞬间隐藏所有狂暴特效
    /// </summary>
    public void HideAllExParts()
    {
        if (exMelee) exMelee.gameObject.SetActive(false);
        if (exFlight01) exFlight01.gameObject.SetActive(false);
        if (exFlight02) exFlight02.gameObject.SetActive(false);
        if (exFlight03) exFlight03.gameObject.SetActive(false);
    }

    /// <summary>
    /// 以中心向两侧展开的方式显示特效
    /// </summary>
    public void ShowExPart(SpriteRenderer part, float duration = 0.3f)
    {
        if (part == null) return;

        part.gameObject.SetActive(true);

        // 杀掉可能残留的旧动画，防止冲突
        part.DOKill();
        part.transform.DOKill();

        // 初始状态：X轴缩放为0（挤压在中间），透明度为0
        part.transform.localScale = new Vector3(0, 1, 1);
        Color c = part.color;
        c.a = 0;
        part.color = c;

        // 动画：X轴展开到 1（向左右伸展），透明度渐变到 1
        part.transform.DOScaleX(1f, duration).SetEase(Ease.OutQuad);
        part.DOFade(1f, duration);
    }

    //虚弱相关

    /// <summary>
    /// 触发最后挣扎状态（由 EndAttackState 的最后一步调用）
    /// </summary>
    public void TriggerExhaustedState()
    {
        isExhausted = true;
        Debug.Log("Knight: 能量耗尽，进入最后的挣扎...");

        // 1. 速度大幅削弱
        meleeDashSpeed = 10f;        // 冲刺变得非常缓慢无力 (假设原先是25f~40f)
        meleeSpinDuration = 1f;   // 旋转一周需要 2 秒 (原本可能是 0.2 秒)，转速极慢！

        // 2. 视觉表现：机体失去光泽，变成黯淡的暗红色
        

        // 3. 观察期调整：在原地喘息更久，或者更无脑地扑向玩家
        if (observeState != null)
        {
            observeState.observeDuration = 1f; // 给玩家充足的打靶时间
            observeState.orbitSpeed = 15f;       // 盘旋变得摇摇欲坠
        }
    }

    /// <summary>
    /// 触发面具碎裂表现
    /// </summary>
    public void BreakMask()
    {
        if (normalMask != null) normalMask.SetActive(false);
        if (brokenMask != null) brokenMask.SetActive(true);

        // 可选：在这里加一点面具碎裂的爆点粒子或屏幕震动，增加冲击力
        // BackgroundFXController.Instance.TriggerDistortion(transform.position);
        // CameraShake.Shake(0.5f, 2f);
    }
}

public interface IAttackReactable
{
    void OnBossAttacked();
}