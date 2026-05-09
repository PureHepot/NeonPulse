using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleThemeConfig", menuName = "Game/InRun/Battle Theme")]
public class BattleThemeConfig : ScriptableObject
{
    public string themeId;
    public string displayName;
    public SOVisualThemePresets backgroundPreset;

    public List<EnemySpawnEntry> enemyPool = new();
    public List<ThemeLoopEnemyPlan> loopEnemyPlans = new();

    public float difficultyMultiplier = 1f;
}

[Serializable]
public class ThemeLoopEnemyPlan
{
    public int loopIndex;
    public List<string> unlockedEnemyIds = new();
    public List<WeightedEnemyId> weightedEnemies = new();
}

[Serializable]
public class WeightedEnemyId
{
    public string enemyId;
    public float weight = 1f;
}

[Serializable]
public class EnemySpawnEntry
{
    public string enemyId;
    public GameObject enemyPrefab;
    public int baseScore = 10;
    public float baseSpawnCost = 1f;
    public float baseThreat = 1f;
    public int minLoopIndex;
    public List<string> tags = new();
}
