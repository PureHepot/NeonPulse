using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleThemeConfig", menuName = "Game/InRun/Battle Theme")]
public class BattleThemeConfig : ScriptableObject
{
    public string themeId;
    public string displayName;
    public SOVisualThemePresets backgroundPreset;

    public List<EnemySpawnEntry> enemyPool = new();
    public List<ThemeLoopEnemyPlan> loopEnemyPlans = new();

    public BossEncounterConfig bossEncounter;
    public RewardPoolConfig loopRewardPool;
    public RewardPoolConfig bossRewardPool;
    public ShopCatalogConfig shopCatalog;

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
    public string enemyResourcePath;
    public int baseScore = 10;
    public float baseSpawnCost = 1f;
    public float baseThreat = 1f;
    public int minLoopIndex;
    public List<string> tags = new();

    public GameObject ResolvePrefab()
    {
        if (enemyPrefab != null)
            return enemyPrefab;

        if (string.IsNullOrWhiteSpace(enemyResourcePath))
            return null;

        enemyPrefab = Resources.Load<GameObject>(NormalizeResourcePath(enemyResourcePath));
        return enemyPrefab;
    }

    private static string NormalizeResourcePath(string resourcePath)
    {
        string normalized = resourcePath.Trim().Replace('\\', '/');
        const string resourcesMarker = "/Resources/";
        int resourcesIndex = normalized.IndexOf(resourcesMarker, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
            normalized = normalized.Substring(resourcesIndex + resourcesMarker.Length);

        return Path.ChangeExtension(normalized, null)?.Trim('/') ?? string.Empty;
    }
}
