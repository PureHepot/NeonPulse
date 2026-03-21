using UnityEngine;

public class MenuState : GameState
{
    private UIBase startUI;
    public override void OnEnter()
    {
        bool hasSave = DataManager.Instance.HasActiveRun;
        startUI = UIManager.Instance.Open<StartUI>(hasSave);

        AudioManager.Instance.PlayBGM("MainTheme");
        PlayerManager.Instance.spawnPoint.gameObject.SetActive(false);
        Debug.Log("UI: 显示开始按钮");
    }

    public override void OnUpdate()
    {
        if (InputManager.Instance.Esc())
        {
        }
    }

    public override void OnExit()
    {
        UIManager.Instance.CloseUI(startUI);
        GameObject.Find("StartScene").SetActive(false);
        Camera.main.orthographic = true;
    }
}
