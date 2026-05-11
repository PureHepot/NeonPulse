using System;
using System.Collections.Generic;
using UnityEngine;

public class ModManager : MonoSingleton<ModManager>
{
    public List<WeaponModConfig> allMods;
    
    public Dictionary<ModType, int> ownedMods = new Dictionary<ModType, int>();
    
    public Dictionary<ShooterModuleBase, List<ModType>> equippedMods = new Dictionary<ShooterModuleBase, List<ModType>>();
    
    public Action OnModUIChanged;

    public float deFireRate = 1f;

    // 
    public void InitForShooter(ShooterModuleBase shooter)
    {
        if (!equippedMods.ContainsKey(shooter))
            equippedMods[shooter] = new List<ModType>();
    }
    
    public void AddMod(ModType type, int count = 1)
    {
        if (!ownedMods.ContainsKey(type))
            ownedMods[type] = 0;

        ownedMods[type] += count;
        OnModUIChanged?.Invoke();
    }

    
    public bool CanEquip(ShooterModuleBase shooter, ModType type)
    {
        if (!ownedMods.ContainsKey(type) || ownedMods[type] <= 0)
            return false;
        
        if (shooter.Mods.Count >= shooter.maxNum)
            return false;

        var config = GetConfig(type);
        var list = equippedMods[shooter];
        
        int current = list.FindAll(x => x == type).Count;
        if (current >= config.maxEquipCount)
            return false;

        return true;
    }
    
    public bool EquipMod(ShooterModuleBase shooter, ModType type)
    {
        if (!CanEquip(shooter, type)) return false;

        var config = GetConfig(type);
        var ModInstance = Instantiate(config.ModPrefab, shooter.transform);
        var Mod = ModInstance.GetComponent<WeaponPlugin>();

        if (Mod == null)
        {
            Destroy(ModInstance);
            return false;
        }

        shooter.AddMod(Mod);
        ownedMods[type]--;
        equippedMods[shooter].Add(type);
        OnModUIChanged?.Invoke();
        return true;
    }
    
    public bool UnequipMod(ShooterModuleBase shooter, WeaponPlugin Mod)
    {
        var config = FindConfigByMod(Mod);
        if (config == null) return false;

        shooter.RemoveMod(Mod);
        Destroy(Mod.gameObject);

        ownedMods[config.ModType]++;
        equippedMods[shooter].Remove(config.ModType);
        OnModUIChanged?.Invoke();
        return true;
    }
    
    public WeaponModConfig GetConfig(ModType type)
    {
        return allMods.Find(c => c.ModType == type);
    }

    public WeaponModConfig FindConfigByMod(WeaponPlugin Mod)
    {
        foreach (var c in allMods)
        {
            if (c.ModPrefab.GetComponent<WeaponPlugin>() != null &&
                c.ModPrefab.GetComponent<WeaponPlugin>().GetType() == Mod.GetType())
                return c;
        }
        return null;
    }

    public int GetModCount(ModType type)
    {
        return ownedMods.TryGetValue(type, out int v) ? v : 0;
    }

    public List<ModType> GetEquippedMods(ShooterModuleBase shooter)
    {
        return equippedMods.TryGetValue(shooter, out var list) ? list : new List<ModType>();
    }

    public List<WeaponModConfig> GetAllOwnedMods()
    {
        var list = new List<WeaponModConfig>();
        foreach (var kvp in ownedMods)
        {
            if (kvp.Value > 0)
                list.Add(GetConfig(kvp.Key));
        }
        return list;
    }
}
