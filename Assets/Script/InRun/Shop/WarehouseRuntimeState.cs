using UnityEngine;

public static class WarehouseRuntimeState
{
    public const int DefaultCapacity = 12;

    public static void ResetForNewRun(InRunRuntimeSaveData runtime)
    {
        if (runtime == null)
            return;

        runtime.warehouse ??= new WarehouseRuntimeSaveData();
        runtime.warehouse.capacity = DefaultCapacity;
        runtime.warehouse.items.Clear();
    }

    public static int GetCount(InRunRuntimeSaveData runtime)
    {
        return runtime?.warehouse?.items != null ? runtime.warehouse.items.Count : 0;
    }

    public static int GetCapacity(InRunRuntimeSaveData runtime)
    {
        return runtime?.warehouse != null ? Mathf.Max(0, runtime.warehouse.capacity) : 0;
    }

    public static bool HasSpace(InRunRuntimeSaveData runtime)
    {
        return GetCount(runtime) < GetCapacity(runtime);
    }

    public static void ApplyCapacityDelta(InRunRuntimeSaveData runtime, int slotsDelta)
    {
        if (runtime == null)
            return;

        runtime.warehouse ??= new WarehouseRuntimeSaveData();
        runtime.warehouse.capacity = Mathf.Max(0, runtime.warehouse.capacity + slotsDelta);
    }

    public static bool TryAddItem(
        InRunRuntimeSaveData runtime,
        string rewardId,
        InRunItemType itemType,
        string itemId,
        string displayName,
        string description,
        string source)
    {
        if (runtime == null)
            return false;

        runtime.warehouse ??= new WarehouseRuntimeSaveData();
        runtime.warehouse.items ??= new System.Collections.Generic.List<WarehouseItemSaveData>();
        if (!HasSpace(runtime))
            return false;

        runtime.warehouse.items.Add(new WarehouseItemSaveData
        {
            rewardId = rewardId,
            itemType = itemType,
            itemId = itemId,
            displayName = displayName,
            description = description,
            source = source
        });
        return true;
    }
}
