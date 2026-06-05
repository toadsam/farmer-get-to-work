using System;

[Serializable]
public class RewardResultData
{
    public bool finalSuccess;

    public string failReasonCode;
    public string failReasonMessage;

    public int focusedMinutes;

    public int rewardGrowth;
    public int rewardUnlockProgress;
    public int rewardGold;

    public bool hasExitPenalty;
    public float rewardMultiplier = 1f;

    public bool unlockedSomething;
    public string unlockedId;
    public string unlockedDisplayName;

    public int rewardStamina;

    public static RewardResultData CreateFailure(
        string reasonCode,
        string reasonMessage,
        int focusedMinutes
    )
    {
        return new RewardResultData
        {
            finalSuccess = false,
            failReasonCode = reasonCode,
            failReasonMessage = reasonMessage,
            focusedMinutes = focusedMinutes,
            rewardGrowth = 0,
            rewardUnlockProgress = 0,
            rewardGold = 0,
            rewardMultiplier = 0f
        };
    }

    public static RewardResultData CreateSuccess(
    int focusedMinutes,
    int rewardGrowth,
    int rewardUnlockProgress,
    int rewardGold,
    bool hasExitPenalty,
    float rewardMultiplier,
    int rewardStamina = 0
)
    {
        return new RewardResultData
        {
            finalSuccess = true,
            failReasonCode = "",
            failReasonMessage = "",
            focusedMinutes = focusedMinutes,
            rewardGrowth = rewardGrowth,
            rewardUnlockProgress = rewardUnlockProgress,
            rewardGold = rewardGold,
            rewardStamina = rewardStamina,
            hasExitPenalty = hasExitPenalty,
            rewardMultiplier = rewardMultiplier
        };
    }
}