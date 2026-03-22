using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyBorderEvent : MonoBehaviour
{
    public bool isFirstTimeEntering = true;
    protected float cooldown = 1f;
    protected float _cooldownTimer;
    protected void Update()
    {
        if (this.GetComponent<EnemyBase>().isInScene)
        {
            isFirstTimeEntering=false;
        }
        if (_cooldownTimer > 0)
            _cooldownTimer -= Time.deltaTime;
    }
    public virtual void OnBorderReached() { }
    public virtual void OnUpdate() { }
    protected virtual bool EventAdmitted()
    {
        return false;
    }
}
