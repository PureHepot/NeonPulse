using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SpawnDirection
{
    Random, // 随机全方向
    Top,    // 上方
    Bottom, // 下方
    Left,   // 左侧
    Right,  // 右侧
    TopCenter
}

[System.Serializable]
public class WaveGroup
{
    [Header("配置")]
    public List<int> enemyIndex; // 怪物的预制体
    public List<float> spawnRate;   // 间隔时间(秒)
    public float groupDuration;
    public SpawnDirection direction = SpawnDirection.Random;
}

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave ";
    public List<GameObject> enemies;
    public List<WaveGroup> groups;
    public float waveDuration;
}

public class WaveManager : MonoSingleton<WaveManager>
{
    [Header("关卡配置")]
    public WavesData wavesData; // 所有波次的数据

    [Header("设置")]
    public float timeBetweenWaves = 3f; // 波次之间的休息时间
    public float spawnPadding = 1.5f;   // 刷怪点距离屏幕边缘的额外距离

    // --- UI需要用的 ---
    public Action<int, string> OnWaveIncoming;
    public Action OnAllWavesCleared; // 通关事件

    // --- 运行时状态 ---
    public int currentWaveIndex = 0;
    private int totalEnemiesAlive = 0;
    private int activeSpawnerCount = 0;
    public HashSet<EnemyBase> activeEnemies = new HashSet<EnemyBase>();
    private int speedLevel = 1;
    private bool isSpawning => activeSpawnerCount > 0;

    private Camera mainCam;
    private float camHeight;
    private float camWidth;

    private Coroutine currentWaveSpawnCoroutine;
    private List<Coroutine> runningGroupRoutines = new List<Coroutine>();

    [Tooltip("当玩家距离墙壁多少米内时，触发避让逻辑")]
    public float wallProximityCheck = 0.5f;
    public float playerSafetyRadius = 0.5f;

    private void Start()
    {
        mainCam = Camera.main;
        UpdateCameraBounds();
    }

    void UpdateCameraBounds()
    {
        camHeight = 2f * mainCam.orthographicSize;
        camWidth = camHeight * mainCam.aspect;
    }

    public void InitFromSaveData()
    {
        var run = DataManager.Instance.Run;
        if (run == null) return;
        currentWaveIndex = run.wave.currentWaveIndex;
    }

    public void SyncToSaveData()
    {
        var run = DataManager.Instance.Run;
        if (run == null) return;
        run.wave.currentWaveIndex = currentWaveIndex;
    }

