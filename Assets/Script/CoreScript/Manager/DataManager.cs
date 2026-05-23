using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoSingleton<DataManager>
{
    private SaveRoot saveRoot;

    // ==================== 快捷访问器 ====================

    public MetaProgressData Meta => saveRoot.meta;
    public RunSaveData Run => saveRoot.currentRun;
    public SettingsData Settings => saveRoot.settings;
    public bool HasActiveRun => Run != null && Run.hasActiveRun;

    /// <summary> 当前单局的装配数据快捷访问 </summary>
    public RunLoadoutData CurrentLoadout => Run?.loadout;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        saveRoot = SaveService.Load() ?? new SaveRoot();
        EnsureDefaultUnlocks();
    }

    // ==================== Run 生命周期 ====================

    /// <summary>
    /// 开始新一局游戏
    /// </summary>
    public void StartNewRun(int seed, string frameId)
    {
        saveRoot.currentRun = new RunSaveData
        {
            hasActiveRun = true,
            runSeed = seed,
            player = new PlayerRunData(),
            progression = new ProgressionRunData { level = 1 },
            loadout = BuildRunLoadout(frameId),
            world = new WorldRunData()
        };

        Meta.SetSelectedFrame(frameId);
        Meta.totalRunsPlayed++;
        Save();
    }

    public string GetPreferredFrameId()
    {
        return Meta.GetSelectedFrameId();
    }

    /// <summary>
    /// 结束当前局（死亡或胜利），结算奖励并清除 Run
    /// </summary>
    public void EndRun(bool victory, int waveReached)
    {
        if (Run == null) return;

        int reward = waveReached * 10;
        if (victory) reward *= 2;
        Meta.softCurrency += reward;

        if (waveReached > Meta.bestWaveReached)
            Meta.bestWaveReached = waveReached;

        saveRoot.currentRun = null;
        Save();
    }

    public void ClearActiveRun()
    {
        if (saveRoot.currentRun == null)
            return;

        saveRoot.currentRun = null;
        Save();
    }

    // ==================== Score 管理 ====================

    public int Score
    {
        get => Run != null ? Run.progression.score : 0;
        set
        {
            if (Run == null) return;
            Run.progression.score = value;
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        EventManager.Broadcast(GameEvent.PlayerScore, Score);
    }

    // ==================== IsGameOver 兼容 ====================

    public bool IsGameOver
    {
        get => Run != null && Run.world.isGameOver;
        set
        {
            if (Run == null) return;
            Run.world.isGameOver = value;
        }
    }

    // ==================== 存档 API ====================

    public void Save()
    {
        SaveService.Save(saveRoot);
    }

    public void ResetAll()
    {
        saveRoot = new SaveRoot();
        Save();
    }

    // ==================== 自动保存 ====================

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void EnsureDefaultUnlocks()
    {
        bool changed = false;
        var database = GameConfigDatabase.Instance;

        if (database?.allFrames != null)
        {
            foreach (var frame in database.allFrames)
            {
                if (frame == null || string.IsNullOrWhiteSpace(frame.frameId))
                    continue;

                int countBefore = saveRoot.meta.unlockedFrameIds.Count;
                saveRoot.meta.UnlockFrame(frame.frameId);
                changed |= saveRoot.meta.unlockedFrameIds.Count != countBefore;
            }
        }

        if (database?.allModules != null)
        {
            foreach (var module in database.allModules)
            {
                if (module == null)
                    continue;

                int countBefore = saveRoot.meta.unlockedModuleIds.Count;
                saveRoot.meta.UnlockModule(module.ModuleId);
                changed |= saveRoot.meta.unlockedModuleIds.Count != countBefore;
            }
        }

        if (database?.allCores != null)
        {
            foreach (var core in database.allCores)
            {
                if (core == null || string.IsNullOrWhiteSpace(core.coreId))
                    continue;

                int countBefore = saveRoot.meta.unlockedCoreIds.Count;
                saveRoot.meta.UnlockCore(core.coreId);
                changed |= saveRoot.meta.unlockedCoreIds.Count != countBefore;
            }
        }

        if (saveRoot.meta.MigrateLegacyUnlockedModules(database))
        {
            changed = true;
        }

        if (string.IsNullOrEmpty(saveRoot.meta.GetSelectedFrameId()) &&
            saveRoot.meta.unlockedFrameIds.Count > 0)
        {
            saveRoot.meta.SetSelectedFrame(saveRoot.meta.unlockedFrameIds[0]);
            changed = true;
        }

        if (changed)
            Save();
    }

    private RunLoadoutData BuildRunLoadout(string frameId)
    {
        var runLoadout = new RunLoadoutData { frameId = frameId };
        if (string.IsNullOrEmpty(frameId))
            return runLoadout;

        var source = Meta.GetOrInitFrameLoadout(frameId);
        var modulesById = new Dictionary<string, ModuleConfig>();
        var modulesByType = new Dictionary<ModuleType, ModuleConfig>();
        var db = GameConfigDatabase.Instance;

        if (db != null && db.allModules != null)
        {
            foreach (var module in db.allModules)
            {
                modulesById[module.ModuleId] = module;
                if (!modulesByType.ContainsKey(module.moduleType))
                    modulesByType[module.moduleType] = module;
            }
        }

        foreach (var slot in source.slots)
        {
            var module = ResolveModule(slot.moduleId, slot.moduleType, modulesById, modulesByType);
            var moduleId = module != null ? module.ModuleId : slot.moduleId;
            var moduleType = module != null ? module.moduleType : slot.moduleType;
            var moduleRarity = slot.moduleRarity;

            if (module != null && string.IsNullOrEmpty(moduleId))
                moduleId = module.ModuleId;

            if (module != null && string.IsNullOrEmpty(slot.moduleId))
                moduleRarity = module.defaultRarity;

            runLoadout.slots.Add(new SlotRuntimeSaveData
            {
                slotId = slot.slotId,
                moduleId = moduleId,
                moduleType = moduleType,
                moduleRarity = moduleRarity,
                coreId = slot.coreId,
                plugins = new List<PluginInstanceSaveData>(slot.plugins ?? new List<PluginInstanceSaveData>())
            });
        }

        return runLoadout;
    }

    private ModuleConfig ResolveModule(
        string moduleId,
        ModuleType moduleType,
        Dictionary<string, ModuleConfig> modulesById,
        Dictionary<ModuleType, ModuleConfig> modulesByType)
    {
        if (!string.IsNullOrEmpty(moduleId) && modulesById.TryGetValue(moduleId, out var moduleById))
            return moduleById;

        modulesByType.TryGetValue(moduleType, out var moduleByType);
        return moduleByType;
    }
}
