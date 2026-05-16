using UnityEngine;
using DG.Tweening;

public class VocalistBoss : BossBase
{
    public enum PlayerStyleTestMode
    {
        AutoFromModules,
        ForceMelee,
        ForceRanged,
        ForceSummoner
    }

    [Header("目标引用")]
    public Transform playerTarget;

    [Header("武器引用")]
    public Transform leftDrill;
    public Transform rightDrill;
    public Transform headgear;
    public Transform headgearHome;
    public GameObject drillClonePrefab;

    [Header("锚点引用 (用于动画)")]
    public Transform[] hairAnchors = new Transform[2];
    public Transform[] handAnchors = new Transform[2];

    [Header("状态管理")]
    public VocalistIntroState introState;
    public VocalistMeleeState meleeState;
    public VocalistBerserkState berserkState;

    [Header("测试用玩家类型")]
    public PlayerStyleTestMode playerStyleTestMode = PlayerStyleTestMode.AutoFromModules;

    [Header("钻头投掷")]
    public float drillThrowDelayBeforeHeadgear = 2f;
    public float drillSpeed = 15f;
    public int drillMaxBounces = 5;
    public float drillMaxFlightTime = 5f;
    public int drillDamage = 1;

    [Header("头饰回旋镖")]
    public float headgearSpeed = 9f;
    public float headgearFlightDuration = 5f;
    public float headgearDockWaitTime = 4f;
    public float headgearRecallDistance = 3f;
    public int headgearDamage = 1;

    [Header("狂暴阶段")]
    public float berserkMoveSpeed = 5f;
    public float berserkChargeSpeed = 13f;
    public float berserkChargeDuration = 1.1f;
    public float berserkRecoverDuration = 0.7f;
    public float cloneWaveInterval = 3f;
    public float cloneSequentialDelay = 0.25f;
    public float cloneSpeed = 18f;
    public float cloneLifetime = 4f;
    public int cloneDamage = 1;
    public Vector2 arenaHalfSize = new Vector2(9f, 5f);

    [HideInInspector] public bool isPhase2Triggered = false;
    [HideInInspector] public bool isPhase3Triggered = false;
    [HideInInspector] public bool hasThrownDesignDrill = false;
    [HideInInspector] public bool isBerserk = false;

    private VocalistHeadgearAI headgearAI;
    private float headgearThrowTimer = -1f;

    protected override void Start()
    {
        base.Start();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        PrepareWeaponReferences();

        introState = new VocalistIntroState();
        meleeState = new VocalistMeleeState();
        berserkState = new VocalistBerserkState();

        SwitchState(introState);
    }

    protected override void Update()
    {
        base.Update();
        UpdateDelayedHeadgearThrow();
    }

    protected override void CheckPhaseTransition()
    {
        float healthRatio = GetHealthRatio();

        if (healthRatio <= 0.8f && !isPhase2Triggered)
        {
            isPhase2Triggered = true;
            TryLaunchDesignDrill();
            Debug.Log("Vocalist: 进入二阶段，发射弹射钻头。 ");
        }

        if (healthRatio <= 0.5f && !isPhase3Triggered)
        {
            isPhase3Triggered = true;
            SwitchState(berserkState);
            Debug.Log("Vocalist: 进入狂暴阶段。 ");
        }
    }

    public float GetHealthRatio()
    {
        if (maxHp <= 0f) return 0f;
        return currentHp / maxHp;
    }

    public bool ShouldOpenWithDrillThrow()
    {
        switch (playerStyleTestMode)
        {
            case PlayerStyleTestMode.ForceRanged:
            case PlayerStyleTestMode.ForceSummoner:
                return true;
            case PlayerStyleTestMode.ForceMelee:
                return false;
        }

        PlayerController player = playerTarget != null ? playerTarget.GetComponent<PlayerController>() : null;
        if (player == null || player.Modules == null) return false;

        return player.Modules.HasAbility(ModuleType.Shooter)
            || player.Modules.HasAbility(ModuleType.Sniper)
            || player.Modules.HasAbility(ModuleType.Shotgun)
            || player.Modules.HasAbility(ModuleType.LaserDrone);
    }

    public bool TryLaunchDesignDrill()
    {
        if (hasThrownDesignDrill) return false;

        bool launched = LaunchOneDrill();
        if (!launched) return false;

        hasThrownDesignDrill = true;
        ScheduleHeadgearThrow(drillThrowDelayBeforeHeadgear);
        return true;
    }

