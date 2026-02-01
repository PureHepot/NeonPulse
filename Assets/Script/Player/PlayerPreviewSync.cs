using UnityEngine;
using System.Collections.Generic;

public class PlayerPreviewSync : MonoBehaviour
{
    [Header("Settings")]
    public GameObject playerPrefab; // 原始玩家预制体
    public Transform previewRoot;   // UI Camera 拍摄的位置
    public string uiLayerName = "UI_Model"; // UI 专用 Layer

    private GameObject dummyPlayer;
    private PlayerModuleManager dummyModules;
    private SpriteRenderer dummyRenderer;

    private void OnEnable()
    {
        RebuildPreview();

        EventManager.AddListener<ModuleType>(GameEvent.PlayerUIModelUnlock, OnModuleUnlock);
        EventManager.AddListener<ModuleType>(GameEvent.PlayerUIModelLock, OnModuleLock);
        EventManager.AddListener(GameEvent.PlayerSkinChanged, OnSkinChanged);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener<ModuleType>(GameEvent.PlayerUIModelUnlock, OnModuleUnlock);
        EventManager.RemoveListener<ModuleType>(GameEvent.PlayerUIModelLock, OnModuleLock);
        EventManager.RemoveListener(GameEvent.PlayerSkinChanged, OnSkinChanged);
    }

    /// <summary>
    /// 完全重建预览模型
    /// </summary>
    public void RebuildPreview()
    {
        // 清理旧模型
        if (dummyPlayer != null) Destroy(dummyPlayer);

        // 生成新模型
        dummyPlayer = Instantiate(playerPrefab, previewRoot.position, Quaternion.identity, previewRoot);
        dummyPlayer.transform.SetPositionZ(1);
        dummyPlayer.tag = "Untagged";

        NeutralizeComponents(dummyPlayer);

        // 设置 Layer
        SetLayerRecursively(dummyPlayer, LayerMask.NameToLayer(uiLayerName));

        // 获取组件引用
        dummyModules = dummyPlayer.GetComponent<PlayerModuleManager>();
        dummyRenderer = dummyPlayer.GetComponentInChildren<SpriteRenderer>();

        if (dummyModules != null)
        {
            var pc = dummyPlayer.GetComponent<PlayerController>();

            dummyModules.Initialize?.Invoke();
        }

        SyncData();
    }

    private void NeutralizeComponents(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // 关键：不受物理影响
            rb.simulated = false; // 进一步确保它不参与碰撞检测
            rb.velocity = Vector2.zero;
        }

        var controller = obj.GetComponent<PlayerController>();
        if (controller)
        {
            controller.isPreview = true;
            controller.enabled = false; // 禁止它响应输入
        }

        var colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = false;
    }
    private void SyncData()
    {
        // 同步外观
        if (MaskSystemManager.Instance.currentMask != null)
        {
            UpdateVisuals(MaskSystemManager.Instance.currentMask);
        }

        // 同步模块
        var unlockedTypes = UpgradeManager.Instance.UnlockedModuleTypes;
        if (dummyModules != null)
        {
            foreach (var type in unlockedTypes)
            {
                dummyModules.UnlockModule(type);
            }
        }
    }

    // --- 事件回调 ---

    private void OnModuleUnlock(ModuleType type)
    {
        if (dummyModules != null)
        {
            dummyModules.UnlockModule(type);
            // 确保新生成的模块也是 UI Layer
            SetLayerRecursively(dummyPlayer, LayerMask.NameToLayer(uiLayerName));
        }
    }

    private void OnModuleLock(ModuleType type)
    {
        if (dummyModules != null)
        {
            dummyModules.DisableModule(type);
        }
    }

    private void OnSkinChanged()
    {
        var mask = MaskSystemManager.Instance.currentMask;
        if (mask != null)
        {
            UpdateVisuals(mask);
        }
    }

    private void OnModuleUpgrade(ModuleType type, StatType stat)
    {
        if (dummyModules != null)
        {
            dummyModules.UpgradeModule(type, stat);
        }
    }

    private void UpdateVisuals(MaskConfig mask)
    {
        if (dummyRenderer != null)
        {
            if (mask.bodySprite) dummyRenderer.sprite = mask.bodySprite;
            dummyRenderer.color = mask.themeColor;
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // 让模型在 UI 里自转，增加动感
    private void Update()
    {
        if (dummyPlayer != null)
        {
            dummyPlayer.transform.Rotate(0, 0, -20 * Time.unscaledDeltaTime);
        }
    }
}