    // --- 核心游戏循环 ---
    public IEnumerator GameLoopRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (currentWaveIndex < wavesData.allWaves.Count)
        {
            WaveData currentWave = wavesData.allWaves[currentWaveIndex];

            if (currentWave.waveName == "Survive")
            {
                BackgroundFXController.Instance.SwitchToTheme($"Fast_{currentWaveIndex % 5}");
            }
            else if (currentWave.waveName == "AirCraft" || currentWave.waveName == "Singer")
            {
                BackgroundFXController.Instance.SwitchToTheme($"Boss");
            }
            else
            {
                BackgroundFXController.Instance.SwitchToTheme($"Normal_{currentWaveIndex % 5}");
            }
            OnWaveIncoming?.Invoke(currentWaveIndex + 1, currentWave.waveName);
            Debug.Log($"<color=cyan>--- {currentWave.waveName} 即将开始 ---</color>");

            yield return new WaitForSeconds(timeBetweenWaves);

            runningGroupRoutines.Clear();
            currentWaveSpawnCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));

            float waveTimer = 0f;
            bool isTimedWave = currentWave.waveDuration > 0;

            while (true)
            {
                bool allClear = activeEnemies.Count == 0 && !isSpawning;
                bool timeUp = isTimedWave && waveTimer >= currentWave.waveDuration;

                if (allClear)
                {
                    Debug.Log("波次结束：所有敌人已清除");
                    break;
                }

                if (timeUp)
                {
                    Debug.Log("波次结束：时间耗尽，强制进入下一波");
                    if (currentWaveSpawnCoroutine != null)
                        StopCoroutine(currentWaveSpawnCoroutine);

                    foreach (var routine in runningGroupRoutines)
                    {
                        if (routine != null) StopCoroutine(routine);
                    }
                    runningGroupRoutines.Clear();
                    activeSpawnerCount = 0;
                    ClearAllEnemies();
                    break;
                }

                waveTimer += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"<color=green>--- {currentWave.waveName} 完成 ---</color>");

            currentWaveIndex++;
            SyncToSaveData();
        }

        // 修改点：循环结束后，不再直接调用胜利 UI，而是等待 Boss 死亡来触发。
        Debug.Log("所有常规波次已生成完毕，等待击败最终 Boss...");
    }

    // ==========================================
    // --- 新增：专门用于触发最终胜利的 API ---
    // ==========================================
    public void TriggerVictory()
    {
        OnAllWavesCleared?.Invoke();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Open<EndUI>();
        }

        // 可选：清理场上残余的其他小怪
        ClearAllEnemies();

        Debug.Log("<color=yellow>最终 Boss 已被击败！通关！</color>");
    }

    // --- 刷怪逻辑 ---
    IEnumerator SpawnWaveRoutine(WaveData wave)
    {
        for (int i = 0; i < wave.groups.Count; i++)
        {
            var currentGroup = wave.groups[i];
            for (int j = 0; j < currentGroup.enemyIndex.Count; j++)
            {
                runningGroupRoutines.Add(StartCoroutine(SpawnGroupRoutine(wave, currentGroup, j)));
            }

            foreach (var routine in runningGroupRoutines)
            {
                if (routine != null) yield return routine;
            }
            runningGroupRoutines.Clear();
        }
    }

    IEnumerator SpawnGroupRoutine(WaveData wave, WaveGroup group, int index)
    {
        activeSpawnerCount++;

        float timer = 0;
        while (timer < group.groupDuration)
        {
            SpawnEnemy(wave.enemies[group.enemyIndex[index]], group.direction);
            yield return new WaitForSeconds(group.spawnRate[index]);
            timer += group.spawnRate[index];
        }

        activeSpawnerCount--;
    }

    void SpawnEnemy(GameObject prefab, SpawnDirection dir)
    {
        Vector3 spawnPos = GetSpawnPosition(dir);
        ObjectPoolManager.Instance.Get(prefab, spawnPos, Quaternion.identity);
    }

    Vector3 GetSpawnPosition(SpawnDirection dir)
    {
        Vector3 pos = Vector3.zero;
        float halfHeight = mainCam.orthographicSize;
        float halfWidth = halfHeight * mainCam.aspect;

        float xLimit = halfWidth + spawnPadding;
        float yLimit = halfHeight + spawnPadding;

        Vector3 playerPos = Vector3.zero;
        bool playerFound = false;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerPos = p.transform.position;
            playerFound = true;
        }

        if (dir == SpawnDirection.Random)
        {
            dir = (SpawnDirection)Random.Range(1, 5);
        }

        switch (dir)
        {
            case SpawnDirection.Top:
                if (playerFound && Mathf.Abs(playerPos.y - yLimit) < wallProximityCheck)
                {
                    float safeX = GetSafeRandomCoord(-xLimit, xLimit, playerPos.x, playerSafetyRadius);
                    pos = new Vector3(safeX, yLimit, 0);
                }
                else pos = new Vector3(Random.Range(-xLimit, xLimit), yLimit, 0);
                break;

            case SpawnDirection.Bottom:
                if (playerFound && Mathf.Abs(playerPos.y - (-yLimit)) < wallProximityCheck)
                {
                    float safeX = GetSafeRandomCoord(-xLimit, xLimit, playerPos.x, playerSafetyRadius);
                    pos = new Vector3(safeX, -yLimit, 0);
                }
                else pos = new Vector3(Random.Range(-xLimit, xLimit), -yLimit, 0);
                break;

            case SpawnDirection.Left:
                if (playerFound && Mathf.Abs(playerPos.x - (-xLimit)) < wallProximityCheck)
                {
                    float safeY = GetSafeRandomCoord(-yLimit, yLimit, playerPos.y, playerSafetyRadius);
                    pos = new Vector3(-xLimit, safeY, 0);
                }
                else pos = new Vector3(-xLimit, Random.Range(-yLimit, yLimit), 0);
                break;

            case SpawnDirection.Right:
                if (playerFound && Mathf.Abs(playerPos.x - xLimit) < wallProximityCheck)
                {
                    float safeY = GetSafeRandomCoord(-yLimit, yLimit, playerPos.y, playerSafetyRadius);
                    pos = new Vector3(xLimit, safeY, 0);
                }
                else pos = new Vector3(xLimit, Random.Range(-yLimit, yLimit), 0);
                break;

            case SpawnDirection.TopCenter:
                pos = new Vector3(0, halfHeight - 1.5f, 0);
                break;
        }
        return pos;
    }

    private float GetSafeRandomCoord(float min, float max, float avoidCenter, float radius)
    {
        float avoidMin = avoidCenter - radius;
        float avoidMax = avoidCenter + radius;

        bool seg1Valid = avoidMin > min;
        bool seg2Valid = avoidMax < max;

        if (seg1Valid && seg2Valid)
        {
            float len1 = avoidMin - min;
            float len2 = max - avoidMax;

            if (Random.value < (len1 / (len1 + len2))) return Random.Range(min, avoidMin);
            else return Random.Range(avoidMax, max);
        }
        else if (seg1Valid) return Random.Range(min, avoidMin);
        else if (seg2Valid) return Random.Range(avoidMax, max);
        else return Random.Range(min, max);
    }

    private void ClearAllEnemies()
    {
        List<EnemyBase> enemiesToClear = new List<EnemyBase>(activeEnemies);
        foreach (var enemy in enemiesToClear)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                enemy.TakeDamage(99999);
        }
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    public void RegisterEnemyDeath()
    {
        totalEnemiesAlive--;
        if (totalEnemiesAlive < 0) totalEnemiesAlive = 0;
    }
}
