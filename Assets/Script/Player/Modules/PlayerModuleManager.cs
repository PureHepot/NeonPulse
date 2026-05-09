using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModuleManager : MonoBehaviour
{
    private PlayerController playerController;

    private Dictionary<ModuleType, PlayerModule> ModuleDict = new Dictionary<ModuleType, PlayerModule>();

    private List<PlayerModule> activeModules = new List<PlayerModule>();

    public Action Initialize { get; private set; }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        PlayerModule[] Modules = GetComponentsInChildren<PlayerModule>(true);
        foreach (var Module in Modules)
        {
            if (!ModuleDict.ContainsKey(Module.ModuleType))
            {
                ModuleDict.Add(Module.ModuleType, Module);
            }
        }

        Initialize = () =>
        {
            PlayerModule[] Modules = GetComponentsInChildren<PlayerModule>(true);

            foreach (var Module in Modules)
            {
                Module.Initialize(playerController);
            }
        };
    }

    void Update()
    {
        for (int i = 0; i < activeModules.Count; i++)
        {
            activeModules[i].OnModuleUpdate();
        }
    }

    public void UnlockModule(ModuleType type)
    {
        if (ModuleDict.TryGetValue(type, out PlayerModule Module))
        {
            if (!Module.isUnlocked)
            {
                Module.OnActivate();
                activeModules.Add(Module);
                Debug.Log($"<color=cyan>模块已装载: {type}</color>");
            }
        }
        else
        {
            Debug.LogError($"找不到模块: {type}，请检查是否挂载了对应脚本并设置了Type");
        }
    }

    public void UpgradeModule(ModuleType type, StatType stat)
    {
        if (ModuleDict.TryGetValue(type, out PlayerModule Module))
        {
            if (Module.isUnlocked)
            {
                Module.UpgradeModule(type, stat);
                Debug.Log($"<color=green>模块已升级: {type}</color>");
            }
            else
            {
                Debug.LogWarning($"模块{type}未解锁，无法升级");
            }
        }
        else
        {
            Debug.LogError($"找不到模块: {type}，请检查是否挂载了对应脚本并设置了Type");
        }
    }

    /// <summary>
    /// 禁用模块
    /// </summary>
    public void DisableModule(ModuleType type)
    {
        if (ModuleDict.TryGetValue(type, out PlayerModule Module))
        {
            if (Module.isUnlocked)
            {
                Module.OnDeactivate();
                activeModules.Remove(Module);
            }
        }
    }

    public void RemoveModule(ModuleType type)
    {
        if (ModuleDict.ContainsKey(type))
        {
            ModuleDict.Remove(type);
        }
    }

    public T GetModule<T>(ModuleType type) where T : PlayerModule
    {
        if (ModuleDict.TryGetValue(type, out PlayerModule Module))
        {
            if (Module.isUnlocked)
            {
                return Module as T;
            }
        }
        return null;
    }

    public List<PlayerModule> GetAllActiveModules()
    {
        return new List<PlayerModule>(activeModules);
    }

    public bool HasAbility(ModuleType type)
    {
        if (ModuleDict.TryGetValue(type, out PlayerModule Module))
        {
            return Module.isUnlocked;
        }
        return false;
    }
}
