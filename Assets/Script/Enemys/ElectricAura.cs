using UnityEngine;

public class ElectricAura : MonoBehaviour
{
    [Header("电流伤害参数")]
    public float damagePerSecond = 2f;
    public float damageInterval = 0.5f;

    private float damageTimer;
    private HealthModule targetHealth;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetHealth = other.GetComponentInChildren<HealthModule>();
            damageTimer = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetHealth = null;
        }
    }

    private void Update()
    {
        if (targetHealth == null) return;

        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            damageTimer = damageInterval;

            int damage = Mathf.RoundToInt(damagePerSecond * damageInterval);
            targetHealth.TakeDamage(damage, transform);
        }
    }
}
