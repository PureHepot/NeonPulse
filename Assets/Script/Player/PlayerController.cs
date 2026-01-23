using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D Rigid2d { get; private set; }
    public Collider2D Colli2d { get; private set; }
    public PlayerModuleManager Modules { get; private set; }
    public SpriteRenderer BodyRenderer { get; private set; }

    //硬直状态
    public bool IsStunned { get; set; } = false;
    //冲刺状态
    public bool IsDashing { get; set; } = false;
    //是否死亡
    public bool IsDead { get; set; } = false;

    public Action OnDeath;

    private void Awake()
    {
        Rigid2d = GetComponent<Rigidbody2D>();
        Colli2d = GetComponent<Collider2D>();
        Modules = GetComponent<PlayerModuleManager>();
        BodyRenderer = transform.Find("Body").GetComponent<SpriteRenderer>();
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (IsStunned) return;
        Rigid2d.velocity = velocity;
    }
}
