using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    private int maxHealth;
    public int MaxHealth => maxHealth;

    public Action<int, int> OnHpChanged;
    public GameObject playerPrefab;
    public Transform spawnPoint;
    public GameObject CurrentPlayerObj { get; private set; }
    public PlayerModuleManager CurrentModules { get; private set; }
    public PlayerPreview PlayerPreview { get; private set; }

    public bool IsPlayerAlive => CurrentPlayerObj != null;

    public Vector3 PlayerPosition => CurrentPlayerObj ? CurrentPlayerObj.transform.position : Vector3.zero;

    // --- 玩家视觉引用 ---
    [Header("Visual References")]
    public SpriteRenderer bodyRenderer;

    // --- 玩家数据 ---
    [SerializeField] private int currentHp;
    public int CurrentHp
    {
        get => currentHp;
        set
        {
            currentHp = Mathf.Clamp(value, 0, MaxHealth);
            OnHpChanged?.Invoke(currentHp, MaxHealth);
        }
    }

    private void Awake()
    {
        if (spawnPoint == null)
        {
            var bf = GameObject.Find("BattleField");
            if (bf) spawnPoint = bf.transform;
        }
    }

    public void SpawnPlayer()
    {
        if (CurrentPlayerObj != null) return;

        if (spawnPoint) spawnPoint.gameObject.SetActive(true);

        // 判断是否从存档恢复
        var run = DataManager.Instance.Run;
        bool resuming = run != null && run.player.maxHp > 0;

        Vector3 spawnPos;
        if (resuming)
        {
            spawnPos = new Vector3(run.player.posX, run.player.posY, 0f);
        }
        else
        {
            spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
        }

        CurrentPlayerObj = Instantiate(playerPrefab, spawnPos, spawnPoint ? spawnPoint.rotation : Quaternion.identity);

        CurrentModules = CurrentPlayerObj.GetComponent<PlayerModuleManager>();

        if (bodyRenderer == null) bodyRenderer = CurrentPlayerObj.GetComponentInChildren<SpriteRenderer>();

        var pc = CurrentPlayerObj.GetComponent<PlayerController>();
        pc.OnDeath += HandlePlayerDeath;

        Debug.Log("<color=green>Player Generated</color>");

        UpgradeManager.Instance.ApplyModulesToPlayer();

        CurrentModules.Initialize?.Invoke();

        // 从存档恢复 HP（在模块初始化之后，覆盖 HealthModule 的默认满血）
        if (resuming)
        {
            var healthModule = CurrentPlayerObj.GetComponent<HealthModule>();
            if (healthModule != null)
            {
                // SyncHp 会设置 PlayerManager 的 currentHp/maxHealth
                SyncHp(run.player.currentHp, run.player.maxHp);
            }
        }
        else
        {
            currentHp = MaxHealth;
        }

        MaskSystemManager.Instance?.ApplyCurrentMaskVisuals();

        GameObject.Find("PlayerModelCamera").GetComponent<PlayerPreviewSync>().RebuildPreview();
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("<color=red>Player Died</color>");
        DataManager.Instance.IsGameOver = true;
        CurrentPlayerObj.SetActive(false);
    }

    public void UpdatePlayerVisuals(Sprite bodySprite, Color color)
    {
        if (CurrentPlayerObj != null && bodyRenderer != null)
        {
            if (bodySprite != null) bodyRenderer.sprite = bodySprite;
            bodyRenderer.color = color;
        }
    }

    public void SyncHp(int current, int max)
    {
        currentHp = current;
        maxHealth = max;
        OnHpChanged?.Invoke(current, max);
    }

    /// <summary>
    /// 将当前玩家状态写入 DataManager.Run.player，用于存档
    /// </summary>
    public void SavePlayerState()
    {
        var run = DataManager.Instance.Run;
        if (run == null) return;

        run.player.currentHp = currentHp;
        run.player.maxHp = maxHealth;

        if (CurrentPlayerObj != null)
        {
            var pos = CurrentPlayerObj.transform.position;
            run.player.posX = pos.x;
            run.player.posY = pos.y;
        }
    }
}
