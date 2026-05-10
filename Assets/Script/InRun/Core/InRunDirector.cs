using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InRunDirector : MonoBehaviour
{
    public static InRunDirector ActiveInstance { get; private set; }

    [SerializeField] private InRunConfigDatabase configOverride;
    [SerializeField] private int themesPerRun = 3;
    [SerializeField] private int loopsPerTheme = 3;
    [SerializeField] private float debugLoopDurationSeconds = 0f; //改成0就是不进行Debug测试
    [SerializeField] private float placeholderAdvanceDelaySeconds = 0.35f;
    [SerializeField] private bool showDebugHud = true;

    private readonly CombatLoopController combatLoopController = new();
    private readonly PulseSystem pulseSystem = new();
    private readonly EnemySpawnDirector enemySpawnDirector = new();
    private readonly BossEncounterDirector bossEncounterDirector = new();
    private readonly RewardDirector rewardDirector = new();
    private readonly ShopDirector shopDirector = new();
    private readonly InRunFlowRunner flowRunner = new();
    private readonly InRunResumeCoordinator resumeCoordinator = new();

    private Coroutine stateFlowRoutine;
    private InRunRuntimeContext context;
    private BattleThemeConfig currentTheme;
    private CombatLoopRuntimeSaveData currentLoop;
    private InRunHUD hud;
    private bool isSessionActive;

    public static InRunDirector GetOrCreate()
    {
        var existing = FindObjectOfType<InRunDirector>();
        if (existing != null)
            return existing;

        var root = new GameObject(nameof(InRunDirector));
        root.transform.SetParent(GameMgr.Instance.transform, false);
        return root.AddComponent<InRunDirector>();
    }

    public InRunPhase CurrentPhase => context != null ? context.CurrentPhase : InRunPhase.None;
    public bool IsHudVisible => showDebugHud && isSessionActive;
    public string CurrentThemeId => context != null ? context.GetCurrentThemeId() : string.Empty;
    public string CurrentThemeLabel => GetDisplayIndex(context != null ? context.CurrentThemeIndex : -1, themesPerRun);
    public string CurrentLoopLabel => GetDisplayIndex(context != null ? context.CurrentLoopIndex : -1, loopsPerTheme);
    public string CurrentLoopTimerText => $"{FormatSeconds(combatLoopController.RemainingSeconds)} / {FormatSeconds(combatLoopController.DurationSeconds)}";
    public string CurrentPulseStatusText => pulseSystem.WasTriggered
        ? "Triggered"
        : pulseSystem.IsArmed ? $"Armed [{CurrentPulseKeyName}]" : "Idle";
    public string CurrentPulseKeyName => pulseSystem.PulseKey.ToString();
    public int CurrentActiveEnemyCount => enemySpawnDirector.ActiveEnemyCount;
    public float CurrentActiveThreat => enemySpawnDirector.CurrentActiveThreat;
    public int CurrentLoopScore => currentLoop != null ? currentLoop.loopScoreRaw : 0;
    public int CurrentLoopCurrencyGain => currentLoop != null ? currentLoop.loopCurrencyGain : 0;
    public CombatGrade CurrentLoopGrade => currentLoop != null ? currentLoop.grade : CombatGrade.F;
    public int CurrentRunCurrency => context != null ? context.Runtime.runCurrency : 0;
    public int CurrentPendingRewardCount => context != null ? context.Runtime.pendingRewards.Count : 0;
    public RewardRollResult CurrentRewardResult => rewardDirector.CurrentResult;
    public IReadOnlyList<ShopOffer> CurrentShopOffers => shopDirector.CurrentOffers;
    public string CurrentBossName => bossEncounterDirector.CurrentBossName;
    public bool IsBossEncounterRunning => bossEncounterDirector.IsRunning;
    internal InRunFlowRunner FlowRunner => flowRunner;
    internal int ThemesPerRun => themesPerRun;
    internal int LoopsPerTheme => loopsPerTheme;
    internal InRunRuntimeContext RuntimeContext => context;
    internal BattleThemeConfig CurrentTheme { get => currentTheme; set => currentTheme = value; }
    internal CombatLoopRuntimeSaveData CurrentLoop { get => currentLoop; set => currentLoop = value; }

    private void OnEnable()
    {
        ActiveInstance = this;
    }

    private void OnDisable()
    {
        if (ActiveInstance == this)
            ActiveInstance = null;
    }

    public void BeginRun(bool resumeExistingRun = false)
    {
        var data = GameMgr.Instance.Data;
        if (data == null || !data.HasActiveRun)
        {
            Debug.LogWarning("[InRunDirector] Cannot begin run because there is no active run snapshot.");
            return;
        }

        var config = configOverride != null ? configOverride : InRunConfigDatabase.Instance;
        context = new InRunRuntimeContext(data.Run, config);

        bool shouldResume = resumeExistingRun && context.HasSavedProgress;
        if (!shouldResume)
            context.InitializeForRun();

        currentTheme = null;
        currentLoop = null;
        combatLoopController.Reset();
        pulseSystem.Reset();
        enemySpawnDirector.Reset();
        bossEncounterDirector.Reset();
        rewardDirector.Reset();
        shopDirector.Reset();
        EnsureHud();
        isSessionActive = true;

        if (stateFlowRoutine != null)
            StopCoroutine(stateFlowRoutine);

        stateFlowRoutine = StartCoroutine(shouldResume
            ? resumeCoordinator.ResumeStateFlow(this)
            : flowRunner.RunFreshStateFlow(this));
    }

    public void EndRunSession()
    {
        if (stateFlowRoutine != null)
        {
            StopCoroutine(stateFlowRoutine);
            stateFlowRoutine = null;
        }

        combatLoopController.Reset();
        pulseSystem.Reset();
        enemySpawnDirector.Reset();
        bossEncounterDirector.Reset();
        rewardDirector.Reset();
        shopDirector.Reset();
        currentTheme = null;
        currentLoop = null;
        isSessionActive = false;
    }

    private void Update()
    {
        if (!isSessionActive)
            return;

        combatLoopController.Tick(Time.deltaTime);
        pulseSystem.Tick();

        if (CurrentPhase == InRunPhase.CombatLoopActive)
            enemySpawnDirector.Tick(Time.deltaTime, combatLoopController.NormalizedTime);
        else if (CurrentPhase == InRunPhase.BossActive)
            bossEncounterDirector.Tick();
        else if (CurrentPhase == InRunPhase.LoopReward || CurrentPhase == InRunPhase.BossReward)
            rewardDirector.Tick(context != null ? context.Runtime : null);
        else if (CurrentPhase == InRunPhase.Shop)
            shopDirector.Tick(context != null ? context.Runtime : null);
    }

    public void NotifyEnemyKilled(EnemyBase enemy)
    {
        if (!isSessionActive || enemy == null || currentLoop == null || context == null)
            return;

        if (CurrentPhase != InRunPhase.CombatLoopActive)
            return;

        currentLoop.loopScoreRaw += Mathf.Max(0, enemy.scoreValue);
        currentLoop.killCount++;
        currentLoop.highestMultiplier = Mathf.Max(1f, currentLoop.highestMultiplier);
        context.Runtime.lifetimeKillsThisRun++;
    }

    internal IEnumerator ResumeThemeIntro(int themeIndex, InRunPhase startPhase)
    {
        if (startPhase == InRunPhase.ThemeSelecting)
            Debug.Log($"[InRunDirector] Resuming theme selection {themeIndex + 1}/{themesPerRun}: {DescribeTheme(currentTheme, themeIndex)}");

        ApplyCurrentThemeVisuals();
        yield return EnterState(startPhase == InRunPhase.ThemeSelecting ? InRunPhase.ThemeSelecting : InRunPhase.ThemeIntro);
        if (startPhase == InRunPhase.ThemeSelecting)
            yield return EnterState(InRunPhase.ThemeIntro);
    }

    internal IEnumerator RunCombatLoop(bool resumeTimer)
    {
        yield return EnterState(InRunPhase.CombatLoopActive, !resumeTimer);
        combatLoopController.StartLoop(currentLoop, ResolveLoopDurationSeconds(), resumeTimer);
        pulseSystem.Arm(ResolvePulseConfig(), currentLoop);
        enemySpawnDirector.BeginLoop(
            currentTheme,
            ResolveLoopGlobalConfig(),
            context != null ? Mathf.Max(0, context.CurrentThemeIndex) : 0,
            context != null ? Mathf.Max(0, context.CurrentLoopIndex) : 0);

        yield return new WaitUntil(() => combatLoopController.IsComplete || pulseSystem.WasTriggered);

        if (pulseSystem.WasTriggered && !combatLoopController.IsComplete)
            combatLoopController.CompleteNow();

        enemySpawnDirector.StopLoop();
        yield return EnterState(InRunPhase.CombatLoopComplete);
    }

    internal IEnumerator RunPulseAndReward(bool resumePulseReady)
    {
        bool pulseAlreadyTriggered = currentLoop != null && currentLoop.pulseUsed;
        if (!pulseAlreadyTriggered)
        {
            yield return EnterState(InRunPhase.PulseReady, !resumePulseReady);
            pulseSystem.Arm(ResolvePulseConfig(), currentLoop);
            yield return new WaitUntil(() => pulseSystem.WasTriggered);
        }

        yield return EnterState(InRunPhase.PulseResolving);
        pulseSystem.ClearTrigger();
        enemySpawnDirector.DespawnAllTrackedEnemies();
        yield return RunLoopRewardPhase(false);
        yield return RunShopPhase(false);
    }

    internal IEnumerator RunBossFresh(int themeIndex)
    {
        yield return EnterState(InRunPhase.BossPreparing);
        enemySpawnDirector.DespawnAllTrackedEnemies();
        bossEncounterDirector.BeginEncounter(currentTheme, context != null ? context.CurrentThemeIndex : 0);
        yield return EnterState(InRunPhase.BossActive);
        yield return new WaitUntil(() => bossEncounterDirector.IsComplete);
        bossEncounterDirector.CleanupEncounter();
        context.MarkBossDefeated();
        yield return RunBossRewardPhase(false);

        if (themeIndex < themesPerRun - 1)
            yield return EnterState(InRunPhase.NextTheme);
    }

    internal IEnumerator RunBossResume(int themeIndex, InRunPhase startPhase)
    {
        switch (startPhase)
        {
            case InRunPhase.BossPreparing:
                yield return EnterState(InRunPhase.BossPreparing);
                bossEncounterDirector.BeginEncounter(currentTheme, context != null ? context.CurrentThemeIndex : 0);
                yield return EnterState(InRunPhase.BossActive);
                yield return new WaitUntil(() => bossEncounterDirector.IsComplete);
                bossEncounterDirector.CleanupEncounter();
                context.MarkBossDefeated();
                yield return RunBossRewardPhase(false);
                break;

            case InRunPhase.BossActive:
                yield return EnterState(InRunPhase.BossActive);
                bossEncounterDirector.BeginEncounter(currentTheme, context != null ? context.CurrentThemeIndex : 0);
                yield return new WaitUntil(() => bossEncounterDirector.IsComplete);
                bossEncounterDirector.CleanupEncounter();
                context.MarkBossDefeated();
                yield return RunBossRewardPhase(false);
                break;

            case InRunPhase.BossReward:
                context.MarkBossDefeated();
                yield return RunBossRewardPhase(true);
                break;

            case InRunPhase.NextTheme:
                yield return EnterState(InRunPhase.NextTheme);
                yield break;

            default:
                yield return RunBossFresh(themeIndex);
                yield break;
        }

        if (themeIndex < themesPerRun - 1)
            yield return EnterState(InRunPhase.NextTheme);
    }

    internal IEnumerator EnterState(InRunPhase phase, bool waitAfter = true)
    {
        TransitionTo(phase);
        if (waitAfter && placeholderAdvanceDelaySeconds > 0f)
            yield return new WaitForSeconds(placeholderAdvanceDelaySeconds);
    }

    internal void TransitionTo(InRunPhase phase)
    {
        if (context == null)
            return;

        context.SetPhase(phase);
        Debug.Log($"[InRunDirector] State -> {phase} | Theme {GetDisplayIndex(context.CurrentThemeIndex, themesPerRun)} | Loop {GetDisplayIndex(context.CurrentLoopIndex, loopsPerTheme)}");
    }

    private void EnsureHud()
    {
        if (hud == null)
            hud = GetComponent<InRunHUD>() ?? gameObject.AddComponent<InRunHUD>();

        hud.Bind(this);
    }

    private float ResolveLoopDurationSeconds()
    {
        if (debugLoopDurationSeconds > 0f)
            return debugLoopDurationSeconds;

        var config = configOverride != null ? configOverride : InRunConfigDatabase.Instance;
        if (config != null && config.loopGlobalConfig != null)
            return config.loopGlobalConfig.loopDurationSeconds;

        return 240f;
    }

    private PulseConfig ResolvePulseConfig()
    {
        var config = configOverride != null ? configOverride : InRunConfigDatabase.Instance;
        return config != null ? config.pulseConfig : null;
    }

    private ScoreConfig ResolveScoreConfig()
    {
        var config = configOverride != null ? configOverride : InRunConfigDatabase.Instance;
        return config != null ? config.scoreConfig : null;
    }

    private CombatLoopGlobalConfig ResolveLoopGlobalConfig()
    {
        var config = configOverride != null ? configOverride : InRunConfigDatabase.Instance;
        return config != null ? config.loopGlobalConfig : null;
    }

    private static string DescribeTheme(BattleThemeConfig theme, int themeIndex)
    {
        if (theme == null)
            return $"debug_theme_{themeIndex + 1}";

        if (!string.IsNullOrWhiteSpace(theme.displayName))
            return $"{theme.displayName} ({theme.themeId})";

        if (!string.IsNullOrWhiteSpace(theme.themeId))
            return theme.themeId;

        return $"debug_theme_{themeIndex + 1}";
    }

    internal void ApplyCurrentThemeVisuals()
    {
        if (currentTheme == null || currentTheme.backgroundPreset == null || BackgroundFXController.Instance == null)
            return;

        BackgroundFXController.Instance.ApplyPresetCollection(currentTheme.backgroundPreset);
    }

    internal IEnumerator RunLoopRewardPhase(bool resumeOpen)
    {
        if (currentLoop == null || context == null)
            yield break;

        yield return EnterState(InRunPhase.LoopReward, !resumeOpen);
        rewardDirector.OpenLoopReward(
            currentTheme,
            currentLoop,
            ResolveScoreConfig(),
            context.CurrentThemeIndex,
            context.CurrentLoopIndex,
            context.Runtime);
        yield return new WaitUntil(() => rewardDirector.IsComplete);
        currentLoop.rewardClaimed = true;
    }

    internal IEnumerator RunShopPhase(bool resumeOpen)
    {
        if (currentLoop == null || context == null)
            yield break;

        yield return EnterState(InRunPhase.Shop, !resumeOpen);
        shopDirector.OpenShop(currentTheme, context.Runtime);
        yield return new WaitUntil(() => shopDirector.IsComplete);
        currentLoop.shopCompleted = true;
    }

    internal IEnumerator RunBossRewardPhase(bool resumeOpen)
    {
        if (context == null)
            yield break;

        yield return EnterState(InRunPhase.BossReward, !resumeOpen);
        rewardDirector.OpenBossReward(currentTheme, context.Runtime);
        yield return new WaitUntil(() => rewardDirector.IsComplete);
    }

    private static string GetDisplayIndex(int zeroBasedIndex, int totalCount)
    {
        if (zeroBasedIndex < 0)
            return $"-/{totalCount}";

        return $"{zeroBasedIndex + 1}/{totalCount}";
    }

    private static string FormatSeconds(float seconds)
    {
        int clampedSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = clampedSeconds / 60;
        int remainingSeconds = clampedSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    internal void MarkStateFlowFinished()
    {
        stateFlowRoutine = null;
    }
}
