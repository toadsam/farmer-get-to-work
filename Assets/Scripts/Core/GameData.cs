using UnityEngine;

namespace FarmerGetToWork
{
    /// <summary>
    /// Prototype-only game data store.
    /// 나중에 저장/로드 시스템을 붙이기 전까지 씬 사이에서 값을 공유하기 위한 static 데이터입니다.
    /// </summary>
    public static class GameData
    {
        public static int gold = 12345;
        public static int totalFocusMinutesToday = 155;
        public static int weeklyFocusMinutes = 860;
        public static int streakDays = 7;
        public static string selectedGoalName = "공부하기";
        public static int selectedGoalMinutes = 30;
        public static int expectedRewardGold = 120;
        public static int farmLevel = 12;
        public static int unlockedItemCount = 24;
        public static int totalItemCount = 78;

        // FocusScene에서 성공 처리가 끝났는지 SuccessScene에 알려주는 임시 플래그입니다.
        public static bool pendingSuccessReward;

        public static void SetSelectedGoal(string goalName, int minutes, int rewardGold)
        {
            selectedGoalName = goalName;
            selectedGoalMinutes = Mathf.Clamp(minutes, 5, 180);
            expectedRewardGold = Mathf.Max(0, rewardGold);
            pendingSuccessReward = false;
        }

        public static void MarkFocusSessionSucceeded()
        {
            pendingSuccessReward = true;
        }

        public static void ApplyPendingSuccessReward()
        {
            if (!pendingSuccessReward)
            {
                return;
            }

            gold += expectedRewardGold;
            totalFocusMinutesToday += selectedGoalMinutes;
            weeklyFocusMinutes += selectedGoalMinutes;
            streakDays += 1;
            pendingSuccessReward = false;
        }

        public static void MarkFocusSessionFailed()
        {
            pendingSuccessReward = false;
        }

        public static bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (gold < amount)
            {
                return false;
            }

            gold -= amount;
            return true;
        }

        public static string FormatMinutesKorean(int minutes)
        {
            int hours = Mathf.Max(0, minutes) / 60;
            int mins = Mathf.Max(0, minutes) % 60;

            if (hours <= 0)
            {
                return $"{mins}분";
            }

            if (mins <= 0)
            {
                return $"{hours}시간";
            }

            return $"{hours}시간 {mins}분";
        }
    }
}
