public class DragonBossFireballState : BossBaseState
{
    private DragonBoss dragon;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        dragon = context as DragonBoss;
        dragon?.BeginAction(dragon.FireballRoutine());
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (dragon != null && dragon.IsActionFinished)
            dragon.SwitchState(dragon.IdleState);
    }
}
