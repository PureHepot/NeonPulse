using UnityEngine;

public abstract class EnemyAuraBase : MonoBehaviour
{
    protected Transform player;
    protected HealthModule health;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            OnPlayerEnter();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerExit();
            player = null;
        }
    }
    protected abstract void OnPlayerEnter();
    protected abstract void OnPlayerExit();
}
