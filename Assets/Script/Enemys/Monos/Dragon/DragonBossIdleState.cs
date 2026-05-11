public class DragonBossIdleState : BossBaseState
{
    private DragonBoss dragon;
    private float idleDuration;

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        dragon = context as DragonBoss;
        idleDuration = dragon != null ? dragon.GetIdleDuration() : 0.5f;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (dragon != null && stateTimer >= idleDuration)
            dragon.SwitchState(dragon.ChooseNextAttackState());
    }
}
