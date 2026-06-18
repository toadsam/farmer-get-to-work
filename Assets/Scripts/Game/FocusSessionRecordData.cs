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

    public string musicTrackId;

    public string beforeMoodId;
    public int beforeMoodScore;
    public int beforePhoneUrgeLevel;
    public string beforeReasonText;

    public string afterMoodId;
    public int afterMoodScore;
    public int afterPhoneUrgeLevel;
    public string afterReflectionText;

    public int musicHelpedLevel;
    public string musicReactionText;

    public int moodDelta;
    public int phoneUrgeDelta;

    public static FocusSessionRecordData FromResult(
        FocusSessionResult result,
        RewardResultData reward
    )
    {
        if (result == null)
            return null;

        DateTime endedAt = result.endedAt == default
            ? DateTime.Now
            : result.endedAt;

        SessionEmotionData emotion = result.emotionData;

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
            failReasonMessage = reward != null ? reward.failReasonMessage : "",

            musicTrackId = result.musicTrackId,

            beforeMoodId = emotion != null ? emotion.beforeMoodId : "",
            beforeMoodScore = emotion != null ? emotion.beforeMoodScore : 0,
            beforePhoneUrgeLevel = emotion != null ? emotion.beforePhoneUrgeLevel : 0,
            beforeReasonText = emotion != null ? emotion.beforeReasonText : "",

            afterMoodId = emotion != null ? emotion.afterMoodId : "",
            afterMoodScore = emotion != null ? emotion.afterMoodScore : 0,
            afterPhoneUrgeLevel = emotion != null ? emotion.afterPhoneUrgeLevel : 0,
            afterReflectionText = emotion != null ? emotion.afterReflectionText : "",

            musicHelpedLevel = emotion != null ? emotion.musicHelpedLevel : 0,
            musicReactionText = emotion != null ? emotion.musicReactionText : "",

            moodDelta = emotion != null ? emotion.MoodDelta : 0,
            phoneUrgeDelta = emotion != null ? emotion.PhoneUrgeDelta : 0
        };
    }
}