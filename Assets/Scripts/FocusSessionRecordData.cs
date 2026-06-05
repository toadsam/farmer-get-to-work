using System;

[Serializable]
public class FocusSessionRecordData
{
    public string recordId;

    public string dateKey;

    public string goalType;
    public string goalName;

    public int plannedMinutes;
    public int focusedMinutes;

    public bool success;

    public int exitCount;
    public float totalExitSeconds;

    public int rewardGrowth;
    public int rewardUnlockProgress;
    public int rewardGold;

    public long startedAtTicks;
    public long endedAtTicks;

    public string failReasonCode;
    public string failReasonMessage;

    public static FocusSessionRecordData FromResult(
        FocusSessionResult result,
        RewardResultData reward
    )
    {
        if (result == null)
            return null;

        DateTime endedAt = result.endedAt == default ? DateTime.Now : result.endedAt;

        return new FocusSessionRecordData
        {
            recordId = Guid.NewGuid().ToString(),
            dateKey = endedAt.ToString("yyyy-MM-dd"),

            goalType = result.goalType,
            goalName = result.goalName,

            plannedMinutes = result.plannedMinutes,
            focusedMinutes = result.focusedMinutes,

            success = reward != null && reward.finalSuccess,

            exitCount = result.exitCount,
            totalExitSeconds = result.totalExitSeconds,

            rewardGrowth = reward != null ? reward.rewardGrowth : 0,
            rewardUnlockProgress = reward != null ? reward.rewardUnlockProgress : 0,
            rewardGold = reward != null ? reward.rewardGold : 0,

            startedAtTicks = result.startedAt == default ? 0 : result.startedAt.Ticks,
            endedAtTicks = endedAt.Ticks,

            failReasonCode = reward != null ? reward.failReasonCode : "",
            failReasonMessage = reward != null ? reward.failReasonMessage : ""
        };
    }
}