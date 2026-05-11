using UnityEngine;

public abstract class EnemyBoundaryReaction : MonoBehaviour
{
    [Header("边界反应")]
    [SerializeField] private float cooldownSeconds = 1f;

    public bool isFirstTimeEntering = true;
    protected EnemyBase enemy;
    protected Rigidbody2D enemyRb;
    private float cooldownTimer;

    protected virtual void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        enemyRb = GetComponent<Rigidbody2D>();
    }

    internal void SetFirstEntryPending(bool pending)
    {
        isFirstTimeEntering = pending;
    }

    internal void AdvanceCooldown(float deltaTime)
    {
        if (cooldownTimer > 0f)
            cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
    }

    internal bool TryReact(in EnemyBoundaryService.EnemyBoundaryContext context)
    {
        if (cooldownTimer > 0f)
            return false;

        if (!CanReact(context))
            return false;

        bool handled = React(context);
        if (handled && cooldownSeconds > 0f)
            cooldownTimer = cooldownSeconds;

        return handled;
    }

    protected virtual bool CanReact(in EnemyBoundaryService.EnemyBoundaryContext context) => true;
    protected abstract bool React(in EnemyBoundaryService.EnemyBoundaryContext context);
}
