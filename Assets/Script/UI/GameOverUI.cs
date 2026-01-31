using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : UIBase
{
    [Header("按钮组件")]
    public Button restartButton;   // 重新开始
    public Button quitButton;      // 退出游戏

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        transform.localScale = Vector3.one * 0.85f;

        if (restartButton == null || quitButton == null)
        {
            Debug.LogError("GameOverUI：请在Inspector中为restartButton和quitButton赋值");
            return;
        }

        // 清理旧监听
        restartButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        restartButton.onClick.AddListener(OnClickRestart);
        quitButton.onClick.AddListener(OnClickQuit);
    }

    public override void OnClose()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (quitButton != null)
            quitButton.onClick.RemoveAllListeners();
        base.OnClose();
    }

    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        UIManager.Instance.CloseUI(this);

        DestroyDontDestroyManagers();

        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    private void OnClickQuit()
    {
        // 直接退出游戏
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DestroyDontDestroyManagers()
    {
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (AudioManager.Instance != null) Destroy(AudioManager.Instance.gameObject);
        if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
        if (PlayerManager.Instance != null) Destroy(PlayerManager.Instance.gameObject);
    }
}