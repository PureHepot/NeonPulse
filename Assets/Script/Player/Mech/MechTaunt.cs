using UnityEngine;

public class MechTaunt : MechBase
{
    [Header("Taunt Settings")]
    public float reflectPercent = 1f;
    public float reflectRadius = 1.5f;
    public LayerMask enemyLayer;

    private static MechTaunt activeTaunt;
    public static bool HasActiveTaunt => activeTaunt != null && !activeTaunt.isDead;
    public static Transform TauntTarget => HasActiveTaunt ? activeTaunt.transform : null;

    protected override void Awake()
    {
        base.Awake();
        if (enemyLayer == 0)
            enemyLayer = 1 << LayerMask.NameToLayer("Enemy");
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        activeTaunt = this;
    }

    public override void OnDespawn()
    {
        if (activeTaunt == this) activeTaunt = null;
        base.OnDespawn();
    }

    protected override void Die()
    {
        if (activeTaunt == this) activeTaunt = null;
        base.Die();
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        ReflectToNearbyEnemies(amount);
    }

    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);
        ReflectToNearbyEnemies(amount);
    }

    private void ReflectToNearbyEnemies(int incomingDamage)
    {
        int reflectDamage = Mathf.RoundToInt(incomingDamage * reflectPercent);
        if (reflectDamage <= 0) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, reflectRadius, enemyLayer);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
            if (knockDir.sqrMagnitude < 0.01f) knockDir = Vector3.up;

            enemy.TakeDamage(reflectDamage, enemy.transform.position, knockDir, 3f);
        }
    }
}
