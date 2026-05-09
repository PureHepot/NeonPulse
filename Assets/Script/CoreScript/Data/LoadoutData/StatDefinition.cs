using UnityEngine;

public enum StatValueKind
{
    Float = 0,
    Integer = 1,
    Percent = 2
}

[CreateAssetMenu(fileName = "NewStatDefinition", menuName = "Game/Loadout/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The first segment of the stat id, e.g. move / health / shooter.")]
    public string groupKey;

    [Tooltip("The second segment of the stat id, e.g. speed / max_hp / base_damage.")]
    public string statKey;

    public string displayName;
    [TextArea] public string description;

    [Header("Usage")]
    public StatValueKind valueKind = StatValueKind.Float;
    public ModuleCategory allowedCategories = ModuleCategory.None;

    public string StatId
    {
        get
        {
            string normalizedGroup = NormalizeKey(groupKey);
            string normalizedStat = NormalizeKey(statKey);

            if (string.IsNullOrEmpty(normalizedGroup))
                return normalizedStat;

            if (string.IsNullOrEmpty(normalizedStat))
                return normalizedGroup;

            return $"{normalizedGroup}.{normalizedStat}";
        }
    }

    public bool Matches(string otherStatId)
    {
        return !string.IsNullOrWhiteSpace(otherStatId) &&
               string.Equals(StatId, otherStatId.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    public bool Allows(ModuleCategory categories)
    {
        if (allowedCategories == ModuleCategory.None)
            return true;

        return (allowedCategories & categories) != 0;
    }

    private static string NormalizeKey(string raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? string.Empty
            : raw.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
