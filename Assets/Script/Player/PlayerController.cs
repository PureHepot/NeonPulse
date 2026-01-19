using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    //public BaseState currentState;

    ////---状态机用状态---
    //public MoveState moveState;
    //public DashState dashState;
    //public DefenceState defenceState;
    //public HitedState hitedState;

    //public bool isInputLocked = false;


    ////---控制参数设置---
    //[Header("Combat Stats")]
    //public int currentHp;

    //[Header("Control Parameters")]
    //public float moveSpeed = 3f;
    //public float defenceSpeed = 1f;

    //[Header("Hurt Settings")]
    //public float knockbackForce = 10f;
    //public float stunDuration = 0.2f;
    //public float invincibilityDuration = 1.0f;

    //[HideInInspector] public bool isInvincible = false;
    //[HideInInspector] public Vector2 lastAttackerPos;//攻击位置

    //[Header("Renderer Settings")]
    //public Color hitColor;
    //public Color normalColor;

    ////---基本组件---
    //private Rigidbody2D rigi2d;
    //private Collider2D colli2;
    //public Rigidbody2D Rigid2d => rigi2d;

    //private PlayerModuleManager modules;

    //private SpriteRenderer body;
    //public PlayerModuleManager Modules => modules;

    ////---数据---
    //public Vector2 Velocity
    //{
    //    get
    //    {
    //        return rigi2d.velocity;
    //    }
    //}

    //public Action onDeath;


    //private void Awake()
    //{
    //    rigi2d = GetComponent<Rigidbody2D>();
    //    colli2 = GetComponent<Collider2D>();
    //    modules = GetComponent<PlayerModuleManager>();
    //    body = transform.Find("Body").GetComponent<SpriteRenderer>();

    //    moveState = new MoveState(this);
    //    dashState = new DashState(this);
    //    defenceState = new DefenceState(this);
    //    hitedState = new HitedState(this);
    //}


    //void Start()
    //{
    //    ChangeState(moveState);
    //}

    //private void OnDestroy()
    //{

    //}

    //void Update()
    //{
    //    if (currentState != null)
    //    {
    //        currentState.LogicUpdate();
    //    }

    //    if (!isInputLocked && (currentState == null || currentState.CanBeInterrupted()))
    //    {
    //        HandleGlobalInput();
    //    }
    //}

    //private void FixedUpdate()
    //{
    //    currentState.PhysicsUpdate();
    //}

    //private void HandleGlobalInput()
    //{
    //    if (InputManager.Instance.Space())
    //    {
    //        if (modules.HasAbility(ModuleType.SpeedBooster))
    //        {
    //            ChangeState(dashState);
    //            return;
    //        }
    //    }

    //    if (InputManager.Instance.Mouse1Down())
    //    {
    //        if (modules.HasAbility(ModuleType.Shield))
    //        {
    //            ChangeState(defenceState);
    //            return;
    //        }
    //    }

    //    if (currentState != moveState)
    //    {
    //        ChangeState(moveState);
    //    }
    //}

    //public void ChangeState(BaseState newState)
    //{
    //    if (currentState == newState) return;

    //    if (currentState != null) currentState.Exit();

    //    currentState = newState;

    //    currentState.Enter();
    //}


    //public void SetVelocity(Vector2 velocity)
    //{
    //    rigi2d.velocity = velocity;
    //}

    //public void TakeDamage(int amount, Transform attacker)
    //{
    //    if (isInvincible) return;

    //    currentHp = PlayerManager.Instance.CurrentHp;

    //    currentHp -= amount;

    //    PlayerManager.Instance.CurrentHp = currentHp;
    //    if (currentHp <= 0)
    //    {
    //        onDeath?.Invoke();
    //        return;
    //    }

    //    lastAttackerPos = attacker != null ? attacker.position : transform.position;

    //    ChangeState(hitedState);
    //}

    //public void PlayHurtVisuals()
    //{
    //    body.DOKill(); // 清除之前的动画防止冲突
    //    body.DOColor(hitColor, 0.05f).OnComplete(() =>
    //    {
    //        body.DOColor(Color.white, 0.2f);
    //    });

    //    transform.DOKill();
    //    transform.DOPunchScale(new Vector3(-0.3f, 0.3f, 0), 0.2f, 10, 1);
    //}

    ////无敌时间
    //public IEnumerator InvincibilityRoutine()
    //{
    //    isInvincible = true;
    //    colli2.enabled = false;

    //    Tween blinkTween = body.DOFade(0.5f, 0.1f).SetLoops(-1, LoopType.Yoyo);

    //    yield return new WaitForSeconds(invincibilityDuration);

    //    blinkTween.Kill();
    //    body.color = normalColor;
    //    isInvincible = false;
    //    colli2.enabled = true;
    //}

    public Rigidbody2D Rigid2d { get; private set; }
    public Collider2D Colli2d { get; private set; }
    public PlayerModuleManager Modules { get; private set; }
    public SpriteRenderer BodyRenderer { get; private set; }

    //硬直状态
    public bool IsStunned { get; set; } = false;
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
