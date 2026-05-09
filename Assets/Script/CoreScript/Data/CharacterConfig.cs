using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 机体/皮肤配置
[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Game/Character Config")]
public class CharacterConfig : ScriptableObject
{
    public string characterId; // 如 "Gunner", "Ninja"
    public string displayName; // 显示名称 "枪手", "忍者"
    public int maxLoadCapacity = 10; // 机体负荷上限

    public List<ModuleConfig> availableModules;

    public GameObject previewPrefab;
}
