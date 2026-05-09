using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnDirector
{
    private sealed class TrackedEnemy
    {
        public EnemyBase enemy;
        public float threat;
    }

    private readonly EnemySpawnPointProvider spawnPointProvider = new();
    private readonly List<TrackedEnemy> activeEnemies = new();
    private readonly List<EnemySpawnEntry> candidateBuffer = new();

    private BattleThemeConfig currentTheme;
    private ThemeLoopEnemyPlan currentPlan;
    private CombatLoopGlobalConfig loopConfig;
    private int currentThemeIndex;
    private int currentLoopIndex;
    private float spawnBudget;
    private bool isRunning;

    public bool IsRunning => isRunning;
    public int ActiveEnemyCount => activeEnemies.Count;
    public float CurrentActiveThreat { get; private set; }
    public string LastMissingPrefabResourcePath { get; private set; }

    public void BeginLoop(
        BattleThemeConfig theme,
        CombatLoopGlobalConfig config,
        int themeIndex,
        int loopIndex)
    {
        currentTheme = theme;
        loopConfig = config;
        currentThemeIndex = themeIndex;
        currentLoopIndex = loopIndex;
        currentPlan = ResolvePlan(theme, loopIndex);
        spawnBudget = 0f;
        isRunning = true;
        LastMissingPrefabResourcePath = string.Empty;
        CleanupTrackedEnemies();
    }

    public void StopLoop()
    {
        isRunning = false;
        spawnBudget = 0f;
    }

    public void Tick(float deltaTime, float normalizedTime)
    {
        if (!isRunning || deltaTime <= 0f || currentTheme == null || loopConfig == null)
            return;

        CleanupTrackedEnemies();

        float loopScale = 1f + Mathf.Max(0, currentLoopIndex) * loopConfig.loopDifficultyStep;
        float themeScale = 1f + Mathf.Max(0, currentThemeIndex) * loopConfig.themeDifficultyStep;
        float themeDifficulty = Mathf.Max(0.1f, currentTheme.difficultyMultiplier);
        float spawnPerSecond = loopConfig.spawnBudgetPerSecondCurve != null
            ? loopConfig.spawnBudgetPerSecondCurve.Evaluate(Mathf.Clamp01(normalizedTime))
            : 1f;
        float maxThreat = Mathf.Max(1f, loopConfig.baseActiveThreatCap * loopScale * themeScale * themeDifficulty);

        spawnBudget += spawnPerSecond * deltaTime * loopScale * themeScale;
        CurrentActiveThreat = CalculateActiveThreat();

        if (CurrentActiveThreat >= maxThreat)
            return;

        int attempts = 0;
        while (attempts < Mathf.Max(1, loopConfig.maxSpawnAttemptsPerTick))
        {
            attempts++;
            var entry = SelectSpawnEntry(spawnBudget);
            if (entry == null)
                break;

            GameObject prefab = entry.ResolvePrefab();
            if (prefab == null)
            {
                LastMissingPrefabResourcePath = entry.enemyResourcePath;
                Debug.LogWarning($"[EnemySpawnDirector] Missing enemy prefab for enemyId={entry.enemyId}, resourcePath={entry.enemyResourcePath}");
                continue;
            }

            if (!spawnPointProvider.TryGetSpawnPoint(
                    GameMgr.Instance.Player != null ? GameMgr.Instance.Player.CurrentPlayerObj?.transform : null,
                    loopConfig.spawnInnerPadding,
                    loopConfig.spawnOuterPadding,
                    out var spawnPoint))
            {
                break;
            }

            var runtimeData = EnemyScalingResolver.Build(entry, loopConfig, currentTheme, currentThemeIndex, currentLoopIndex, normalizedTime);
            SpawnEnemy(prefab, entry, runtimeData, spawnPoint);
            spawnBudget -= Mathf.Max(0.01f, entry.baseSpawnCost);
            CurrentActiveThreat = CalculateActiveThreat();

            if (CurrentActiveThreat >= maxThreat)
                break;
        }
    }

    public void DespawnAllTrackedEnemies()
    {
        CleanupTrackedEnemies();

        foreach (var tracked in activeEnemies)
        {
            if (tracked?.enemy == null)
                continue;

            ObjectPoolManager.Instance.Return(tracked.enemy.gameObject);
        }

        activeEnemies.Clear();
        CurrentActiveThreat = 0f;
    }

    public void Reset()
    {
        StopLoop();
        activeEnemies.Clear();
        candidateBuffer.Clear();
        currentTheme = null;
        currentPlan = null;
        loopConfig = null;
        CurrentActiveThreat = 0f;
        LastMissingPrefabResourcePath = string.Empty;
    }

    private ThemeLoopEnemyPlan ResolvePlan(BattleThemeConfig theme, int loopIndex)
    {
        if (theme?.loopEnemyPlans == null)
            return null;

        foreach (var plan in theme.loopEnemyPlans)
        {
            if (plan != null && plan.loopIndex == loopIndex)
                return plan;
        }

        return null;
    }

    private EnemySpawnEntry SelectSpawnEntry(float availableBudget)
    {
        candidateBuffer.Clear();
        if (currentTheme?.enemyPool == null)
            return null;

        foreach (var entry in currentTheme.enemyPool)
        {
            if (entry == null)
                continue;

            if (entry.minLoopIndex > currentLoopIndex)
                continue;

            if (entry.baseSpawnCost > availableBudget)
                continue;

            if (currentPlan != null && currentPlan.weightedEnemies != null && currentPlan.weightedEnemies.Count > 0)
            {
                if (GetPlanWeight(entry.enemyId) <= 0f)
                    continue;
            }

            candidateBuffer.Add(entry);
        }

        if (candidateBuffer.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < candidateBuffer.Count; i++)
            totalWeight += GetEntryWeight(candidateBuffer[i]);

        if (totalWeight <= 0f)
            return candidateBuffer[Random.Range(0, candidateBuffer.Count)];

        float roll = Random.Range(0f, totalWeight);
        float cursor = 0f;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            var entry = candidateBuffer[i];
            cursor += GetEntryWeight(entry);
            if (roll <= cursor)
                return entry;
        }

        return candidateBuffer[candidateBuffer.Count - 1];
    }

    private float GetEntryWeight(EnemySpawnEntry entry)
    {
        if (currentPlan == null)
            return 1f;

        if (currentPlan.weightedEnemies != null && currentPlan.weightedEnemies.Count > 0)
            return Mathf.Max(0f, GetPlanWeight(entry.enemyId));

        if (IsEnemyUnlockedInPlan(entry.enemyId))
            return 1f;

        return 0f;
    }

    private float GetPlanWeight(string enemyId)
    {
        if (currentPlan?.weightedEnemies == null || string.IsNullOrWhiteSpace(enemyId))
            return 0f;

        foreach (var weightedEnemy in currentPlan.weightedEnemies)
        {
            if (weightedEnemy != null && string.Equals(weightedEnemy.enemyId, enemyId, System.StringComparison.OrdinalIgnoreCase))
                return weightedEnemy.weight;
        }

        return 0f;
    }

    private bool IsEnemyUnlockedInPlan(string enemyId)
    {
        if (currentPlan?.unlockedEnemyIds == null || string.IsNullOrWhiteSpace(enemyId))
            return false;

        foreach (var unlockedEnemyId in currentPlan.unlockedEnemyIds)
        {
            if (string.Equals(unlockedEnemyId, enemyId, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void SpawnEnemy(GameObject prefab, EnemySpawnEntry entry, EnemyRuntimeSpawnData runtimeData, Vector3 spawnPoint)
    {
        GameObject enemyObject = ObjectPoolManager.Instance.Get(prefab, spawnPoint, Quaternion.identity);
        var enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogWarning($"[EnemySpawnDirector] Spawned prefab {prefab.name} has no EnemyBase.");
            return;
        }

        ApplyRuntimeScaling(enemyObject, enemy, entry, runtimeData);
        activeEnemies.Add(new TrackedEnemy
        {
            enemy = enemy,
            threat = Mathf.Max(0.1f, entry.baseThreat * runtimeData.threatMultiplier)
        });
    }

    private static void ApplyRuntimeScaling(GameObject enemyObject, EnemyBase enemy, EnemySpawnEntry entry, EnemyRuntimeSpawnData runtimeData)
    {
        var receiver = enemyObject.GetComponent<IEnemySpawnDataReceiver>();
        if (receiver != null)
            receiver.ApplySpawnData(runtimeData);

        enemy.maxHp = Mathf.Max(1f, enemy.maxHp * runtimeData.hpMultiplier);
        enemy.currentHp = enemy.maxHp;
        enemy.moveSpeed = Mathf.Max(0.1f, enemy.moveSpeed * runtimeData.moveSpeedMultiplier);
        enemy.scoreValue = Mathf.Max(1, Mathf.RoundToInt(entry.baseScore * runtimeData.scoreMultiplier));
    }

    private void CleanupTrackedEnemies()
    {
        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            var tracked = activeEnemies[index];
            if (tracked == null || tracked.enemy == null || !tracked.enemy.gameObject.activeInHierarchy)
                activeEnemies.RemoveAt(index);
        }

        CurrentActiveThreat = CalculateActiveThreat();
    }

    private float CalculateActiveThreat()
    {
        float total = 0f;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
                total += activeEnemies[i].threat;
        }

        return total;
    }
}
