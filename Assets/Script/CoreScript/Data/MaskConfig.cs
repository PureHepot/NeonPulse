using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMaskConfig", menuName = "Game/Mask Config")]
public class MaskConfig : ScriptableObject
{
    [Header("UI Display")]
    public string maskName;
    public Sprite icon;

    [Header("Visual Rewards")]
    public Sprite bodySprite;
    public Color themeColor = Color.white;

    [Header("Module Rewards")]
    public List<ModuleType> guaranteedModules;

}
