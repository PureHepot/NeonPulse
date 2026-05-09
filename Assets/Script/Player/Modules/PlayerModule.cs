using UnityEngine;

public enum ModuleType
{
    Movement,
    Health,
    Shooter,
    Shield,
    Dash,
    LaserDrone,
    Sniper,
    Shotgun,
    SawBlade,
    None
}

public abstract class PlayerModule : MonoBehaviour
{
    protected PlayerController player;
    private LoadoutModuleRuntimeData runtimeData;

    public ModuleType moduleType { get; private set; } = ModuleType.None;
    public bool isUnlocked => IsActiveModule;
    public bool IsInitialized { get; private set; }
    public bool IsActiveModule { get; private set; }
    public string SlotId => runtimeData != null ? runtimeData.slotId : string.Empty;
    public LoadoutModuleRuntimeData RuntimeData => runtimeData;
    public ModuleConfig ModuleConfig => runtimeData != null ? runtimeData.moduleConfig : null;
    public CoreConfig CoreConfig => runtimeData != null ? runtimeData.coreConfig : null;

    protected float DeltaTime => player != null ? player.ModuleDeltaTime : Time.deltaTime;
    protected bool HasControl => player != null && player.AcceptsInput;
    protected bool IsPrimaryPlayer => player != null && player.IsPrimaryRuntimePlayer;

    public void Initialize(PlayerController playerController, LoadoutModuleRuntimeData moduleRuntimeData)
    {
        player = playerController;
        runtimeData = moduleRuntimeData;
        moduleType = moduleRuntimeData != null ? moduleRuntimeData.moduleType : ModuleType.None;
        IsInitialized = true;
        OnInitialize();
    }

    public void ActivateModule()
    {
        if (!IsInitialized)
            return;

        IsActiveModule = true;
        enabled = true;
        OnActivate();
    }

    public void DeactivateModule()
    {
        OnDeactivate();
        IsActiveModule = false;
        enabled = false;
    }

    public virtual void OnModuleUpdate()
    {
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnActivate()
    {
    }

    protected virtual void OnDeactivate()
    {
    }

    protected float GetStat(StatDefinition statDefinition, float fallbackValue = 0f)
    {
        if (runtimeData == null || statDefinition == null)
            return fallbackValue;

        float value = runtimeData.GetFinalStat(statDefinition);
        return Mathf.Approximately(value, 0f) ? fallbackValue : value;
    }

    protected float GetStat(string statId, float fallbackValue = 0f)
    {
        if (runtimeData == null || string.IsNullOrWhiteSpace(statId))
            return fallbackValue;

        float value = runtimeData.GetFinalStat(statId);
        return Mathf.Approximately(value, 0f) ? fallbackValue : value;
    }

    protected int GetIntStat(StatDefinition statDefinition, int fallbackValue = 0)
    {
        return Mathf.RoundToInt(GetStat(statDefinition, fallbackValue));
    }

    protected int GetIntStat(string statId, int fallbackValue = 0)
    {
        return Mathf.RoundToInt(GetStat(statId, fallbackValue));
    }

    protected bool HasPlugin(string effectId)
    {
        return runtimeData != null && runtimeData.HasPlugin(effectId);
    }

    protected bool HasPlugin(PluginType pluginType)
    {
        return runtimeData != null && runtimeData.HasPlugin(pluginType);
    }

    protected bool TryGetPlugin(string effectId, out LoadoutPluginRuntimeData pluginRuntime)
    {
        if (runtimeData != null)
            return runtimeData.TryGetPlugin(effectId, out pluginRuntime);

        pluginRuntime = null;
        return false;
    }

    protected bool TryGetPlugin(PluginType pluginType, out LoadoutPluginRuntimeData pluginRuntime)
    {
        if (runtimeData != null)
            return runtimeData.TryGetPlugin(pluginType, out pluginRuntime);

        pluginRuntime = null;
        return false;
    }
}
