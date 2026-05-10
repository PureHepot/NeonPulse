using UnityEngine;

[CreateAssetMenu(fileName = "CombatLoopGlobalConfig", menuName = "Game/InRun/Combat Loop Global Config")]
public class CombatLoopGlobalConfig : ScriptableObject
{
    public float loopDurationSeconds = 240f;
    public AnimationCurve maxTreatCurve = AnimationCurve.Linear(0f, 1f, 1f, 4f);
    public AnimationCurve spawnBudgetPerSecondCurve = AnimationCurve.Linear(0f, 1f, 1f, 4f);
    public AnimationCurve enemyStrengthCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
    public AnimationCurve eliteChanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.25f);
    public float loopDifficultyStep = 0.18f;
    public float themeDifficultyStep = 0.35f;
    public float baseActiveThreatCap = 10f;
    public float spawnInnerPadding = 1.5f;
    public float spawnOuterPadding = 3f;
    public int maxSpawnAttemptsPerTick = 8;
}
