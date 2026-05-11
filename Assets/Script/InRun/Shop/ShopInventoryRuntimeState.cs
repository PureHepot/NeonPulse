using System.Collections.Generic;

public static class ShopInventoryRuntimeState
{
    public static void Clear(InRunRuntimeSaveData runtime)
    {
        if (runtime == null)
            return;

        runtime.shopInventory ??= new ShopInventoryRuntimeSaveData();
        runtime.shopInventory.catalogId = string.Empty;
        runtime.shopInventory.offers.Clear();
    }

    public static bool TryRestore(string catalogId, InRunRuntimeSaveData runtime, List<ShopOffer> targetOffers)
    {
        targetOffers.Clear();
        if (runtime?.shopInventory == null || runtime.shopInventory.offers == null)
            return false;

        if (string.IsNullOrWhiteSpace(catalogId) || !string.Equals(runtime.shopInventory.catalogId, catalogId))
            return false;

        foreach (ShopOfferSaveData saved in runtime.shopInventory.offers)
        {
            if (saved == null)
                continue;

            targetOffers.Add(new ShopOffer
            {
                offerId = saved.offerId,
                displayName = saved.displayName,
                description = saved.description,
                cost = saved.cost,
                itemType = saved.itemType,
                itemId = saved.itemId,
                warehouseSlotsDelta = saved.warehouseSlotsDelta,
                purchased = saved.purchased
            });
        }

        return targetOffers.Count > 0;
    }

    public static void Snapshot(string catalogId, InRunRuntimeSaveData runtime, IReadOnlyList<ShopOffer> offers)
    {
        if (runtime == null)
            return;

        runtime.shopInventory ??= new ShopInventoryRuntimeSaveData();
        runtime.shopInventory.catalogId = catalogId ?? string.Empty;
        runtime.shopInventory.offers.Clear();

        if (offers == null)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            if (offer == null)
                continue;

            runtime.shopInventory.offers.Add(new ShopOfferSaveData
            {
                offerId = offer.offerId,
                displayName = offer.displayName,
                description = offer.description,
                cost = offer.cost,
                itemType = offer.itemType,
                itemId = offer.itemId,
                warehouseSlotsDelta = offer.warehouseSlotsDelta,
                purchased = offer.purchased
            });
        }
    }
}
