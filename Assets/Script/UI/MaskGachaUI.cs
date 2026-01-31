using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MaskGachaUI : UIBase
{
    [Header("References")]
    public RectTransform scrollContainer;
    public GameObject maskItemPrefab;
    private Button startButton;
    private Button closeButton;
    public Text costText;

    [Header("Animation Settings")]
    public int totalFakeItems = 15;
    public float scrollDuration = 3f;
    public float itemHeight = 120f;

    private bool isRolling = false;
    private Tween currentTween;

    private void Start()
    {
        startButton = Get<Button>("StartBtn");
        startButton.onClick.SetListener(OnStartClick);
        closeButton = Get<Button>("CloseBtn");
        closeButton.onClick.SetListener(OnCloseClick);
    }

    /// <summary>
    /// UIManager 打开此 UI 时调用
    /// </summary>
    /// <param name="args">可选参数</param>
    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        isRolling = false;

        Get<Text>("Title").text = "Search";

        UpdateUI();

        ClearContainer();
    }

    /// <summary>
    /// 当 UI 关闭时调用 (由 UIManager 触发)
    /// </summary>
    public override void OnClose()
    {
        // 杀死所有正在运行的动画，防止报错
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        base.OnClose();
    }

    private void UpdateUI()
    {
        if (costText)
            costText.text = $"COST: {MaskSystemManager.Instance.gachaCost} PTS";

        bool canAfford = MaskSystemManager.Instance.CanAfford();

        if (startButton)
            startButton.interactable = canAfford && !isRolling;

        if (closeButton)
            closeButton.interactable = !isRolling; // 抽奖过程中禁止关闭
    }

    private void OnStartClick()
    {
        if (isRolling) return;

        MaskConfig result = MaskSystemManager.Instance.RollGacha();
        if (result == null) return;
        Get<Text>("Title").text = "Searching";
        StartCoroutine(PlayGachaAnimation(result));
        UpdateUI();
    }

    private void OnCloseClick()
    {
        OnClose();
        UIManager.Instance.GetUI<LevelUpUI>().RefreshUI();
    }

    private IEnumerator PlayGachaAnimation(MaskConfig finalResult)
    {
        isRolling = true;
        if (startButton) startButton.interactable = false;
        if (closeButton) closeButton.interactable = false;

        ClearContainer();

        List<MaskConfig> pool = MaskSystemManager.Instance.maskPool;

        for (int i = 0; i < totalFakeItems; i++)
        {
            MaskConfig randomMask = pool[Random.Range(0, pool.Count)];
            CreateItem(randomMask);
        }

        // 放入真结果
        CreateItem(finalResult);

        // 放入垫底数据
        if (pool.Count > 0) CreateItem(pool[0]);

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContainer);

        Image edgeImg = Get<Image>("Edge"); // 确保Hierarchy里那个框的名字叫 "Edge"
        Color originalEdgeColor = Color.white;
        Tween rainbowTween = null;

        if (edgeImg != null)
        {
            originalEdgeColor = edgeImg.color;

            rainbowTween = DOVirtual.Float(0f, 1f, 0.5f, (value) =>
            {
                Color rainbowColor = Color.HSVToRGB(value, 0.8f, 1f);
                edgeImg.color = rainbowColor;
            })
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        }

        //开始滚动
        scrollContainer.anchoredPosition = new Vector2(scrollContainer.anchoredPosition.x, 0);
        float targetY = totalFakeItems * itemHeight - 0.25f * itemHeight;

        // 保存 Tween 引用
        currentTween = scrollContainer.DOAnchorPosY(targetY, scrollDuration)
            .SetEase(Ease.OutCubic).SetUpdate(true);

        yield return currentTween.WaitForCompletion();

        if (rainbowTween != null) rainbowTween.Kill(); //停止彩虹动画
        if (edgeImg != null)
        {
            edgeImg.color = originalEdgeColor; //恢复原来的颜色
        }

        Debug.Log("Gacha Finished: " + finalResult.maskName);

        if (scrollContainer.childCount > totalFakeItems)
        {
            Transform finalItem = scrollContainer.GetChild(totalFakeItems);
            yield return finalItem.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 0.5f)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        yield return new WaitForSecondsRealtime(0.5f);

        isRolling = false;
        Get<Text>("Title").text = "Search";
        UpdateUI();
    }

    private void CreateItem(MaskConfig data)
    {
        if (maskItemPrefab == null) return;

        GameObject obj = Instantiate(maskItemPrefab, scrollContainer);

        obj.transform.Find("Icon").GetComponent<Image>().sprite = data.icon;
        obj.transform.Find("Name").GetComponent<Text>().text = data.maskName;

        var layout = obj.GetComponent<LayoutElement>();
        if (layout) layout.minHeight = itemHeight;
    }

    private void ClearContainer()
    {
        foreach (Transform child in scrollContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
