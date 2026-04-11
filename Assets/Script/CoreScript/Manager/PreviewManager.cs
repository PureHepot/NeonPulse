using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewManager : MonoSingleton<PreviewManager>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //生成玩家预览模型
    public void CreatePlayer(PreviewData data)
    {
        
    }
}

public class PreviewData
{
    public GameObject playerPrefab;
    public string uiLayerName = "UI_Model";
    public List<PlayerModule> needModules = new List<PlayerModule>();
    public Transform spawnPoint;

}