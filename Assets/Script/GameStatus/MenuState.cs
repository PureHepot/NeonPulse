using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuState : GameState
{
    private UIBase startUI;
    private GameObject startScene;

    public override void OnEnter()
    {
        if (Camera.main != null)
            Camera.main.orthographic = false;
        
        var mgr = GameMgr.Instance;
        bool hasSave = mgr.Data.HasActiveRun;
        startUI = mgr.UI.Open<StartUI>(hasSave);

        if (startScene == null)
            startScene = FindSceneObject("StartScene");

        if (startScene == null)
            Debug.LogWarning("[MenuState] StartScene not found in active scene.");

        startScene?.SetActive(true);

        mgr.Audio.PlayBGM("MainTheme");
    }

    public override void OnUpdate()
    {
    }

    public override void OnExit()
    {
        if (startUI != null)
            GameMgr.Instance.UI.CloseUI(startUI);

        startScene?.SetActive(false);

        if (Camera.main != null)
            Camera.main.orthographic = true;
    }

    private GameObject FindSceneObject(string objectName)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var result = FindChildRecursive(root.transform, objectName);
            if (result != null)
                return result.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform current, string targetName)
    {
        if (current.name == targetName)
            return current;

        for (int index = 0; index < current.childCount; index++)
        {
            var result = FindChildRecursive(current.GetChild(index), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}
