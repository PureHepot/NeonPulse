using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HpBarUI : UIBase
{
    public Image hpFillImage;
    public Text hpText;
    public float smoothTime = 0.15f;

    private void Awake()
    {
        InitHpUI();
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        PlayerManager.Instance.OnHpChanged += UpdateHpUI;
    }

    public override void OnClose()
    {
        base.OnClose();
        PlayerManager.Instance.OnHpChanged -= UpdateHpUI;
        Destroy(gameObject);
    }

    private void InitHpUI()
    {
        if (PlayerManager.Instance != null)
        {
            UpdateHpUI(
                PlayerManager.Instance.CurrentHp,
                PlayerManager.Instance.MaxHealth
            );
        }
    }

    private void UpdateHpUI(int current, int max)
    {
        if (max <= 0) return;

        float percent = (float)current / max;

        hpFillImage.DOKill();
        hpFillImage.DOFillAmount(percent, smoothTime);

        hpText.text = $"HP:{current}/{max}";
    }
}
