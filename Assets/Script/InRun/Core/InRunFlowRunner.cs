using System.Collections;

public class InRunFlowRunner
{
    public IEnumerator RunFreshStateFlow(InRunDirector director)
    {
        yield return director.EnterState(InRunPhase.Bootstrap);

        if (director.IsBossRushMode)
        {
            // TEMP/BOSS_RUSH_CUT: keep the old loop runner intact and branch here for the deadline-safe boss-only flow.
            int themeIndex = 0;
            while (director.IsPlayerAliveForFlow)
            {
                yield return RunBossRushThemeFresh(director, themeIndex);
                if (!director.IsPlayerAliveForFlow)
                    break;

                themeIndex++;
            }
        }
        else
        {
            for (int themeIndex = 0; themeIndex < director.ThemesPerRun; themeIndex++)
                yield return RunThemeFresh(director, themeIndex);
        }

        yield return director.EnterState(InRunPhase.FinalSettlement);
        yield return director.EnterState(InRunPhase.RunEnded);
        director.MarkStateFlowFinished();
    }

    public IEnumerator RunThemeFresh(InRunDirector director, int themeIndex)
    {
        yield return director.EnterState(InRunPhase.ThemeSelecting);
        director.CurrentTheme = director.RuntimeContext.SelectTheme(themeIndex);
        director.ApplyCurrentThemeVisuals();

        yield return director.EnterState(InRunPhase.ThemeIntro);
        for (int loopIndex = 0; loopIndex < director.LoopsPerTheme; loopIndex++)
            yield return RunLoopFresh(director, loopIndex);

        yield return director.RunBossFresh(themeIndex);
    }

    public IEnumerator RunBossRushThemeFresh(InRunDirector director, int themeIndex)
    {
        yield return director.EnterState(InRunPhase.ThemeSelecting);
        director.CurrentTheme = director.RuntimeContext.SelectTheme(themeIndex);
        director.ApplyCurrentThemeVisuals();

        yield return director.EnterState(InRunPhase.ThemeIntro);
        yield return director.RunBossFresh(themeIndex);
    }

    public IEnumerator RunLoopFresh(InRunDirector director, int loopIndex)
    {
        director.CurrentLoop = director.RuntimeContext.BeginLoop(loopIndex);
        director.CurrentLoop.elapsedSeconds = 0f;
        director.CurrentLoop.pulseUsed = false;
        director.CurrentLoop.rewardClaimed = false;
        director.CurrentLoop.shopCompleted = false;

        yield return director.EnterState(InRunPhase.CombatLoopPreparing);
        yield return director.RunCombatLoop(false);
        yield return director.RunPulseAndReward(false);
    }
}

