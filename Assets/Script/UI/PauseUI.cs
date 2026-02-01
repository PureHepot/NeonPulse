using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseUI : UIBase
{
    [Header("按钮")]
    public Button continueButton;
    //public Button settingsButton;
    public Button restartButton;
    public Button exitButton;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        Time.timeScale = 0;

        continueButton.onClick.SetListener(OnClickContinue);
        //settingsButton.onClick.SetListener(OnClickSettings);
        restartButton.onClick.SetListener(OnClickRestart);
        exitButton.onClick.SetListener(OnClickExit);
    }

    public override void OnClose()
    {
        continueButton.onClick.RemoveAllListeners();
        //settingsButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
        base.OnClose();
        Time.timeScale = 1f;
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
        UIManager.Instance.CloseUI(this);
        Time.timeScale = 1f;
        DestroyDontDestroyManagers();

        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    void OnClickExit()
    {
        Application.Quit();
    }

    private void DestroyDontDestroyManagers()
    {
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (AudioManager.Instance != null) Destroy(AudioManager.Instance.gameObject);
        if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
        if (PlayerManager.Instance != null) Destroy(PlayerManager.Instance.gameObject);
    }
}
