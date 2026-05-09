using UnityEngine;

public class ShieldModule : PlayerModule
{
    private const string ShieldCapacityStatId = "defence.shiledcapacity";
    private const string ShieldRegenStatId = "defence.shieldcharge";
    private const string ShieldKnockbackStatId = "defence.shieldknockback";

    [Header("Shield References")]
    public GameObject shieldObject;
    public ShieldController shieldScript;

    [Header("Settings")]
    public float rechargeRate = 1f;
    public float rotateSpeed = 200f;

    [Header("Parameter")]
    public float ShieldCapacity = 100f;
    public float ShieldRegen = 10f;
    public float ShieldKnockback = 10f;

    protected override void OnInitialize()
    {
        RecalculateStats();
    }

    protected override void OnActivate()
    {
        if (shieldObject != null)
            shieldObject.SetActive(true);

        if (shieldScript != null)
            shieldScript.SetDefend(false);
    }

    protected override void OnDeactivate()
    {
        if (shieldObject != null)
            shieldObject.SetActive(false);
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || !HasControl)
            return;

        HandleRotation();

        if (shieldScript != null)
            shieldScript.SetDefend(InputManager.Instance.Mouse1());
    }

    private void HandleRotation()
    {
        if (shieldObject == null)
            return;

        Vector3 mousePos = MUtils.GetMouseWorldPosition();
        Vector2 direction = mousePos - shieldObject.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        shieldObject.transform.rotation = Quaternion.Slerp(shieldObject.transform.rotation, targetRotation, rotateSpeed * DeltaTime);
    }

    private void RecalculateStats()
    {
        ShieldCapacity = GetStat(ShieldCapacityStatId, ShieldCapacity);
        ShieldRegen = GetStat(ShieldRegenStatId, ShieldRegen);
        ShieldKnockback = GetStat(ShieldKnockbackStatId, ShieldKnockback);
    }
}
