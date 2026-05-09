using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnDirection
{
    Random,
    Top,
    Bottom,
    Left,
    Right,
    TopCenter
}

[Serializable]
public class WaveGroup
{
    public List<int> enemyIndex = new();
    public List<float> spawnRate = new();
    public float groupDuration;
    public SpawnDirection direction = SpawnDirection.Random;
}

[Serializable]
public class WaveData
{
    public string waveName = "Wave";
    public List<GameObject> enemies = new();
    public List<WaveGroup> groups = new();
    public float waveDuration;
}

public class WaveManager : MonoSingleton<WaveManager>
{
    public Action<int, string> OnWaveIncoming;
    public Action OnAllWavesCleared;

    public int currentWaveIndex;
    public readonly HashSet<EnemyBase> activeEnemies = new();

    public void InitFromSaveData()
    {
        var run = DataManager.Instance.Run;
        currentWaveIndex = run != null ? run.wave.currentWaveIndex : 0;
    }

    public void SyncToSaveData()
    {
        var run = DataManager.Instance.Run;
        if (run != null)
            run.wave.currentWaveIndex = currentWaveIndex;
    }

    public IEnumerator GameLoopRoutine()
    {
        yield break;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy != null)
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy != null)
            activeEnemies.Remove(enemy);
    }

    public void RegisterEnemyDeath()
    {
    }
}
