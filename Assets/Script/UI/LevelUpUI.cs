using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : UIBase
{
    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        Init();
    }

    private void Init()
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
                    UpgradeManager.Instance.UpgradeModuleStat(modules[i].moduleType, statType);
                    Init();
                });
            }
        }

        Get<Button>("ModuleBtn").onClick.SetListener(() =>
        {
            //UIManager.Instance.Open<>();
            Init();
        });
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
    }
}
