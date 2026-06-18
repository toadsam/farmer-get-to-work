using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SuccessResultRuntimeBinder : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text summaryText;
    public TMP_Text growthText;
    public TMP_Text unlockProgressText;
    public TMP_Text staminaText;
    public TMP_Text unlockedElementTitleText;
    public TMP_Text unlockedElementDescriptionText;
    public TMP_Text exitPenaltyText;

    [Header("Optional Roots")]
    public GameObject unlockedElementRoot;

    [Header("Unlock Preview Database")]
    public UnlockPreviewDatabase unlockPreviewDatabase;

    private void Start()
    {
        Refresh();
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        FocusSessionResult result = null;
        RewardResultData reward = null;

        if (GameStateManager.Instance != null)
        {
            result = GameStateManager.Instance.lastSessionResult;
            reward = GameStateManager.Instance.lastRewardResult;
        }

        if (result == null || reward == null)
        {
            SetFallback();
            return;
        }

        TryFillUnlockPreviewIfMissing(reward);

        if (titleText != null)
            titleText.text = "집중 성공!";

        if (summaryText != null)
            summaryText.text = $"{result.goalName} {result.focusedMinutes}분을 완료했어요.";

        if (exitPenaltyText != null)
            exitPenaltyText.text = BuildExitPenaltyText(result, reward);

        if (growthText != null)
            growthText.text = $"작물 성장 +{reward.rewardGrowth}";

        if (unlockProgressText != null)
            unlockProgressText.text = $"해금 진행도 +{reward.rewardUnlockProgress}";

        if (staminaText != null)
            staminaText.text = $"스태미너 +{reward.rewardStamina}";

        int unlockedCount = reward.GetUnlockedElementCount();

        bool hasUnlocked =
            unlockedCount > 0 ||
            (
                reward.unlockedSomething &&
                !string.IsNullOrEmpty(reward.unlockedDisplayName)
            );

        if (unlockedElementRoot != null)
            unlockedElementRoot.SetActive(hasUnlocked);

        if (hasUnlocked)
        {
            if (unlockedElementTitleText != null)
            {
                if (unlockedCount > 1)
                    unlockedElementTitleText.text = $"새로운 요소 {unlockedCount}개 해금!";
                else
                    unlockedElementTitleText.text = "새로운 요소 해금!";
            }

            if (unlockedElementDescriptionText != null)
            {
                string namesText = reward.GetUnlockedDisplayNamesText();

                if (unlockedCount > 1)
                    unlockedElementDescriptionText.text =
                        $"{namesText}\n농장으로 돌아가 확인해보세요.";
                else
                    unlockedElementDescriptionText.text =
                        $"{namesText}을(를) 확인해보세요.";
            }
        }
        else
        {
            if (unlockedElementTitleText != null)
                unlockedElementTitleText.text = "";

            if (unlockedElementDescriptionText != null)
                unlockedElementDescriptionText.text = "";
        }

        Debug.Log(
            $"[SuccessResultBinder] 표시 완료 / " +
            $"해금 여부: {hasUnlocked}, " +
            $"해금 개수: {unlockedCount}, " +
            $"해금 이름: {reward.GetUnlockedDisplayNamesText()}, " +
            $"해금 진행도 +{reward.rewardUnlockProgress}"
        );
    }

    private void TryFillUnlockPreviewIfMissing(RewardResultData reward)
    {
        if (reward == null)
            return;

        if (!reward.finalSuccess)
            return;

        if (reward.unlockedSomething)
            return;

        if (reward.rewardUnlockProgress <= 0)
            return;

        if (unlockPreviewDatabase == null)
        {
            Debug.LogWarning(
                "[SuccessResultBinder] UnlockPreviewDatabase가 연결되어 있지 않습니다.",
                this
            );
            return;
        }

        int beforeProgress = GetCurrentUnlockProgressBeforeReward();
        int afterProgress = beforeProgress + reward.rewardUnlockProgress;

        List<UnlockPreviewEntry> entries =
    unlockPreviewDatabase.FindNewlyUnlockedEntries(beforeProgress, afterProgress);

        if (entries == null || entries.Count == 0)
        {
            Debug.Log(
                $"[SuccessResultBinder] 이번 세션에서 새로 해금된 요소 없음 / " +
                $"{beforeProgress} → {afterProgress}"
            );
            return;
        }

        foreach (UnlockPreviewEntry entry in entries)
        {
            if (entry == null)
                continue;

            reward.AddUnlockedElement(entry.unlockId, entry.displayName);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveLastSessionResult(
                GameStateManager.Instance.lastSessionResult,
                reward
            );
        }

        Debug.Log(
            $"[SuccessResultBinder] 해금 미리보기 표시: {entries.Count}개 / " +
            $"{beforeProgress} → {afterProgress}"
        );
    }

    private int GetCurrentUnlockProgressBeforeReward()
    {
        if (SaveSystem.Instance != null &&
            SaveSystem.Instance.currentSaveData != null)
        {
            return SaveSystem.Instance.currentSaveData.totalUnlockProgress;
        }

        if (UnlockManager.Instance != null)
            return UnlockManager.Instance.totalProgress;

        return 0;
    }

    private void SetFallback()
    {
        if (titleText != null)
            titleText.text = "집중 성공!";

        if (summaryText != null)
            summaryText.text = "세션을 완료했어요.";

        if (growthText != null)
            growthText.text = "작물 성장 +0";

        if (unlockProgressText != null)
            unlockProgressText.text = "해금 진행도 +0";

        if (staminaText != null)
            staminaText.text = "스태미너 +0";

        if (exitPenaltyText != null)
            exitPenaltyText.text = "앱 이탈 0회 / 0.0초\n패널티 없음";

        if (unlockedElementRoot != null)
            unlockedElementRoot.SetActive(false);
    }

    private string BuildExitPenaltyText(
    FocusSessionResult result,
    RewardResultData reward
)
    {
        if (result == null)
            return "";

        if (reward == null)
            return $"앱 이탈: {result.exitCount}회 / {result.totalExitSeconds:F1}초";

        if (!reward.finalSuccess)
        {
            if (reward.failReasonCode == "LongExit")
            {
                return
                    $"앱 이탈 시간이 {result.totalExitSeconds:F1}초로 180초를 넘어 " +
                    "세션 실패로 처리되었습니다.";
            }

            if (!string.IsNullOrEmpty(reward.failReasonMessage))
                return reward.failReasonMessage;

            return $"앱 이탈: {result.exitCount}회 / {result.totalExitSeconds:F1}초";
        }

        if (reward.hasExitPenalty)
        {
            int percent = Mathf.RoundToInt(reward.rewardMultiplier * 100f);

            return
                $"앱 이탈 {result.exitCount}회 / {result.totalExitSeconds:F1}초\n" +
                $"보상 {percent}% 적용";
        }

        return
            $"앱 이탈 {result.exitCount}회 / {result.totalExitSeconds:F1}초\n" +
            "패널티 없음";
    }
}