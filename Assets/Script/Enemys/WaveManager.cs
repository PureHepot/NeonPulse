using Sirenix.Reflection.Editor;
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
    Right,   // 右侧
    TopCenter //
}
public class WaveManager : MonoSingleton<WaveManager>
{
    [Header("关卡配置")]
    public WavesData wavesData; // 所有波次的数据

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
    private int speedLevel = 1;
    private bool isSpawning => activeSpawnerCount > 0;

    private Camera mainCam;
    private float camHeight;
    private float camWidth;

    private Coroutine currentWaveSpawnCoroutine;

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

        while (currentWaveIndex < wavesData.allWaves.Count)
        {
            WaveData currentWave = wavesData.allWaves[currentWaveIndex];

            //触发 UI 弹窗事件
            if (currentWave.waveName == "Speed!")
            {
                speedLevel++;
            }
            OnWaveIncoming?.Invoke(currentWaveIndex + 1, currentWave.waveName);
            Debug.Log($"<color=cyan>--- {currentWave.waveName} 即将开始 ---</color>");

            //波次休息时间
            yield return new WaitForSeconds(timeBetweenWaves);

            currentWaveSpawnCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));

            float waveTimer = 0f;
            bool isTimedWave = currentWave.waveDuration > 0;


            //每帧检查有没有怪，还有就继续循环等下一帧
            while (true)
            {
                // 1. 胜利条件：怪杀光了 且 不再刷怪了
                bool allClear = totalEnemiesAlive <= 0 && !isSpawning;

                // 2. 超时条件：是限时波次 且 时间到了
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

                    activeSpawnerCount = 0;

                    break;
                }

                waveTimer += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"<color=green>--- {currentWave.waveName} 完成 ---</color>");

            currentWaveIndex++;
            if(speedLevel == 1)
                BackgroundFXController.Instance.SwitchToTheme($"Normal_{currentWaveIndex % 5}");
            else if(speedLevel == 2)
                BackgroundFXController.Instance.SwitchToTheme($"Fast_{currentWaveIndex % 5}");
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

        // 计算屏幕的一半宽高
        float halfHeight = camHeight / 2f;
        float halfWidth = camWidth / 2f;

        // 原有的 limit 是给屏幕外生成用的
        float xLimit = halfWidth + spawnPadding;
        float yLimit = halfHeight + spawnPadding;

        if (dir == SpawnDirection.Random)
        {
            dir = (SpawnDirection)Random.Range(1, 5);
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

            // 【修改处】
            case SpawnDirection.TopCenter:
                // X轴居中 = 0
                // Y轴 = 屏幕顶边缘(halfHeight) - 偏移量(比如 1.5f 或 2.0f)
                // 这样怪物就会刚好出现在屏幕内部的上方，而不是屏幕外
                pos = new Vector3(0, halfHeight - 1.5f, 0);
                break;
        }

        return pos;
    }

    public void RegisterEnemyDeath()
    {
        totalEnemiesAlive--;
        if (totalEnemiesAlive < 0)
        {
            totalEnemiesAlive = 0;
        }
    }
}
