using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

public class GameScreenRotator : MonoBehaviour
{
    [Header("摄像机引用")]
    public Camera mainCamera;
    public Camera uiCamera;

    [Header("旋转设置")]
    public float duration = 1.0f;
    public Ease easeType = Ease.OutBack;

    // 记录初始数据
    private float initialOrthoSize;
    private float rotatedOrthoSize;
    private bool isRotated = false;
    private bool isAnimating = false;

    // 公开这个变量，供玩家移动脚本读取，用来修正方向
    public float CurrentRotationZ { get; private set; } = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. 记录初始大小 (横屏时的 Size)
        initialOrthoSize = mainCamera.orthographicSize;

        // 2. 计算旋转后需要的大小 (竖屏时的 Size)
        // 原理：为了防止穿帮，我们需要让摄像机“拉远”，覆盖更宽的区域
        // 计算公式：初始大小 * (屏幕宽 / 屏幕高)
        float aspectRatio = (float)Screen.width / Screen.height;

        // 如果是宽屏(16:9)，ratio 约为 1.77。旋转后 Size 需要变大 1.77 倍才能填满宽度
        rotatedOrthoSize = initialOrthoSize * aspectRatio;
    }

    void Update()
    {
        // 测试按键
        if (Input.GetKeyDown(KeyCode.R) && !isAnimating)
        {
            ToggleRotation();
        }
    }

    public void ToggleRotation()
    {
        isAnimating = true;
        isRotated = !isRotated;

        // 目标角度
        float targetAngle = isRotated ? -90f : 0f;
        CurrentRotationZ = targetAngle; // 更新记录供输入修正使用

        // 目标视野大小
        float targetSize = isRotated ? rotatedOrthoSize : initialOrthoSize;

        // 创建序列
        Sequence seq = DOTween.Sequence();

        // 1. 旋转 Main Camera
        seq.Join(mainCamera.transform.DORotate(new Vector3(0, 0, targetAngle), duration).SetEase(easeType));
        // 2. 缩放 Main Camera (解决边界穿帮问题)
        seq.Join(mainCamera.DOOrthoSize(targetSize, duration).SetEase(easeType));

        // 3. 处理 UI Camera (如果需要)
        // 注意：UI通常不需要变焦，只需要旋转，除非你的UI是世界空间的
        if (uiCamera != null)
        {
            seq.Join(uiCamera.transform.DORotate(new Vector3(0, 0, targetAngle), duration).SetEase(easeType));
        }

        seq.OnComplete(() => isAnimating = false);
    }
}
