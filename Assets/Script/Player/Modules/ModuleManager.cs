using System.Collections.Generic;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    private PlayerController playerController;
    private readonly Dictionary<string, PlayerModule> modulesBySlot = new();
    private readonly Dictionary<ModuleType, List<PlayerModule>> modulesByType = new();
    private readonly List<PlayerModule> activeModules = new();

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        float deltaTime = playerController != null ? playerController.ModuleDeltaTime : Time.deltaTime;
        if (Mathf.Approximately(deltaTime, 0f))
            return;

        for (int index = 0; index < activeModules.Count; index++)
        {
            var module = activeModules[index];
            if (module != null && module.IsActiveModule)
                module.OnModuleUpdate();
        }
    }

    public void ClearRuntimeModules(bool destroyModuleObjects = true)
    {
        for (int index = activeModules.Count - 1; index >= 0; index--)
        {
            var module = activeModules[index];
            if (module == null)
                continue;

            module.DeactivateModule();
            if (destroyModuleObjects)
                Destroy(module.gameObject);
        }

        activeModules.Clear();
        modulesBySlot.Clear();
        modulesByType.Clear();
    }

    public bool RegisterRuntimeModule(PlayerModule module, LoadoutModuleRuntimeData runtimeData)
    {
        if (module == null || runtimeData == null || string.IsNullOrWhiteSpace(runtimeData.slotId))
            return false;

        if (modulesBySlot.TryGetValue(runtimeData.slotId, out var existingModule))
        {
            if (existingModule != null)
            {
                existingModule.DeactivateModule();
                Destroy(existingModule.gameObject);
            }

            RemoveModuleReferences(runtimeData.slotId);
        }

        module.Initialize(playerController, runtimeData);
        module.ActivateModule();

        modulesBySlot[runtimeData.slotId] = module;
        activeModules.Add(module);

        if (!modulesByType.TryGetValue(module.moduleType, out var typedModules))
        {
            typedModules = new List<PlayerModule>();
            modulesByType[module.moduleType] = typedModules;
        }

        typedModules.Add(module);
        return true;
    }

    public PlayerModule GetModule(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return null;

        modulesBySlot.TryGetValue(slotId, out var module);
        return module;
    }

    public T GetModule<T>(ModuleType type) where T : PlayerModule
    {
        if (!modulesByType.TryGetValue(type, out var typedModules))
            return null;

        for (int index = 0; index < typedModules.Count; index++)
        {
            if (typedModules[index] is T matchedModule)
                return matchedModule;
        }

        return null;
    }

    public List<PlayerModule> GetAllActiveModules()
    {
        return new List<PlayerModule>(activeModules);
    }

    public bool HasAbility(ModuleType type)
    {
        return modulesByType.TryGetValue(type, out var typedModules) && typedModules.Count > 0;
    }

    public void DisableModule(ModuleType type)
    {
        if (!modulesByType.TryGetValue(type, out var typedModules))
            return;

        for (int index = 0; index < typedModules.Count; index++)
        {
            if (typedModules[index] != null)
                typedModules[index].DeactivateModule();
        }
    }

    public void RemoveModule(ModuleType type)
    {
        if (!modulesByType.TryGetValue(type, out var typedModules))
            return;

        var snapshot = new List<PlayerModule>(typedModules);
        for (int index = 0; index < snapshot.Count; index++)
        {
            var module = snapshot[index];
            if (module == null)
                continue;

            string slotId = module.SlotId;
            module.DeactivateModule();
            Destroy(module.gameObject);
            RemoveModuleReferences(slotId);
        }
    }

    private void RemoveModuleReferences(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return;

        if (!modulesBySlot.TryGetValue(slotId, out var module))
            return;

        modulesBySlot.Remove(slotId);
        activeModules.Remove(module);

        if (modulesByType.TryGetValue(module.moduleType, out var typedModules))
        {
            typedModules.Remove(module);
            if (typedModules.Count == 0)
                modulesByType.Remove(module.moduleType);
        }
    }
}
