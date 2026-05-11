using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "Game/Database")]
public class GameConfigDatabase : ScriptableObject
{
    private static GameConfigDatabase _instance;

    private Dictionary<string, StatDefinition> statDefinitionsById;
    private Dictionary<string, ModuleStatSchema> moduleStatSchemasById;

    public static GameConfigDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<GameConfigDatabase>("Configs/GameConfigDatabase");

            return _instance;
        }
    }

    private void OnEnable()
    {
        ClearCaches();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ClearCaches();
    }
#endif

    [Header("Characters")]
    public List<CharacterConfig> allCharacters;

    [Header("Frames")]
    public List<FrameConfig> allFrames;

    [SerializeField, HideInInspector]
    private List<StatDefinition> allStatDefinitions = new List<StatDefinition>();

    [Header("Stat Schemas")]
    public List<ModuleStatSchema> allModuleStatSchemas;

    [Header("Modules")]
    public List<ModuleConfig> allModules;

    [Header("Cores")]
    public List<CoreConfig> allCores;

    [Header("Plugins")]
    public List<PluginConfig> allPlugins;

    public IReadOnlyList<StatDefinition> AllStatDefinitions => allStatDefinitions;

    public StatDefinition GetStatDefinition(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
            return null;

        BuildCachesIfNeeded();
        statDefinitionsById.TryGetValue(statId.Trim().ToLowerInvariant(), out var definition);
        return definition;
    }

    public ModuleStatSchema GetModuleStatSchema(string schemaId)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
            return null;

        BuildCachesIfNeeded();
        moduleStatSchemasById.TryGetValue(schemaId.Trim(), out var schema);
        return schema;
    }

    private void BuildCachesIfNeeded()
    {
        if (statDefinitionsById != null &&
            moduleStatSchemasById != null)
        {
            return;
        }

        statDefinitionsById = new Dictionary<string, StatDefinition>();
        moduleStatSchemasById = new Dictionary<string, ModuleStatSchema>();

        foreach (var definition in CollectResolvedStatDefinitions())
        {
            if (definition == null)
                continue;

            statDefinitionsById[definition.StatId.ToLowerInvariant()] = definition;
        }

        if (allModuleStatSchemas != null)
        {
            foreach (var schema in allModuleStatSchemas)
            {
                if (schema == null)
                    continue;

                moduleStatSchemasById[schema.SchemaId] = schema;
            }
        }
    }

    private void ClearCaches()
    {
        statDefinitionsById = null;
        moduleStatSchemasById = null;
    }

    private List<StatDefinition> CollectResolvedStatDefinitions()
    {
        var result = new List<StatDefinition>();
        var uniqueDefinitions = new HashSet<StatDefinition>();

        AddStatDefinitions(result, uniqueDefinitions, allStatDefinitions);

        if (allModuleStatSchemas != null)
        {
            foreach (var schema in allModuleStatSchemas)
            {
                if (schema?.availableStats == null)
                    continue;

                AddStatDefinitions(result, uniqueDefinitions, schema.availableStats);
            }
        }

        if (allModules != null)
        {
            foreach (var module in allModules)
            {
                if (module?.rarityProfiles == null)
                    continue;

                foreach (var profile in module.rarityProfiles)
                {
                    if (profile?.baseStats == null)
                        continue;

                    foreach (var stat in profile.baseStats)
                    {
                        if (stat.statDefinition != null && uniqueDefinitions.Add(stat.statDefinition))
                            result.Add(stat.statDefinition);
                    }
                }
            }
        }

        return result;
    }

    private static void AddStatDefinitions(
        List<StatDefinition> destination,
        HashSet<StatDefinition> uniqueDefinitions,
        List<StatDefinition> source)
    {
        if (source == null)
            return;

        foreach (var definition in source)
        {
            if (definition != null && uniqueDefinitions.Add(definition))
                destination.Add(definition);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Collect Config Assets")]
    public void AutoCollectConfigAssets()
    {
        allCharacters = CollectAssets<CharacterConfig>(config => config != null ? config.name : string.Empty);
        allFrames = CollectAssets<FrameConfig>(config => config != null ? config.name : string.Empty);
        allModuleStatSchemas = CollectAssets<ModuleStatSchema>(config => config != null ? config.SchemaId : string.Empty);
        allModules = CollectAssets<ModuleConfig>(config => config != null ? config.ModuleId : string.Empty);
        allCores = CollectAssets<CoreConfig>(config => config != null ? config.name : string.Empty);
        allPlugins = CollectAssets<PluginConfig>(config => config != null ? config.name : string.Empty);

        var collectedStats = CollectAssets<StatDefinition>(config => config != null ? config.StatId : string.Empty);
        allStatDefinitions = collectedStats;
        AddStatDefinitions(allStatDefinitions, new HashSet<StatDefinition>(allStatDefinitions), CollectResolvedStatDefinitions());

        ClearCaches();
    }

    private static List<T> CollectAssets<T>(System.Func<T, string> sortKeySelector) where T : ScriptableObject
    {
        var assets = new List<T>();
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        assets.Sort((left, right) =>
        {
            string leftKey = sortKeySelector(left);
            string rightKey = sortKeySelector(right);
            return string.Compare(leftKey, rightKey, System.StringComparison.OrdinalIgnoreCase);
        });

        return assets;
    }
#endif
}
