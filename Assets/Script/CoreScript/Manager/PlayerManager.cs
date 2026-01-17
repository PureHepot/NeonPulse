using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    [Header("配置")]
    public int MaxHealth;
    public GameObject playerPrefab;
    public Transform spawnPoint;

    public GameObject CurrentPlayerObj { get; private set; }

    public Vector3 PlayerPosition => CurrentPlayerObj ? CurrentPlayerObj.transform.position : Vector3.zero;

    public bool IsPlayerAlive => CurrentPlayerObj != null;

    public Action<GameObject> OnPlayerSpawned;
    public Action OnPlayerDead;

    //---模块---

    private HashSet<ModuleType> unlockedModuleTypes = new HashSet<ModuleType>();
    public PlayerModuleManager CurrentModules { get; private set; }

    //---玩家数据---
    private int currentHp;
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }


    /// <summary>
    /// 生成玩家
    /// </summary>
    public void SpawnPlayer()
    {
        if (CurrentPlayerObj != null) return; // 防止重复生成

        currentHp = MaxHealth;

        // 实例化
        CurrentPlayerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        CurrentModules = CurrentPlayerObj.GetComponent<PlayerModuleManager>();

        var health = CurrentPlayerObj.GetComponent<IDamageable>();
        if (health is PlayerController pc)
        {
            pc.onDeath += HandlePlayerDeath;
        }

        Debug.Log("<color=green>Player Generated</color>");
        OnPlayerSpawned?.Invoke(CurrentPlayerObj);
    }

    /// <summary>
    /// 处理玩家死亡
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.Log("<color=red>Player Died</color>");

        OnPlayerDead?.Invoke();

        DataManager.Instance.GameData.IsGameOver = true;

        CurrentPlayerObj.SetActive(false);

    }

    /// <summary>
    /// 给玩家添加新能力
    /// </summary>
    public void UnlockModuleData(ModuleType type)
    {
        if (!unlockedModuleTypes.Contains(type))
        {
            unlockedModuleTypes.Add(type);

            if (CurrentModules != null)
            {
                CurrentModules.UnlockModule(type);
            }
        }
    }

    //用于复活/存档
    private void SyncModulesToPlayer()
    {
        foreach (var type in unlockedModuleTypes)
        {
            CurrentModules.UnlockModule(type);
        }
    }
}
