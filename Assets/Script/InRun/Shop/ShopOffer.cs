using System;

[Serializable]
public class ShopOffer
{
    public string offerId;
    public string itemId;
    public string displayName;
    public string description;
    public int cost;
    public InRunItemType itemType = InRunItemType.Misc;
    public int warehouseSlotsDelta;
    public bool purchased;
}
