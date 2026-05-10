using System.Collections.Generic;
using UnityEngine;

public class BossEncounterDirector
{
    private readonly BossArenaLimiter arenaLimiter = new();
    private GameObject activeBossObject;
    private EnemyBase activeBossEnemy;
    private BossEncounterConfig activeConfig;
    private PlayerController activePlayer;

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    public string CurrentBossName => activeConfig != null && !string.IsNullOrWhiteSpace(activeConfig.displayName)
        ? activeConfig.displayName
        : activeConfig != null ? activeConfig.bossId : string.Empty;
    public EnemyBase ActiveBossEnemy => activeBossEnemy;

    public void BeginEncounter(BattleThemeConfig theme, int themeIndex)
    {
        Reset();

        activeConfig = ResolveEncounterConfig(theme);
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
        activeBossEnemy = activeBossObject.GetComponent<EnemyBase>();

        if (activeBossEnemy == null)
        {
            Debug.LogWarning($"[BossEncounterDirector] Spawned boss prefab {bossPrefab.name} has no EnemyBase.");
            IsComplete = true;
            return;
        }

        ApplyBossScaling(activeBossEnemy, themeIndex);
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
        activeBossEnemy = null;
        activePlayer = null;
        arenaLimiter.Deactivate();
        IsRunning = false;
    }

    public void Reset()
    {
        CleanupEncounter();
        activeConfig = null;
        IsComplete = false;
    }

    private BossEncounterConfig ResolveEncounterConfig(BattleThemeConfig theme)
    {
        if (theme != null && theme.bossEncounter != null)
            return theme.bossEncounter;

        return CreateFallbackConfig();
    }

    private BossEncounterConfig CreateFallbackConfig()
    {
        return ScriptableObject.CreateInstance<BossEncounterConfig>().WithFallbackValues();
    }

    private void ApplyBossScaling(EnemyBase bossEnemy, int themeIndex)
    {
        float difficultyMultiplier = activeConfig != null ? Mathf.Max(0.1f, activeConfig.difficultyMultiplier) : 1f;
        float hpScale = (1f + Mathf.Max(0, themeIndex) * 0.35f) * difficultyMultiplier;
        float damageScale = (1f + Mathf.Max(0, themeIndex) * 0.20f) * difficultyMultiplier;

        bossEnemy.maxHp = Mathf.Max(1f, bossEnemy.maxHp * hpScale);
        bossEnemy.currentHp = bossEnemy.maxHp;
        bossEnemy.contactDamage = Mathf.Max(1, Mathf.RoundToInt(bossEnemy.contactDamage * damageScale));
        bossEnemy.scoreValue = Mathf.Max(50, Mathf.RoundToInt(bossEnemy.scoreValue * hpScale));
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
