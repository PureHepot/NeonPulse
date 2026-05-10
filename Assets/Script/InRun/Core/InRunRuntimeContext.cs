using System;
using System.Collections.Generic;
using UnityEngine;

public class InRunRuntimeContext
{
    private readonly System.Random random;
    private readonly List<BattleThemeConfig> themeDrawBag = new();

    public InRunRuntimeContext(RunSaveData run, InRunConfigDatabase config)
    {
        Run = run;
        Config = config;
        Runtime = run.inRun ?? new InRunRuntimeSaveData();
        Run.inRun = Runtime;

        int seed = run != null && run.runSeed != 0 ? run.runSeed : Environment.TickCount;
        random = new System.Random(seed);
    }

    public RunSaveData Run { get; }
    public InRunConfigDatabase Config { get; }
    public InRunRuntimeSaveData Runtime { get; }

    public int CurrentThemeIndex => Runtime.currentThemeIndex;
    public int CurrentLoopIndex => Runtime.currentLoopIndex;
    public InRunPhase CurrentPhase => Runtime.phase;
    public bool HasSavedProgress => Runtime.phase != InRunPhase.None && Runtime.phase != InRunPhase.RunEnded;

    public void InitializeForRun()
    {
        Runtime.runSeed = Run != null ? Run.runSeed : 0;
        Runtime.currentThemeIndex = -1;
        Runtime.currentLoopIndex = -1;
        Runtime.phase = InRunPhase.None;
        Runtime.selectedThemeIds.Clear();
        Runtime.themes.Clear();
        Runtime.runCurrency = 0;
        Runtime.runScoreTotal = 0;
        Runtime.lifetimeKillsThisRun = 0;
        WarehouseRuntimeState.ResetForNewRun(Runtime);
        ShopInventoryRuntimeState.Clear(Runtime);
        Runtime.pendingRewards.Clear();

        themeDrawBag.Clear();
        if (Config?.allThemes != null)
        {
            foreach (var theme in Config.allThemes)
            {
                if (theme != null)
                    themeDrawBag.Add(theme);
            }
        }
    }

    public void SetPhase(InRunPhase phase)
    {
        Runtime.phase = phase;
    }

    public BattleThemeConfig SelectTheme(int themeIndex)
    {
        Runtime.currentThemeIndex = themeIndex;
        Runtime.currentLoopIndex = -1;

        var theme = DrawTheme();
        string themeId = ResolveThemeId(theme, themeIndex);

        EnsureThemeSave(themeIndex, themeId);
        Runtime.themes[themeIndex].themeId = themeId;

        if (Runtime.selectedThemeIds.Count <= themeIndex)
            Runtime.selectedThemeIds.Add(themeId);
        else
            Runtime.selectedThemeIds[themeIndex] = themeId;

        return theme;
    }

    public BattleThemeConfig GetOrSelectTheme(int themeIndex)
    {
        Runtime.currentThemeIndex = themeIndex;

        string existingThemeId = GetThemeId(themeIndex);
        if (!string.IsNullOrWhiteSpace(existingThemeId))
        {
            var existingTheme = ResolveThemeConfig(existingThemeId);
            if (existingTheme != null)
                return existingTheme;
        }

        return SelectTheme(themeIndex);
    }

    public CombatLoopRuntimeSaveData BeginLoop(int loopIndex)
    {
        Runtime.currentLoopIndex = loopIndex;

        var themeSave = EnsureThemeSave(Runtime.currentThemeIndex, GetCurrentThemeId());
        while (themeSave.loops.Count <= loopIndex)
        {
            themeSave.loops.Add(new CombatLoopRuntimeSaveData
            {
                loopIndex = themeSave.loops.Count
            });
        }

        return themeSave.loops[loopIndex];
    }

    public void MarkBossDefeated()
    {
        var themeSave = EnsureThemeSave(Runtime.currentThemeIndex, GetCurrentThemeId());
        themeSave.bossDefeated = true;
    }

    public string GetCurrentThemeId()
    {
        if (Runtime.currentThemeIndex < 0 || Runtime.currentThemeIndex >= Runtime.selectedThemeIds.Count)
            return string.Empty;

        return Runtime.selectedThemeIds[Runtime.currentThemeIndex];
    }

    public string GetThemeId(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= Runtime.selectedThemeIds.Count)
            return string.Empty;

        return Runtime.selectedThemeIds[themeIndex];
    }

    public BattleThemeConfig ResolveThemeConfig(string themeId)
    {
        if (Config?.allThemes == null || string.IsNullOrWhiteSpace(themeId))
            return null;

        string normalizedThemeId = themeId.Trim();
        foreach (var theme in Config.allThemes)
        {
            if (theme != null && string.Equals(theme.themeId, normalizedThemeId, StringComparison.OrdinalIgnoreCase))
                return theme;
        }

        return null;
    }

    private ThemeRuntimeSaveData EnsureThemeSave(int themeIndex, string themeId)
    {
        while (Runtime.themes.Count <= themeIndex)
        {
            Runtime.themes.Add(new ThemeRuntimeSaveData
            {
                themeId = themeId
            });
        }

        if (string.IsNullOrWhiteSpace(Runtime.themes[themeIndex].themeId))
            Runtime.themes[themeIndex].themeId = themeId;

        return Runtime.themes[themeIndex];
    }

    private BattleThemeConfig DrawTheme()
    {
        if (themeDrawBag.Count == 0)
            RefillThemeBag();

        if (themeDrawBag.Count == 0)
            return null;

        int index = random.Next(0, themeDrawBag.Count);
        var theme = themeDrawBag[index];
        themeDrawBag.RemoveAt(index);
        return theme;
    }

    private void RefillThemeBag()
    {
        if (Config?.allThemes == null)
            return;

        themeDrawBag.Clear();
        foreach (var theme in Config.allThemes)
        {
            if (theme != null)
                themeDrawBag.Add(theme);
        }
    }

    private static string ResolveThemeId(BattleThemeConfig theme, int themeIndex)
    {
        if (theme != null && !string.IsNullOrWhiteSpace(theme.themeId))
            return theme.themeId.Trim();

        return $"debug_theme_{themeIndex + 1}";
    }
}
