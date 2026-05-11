using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewModuleStatSchema", menuName = "Game/Loadout/Module Stat Schema")]
public class ModuleStatSchema : ScriptableObject
{
    [Header("Identity")]
    public string schemaId;
    public string displayName;
    [TextArea] public string description;

    [Header("Usage")]
    public ModuleCategory allowedCategories = ModuleCategory.None;
    public List<StatDefinition> availableStats = new List<StatDefinition>();

    public string SchemaId => string.IsNullOrWhiteSpace(schemaId) ? name : schemaId.Trim();

    public bool Allows(ModuleCategory categories)
    {
        if (allowedCategories == ModuleCategory.None)
            return true;

        return (allowedCategories & categories) != 0;
    }

    public bool Contains(StatDefinition definition)
    {
        if (definition == null || availableStats == null)
            return false;

        foreach (var stat in availableStats)
        {
            if (stat == null)
                continue;

            if (stat == definition || stat.Matches(definition.StatId))
                return true;
        }

        return false;
    }

    public bool Contains(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId) || availableStats == null)
            return false;

        foreach (var stat in availableStats)
        {
            if (stat != null && stat.Matches(statId))
                return true;
        }

        return false;
    }
}
