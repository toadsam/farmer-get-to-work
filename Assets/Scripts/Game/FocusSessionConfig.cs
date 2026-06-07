using System;
using UnityEngine;

[Serializable]
public class FocusSessionConfig
{
    [Header("Goal")]
    public string goalType;
    public string goalName;

    [Header("Time")]
    public int plannedMinutes;
    public int rewardMinutes;

    [Header("Test Mode")]
    public bool useTestDuration;
    public float testDurationSeconds;

    [Header("Optional")]
    public string musicTrackId;

    public long selectedAtTicks;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(goalType) &&
               !string.IsNullOrEmpty(goalName) &&
               plannedMinutes > 0 &&
               rewardMinutes > 0;
    }

    public float GetSessionDurationSeconds()
    {
        if (useTestDuration)
            return Mathf.Max(1f, testDurationSeconds);

        return Mathf.Max(1, plannedMinutes) * 60f;
    }

    public static FocusSessionConfig Create(
        string goalType,
        string goalName,
        int plannedMinutes,
        int rewardMinutes,
        bool useTestDuration = false,
        float testDurationSeconds = 0f
    )
    {
        return new FocusSessionConfig
        {
            goalType = goalType,
            goalName = goalName,
            plannedMinutes = plannedMinutes,
            rewardMinutes = rewardMinutes,
            useTestDuration = useTestDuration,
            testDurationSeconds = testDurationSeconds,
            selectedAtTicks = DateTime.Now.Ticks
        };
    }
}