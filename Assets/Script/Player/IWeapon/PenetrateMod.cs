using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenetrateMod : WeaponPlugin
{
    public override void ModifyBullet(PlayerBullet bullet)
    {
        bullet.isPenetrate = true;
    }

}
