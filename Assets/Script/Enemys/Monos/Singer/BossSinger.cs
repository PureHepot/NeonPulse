using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossSinger : EnemyBase
{
    [Header("=== 状态机核心 ===")]
    private SingerBossBaseState currentState;
    public BossPhase1State Phase1 { get; private set; }
    public BossPhase2State Phase2 { get; private set; }
    public BossPhase3State Phase3 { get; private set; }
    public BossPhase4State Phase4 { get; private set; }

    [Header("=== Phase 1: 引用 ===")]
    public GameObject idleForm;
    public GameObject battleForm;
    public Transform faceAngry;
    public Transform speakerLeft;
    public Transform speakerRight;
    public Transform hairLeft;
    public Transform hairRight;

    [Header("=== Phase 1: 部署位置 ===")]
    public Vector3 faceTargetPos = new Vector3(0, 4, 0);
    public Vector3 speakerLeftTargetPos = new Vector3(-7, 4, 0);
    public Vector3 speakerRightTargetPos = new Vector3(7, 4, 0);
    public Vector3 hairLeftTargetPos = new Vector3(-8.5f, 0, 0);
    public Vector3 hairRightTargetPos = new Vector3(8.5f, 0, 0);
    public float deployDuration = 2.0f;

    [Header("=== Phase 1: 头发运动 ===")]
    public Vector3 hairLeftTargetRot = new Vector3(0, 0, 90);
    public Vector3 hairRightTargetRot = new Vector3(0, 0, -90);
    public float hairMoveSpeed = 2.0f;
    public float hairTopY = 2.0f;
    public float hairBottomY = -4.0f;

    [Header("=== Phase 1: 脸部悬浮参数 ===")]
    public float faceHoverAmplitude = 0.5f;
    public float faceHoverSpeed = 1.5f;

    [Header("=== Phase 1: 弹幕攻击 ===")]
    public GameObject redBulletPrefab;
    public float p1AttackInterval = 3.5f;
    public int roundsPerAttack = 3;
    public float pointStaggerDelay = 0.15f;
    public float showerSpreadAngle = 15f;
    public Vector2 bulletSpeedRange = new Vector2(3f, 5f);
    public int maxSearchIndex = 5;

    [Header("=== Phase 2: 激光阶段设置 ===")]
    public GameObject levelHairPrefab;
    public GameObject verticalHairPrefab;
    public GameObject laserBeamPrefab;
    public float p2TotalDuration = 15.0f;
    public float p2EnterDelay = 1.5f;
    public float p2ExitDelay = 1.5f;
    public float p2StabilizeTime = 1.0f;
    public float p2PostFireDelay = 0.5f;
    public float p2LaserWidth = 1.5f;
    public float levelHairY = 4.5f;
    public Vector2 levelHairXRange = new Vector2(-6f, 6f);
    public float verticalHairX = -8f;
    public Vector2 verticalHairYRange = new Vector2(-3f, 3f);
    public bool hasFinishedPhase2 = false;

    [Header("=== Phase 3: 终极狙击阶段 ===")]
    public GameObject shortLevelHairPrefab;
    public GameObject shortVerticalHairPrefab;
    public float p3ShootInterval = 0.8f;
    public float p3AimTime = 0.4f;
    public float p3LaserActiveTime = 0.2f;
    public float p3LaserWidth = 1.0f;
    public float p3LevelY = 4.5f;
    public float p3VerticalX = 8.5f;
    public bool hasFinishedPhase3 = false;

    [Header("=== Phase 3: 疯牛冲撞参数 ===")]
    [Tooltip("锁定预警时间 (瞪着玩家的时间)")]
    public float p3ChargeAimTime = 1.0f;
    [Tooltip("冲撞速度 (越小越慢)")]
    public float p3ChargeSpeed = 15.0f;
    [Tooltip("刹车停稳时间 (冲完停多久)")]
    public float p3BrakeDuration = 1.5f;

    [Header("=== Phase 4: 垂死挣扎阶段 ===")]
    public float p4Duration = 10.0f;
    [Tooltip("P4 阶段生成的左音响预制件")]
    public GameObject loudspeakerLeftPrefab;
    [Tooltip("P4 阶段生成的右音响预制件")]
    public GameObject loudspeakerRightPrefab;

    [Header("=== 屏幕干扰技能 ===")]
    public GameScreenRotator screenRotator;
    public float rotateInterval = 5.0f;
    [Range(0, 1)] public float flipChance = 0.2f;

    [Header("=== 转场特效设置 (自动生成) ===")]
    public CanvasGroup whiteScreenEffect;
    public float transShakeDuration = 1.5f;
    public float transFadeInDuration = 0.5f;
    public float transHoldDuration = 0.5f;
    public float transFadeOutDuration = 0.5f;

    private Coroutine screenDisturbRoutine;
    [SerializeField] private string currentStateName;
    public float HairCenterY => (hairTopY + hairBottomY) / 2f;
    public float HairAmplitude => (hairTopY - hairBottomY) / 2f;
    public List<Transform> leftBulletPoints = new List<Transform>();
    public List<Transform> rightBulletPoints = new List<Transform>();
    [HideInInspector] public float savedHairLeftX, savedHairLeftZ;
    [HideInInspector] public float savedHairRightX, savedHairRightZ;
    private float moveStartTime;

    [HideInInspector] public bool isTransitioning = false;
    private bool enableAnimations = true;

    public float HpPercent => (float)currentHp / maxHp;
    private bool isP1Attacking = false;

    [HideInInspector] public bool isInFinalPhase = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (screenRotator == null) screenRotator = FindObjectOfType<GameScreenRotator>();

        SetupWhiteScreenEffect();

        Phase1 = new BossPhase1State(this);
        Phase2 = new BossPhase2State(this);
        Phase3 = new BossPhase3State(this);
        Phase4 = new BossPhase4State(this);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        hasFinishedPhase2 = false;
        hasFinishedPhase3 = false;
        isTransitioning = false;
        enableAnimations = true;
        isInFinalPhase = false;

        if (whiteScreenEffect) whiteScreenEffect.alpha = 0;
        StopScreenDisturb();

        TransitionToState(Phase1);
    }

    private void Update()
    {
        currentState?.Update();
        currentStateName = currentState?.GetType().Name;

        // 优先检测死亡 (P4 可以被杀死)
        if (currentHp <= 0)
        {
            DieForReal();
            return;
        }

        CheckGlobalTransitions();
    }

    void CheckGlobalTransitions()
    {
        // P1/P2 -> P3
        if (!hasFinishedPhase3 && HpPercent <= 0.334f && currentState != Phase3 && currentState != Phase4 && !isTransitioning)
        {
            TriggerPhaseTransition(Phase3);
            return;
        }

        // P3 -> P4 (残血触发)
        if (currentState == Phase3 && !isInFinalPhase && !isTransitioning)
        {
            float p4Threshold = maxHp / 6.0f;
            if (currentHp <= p4Threshold && currentHp > 1)
            {
                Debug.Log($">>> 触发 P4 (HP: {currentHp} <= {p4Threshold})");
                TriggerPhaseTransition(Phase4);
            }
        }
    }

    // ... (后续 UI 和 功能代码保持不变，直接复制即可) ...
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
    public IEnumerator FirePhase1Barrage() { isP1Attacking = true; Coroutine L = StartCoroutine(FireSequenceRoutine(leftBulletPoints, new Vector3(0.5f, -1f, 0))); Coroutine R = StartCoroutine(FireSequenceRoutine(rightBulletPoints, new Vector3(-0.5f, -1f, 0))); yield return L; yield return R; }
    public void StopPhase1Attack() { isP1Attacking = false; }
    public IEnumerator FireSequenceRoutine(List<Transform> p, Vector3 d) { if (redBulletPrefab == null) yield break; for (int r = 0; r < roundsPerAttack; r++) { foreach (var pt in p) { if (!isP1Attacking && !isInFinalPhase) yield break; if (!isInFinalPhase && !battleForm.activeInHierarchy) yield break; if (pt == null) continue; GameObject b = ObjectPoolManager.Instance != null ? ObjectPoolManager.Instance.Get(redBulletPrefab, pt.position, Quaternion.identity) : Instantiate(redBulletPrefab, pt.position, Quaternion.identity); Vector3 td = d.normalized; float a = Random.Range(-showerSpreadAngle, showerSpreadAngle); Vector3 fd = Quaternion.Euler(0, 0, a) * td; float s = Random.Range(bulletSpeedRange.x, bulletSpeedRange.y); var ep = b.GetComponent<EnemyProjectile>(); if (ep != null) { ep.speed = s; ep.Initialize(fd); } yield return new WaitForSeconds(pointStaggerDelay); } } }
    public void ClearAllBullets() { EnemyProjectile[] bullets = FindObjectsOfType<EnemyProjectile>(); foreach (var b in bullets) { if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.Return(b.gameObject); else Destroy(b.gameObject); } }
    public void DieForReal() { if (currentState != null) currentState.Exit(); currentHp = 0; Destroy(gameObject); }
    public void StartScreenDisturb() { if (screenDisturbRoutine != null) StopCoroutine(screenDisturbRoutine); screenDisturbRoutine = StartCoroutine(DisturbRoutine()); }
    public void StopScreenDisturb() { if (screenDisturbRoutine != null) StopCoroutine(screenDisturbRoutine); screenDisturbRoutine = null; if (screenRotator) screenRotator.ResetImmediate(); }
    IEnumerator DisturbRoutine() { while (true) { yield return new WaitForSeconds(rotateInterval); if (screenRotator == null) yield break; float r = Random.value; float a = 0f; if (r < flipChance) a = 180f; else { int rr = Random.Range(0, 3); if (rr == 1) a = 30f; else if (rr == 2) a = -30f; } screenRotator.RotateTo(a); } }
    public Transform FindDeepChild(Transform p, string n) { foreach (Transform c in p) { if (c.name == n) return c; Transform r = FindDeepChild(c, n); if (r != null) return r; } return null; }
    public void ForceSetHp(int value) { currentHp = value; }
    protected override void MoveBehavior() { }
}
