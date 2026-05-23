using System;

public static class PluginSpecialEffectUtility
{
    public const string DoubleWeaponDamageEffectId = "DoubleDamage";
    public const string DoubleWeaponAttackSpeedEffectId = "DoubleAttackSpeed";
    public const string DoubleHealthRegenEffectId = "DoubleRegen";
    public const string DoubleDefenceReflectionEffectId = "DoubleReflection";

    public static bool MatchesEffect(LoadoutPluginRuntimeData pluginRuntime, string effectId)
    {
        return pluginRuntime?.pluginConfig != null &&
               !string.IsNullOrWhiteSpace(effectId) &&
               string.Equals(pluginRuntime.pluginConfig.effectId, effectId, StringComparison.OrdinalIgnoreCase);
    }

    public static float ResolveMultiplier(LoadoutPluginRuntimeData pluginRuntime, float fallbackMultiplier = 2f)
    {
        if (pluginRuntime == null)
            return fallbackMultiplier;

        return pluginRuntime.effectParams.param1 > 0f
            ? pluginRuntime.effectParams.param1
            : fallbackMultiplier;
    }
}
