using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

public class PreviewManager : MonoSingleton<PreviewManager>
{
    private GameObject currentPreview;
    /// <summary>
    /// 生成预览模型
    /// </summary>
    /// <param name="data"></param>
    public void CreatePreviewPlayer(PreviewData data)
    {
        if (currentPreview != null) DestroyPreview(currentPreview);
        currentPreview = Instantiate(data.playerPrefab, data.spawnPoint.position, Quaternion.identity,data.spawnPoint);
        currentPreview.transform.SetPositionZ(1);
        currentPreview.tag = "Untagged";
        ModuleManager moduleManager = currentPreview.GetComponent<ModuleManager>();
        moduleManager.Initialize?.Invoke();
        if(currentPreview.activeInHierarchy==false) ShowPreview(currentPreview);
        if(data.moduleDict!=null) UpdateModules(data.moduleDict,currentPreview);
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer(data.uiLayerName));

    }
    /// <summary>
    /// 根据模块数据更新预览模型的状态，解锁和升级对应的模组
    /// </summary>
    /// <param name="modules"></param>
    /// <param name="moduleManager"></param>
    public void UpdateModules(Dictionary<StatType, PlayerModule> modules,GameObject previewObject)
    {
        ModuleManager moduleManager = previewObject.GetComponent<ModuleManager>();
        foreach(var module in modules)
        {
            moduleManager.UnlockModule(module.Value.moduleType);
            SetLayerRecursively(previewObject, LayerMask.NameToLayer("UI_Model"));
            moduleManager.UpgradeModule(module.Value.moduleType, module.Key);
        }
    }
    /// <summary>
    /// 不销毁，只展示
    /// </summary>
    /// <param name="previewObject"></param>
    public void ShowPreview(GameObject previewObject)
    {
        if(previewObject!=null)
        {
            previewObject.SetActive(true);
        }
    }
    /// <summary>
    /// 隐藏
    /// </summary>
    /// <param name="previewObject"></param>
    public void HidePreview(GameObject previewObject)
    {
        if(previewObject!=null)
        {
            previewObject.SetActive(false);
        }
    }
    /// <summary>
    /// 只加一个东西使用，Todo：升级时候不重构整个物体
    /// </summary>
    /// <param name="previewObject"></param>
    /// <param name="data"></param>
    public void UpdatePreview(GameObject previewObject, PreviewData data)
    {
        
    }
    /// <summary>
    /// 正常摧毁
    /// </summary>
    /// <param name="previewObject"></param>
    public void DestroyPreview(GameObject previewObject)
    {
        if (previewObject != null) Destroy(previewObject);
    }
    /// <summary>
    /// 照抄的。可能要改
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="newLayer"></param>
    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    /// <summary>
    /// 待做，升级时候不重构整个物体，只是改变模组加载状态
    /// </summary>
    /// <param name="modules"></param>
    /// <param name="data"></param>
    public void ChangeModulesPerformance(PreviewData data,ModuleManager moduleManager)
    {
        if (moduleManager == null) return;

        foreach (var module in data.needModules)
        {
        }
    }
    
}

public class PreviewData
{
    public GameObject playerPrefab;
    public string uiLayerName = "UI_Model";
    public List<PlayerModule> needModules = new List<PlayerModule>();
    public Transform spawnPoint;

    //等级传入加上模块
    public Dictionary<StatType, PlayerModule> moduleDict = new Dictionary<StatType, PlayerModule>();
}