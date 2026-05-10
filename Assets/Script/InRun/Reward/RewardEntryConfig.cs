using UnityEngine;

[CreateAssetMenu(fileName = "RewardEntry", menuName = "Game/InRun/Reward Entry")]
public class RewardEntryConfig : ScriptableObject
{
    public string rewardId;
    public string itemId;
    public string displayName;
    [TextArea] public string description;
    public InRunItemType itemType = InRunItemType.Misc;
    public RewardRarity rarity = RewardRarity.Common;
    public int currencyBonus;
    public int warehouseSlotsDelta;
    public float weight = 1f;
}

public enum RewardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}
