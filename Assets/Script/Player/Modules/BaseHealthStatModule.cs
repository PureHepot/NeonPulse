// Loadout-owned health module.
// Provides additional HP / regen stats through LoadoutStatGraph,
// while the actual player health runtime is handled by HealthModule.
public sealed class BaseHealthStatModule : PlayerModule
{
    private const string DoubleHealthRegenEffectId = PluginSpecialEffectUtility.DoubleHealthRegenEffectId;

    protected override void OnInitialize()
    {
        RefreshRuntimeHealth();
    }

    protected override void OnActivate()
    {
        RefreshRuntimeHealth();
    }

    protected override void OnDeactivate()
    {
        RefreshRuntimeHealth();
    }

    private void RefreshRuntimeHealth()
    {
        if (player == null || player.Modules == null)
            return;

        var healthModule = player.Modules.GetModule<HealthModule>(ModuleType.Health);
        if (healthModule == null)
            return;

        healthModule.ConfigureHealthModifiers(ResolveHealthRegenMultiplier());
        healthModule.RefreshFromLoadout();
    }

    private float ResolveHealthRegenMultiplier()
    {
        if (!TryGetPlugin(DoubleHealthRegenEffectId, out var pluginRuntime))
            return 1f;

        return PluginSpecialEffectUtility.ResolveMultiplier(pluginRuntime);
    }
}
