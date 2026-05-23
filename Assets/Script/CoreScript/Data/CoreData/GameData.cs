using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Save data root and serialized runtime/meta models.
// ============================================================

[Serializable]
public class SaveRoot
{
    public int version = 2;
    public SettingsData settings = new();
    public MetaProgressData meta = new();
    public RunSaveData currentRun;
}

[Serializable]
public class SettingsData
{
    public float bgmVolume = 1f;
    public float effectVolume = 1f;
    public bool isBgmMuted;
    public bool isEffectMuted;
}

[Serializable]
public class MetaProgressData
{
    public int softCurrency;
    public int totalRunsPlayed;
    public int bestWaveReached;
    public string selectedFrameId;

    public List<string> unlockedFrameIds = new();
    public List<ModuleType> unlockedModules = new();
    public List<string> unlockedModuleIds = new();
    public List<string> unlockedCoreIds = new();
    public List<string> unlockedPluginIds = new();
    public List<FrameLoadoutSaveData> frameLoadouts = new();

    public bool IsFrameUnlocked(string frameId) => unlockedFrameIds.Contains(frameId);
    public bool IsModuleUnlocked(ModuleType type)
    {
        if (type == ModuleType.None)
            return false;

        if (unlockedModules.Contains(type))
            return true;

        var database = GameConfigDatabase.Instance;
        if (database?.allModules == null)
            return false;

        foreach (var module in database.allModules)
        {
            if (module == null || module.moduleType != type)
                continue;

            if (unlockedModuleIds.Contains(module.ModuleId))
                return true;
        }

        return false;
    }

    public bool IsModuleUnlocked(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return false;

        if (unlockedModuleIds.Contains(moduleId))
            return true;

        var database = GameConfigDatabase.Instance;
        var module = ResolveModuleConfig(database, moduleId);
        return module != null && unlockedModules.Contains(module.moduleType);
    }

    public bool IsCoreUnlocked(string coreId) => unlockedCoreIds.Contains(coreId);
    public bool IsPluginUnlocked(string pluginId) => unlockedPluginIds.Contains(pluginId);

    public void UnlockFrame(string frameId)
    {
        if (!unlockedFrameIds.Contains(frameId))
            unlockedFrameIds.Add(frameId);
    }

    public void UnlockModule(ModuleType type)
    {
        if (type == ModuleType.None)
            return;

        if (!unlockedModules.Contains(type))
            unlockedModules.Add(type);

        var database = GameConfigDatabase.Instance;
        if (database?.allModules == null)
            return;

        foreach (var module in database.allModules)
        {
            if (module != null && module.moduleType == type)
                UnlockModule(module.ModuleId);
        }
    }

    public void UnlockModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        moduleId = moduleId.Trim();
        if (!unlockedModuleIds.Contains(moduleId))
            unlockedModuleIds.Add(moduleId);
    }

    public void UnlockCore(string coreId)
    {
        if (!unlockedCoreIds.Contains(coreId))
            unlockedCoreIds.Add(coreId);
    }

    public void UnlockPlugin(string pluginId)
    {
        if (!unlockedPluginIds.Contains(pluginId))
            unlockedPluginIds.Add(pluginId);
    }

    public FrameLoadoutSaveData GetOrInitFrameLoadout(string frameId)
    {
        var loadout = frameLoadouts.Find(f => f.frameId == frameId);
        if (loadout != null)
            return loadout;

        loadout = new FrameLoadoutSaveData { frameId = frameId };
        frameLoadouts.Add(loadout);
        return loadout;
    }

    public string GetSelectedFrameId()
    {
        if (!string.IsNullOrEmpty(selectedFrameId))
            return selectedFrameId;

        if (frameLoadouts.Count > 0 && !string.IsNullOrEmpty(frameLoadouts[^1].frameId))
            return frameLoadouts[^1].frameId;

        return unlockedFrameIds.Count > 0 ? unlockedFrameIds[0] : null;
    }

    public void SetSelectedFrame(string frameId)
    {
        if (string.IsNullOrEmpty(frameId))
            return;

        selectedFrameId = frameId;

        var loadout = GetOrInitFrameLoadout(frameId);
        frameLoadouts.Remove(loadout);
        frameLoadouts.Add(loadout);
    }

    public bool MigrateLegacyUnlockedModules(GameConfigDatabase database)
    {
        if (unlockedModules == null || unlockedModules.Count == 0 || database?.allModules == null)
            return false;

        bool changed = false;
        foreach (var module in database.allModules)
        {
            if (module == null || !unlockedModules.Contains(module.moduleType))
                continue;

            if (unlockedModuleIds.Contains(module.ModuleId))
                continue;

            unlockedModuleIds.Add(module.ModuleId);
            changed = true;
        }

        return changed;
    }

    private static ModuleConfig ResolveModuleConfig(GameConfigDatabase database, string moduleId)
    {
        if (database?.allModules == null || string.IsNullOrWhiteSpace(moduleId))
            return null;

        foreach (var module in database.allModules)
        {
            if (module != null && string.Equals(module.ModuleId, moduleId.Trim(), StringComparison.OrdinalIgnoreCase))
                return module;
        }

        return null;
    }
}

[Serializable]
public class FrameLoadoutSaveData
{
    public string frameId;
    public List<SlotSaveData> slots = new();
}

[Serializable]
public class RunSaveData
{
    public bool hasActiveRun;
    public int runSeed;

    public PlayerRunData player = new();
    public ProgressionRunData progression = new();
    public RunLoadoutData loadout = new();
    public WorldRunData world = new();
    public InRunRuntimeSaveData inRun = new();
}

[Serializable]
public class RunLoadoutData
{
    public string frameId;
    public List<SlotRuntimeSaveData> slots = new();
}

[Serializable]
public class SlotRuntimeSaveData
{
    public string slotId;
    public string moduleId;
    public ModuleType moduleType = ModuleType.None;
    public ModuleRarity moduleRarity = ModuleRarity.Common;
    public string coreId;
    public List<PluginInstanceSaveData> plugins = new();
}

[Serializable]
public class PluginInstanceSaveData
{
    public string pluginId;
    public PluginRarity rarity;
}

[Serializable]
public class SlotSaveData
{
    public string slotId;
    public string moduleId;
    public ModuleType moduleType = ModuleType.None;
    public ModuleRarity moduleRarity = ModuleRarity.Common;
    public string coreId;
    public List<PluginInstanceSaveData> plugins = new();
}

[Serializable]
public class PlayerRunData
{
    public float currentHp;
    public float maxHp;
    public float posX;
    public float posY;
}

[Serializable]
public class ProgressionRunData
{
    public int level = 1;
    public int score;
}

[Serializable]
public class WaveRunData
{
    public int currentWaveIndex;
    public float elapsedTimeInWave;
}

[Serializable]
public class WorldRunData
{
    public string backgroundThemeId;
    public bool isGameOver;
    public bool isVictory;
}
