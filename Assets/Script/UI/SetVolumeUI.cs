using UnityEngine;
using UnityEngine.UI;

public class SetVolumeUI : UIBase
{
    [Header("音量滑块组件")]
    public Slider bgmSlider;
    public Slider effectSlider;

    [Header("关闭按钮")]
    public Button closeButton;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        if (bgmSlider == null || effectSlider == null || closeButton == null)
        {
            Debug.LogError("请在Inspector面板给Slider和Button赋值");
            return;
        }

        // 初始化Slider
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 2f;
        bgmSlider.value = AudioManager.Instance.GetBGMVolume();

        effectSlider.minValue = 0f;
        effectSlider.maxValue = 2f;
        effectSlider.value = AudioManager.Instance.GetEffectVolume();

        // Slider回调
        bgmSlider.onValueChanged.AddListener(value =>
        {
            AudioManager.Instance.SetBGMVolume(value);
        });

        effectSlider.onValueChanged.AddListener(value =>
        {
            AudioManager.Instance.SetEffectVolume(value);
        });

        // 关闭按钮
        closeButton.onClick.SetListener(() =>
        {
            UIManager.Instance.CloseUI(this);
        });
    }

    public override void OnClose()
    {
        bgmSlider.onValueChanged.RemoveAllListeners();
        effectSlider.onValueChanged.RemoveAllListeners();
        base.OnClose();
    }
}
