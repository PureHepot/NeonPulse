using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 【关键点1】显式声明实现 IDamageable，确保子弹的 GetComponent<IDamageable> 调用会走这里的逻辑
public class BossSinger : EnemyBase, IDamageable
{
    [Header("=== 状态机核心 ===")]
    private SingerBossBaseState currentState;
    public BossPhase1State Phase1 { get; private set; }
    public BossPhase2State Phase2 { get; private set; }
    public BossPhase3State Phase3 { get; private set; }
    public BossPhase4State Phase4 { get; private set; }

    [Header("=== ?? 阶段独立血量配置 (Inspector数值优先) ===")]
    [Tooltip("P1 阶段独立血量")]
    public float hpPhase1 = 1000f;
    [Tooltip("P2 阶段独立血量")]
    public float hpPhase2 = 1000f;
    [Tooltip("P3 阶段独立血量")]
    public float hpPhase3 = 1500f;

    // 运行时实时记录各阶段剩余血量
    public float currentHpP1;
    public float currentHpP2;
    public float currentHpP3;

    // 【关键点2】总血量属性 (供外部读取)
    public float CurrentTotalHp { get; private set; }
    public float MaxTotalHp { get; private set; }

    // 父类血量“护盾”
    private float _hpShield = 999999f;
    private float _lastFrameHp;

    [Header("=== Phase 4: 最后的挣扎 ===")]
    public float p4Duration = 10.0f;
    public GameObject loudspeakerLeftPrefab;
    public GameObject loudspeakerRightPrefab;

    [Header("=== Phase 1: 引用与参数 ===")]
    public GameObject idleForm;
    public GameObject battleForm;
    public Transform faceAngry;
    public Transform speakerLeft;
    public Transform speakerRight;
    public Transform hairLeft;
    public Transform hairRight;
    public Vector3 faceTargetPos = new Vector3(0, 4, 0);
    public Vector3 speakerLeftTargetPos = new Vector3(-7, 4, 0);
    public Vector3 speakerRightTargetPos = new Vector3(7, 4, 0);
    public Vector3 hairLeftTargetPos = new Vector3(-8.5f, 0, 0);
    public Vector3 hairRightTargetPos = new Vector3(8.5f, 0, 0);
    public float deployDuration = 2.0f;
    public Vector3 hairLeftTargetRot = new Vector3(0, 0, 90);
    public Vector3 hairRightTargetRot = new Vector3(0, 0, -90);
    public float hairMoveSpeed = 2.0f;
    public float hairTopY = 2.0f;
    public float hairBottomY = -4.0f;
    public float faceHoverAmplitude = 0.5f;
    public float faceHoverSpeed = 1.5f;

    [Header("=== 弹幕攻击 ===")]
    public GameObject redBulletPrefab;
    public float p1AttackInterval = 3.5f;
    public int roundsPerAttack = 3;
    public float pointStaggerDelay = 0.15f;
    public float showerSpreadAngle = 15f;
    public Vector2 bulletSpeedRange = new Vector2(3f, 5f);
    public int maxSearchIndex = 5;

    [Header("=== Phase 2: 激光与干扰 ===")]
    public GameObject levelHairPrefab;
    public GameObject verticalHairPrefab;
    public GameObject laserBeamPrefab;

    // 【修复 CS1061】补回缺失的变量定义
    public float p2LaserDuration = 5.0f;
    public float p2StabilizeTime = 1.0f;
    public float p2PostFireDelay = 0.5f;
    public float p2LaserWidth = 1.5f;
    public float p2EnterDelay = 1.5f;
    public float p2ExitDelay = 1.5f;
    public float p2TotalDuration = 15.0f;

    public float levelHairY = 4.5f;
    public Vector2 levelHairXRange = new Vector2(-6f, 6f);
    public float verticalHairX = -8f;
    public Vector2 verticalHairYRange = new Vector2(-3f, 3f);

    [Header("=== Phase 3: 狙击与冲撞 ===")]
    public GameObject shortLevelHairPrefab;
    public GameObject shortVerticalHairPrefab;
    public float p3ShootInterval = 0.8f;
    public float p3AimTime = 0.4f;
    public float p3LaserActiveTime = 0.2f;
    public float p3LaserWidth = 1.0f;
    public float p3LevelY = 4.5f;
    public float p3VerticalX = 8.5f;
    public float p3ChargeAimTime = 1.0f;
    public float p3ChargeSpeed = 15.0f;
    public float p3BrakeDuration = 1.5f;

    [Header("=== 屏幕干扰技能 ===")]
    public GameScreenRotator screenRotator;
    public float rotateInterval = 5.0f;
    [Range(0, 1)] public float flipChance = 0.2f;

