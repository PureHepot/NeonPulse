using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameScreenRotator : MonoBehaviour
{
    [Header("引用")]
    public Camera mainCamera;
    public Camera uiCamera; // 如果有 UI 相机就拖进去，没有就留空

    [Header("设置")]
    public float duration = 0.8f;
    public Ease easeType = Ease.OutBack;
    public float zoomScale30 = 1.2f; // 旋转30度时稍微放大视野防止黑边

    private float initialOrthoSize;
    public float CurrentRotationZ { get; private set; } = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        initialOrthoSize = mainCamera.orthographicSize;
    }

    public void RotateTo(float targetAngle)
    {
        CurrentRotationZ = targetAngle;

        // 计算目标缩放：如果是倾斜状态，放大视野；否则恢复原状
        float targetSize = initialOrthoSize;
        if (Mathf.Abs(targetAngle) == 30f) targetSize = initialOrthoSize * zoomScale30;

        // 执行动画
        mainCamera.transform.DORotate(new Vector3(0, 0, targetAngle), duration).SetEase(easeType);
        mainCamera.DOOrthoSize(targetSize, duration).SetEase(easeType);

        if (uiCamera != null)
        {
            uiCamera.transform.DORotate(new Vector3(0, 0, targetAngle), duration).SetEase(easeType);
        }
    }

    public void ResetImmediate()
    {
        // 强制复位（用于P3进场）
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.transform.DOKill();
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.orthographicSize = initialOrthoSize;
        CurrentRotationZ = 0;
    }
}
