using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTeleportEvent : EnemyBorderEvent
{
    private EnemyBase enemy;
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }
    public override void OnBorderReached()
    {
        if(_cooldownTimer>0)return;
        _cooldownTimer = cooldown;
        if (EventAdmitted())
        {
            enemy.transform.position = -enemy.transform.position;
        }
    }
    protected override bool EventAdmitted()
    {
        return true;
    }
}
