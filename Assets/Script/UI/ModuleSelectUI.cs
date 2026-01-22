using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级能力选择UI
/// </summary>
public class ModuleSelectUI : UIBase
{
    [Header("UI控件")]
    public Text titleText;
    public Button optionBtnTemplate;
    public Transform optionBtnGroup;
    public Image blackBg;

    private List<UpgradeOption> _options;

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
            blackBg.color = new Color(0, 0, 0, 0.7f);
        }
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        if (args is not Tuple<int, List<UpgradeOption>> data)
        {
            Debug.LogError("ModuleSelectUI参数类型错误");
            return;
        }

        int currentLevel = data.Item1;
        _options = data.Item2;

        InitAbilitySelectUI(currentLevel);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 初始化能力选择界面
    /// </summary>
    private void InitAbilitySelectUI(int currentLevel)
    {
        titleText.text = $"Lv.{currentLevel} 升级，选择一个强化";
        ClearAllOptionBtns();

        foreach (var option in _options)
        {
            CreateOptionBtn(option);
        }
    }

    /// <summary>
    /// 生成单个能力选项按钮
    /// </summary>
    private void CreateOptionBtn(UpgradeOption option)
    {
        Button btn = Instantiate(optionBtnTemplate, optionBtnGroup);
        btn.gameObject.SetActive(true);

        ModuleDescConfig desc = ModuleSelectManager.Instance.GetUpgradeDesc(option);

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

        btn.onClick.AddListener(() => OnClickOption(option));
    }

    /// <summary>
    /// 点击能力选项后的处理
    /// </summary>
    private void OnClickOption(UpgradeOption option)
    {
        ModuleSelectManager.Instance.OnChooseUpgrade(option);
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
