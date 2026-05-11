public abstract class RangedWeaponModule : ProjectileWeaponModule
{
    protected virtual ModuleCategory WeaponCategory => ModuleCategory.Weapon | ModuleCategory.Ranged;

    public bool SupportsCategory(ModuleCategory category)
    {
        return (WeaponCategory & category) != 0;
    }
}
