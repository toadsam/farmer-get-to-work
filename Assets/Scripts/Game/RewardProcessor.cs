using System.Collections.Generic;
using UnityEngine;

public class RewardProcessor : MonoBehaviour
{
    public static RewardProcessor Instance { get; private set; }

    [Header("References")]
    public FarmManager farmManager;
    public UnlockManager unlockManager;
    public GameStateManager gameStateManager;
    public IslandSetManager islandSetManager;
    public FocusMusicPlayer focusMusicPlayer;

    [Header("Unlock Preview Database")]
    public UnlockPreviewDatabase unlockPreviewDatabase;
    public string unlockPreviewDatabaseResourcePath = "UnlockPreviewDatabase";

    [Header("Reward Rules")]
    [Tooltip("이 시간 미만의 세션은 보상을 지급하지 않습니다.")]
    public int minRewardMinutes = 10;

    [Tooltip("집중 1분당 작물 성장량입니다.")]
    public int growthPerMinute = 4;

    [Tooltip("집중 1분당 해금 진행도입니다.")]
    public int unlockProgressPerMinute = 2;

    [Tooltip("현재는 기본 0입니다. 나중에 세션 성공 골드 보상이 필요하면 사용합니다.")]
    public int goldPerMinute = 0;

    [Header("Exit Penalty Rules")]
    [Tooltip("이탈 시간이 이 값 이하이면 패널티가 없습니다.")]
    public float shortExitGraceSeconds = 30f;

    [Tooltip("이탈 시간이 이 값 이상이면 세션 실패로 처리합니다.")]
    public float failExitSeconds = 180f;

    [Range(0f, 1f)]
    [Tooltip("짧은 허용 시간을 넘겼지만 실패 기준 미만일 때 지급할 보상 비율입니다.")]
    public float reducedRewardMultiplier = 0.7f;

    [Header("Apply")]
    [Tooltip("성공 보상으로 모든 작물에 성장량을 적용합니다.")]
    public bool applyGrowthToCrops = true;

    [Tooltip("성공 보상으로 해금 진행도를 적용합니다.")]
    public bool applyUnlockProgress = true;

    [Tooltip("성공 보상으로 골드를 지급합니다. 현재 기본값은 꺼두는 것을 권장합니다.")]
    public bool applyGoldReward = false;

    [Header("Stamina Reward")]
    public bool applyStaminaReward = true;

    [Tooltip("성공 세션 1회당 회복할 스태미너입니다.")]
    public int staminaRewardOnSuccess = 2;

