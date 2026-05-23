using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : UIBase
{
    public HpBarUI hpBar;
    public Transform bossTitle;
    public TMP_Text bossCountText;
    //public ExpBarUI expBar;

    private void Awake()
    {
        if (hpBar == null)
            hpBar = GetComponentInChildren<HpBarUI>(true);

        if (bossTitle == null)
            bossTitle = transform.Find("BossTitle");

        if (bossCountText == null && bossTitle != null)
        {
            var bossCount = bossTitle.Find("BossCount");
            if (bossCount != null)
                bossCountText = bossCount.GetComponent<TMP_Text>();
        }
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        if (hpBar != null)
            hpBar.OnEnter(null);

        RefreshBossCount();
    }

    public override void OnClose()
    {
        if (hpBar != null)
            hpBar.OnClose();

        base.OnClose();
    }

    private void Update()
    {
        RefreshBossCount();
    }

    private void RefreshBossCount()
    {
        if (bossCountText == null)
            return;

        int defeatedBossCount = InRunDirector.ActiveInstance != null
            ? InRunDirector.ActiveInstance.CurrentBossKillCount
            : 0;

        bossCountText.text = defeatedBossCount.ToString();
    }
}
