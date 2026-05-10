using System.Collections.Generic;
using UnityEngine;

public static class ScoreResolver
{
    public static CombatGrade ResolveGrade(
        CombatLoopRuntimeSaveData loop,
        ScoreConfig scoreConfig,
        BattleThemeConfig theme,
        int themeIndex,
        int loopIndex)
    {
        if (loop == null)
            return CombatGrade.F;

        float expectedScore = CalculateExpectedScore(theme, themeIndex, loopIndex);
        float scoreRatio = expectedScore > 0f ? loop.loopScoreRaw / expectedScore : 0f;

        CombatGrade resolvedGrade = CombatGrade.F;
        List<GradeThreshold> thresholds = scoreConfig != null ? scoreConfig.gradeThresholds : null;
        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
            {
                if (scoreRatio >= threshold.minScoreRatio)
                    resolvedGrade = threshold.grade;
            }
        }

        return resolvedGrade;
    }

    public static float CalculateExpectedScore(BattleThemeConfig theme, int themeIndex, int loopIndex)
    {
        float averageBaseScore = 12f;
        if (theme != null && theme.enemyPool != null && theme.enemyPool.Count > 0)
        {
            float scoreSum = 0f;
            int count = 0;
            foreach (var entry in theme.enemyPool)
            {
                if (entry == null)
                    continue;

                scoreSum += Mathf.Max(1, entry.baseScore);
                count++;
            }

            if (count > 0)
                averageBaseScore = scoreSum / count;
        }

        float loopScale = 1f + Mathf.Max(0, loopIndex) * 0.2f;
        float themeScale = 1f + Mathf.Max(0, themeIndex) * 0.25f;
        float themeDifficulty = theme != null ? Mathf.Max(0.1f, theme.difficultyMultiplier) : 1f;
        return averageBaseScore * 18f * loopScale * themeScale * themeDifficulty;
    }
}
