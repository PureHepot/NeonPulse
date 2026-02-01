using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewThemeConfig", menuName = "Game/Theme Config")]
public class SOVisualThemePresets : ScriptableObject
{
    public List<VisualThemePreset> allPresets;
}
