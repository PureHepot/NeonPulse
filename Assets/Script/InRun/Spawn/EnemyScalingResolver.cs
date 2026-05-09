using UnityEngine;

public static class EnemyScalingResolver
{
    public static EnemyRuntimeSpawnData Build(
        EnemySpawnEntry entry,
        CombatLoopGlobalConfig loopConfig,
        BattleThemeConfig themeConfig,
        int themeIndex,
        int loopIndex,
        float normalizedTime)
    {
        float loopScale = 1f + Mathf.Max(0, loopIndex) * loopConfig.loopDifficultyStep;
        float themeScale = 1f + Mathf.Max(0, themeIndex) * loopConfig.themeDifficultyStep;
        float timeStrength = loopConfig.enemyStrengthCurve != null
            ? loopConfig.enemyStrengthCurve.Evaluate(Mathf.Clamp01(normalizedTime))
            : 1f;
        float themeDifficulty = themeConfig != null ? Mathf.Max(0.1f, themeConfig.difficultyMultiplier) : 1f;
        float totalScale = timeStrength * loopScale * themeScale * themeDifficulty;

        return new EnemyRuntimeSpawnData
        {
            enemyId = entry != null ? entry.enemyId : string.Empty,
            hpMultiplier = Mathf.Max(0.5f, totalScale),
            moveSpeedMultiplier = Mathf.Lerp(1f, totalScale, 0.35f),
            scoreMultiplier = Mathf.Max(1f, Mathf.Lerp(1f, totalScale, 0.5f)),
            threatMultiplier = Mathf.Max(1f, Mathf.Lerp(1f, totalScale, 0.25f)),
            themeIndex = themeIndex,
            loopIndex = loopIndex,
            normalizedTime = Mathf.Clamp01(normalizedTime)
        };
    }
}
