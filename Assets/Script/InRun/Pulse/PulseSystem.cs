using UnityEngine;

public class PulseSystem
{
    private PulseConfig config;
    private CombatLoopRuntimeSaveData currentLoop;

    public bool IsArmed { get; private set; }
    public bool WasTriggered { get; private set; }
    public KeyCode PulseKey => config != null ? config.pulseKey : KeyCode.R;

    public void Arm(PulseConfig pulseConfig, CombatLoopRuntimeSaveData loopSave)
    {
        config = pulseConfig;
        currentLoop = loopSave;
        WasTriggered = false;
        IsArmed = loopSave == null || !loopSave.pulseUsed;
    }

    public void Tick()
    {
        if (!IsArmed || WasTriggered)
            return;

        if (!Input.GetKeyDown(PulseKey))
            return;

        WasTriggered = true;
        IsArmed = false;

        if (currentLoop != null)
            currentLoop.pulseUsed = true;
    }

    public void ClearTrigger()
    {
        WasTriggered = false;
    }

    public void Reset()
    {
        config = null;
        currentLoop = null;
        IsArmed = false;
        WasTriggered = false;
    }
}
