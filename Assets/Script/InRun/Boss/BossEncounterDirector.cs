using System.Collections.Generic;
using UnityEngine;

public class BossEncounterDirector
{
    private const string BossConfigResourceFolder = "Configs/Boss";

    private readonly BossArenaLimiter arenaLimiter = new();
    private readonly List<BossEncounterConfig> bossSequence = new();
    private GameObject activeBossObject;
    private MonoBase activeBoss;
    private EnemyBase activeBossEnemy;
    private BossEncounterConfig activeConfig;
    private BossEncounterConfig pendingConfig;
    private PlayerController activePlayer;
    private int nextBossSequenceIndex;

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    public string PendingBossName => ResolveBossDisplayName(pendingConfig);
    public string CurrentBossName => ResolveBossDisplayName(activeConfig);
    public MonoBase ActiveBoss => activeBoss;
    public EnemyBase ActiveBossEnemy => activeBossEnemy;

    public string PrepareEncounter(BattleThemeConfig theme)
    {
        pendingConfig ??= ResolveEncounterConfig(theme);
        return PendingBossName;
    }

    public void BeginEncounter(BattleThemeConfig theme, int themeIndex)
    {
        var preparedConfig = pendingConfig;
        CleanupEncounter();
        IsComplete = false;

        activeConfig = preparedConfig ?? ResolveEncounterConfig(theme);
        pendingConfig = null;
        arenaLimiter.Activate(activeConfig != null ? activeConfig.arenaConfig : null);
        activePlayer = ResolveActivePlayer();

        GameObject bossPrefab = activeConfig != null ? activeConfig.ResolvePrefab() : null;
        if (bossPrefab == null)
        {
            Debug.LogWarning("[BossEncounterDirector] Missing boss prefab.");
            IsComplete = true;
            return;
        }

        Vector2 arenaCenter = arenaLimiter.IsActive ? arenaLimiter.Center : (Vector2)(Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        Vector2 spawnOffset = activeConfig != null && activeConfig.arenaConfig != null ? activeConfig.arenaConfig.bossSpawnOffset : new Vector2(0f, 3f);
        Vector3 spawnPosition = new Vector3(arenaCenter.x + spawnOffset.x, arenaCenter.y + spawnOffset.y, 0f);

        activeBossObject = ObjectPoolManager.Instance.Get(bossPrefab, spawnPosition, Quaternion.identity);
        activeBoss = activeBossObject.GetComponent<MonoBase>();
        activeBossEnemy = activeBossObject.GetComponent<EnemyBase>();

        if (activeBoss == null)
        {
            Debug.LogWarning($"[BossEncounterDirector] Spawned boss prefab {bossPrefab.name} has no MonoBase.");
            IsComplete = true;
            return;
        }

        ApplyBossScaling(activeBoss, activeBossEnemy, themeIndex);
        IsRunning = true;
        IsComplete = false;
    }

    public void Tick()
    {
        if (!IsRunning || IsComplete)
            return;

        if (activeBossObject == null || !activeBossObject.activeInHierarchy)
        {
            IsRunning = false;
            IsComplete = true;
        }
    }

    public void LateTick()
    {
        if (!IsRunning || IsComplete || !arenaLimiter.IsActive)
            return;

        if (activePlayer == null || !activePlayer.gameObject.activeInHierarchy)
            activePlayer = ResolveActivePlayer();

        arenaLimiter.ConstrainPlayer(activePlayer);
    }

    public void CleanupEncounter()
    {
        ClearActiveEnemies();

        if (activeBossObject != null && activeBossObject.activeInHierarchy)
            ObjectPoolManager.Instance.Return(activeBossObject);

        activeBossObject = null;
        activeBoss = null;
        activeBossEnemy = null;
        activePlayer = null;
        arenaLimiter.Deactivate();
        IsRunning = false;
    }

    public void Reset()
    {
        CleanupEncounter();
        activeConfig = null;
        pendingConfig = null;
        IsComplete = false;
    }

    private BossEncounterConfig ResolveEncounterConfig(BattleThemeConfig theme)
    {
        if (TryGetNextSequenceConfig(out var sequenceConfig))
            return sequenceConfig;

        if (theme != null && theme.bossEncounter != null)
            return theme.bossEncounter;

        return CreateFallbackConfig();
    }

    private bool TryGetNextSequenceConfig(out BossEncounterConfig config)
    {
        EnsureBossSequence();
        if (bossSequence.Count == 0)
        {
            config = null;
            return false;
        }

        if (nextBossSequenceIndex >= bossSequence.Count)
        {
            ShuffleBossSequence();
            nextBossSequenceIndex = 0;
        }

        config = bossSequence[nextBossSequenceIndex];
        nextBossSequenceIndex++;
        return config != null;
    }

    private void EnsureBossSequence()
    {
        if (bossSequence.Count > 0)
            return;

        var loadedConfigs = Resources.LoadAll<BossEncounterConfig>(BossConfigResourceFolder);
        if (loadedConfigs == null || loadedConfigs.Length == 0)
            return;

        for (int index = 0; index < loadedConfigs.Length; index++)
        {
            var config = loadedConfigs[index];
            if (config != null)
                bossSequence.Add(config);
        }

        ShuffleBossSequence();
        nextBossSequenceIndex = 0;
    }

    private void ShuffleBossSequence()
    {
        for (int index = bossSequence.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (bossSequence[index], bossSequence[swapIndex]) = (bossSequence[swapIndex], bossSequence[index]);
        }
    }

    private static string ResolveBossDisplayName(BossEncounterConfig config)
    {
        if (config == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(config.displayName))
            return config.displayName;

        return config.bossId ?? string.Empty;
    }

    private BossEncounterConfig CreateFallbackConfig()
    {
        return ScriptableObject.CreateInstance<BossEncounterConfig>().WithFallbackValues();
    }

    private void ApplyBossScaling(MonoBase boss, EnemyBase enemyBoss, int themeIndex)
    {
        float difficultyMultiplier = activeConfig != null ? Mathf.Max(0.1f, activeConfig.difficultyMultiplier) : 1f;
        float hpScale = (1f + Mathf.Max(0, themeIndex) * 0.35f) * difficultyMultiplier;
        float damageScale = (1f + Mathf.Max(0, themeIndex) * 0.20f) * difficultyMultiplier;

        boss.maxHp = Mathf.Max(1f, boss.maxHp * hpScale);
        boss.currentHp = boss.maxHp;

        if (enemyBoss != null)
        {
            enemyBoss.contactDamage = Mathf.Max(1, Mathf.RoundToInt(enemyBoss.contactDamage * damageScale));
            enemyBoss.scoreValue = Mathf.Max(50, Mathf.RoundToInt(enemyBoss.scoreValue * hpScale));
        }
    }

    private static void ClearActiveEnemies()
    {
        var enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            ObjectPoolManager.Instance.Return(enemy.gameObject);
        }

        var bosses = Object.FindObjectsByType<BossBase>(FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            var boss = bosses[i];
            if (boss == null || !boss.gameObject.activeInHierarchy)
                continue;

            ObjectPoolManager.Instance.Return(boss.gameObject);
        }
    }

    private static PlayerController ResolveActivePlayer()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayerObj != null)
            return PlayerManager.Instance.CurrentPlayerObj.GetComponent<PlayerController>();

        return Object.FindFirstObjectByType<PlayerController>();
    }
}

internal static class BossEncounterConfigFallbackExtensions
{
    public static BossEncounterConfig WithFallbackValues(this BossEncounterConfig config)
    {
        config.bossId = "boss_aircraft";
        config.displayName = "AirCraft";
        config.bossResourcePath = "Prefabs/Mono/Boss/BossPre/AirCraft";
        config.difficultyMultiplier = 1f;
        config.arenaConfig = new BossArenaConfig
        {
            centerOffset = Vector2.zero,
            halfExtents = new Vector2(8f, 4.5f),
            bossSpawnOffset = new Vector2(0f, 3f)
        };
        return config;
    }
}
