using System;
using System.Collections.Generic;
using System.Text;

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

    public List<UnlockedRewardElementData> unlockedElements =
        new List<UnlockedRewardElementData>();

    public int rewardStamina;

    public bool appliedToWorldState;

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

    public void AddUnlockedElement(string id, string displayName)
    {
        if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(displayName))
            return;

        if (unlockedElements == null)
            unlockedElements = new List<UnlockedRewardElementData>();

        foreach (UnlockedRewardElementData element in unlockedElements)
        {
            if (element == null)
                continue;

            if (!string.IsNullOrEmpty(id) && element.unlockId == id)
                return;
        }

        unlockedElements.Add(new UnlockedRewardElementData
        {
            unlockId = id,
            displayName = displayName
        });

        unlockedSomething = true;

        if (string.IsNullOrEmpty(unlockedId))
            unlockedId = id;

        if (string.IsNullOrEmpty(unlockedDisplayName))
            unlockedDisplayName = displayName;
    }

    public int GetUnlockedElementCount()
    {
        int count = 0;

        if (unlockedElements != null)
        {
            foreach (UnlockedRewardElementData element in unlockedElements)
            {
                if (element != null)
                    count++;
            }
        }

        if (count == 0 &&
            unlockedSomething &&
            !string.IsNullOrEmpty(unlockedDisplayName))
        {
            count = 1;
        }

        return count;
    }

    public string GetUnlockedDisplayNamesText()
    {
        if (unlockedElements != null && unlockedElements.Count > 0)
        {
            StringBuilder builder = new StringBuilder();

            foreach (UnlockedRewardElementData element in unlockedElements)
            {
                if (element == null)
                    continue;

                if (string.IsNullOrEmpty(element.displayName))
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(element.displayName);
            }

            if (builder.Length > 0)
                return builder.ToString();
        }

        if (!string.IsNullOrEmpty(unlockedDisplayName))
            return unlockedDisplayName;

        return "새로운 요소";
    }
}

[Serializable]
public class UnlockedRewardElementData
{
    public string unlockId;
    public string displayName;
}