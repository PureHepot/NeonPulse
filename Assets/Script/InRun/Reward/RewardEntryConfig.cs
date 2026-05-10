using UnityEngine;

[CreateAssetMenu(fileName = "RewardEntry", menuName = "Game/InRun/Reward Entry")]
public class RewardEntryConfig : ScriptableObject
{
    public string rewardId;
    public string displayName;
    [TextArea] public string description;
    public RewardRarity rarity = RewardRarity.Common;
    public int currencyBonus;
    public float weight = 1f;
}

public enum RewardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}
