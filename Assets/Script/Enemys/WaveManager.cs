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
    Right   // 右侧
}

[System.Serializable]
public class WaveGroup
{
    [Header("配置")]
    public GameObject enemyPrefab; // 怪物的预制体
    public int count = 5;          // 数量
    public float spawnRate = 1f;   // 间隔时间(秒)
    public bool isParallel = false;
    public SpawnDirection direction = SpawnDirection.Random;

    [Header("延迟")]
    public float delayBeforeStart = 0f; // 这组怪开始刷之前的等待时间
}

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave 1";
    public List<WaveGroup> groups; // 这一波包含的所有怪组
}


public class WaveManager : MonoSingleton<WaveManager>
{
    [Header("关卡配置")]
    public List<WaveData> waves; // 所有波次的数据

    [Header("设置")]
    public float timeBetweenWaves = 3f; // 波次之间的休息时间
    public float spawnPadding = 1.5f;   // 刷怪点距离屏幕边缘的额外距离

    // --- UI需要用的 ---
    // 参数1: 当前波次索引, 参数2: 波次名字
    public Action<int, string> OnWaveIncoming;
    public Action OnAllWavesCleared; // 通关事件

    // --- 运行时状态 ---
    private int currentWaveIndex = 0;
    private int totalEnemiesAlive = 0;
    private int activeSpawnerCount = 0;
    private bool isSpawning => activeSpawnerCount > 0;

    private Camera mainCam;
    private float camHeight;
    private float camWidth;

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

    // --- 核心游戏循环 ---
    //在GameManager的MainGameState状态里调用此协程启动游戏循环
    public IEnumerator GameLoopRoutine()
    {
        // 稍微等待一下游戏初始化
        yield return new WaitForSeconds(1f);

        while (currentWaveIndex < waves.Count)
        {
            WaveData currentWave = waves[currentWaveIndex];

            //触发 UI 弹窗事件
            OnWaveIncoming?.Invoke(currentWaveIndex + 1, currentWave.waveName);
            Debug.Log($"<color=cyan>--- {currentWave.waveName} 即将开始 ---</color>");

            //等待 UI 动画展示时间
            yield return new WaitForSeconds(timeBetweenWaves);

            //开始执行这一波的刷怪逻辑
            yield return StartCoroutine(SpawnWaveRoutine(currentWave));

            //每帧检查有没有怪，还有就继续循环等下一帧
            while (totalEnemiesAlive > 0 || isSpawning)
            {
                yield return null;
            }

            Debug.Log($"<color=green>--- {currentWave.waveName} 完成 ---</color>");

            currentWaveIndex++;
        }

        // 所有波次结束
        OnAllWavesCleared?.Invoke();
        Debug.Log("所有波次已清空！胜利！");
    }

    // --- 刷怪逻辑 ---
    IEnumerator SpawnWaveRoutine(WaveData wave)
    {
        for (int i = 0; i < wave.groups.Count; i++)
        {
            var currentGroup = wave.groups[i];

            // 启动当前组的刷怪协程
            Coroutine groupRoutine = StartCoroutine(SpawnGroupRoutine(currentGroup));

            // 检查下一组是否是并行
            bool nextIsParallel = false;
            if (i + 1 < wave.groups.Count)
            {
                nextIsParallel = wave.groups[i + 1].isParallel;
            }

            if (!nextIsParallel)
            {
                yield return groupRoutine;
            }
        }

    }

    IEnumerator SpawnGroupRoutine(WaveGroup group)
    {
        activeSpawnerCount++; // 标记有一个刷怪进程开始

        // 处理组延迟
        if (group.delayBeforeStart > 0)
            yield return new WaitForSeconds(group.delayBeforeStart);

        for (int i = 0; i < group.count; i++)
        {
            SpawnEnemy(group.enemyPrefab, group.direction);
            totalEnemiesAlive++;
            yield return new WaitForSeconds(group.spawnRate);
        }

        activeSpawnerCount--; // 标记这个刷怪进程结束
    }

    void SpawnEnemy(GameObject prefab, SpawnDirection dir)
    {
        Vector3 spawnPos = GetSpawnPosition(dir);

        ObjectPoolManager.Instance.Get(prefab, spawnPos, Quaternion.identity);
    }

    Vector3 GetSpawnPosition(SpawnDirection dir)
    {
        Vector3 pos = Vector3.zero;
        float xLimit = camWidth / 2f + spawnPadding;
        float yLimit = camHeight / 2f + spawnPadding;

        if (dir == SpawnDirection.Random)
        {
            dir = (SpawnDirection)Random.Range(1, 5); // 1-4 是上下左右
        }

        switch (dir)
        {
            case SpawnDirection.Top:
                pos = new Vector3(Random.Range(-xLimit, xLimit), yLimit, 0);
                break;
            case SpawnDirection.Bottom:
                pos = new Vector3(Random.Range(-xLimit, xLimit), -yLimit, 0);
                break;
            case SpawnDirection.Left:
                pos = new Vector3(-xLimit, Random.Range(-yLimit, yLimit), 0);
                break;
            case SpawnDirection.Right:
                pos = new Vector3(xLimit, Random.Range(-yLimit, yLimit), 0);
                break;
        }

        return pos;
    }

    // --- 外部调用接口：怪物死亡时调用 ---
    public void RegisterEnemyDeath()
    {
        totalEnemiesAlive--;
        if (totalEnemiesAlive < 0) totalEnemiesAlive = 0;
    }
}
