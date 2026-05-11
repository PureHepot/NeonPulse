using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    private const string PlayerPrefabResourcePath = "Prefabs/Mono/Player/Player";
    private const string FrameCoreResourceRoot = "Prefabs/Mono/Frame/Core";

    public Action<int, int> OnHpChanged;

    public GameObject playerPrefab;
    public Transform spawnPoint;

    public GameObject CurrentPlayerObj { get; private set; }
    public ModuleManager CurrentModules { get; private set; }

    [Header("Visual References")]
    public SpriteRenderer bodyRenderer;

    [SerializeField] private int currentHp;
    [SerializeField] private int maxHealth;

    public int CurrentHp
    {
        get => currentHp;
        set
        {
            currentHp = Mathf.Clamp(value, 0, MaxHealth);
            OnHpChanged?.Invoke(currentHp, MaxHealth);
        }
    }

    public int MaxHealth => maxHealth;
    public bool IsPlayerAlive => CurrentPlayerObj != null && CurrentPlayerObj.activeInHierarchy;
    public Vector3 PlayerPosition => CurrentPlayerObj != null ? CurrentPlayerObj.transform.position : Vector3.zero;

    public void SpawnPlayer()
    {
        var data = DataManager.Instance;
        var database = GameConfigDatabase.Instance;
        var runtimeLoadout = data != null ? data.CurrentLoadout : null;
        if (runtimeLoadout == null || database == null)
        {
            Debug.LogWarning("[PlayerManager] Cannot spawn runtime player because run loadout or database is missing.");
            return;
        }

        var prefab = playerPrefab != null ? playerPrefab : Resources.Load<GameObject>(PlayerPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("[PlayerManager] Player prefab not found for runtime spawn.");
            return;
        }

        ClearRuntimePlayer();

        Vector3 spawnPosition = ResolveSpawnPosition();
        var playerObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        playerObject.name = prefab.name;

        RegisterRuntimePlayer(playerObject);
        AttachFrameCore(playerObject.transform, ResolveFrameConfig(runtimeLoadout.frameId));
        AttachRuntimeModules(playerObject, runtimeLoadout, database);
    }

    public void ClearRuntimePlayer()
    {
        if (CurrentPlayerObj != null)
            Destroy(CurrentPlayerObj);

        CurrentPlayerObj = null;
        CurrentModules = null;
        bodyRenderer = null;
        currentHp = 0;
        maxHealth = 0;
    }

    public void RegisterRuntimePlayer(GameObject playerObject)
    {
        CurrentPlayerObj = playerObject;
        CurrentModules = playerObject != null ? playerObject.GetComponent<ModuleManager>() : null;
        bodyRenderer = null;

        if (playerObject == null)
            return;

        var playerController = playerObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.IsDead = false;
            playerController.IsStunned = false;
            playerController.IsDashing = false;
            playerController.ConfigureRuntime(true, false);
        }
    }

    public void UpdatePlayerVisuals(Sprite bodySprite, Color color)
    {
        if (bodyRenderer == null)
            return;

        if (bodySprite != null)
            bodyRenderer.sprite = bodySprite;

        bodyRenderer.color = color;
    }

    public void SyncHp(int current, int max)
    {
        maxHealth = Mathf.Max(0, max);
        currentHp = Mathf.Clamp(current, 0, maxHealth);
        OnHpChanged?.Invoke(currentHp, maxHealth);
    }

    public void SavePlayerState()
    {
        var run = DataManager.Instance.Run;
        if (run == null)
            return;

        run.player.currentHp = currentHp;
        run.player.maxHp = maxHealth;

        if (CurrentPlayerObj == null)
            return;

        var pos = CurrentPlayerObj.transform.position;
        run.player.posX = pos.x;
        run.player.posY = pos.y;
    }

    private void AttachRuntimeModules(GameObject playerObject, RunLoadoutData runtimeLoadout, GameConfigDatabase database)
    {
        if (playerObject == null || runtimeLoadout?.slots == null)
            return;

        var moduleManager = playerObject.GetComponent<ModuleManager>();
        if (moduleManager == null)
            return;

        moduleManager.ClearRuntimeModules();

        var modulesRoot = playerObject.transform.Find("Modules") ?? playerObject.transform;
        foreach (var slot in runtimeLoadout.slots)
        {
            var runtimeData = LoadoutModuleRuntimeBuilder.Build(slot, database);
            if (runtimeData == null || !runtimeData.HasModule)
                continue;

            var modulePrefab = PlayerModulePrefabResolver.Resolve(runtimeData);
            if (modulePrefab == null)
            {
                Debug.LogWarning($"[PlayerManager] Missing runtime prefab for module {runtimeData.moduleId}.");
                continue;
            }

            var moduleObject = Instantiate(modulePrefab, modulesRoot);
            var playerModule = moduleObject.GetComponent<PlayerModule>();
            if (playerModule == null)
                playerModule = moduleObject.AddComponent<PassiveModule>();

            moduleManager.RegisterRuntimeModule(playerModule, runtimeData);
        }
    }

    private void AttachFrameCore(Transform playerRoot, FrameConfig frameConfig)
    {
        if (playerRoot == null)
            return;

        var coreRoot = playerRoot.Find("Core") ?? playerRoot;
        for (int index = coreRoot.childCount - 1; index >= 0; index--)
            Destroy(coreRoot.GetChild(index).gameObject);

        var frameCorePrefab = ResolveFrameCorePrefab(frameConfig);
        if (frameCorePrefab == null)
            return;

        var frameCoreInstance = Instantiate(frameCorePrefab, coreRoot);
        frameCoreInstance.name = frameCorePrefab.name;
        frameCoreInstance.transform.localPosition = Vector3.zero;
        frameCoreInstance.transform.localRotation = Quaternion.identity;
        frameCoreInstance.transform.localScale = Vector3.one;
    }

    private Vector3 ResolveSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        var run = DataManager.Instance.Run;
        if (run != null)
        {
            var savedPosition = new Vector3(run.player.posX, run.player.posY, 0f);
            if (savedPosition.sqrMagnitude > 0.0001f)
                return savedPosition;
        }

        var camera = Camera.main;
        if (camera != null)
            return new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);

        return Vector3.zero;
    }

    private static FrameConfig ResolveFrameConfig(string frameId)
    {
        if (string.IsNullOrWhiteSpace(frameId))
            return null;

        var database = GameConfigDatabase.Instance;
        if (database?.allFrames == null)
            return null;

        foreach (var frame in database.allFrames)
        {
            if (frame != null && string.Equals(frame.frameId, frameId, StringComparison.OrdinalIgnoreCase))
                return frame;
        }

        return null;
    }

    private static GameObject ResolveFrameCorePrefab(FrameConfig frameConfig)
    {
        if (frameConfig == null)
            return null;

        string framePrefabName = string.Empty;
        if (frameConfig.slotLayoutPrefab != null)
            framePrefabName = frameConfig.slotLayoutPrefab.name;
        else if (frameConfig.frameCore != null)
            framePrefabName = frameConfig.frameCore.name;
        else if (!string.IsNullOrWhiteSpace(frameConfig.frameId))
            framePrefabName = frameConfig.frameId.Trim();

        if (!string.IsNullOrWhiteSpace(framePrefabName))
        {
            var runtimeFrameCore = Resources.Load<GameObject>($"{FrameCoreResourceRoot}/Core_{framePrefabName}");
            if (runtimeFrameCore != null)
                return runtimeFrameCore;
        }

        return frameConfig.frameCore;
    }
}
