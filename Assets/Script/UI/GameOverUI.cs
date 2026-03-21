using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : UIBase
{
    [Header("��ť���")]
    public Button restartButton;   // ���¿�ʼ
    public Button quitButton;      // �˳���Ϸ

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        transform.localScale = Vector3.one * 0.85f;

        if (restartButton == null || quitButton == null)
        {
            Debug.LogError("GameOverUI������Inspector��ΪrestartButton��quitButton��ֵ");
            return;
        }

        // �����ɼ���
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
        // 结算本局：死亡，记录到达的波次
        int waveReached = WaveManager.Instance.currentWaveIndex;
        DataManager.Instance.EndRun(false, waveReached);

        Time.timeScale = 1f;
        UIManager.Instance.CloseUI(this);

        DestroyDontDestroyManagers();

        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    private void OnClickQuit()
    {
        int waveReached = WaveManager.Instance.currentWaveIndex;
        DataManager.Instance.EndRun(false, waveReached);
        // ֱ���˳���Ϸ
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