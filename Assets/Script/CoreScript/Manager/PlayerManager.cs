using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    [Header("配置")]
    [SerializeField] private int maxHealth;
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
        // 移除原有的模块初始化逻辑，全部移交 UpgradeManager
        if (spawnPoint == null)
        {
            var bf = GameObject.Find("BattleField");
            if (bf) spawnPoint = bf.transform;
        }
    }

    public void SpawnPlayer()
    {
        if (CurrentPlayerObj != null) return;

        currentHp = MaxHealth;
        if (spawnPoint) spawnPoint.gameObject.SetActive(true);

        CurrentPlayerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        CurrentModules = CurrentPlayerObj.GetComponent<PlayerModuleManager>();

        if (bodyRenderer == null) bodyRenderer = CurrentPlayerObj.GetComponentInChildren<SpriteRenderer>();

        var pc = CurrentPlayerObj.GetComponent<PlayerController>();
        pc.OnDeath += HandlePlayerDeath;

        Debug.Log("<color=green>Player Generated</color>");

        this.PlayerPreview = GameObject.Find("PlayerModelCamera").GetComponent<PlayerPreview>();

        UpgradeManager.Instance.ApplyModulesToPlayer();
        //UpgradeManager.Instance.ApplyModulesToPlayer(this.PlayerPreview.CurrentModel.GetComponent<PlayerModuleManager>());

        CurrentModules.Initialize?.Invoke();

        MaskSystemManager.Instance?.ApplyCurrentMaskVisuals();
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("<color=red>Player Died</color>");
        DataManager.Instance.GameData.IsGameOver = true;
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
}
//public class PlayerManager : MonoSingleton<PlayerManager>
//{
//    [Header("配置")]
//    [SerializeField]
//    private int maxHealth;

//    public int MaxHealth
//    {
//        get => maxHealth;
//        set
//        {
//            maxHealth = value;
//            OnHpChanged?.Invoke(CurrentHp, maxHealth);
//        }
//    }

//    [SerializeField]
//    private int bulletDamage = 1;

//    public Action<int, int> OnHpChanged; 

//    public GameObject playerPrefab;
//    public Transform spawnPoint;

//    public GameObject CurrentPlayerObj { get; private set; }

//    public Vector3 PlayerPosition => CurrentPlayerObj ? CurrentPlayerObj.transform.position : Vector3.zero;

//    public bool IsPlayerAlive => CurrentPlayerObj != null;

//    public Action<GameObject> OnPlayerSpawned;
//    public Action OnPlayerDead;

//    //---模块---
//    [Header("Default Loadout")]
//    public List<ModuleType> startingModules;

//    private HashSet<ModuleType> unlockedModuleTypes = new HashSet<ModuleType>();
//    public PlayerModuleManager CurrentModules { get; private set; }

//    //---玩家数据---
//    [SerializeField]
//    private int currentHp;
//    public int CurrentHp
//    {
//        get => currentHp;
//        set
//        {
//            currentHp = Mathf.Clamp(value, 0, MaxHealth);
//            OnHpChanged?.Invoke(currentHp, MaxHealth);
//        }
//    }

//    public int BulletDamage { get { return bulletDamage; } set { bulletDamage = value; } }

//    private void Awake()
//    {
//        // 初始化初始解锁模块
//        foreach (var type in startingModules)
//        {
//            if (!unlockedModuleTypes.Contains(type))
//            {
//                unlockedModuleTypes.Add(type);
//            }
//        }
//        UpgradeManager.Instance.SyncWithPlayerManager();

//        if (spawnPoint == null)
//        {
//            spawnPoint = GameObject.Find("BattleField").transform;
//            spawnPoint.gameObject.SetActive(false);
//        }

//    }

//    /// <summary>
//    /// 生成玩家
//    /// </summary>
//    public void SpawnPlayer()
//    {
//        if (CurrentPlayerObj != null) return; 

//        currentHp = MaxHealth;

//        spawnPoint.gameObject.SetActive(true);

//        CurrentPlayerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

//        CurrentModules = CurrentPlayerObj.GetComponent<PlayerModuleManager>();

//        var pc = CurrentPlayerObj.GetComponent<PlayerController>();
//        pc.OnDeath += HandlePlayerDeath;

//        Debug.Log("<color=green>Player Generated</color>");

//        SyncModulesToPlayer();

//        OnPlayerSpawned?.Invoke(CurrentPlayerObj);
//    }

//    private void SyncModulesToPlayer()
//    {
//        if (CurrentModules == null) return;

//        foreach (var type in unlockedModuleTypes)
//        {
//            CurrentModules.UnlockModule(type);
//        }
//    }

//    /// <summary>
//    /// 处理玩家死亡
//    /// </summary>
//    private void HandlePlayerDeath()
//    {
//        Debug.Log("<color=red>Player Died</color>");

//        OnPlayerDead?.Invoke();

//        DataManager.Instance.GameData.IsGameOver = true;

//        CurrentPlayerObj.SetActive(false);
//    }

//    /// <summary>
//    /// 添加新能力
//    /// </summary>
//    public void UnlockModuleData(ModuleType type)
//    {
//        if (!unlockedModuleTypes.Contains(type))
//        {
//            unlockedModuleTypes.Add(type);
//            Debug.Log($"模块{type}已加入解锁列表");

//            if (CurrentModules != null)
//            {
//                CurrentModules.UnlockModule(type);
//            }
//        }
//    }

//    /// <summary>
//    /// 检查模块是否已解锁
//    /// </summary>
//    public bool IsModuleUnlocked(ModuleType type)
//    {
//        return unlockedModuleTypes.Contains(type);
//    }



//}