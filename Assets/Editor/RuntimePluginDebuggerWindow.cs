using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RuntimePluginDebuggerWindow : EditorWindow
{
    private int selectedModuleIndex = -1;
    private int selectedPluginIndex = -1;
    private PluginRarity selectedPluginRarity = PluginRarity.Common;
    private Vector2 moduleScroll;
    private Vector2 pluginScroll;

    [MenuItem("Tools/Runtime Plugin Debugger")]
    public static void Open()
    {
        GetWindow<RuntimePluginDebuggerWindow>("Runtime Plugin Debugger");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Runtime Plugin Debugger", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect the current runtime player and add plugins.", MessageType.Info);
            return;
        }

        var playerManager = PlayerManager.Instance;
        var loadoutManager = GameMgr.Instance != null ? GameMgr.Instance.Loadout : null;
        if (playerManager == null || loadoutManager == null)
        {
            EditorGUILayout.HelpBox("PlayerManager or LoadoutManager is not available.", MessageType.Warning);
            return;
        }

        var modules = playerManager.CurrentModules != null
            ? playerManager.CurrentModules.GetAllActiveModules()
            : new List<PlayerModule>();

        EditorGUILayout.LabelField($"Player: {(playerManager.CurrentPlayerObj != null ? playerManager.CurrentPlayerObj.name : "None")}");
        EditorGUILayout.LabelField($"Module Count: {modules.Count}");

        if (modules.Count == 0)
        {
            EditorGUILayout.HelpBox("No active runtime modules found on the current player.", MessageType.Info);
            return;
        }

        ClampSelection(ref selectedModuleIndex, modules.Count);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawModuleList(modules);
            DrawPluginPanel(modules[selectedModuleIndex], loadoutManager);
        }
    }

    private void DrawModuleList(List<PlayerModule> modules)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
        {
            EditorGUILayout.LabelField("Runtime Modules", EditorStyles.boldLabel);
            moduleScroll = EditorGUILayout.BeginScrollView(moduleScroll);
            for (int index = 0; index < modules.Count; index++)
            {
                var module = modules[index];
                if (module == null)
                    continue;

                string moduleName = module.ModuleConfig != null
                    ? module.ModuleConfig.moduleName
                    : module.name;
                string label = $"{moduleName}  [{module.SlotId}]";
                if (GUILayout.Toggle(selectedModuleIndex == index, label, "Button"))
                {
                    if (selectedModuleIndex != index)
                    {
                        selectedModuleIndex = index;
                        selectedPluginIndex = -1;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPluginPanel(PlayerModule module, LoadoutManager loadoutManager)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (module == null)
            {
                EditorGUILayout.HelpBox("Select a module.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Selected Module", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", module.ModuleConfig != null ? module.ModuleConfig.moduleName : module.name);
            EditorGUILayout.LabelField("Slot", module.SlotId);
            EditorGUILayout.LabelField("Type", module.moduleType.ToString());
            EditorGUILayout.LabelField("Category", module.ModuleConfig != null ? module.ModuleConfig.categories.ToString() : "Unknown");
            if (module.RuntimeData != null)
            {
                EditorGUILayout.LabelField("Rarity", module.RuntimeData.moduleRarity.ToString());
                EditorGUILayout.LabelField(
                    "Plugin Capacity",
                    $"{module.RuntimeData.Plugins.Count} / {module.RuntimeData.GetPluginCapacity()}");
            }

            EditorGUILayout.Space(8f);
            DrawInstalledPlugins(module);

            var compatiblePlugins = GetCompatiblePlugins(module, loadoutManager);
            if (compatiblePlugins.Count == 0)
            {
                EditorGUILayout.HelpBox("No compatible plugins found for this module.", MessageType.Info);
                return;
            }

            ClampSelection(ref selectedPluginIndex, compatiblePlugins.Count);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Compatible Plugins", EditorStyles.boldLabel);

            pluginScroll = EditorGUILayout.BeginScrollView(pluginScroll);
            for (int index = 0; index < compatiblePlugins.Count; index++)
            {
                var plugin = compatiblePlugins[index];
                if (plugin == null)
                    continue;

                string label = $"{plugin.displayName}  [{plugin.pluginType}]";
                if (GUILayout.Toggle(selectedPluginIndex == index, label, "Button"))
                    selectedPluginIndex = index;
            }

            EditorGUILayout.EndScrollView();

            if (selectedPluginIndex < 0 || selectedPluginIndex >= compatiblePlugins.Count)
                return;

            var selectedPlugin = compatiblePlugins[selectedPluginIndex];
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Plugin Detail", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", selectedPlugin.displayName);
            EditorGUILayout.LabelField("Type", selectedPlugin.pluginType.ToString());
            EditorGUILayout.LabelField("Effect", string.IsNullOrWhiteSpace(selectedPlugin.effectId) ? "-" : selectedPlugin.effectId);
            EditorGUILayout.LabelField("Description", string.IsNullOrWhiteSpace(selectedPlugin.description) ? "-" : selectedPlugin.description, EditorStyles.wordWrappedLabel);
            selectedPluginRarity = (PluginRarity)EditorGUILayout.EnumPopup("Rarity", selectedPluginRarity);

            using (new EditorGUI.DisabledScope(!CanAddPlugin(module, selectedPlugin)))
            {
                if (GUILayout.Button("Add Plugin To Module", GUILayout.Height(28f)))
                    AddPluginToModule(module, selectedPlugin, selectedPluginRarity, loadoutManager);
            }
        }
    }

    private static void DrawInstalledPlugins(PlayerModule module)
    {
        EditorGUILayout.LabelField("Installed Plugins", EditorStyles.boldLabel);
        var runtimePlugins = module.RuntimeData != null ? module.RuntimeData.Plugins : null;
        if (runtimePlugins == null || runtimePlugins.Count == 0)
        {
            EditorGUILayout.LabelField("None");
            return;
        }

        for (int index = 0; index < runtimePlugins.Count; index++)
        {
            var runtimePlugin = runtimePlugins[index];
            if (runtimePlugin?.pluginConfig == null)
                continue;

            EditorGUILayout.LabelField($"- {runtimePlugin.pluginConfig.displayName} ({runtimePlugin.rarity})");
        }
    }

    private static List<PluginConfig> GetCompatiblePlugins(PlayerModule module, LoadoutManager loadoutManager)
    {
        var result = new List<PluginConfig>();
        if (module?.ModuleConfig == null || loadoutManager == null)
            return result;

        foreach (var plugin in loadoutManager.GetAllPlugins())
        {
            if (plugin == null)
                continue;
            if (!plugin.CanInsertInto(module.ModuleConfig))
                continue;

            result.Add(plugin);
        }

        result.Sort((left, right) => string.CompareOrdinal(left.displayName, right.displayName));
        return result;
    }

    private static bool CanAddPlugin(PlayerModule module, PluginConfig plugin)
    {
        if (module?.RuntimeData == null || module.ModuleConfig == null || plugin == null)
            return false;

        if (!plugin.CanInsertInto(module.ModuleConfig))
            return false;

        return module.RuntimeData.GetPluginCapacity() > module.RuntimeData.Plugins.Count;
    }

    private static void AddPluginToModule(PlayerModule module, PluginConfig plugin, PluginRarity rarity, LoadoutManager loadoutManager)
    {
        if (module == null || plugin == null || loadoutManager == null)
            return;

        bool success = loadoutManager.InsertPlugin(module.SlotId, plugin.pluginId, rarity);
        if (!success)
        {
            Debug.LogWarning($"[RuntimePluginDebugger] Failed to add plugin {plugin.pluginId} to slot {module.SlotId}.");
            return;
        }

        var playerManager = GameMgr.Instance != null ? GameMgr.Instance.Player : null;
        if (playerManager != null)
        {
            playerManager.SavePlayerState();
            playerManager.SpawnPlayer();
        }

        Debug.Log($"[RuntimePluginDebugger] Added plugin {plugin.pluginId} ({rarity}) to slot {module.SlotId}.");
    }

    private static void ClampSelection(ref int index, int count)
    {
        if (count <= 0)
        {
            index = -1;
            return;
        }

        if (index < 0 || index >= count)
            index = 0;
    }
}
