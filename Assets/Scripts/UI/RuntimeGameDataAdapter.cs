using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmerGetToWork
{
    /// <summary>
    /// UI 프로토타입과 KBW 메인 게임 시스템 사이를 이어주는 공통 어댑터입니다.
    /// 기존 UI는 GameData를 백업 값으로 유지하되, 실제 플레이 데이터는 GameRoot 계열 런타임 시스템을 먼저 사용합니다.
    /// </summary>
    public static class RuntimeGameDataAdapter
    {
        public const string MainFarmSceneName = "KBW";

        public static global::SceneFlowManager SceneFlow
        {
            get
            {
                if (global::SceneFlowManager.Instance != null)
                    return global::SceneFlowManager.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::SceneFlowManager>();
            }
        }

        public static global::GameStateManager GameState
        {
            get
            {
                if (global::GameStateManager.Instance != null)
                    return global::GameStateManager.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::GameStateManager>();
            }
        }

        public static global::FocusSessionService FocusSession
        {
            get
            {
                if (global::FocusSessionService.Instance != null)
                    return global::FocusSessionService.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::FocusSessionService>();
            }
        }

        public static global::RewardProcessor RewardProcessor
        {
            get
            {
                if (global::RewardProcessor.Instance != null)
                    return global::RewardProcessor.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::RewardProcessor>();
            }
        }

        public static global::FarmManager Farm
        {
            get
            {
                if (global::FarmManager.Instance != null)
                    return global::FarmManager.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::FarmManager>();
            }
        }

        public static global::UnlockManager Unlocks
        {
            get
            {
                if (global::UnlockManager.Instance != null)
                    return global::UnlockManager.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::UnlockManager>();
            }
        }

        public static global::SessionRecordManager Records
        {
            get
            {
                if (global::SessionRecordManager.Instance != null)
                    return global::SessionRecordManager.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::SessionRecordManager>();
            }
        }

        public static global::SaveSystem Save
        {
            get
            {
                if (global::SaveSystem.Instance != null)
                    return global::SaveSystem.Instance;

                return UnityEngine.Object.FindAnyObjectByType<global::SaveSystem>();
            }
        }

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[RuntimeGameDataAdapter] 이동할 씬 이름이 비어 있습니다.");
                return;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            global::SceneFlowManager flow = SceneFlow;
            if (flow != null)
            {
                flow.LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public static void GoMainFarm()
        {
            LoadScene(MainFarmSceneName);
        }

        public static void SetSelectedSession(
            string goalName,
            int plannedMinutes,
            int expectedGoldForLegacyUI
        )
        {
            string safeGoalName = string.IsNullOrWhiteSpace(goalName) ? "공부하기" : goalName;
            int safeMinutes = Mathf.Clamp(plannedMinutes, 5, 180);

            GameData.SetSelectedGoal(safeGoalName, safeMinutes, expectedGoldForLegacyUI);

            global::GameStateManager gameState = GameState;
            if (gameState == null)
            {
                Debug.LogWarning("[RuntimeGameDataAdapter] GameStateManager가 없어 선택 세션을 런타임에 저장하지 못했습니다.");
                return;
            }

            gameState.SetSelectedGoal(
                ResolveGoalType(safeGoalName),
                safeGoalName,
                safeMinutes,
                safeMinutes
            );
        }

        public static FocusSessionConfig GetSelectedSessionOrFallback()
        {
            global::GameStateManager gameState = GameState;
            if (gameState != null && gameState.HasSelectedSession)
                return gameState.selectedSessionConfig;

            global::FocusSessionConfig fallback = global::FocusSessionConfig.Create(
                ResolveGoalType(GameData.selectedGoalName),
                GameData.selectedGoalName,
                Mathf.Clamp(GameData.selectedGoalMinutes, 5, 180),
                Mathf.Clamp(GameData.selectedGoalMinutes, 5, 180)
            );

            gameState?.SetSelectedSession(fallback);
            return fallback;
        }

        public static global::FocusSessionResult GetLastSessionResult()
        {
            global::GameStateManager gameState = GameState;
            if (gameState != null && gameState.lastSessionResult != null)
                return gameState.lastSessionResult;

            global::FocusSessionService service = FocusSession;
            return service == null ? null : service.LastResult;
        }

        public static global::RewardResultData GetLastRewardResult()
        {
            global::GameStateManager gameState = GameState;
            if (gameState != null && gameState.lastRewardResult != null)
                return gameState.lastRewardResult;

            global::FocusSessionService service = FocusSession;
            return service == null ? null : service.LastRewardResult;
        }

        public static string ResolveGoalType(string goalName)
        {
            if (string.IsNullOrWhiteSpace(goalName))
                return "Study";

            if (goalName.Contains("독서"))
                return "Reading";

            if (goalName.Contains("운동"))
                return "Exercise";

            if (goalName.Contains("수면"))
                return "Sleep";

            return "Study";
        }

        public static int GetExpectedGrowth(int minutes)
        {
            global::RewardProcessor rewardProcessor = RewardProcessor;
            int perMinute = rewardProcessor == null ? 4 : rewardProcessor.growthPerMinute;
            return Mathf.Max(0, minutes * perMinute);
        }

        public static int GetExpectedUnlockProgress(int minutes)
        {
            global::RewardProcessor rewardProcessor = RewardProcessor;
            int perMinute = rewardProcessor == null ? 2 : rewardProcessor.unlockProgressPerMinute;
            return Mathf.Max(0, minutes * perMinute);
        }

        public static int GetExpectedGold(int minutes, int legacyBaseReward)
        {
            global::RewardProcessor rewardProcessor = RewardProcessor;
            if (rewardProcessor != null && rewardProcessor.applyGoldReward)
                return Mathf.Max(0, minutes * rewardProcessor.goldPerMinute);

            return Mathf.Max(0, Mathf.RoundToInt(legacyBaseReward * (minutes / 30f)));
        }

        public static bool TrySpendGold(int amount)
        {
            global::FarmManager farm = Farm;
            if (farm != null)
            {
                bool spent = farm.SpendGold(amount);
                if (spent)
                    Save?.SaveGame();

                return spent;
            }

            return GameData.TrySpendGold(amount);
        }

        public static int GetGold()
        {
            global::FarmManager farm = Farm;
            if (farm != null)
                return farm.gold;

            global::SaveSystem save = Save;
            if (save != null && save.currentSaveData != null)
                return save.currentSaveData.gold;

            return GameData.gold;
        }

        public static int GetTodayFocusMinutes()
        {
            global::SessionRecordManager records = Records;
            if (records != null)
                return records.GetTodaySummary().totalFocusedMinutes;

            return GameData.totalFocusMinutesToday;
        }

        public static int GetWeeklyFocusMinutes()
        {
            global::SessionRecordManager records = Records;
            if (records == null)
                return GameData.weeklyFocusMinutes;

            int total = 0;
            foreach (global::SessionSummaryData summary in records.GetRecentDaySummaries(7))
            {
                if (summary != null)
                    total += summary.totalFocusedMinutes;
            }

            return total;
        }

        public static int GetTotalFocusedMinutes()
        {
            global::SessionRecordManager records = Records;
            if (records != null)
                return records.GetTotalFocusedMinutes();

            return GameData.weeklyFocusMinutes;
        }

        public static int GetStreakDays()
        {
            global::SessionRecordManager records = Records;
            if (records == null)
                return GameData.streakDays;

            int streak = 0;
            DateTime day = DateTime.Today;

            for (int i = 0; i < 365; i++)
            {
                List<global::FocusSessionRecordData> dayRecords =
                    records.GetRecordsByDateKey(GameDataUtility.ToDateKey(day));

                bool hasSuccess = false;
                foreach (global::FocusSessionRecordData record in dayRecords)
                {
                    if (record != null && record.success)
                    {
                        hasSuccess = true;
                        break;
                    }
                }

                if (!hasSuccess)
                    break;

                streak++;
                day = day.AddDays(-1);
            }

            return streak;
        }

        public static int GetBestSessionMinutes()
        {
            global::SessionRecordManager records = Records;
            if (records == null)
                return 185;

            int best = 0;
            foreach (global::FocusSessionRecordData record in records.GetAllRecords())
            {
                if (record != null)
                    best = Mathf.Max(best, record.focusedMinutes);
            }

            return best;
        }

        public static int GetFarmLevel()
        {
            global::UnlockManager unlocks = Unlocks;
            if (unlocks != null)
                return Mathf.Max(1, 1 + unlocks.totalProgress / 100);

            return GameData.farmLevel;
        }

        public static void GetUnlockedItemCounts(out int unlockedCount, out int totalCount)
        {
            global::UnlockManager unlocks = Unlocks;
            if (unlocks == null || unlocks.unlockEntries == null || unlocks.unlockEntries.Count == 0)
            {
                unlockedCount = GameData.unlockedItemCount;
                totalCount = GameData.totalItemCount;
                return;
            }

            unlockedCount = 0;
            totalCount = unlocks.unlockEntries.Count;

            foreach (global::UnlockEntry entry in unlocks.unlockEntries)
            {
                if (entry != null && entry.unlocked)
                    unlockedCount++;
            }
        }

        public static string FormatMinutesKorean(int minutes)
        {
            return GameData.FormatMinutesKorean(minutes);
        }
    }
}
