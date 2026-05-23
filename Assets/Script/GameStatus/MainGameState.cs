using UnityEngine;

public class MainGameState : GameState
{
    private readonly bool isContinue;
    private InRunDirector inRunDirector;

    public MainGameState(bool isContinue = false)
    {
        this.isContinue = isContinue;
    }

    public override void OnEnter()
    {
        var mgr = GameMgr.Instance;
        Time.timeScale = 1f;
        mgr.Audio.PlayBGM("FightBGM_2");

        if (!isContinue || !mgr.Data.HasActiveRun)
            StartRunSnapshot();

        mgr.Player.SpawnPlayer();
        mgr.UI.OpenFullScreen<HUDUI>();
        inRunDirector = InRunDirector.GetOrCreate();
        inRunDirector.BeginRun(isContinue);
        mgr.Data.Save();
    }

    public override void OnExit()
    {
        inRunDirector?.EndRunSession();
        SaveRunSnapshot();
        GameMgr.Instance.UI.CloseFullScreen();
        Time.timeScale = 1f;
    }

    public override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        var mgr = GameMgr.Instance;
        if (mgr.UI.CheckUIListEmpty())
        {
            SaveRunSnapshot();
            mgr.UI.Open<PauseUI>();
        }
        else
        {
            mgr.UI.CloseTopPanel();
        }
    }

    private void StartRunSnapshot()
    {
        int seed = Random.Range(0, int.MaxValue);
        string frameId = GameMgr.Instance.Data.GetPreferredFrameId();
        if (string.IsNullOrEmpty(frameId))
        {
            var db = GameConfigDatabase.Instance;
            if (db != null && db.allFrames != null && db.allFrames.Count > 0)
                frameId = db.allFrames[0].frameId;
        }

        GameMgr.Instance.Data.StartNewRun(seed, frameId);
    }

    private void SaveRunSnapshot()
    {
        var data = GameMgr.Instance.Data;
        GameMgr.Instance.Player.SavePlayerState();
        if (data.HasActiveRun)
            data.Save();
    }
}
