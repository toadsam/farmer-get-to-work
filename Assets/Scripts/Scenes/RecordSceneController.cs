using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 기록 화면의 UI를 KBW 메인 런타임의 SessionRecordManager와 FarmManager 기준으로 갱신합니다.
    /// 주간 그래프는 각 Img_Bar_* RectTransform 높이로 표현합니다.
    /// </summary>
    public class RecordSceneController : MonoBehaviour
    {
        [SerializeField] private List<Image> weeklyBars = new List<Image>();
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI commentText;
        [SerializeField] private List<AchievementCardUI> achievementCards = new List<AchievementCardUI>();

        private readonly int[] fallbackWeeklyMinutes = { 90, 130, 185, 165, 140, 110, 80 };

        private void Awake()
        {
            Bind();
        }

        private void Start()
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshStatPanels();
            RefreshWeeklyChart();
            RefreshAchievements();
        }

        private void RefreshStatPanels()
        {
            foreach (StatPanelUI panel in FindObjectsByType<StatPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                panel.RefreshFromGameData();
            }
        }

        private void RefreshWeeklyChart()
        {
            int[] weeklyMinutes = GetWeeklyMinutes();
            int maxMinutes = 1;
            foreach (int minutes in weeklyMinutes)
            {
                maxMinutes = Mathf.Max(maxMinutes, minutes);
            }

            for (int i = 0; i < weeklyBars.Count && i < weeklyMinutes.Length; i++)
            {
                RectTransform rect = weeklyBars[i].rectTransform;
                float height = Mathf.Lerp(80f, 260f, weeklyMinutes[i] / (float)maxMinutes);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }

            UIBinder.SetText(totalTimeText, $"이번 주 {RuntimeGameDataAdapter.FormatMinutesKorean(RuntimeGameDataAdapter.GetWeeklyFocusMinutes())}");
            UIBinder.SetText(commentText, CreateWeeklyComment(weeklyMinutes));
        }

        private void RefreshAchievements()
        {
            string[] titles =
            {
                "새싹 농부", "꾸준한 농부", "성실한 농부", "부지런한 농부", "황금 농부"
            };

            string[] descriptions =
            {
                "집중 시간 10시간 달성",
                "연속 성공 7일 달성",
                "집중 시간 50시간 달성",
                "연속 성공 30일 달성",
                "골드 50,000개 획득"
            };

            string[] progress =
            {
                $"{RuntimeGameDataAdapter.GetTotalFocusedMinutes() / 60}/10시간",
                $"{RuntimeGameDataAdapter.GetStreakDays()}/7일",
                $"{RuntimeGameDataAdapter.GetTotalFocusedMinutes() / 60}/50시간",
                $"{RuntimeGameDataAdapter.GetStreakDays()}/30일",
                $"{RuntimeGameDataAdapter.GetGold():N0}/50,000"
            };

            bool[] unlocked =
            {
                RuntimeGameDataAdapter.GetTotalFocusedMinutes() >= 600,
                RuntimeGameDataAdapter.GetStreakDays() >= 7,
                RuntimeGameDataAdapter.GetTotalFocusedMinutes() >= 3000,
                RuntimeGameDataAdapter.GetStreakDays() >= 30,
                RuntimeGameDataAdapter.GetGold() >= 50000
            };

            for (int i = 0; i < achievementCards.Count && i < titles.Length; i++)
            {
                achievementCards[i].SetData(titles[i], descriptions[i], progress[i], unlocked[i]);
            }
        }

        private int[] GetWeeklyMinutes()
        {
            SessionRecordManager records = RuntimeGameDataAdapter.Records;
            if (records == null)
                return fallbackWeeklyMinutes;

            List<SessionSummaryData> summaries = records.GetRecentDaySummaries(7);
            if (summaries == null || summaries.Count == 0)
                return fallbackWeeklyMinutes;

            int[] result = new int[7];
            for (int i = 0; i < result.Length && i < summaries.Count; i++)
            {
                result[i] = summaries[i] == null ? 0 : summaries[i].totalFocusedMinutes;
            }

            return result;
        }

        private string CreateWeeklyComment(int[] weeklyMinutes)
        {
            string[] dayNames = { "월요일", "화요일", "수요일", "목요일", "금요일", "토요일", "일요일" };
            int bestIndex = 0;
            int bestMinutes = 0;

            for (int i = 0; i < weeklyMinutes.Length; i++)
            {
                if (weeklyMinutes[i] > bestMinutes)
                {
                    bestMinutes = weeklyMinutes[i];
                    bestIndex = i;
                }
            }

            if (bestMinutes <= 0)
                return "아직 이번 주 집중 기록이 없어요.\n첫 세션을 완료하면 기록이 채워져요!";

            return $"{dayNames[bestIndex]}이 가장 집중력이 좋았어요!\n좋은 페이스예요! 계속해봐요!";
        }

        private void Bind()
        {
            if (weeklyBars.Count == 0)
            {
                foreach (string name in new[]
                {
                    "Img_Bar_Mon", "Img_Bar_Tue", "Img_Bar_Wed", "Img_Bar_Thu", "Img_Bar_Fri", "Img_Bar_Sat", "Img_Bar_Sun"
                })
                {
                    Image bar = UIBinder.FindImage(transform.root, name);
                    if (bar != null)
                    {
                        weeklyBars.Add(bar);
                    }
                }
            }

            Transform weeklyChart = UIBinder.FindDeepChild(transform.root, "Panel_WeeklyChart");
            if (weeklyChart != null)
            {
                totalTimeText ??= UIBinder.FindText(weeklyChart, "Txt_TotalTime");
            }

            Transform comment = UIBinder.FindDeepChild(transform.root, "Panel_Comment");
            commentText ??= comment == null ? null : UIBinder.FindText(comment, "Txt_Comment");

            if (achievementCards.Count == 0)
            {
                achievementCards.AddRange(FindObjectsByType<AchievementCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }
        }
    }
}
