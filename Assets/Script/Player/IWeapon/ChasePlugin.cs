using UnityEngine;

public class ChasePlugin : WeaponPlugin
{
    public float deFireRate = 1.5f;
    public override void ModifyBullet(PlayerBullet bullet)
    {
        if (bullet == null)
            return;

        bullet.homingEnabled = true;
    }

    public void ChangeDeFireRate()
    {
        if (ModManager.Instance != null)
            ModManager.Instance.deFireRate *= deFireRate;
    }
}
