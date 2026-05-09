using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectMod : WeaponPlugin
{
    public override void ModifyBullet(PlayerBullet bullet)
    {
        bullet.isReflect = true;
    }
}
