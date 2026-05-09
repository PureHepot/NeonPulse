public class HUDUI : UIBase
{
    public HpBarUI hpBar;
    //public ExpBarUI expBar;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        if (hpBar != null)
            hpBar.OnEnter(null);
    }
}
