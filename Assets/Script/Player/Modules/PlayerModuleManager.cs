using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModuleManager : MonoBehaviour
{
    private PlayerController playerController;

    private Dictionary<ModuleType, PlayerModule> moduleDict = new Dictionary<ModuleType, PlayerModule>();

    private List<PlayerModule> activeModules = new List<PlayerModule>();

    public Action Initialize { get; private set; }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        PlayerModule[] modules = GetComponentsInChildren<PlayerModule>(true);
        foreach (var module in modules)
        {
            if (!moduleDict.ContainsKey(module.moduleType))
            {
                moduleDict.Add(module.moduleType, module);
            }
        }

        Initialize = () =>
        {
            modules = GetComponentsInChildren<PlayerModule>(true);

            foreach (var module in modules)
            {
                module.Initialize(playerController);
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
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            if (!module.isUnlocked)
            {
                module.OnActivate();
                activeModules.Add(module);
                Debug.Log($"<color=cyan>模块已装载: {type}</color>");
            }
        }
        else
        {
            Debug.LogError($"找不到模块: {type}，请检查是否挂载了对应脚本并设置了Type");
        }
    }

    public void UpgradeModule(ModuleType type)
    {
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            if (module.isUnlocked)
            {
                //module.UpgradeModule();
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
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            if (module.isUnlocked)
            {
                module.OnDeactivate();
                activeModules.Remove(module);
            }
        }
    }

    public T GetModule<T>(ModuleType type) where T : PlayerModule
    {
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            if (module.isUnlocked)
            {
                return module as T;
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
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            return module.isUnlocked;
        }
        return false;
    }
}
