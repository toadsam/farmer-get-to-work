using System;

[Serializable]
public class SessionEmotionData
{
    public string beforeMoodId;
    public int beforeMoodScore;          // 1~5
    public int beforePhoneUrgeLevel;     // 1~5
    public string beforeReasonText;

    public string afterMoodId;
    public int afterMoodScore;           // 1~5
    public int afterPhoneUrgeLevel;      // 1~5
    public string afterReflectionText;

    public int musicHelpedLevel;         // 0~5, 0이면 미입력
    public string musicReactionText;

    public bool HasBeforeData()
    {
        return !string.IsNullOrEmpty(beforeMoodId) ||
               beforeMoodScore > 0 ||
               beforePhoneUrgeLevel > 0 ||
               !string.IsNullOrEmpty(beforeReasonText);
    }

    public bool HasAfterData()
    {
        return !string.IsNullOrEmpty(afterMoodId) ||
               afterMoodScore > 0 ||
               afterPhoneUrgeLevel > 0 ||
               !string.IsNullOrEmpty(afterReflectionText) ||
               musicHelpedLevel > 0 ||
               !string.IsNullOrEmpty(musicReactionText);
    }

    public int MoodDelta
    {
        get
        {
            if (beforeMoodScore <= 0 || afterMoodScore <= 0)
                return 0;

            return afterMoodScore - beforeMoodScore;
        }
    }

    public int PhoneUrgeDelta
    {
        get
        {
            if (beforePhoneUrgeLevel <= 0 || afterPhoneUrgeLevel <= 0)
                return 0;

            // 양수면 스마트폰 사용 욕구가 줄어든 것
            return beforePhoneUrgeLevel - afterPhoneUrgeLevel;
        }
    }

    public SessionEmotionData Clone()
    {
        return new SessionEmotionData
        {
            beforeMoodId = beforeMoodId,
            beforeMoodScore = beforeMoodScore,
            beforePhoneUrgeLevel = beforePhoneUrgeLevel,
            beforeReasonText = beforeReasonText,

            afterMoodId = afterMoodId,
            afterMoodScore = afterMoodScore,
            afterPhoneUrgeLevel = afterPhoneUrgeLevel,
            afterReflectionText = afterReflectionText,

            musicHelpedLevel = musicHelpedLevel,
            musicReactionText = musicReactionText
        };
    }
}