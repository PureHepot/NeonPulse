using System.Collections;
using UnityEngine;

public class InRunResumeCoordinator
{
    public IEnumerator ResumeStateFlow(InRunDirector director)
    {
        switch (director.CurrentPhase)
        {
            case InRunPhase.Bootstrap:
                yield return director.EnterState(InRunPhase.Bootstrap);
                yield return ContinueFromTheme(director, Mathf.Max(0, director.RuntimeContext.CurrentThemeIndex), InRunPhase.ThemeSelecting, 0);
                break;

            case InRunPhase.ThemeSelecting:
            case InRunPhase.ThemeIntro:
                yield return ContinueFromTheme(director, Mathf.Max(0, director.RuntimeContext.CurrentThemeIndex), director.CurrentPhase, 0);
                break;

            case InRunPhase.CombatLoopPreparing:
            case InRunPhase.CombatLoopActive:
            case InRunPhase.CombatLoopComplete:
            case InRunPhase.PulseReady:
            case InRunPhase.PulseResolving:
            case InRunPhase.LoopReward:
            case InRunPhase.Shop:
                yield return ContinueFromTheme(
                    director,
                    Mathf.Max(0, director.RuntimeContext.CurrentThemeIndex),
                    director.CurrentPhase,
                    Mathf.Clamp(director.RuntimeContext.CurrentLoopIndex, 0, Mathf.Max(0, director.LoopsPerTheme - 1)));
                break;

            case InRunPhase.BossPreparing:
            case InRunPhase.BossActive:
            case InRunPhase.BossReward:
            case InRunPhase.NextTheme:
                yield return ContinueFromBoss(director, Mathf.Max(0, director.RuntimeContext.CurrentThemeIndex), director.CurrentPhase);
                break;

            case InRunPhase.FinalSettlement:
                yield return director.EnterState(InRunPhase.FinalSettlement);
                yield return director.EnterState(InRunPhase.RunEnded);
                break;

            case InRunPhase.RunEnded:
                director.TransitionTo(InRunPhase.RunEnded);
                break;

            default:
                yield return director.FlowRunner.RunFreshStateFlow(director);
                yield break;
        }

        director.MarkStateFlowFinished();
    }

    private IEnumerator ContinueFromTheme(InRunDirector director, int themeIndex, InRunPhase startPhase, int loopIndex)
    {
        director.CurrentTheme = director.RuntimeContext.GetOrSelectTheme(themeIndex);

        if (startPhase == InRunPhase.ThemeSelecting || startPhase == InRunPhase.ThemeIntro)
        {
            yield return director.ResumeThemeIntro(themeIndex, startPhase);
            startPhase = InRunPhase.CombatLoopPreparing;
            loopIndex = 0;
        }

        for (int index = loopIndex; index < director.LoopsPerTheme; index++)
        {
            bool isResumeLoop = index == loopIndex;
            yield return isResumeLoop
                ? RunLoopResume(director, index, startPhase)
                : director.FlowRunner.RunLoopFresh(director, index);

            startPhase = InRunPhase.CombatLoopPreparing;
        }

        yield return director.RunBossFresh(themeIndex);
        for (int nextTheme = themeIndex + 1; nextTheme < director.ThemesPerRun; nextTheme++)
            yield return director.FlowRunner.RunThemeFresh(director, nextTheme);

        yield return director.EnterState(InRunPhase.FinalSettlement);
        yield return director.EnterState(InRunPhase.RunEnded);
    }

    private IEnumerator ContinueFromBoss(InRunDirector director, int themeIndex, InRunPhase startPhase)
    {
        director.CurrentTheme = director.RuntimeContext.GetOrSelectTheme(themeIndex);
        yield return director.RunBossResume(themeIndex, startPhase);

        for (int nextTheme = themeIndex + 1; nextTheme < director.ThemesPerRun; nextTheme++)
            yield return director.FlowRunner.RunThemeFresh(director, nextTheme);

        yield return director.EnterState(InRunPhase.FinalSettlement);
        yield return director.EnterState(InRunPhase.RunEnded);
    }

    private IEnumerator RunLoopResume(InRunDirector director, int loopIndex, InRunPhase startPhase)
    {
        director.CurrentLoop = director.RuntimeContext.BeginLoop(loopIndex);

        switch (startPhase)
        {
            case InRunPhase.CombatLoopPreparing:
                yield return director.EnterState(InRunPhase.CombatLoopPreparing);
                yield return director.RunCombatLoop(false);
                yield return director.RunPulseAndReward(false);
                break;

            case InRunPhase.CombatLoopActive:
                yield return director.RunCombatLoop(true);
                yield return director.RunPulseAndReward(false);
                break;

            case InRunPhase.CombatLoopComplete:
                yield return director.EnterState(InRunPhase.CombatLoopComplete);
                yield return director.RunPulseAndReward(false);
                break;

            case InRunPhase.PulseReady:
                yield return director.RunPulseAndReward(true);
                break;

            case InRunPhase.PulseResolving:
                yield return director.EnterState(InRunPhase.PulseResolving);
                yield return director.RunLoopRewardPhase(false);
                yield return director.RunShopPhase(false);
                break;

            case InRunPhase.LoopReward:
                yield return director.RunLoopRewardPhase(true);
                yield return director.RunShopPhase(false);
                break;

            case InRunPhase.Shop:
                yield return director.RunShopPhase(true);
                break;

            default:
                yield return director.FlowRunner.RunLoopFresh(director, loopIndex);
                break;
        }
    }
}
