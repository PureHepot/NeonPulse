using UnityEngine;

/// <summary>
/// 负责根据 UpgradeManager.startingModules 实例化模块预制体并注册到 IModuleManager
/// 预制体在对应 ModuleConfig.prefab 字段中指定
/// </summary>
public class LoadModule : MonoBehaviour
{
    private ModuleManager moduleManager;

    void Awake()
    {
        moduleManager = GetComponentInParent<ModuleManager>();

        var upgradeManager = UpgradeManager.Instance;

        foreach (var moduleType in upgradeManager.startingModules)
        {
            ModuleConfig config = upgradeManager.GetConfig(moduleType);
            GameObject instance = Instantiate(config.prefab, transform);
            PlayerModule module = instance.GetComponent<PlayerModule>();

            if (module != null)
            {
                //moduleManager.RegisterModule(module);
            }
            else
            {
                Debug.LogWarning($"[LoadModule] 预制体 {config.prefab.name} 根节点上没有 PlayerModule 组件，已跳过");
            }
        }
    }
}
