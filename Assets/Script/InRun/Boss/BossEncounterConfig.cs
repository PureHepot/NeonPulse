using System;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "BossEncounterConfig", menuName = "Game/InRun/Boss Encounter")]
public class BossEncounterConfig : ScriptableObject
{
    [Header("基础信息")]
    public string bossId = "boss_aircraft";
    public string displayName = "AirCraft";

    [Header("资源")]
    public GameObject bossPrefab;
    public string bossResourcePath;

    [Header("场地")]
    public BossArenaConfig arenaConfig;

    [Header("难度")]
    public float difficultyMultiplier = 1f;

    public GameObject ResolvePrefab()
    {
        if (bossPrefab != null)
            return bossPrefab;

        if (string.IsNullOrWhiteSpace(bossResourcePath))
            return null;

        bossPrefab = Resources.Load<GameObject>(NormalizeResourcePath(bossResourcePath));
        return bossPrefab;
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

[Serializable]
public class BossArenaConfig
{
    [Tooltip("Arena 中心相对摄像机中心的偏移。")]
    public Vector2 centerOffset = Vector2.zero;

    [Tooltip("Arena 半宽半高。")]
    public Vector2 halfExtents = new(8f, 4.5f);

    [Tooltip("Boss 初始生成点相对 Arena 中心的偏移。")]
    public Vector2 bossSpawnOffset = new(0f, 3f);
}
