using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FrameSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Config")]
    public string slotId;
    public ModuleCategory allowedCategories;

    public event Action<FrameSlotButton> OnSlotClicked;
    public event Action<FrameSlotButton> OnSlotHovered;
    public event Action<FrameSlotButton> OnSlotHoverExited;

    private Button button;
    private Image backgroundImage;
    private Image iconImage;
    private Color defaultBackgroundColor;

    public Image BackgroundImage => backgroundImage;
    public Image IconImage => iconImage;
    public Color DefaultBackgroundColor => defaultBackgroundColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
        iconImage = transform.Find("Image")?.GetComponent<Image>();
        defaultBackgroundColor = backgroundImage != null ? backgroundImage.color : Color.white;

        if (button != null)
            button.onClick.AddListener(() => OnSlotClicked?.Invoke(this));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnSlotHovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnSlotHoverExited?.Invoke(this);
    }
}
