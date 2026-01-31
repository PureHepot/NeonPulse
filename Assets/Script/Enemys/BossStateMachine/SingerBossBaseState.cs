using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingerBossBaseState
{
    // 持有 BossSinger 的引用，供子类访问
    protected BossSinger boss;

    // 可选：记录当前状态运行了多久，方便子类做计时逻辑
    protected float stateTimer;

    // 构造函数：注入 Boss 引用
    public SingerBossBaseState(BossSinger boss)
    {
        this.boss = boss;
    }

    // 进入状态时调用一次
    public virtual void Enter()
    {
        stateTimer = 0f;
    }

    // 每帧调用
    public virtual void Update()
    {
        stateTimer += Time.deltaTime;
    }

    // 物理帧调用 (如果有物理移动逻辑可以重写此方法)
    public virtual void FixedUpdate() { }

    // 退出状态时调用一次
    public virtual void Exit() { }
}