    [Header("=== 转场特效 ===")]
    public CanvasGroup whiteScreenEffect;
    public float transShakeDuration = 1.5f;
    public float transFadeInDuration = 0.5f;
    public float transHoldDuration = 0.5f;
    public float transFadeOutDuration = 0.5f;

    // 内部状态
    private Coroutine screenDisturbRoutine;
    public List<Transform> leftBulletPoints = new List<Transform>();
    public List<Transform> rightBulletPoints = new List<Transform>();
    [HideInInspector] public float savedHairLeftX, savedHairLeftZ;
    [HideInInspector] public float savedHairRightX, savedHairRightZ;
    private float moveStartTime;

    // 状态标记
    [HideInInspector] public bool isTransitioning = false;
    [HideInInspector] public bool isInFinalPhase = false;
    [HideInInspector] public bool hasFinishedPhase2 = false;
    [HideInInspector] public bool hasFinishedPhase3 = false;

    private bool enableAnimations = true;
    private bool isP1Attacking = false;

    public float HairCenterY => (hairTopY + hairBottomY) / 2f;
    public float HairAmplitude => (hairTopY - hairBottomY) / 2f;

    protected override void Awake()
    {
        base.Awake();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (screenRotator == null) screenRotator = FindObjectOfType<GameScreenRotator>();

        SetupWhiteScreenEffect(); // 确保这里调用了

        Phase1 = new BossPhase1State(this);
        Phase2 = new BossPhase2State(this);
        Phase3 = new BossPhase3State(this);
        Phase4 = new BossPhase4State(this);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();

        // 1. 初始化各阶段血量
        currentHpP1 = hpPhase1;
        currentHpP2 = hpPhase2;
        currentHpP3 = hpPhase3;

        // 2. 计算总血量
        MaxTotalHp = hpPhase1 + hpPhase2 + hpPhase3;
        CurrentTotalHp = MaxTotalHp;

        // 3. 设置父类“护盾血量”
        currentHp = _hpShield;
        _lastFrameHp = currentHp;

        // 4. 重置标记
        isTransitioning = false;
        enableAnimations = true;
        isInFinalPhase = false;
        hasFinishedPhase2 = false;
        hasFinishedPhase3 = false;
        isP1Attacking = false;

        if (whiteScreenEffect) whiteScreenEffect.alpha = 0;
        StopScreenDisturb();

        Debug.Log($"Boss初始化: 总血量={CurrentTotalHp} (P1={hpPhase1}, P2={hpPhase2}, P3={hpPhase3})");

        TransitionToState(Phase1);
    }

    private void Update()
    {
        // 1. 监听并分配伤害
        MonitorAndDistributeDamage();
        // 2. 状态机更新
        currentState?.Update();
    }

    // --- 伤害分配核心逻辑 ---
    private void MonitorAndDistributeDamage()
    {
        if (isTransitioning || isInFinalPhase)
        {
            currentHp = _hpShield;
            _lastFrameHp = currentHp;
            return;
        }

        // 检测父类血量是否减少 (如果子弹通过父类扣血，这里会捕捉到)
        if (currentHp < _lastFrameHp)
        {
            float rawDamage = _lastFrameHp - currentHp;
            float damageValue = Mathf.Max(1f, rawDamage); // 保底1点伤害

            ApplyPhaseDamage(damageValue);

            // 重置父类护盾
            currentHp = _hpShield;
        }

        _lastFrameHp = currentHp;
    }

    // --- 接口实现 (拦截 PlayerBullet) ---
    // 【关键修复 CS0506】使用 new 关键字而不是 override
    // 这告诉编译器：我要隐藏父类的方法，自己实现一个新版本
    // 配合类声明中的 IDamageable，子弹调用接口时会优先走这里
    public new void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 1. 播放父类的视觉特效
        base.PlayHitEffect(hitPoint, hitNormal);

