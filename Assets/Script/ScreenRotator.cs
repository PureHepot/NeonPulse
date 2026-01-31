using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening; // 必须引用 DOTween

public class ScreenRotator : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转持续时间")]
    public float duration = 1.0f;

    [Tooltip("旋转动画曲线 (推荐 OutBack 或 OutCubic)")]
    public Ease easeType = Ease.OutBack;

    private bool isRotating = false;

    // 测试用：按键触发
    private void Update()
    {
        // 按 Q 逆时针转 90度
        if (Input.GetKeyDown(KeyCode.Q)) RotateScreen(90);

        // 按 E 顺时针转 90度
        if (Input.GetKeyDown(KeyCode.E)) RotateScreen(-90);
    }

    /// <summary>
    /// 旋转屏幕
    /// </summary>
    /// <param name="angle">旋转角度 (例如 90 或 -90)</param>
    public void RotateScreen(float angle)
    {
        if (isRotating) return; // 防止连续触发导致鬼畜
        isRotating = true;

        // 使用 DOTween 旋转摄像机 Z 轴
        // RotateMode.LocalAxisAdd 表示在当前角度基础上增加，而不是旋转到绝对角度
        transform.DORotate(new Vector3(0, 0, angle), duration, RotateMode.LocalAxisAdd)
            .SetEase(easeType)
            .OnComplete(() => isRotating = false);
    }

    /// <summary>
    /// 重置回 0 度 (恢复正常)
    /// </summary>
    public void ResetRotation()
    {
        transform.DORotate(Vector3.zero, duration).SetEase(easeType);
    }
}
