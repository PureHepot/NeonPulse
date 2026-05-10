using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalogConfig", menuName = "Game/InRun/Shop Catalog")]
public class ShopCatalogConfig : ScriptableObject
{
    public List<ShopOfferEntry> baseOffers = new();
    public List<ShopOfferEntry> themeOffers = new();
}

[Serializable]
public class ShopOfferEntry
{
    public string offerId;
    public string displayName;
    [TextArea] public string description;
    public int cost = 30;
}
