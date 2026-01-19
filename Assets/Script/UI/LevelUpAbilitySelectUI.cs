using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级能力选择UI
/// </summary>
public class LevelUpAbilitySelectUI : UIBase
{
    [Header("UI控件")]
    public Text titleText; 
    public Button optionBtnTemplate; 
    public Transform optionBtnGroup; 
    public Image blackBg; 

    private List<ModuleType> _candidateModules; 

    private void Awake()
    {
        if (optionBtnTemplate != null)
        {
            optionBtnTemplate.gameObject.SetActive(false);
        }

        if (blackBg != null)
        {
            blackBg.rectTransform.anchorMin = Vector2.zero;
            blackBg.rectTransform.anchorMax = Vector2.one;
            blackBg.rectTransform.offsetMin = Vector2.zero;
            blackBg.rectTransform.offsetMax = Vector2.zero;
            blackBg.color = new Color(0, 0, 0, 0.7f); // 半透明黑
        }
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        if (args is not Tuple<int, List<ModuleType>> data)
        {
            return;
        }

        int currentLevel = data.Item1;
        _candidateModules = data.Item2;

        InitAbilitySelectUI(currentLevel);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 初始化能力选择界面
    /// </summary>
    private void InitAbilitySelectUI(int currentLevel)
    {
        titleText.text = $"Lv.{currentLevel} 升级,选择一个能力解锁";
        ClearAllOptionBtns();
        foreach (var moduleType in _candidateModules)
        {
            CreateOptionBtn(moduleType);
        }
    }

    /// <summary>
    /// 生成单个能力选项按钮
    /// </summary>
    private void CreateOptionBtn(ModuleType type)
    {
        Button btn = Instantiate(optionBtnTemplate, optionBtnGroup);
        btn.gameObject.SetActive(true);

        ModuleDescConfig desc = PlayerAbilitySelectManager.Instance.GetModuleDesc(type);
        if (desc == null)
        {
            desc = new ModuleDescConfig()
            {
                moduleName = type.ToString(),
                moduleDesc = $"解锁{type}能力，提升战斗能力"
            };
            Debug.LogWarning($"未配置{type}的显示信息");
        }

        Text btnName = btn.transform.Find("Name")?.GetComponent<Text>();
        Text btnDesc = btn.transform.Find("Desc")?.GetComponent<Text>();
        Image btnIcon = btn.transform.Find("Icon")?.GetComponent<Image>();

        if (btnName != null) btnName.text = desc.moduleName;
        if (btnDesc != null) btnDesc.text = desc.moduleDesc;
        if (btnIcon != null && desc.moduleIcon != null)
        {
            btnIcon.sprite = desc.moduleIcon;
            btnIcon.enabled = true;
        }

        btn.onClick.AddListener(() => OnClickOption(type));
    }

    /// <summary>
    /// 点击能力选项后的处理
    /// </summary>
    private void OnClickOption(ModuleType type)
    {
        PlayerAbilitySelectManager.Instance.OnChooseModule(type);
        UIManager.Instance.CloseUI(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        Time.timeScale = 1f;
        ClearAllOptionBtns();
        Destroy(gameObject);
    }

    /// <summary>
    /// 清空所有选项按钮
    /// </summary>
    private void ClearAllOptionBtns()
    {
        foreach (Transform child in optionBtnGroup)
        {
            if (child != optionBtnTemplate.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 防止暂停时意外退出
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        if (gameObject.activeSelf)
        {
            Time.timeScale = 0f;
        }
    }
}