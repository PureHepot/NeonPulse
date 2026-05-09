using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PreviewGetConfigUI : MonoBehaviour
{
    public PreviewData previewData= new PreviewData
    {
        playerPrefab = null,
        uiLayerName = "UI_Model",
        needModules = new List<PlayerModule>(),
         spawnPoint = null
    };
/// <summary>
/// 获取ui信息
/// </summary>
/// <param name="transform">传入位置的信息</param>
/// <returns></returns>
    public PreviewData GetUIConfig(Transform transform)
    {
        PreviewData previewData=new PreviewData();
        previewData.needModules=PlayerManager.Instance.CurrentModules.GetAllActiveModules();
        //暂定为直接使用玩家预制体，后续可以根据需要定制专门的预览预制体
        previewData.playerPrefab=PlayerManager.Instance.playerPrefab;
        previewData.spawnPoint=transform;
        return previewData;
    }
    /// <summary>
    /// 获取UI摄像机位置
    /// </summary>
    /// <returns></returns>
    public Transform UICameraPos()
    {
        var camObj = GameObject.Find("UICamera").transform;
        return camObj;
    }
    /// <summary>
    /// 在UI摄像机位置自动获取UI配置
    /// </summary>
    public PreviewData AutoGetUIConfigInUICamera()
    {
        var camTransform = UICameraPos();
        return GetUIConfig(camTransform);
    }
}
