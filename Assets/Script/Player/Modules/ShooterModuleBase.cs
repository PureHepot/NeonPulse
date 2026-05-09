using System.Collections.Generic;
using UnityEngine;

// Legacy compatibility layer for the old weapon-mod pipeline.
public class ShooterModuleBase : PlayerModule
{
    public List<WeaponPlugin> Mods = new List<WeaponPlugin>();
    public int maxNum = 4;

    protected override void OnActivate()
    {
        ModManager.Instance?.InitForShooter(this);
    }

    public void AddMod(WeaponPlugin mod)
    {
        if (mod == null || Mods.Count >= maxNum || Mods.Contains(mod))
            return;

        Mods.Add(mod);
    }

    public void RemoveMod(WeaponPlugin mod)
    {
        if (mod == null)
            return;

        Mods.Remove(mod);
    }

    public void ApplyAllModsToBullet(PlayerBullet bullet)
    {
        if (bullet == null)
            return;

        for (int index = 0; index < Mods.Count; index++)
        {
            if (Mods[index] != null)
                Mods[index].ModifyBullet(bullet);
        }
    }
}
