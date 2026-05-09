using UnityEngine;

[CreateAssetMenu(fileName = "PulseConfig", menuName = "Game/InRun/Pulse Config")]
public class PulseConfig : ScriptableObject
{
    public KeyCode pulseKey = KeyCode.R;
    public float pulseClearRadius = -1f;
    public bool pulseClearedEnemiesGrantScore;
    public bool pulseClearedEnemiesTriggerDrops;
    public float pulseVfxDuration = 0.8f;
}
