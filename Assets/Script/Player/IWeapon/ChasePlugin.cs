using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChasePlugin : WeaponPlugin
{
    public float deFireRate = 1.5f;
    public override void ModifyBullet(PlayerBullet bullet)
    {
        bullet.isChase = true;
    }
    public void ChangeDeFireRate()
    {
        ModManager.Instance.deFireRate*=deFireRate;
    }
}
