using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config")]
public class WavesData : ScriptableObject
{
    public List<WaveData> allWaves;
}
