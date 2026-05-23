using System.Collections.Generic;
using UnityEngine;

public abstract class BossBase : MonoBase
{
    [Header("Boss Core Settings")]
    public string bossName = "Unknown Boss";
    public int enemyExp = 100;
    
    [Header("Contact Damage")]
    public int contactDamage = 1;

    [Header("Boss Parts Management")]
    public List<BossPart> bossParts = new List<BossPart>();
    protected Dictionary<string, BossPart> partDictionary = new Dictionary<string, BossPart>();

    protected BossBaseState currentState;

    [Header("Boss Flags")]
    public bool isFinalBoss;

    protected virtual void Start()
    {
        currentHp = maxHp;
        InitializeParts();
    }

    private void InitializeParts()
    {
        foreach (var part in bossParts)
        {
            part.Initialize(this);
            if (!string.IsNullOrEmpty(part.partName) && !partDictionary.ContainsKey(part.partName))
                partDictionary.Add(part.partName, part);
        }
    }

    protected virtual void Update()
    {
        if (!isDead)
            currentState?.LogicUpdate();
    }

    protected virtual void FixedUpdate()
    {
        if (!isDead)
            currentState?.PhysicsUpdate();
    }

    public virtual void SwitchState(BossBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter(this);
    }

    public override void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);
        if (!isDead)
            CheckPhaseTransition();
    }

    protected abstract void CheckPhaseTransition();

    protected override void Die()
    {
        if (currentState != null)
        {
            currentState.Exit();
            currentState = null;
        }

        CleanupBossArtifacts();
        base.Die();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    protected virtual void CleanupBossArtifacts()
    {
    }

    public BossPart GetPart(string lookupPartName)
    {
        if (partDictionary.TryGetValue(lookupPartName, out BossPart part))
            return part;

        Debug.LogWarning($"[BossBase] Part '{lookupPartName}' not found!");
        return null;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || collision == null)
            return;

        TryDamagePlayerOnContact(collision.gameObject);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || collision == null)
            return;

        TryDamagePlayerOnContact(collision.gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || other == null)
            return;

        TryDamagePlayerOnContact(other.gameObject);
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || other == null)
            return;

        TryDamagePlayerOnContact(other.gameObject);
    }

    private void TryDamagePlayerOnContact(GameObject otherObject)
    {
        if (otherObject == null || !otherObject.CompareTag("Player"))
            return;

        otherObject.GetComponentInChildren<HealthModule>()?.TakeDamage(contactDamage, transform);
    }
}
