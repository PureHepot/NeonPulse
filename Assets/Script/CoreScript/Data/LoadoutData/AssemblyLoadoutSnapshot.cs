using System.Collections.Generic;

public sealed class AssemblyLoadoutSnapshot
{
    public string frameId;
    public readonly List<AssemblyLoadoutSlotSnapshot> slots = new();
}

public sealed class AssemblyLoadoutSlotSnapshot
{
    public string slotId;
    public string moduleId;
    public ModuleType moduleType = ModuleType.None;
    public ModuleRarity moduleRarity = ModuleRarity.Common;
    public string coreId;
    public LoadoutModuleRuntimeData runtimeData;

    public bool HasModule => runtimeData != null && runtimeData.HasModule;
}
