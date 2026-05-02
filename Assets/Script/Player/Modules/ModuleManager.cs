using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    private PlayerController playerController;

    private Dictionary<ModuleType, PlayerModule> moduleDict = new Dictionary<ModuleType, PlayerModule>();

    private List<PlayerModule> activeModules = new List<PlayerModule>();

    public Action Initialize { get; private set; }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

       

       
    }

    /// <summary>
    /// 注册一个模块实例到管理器（由 LoadModule 调用）
    /// </summary>
    /*public void RegisterModule(PlayerModule module)
    {
        if (module == null) return;
        if (!moduleDict.ContainsKey(module.moduleType))
        {
            moduleDict.Add(module.moduleType, module);
        }
        else
        {
            Debug.LogWarning($"模块已存在，跳过注册: {module.moduleType}");
        }
    }*/

    void Update()
    {
        float delta = Time.deltaTime;  
        // 这里添加层级检查，确保只有玩家对象或UI模型对象的模块更新被执行,同时UI_Model不受时间缩放影响
        if(transform.gameObject.layer==LayerMask.NameToLayer("Player")) 
        {
            delta = Time.timeScale;
        }
        if(transform.gameObject.layer==LayerMask.NameToLayer("UI_Model")) 
        {
            delta = Time.unscaledDeltaTime;
        }
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
            ModuleConfig config=UpgradeManager.Instance.GetConfig(type);
            if(config)
            {
                Transform modulesRoot = transform.Find("Modules") ?? transform;
                GameObject instance = Instantiate(config.prefab, modulesRoot);
                PlayerModule module1=instance.GetComponent<PlayerModule>();
                module1.OnActivate();
                activeModules.Add(module1);
                moduleDict.Add(config.moduleType, module1);
                module1.Initialize(playerController);
            }
        }
    }

    public void UpgradeModule(ModuleType type, StatType stat)
    {
        if (moduleDict.TryGetValue(type, out PlayerModule module))
        {
            if (module.isUnlocked)
            {
                module.UpgradeModule(type, stat);
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

    /// <summary>                   /// 禁用模块
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

    public void RemoveModule(ModuleType type)
    {
        if (moduleDict.ContainsKey(type))
        {
            moduleDict.Remove(type);
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