        // 2. 执行我们自己的分段扣血逻辑
        ApplyPhaseDamage(amount);
    }

    public new void TakeDamage(float amount)
    {
        base.PlayHitEffect(transform.position, Vector3.zero);
        ApplyPhaseDamage(amount);
    }

    public new void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackDir, float customForce)
    {
        base.PlayHitEffect(hitPoint, knockbackDir);
        ApplyPhaseDamage(amount);
    }

    // --- 核心扣血逻辑 (溢出保护) ---
    public void ApplyPhaseDamage(float damage)
    {
        if (damage <= 0f) damage = 1f;
        if (isTransitioning || isInFinalPhase) return;

        float damageToApply = 0f;

        if (currentState == Phase1)
        {
            // 溢出保护：只扣除当前阶段的剩余血量
            damageToApply = Mathf.Min(damage, currentHpP1);
            currentHpP1 -= damageToApply;

            if (currentHpP1 <= 0)
            {
                currentHpP1 = 0;
                Debug.Log("<color=red>P1 击破 -> 转 P2</color>");
                TriggerPhaseTransition(Phase2);
            }
        }
        else if (currentState == Phase2)
        {
            damageToApply = Mathf.Min(damage, currentHpP2);
            currentHpP2 -= damageToApply;

            if (currentHpP2 <= 0)
            {
                currentHpP2 = 0;
                Debug.Log("<color=red>P2 击破 -> 转 P3</color>");
                TriggerPhaseTransition(Phase3);
            }
        }
        else if (currentState == Phase3)
        {
            damageToApply = Mathf.Min(damage, currentHpP3);
            currentHpP3 -= damageToApply;

            if (currentHpP3 <= 0)
            {
                currentHpP3 = 0;
                Debug.Log("<color=red>P3 击破 -> 转 P4</color>");
                TriggerPhaseTransition(Phase4);
            }
        }

        // 更新总血量 (供显示用)
        CurrentTotalHp -= damageToApply;
        Debug.Log($"[BOSS受伤] 原始伤害:{damage} | 有效扣血:{damageToApply} | 总血量:{CurrentTotalHp}");
    }

    // 左上角显示信息
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        string info = "";
        if (isInFinalPhase) info = "PHASE: 4 (FINAL)";
        else if (isTransitioning) info = "STATUS: TRANSITIONING...";
        else
        {
            info += $"TOTAL HP: {CurrentTotalHp} / {MaxTotalHp}\n";
            info += $"P1: {currentHpP1} | P2: {currentHpP2} | P3: {currentHpP3}";
        }

        style.richText = true;
        GUI.Label(new Rect(20, 20, 500, 150), info, style);
    }

    // ... (辅助方法) ...
    public IEnumerator FirePhase1Barrage() { isP1Attacking = true; Coroutine L = StartCoroutine(FireSequenceRoutine(leftBulletPoints, new Vector3(0.5f, -1f, 0))); Coroutine R = StartCoroutine(FireSequenceRoutine(rightBulletPoints, new Vector3(-0.5f, -1f, 0))); yield return L; yield return R; }
    public void StopPhase1Attack() { isP1Attacking = false; }
    public IEnumerator FireSequenceRoutine(List<Transform> p, Vector3 d) { if (redBulletPrefab == null) yield break; for (int r = 0; r < roundsPerAttack; r++) { foreach (var pt in p) { if (!isP1Attacking && !isInFinalPhase && currentState != Phase2) yield break; if (!isInFinalPhase && !battleForm.activeInHierarchy) yield break; if (pt == null) continue; GameObject b = ObjectPoolManager.Instance != null ? ObjectPoolManager.Instance.Get(redBulletPrefab, pt.position, Quaternion.identity) : Instantiate(redBulletPrefab, pt.position, Quaternion.identity); Vector3 td = d.normalized; float a = Random.Range(-showerSpreadAngle, showerSpreadAngle); Vector3 fd = Quaternion.Euler(0, 0, a) * td; float s = Random.Range(bulletSpeedRange.x, bulletSpeedRange.y); var ep = b.GetComponent<EnemyProjectile>(); if (ep != null) { ep.speed = s; ep.Initialize(fd); } yield return new WaitForSeconds(pointStaggerDelay); } } }
    public void ClearAllBullets() { EnemyProjectile[] bullets = FindObjectsOfType<EnemyProjectile>(); foreach (var b in bullets) { if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.Return(b.gameObject); else Destroy(b.gameObject); } }
    public void DieForReal() { if (currentState != null) currentState.Exit(); Destroy(gameObject); }

    // 【修复 CS0103】补回丢失的辅助方法
    void SetupWhiteScreenEffect() { if (whiteScreenEffect != null) { ForceUpdatePanel(whiteScreenEffect.gameObject); return; } GameObject existingPanel = GameObject.Find("WhiteFlashPanel"); if (existingPanel != null) { ForceUpdatePanel(existingPanel); whiteScreenEffect = existingPanel.GetComponent<CanvasGroup>(); return; } GameObject newCanvas = new GameObject("AutoCanvas_WhiteScreen"); newCanvas.layer = 5; Canvas c = newCanvas.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 9999; newCanvas.AddComponent<CanvasScaler>(); newCanvas.AddComponent<GraphicRaycaster>(); GameObject panelObj = new GameObject("WhiteFlashPanel"); panelObj.layer = 5; panelObj.transform.SetParent(newCanvas.transform, false); RectTransform rt = panelObj.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; panelObj.AddComponent<Image>(); whiteScreenEffect = panelObj.AddComponent<CanvasGroup>(); ForceUpdatePanel(panelObj); }
    void ForceUpdatePanel(GameObject panel) { Image img = panel.GetComponent<Image>(); if (img == null) img = panel.AddComponent<Image>(); img.color = new Color(1, 1, 1, 1); img.raycastTarget = false; CanvasGroup cg = panel.GetComponent<CanvasGroup>(); if (cg == null) cg = panel.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; Canvas parentCanvas = panel.GetComponentInParent<Canvas>(); if (parentCanvas != null) { parentCanvas.sortingOrder = 9999; parentCanvas.gameObject.layer = 5; } panel.SetActive(true); }

    public void TriggerPhaseTransition(SingerBossBaseState nextState) { if (isTransitioning) return; StartCoroutine(TransitionEffectRoutine(nextState)); }
    IEnumerator TransitionEffectRoutine(SingerBossBaseState nextState) { isTransitioning = true; enableAnimations = false; if (faceAngry) faceAngry.DOShakePosition(transShakeDuration, 0.6f, 30, 90, false, true); float waitBeforeFade = Mathf.Max(0, transShakeDuration - transFadeInDuration); yield return new WaitForSeconds(waitBeforeFade); if (whiteScreenEffect) whiteScreenEffect.DOFade(1f, transFadeInDuration); yield return new WaitForSeconds(transFadeInDuration); TransitionToState(nextState); yield return new WaitForSeconds(transHoldDuration); if (whiteScreenEffect) whiteScreenEffect.DOFade(0f, transFadeOutDuration); enableAnimations = true; isTransitioning = false; }
    public void TransitionToState(SingerBossBaseState newState) { if (currentState != null) currentState.Exit(); currentState = newState; currentState.Enter(); }
    public void SetPhase1PartsActive(bool isActive) { if (battleForm) battleForm.SetActive(isActive); if (faceAngry) faceAngry.gameObject.SetActive(isActive); if (speakerLeft) speakerLeft.gameObject.SetActive(isActive); if (speakerRight) speakerRight.gameObject.SetActive(isActive); if (hairLeft) hairLeft.gameObject.SetActive(isActive); if (hairRight) hairRight.gameObject.SetActive(isActive); }
    public void SetPhase4PartsActive() { if (battleForm) battleForm.SetActive(false); }
    public void FindBulletPoints() { leftBulletPoints.Clear(); rightBulletPoints.Clear(); for (int i = 1; i <= maxSearchIndex; i++) { string n = "BulletPoint" + i; if (speakerLeft) { Transform t = FindDeepChild(speakerLeft, n); if (t) leftBulletPoints.Add(t); } if (speakerRight) { Transform t = FindDeepChild(speakerRight, n); if (t) rightBulletPoints.Add(t); } } leftBulletPoints.Sort((a, b) => string.Compare(a.name, b.name)); rightBulletPoints.Sort((a, b) => string.Compare(a.name, b.name)); }
    public void ResetHairMovementTime() { moveStartTime = Time.time; }
    public void HandlePhase1HairMovement() { if (!enableAnimations) return; float t = Time.time - moveStartTime; float o = Mathf.Sin(t * hairMoveSpeed) * HairAmplitude; if (hairLeft) hairLeft.position = new Vector3(savedHairLeftX, HairCenterY + o, savedHairLeftZ); if (hairRight) hairRight.position = new Vector3(savedHairRightX, HairCenterY - o, savedHairRightZ); }
    public void HandleFaceHover() { if (faceAngry == null || !enableAnimations) return; float t = Time.time - moveStartTime; float y = Mathf.Sin(t * faceHoverSpeed) * faceHoverAmplitude; faceAngry.position = new Vector3(faceTargetPos.x, faceTargetPos.y + y, faceTargetPos.z); }
    public void StartScreenDisturb() { if (screenDisturbRoutine != null) StopCoroutine(screenDisturbRoutine); screenDisturbRoutine = StartCoroutine(DisturbRoutine()); }
    public void StopScreenDisturb() { if (screenDisturbRoutine != null) StopCoroutine(screenDisturbRoutine); screenDisturbRoutine = null; if (screenRotator) screenRotator.ResetImmediate(); }
    IEnumerator DisturbRoutine() { while (true) { yield return new WaitForSeconds(rotateInterval); if (screenRotator == null) yield break; float r = Random.value; float a = 0f; if (r < flipChance) a = 180f; else { int rr = Random.Range(0, 3); if (rr == 1) a = 30f; else if (rr == 2) a = -30f; } screenRotator.RotateTo(a); } }
    public Transform FindDeepChild(Transform p, string n) { foreach (Transform c in p) { if (c.name == n) return c; Transform r = FindDeepChild(c, n); if (r != null) return r; } return null; }
    protected override void MoveBehavior() { }
}

