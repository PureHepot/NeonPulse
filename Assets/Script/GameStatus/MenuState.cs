using UnityEngine;

public class MenuState : GameState
{
    private UIBase startUI;
    public override void OnEnter()
    {
        // UIManager.Instance.PushPanel<MainMenuPanel>(); 
        startUI = UIManager.Instance.Open<StartUI>();

        AudioManager.Instance.PlayBGM("MainTheme");
        PlayerManager.Instance.spawnPoint.gameObject.SetActive(false);
        Debug.Log("UI: 显示开始按钮");
    }

    public override void OnUpdate()
    {
        if (InputManager.Instance.Esc()) // 假设空格开始
        {
            //manager.ChangeState(new GameplayState(manager));
        }
    }

    public override void OnExit()
    {
        // 关闭主菜单UI
        // UIManager.Instance.PopPanel();
        //UIManager.Instance.CloseAllPanels();
        UIManager.Instance.CloseUI(startUI);
        //更改摄像机模式
        GameObject.Find("StartScene").SetActive(false);
        Camera.main.orthographic = true;
    }
}