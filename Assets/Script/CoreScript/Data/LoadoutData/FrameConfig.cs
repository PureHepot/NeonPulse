using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 机体框架配置 (ScriptableObject)
// 框架决定：基础属性、固有特效、视觉展示
// 插槽配置由展示Prefab中的按钮布局决定
// ============================================================

[CreateAssetMenu(fileName = "NewFrameConfig", menuName = "Game/Frame Config")]
public class FrameConfig : ScriptableObject
{
    [Header("基本信息")]
    public string frameId;          // 唯一标识，如 "Duelist"
    public string displayName;      // 显示名称，如 "决斗者"
    [TextArea] public string description;
    public Sprite icon;

    [Header("基础属性")]
    public float baseMaxHP = 100f;

    [Header("框架固有特效")]
    [Tooltip("框架自带的机制级特效（不依赖模块），如近战攻击附带眩晕等")]
    public List<FrameInherentEffect> inherentEffects = new List<FrameInherentEffect>();

    [Header("视觉展示")]
    [Tooltip("框架核心显示物体，用于动画和视觉区分")]
    public GameObject frameCore;

    [Tooltip("框架插槽展示Prefab，包含按钮布局（每个按钮代表一个插槽）")]
    public GameObject slotLayoutPrefab;
}

// ============================================================
// 框架固有特效：不依赖模块，框架自带的机制
// ============================================================

[Serializable]
public struct FrameInherentEffect
{
    public string effectId;         // 如 "DashThroughEnemy", "AutoShieldOnLowHP"
    public float param1;
    public float param2;
    public float param3;
    [TextArea] public string description;
}