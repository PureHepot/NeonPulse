using System.Collections;
using UnityEngine;

public class InRunDirector : MonoBehaviour
{
    [SerializeField] private InRunConfigDatabase configOverride;
    [SerializeField] private int themesPerRun = 3;
    [SerializeField] private int loopsPerTheme = 3;
    [SerializeField] private float debugLoopDurationSeconds = 30f;
    [SerializeField] private float placeholderAdvanceDelaySeconds = 0.35f;
    [SerializeField] private bool showDebugHud = true;

    private readonly CombatLoopController combatLoopController = new();
    private readonly PulseSystem pulseSystem = new();

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
    public string CurrentPulseStatusText => CurrentPhase == InRunPhase.PulseReady
        ? $"Ready [{CurrentPulseKeyName}]"
        : pulseSystem.WasTriggered ? "Triggered" : "Idle";
    public string CurrentPulseKeyName => pulseSystem.PulseKey.ToString();

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
        EnsureHud();
        isSessionActive = true;

        if (stateFlowRoutine != null)
            StopCoroutine(stateFlowRoutine);

        stateFlowRoutine = StartCoroutine(shouldResume ? ResumeStateFlow() : RunFreshStateFlow());
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
    }

    private IEnumerator RunFreshStateFlow()
    {
        yield return EnterState(InRunPhase.Bootstrap);

        for (int themeIndex = 0; themeIndex < themesPerRun; themeIndex++)
            yield return RunThemeFresh(themeIndex);

        yield return EnterState(InRunPhase.FinalSettlement);
        yield return EnterState(InRunPhase.RunEnded);
        stateFlowRoutine = null;
    }

    private IEnumerator ResumeStateFlow()
    {
        switch (CurrentPhase)
        {
            case InRunPhase.Bootstrap:
                yield return EnterState(InRunPhase.Bootstrap);
                yield return ContinueFromTheme(Mathf.Max(0, context.CurrentThemeIndex), InRunPhase.ThemeSelecting, 0);
                break;

            case InRunPhase.ThemeSelecting:
            case InRunPhase.ThemeIntro:
                yield return ContinueFromTheme(Mathf.Max(0, context.CurrentThemeIndex), CurrentPhase, 0);
                break;

            case InRunPhase.CombatLoopPreparing:
            case InRunPhase.CombatLoopActive:
            case InRunPhase.CombatLoopComplete:
            case InRunPhase.PulseReady:
            case InRunPhase.PulseResolving:
            case InRunPhase.LoopReward:
            case InRunPhase.Shop:
                yield return ContinueFromTheme(
                    Mathf.Max(0, context.CurrentThemeIndex),
                    CurrentPhase,
                    Mathf.Clamp(context.CurrentLoopIndex, 0, Mathf.Max(0, loopsPerTheme - 1)));
                break;

            case InRunPhase.BossPreparing:
            case InRunPhase.BossActive:
            case InRunPhase.BossReward:
            case InRunPhase.NextTheme:
                yield return ContinueFromBoss(Mathf.Max(0, context.CurrentThemeIndex), CurrentPhase);
                break;

            case InRunPhase.FinalSettlement:
                yield return EnterState(InRunPhase.FinalSettlement);
                yield return EnterState(InRunPhase.RunEnded);
                break;

            case InRunPhase.RunEnded:
                TransitionTo(InRunPhase.RunEnded);
                break;

            default:
                yield return RunFreshStateFlow();
                yield break;
        }

        stateFlowRoutine = null;
    }

    private IEnumerator ContinueFromTheme(int themeIndex, InRunPhase startPhase, int loopIndex)
    {
        currentTheme = context.GetOrSelectTheme(themeIndex);

        if (startPhase == InRunPhase.ThemeSelecting || startPhase == InRunPhase.ThemeIntro)
        {
            yield return ResumeThemeIntro(themeIndex, startPhase);
            startPhase = InRunPhase.CombatLoopPreparing;
            loopIndex = 0;
        }

        for (int index = loopIndex; index < loopsPerTheme; index++)
        {
            bool isResumeLoop = index == loopIndex;
            yield return isResumeLoop
                ? RunLoopResume(index, startPhase)
                : RunLoopFresh(index);

            startPhase = InRunPhase.CombatLoopPreparing;
        }

        yield return RunBossFresh(themeIndex);
        for (int nextTheme = themeIndex + 1; nextTheme < themesPerRun; nextTheme++)
            yield return RunThemeFresh(nextTheme);

        yield return EnterState(InRunPhase.FinalSettlement);
        yield return EnterState(InRunPhase.RunEnded);
    }

    private IEnumerator ContinueFromBoss(int themeIndex, InRunPhase startPhase)
    {
        currentTheme = context.GetOrSelectTheme(themeIndex);
        yield return RunBossResume(themeIndex, startPhase);

        for (int nextTheme = themeIndex + 1; nextTheme < themesPerRun; nextTheme++)
            yield return RunThemeFresh(nextTheme);

        yield return EnterState(InRunPhase.FinalSettlement);
        yield return EnterState(InRunPhase.RunEnded);
    }

    private IEnumerator RunThemeFresh(int themeIndex)
    {
        yield return EnterState(InRunPhase.ThemeSelecting);
        currentTheme = context.SelectTheme(themeIndex);
        Debug.Log($"[InRunDirector] Selected theme {themeIndex + 1}/{themesPerRun}: {DescribeTheme(currentTheme, themeIndex)}");

        yield return EnterState(InRunPhase.ThemeIntro);
        for (int loopIndex = 0; loopIndex < loopsPerTheme; loopIndex++)
            yield return RunLoopFresh(loopIndex);

        yield return RunBossFresh(themeIndex);
    }

    private IEnumerator ResumeThemeIntro(int themeIndex, InRunPhase startPhase)
    {
        if (startPhase == InRunPhase.ThemeSelecting)
            Debug.Log($"[InRunDirector] Resuming theme selection {themeIndex + 1}/{themesPerRun}: {DescribeTheme(currentTheme, themeIndex)}");

        yield return EnterState(startPhase == InRunPhase.ThemeSelecting ? InRunPhase.ThemeSelecting : InRunPhase.ThemeIntro);
        if (startPhase == InRunPhase.ThemeSelecting)
            yield return EnterState(InRunPhase.ThemeIntro);
    }

    private IEnumerator RunLoopFresh(int loopIndex)
    {
        currentLoop = context.BeginLoop(loopIndex);
        currentLoop.elapsedSeconds = 0f;
        currentLoop.pulseUsed = false;
        currentLoop.rewardClaimed = false;
        currentLoop.shopCompleted = false;

        yield return EnterState(InRunPhase.CombatLoopPreparing);
        yield return RunCombatLoop(false);
        yield return RunPulseAndReward(false);
    }

    private IEnumerator RunLoopResume(int loopIndex, InRunPhase startPhase)
    {
        currentLoop = context.BeginLoop(loopIndex);

        switch (startPhase)
        {
            case InRunPhase.CombatLoopPreparing:
                yield return EnterState(InRunPhase.CombatLoopPreparing);
                yield return RunCombatLoop(false);
                yield return RunPulseAndReward(false);
                break;

            case InRunPhase.CombatLoopActive:
                yield return RunCombatLoop(true);
                yield return RunPulseAndReward(false);
                break;

            case InRunPhase.CombatLoopComplete:
                yield return EnterState(InRunPhase.CombatLoopComplete);
                yield return RunPulseAndReward(false);
                break;

            case InRunPhase.PulseReady:
                yield return RunPulseAndReward(true);
                break;

            case InRunPhase.PulseResolving:
                yield return EnterState(InRunPhase.PulseResolving);
                yield return EnterState(InRunPhase.LoopReward);
                currentLoop.rewardClaimed = true;
                yield return EnterState(InRunPhase.Shop);
                currentLoop.shopCompleted = true;
                break;

            case InRunPhase.LoopReward:
                yield return EnterState(InRunPhase.LoopReward);
                currentLoop.rewardClaimed = true;
                yield return EnterState(InRunPhase.Shop);
                currentLoop.shopCompleted = true;
                break;

            case InRunPhase.Shop:
                yield return EnterState(InRunPhase.Shop);
                currentLoop.shopCompleted = true;
                break;

            default:
                yield return RunLoopFresh(loopIndex);
                break;
        }
    }

    private IEnumerator RunCombatLoop(bool resumeTimer)
    {
        yield return EnterState(InRunPhase.CombatLoopActive, !resumeTimer);
        combatLoopController.StartLoop(currentLoop, ResolveLoopDurationSeconds(), resumeTimer);
        yield return new WaitUntil(() => combatLoopController.IsComplete);
        yield return EnterState(InRunPhase.CombatLoopComplete);
    }

    private IEnumerator RunPulseAndReward(bool resumePulseReady)
    {
        yield return EnterState(InRunPhase.PulseReady, !resumePulseReady);
        pulseSystem.Arm(ResolvePulseConfig(), currentLoop);

        if (currentLoop == null || !currentLoop.pulseUsed)
        {
            yield return new WaitUntil(() => pulseSystem.WasTriggered);
            pulseSystem.ClearTrigger();
        }

        yield return EnterState(InRunPhase.PulseResolving);
        yield return EnterState(InRunPhase.LoopReward);
        currentLoop.rewardClaimed = true;

        yield return EnterState(InRunPhase.Shop);
        currentLoop.shopCompleted = true;
    }

    private IEnumerator RunBossFresh(int themeIndex)
    {
        yield return EnterState(InRunPhase.BossPreparing);
        yield return EnterState(InRunPhase.BossActive);
        context.MarkBossDefeated();
        yield return EnterState(InRunPhase.BossReward);

        if (themeIndex < themesPerRun - 1)
            yield return EnterState(InRunPhase.NextTheme);
    }

    private IEnumerator RunBossResume(int themeIndex, InRunPhase startPhase)
    {
        switch (startPhase)
        {
            case InRunPhase.BossPreparing:
                yield return EnterState(InRunPhase.BossPreparing);
                yield return EnterState(InRunPhase.BossActive);
                context.MarkBossDefeated();
                yield return EnterState(InRunPhase.BossReward);
                break;

            case InRunPhase.BossActive:
                yield return EnterState(InRunPhase.BossActive);
                context.MarkBossDefeated();
                yield return EnterState(InRunPhase.BossReward);
                break;

            case InRunPhase.BossReward:
                context.MarkBossDefeated();
                yield return EnterState(InRunPhase.BossReward);
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

    private IEnumerator EnterState(InRunPhase phase, bool waitAfter = true)
    {
        TransitionTo(phase);
        if (waitAfter && placeholderAdvanceDelaySeconds > 0f)
            yield return new WaitForSeconds(placeholderAdvanceDelaySeconds);
    }

    private void TransitionTo(InRunPhase phase)
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
}
