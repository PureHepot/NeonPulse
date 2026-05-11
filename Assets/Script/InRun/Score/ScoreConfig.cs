using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScoreConfig", menuName = "Game/InRun/Score Config")]
public class ScoreConfig : ScriptableObject
{
    public List<GradeThreshold> gradeThresholds = new()
    {
        new GradeThreshold(CombatGrade.F, 0f),
        new GradeThreshold(CombatGrade.D, 0.20f),
        new GradeThreshold(CombatGrade.C, 0.35f),
        new GradeThreshold(CombatGrade.B, 0.50f),
        new GradeThreshold(CombatGrade.A, 0.65f),
        new GradeThreshold(CombatGrade.S, 0.80f),
        new GradeThreshold(CombatGrade.SS, 0.95f),
        new GradeThreshold(CombatGrade.SSS, 1.10f)
    };

    public float comboWindowSeconds = 4f;
    public int killsPerComboStep = 10;
    public float comboMultiplierStep = 0.1f;
    public float maxComboMultiplier = 3f;
    public bool damageBreaksCombo = true;
}

[Serializable]
public struct GradeThreshold
{
    public CombatGrade grade;
    public float minScoreRatio;

    public GradeThreshold(CombatGrade grade, float minScoreRatio)
    {
        this.grade = grade;
        this.minScoreRatio = minScoreRatio;
    }
}
