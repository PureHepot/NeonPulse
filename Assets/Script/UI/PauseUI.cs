using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseUI : UIBase
{
    [Header("按钮")]
    public Button continueButton;
    public Button settingsButton;
    public Button restartButton;
    public Button exitButton;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        continueButton.onClick.SetListener(OnClickContinue);
        settingsButton.onClick.SetListener(OnClickSettings);
        restartButton.onClick.SetListener(OnClickRestart);
        exitButton.onClick.SetListener(OnClickExit);
    }

    public override void OnClose()
    {
        continueButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
        base.OnClose();
    }

    void OnClickContinue()
    {
        Time.timeScale = 1f;
        UIManager.Instance.CloseUI(this);
    }

    void OnClickSettings()
    {
        UIManager.Instance.Open<SetVolumeUI>();
    }

    void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnClickExit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");  // 改成你主菜单场景名
    }
}
