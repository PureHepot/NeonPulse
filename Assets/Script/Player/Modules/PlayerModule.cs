using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ModuleType
{
    Shield,
    SpeedBooster,
}


public abstract class PlayerModule : MonoBehaviour
{
    [Header("Base Settings")]
    public ModuleType moduleType;
    public bool isUnlocked = false;

    protected PlayerController player;

    //初始化
    public virtual void Initialize(PlayerController _player)
    {
        this.player = _player;
        if (!isUnlocked) OnDeactivate();
    }

    public virtual void OnModuleUpdate()
    {
        // 子类重写此方法来实现每帧逻辑
    }

    // 解锁/激活时调用
    public virtual void OnActivate()
    {
        isUnlocked = true;
        this.enabled = true; // 启用组件
    }

    // 禁用时调用
    public virtual void OnDeactivate()
    {
        this.enabled = false;
    }
}
