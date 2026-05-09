using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterModuleBase : PlayerModule
{
    public List<WeaponPlugin> Mods = new List<WeaponPlugin>();
    public int maxNum = 4;
    // 安装插件
    public override void OnActivate()
    {
        base.OnActivate();
        ModManager.Instance.InitForShooter(this);
    }
    public void AddMod(WeaponPlugin Mod)
    {
        if (Mod == null) return;

        // 核心：超过4个就不让装
        if (Mods.Count >= maxNum)
        {
            Debug.Log("插件已满，最多装备4个！");
            return;
        }

        // 防止重复装同一个
        if (Mods.Contains(Mod))
        {
            Debug.Log("不能重复装备同一个插件！");
            return;
        }

        Mods.Add(Mod);
    }

    public void RemoveMod(WeaponPlugin Mod)
    {
        Mods.Remove(Mod);
    }

    public void ApplyAllModsToBullet(PlayerBullet bullet)
    {
        foreach (var Mod in Mods)
        {
            Mod.ModifyBullet(bullet);
        }
    }
}
