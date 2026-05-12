using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BossAirCraft : EnemyBase
{
    [Header("Parts References")]
    public BossPart bodyPart;
    public BossPart leftWing;
    public BossPart rightWing;
    public BossPart leftTurretPart;
    public BossPart rightTurretPart;

    [Header("Weapon Components")]
    public BossTurret leftTurret;
    public BossTurret rightTurret;

    [Header("Spawner Settings")]
    public List<GameObject> minionPrefabs;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public int spawnCountPerWave = 3;
    public int maxMinions = 18;
    [HideInInspector] public List<GameObject> activeMinions = new List<GameObject>();

    [Header("Movement Settings")]
    public Vector3 targetEntryPosition = new Vector3(0, 11, 0);
    public float enterSpeed = 3.0f;

    [Header("Laser Settings")]
    public GameObject laserBeamObj;
    public float laserWidth = 2.0f;       // 婵€鍏夊垽瀹氬搴?
    public float laserMaxDist = 20.0f;    // 婵€鍏夋渶澶ч暱搴?
    public int laserDamage = 5;           // 婵€鍏変激瀹?
    public float laserTickRate = 0.1f;    // 浼ゅ棰戠巼
    public LayerMask laserHitLayer;
    public float laserModeSpawnInterval = 1.5f;

    [Header("Smooth Hover Settings")]
    public float smoothTime = 0.8f;
    public float maxSpeed = 5.0f;
    public float xFreq = 0.6f;
    public float xDist = 6f;
    public float yFreq = 1.0f;
    public float yDist = 1f;

    public Vector2 CurrentVelocity;

    [Tooltip("Hover speed.")]
    public float hoverSpeed = 1.0f;
    [Tooltip("Hover distance.")]
    public float hoverDistance = 0.5f;

    [Tooltip("澶х敓鎴愰棿闅旓細涓ゆ尝澶х敓鎴愪箣闂寸殑绛夊緟鏃堕棿")]
    public float majorSpawnInterval = 5.0f;

    [Tooltip("灏忕敓鎴愰棿闅旓細涓€娆″ぇ鐢熸垚鍐呴儴锛岃繛缁敓鎴愬皬鎬殑闂撮殧")]
    public float minorSpawnInterval = 1.0f;

    [Tooltip("姣忔澶х敓鎴愬寘鍚嚑娆″皬鐢熸垚")]
    public int wavesPerMajor = 3;

    // --- 鐘舵€佹満 ---
    private BossState currentState;
    public Vector3 HoverAnchorPos { get; set; } // 鎮诞鐨勫熀鍑嗙偣

    // 鐘舵€佸疄渚?
    public StateEntrance stateEntrance;
    public StateIdle stateIdle;
    public StateSpawn stateSpawn;
    public StateBarrage stateBarrage;
    public StateWild stateWild;
    public StateLaser stateLaser;


    private float majorTimer;
    private bool isEntering = true;
    private bool isSpawningWave = false;

    private int brokenTurretCount = 0;
    private bool bodyShieldBroken = false;

    // 銆愭柊澧炪€戣褰曟偓娴殑涓績鐐逛綅缃?
    private Vector3 hoverAnchorPos;

    protected override void Awake()
    {
        base.Awake();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (!bodyPart) bodyPart = transform.FindDeepChild("aircraft_body")?.GetComponent<BossPart>();
        if (!leftWing) leftWing = transform.FindDeepChild("aircraftLeft")?.GetComponent<BossPart>();
        if (!rightWing) rightWing = transform.FindDeepChild("aircraftRight")?.GetComponent<BossPart>();

        if (!leftTurretPart) leftTurretPart = transform.FindDeepChild("canotower_left")?.GetComponent<BossPart>();
        if (!rightTurretPart) rightTurretPart = transform.FindDeepChild("canotower_right")?.GetComponent<BossPart>();

        if (!leftTurret) leftTurret = transform.FindDeepChild("canotower_left")?.GetComponent<BossTurret>();
        if (!rightTurret) rightTurret = transform.FindDeepChild("canotower_right")?.GetComponent<BossTurret>();

        // 鐩戝惉閮ㄤ綅鐮村潖锛堝彲閫夛紝鐢ㄤ簬鐘舵€佸垏鎹㈠垽鏂級
        if (leftWing) leftWing.OnPartBroken += OnWingBroken;
        if (rightWing) rightWing.OnPartBroken += OnWingBroken;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        activeMinions.Clear();

        if (leftTurretPart) { leftTurretPart.OnPartBroken -= OnTurretPartBroken; leftTurretPart.OnPartBroken += OnTurretPartBroken; }
        if (rightTurretPart) { rightTurretPart.OnPartBroken -= OnTurretPartBroken; rightTurretPart.OnPartBroken += OnTurretPartBroken; }

        brokenTurretCount = 0;
        bodyShieldBroken = false;
        if (laserBeamObj) laserBeamObj.SetActive(false);

        // 鍒濆鍖栫姸鎬?
        stateEntrance = new StateEntrance(this);
        stateIdle = new StateIdle(this);
        stateSpawn = new StateSpawn(this);
        stateBarrage = new StateBarrage(this);
        stateWild = new StateWild(this);
        stateLaser = new StateLaser(this);

        // 鍒濆鐘舵€侊細鍏ュ満
        ChangeState(stateEntrance);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        ChangeState(null);
    }

    private void Update()
    {
        if (isDead) return;
        currentState?.OnUpdate();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        currentState?.OnFixedUpdate();
    }

    public void ChangeState(BossState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();
    }


    protected override void MoveBehavior()
    {
        
    }

    public void PerformSmoothHover()
    {
        float targetX = HoverAnchorPos.x + Mathf.Cos(Time.time * xFreq) * xDist;
        float targetY = HoverAnchorPos.y + Mathf.Sin(Time.time * yFreq) * yDist;
        Vector2 targetPos = new Vector2(targetX, targetY);

        Vector2 nextPos = Vector2.SmoothDamp(transform.position, targetPos, ref CurrentVelocity, smoothTime, maxSpeed);

        rb.MovePosition(nextPos);
    }


    // --- 閮ㄤ綅鐮村潖鍥炶皟 ---
    private void OnWingBroken(BossPart part)
    {
        Debug.Log($"Boss Wing Broken: {part.name}");
        // 鍙互鍦ㄨ繖閲屾挱鏀剧壒瀹氱殑鏂鐗规晥
        BackgroundFXController.Instance.TriggerDistortion(part.transform.position);
    }

    private void OnTurretPartBroken(BossPart brokenPart)
    {
        brokenTurretCount++;

        bool isLeftBroken = (brokenPart == leftTurretPart);

        Debug.Log($"Turret broken: {(isLeftBroken ? "Left" : "Right")}, count: {brokenTurretCount}");

        if (brokenTurretCount == 1)
        {
            if (isLeftBroken && rightTurret != null)
            {
                rightTurret.SetWildMode(true);
            }
            else if (!isLeftBroken && leftTurret != null)
            {
                leftTurret.SetWildMode(true);
            }

            ChangeState(stateWild);
        }
        else if (brokenTurretCount >= 2)
        {
            rightTurret.SetWildMode(false);
            leftTurret.SetWildMode(false);

            EnterLaserMode();
        }
    }

    private void EnterLaserMode()
    {
        if (bodyPart != null && !bodyShieldBroken)
        {
            bodyPart.TakeDamage(99999);
            bodyShieldBroken = true;
        }

        ChangeState(stateLaser);
        Debug.Log("Boss 鑳哥敳鐮寸锛岃繘鍏ユ縺鍏夌粓鏋佹ā寮忥紒");

        //BackgroundFXController.Instance.TriggerDistortion(transform.position);
        BackgroundFXController.Instance.SwitchToTheme("Boss");
    }

    protected override void PlayHitEffect(Vector3 pos, Vector3 normal)
    {
        if (bodyRenderer != null)
        {
            // 鍋囪鎴戜滑鍦⊿hader閲屽畾涔変簡 "_HitFlashStrength"
            bodyRenderer.material.DOKill();
            bodyRenderer.material.SetFloat("_HitFlashStrength", 2f);
            bodyRenderer.material.DOFloat(0.1f, "_HitFlashStrength", 0.8f);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0), 0.1f);
        }

        if (hitParticlePrefab == null)
        {
            hitParticlePrefab = Resources.Load<GameObject>("ParticleSystem/PS_HitSparks");
        }

        if (hitParticlePrefab != null)
        {
            GameObject particleObj = ObjectPoolManager.Instance.Get(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            Timer.Register(1f, onComplete: () =>
            {
                ObjectPoolManager.Instance.Return(particleObj);
            });

            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;

                main.startColor = normalColor;

                ps.Play();
            }
        }
    }

    public GameObject GetRandomMinionPrefab()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return null;
        return minionPrefabs[Random.Range(0, minionPrefabs.Count)];
    }

    // --- 杈呭姪鏂规硶锛氱敓鎴愪竴鍙皬鎬?---
    public void SpawnSingleMinion(Transform spawnPoint)
    {
        activeMinions.RemoveAll(x => x == null || !x.activeInHierarchy);

        // 鐒跺悗鍐嶅垽鏂暟閲?
        if (activeMinions.Count >= maxMinions) return;

        // 妫€鏌ラ儴浣嶇牬鍧?
        if (spawnPoint == null || !spawnPoint.gameObject.activeInHierarchy) return;

        GameObject prefab = GetRandomMinionPrefab();
        if (prefab == null) return;

        GameObject minion = ObjectPoolManager.Instance.Get(prefab, spawnPoint.position, Quaternion.identity);
        activeMinions.Add(minion);

        var enemy = minion.GetComponent<EnemyBase>();
        if (enemy) enemy.OnSpawn();
    }

    public void CleanMinionList()
    {
        activeMinions.RemoveAll(x => x == null || !x.activeInHierarchy);
    }

    private void OnDrawGizmosSelected()
    {
        // 濡傛灉娌℃湁寮€鍚縺鍏夌浉鍏崇殑寮曠敤鎴栧弬鏁帮紝灏变笉鐢?
        if (laserWidth <= 0 || laserMaxDist <= 0) return;

        Gizmos.color = Color.red;

        // 1. 鑾峰彇瀵瑰簲 StateLaser 涓?BoxCast 鐨勫弬鏁?
        Vector3 startPos = transform.position; // 璧风偣
        Vector2 boxSize = new Vector2(laserWidth, 0.1f); // 鍒ゅ畾鐩掑ぇ灏?(StateLaser閲屽啓鐨勬槸0.1楂?
        Vector3 direction = Vector3.down; // 鏂瑰悜
        float distance = laserMaxDist; // 鏈€澶ц窛绂?

        // 涓轰簡婕旂ず瀹為檯鍑讳腑浣嶇疆锛屾垜浠彲浠ュ皾璇曡繘琛屼竴娆＄湡瀹炵殑灏勭嚎妫€娴?(浠呭湪缂栬緫鍣ㄦā寮忎笅)
        // 娉ㄦ剰锛氳繖鍙兘浼氱◢寰奖鍝嶇紪杈戝櫒鎬ц兘锛屼絾鑳界湅鍒板疄闄呮尅鍦ㄥ摢浜?
        /*
        RaycastHit2D hit = Physics2D.BoxCast(startPos, boxSize, 0f, direction, distance, laserHitLayer);
        if (hit.collider != null)
        {
            distance = hit.distance; // 濡傛灉鎵撲腑涓滆タ锛屽彧鐢诲埌鎵撲腑鐐?
            Gizmos.color = Color.yellow; // 鎵撲腑鏃跺彉榛?
        }
        */

        // 2. 缁樺埗璧风偣鐩掑瓙
        Gizmos.DrawWireCube(startPos, boxSize);

        // 3. 缁樺埗缁堢偣鐩掑瓙
        Vector3 endPos = startPos + direction * distance;
        Gizmos.DrawWireCube(endPos, boxSize);

        // 4. 缁樺埗杩炴帴绾?(妯℃嫙鎵繃鐨勫尯鍩?
        Vector3 halfWidth = Vector3.right * (laserWidth * 0.5f);
        Gizmos.DrawLine(startPos - halfWidth, endPos - halfWidth); // 宸﹁竟缂?
        Gizmos.DrawLine(startPos + halfWidth, endPos + halfWidth); // 鍙宠竟缂?

        // 5. 缁樺埗涓績绾?
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawLine(startPos, endPos);
    }

    //void EntranceMovement()
    //{
    //    Vector3 currentPos = transform.position;
    //    Vector3 target = new Vector3(targetEntryPosition.x, targetEntryPosition.y, currentPos.z);
    //    Vector3 nextPos = Vector3.MoveTowards(currentPos, target, enterSpeed * Time.deltaTime);
    //    rb.MovePosition(nextPos);

    //    if (Vector3.Distance(currentPos, target) < 0.05f)
    //    {
    //        FinishEntrance();
    //    }
    //}

    //void FinishEntrance()
    //{
    //    isEntering = false;
    //    rb.velocity = Vector2.zero;

    //    hoverAnchorPos = transform.position;

    //    majorTimer = majorSpawnInterval;
    //}

    //void HoverMovement()
    //{
    //    if (isDead) return;

    //    // 浣跨敤 Sin 鍑芥暟璁＄畻褰撳墠鐨?Y 杞村亸绉婚噺
    //    // Time.time * hoverSpeed 鎺у埗棰戠巼
    //    // * hoverDistance 鎺у埗骞呭害
    //    float newY = hoverAnchorPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;

    //    // 淇濇寔 X 杞翠綅缃笉鍙橈紙鍗?hoverAnchorPos.x锛夛紝鍙敼鍙?Y
    //    Vector2 targetPos = new Vector2(hoverAnchorPos.x, newY);

    //    // 浣跨敤 MovePosition 绉诲姩鍒氫綋
    //    rb.MovePosition(targetPos);
    //}

    //// --- 鐢熸垚閫昏緫鎺у埗 ---
    //void HandleSpawningTimer()
    //{
    //    if (isSpawningWave) return;

    //    activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

    //    if (activeMinions.Count >= maxMinions)
    //    {
    //        return;
    //    }

    //    majorTimer -= Time.deltaTime;

    //    if (majorTimer <= 0)
    //    {
    //        StartCoroutine(MajorSpawnRoutine());
    //    }
    //}

    //IEnumerator MajorSpawnRoutine()
    //{
    //    isSpawningWave = true;

    //    for (int i = 0; i < wavesPerMajor; i++)
    //    {
    //        SpawnDrifters();

    //        if (i < wavesPerMajor - 1)
    //        {
    //            yield return new WaitForSeconds(minorSpawnInterval);
    //        }
    //    }

    //    isSpawningWave = false;
    //    majorTimer = majorSpawnInterval;
    //}

    //void SpawnDrifters()
    //{
    //    if (drifterPrefab == null) return;

    //    activeMinions.RemoveAll(item => item == null || !item.activeInHierarchy);

    //    // 妫€鏌ョ繀鑶€鏄惁瀛樻椿 + 鏁伴噺闄愬埗
    //    if (leftSpawnPoint != null && leftSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
    //        CreateMinion(leftSpawnPoint.position);

    //    if (rightSpawnPoint != null && rightSpawnPoint.gameObject.activeInHierarchy && activeMinions.Count < maxMinions)
    //        CreateMinion(rightSpawnPoint.position);
    //}

    //void CreateMinion(Vector3 spawnPos)
    //{
    //    GameObject minionObj = Instantiate(drifterPrefab, spawnPos, Quaternion.identity);
    //    activeMinions.Add(minionObj);

    //    EnemyDrifter drifterScript = minionObj.GetComponent<EnemyDrifter>();
    //    if (drifterScript != null)
    //    {
    //        drifterScript.OnSpawn();
    //    }
    //}
}

public static class TransformDeepChildExtension
{
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName) return c;
            foreach (Transform t in c) queue.Enqueue(t);
        }
        return null;
    }
}
