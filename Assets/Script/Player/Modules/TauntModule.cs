using UnityEngine;

public class TauntModule : PlayerModule
{
    private const string TauntCooldownStatId = "utility.tauntcooldown";
    private const string TauntDurationStatId = "utility.tauntduration";

    [Header("Combat Settings")]
    public float tauntCooldown = 10f;
    public float tauntDuration = 5f;

    [Header("Summon Settings")]
    public GameObject tauntMechPrefab;

    private float cooldownTimer;

    protected override void OnInitialize()
    {
        cooldownTimer = 0f;
        RecalculateStats();
    }

    protected override void OnActivate()
    {
        RecalculateStats();
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || !HasControl)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= DeltaTime;

        if (InputManager.Instance.G() && cooldownTimer <= 0f)
        {
            SpawnTaunt();
            cooldownTimer = tauntCooldown;
        }
    }

    private void SpawnTaunt()
    {
        if (tauntMechPrefab == null)
            return;

        Vector3 spawnPos = MUtils.GetMouseWorldPosition();
        GameObject obj = ObjectPoolManager.Instance.Get(tauntMechPrefab, spawnPos, Quaternion.identity);
        MechTaunt taunt = obj.GetComponent<MechTaunt>();
        if (taunt != null)
            taunt.duration = tauntDuration;
    }

    private void RecalculateStats()
    {
        tauntCooldown = GetStat(TauntCooldownStatId, tauntCooldown);
        tauntDuration = GetStat(TauntDurationStatId, tauntDuration);
    }
}
