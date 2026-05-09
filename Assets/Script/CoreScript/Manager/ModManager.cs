using System;
using System.Collections.Generic;
using UnityEngine;

public class ModManager : MonoSingleton<ModManager>
{
    [Header("所有插件配置")]
    public List<WeaponModConfig> allMods;

    // 已拥有插件（类型 + 数量）
    public Dictionary<ModType, int> ownedMods = new Dictionary<ModType, int>();

    // 每把武器当前装备的插件
    public Dictionary<ShooterModuleBase, List<ModType>> equippedMods = new Dictionary<ShooterModuleBase, List<ModType>>();

    // 事件
    public Action OnModUIChanged;
    //开火延长
    public float deFireRate = 1f;

    // ==================== 初始化 ====================
    public void InitForShooter(ShooterModuleBase shooter)
    {
        if (!equippedMods.ContainsKey(shooter))
            equippedMods[shooter] = new List<ModType>();
    }

    // ==================== 获得插件 ====================
    public void AddMod(ModType type, int count = 1)
    {
        if (!ownedMods.ContainsKey(type))
            ownedMods[type] = 0;

        ownedMods[type] += count;
        OnModUIChanged?.Invoke();
    }

    // ==================== 检查能否装备 ====================
    public bool CanEquip(ShooterModuleBase shooter, ModType type)
    {
        // 没有插件
        if (!ownedMods.ContainsKey(type) || ownedMods[type] <= 0)
            return false;

        // 武器插件槽满了
        if (shooter.Mods.Count >= shooter.maxNum)
            return false;

        var config = GetConfig(type);
        var list = equippedMods[shooter];

        // 同插件超过最大数量
        int current = list.FindAll(x => x == type).Count;
        if (current >= config.maxEquipCount)
            return false;

        return true;
    }

    // ==================== 装备插件 ====================
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

    // ==================== 卸下插件 ====================
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

    // ==================== 工具 ====================
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
