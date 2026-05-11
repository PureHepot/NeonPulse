public class AssembleGameState : GameState
{
    private UIBase assembleUI;

    public override void OnEnter()
    {
        GameMgr.Instance.Data.ClearActiveRun();
        assembleUI = GameMgr.Instance.UI.Open<AssembleUI>();
    }

    public override void OnUpdate()
    {
    }

    public override void OnExit()
    {
        if (assembleUI != null)
            GameMgr.Instance.UI.CloseUI(assembleUI);
    }
}
