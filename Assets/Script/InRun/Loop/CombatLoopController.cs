using UnityEngine;

public class CombatLoopController
{
    private CombatLoopRuntimeSaveData currentLoop;

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public float DurationSeconds { get; private set; }
    public float RemainingSeconds => Mathf.Max(0f, DurationSeconds - ElapsedSeconds);
    public float NormalizedTime => DurationSeconds > 0f ? Mathf.Clamp01(ElapsedSeconds / DurationSeconds) : 1f;

    public void StartLoop(CombatLoopRuntimeSaveData loopSave, float durationSeconds, bool resumeFromSave)
    {
        currentLoop = loopSave;
        DurationSeconds = Mathf.Max(0.01f, durationSeconds);
        ElapsedSeconds = resumeFromSave && loopSave != null
            ? Mathf.Clamp(loopSave.elapsedSeconds, 0f, DurationSeconds)
            : 0f;

        if (loopSave != null)
            loopSave.elapsedSeconds = ElapsedSeconds;

        IsComplete = ElapsedSeconds >= DurationSeconds;
        IsRunning = !IsComplete;
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning || IsComplete || deltaTime <= 0f)
            return;

        ElapsedSeconds = Mathf.Min(DurationSeconds, ElapsedSeconds + deltaTime);
        if (currentLoop != null)
            currentLoop.elapsedSeconds = ElapsedSeconds;

        if (ElapsedSeconds >= DurationSeconds)
        {
            IsRunning = false;
            IsComplete = true;
        }
    }

    public void Reset()
    {
        currentLoop = null;
        IsRunning = false;
        IsComplete = false;
        ElapsedSeconds = 0f;
        DurationSeconds = 0f;
    }
}
