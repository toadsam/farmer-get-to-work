using System;
using System.Collections.Generic;

[Serializable]
public class UnlockedElementResultData
{
    public string unlockId;
    public string displayName;
}

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

    public List<UnlockedElementResultData> unlockedElements =
        new List<UnlockedElementResultData>();

    public int rewardStamina;

    public bool appliedToWorldState;

    public void AddUnlockedElement(string unlockId, string displayName)
    {
        if (string.IsNullOrEmpty(unlockId) && string.IsNullOrEmpty(displayName))
            return;

        if (unlockedElements == null)
            unlockedElements = new List<UnlockedElementResultData>();

        foreach (UnlockedElementResultData element in unlockedElements)
        {
            if (element == null)
                continue;

            if (element.unlockId == unlockId)
                return;
        }

        unlockedElements.Add(new UnlockedElementResultData
        {
            unlockId = unlockId,
            displayName = displayName
        });

        unlockedSomething = true;

        if (string.IsNullOrEmpty(unlockedId))
            unlockedId = unlockId;

        if (string.IsNullOrEmpty(unlockedDisplayName))
            unlockedDisplayName = displayName;
    }

    public int GetUnlockedElementCount()
    {
        if (unlockedElements == null)
            return 0;

        return unlockedElements.Count;
    }

    public string GetUnlockedDisplayNamesText()
    {
        if (unlockedElements == null || unlockedElements.Count == 0)
        {
            if (!string.IsNullOrEmpty(unlockedDisplayName))
                return unlockedDisplayName;

            return "";
        }

        string result = "";

        for (int i = 0; i < unlockedElements.Count; i++)
        {
            UnlockedElementResultData element = unlockedElements[i];

            if (element == null)
                continue;

            if (!string.IsNullOrEmpty(result))
                result += "\n";

            result += $"- {element.displayName}";
        }

        return result;
    }

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