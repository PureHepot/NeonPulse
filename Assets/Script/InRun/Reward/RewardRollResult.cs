using System;
using System.Collections.Generic;

[Serializable]
public class RewardRollResult
{
    public CombatGrade grade = CombatGrade.F;
    public int picksAllowed = 1;
    public int picksMade;
    public List<RewardChoice> choices = new();
}

[Serializable]
public class RewardChoice
{
    public string rewardId;
    public string itemId;
    public string displayName;
    public string description;
    public InRunItemType itemType = InRunItemType.Misc;
    public RewardRarity rarity = RewardRarity.Common;
    public int currencyBonus;
    public int warehouseSlotsDelta;
    public bool selected;
}