    [Header("Unlock Preview For Result UI")]
    public List<UnlockPreviewEntry> unlockPreviewEntries = new List<UnlockPreviewEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        RefreshReferences();
        LoadUnlockPreviewDatabaseIfNeeded();
    }

    private void RefreshReferences()
    {
        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (islandSetManager == null)
            islandSetManager = IslandSetManager.Instance;

        if (islandSetManager == null)
            islandSetManager = FindAnyObjectByType<IslandSetManager>();

        if (focusMusicPlayer == null)
            focusMusicPlayer = FocusMusicPlayer.Instance;

        if (focusMusicPlayer == null)
            focusMusicPlayer = FindAnyObjectByType<FocusMusicPlayer>();
    }

    public RewardResultData ProcessSessionResult(FocusSessionResult result)
    {
        RefreshReferences();

        RewardResultData reward = CalculateReward(result);

        ApplyReward(result, reward);

        if (gameStateManager != null)
            gameStateManager.SaveLastSessionResult(result, reward);

        LogRewardResult(result, reward);

        return reward;
    }

    public RewardResultData CalculateReward(FocusSessionResult result)
    {
        if (result == null)
        {
            return RewardResultData.CreateFailure(
                reasonCode: "NoResult",
                reasonMessage: "세션 결과가 없습니다.",
                focusedMinutes: 0
            );
        }

        if (result.totalExitSeconds >= failExitSeconds)
        {
            return RewardResultData.CreateFailure(
                reasonCode: "LongExit",
                reasonMessage: "앱 이탈 시간이 길어 집중 흐름이 끊겼습니다. 농장 성장은 적용되지 않았습니다.",
                focusedMinutes: result.focusedMinutes
            );
        }

        if (!result.success)
        {
            return RewardResultData.CreateFailure(
                reasonCode: "Abandoned",
                reasonMessage: "세션을 중단했습니다. 다음 세션에서 다시 이어갈 수 있습니다.",
                focusedMinutes: result.focusedMinutes
            );
        }

        if (result.focusedMinutes < minRewardMinutes)
        {
            return RewardResultData.CreateFailure(
                reasonCode: "TooShort",
                reasonMessage: "집중 시간이 최소 보상 시간보다 짧아 성장이 적용되지 않았습니다.",
                focusedMinutes: result.focusedMinutes
            );
        }

        bool hasExitPenalty =
            result.totalExitSeconds > shortExitGraceSeconds &&
            result.totalExitSeconds < failExitSeconds;

        float multiplier = hasExitPenalty ? reducedRewardMultiplier : 1f;

        int growth = CalculateAmount(result.focusedMinutes, growthPerMinute, multiplier);
        int unlockProgress = CalculateAmount(result.focusedMinutes, unlockProgressPerMinute, multiplier);
        int gold = CalculateAmount(result.focusedMinutes, goldPerMinute, multiplier);

        int staminaReward = applyStaminaReward ? staminaRewardOnSuccess : 0;

        RewardResultData reward = RewardResultData.CreateSuccess(
            focusedMinutes: result.focusedMinutes,
            rewardGrowth: growth,
            rewardUnlockProgress: unlockProgress,
            rewardGold: gold,
            hasExitPenalty: hasExitPenalty,
            rewardMultiplier: multiplier,
            rewardStamina: staminaReward
        );

        FillUnlockPreview(reward);

        return reward;
    }

    private void FillUnlockPreview(RewardResultData reward)
    {
        if (reward == null)
            return;

        if (!reward.finalSuccess)
            return;

        if (reward.rewardUnlockProgress <= 0)
            return;

        LoadUnlockPreviewDatabaseIfNeeded();

        if (unlockPreviewDatabase == null)
            return;

        int beforeProgress = GetCurrentUnlockProgressForPreview();
        int afterProgress = beforeProgress + reward.rewardUnlockProgress;

        UnlockPreviewEntry newlyUnlocked =
            unlockPreviewDatabase.FindNewlyUnlockedEntry(beforeProgress, afterProgress);

        if (newlyUnlocked == null)
            return;

        reward.unlockedSomething = true;
        reward.unlockedId = newlyUnlocked.unlockId;
        reward.unlockedDisplayName = newlyUnlocked.displayName;

        Debug.Log(
            $"[RewardProcessor] 해금 미리보기: {newlyUnlocked.displayName} " +
            $"({beforeProgress} → {afterProgress})"
        );
    }

    private int GetCurrentUnlockProgressForPreview()
    {
        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (unlockManager != null)
            return unlockManager.totalProgress;

        if (SaveSystem.Instance != null &&
            SaveSystem.Instance.currentSaveData != null)
        {
            return SaveSystem.Instance.currentSaveData.totalUnlockProgress;
        }

        return 0;
    }

    private int CalculateAmount(int focusedMinutes, int amountPerMinute, float multiplier)
    {
        int baseAmount = focusedMinutes * amountPerMinute;
        int finalAmount = Mathf.RoundToInt(baseAmount * multiplier);

        return Mathf.Max(0, finalAmount);
    }

    private void ApplyReward(FocusSessionResult result, RewardResultData reward)
    {
        if (reward == null)
            return;

        if (!reward.finalSuccess)
            return;

        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (applyGrowthToCrops && reward.rewardGrowth > 0)
        {
            bool appliedAnyGrowth = false;

            if (farmManager != null)
            {
                farmManager.AddGrowthToAllCrops(reward.rewardGrowth);
                appliedAnyGrowth = true;
            }

            if (islandSetManager != null)
            {
                islandSetManager.AddGrowthToActivatedIslandCrops(reward.rewardGrowth);
                appliedAnyGrowth = true;
            }

            if (!appliedAnyGrowth)
            {
                Debug.LogWarning("[RewardProcessor] 성장 보상을 적용할 농장 또는 섬 매니저가 없습니다.", this);
            }
        }

        if (applyUnlockProgress && reward.rewardUnlockProgress > 0)
        {
            if (unlockManager != null)
            {
                UnlockEntry nextBeforeUnlock = unlockManager.GetNextLockedEntry();

                unlockManager.AddProgress(reward.rewardUnlockProgress);

                if (focusMusicPlayer != null && unlockManager != null)
                {
                    focusMusicPlayer.RefreshUnlockedTracksFromProgress(unlockManager.totalProgress);
                }

                List<IslandSetController> newlyUnlockedIslands = null;

                if (islandSetManager != null)
                {
                    newlyUnlockedIslands =
                        islandSetManager.RefreshUnlocksFromProgress(unlockManager.totalProgress);
                    if (focusMusicPlayer != null && unlockManager != null)
                    {
                        focusMusicPlayer.RefreshUnlockedTracksFromProgress(unlockManager.totalProgress);
                    }
                }

                if (newlyUnlockedIslands != null && newlyUnlockedIslands.Count > 0)
                {
                    foreach (IslandSetController unlockedIsland in newlyUnlockedIslands)
                    {
                        if (unlockedIsland == null)
                            continue;

                        reward.AddUnlockedElement(
                            unlockedIsland.islandId,
                            unlockedIsland.displayName
                        );
                    }
                }
                else if (nextBeforeUnlock != null && nextBeforeUnlock.unlocked)
                {
                    reward.unlockedSomething = true;
                    reward.unlockedId = nextBeforeUnlock.unlockId;
                    reward.unlockedDisplayName = nextBeforeUnlock.displayName;
                }
            }
            else
            {
                Debug.LogWarning("[RewardProcessor] UnlockManager가 없어 해금 진행도를 적용하지 못했습니다.", this);
            }
        }

        if (applyGoldReward && reward.rewardGold > 0)
        {
            if (farmManager != null)
            {
                farmManager.AddGold(reward.rewardGold);
            }
            else
            {
                Debug.LogWarning("[RewardProcessor] FarmManager가 없어 골드 보상을 적용하지 못했습니다.", this);
            }
        }

        if (reward.rewardStamina > 0)
        {
            if (farmManager != null)
            {
                farmManager.AddStamina(reward.rewardStamina);
            }
            else
            {
                Debug.LogWarning("[RewardProcessor] FarmManager가 없어 스태미너 보상을 적용하지 못했습니다.", this);
            }
        }

        reward.appliedToWorldState = farmManager != null || unlockManager != null || islandSetManager != null;
    }

    private void LogRewardResult(FocusSessionResult result, RewardResultData reward)
    {
        if (result == null || reward == null)
            return;

        if (reward.finalSuccess)
        {
            Debug.Log(
                $"[RewardProcessor] 성공 보상 처리 / 목표: {result.goalName}, " +
                $"집중 {reward.focusedMinutes}분, 성장 +{reward.rewardGrowth}, " +
                $"해금 +{reward.rewardUnlockProgress}, 골드 +{reward.rewardGold}, " +
                $"보상 배율 {reward.rewardMultiplier:F2}"
            );

            if (reward.unlockedSomething)
            {
                Debug.Log(
                    $"[RewardProcessor] 새 항목 해금: {reward.unlockedDisplayName}"
                );
            }
        }
        else
        {
            Debug.Log(
                $"[RewardProcessor] 실패 처리 / 목표: {result.goalName}, " +
                $"사유: {reward.failReasonCode}, 메시지: {reward.failReasonMessage}"
            );
        }
    }

    public bool TryApplyLastRewardToCurrentWorld()
    {
        RefreshReferences();

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (gameStateManager == null)
        {
            Debug.LogWarning("[RewardProcessor] GameStateManager가 없어 마지막 보상을 적용할 수 없습니다.", this);
            return false;
        }

        FocusSessionResult result = gameStateManager.lastSessionResult;
        RewardResultData reward = gameStateManager.lastRewardResult;

        if (result == null || reward == null)
            return false;

        if (!reward.finalSuccess)
            return false;

        if (reward.appliedToWorldState)
            return false;

        bool applied = ApplyRewardToAvailableWorld(result, reward);

        if (applied)
        {
            reward.appliedToWorldState = true;
            gameStateManager.SaveLastSessionResult(result, reward);

            if (SaveSystem.Instance != null)
                SaveSystem.Instance.SaveGame();

            Debug.Log("[RewardProcessor] 대기 중이던 세션 보상을 현재 농장에 적용했습니다.");
        }

        return applied;
    }

    private bool ApplyRewardToAvailableWorld(FocusSessionResult result, RewardResultData reward)
    {
        RefreshReferences();

        bool hasAnyTarget = false;

        if (farmManager != null)
            hasAnyTarget = true;

        if (unlockManager != null)
            hasAnyTarget = true;

        if (islandSetManager != null)
            hasAnyTarget = true;

        if (!hasAnyTarget)
        {
            Debug.LogWarning("[RewardProcessor] 현재 씬에 보상을 적용할 대상 매니저가 없습니다.", this);
            return false;
        }

        if (applyGrowthToCrops && reward.rewardGrowth > 0)
        {
            if (farmManager != null)
                farmManager.AddGrowthToAllCrops(reward.rewardGrowth);

            if (islandSetManager != null)
                islandSetManager.AddGrowthToActivatedIslandCrops(reward.rewardGrowth);
        }

        if (applyUnlockProgress && reward.rewardUnlockProgress > 0 && unlockManager != null)
        {
            UnlockEntry nextBeforeUnlock = unlockManager.GetNextLockedEntry();

            unlockManager.AddProgress(reward.rewardUnlockProgress);

            List<IslandSetController> newlyUnlockedIslands = null;

            if (islandSetManager != null)
            {
                newlyUnlockedIslands =
                    islandSetManager.RefreshUnlocksFromProgress(unlockManager.totalProgress);
            }

            if (focusMusicPlayer != null)
            {
                focusMusicPlayer.RefreshUnlockedTracksFromProgress(unlockManager.totalProgress);
            }

            if (newlyUnlockedIslands != null && newlyUnlockedIslands.Count > 0)
            {
                foreach (IslandSetController unlockedIsland in newlyUnlockedIslands)
                {
                    if (unlockedIsland == null)
                        continue;

                    reward.AddUnlockedElement(
                        unlockedIsland.islandId,
                        unlockedIsland.displayName
                    );
                }
            }
            else if (nextBeforeUnlock != null && nextBeforeUnlock.unlocked)
            {
                reward.unlockedSomething = true;
                reward.unlockedId = nextBeforeUnlock.unlockId;
                reward.unlockedDisplayName = nextBeforeUnlock.displayName;
            }
        }

        if (applyGoldReward && reward.rewardGold > 0 && farmManager != null)
        {
            farmManager.AddGold(reward.rewardGold);
        }

        if (reward.rewardStamina > 0 && farmManager != null)
        {
            farmManager.AddStamina(reward.rewardStamina);
        }

        return true;
    }

    private void LoadUnlockPreviewDatabaseIfNeeded()
    {
        if (unlockPreviewDatabase != null)
            return;

        unlockPreviewDatabase =
            Resources.Load<UnlockPreviewDatabase>(unlockPreviewDatabaseResourcePath);

        if (unlockPreviewDatabase == null)
        {
            Debug.LogWarning(
                $"[RewardProcessor] UnlockPreviewDatabase를 찾지 못했습니다. " +
                $"Resources/{unlockPreviewDatabaseResourcePath}.asset 경로를 확인하세요.",
                this
            );
        }
    }



    [ContextMenu("Test Process Success 10 Min")]
    public void TestProcessSuccess10Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 10,
            focusedMinutes = 10,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f,
            startedAt = System.DateTime.Now.AddMinutes(-10),
            endedAt = System.DateTime.Now
        };

        ProcessSessionResult(result);
    }

    [ContextMenu("Test Process Success 30 Min")]
    public void TestProcessSuccess30Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f,
            startedAt = System.DateTime.Now.AddMinutes(-30),
            endedAt = System.DateTime.Now
        };

        ProcessSessionResult(result);
    }

    [ContextMenu("Test Process Short Exit 30 Min")]
    public void TestProcessShortExit30Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Reading",
            goalName = "독서하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 1,
            totalExitSeconds = 45f,
            startedAt = System.DateTime.Now.AddMinutes(-30),
            endedAt = System.DateTime.Now
        };

        ProcessSessionResult(result);
    }

    [ContextMenu("Test Process Long Exit 30 Min")]
    public void TestProcessLongExit30Min()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 1,
            totalExitSeconds = 200f,
            startedAt = System.DateTime.Now.AddMinutes(-30),
            endedAt = System.DateTime.Now
        };

        ProcessSessionResult(result);
    }

    [ContextMenu("Test Process Cancel")]
    public void TestProcessCancel()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 30,
            focusedMinutes = 6,
            success = false,
            exitCount = 0,
            totalExitSeconds = 0f,
            startedAt = System.DateTime.Now.AddMinutes(-6),
            endedAt = System.DateTime.Now
        };

        ProcessSessionResult(result);
    }
}