using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HpBarUI : UIBase
{
    public Image hpFillImage;
    public Text hpText;
    public float smoothTime = 0.15f;
    private bool isBound;

    private void Awake()
    {
        InitHpUI();
    }

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        BindPlayerHp();
        InitHpUI();
    }

    public override void OnClose()
    {
        UnbindPlayerHp();
        base.OnClose();
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

    private void BindPlayerHp()
    {
        if (isBound || PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.OnHpChanged += UpdateHpUI;
        isBound = true;
    }

    private void UnbindPlayerHp()
    {
        if (!isBound || PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.OnHpChanged -= UpdateHpUI;
        isBound = false;
    }

    private void UpdateHpUI(float current, float max)
    {
        if (max <= 0) return;

        float percent = current / max;

        hpFillImage.DOKill();
        hpFillImage.DOFillAmount(percent, smoothTime);

        int displayCurrent = current <= 0f ? 0 : Mathf.Max(1, Mathf.FloorToInt(current));
        int displayMax = Mathf.Max(1, Mathf.RoundToInt(max));
        hpText.text = $"{displayCurrent}/{displayMax}";
    }
}
