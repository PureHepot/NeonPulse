using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldModule : PlayerModule
{
    [Header("Shield References")]
    public GameObject shieldObject;
    public ShieldController shieldScript;

    [Header("Settings")]
    public float rechargeRate = 1f;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (shieldObject) shieldObject.SetActive(false);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        // 激活时，打开护盾物体
        if (shieldObject) shieldObject.SetActive(true);
        // 重置护盾状态
        if (shieldScript) shieldScript.SetDefend(false); // 假设你之前的脚本有这个方法
    }

    public override void OnModuleUpdate()
    {
        // 这里可以写每一帧的逻辑
        // 比如：如果护盾破了，在这里计算自动回复时间的逻辑
        // 只要这个 Update 被调用，说明模块是激活状态
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (shieldObject) shieldObject.SetActive(false);
    }
}
