using System;

[Serializable]
public class FocusSessionRuntimeData
{
    public FocusSessionConfig config;

    public bool isRunning;
    public bool isCompleted;

    public float durationSeconds;
    public float remainingSeconds;
    public float elapsedSeconds;

    public int exitCount;
    public float totalExitSeconds;

    public long startedAtTicks;
    public long endedAtTicks;

    public float Progress01
    {
        get
        {
            if (durationSeconds <= 0f)
                return 0f;

            return elapsedSeconds / durationSeconds;
        }
    }

    public int GetFocusedMinutes()
    {
        if (config == null)
            return 0;

        float progress = Progress01;

        if (progress < 0f)
            progress = 0f;

        if (progress > 1f)
            progress = 1f;

        return UnityEngine.Mathf.FloorToInt(config.rewardMinutes * progress);
    }

    public void Reset()
    {
        config = null;

        isRunning = false;
        isCompleted = false;

        durationSeconds = 0f;
        remainingSeconds = 0f;
        elapsedSeconds = 0f;

        exitCount = 0;
        totalExitSeconds = 0f;

        startedAtTicks = 0;
        endedAtTicks = 0;
    }
}