using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public BaseState currentState;

    //---状态机用状态---
    public MoveState moveState = new MoveState();
    //public IdleState idleState = new IdleState();
    public DashState dashState = new DashState();
    public DefenceState defenceState = new DefenceState();


    //---控制参数设置---
    [Header("Control Parameters")]
    public float moveSpeed = 3f;
    public float dashSpeed = 5f;
    public float defenceSpeed = 1f;

    [Header("Dash Settings")]
    public float dashCooldown = 0.3f;
    [HideInInspector]
    public float lastDashTime = -999f;


    //基本组件
    private Rigidbody2D rigi2d;
    public Vector2 Velocity
    {
        get
        {
            return rigi2d.velocity;
        }
    }

    private void Awake()
    {
        rigi2d = GetComponent<Rigidbody2D>();
    }


    void Start()
    {
        ChangeState(moveState);
    }

    private void OnDestroy()
    {

    }

    void Update()
    {
        currentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        currentState.PhysicsUpdate();
    }

    public void ChangeState(BaseState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
   

    public void SetVelocity(Vector2 velocity)
    {
        rigi2d.velocity = velocity;
    }

    public bool CanDash()
    {
        return Time.time >= lastDashTime + dashCooldown;
    }
}
