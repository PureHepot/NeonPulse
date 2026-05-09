using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InRunConfigDatabase", menuName = "Game/InRun/Config Database")]
public class InRunConfigDatabase : ScriptableObject
{
    private static InRunConfigDatabase instance;

    public static InRunConfigDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<InRunConfigDatabase>("Configs/InRun/InRunConfigDatabase");
                if (instance == null)
                    instance = Resources.Load<InRunConfigDatabase>("Configs/InRunConfigDatabase");
            }

            return instance;
        }
    }

    [Header("Themes")]
    public List<BattleThemeConfig> allThemes = new();

    [Header("Global Loop Config")]
    public CombatLoopGlobalConfig loopGlobalConfig;

    [Header("Score")]
    public ScoreConfig scoreConfig;

    [Header("Pulse")]
    public PulseConfig pulseConfig;
}
