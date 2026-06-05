using UnityEngine;

public class FocusSessionResultProcessor : MonoBehaviour
{
    [Header("References")]
    public UnlockManager unlockManager;

    [Header("Reward Rules")]
    [Tooltip("이 시간 미만의 세션은 보상을 지급하지 않습니다.")]
    public int minRewardMinutes = 10;

    [Tooltip("집중 1분당 작물 성장량입니다.")]
    public int growthPerMinute = 4;

    [Tooltip("집중 1분당 해금 진행도입니다.")]
    public int unlockProgressPerMinute = 2;

    [Header("Exit Penalty Rules")]
    [Tooltip("이탈 시간이 이 값 이하이면 패널티가 없습니다.")]
    public float shortExitGraceSeconds = 30f;

    [Tooltip("이탈 시간이 이 값 이상이면 세션 실패로 처리합니다.")]
    public float failExitSeconds = 180f;

    [Range(0f, 1f)]
    [Tooltip("짧은 허용 시간을 넘겼지만 실패 기준 미만일 때 지급할 보상 비율입니다.")]
    public float reducedRewardMultiplier = 0.7f;

    private void Awake()
    {
        if (unlockManager == null)
            unlockManager = FindAnyObjectByType<UnlockManager>();
    }

    public void ApplySessionResult(FocusSessionResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[FocusResult] result가 null입니다.", this);
            return;
        }

        bool finalSuccess = IsFinalSuccess(result);

        if (finalSuccess)
        {
            ApplySuccess(result);
        }
        else
        {
            ApplyFailure(result);
        }
    }

    private bool IsFinalSuccess(FocusSessionResult result)
    {
        if (!result.success)
            return false;

        if (result.focusedMinutes < minRewardMinutes)
            return false;

        if (result.totalExitSeconds >= failExitSeconds)
            return false;

        return true;
    }

    private void ApplySuccess(FocusSessionResult result)
    {
        int growth = CalculateRewardAmount(result, growthPerMinute);
        int unlockProgress = CalculateRewardAmount(result, unlockProgressPerMinute);

        if (FarmManager.Instance == null)
        {
            Debug.LogError("[FocusResult] FarmManager.Instance가 없습니다.", this);
            return;
        }

        FarmManager.Instance.AddGrowthToAllCrops(growth);

        if (unlockManager != null)
        {
            unlockManager.AddProgress(unlockProgress);
        }
        else
        {
            Debug.LogWarning("[FocusResult] UnlockManager가 연결되지 않았습니다.", this);
        }

        Debug.Log(
            $"[FocusResult] 성공 처리 완료 / 목표: {result.goalName}, " +
            $"집중 시간: {result.focusedMinutes}분, 성장량: +{growth}, " +
            $"해금 진행도: +{unlockProgress}, " +
            $"이탈 횟수: {result.exitCount}, 이탈 시간: {result.totalExitSeconds:F1}초"
        );
    }

    private int CalculateRewardAmount(FocusSessionResult result, int amountPerMinute)
    {
        int amount = result.focusedMinutes * amountPerMinute;

        bool hasExitPenalty =
            result.totalExitSeconds > shortExitGraceSeconds &&
            result.totalExitSeconds < failExitSeconds;

        if (hasExitPenalty)
            amount = Mathf.RoundToInt(amount * reducedRewardMultiplier);

        return Mathf.Max(0, amount);
    }

    private void ApplyFailure(FocusSessionResult result)
    {
        Debug.Log(
            $"[FocusResult] 실패 처리 / 목표: {result.goalName}, " +
            $"집중 시간: {result.focusedMinutes}분, " +
            $"이탈 횟수: {result.exitCount}, 이탈 시간: {result.totalExitSeconds:F1}초"
        );
    }

    [ContextMenu("Test Session Success 10 Min")]
    public void TestSessionSuccess10Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "수학 공부",
            plannedMinutes = 10,
            focusedMinutes = 10,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f
        };

        ApplySessionResult(result);
    }

    [ContextMenu("Test Session Success 30 Min")]
    public void TestSessionSuccess30Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "영어 단어 암기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f
        };

        ApplySessionResult(result);
    }

    [ContextMenu("Test Session Short Exit 30 Min")]
    public void TestSessionShortExit30Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Reading",
            goalName = "독서하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 1,
            totalExitSeconds = 45f
        };

        ApplySessionResult(result);
    }

    [ContextMenu("Test Session Fail Too Short")]
    public void TestSessionFailTooShort()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "짧은 공부",
            plannedMinutes = 5,
            focusedMinutes = 5,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f
        };

        ApplySessionResult(result);
    }

    [ContextMenu("Test Session Fail Long Exit")]
    public void TestSessionFailLongExit()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Exercise",
            goalName = "운동하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 2,
            totalExitSeconds = 200f
        };

        ApplySessionResult(result);
    }
}