    public bool LaunchOneDrill()
    {
        Transform drillToLaunch = PickAvailableDrill();
        if (drillToLaunch == null || playerTarget == null) return false;

        Transform returnAnchor = GetReturnAnchor(drillToLaunch);
        BouncingDrill bouncingDrill = drillToLaunch.GetComponent<BouncingDrill>();
        if (bouncingDrill == null) bouncingDrill = drillToLaunch.gameObject.AddComponent<BouncingDrill>();

        bouncingDrill.speed = drillSpeed;
        bouncingDrill.maxBounces = drillMaxBounces;
        bouncingDrill.maxFlightTime = drillMaxFlightTime;
        bouncingDrill.damage = drillDamage;

        Vector2 dir = (playerTarget.position - drillToLaunch.position).normalized;
        bouncingDrill.Launch(dir, returnAnchor);

        Debug.Log($"Vocalist 发射了钻头: {drillToLaunch.name}");
        return true;
    }

    public void ThrowHeadgearNow()
    {
        if (headgearAI == null || playerTarget == null || isBerserk) return;

        Vector2 dir = (playerTarget.position - headgearAI.transform.position).normalized;
        headgearAI.Throw(dir);
    }

    public bool ShouldRetrieveHeadgear()
    {
        if (headgearAI == null || isBerserk) return false;
        if (!headgearAI.CanBeRecalledEarly) return false;

        return Vector2.Distance(transform.position, headgearAI.DockPosition) <= headgearRecallDistance;
    }

    public bool HasDockedHeadgearReady()
    {
        return headgearAI != null && headgearAI.CanBeRecalledEarly && !isBerserk;
    }

    public Vector3 GetHeadgearDockPosition()
    {
        return headgearAI != null ? headgearAI.DockPosition : transform.position;
    }

    public void SpawnDrillClone(Vector2 spawnPosition, Vector2 direction)
    {
        GameObject template = drillClonePrefab != null ? drillClonePrefab : GetCloneFallbackTemplate();
        if (template == null) return;

        GameObject clone = Instantiate(template, spawnPosition, Quaternion.identity);
        clone.name = "VocalistDrillClone";
        clone.transform.localScale = template.transform.lossyScale;

        BossPart part = clone.GetComponent<BossPart>();
        if (part != null) Destroy(part);

        BouncingDrill bouncing = clone.GetComponent<BouncingDrill>();
        if (bouncing != null) Destroy(bouncing);

        VocalistDrillCloneProjectile projectile = clone.GetComponent<VocalistDrillCloneProjectile>();
        if (projectile == null) projectile = clone.AddComponent<VocalistDrillCloneProjectile>();

        projectile.Launch(direction, cloneSpeed, cloneLifetime, cloneDamage);
    }

    private void PrepareWeaponReferences()
    {
        if (headgear != null)
        {
            if (headgearHome == null)
            {
                GameObject home = new GameObject("HeadgearHome");
                home.transform.SetParent(transform);
                home.transform.position = headgear.position;
                home.transform.rotation = headgear.rotation;
                headgearHome = home.transform;
            }

            headgearAI = headgear.GetComponent<VocalistHeadgearAI>();
            if (headgearAI == null) headgearAI = headgear.gameObject.AddComponent<VocalistHeadgearAI>();

            headgearAI.speed = headgearSpeed;
            headgearAI.flightDuration = headgearFlightDuration;
            headgearAI.dockWaitTime = headgearDockWaitTime;
            headgearAI.damage = headgearDamage;
            headgearAI.fallbackArenaHalfSize = arenaHalfSize;
            headgearAI.Initialize(this);
        }
    }

    private void ScheduleHeadgearThrow(float delay)
    {
        if (headgearAI == null || isBerserk) return;
        headgearThrowTimer = Mathf.Max(0f, delay);
    }

    private void UpdateDelayedHeadgearThrow()
    {
        if (headgearThrowTimer < 0f) return;

        headgearThrowTimer -= Time.deltaTime;
        if (headgearThrowTimer <= 0f)
        {
            headgearThrowTimer = -1f;
            ThrowHeadgearNow();
        }
    }

    private Transform PickAvailableDrill()
    {
        bool leftAvailable = IsDrillAvailable(leftDrill);
        bool rightAvailable = IsDrillAvailable(rightDrill);

        if (leftAvailable && rightAvailable) return Random.value > 0.5f ? leftDrill : rightDrill;
        if (leftAvailable) return leftDrill;
        if (rightAvailable) return rightDrill;
        return null;
    }

    private bool IsDrillAvailable(Transform drill)
    {
        if (drill == null) return false;
        return drill.parent != null;
    }

    private Transform GetReturnAnchor(Transform drill)
    {
        if (drill == leftDrill && handAnchors != null && handAnchors.Length > 0) return handAnchors[0];
        if (drill == rightDrill && handAnchors != null && handAnchors.Length > 1) return handAnchors[1];
        return null;
    }

    private GameObject GetCloneFallbackTemplate()
    {
        if (leftDrill != null) return leftDrill.gameObject;
        if (rightDrill != null) return rightDrill.gameObject;
        return null;
    }
}
