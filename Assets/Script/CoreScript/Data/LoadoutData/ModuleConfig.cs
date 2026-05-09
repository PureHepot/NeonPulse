using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewModuleConfig", menuName = "Game/Loadout/Module Config")]
public class ModuleConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string moduleId;
    public string moduleName;
    public ModuleType moduleType;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Categories")]
    [Tooltip("Tags used by frame slots to decide whether this module can be equipped.")]
    public ModuleCategory categories = ModuleCategory.Weapon;

    [Header("Stat Schema")]
    [Tooltip("Defines which stats this module category is allowed to use.")]
    public ModuleStatSchema statSchema;

    [Header("Rarity")]
    public ModuleRarity defaultRarity = ModuleRarity.Common;
    public List<ModuleRarityProfile> rarityProfiles = new List<ModuleRarityProfile>();

    [Header("Slots")]
    public int coreSlotCount = 1;

    [Header("Visuals")]
    public Color themeColor = Color.cyan;
    public GameObject runtimePrefab;
    public GameObject previewPrefab;

    [Header("Meta")]
    public int unlockCost = 0;

    public string ModuleId => string.IsNullOrEmpty(moduleId) ? moduleType.ToString() : moduleId;
    public int CoreSlotCount => Mathf.Max(0, coreSlotCount);

    public int GetPluginSlots(ModuleRarity rarity)
    {
        var profile = GetProfile(rarity);
        return profile != null ? Mathf.Max(0, profile.maxPluginSlots) : 0;
    }

    public int GetLoadCost(ModuleRarity rarity)
    {
        var profile = GetProfile(rarity);
        return profile != null ? Mathf.Max(0, profile.loadCost) : 0;
    }

    public ModuleRarityProfile GetProfile(ModuleRarity rarity)
    {
        if (rarityProfiles == null)
            return null;

        foreach (var profile in rarityProfiles)
        {
            if (profile != null && profile.rarity == rarity)
                return profile;
        }

        return null;
    }

    public IEnumerable<StatDefinition> GetAllowedStats()
    {
        if (statSchema?.availableStats == null)
            return Array.Empty<StatDefinition>();

        return statSchema.availableStats;
    }

    public bool CanUseStat(StatDefinition definition)
    {
        if (definition == null)
            return false;

        if (statSchema != null && !statSchema.Contains(definition))
            return false;

        return definition.Allows(categories);
    }

    public bool CanUseStat(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
            return false;

        if (statSchema != null && !statSchema.Contains(statId))
            return false;

        var definition = GameConfigDatabase.Instance != null
            ? GameConfigDatabase.Instance.GetStatDefinition(statId)
            : null;

        return definition == null || definition.Allows(categories);
    }

    public float GetBaseStat(StatDefinition definition)
    {
        return GetBaseStat(definition, defaultRarity);
    }

    public float GetBaseStat(StatDefinition definition, ModuleRarity rarity)
    {
        return TryGetBaseStat(definition, rarity, out var value) ? value : 0f;
    }

    public float GetBaseStat(string statId)
    {
        return GetBaseStat(statId, defaultRarity);
    }

    public float GetBaseStat(string statId, ModuleRarity rarity)
    {
        return TryGetBaseStat(statId, rarity, out var value) ? value : 0f;
    }

    public bool TryGetBaseStat(StatDefinition definition, ModuleRarity rarity, out float value)
    {
        if (definition == null)
        {
            value = 0f;
            return false;
        }

        return TryGetStat(GetProfileStats(rarity), stat => stat.Matches(definition), out value);
    }

    public bool TryGetBaseStat(string statId, ModuleRarity rarity, out float value)
    {
        if (string.IsNullOrWhiteSpace(statId))
        {
            value = 0f;
            return false;
        }

        return TryGetStat(GetProfileStats(rarity), stat => stat.Matches(statId), out value);
    }

    public bool HasCategory(ModuleCategory category)
    {
        return (categories & category) != 0;
    }

    public GameObject GetRuntimePrefab()
    {
        return runtimePrefab != null ? runtimePrefab : previewPrefab;
    }

    private List<ModuleStatValue> GetProfileStats(ModuleRarity rarity)
    {
        return GetProfile(rarity)?.baseStats;
    }

    private bool TryGetStat(List<ModuleStatValue> stats, Predicate<ModuleStatValue> matcher, out float value)
    {
        if (stats != null)
        {
            foreach (var stat in stats)
            {
                if (!matcher(stat))
                    continue;

                value = stat.value;
                return true;
            }
        }

        value = 0f;
        return false;
    }
}

[Serializable]
public struct ModuleStatValue
{
    public StatDefinition statDefinition;
    public float value;

    public string StatId
    {
        get { return statDefinition != null ? statDefinition.StatId : string.Empty; }
    }

    public bool Matches(StatDefinition definition)
    {
        if (definition == null)
            return false;

        if (statDefinition != null &&
            (statDefinition == definition || statDefinition.Matches(definition.StatId)))
        {
            return true;
        }

        return false;
    }

    public bool Matches(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
            return false;

        if (statDefinition != null && statDefinition.Matches(statId))
            return true;
        
        return false;
    }
}

[Serializable]
public class ModuleRarityProfile
{
    public ModuleRarity rarity;
    public List<ModuleStatValue> baseStats = new List<ModuleStatValue>();
    public int maxPluginSlots = 1;
    public int loadCost = 1;
}
