using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : UIBase
{
    private PlayerPreview playerPreview;

    private void Awake()
    {
        playerPreview = GameObject.Find("PlayerModelCamera").GetComponent<PlayerPreview>();
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        Time.timeScale = 0f;

        RefreshUI();
    }

    public void RefreshUI()
    {
        List<PlayerModule> modules = PlayerManager.Instance.CurrentModules.GetAllActiveModules();

        Transform trans = Get<Transform>("ItemContent");

        trans.IteratorChild(modules.Count, iterator);

        void iterator(int index, Transform item)
        {
            int i = index;
            item.Find("Name").GetComponent<Text>().text = modules[i].moduleType.ToString();

            List<StatType> statTypes = UpgradeManager.Instance.GetUpgradedStats(modules[i].moduleType);

            item.Find("Detail_Container").IteratorChild(statTypes.Count, detailIterator);
            void detailIterator(int detailIndex, Transform detailItem)
            {
                int j = detailIndex;

                StatType statType = statTypes[j];
                detailItem.Find("Name").GetComponent<Text>().text = statType.ToString();
                int currentLevel = UpgradeManager.Instance.GetLevel(modules[i].moduleType, statType) + 1;
                detailItem.Find("LevelNum").GetComponent<Text>().text = currentLevel.ToString();
                string description = UpgradeManager.Instance.GetConfig(modules[i].moduleType).GetDescription(statType);
                detailItem.Find("Description").GetComponent<Text>().text = description;

                detailItem.Find("UpgradeBtn").GetComponent<Button>().onClick.SetListener(() =>
                {
                    if (UpgradeManager.Instance.CanUpgrade(modules[i].moduleType, statType))
                    {
                        UpgradeManager.Instance.UpgradeModuleStat(modules[i].moduleType, statType);
                        RefreshUI();
                    }
                });
            }
        }

        Get<Button>("ModuleBtn").onClick.SetListener(() =>
        {
            UIManager.Instance.Open<MaskGachaUI>();
            RefreshUI();
        });

        Get<Text>("PointNum").text = UpgradeManager.Instance.UpgradePoints.ToString();
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
    }
}
