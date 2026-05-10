using UnityEngine;

public struct EnemyRuntimeSpawnData
{
    public string enemyId;
    public float hpMultiplier;
    public float moveSpeedMultiplier;
    public float scoreMultiplier;
    public float threatMultiplier;
    public int themeIndex;
    public int loopIndex;
    public float normalizedTime;
}

public interface IEnemySpawnDataReceiver
{
    void ApplySpawnData(EnemyRuntimeSpawnData data);
}
