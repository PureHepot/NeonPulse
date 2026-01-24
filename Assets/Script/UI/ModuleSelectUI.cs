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
    public Text pointText;
    public Button optionBtnTemplate;
    public Transform optionBtnGroup;
    public Image blackBg;

    private List<UpgradeOption> _options;
    private bool _isPanelOpen = false;

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

        UpgradeManager.Instance.OnUpgradePointsChanged += UpdatePointText;
        ModuleSelectManager.Instance.OnShowAbilitySelectUI += RefreshAbilitySelectUI;
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
        _isPanelOpen = true;
        UpdatePointText(UpgradeManager.Instance.UpgradePoints);
    }

    /// <summary>
    /// 初始化能力选择界面
    /// </summary>
    private void InitAbilitySelectUI(int currentLevel)
    {
        titleText.text = $"选择强化 (按Tab退出) | 当前等级：Lv.{currentLevel}";
        ClearAllOptionBtns();
        CreateOptionBtns(_options);
    }

    /// <summary>
    /// 刷新整个面板
    /// </summary>
    private void RefreshAbilitySelectUI(int level, List<UpgradeOption> options)
    {
        _options = options;
        titleText.text = $"选择强化 (按Tab退出) | 当前等级：Lv.{level}";
        ClearAllOptionBtns();
        CreateOptionBtns(_options);
        UpdatePointText(UpgradeManager.Instance.UpgradePoints);

        if (UpgradeManager.Instance.UpgradePoints <= 0)
        {
            DisableAllOptionBtns();
        }
    }

    // 批量创建选项按钮
    private void CreateOptionBtns(List<UpgradeOption> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            CreateOptionBtn(options[i], i);
        }
    }

    // 更新点数显示
    private void UpdatePointText(int points)
    {
        if (pointText != null)
        {
            pointText.text = $"剩余升级点数：{points}";
        }
    }

    /// <summary>
    /// 生成单个能力选项按钮
    /// </summary>
    private void CreateOptionBtn(UpgradeOption option, int index)
    {
        Button btn = Instantiate(optionBtnTemplate, optionBtnGroup);
        btn.gameObject.SetActive(true);

        btn.interactable = UpgradeManager.Instance.UpgradePoints > 0;

        RefreshBtnUI(btn, option);

        btn.onClick.AddListener(() => OnClickOption(option, btn, index));
    }

    /// <summary>
    /// 点击能力选项
    /// </summary>
    private void OnClickOption(UpgradeOption option, Button btn, int index)
    {
        ModuleSelectManager.Instance.OnChooseUpgrade(option, index);

        if (UpgradeManager.Instance.UpgradePoints <= 0)
        {
            DisableAllOptionBtns();
            UpdatePointText(0);
            return;
        }

        var newOption = ModuleSelectManager.Instance.GetOrCreateOptions()[index];

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnClickOption(newOption, btn, index));

        RefreshBtnUI(btn, newOption);
        UpdatePointText(UpgradeManager.Instance.UpgradePoints);
    }


    // 禁用所有选项按钮
    private void DisableAllOptionBtns()
    {
        foreach (Transform child in optionBtnGroup)
        {
            if (child != optionBtnTemplate.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }
    }

    private void RefreshBtnUI(Button btn, UpgradeOption option)
    {
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
    }

    // 监听Tab键关闭面板
    private void Update()
    {
        if (_isPanelOpen && Input.GetKeyDown(KeyCode.Tab))
        {
            UIManager.Instance.CloseUI(this);
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        Time.timeScale = 1f;
        _isPanelOpen = false;
        ClearAllOptionBtns();
        UpgradeManager.Instance.OnUpgradePointsChanged -= UpdatePointText;
        ModuleSelectManager.Instance.OnShowAbilitySelectUI -= RefreshAbilitySelectUI;
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
        if (gameObject.activeSelf && _isPanelOpen)
        {
            Time.timeScale = 0f;
        }
    }

    private void OnDestroy()
    {
        UpgradeManager.Instance.OnUpgradePointsChanged -= UpdatePointText;
        ModuleSelectManager.Instance.OnShowAbilitySelectUI -= RefreshAbilitySelectUI;
    }
}