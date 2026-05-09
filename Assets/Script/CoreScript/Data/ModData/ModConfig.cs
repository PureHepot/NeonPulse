using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ModType
{
    ReflectMod,
    PenetrateMod,
    ChaseMod,
    CriticalhitMod,
    ExplodePlugin
}
[CreateAssetMenu(fileName = "NewModuleConfig", menuName = "Game/Mod Config")]
public class WeaponModConfig : ScriptableObject
{
    public ModType ModType;
    public string ModName;
    public GameObject ModPrefab; // 挂载到武器上的 WeaponPlugin 脚本所在预制体
    public int maxEquipCount = 1; // 最多装几个
    public Sprite icon;
    public string Description;
}
