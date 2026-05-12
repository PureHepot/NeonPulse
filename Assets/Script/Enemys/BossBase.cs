using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBase : MonoBase
{
    [Header("Boss Core Settings")]
    public string bossName = "Unknown Boss";
    public int enemyExp = 100;
    //protected Transform playerTarge;
    [Header("Boss Parts Management")]
    public List<BossPart> bossParts = new List<BossPart>();
    protected Dictionary<string, BossPart> partDictionary = new Dictionary<string, BossPart>();
    
    protected BossBaseState currentState;

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
            {
                partDictionary.Add(part.partName, part);
            }
        }
    }

    protected virtual void Update()
    {
        if (!isDead) currentState?.LogicUpdate();
    }

    protected virtual void FixedUpdate()
    {
        if (!isDead) currentState?.PhysicsUpdate();
    }

    public virtual void SwitchState(BossBaseState newState)
{
    // ���ؼ��������л�ǰ�����þ�״̬���˳��߼�
    if (currentState != null)
    {
        currentState.Exit(); 
    }

    currentState = newState;

    if (currentState != null)
    {
        currentState.Enter(this);
    }
}

    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.TakeDamage(amount, hitPoint, hitNormal);
        if (!isDead) CheckPhaseTransition(); // ÿ���ܻ�����Ƿ���Ҫת�׶�
    }

    protected abstract void CheckPhaseTransition();

    [Header("�Ƿ������չؿ�Boss")]
    public bool isFinalBoss = false; // �� Inspector ����ﹴѡ���

    protected override void Die()
    {
        base.Die();
        Destroy(gameObject, 2f);
    }

    public BossPart GetPart(string partName)
    {
        if (partDictionary.TryGetValue(partName, out BossPart part)) return part;
        Debug.LogWarning($"[BossBase] Part '{partName}' not found!");
        return null;
    }
}
