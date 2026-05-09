using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponPlugin : Plugin
{
    // ÐÞ¸Ä×Óµ¯
    public abstract void ModifyBullet(PlayerBullet bullet);
}
