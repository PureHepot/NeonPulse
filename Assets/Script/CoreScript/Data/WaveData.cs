using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public string waveName = "Wave ";
    public List<WaveGroup> groups; // 这一波包含的所有怪组
    public float waveDuration;
}

[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config")]
public class WavesData : ScriptableObject
{
    public List<WaveData> allWaves;
}
