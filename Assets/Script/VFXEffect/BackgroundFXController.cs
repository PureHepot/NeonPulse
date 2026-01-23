using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class VisualThemePreset
{
    public string themeName = "Default Theme";

    public float themeTransitionDuration = 2.0f;

    [Header("颜色渐变 (Gradient)")]
    [ColorUsage(true, true)] public Color colorTop = Color.cyan;
    [ColorUsage(true, true)] public Color colorBottom = Color.blue;

    [Header("背景参数")]
    [Range(0f, 1f)] public float bgBrightness = 0.2f; // 背景暗度
    public float waveSpeed = 0.5f;     // 流动速度
    public float peakFocus = 3.0f;     // 峰值陡峭程度 (中间亮斑的大小)
    public float waveFrequency = 5.0f; // 周期/密度

    [Header("受击反馈设置")]
    public float punchStrength = 2.0f; // 这个主题下的受击扭曲强度
}

[CreateAssetMenu(fileName = "NewThemeConfig", menuName = "Game/Theme Config")]
public class SOVisualThemePresets : ScriptableObject
{
    public List<VisualThemePreset> allPresets;
}

public class BackgroundFXController : MonoSingleton<BackgroundFXController>
{
    [Header("材质引用")]
    public Material bgMaterial;   // 背景材质
    public Material wallMaterial; // 墙壁材质 (同步颜色用)

    [Header("受击反馈通用设置")]
    public float punchDuration = 0.5f;

    [Header("主题预设列表")]
    public SOVisualThemePresets presets;
     // 切换主题的渐变时间

    // --- 运行时状态 ---
    private int currentPresetIndex = 0;

    // 用于记录当前正在显示的值 (用于 Lerp 过渡)
    private VisualThemePreset currentValues;

    // --- Shader 属性 ID 缓存 ---
    private static readonly int ID_ColorTop = Shader.PropertyToID("_ColorTop");
    private static readonly int ID_ColorBottom = Shader.PropertyToID("_ColorBottom");
    private static readonly int ID_LineBrightness = Shader.PropertyToID("_LineBrightness");
    private static readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
    private static readonly int ID_PeakFocus = Shader.PropertyToID("_PeakFocus");
    private static readonly int ID_WaveFrequency = Shader.PropertyToID("_WaveFrequency");

    // 互动相关 ID
    private static readonly int ID_FocusPos = Shader.PropertyToID("_FocusPos");
    private static readonly int ID_FocusPower = Shader.PropertyToID("_FocusPower");

    // 墙壁相关 ID
    private static readonly int ID_WallColor = Shader.PropertyToID("_NeonColor");

    private void Awake()
    {
        currentValues = new VisualThemePreset();
    }

    private void Start()
    {
        if (presets.allPresets.Count > 0)
        {
            ApplyPresetImmediate(presets.allPresets[0]);
        }
    }

    /// <summary>
    /// 切换主题 (渐变)
    /// </summary>
    /// <param name="index"></param>
    public void SwitchToTheme(int index)
    {
        if (index < 0 || index >= presets.allPresets.Count) return;
        if (index == currentPresetIndex) return;

        // 获取目标和起始
        VisualThemePreset target = presets.allPresets[index];
        VisualThemePreset start = ClonePreset(currentValues); // 记录当前状态作为起点

        currentPresetIndex = index;

        DOVirtual.Float(0f, 1f, target.themeTransitionDuration, (t) =>
        {
            //颜色插值
            currentValues.colorTop = Color.Lerp(start.colorTop, target.colorTop, t);
            currentValues.colorBottom = Color.Lerp(start.colorBottom, target.colorBottom, t);

            //数值插值
            currentValues.bgBrightness = Mathf.Lerp(start.bgBrightness, target.bgBrightness, t);
            currentValues.waveSpeed = Mathf.Lerp(start.waveSpeed, target.waveSpeed, t);
            currentValues.peakFocus = Mathf.Lerp(start.peakFocus, target.peakFocus, t);
            currentValues.waveFrequency = Mathf.Lerp(start.waveFrequency, target.waveFrequency, t);
            currentValues.punchStrength = Mathf.Lerp(start.punchStrength, target.punchStrength, t);

            //应用到材质
            UpdateMaterials();
        });
    }

    public void SwitchToNextTheme()
    {
        int nextIndex = (currentPresetIndex + 1) % presets.allPresets.Count;
        SwitchToTheme(nextIndex);
    }

    /// <summary>
    /// 受击扭曲
    /// </summary>
    /// <param name="worldPos"></param>
    public void TriggerDistortion(Vector3 worldPos)
    {
        if (bgMaterial == null) return;

        Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);
        bgMaterial.SetVector(ID_FocusPos, new Vector2(viewportPos.x, viewportPos.y));

        // 读取当前预设中的 punchStrength 力度
        float strength = currentValues.punchStrength;

        // 先杀掉之前的动画防止冲突
        bgMaterial.DOKill();

        // 瞬间设为强度值
        bgMaterial.SetFloat(ID_FocusPower, strength);

        // 弹回 0
        DOVirtual.Float(strength, 0f, punchDuration, (v) =>
        {
            bgMaterial.SetFloat(ID_FocusPower, v);
        }).SetEase(Ease.OutElastic);
    }

    // =========================================================
    // 内部辅助方法
    // =========================================================

    private void ApplyPresetImmediate(VisualThemePreset p)
    {
        // 深度复制数据到 currentValues
        currentValues.themeTransitionDuration = p.themeTransitionDuration;
        currentValues.colorTop = p.colorTop;
        currentValues.colorBottom = p.colorBottom;
        currentValues.bgBrightness = p.bgBrightness;
        currentValues.waveSpeed = p.waveSpeed;
        currentValues.peakFocus = p.peakFocus;
        currentValues.waveFrequency = p.waveFrequency;
        currentValues.punchStrength = p.punchStrength;

        UpdateMaterials();
    }

    private void UpdateMaterials()
    {
        if (bgMaterial != null)
        {
            bgMaterial.SetColor(ID_ColorTop, currentValues.colorTop);
            bgMaterial.SetColor(ID_ColorBottom, currentValues.colorBottom);
            bgMaterial.SetFloat(ID_LineBrightness, currentValues.bgBrightness);
            bgMaterial.SetFloat(ID_WaveSpeed, currentValues.waveSpeed);
            bgMaterial.SetFloat(ID_PeakFocus, currentValues.peakFocus);
            bgMaterial.SetFloat(ID_WaveFrequency, currentValues.waveFrequency);
        }

        if (wallMaterial != null)
        {
            // 将背景的顶部颜色（通常是亮色）同步给墙壁
            // 你也可以在这里混合 top 和 bottom
            wallMaterial.SetColor(ID_WallColor, currentValues.colorTop);
        }
    }

    private VisualThemePreset ClonePreset(VisualThemePreset source)
    {
        return new VisualThemePreset
        {
            themeTransitionDuration = source.themeTransitionDuration,
            colorTop = source.colorTop,
            colorBottom = source.colorBottom,
            bgBrightness = source.bgBrightness,
            waveSpeed = source.waveSpeed,
            peakFocus = source.peakFocus,
            waveFrequency = source.waveFrequency,
            punchStrength = source.punchStrength
        };
    }
}