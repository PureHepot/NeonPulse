using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriticalhitPlugin : WeaponPlugin
{
    public override void ModifyBullet(PlayerBullet bullet)
    {
        int random = Random.Range(1, 101);
        if (random <= 30)
        {
            bullet.damage *= 2;
        }
    }
}
