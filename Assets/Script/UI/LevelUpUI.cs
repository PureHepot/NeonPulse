using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.UI;

public class LevelUpUI : UIBase
{
    private PlayerPreviewSync playerPreview;

    private void Awake()
    {
        var camObj = GameObject.Find("PlayerModelCamera");
        if (camObj) playerPreview = camObj.GetComponent<PlayerPreviewSync>();
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        Time.timeScale = 0.01f;

        InputManager.Instance.SetLockLevel(InputLockLevel.AllLocked);

        RefreshUI();
    }

    public void RefreshUI()
    {
        List<PlayerModule> Modules = PlayerManager.Instance.CurrentModules.GetAllActiveModules();

        Transform trans = Get<Transform>("ItemContent");

        trans.IteratorChild(Modules.Count, iterator);

        void iterator(int index, Transform item)
        {
            int i = index;

            ModuleConfig config = UpgradeManager.Instance.GetConfig(Modules[i].ModuleType);
            Color ModuleColor = Color.cyan;

            if (config != null)
            {
                ModuleColor = config.themeColor;
            }

            var itemImg = item.GetComponent<Image>();
            if (itemImg) itemImg.color = ModuleColor;

            item.Find("Name").GetComponent<Text>().text = Modules[i].ModuleType.ToString();

            List<StatType> statTypes = UpgradeManager.Instance.GetUpgradedStats(Modules[i].ModuleType);
            item.Find("Detail_Container").IteratorChild(statTypes.Count, detailIterator);

            void detailIterator(int detailIndex, Transform detailItem)
            {
                int j = detailIndex;
                StatType statType = statTypes[j];

                var detailImg = detailItem.GetComponent<Image>();
                if (detailImg) detailImg.color = ModuleColor * 0.8f;

                detailItem.Find("Name").GetComponent<Text>().text = statType.ToString();
                int currentLevel = UpgradeManager.Instance.GetLevel(Modules[i].ModuleType, statType) + 1;
                detailItem.Find("LevelNum").GetComponent<Text>().text = currentLevel.ToString();
                string description = UpgradeManager.Instance.GetConfig(Modules[i].ModuleType).GetDescription(statType);
                detailItem.Find("Description").GetComponent<Text>().text = description;
                detailItem.Find("CostPoint").GetComponent<Text>().text =
                    UpgradeManager.Instance.GetCost(Modules[i].ModuleType, statType).ToString();
                detailItem.Find("DesBg").GetComponent<Image>().color = ModuleColor * 0.8f;
                detailItem.Find("UpgradeBtn").GetComponent<Image>().color = ModuleColor * 0.8f;
                detailItem.Find("UpgradeBtn").GetComponent<Button>().onClick.SetListener(() =>
                {
                    if (UpgradeManager.Instance.CanUpgrade(Modules[i].ModuleType, statType))
                    {
                        UpgradeManager.Instance.UpgradeModuleStat(Modules[i].ModuleType, statType);
                        
                        RefreshUI();
                    }
                });
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponent<RectTransform>());
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(trans.GetComponent<RectTransform>());

        Get<Button>("ModuleBtn").onClick.SetListener(() =>
        {
            UIManager.Instance.Open<MaskGachaUI>();
        });

        Get<Text>("PointNum").text = UpgradeManager.Instance.UpgradePoints.ToString();

        playerPreview.RebuildPreview();
    }

    private void ChangeThemeColor()
    {

    }

    public override void OnPause()
    {
        base.OnPause();
    }

    public override void OnClose()
    {
        base.OnClose();
        Time.timeScale = 1f;
        InputManager.Instance.SetLockLevel(InputLockLevel.None);
    }
}
