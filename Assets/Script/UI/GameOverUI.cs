using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : UIBase
{
    [Header("Buttons")]
    public Button restartButton;
    public Button quitButton;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);
        transform.localScale = Vector3.one * 0.85f;

        if (restartButton == null || quitButton == null)
        {
            Debug.LogError("GameOverUI is missing restartButton or quitButton binding.");
            return;
        }

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
        int waveReached = 0;
        DataManager.Instance.EndRun(false, waveReached);

        Time.timeScale = 1f;
        UIManager.Instance.CloseUI(this);
        GameMgr.Instance.Game.ChangeState(new AssembleGameState());
    }

    private void OnClickQuit()
    {
        int waveReached = 0;
        DataManager.Instance.EndRun(false, waveReached);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
