using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardPoolConfig", menuName = "Game/InRun/Reward Pool")]
public class RewardPoolConfig : ScriptableObject
{
    public List<RewardEntryConfig> entries = new();
    public List<GradeRewardRule> gradeRules = new();
}

[Serializable]
public class GradeRewardRule
{
    public CombatGrade grade = CombatGrade.C;
    public int offerCount = 2;
    public int picksAllowed = 1;
}
