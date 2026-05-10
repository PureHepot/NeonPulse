using System.Collections.Generic;
using UnityEngine;

public class ShopDirector
{
    private readonly List<ShopOffer> currentOffers = new();

    public IReadOnlyList<ShopOffer> CurrentOffers => currentOffers;
    public bool IsComplete { get; private set; }

    public void OpenShop(BattleThemeConfig theme, InRunRuntimeSaveData runtime)
    {
        currentOffers.Clear();
        IsComplete = false;

        var catalog = theme != null ? theme.shopCatalog : null;
        string catalogId = ResolveCatalogId(catalog);
        if (runtime != null && ShopInventoryRuntimeState.TryRestore(catalogId, runtime, currentOffers))
            return;

        if (catalog != null)
        {
            AddOffers(catalog.baseOffers);
            AddOffers(catalog.themeOffers);
        }

        if (currentOffers.Count == 0)
        {
            currentOffers.Add(new ShopOffer
            {
                offerId = "shop_hull_patch",
                displayName = "Hull Patch",
                description = "Placeholder shop item for future HP repair integration.",
                cost = 35,
                itemType = InRunItemType.Repair,
                itemId = "shop_hull_patch"
            });
            currentOffers.Add(new ShopOffer
            {
                offerId = "shop_target_cache",
                displayName = "Target Cache",
                description = "Placeholder offense upgrade pack.",
                cost = 60,
                itemType = InRunItemType.Module,
                itemId = "shop_target_cache"
            });
            currentOffers.Add(new ShopOffer
            {
                offerId = "shop_map_key",
                displayName = "Map Key",
                description = "Placeholder map expansion purchase.",
                cost = 80,
                itemType = InRunItemType.MapExpansion,
                itemId = "shop_map_key"
            });
        }

        ShopInventoryRuntimeState.Snapshot(catalogId, runtime, currentOffers);
    }

    public void Tick(InRunRuntimeSaveData runtime)
    {
        if (IsComplete || runtime == null)
            return;

        for (int i = 0; i < currentOffers.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                Purchase(i, runtime);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.N))
            IsComplete = true;
    }

    public void Purchase(int index, InRunRuntimeSaveData runtime)
    {
        if (IsComplete || runtime == null)
            return;

        if (index < 0 || index >= currentOffers.Count)
            return;

        var offer = currentOffers[index];
        if (offer == null || offer.purchased || runtime.runCurrency < offer.cost)
            return;

        if (!CanPurchase(offer, runtime))
            return;

        runtime.runCurrency -= offer.cost;
        offer.purchased = true;
        runtime.pendingRewards.Add(new RunRewardSaveData
        {
            rewardId = offer.offerId,
            itemId = offer.itemId,
            displayName = offer.displayName,
            description = offer.description,
            source = "Shop",
            currencyBonus = 0,
            itemType = offer.itemType,
            warehouseSlotsDelta = offer.warehouseSlotsDelta
        });

        if (offer.warehouseSlotsDelta != 0)
            WarehouseRuntimeState.ApplyCapacityDelta(runtime, offer.warehouseSlotsDelta);

        if (offer.itemType != InRunItemType.Currency)
        {
            WarehouseRuntimeState.TryAddItem(
                runtime,
                offer.offerId,
                offer.itemType,
                offer.itemId,
                offer.displayName,
                offer.description,
                "Shop");
        }

        ShopInventoryRuntimeState.Snapshot(runtime.shopInventory != null ? runtime.shopInventory.catalogId : string.Empty, runtime, currentOffers);
    }

    public void Reset()
    {
        currentOffers.Clear();
        IsComplete = false;
    }

    private void AddOffers(List<ShopOfferEntry> entries)
    {
        if (entries == null)
            return;

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            currentOffers.Add(new ShopOffer
            {
                offerId = entry.offerId,
                itemId = string.IsNullOrWhiteSpace(entry.itemId) ? entry.offerId : entry.itemId,
                displayName = entry.displayName,
                description = entry.description,
                cost = entry.cost,
                itemType = entry.itemType,
                warehouseSlotsDelta = entry.warehouseSlotsDelta
            });
        }
    }

    private static bool CanPurchase(ShopOffer offer, InRunRuntimeSaveData runtime)
    {
        if (offer == null || runtime == null)
            return false;

        if (offer.warehouseSlotsDelta > 0)
            return true;

        if (offer.itemType == InRunItemType.Currency)
            return true;

        return WarehouseRuntimeState.HasSpace(runtime);
    }

    private static string ResolveCatalogId(ShopCatalogConfig catalog)
    {
        if (catalog == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(catalog.catalogId))
            return catalog.catalogId.Trim();

        return catalog.name;
    }
}
