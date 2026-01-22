using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验进度条UI
/// </summary>
public class ExpBarUI : UIBase
{
    public Image expFillImage;  
    public Text levelText;      
    public Text expText;        

    private void Awake()
    {
        // 初始化UI
        InitExpUI();
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        // 订阅经验事件
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnExpChanged += UpdateExpUI;
        }
    }

    public override void OnClose()
    {
        base.OnClose();

        // 取消订阅
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnExpChanged -= UpdateExpUI;
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitExpUI()
    {
        // 先检查控件是否挂载成功
        if (expFillImage == null || levelText == null || expText == null)
        {
            Debug.LogError("控件未挂载，请在Inspector拖入对应组件");
            return;
        }

        expFillImage.fillAmount = 0;
        levelText.text = "Lv.1";
        expText.text = "0/100";

        // 同步真实数据
        if (UpgradeManager.Instance != null)
        {
            int initExp = UpgradeManager.Instance.baseExpToLevelUp;
            UpdateExpUI(0, initExp, 1);
        }
    }

    /// <summary>
    /// 更新经验UI
    /// </summary>
    public void UpdateExpUI(int currentExp, int expToLevelUp, int currentLevel)
    {
        if (expFillImage == null || levelText == null || expText == null) return;

        float progress = expToLevelUp <= 0 ? 0 : (float)currentExp / expToLevelUp;

        // 更新进度条和文本
        expFillImage.fillAmount = progress;
        levelText.text = $"Lv.{currentLevel}";
        expText.text = $"{currentExp}/{expToLevelUp}";
    }
}