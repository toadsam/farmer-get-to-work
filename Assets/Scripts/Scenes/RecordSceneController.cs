using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 기록 화면의 정적 UI를 GameData 기반으로 갱신합니다.
    /// 주간 그래프는 각 Img_Bar_* RectTransform 높이로 표현합니다.
    /// </summary>
    public class RecordSceneController : MonoBehaviour
    {
        [SerializeField] private List<Image> weeklyBars = new List<Image>();
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI commentText;
        [SerializeField] private List<AchievementCardUI> achievementCards = new List<AchievementCardUI>();

        private readonly int[] weeklyMinutes = { 90, 130, 185, 165, 140, 110, 80 };

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

            UIBinder.SetText(totalTimeText, $"이번 주 {GameData.FormatMinutesKorean(GameData.weeklyFocusMinutes)}");
            UIBinder.SetText(commentText, "수요일이 가장 집중력이 좋았어요!\n좋은 페이스예요! 계속해봐요!");
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
                "10/10시간", $"{GameData.streakDays}/7일", "18/50시간", $"{GameData.streakDays}/30일", $"{GameData.gold:N0}/50,000"
            };

            for (int i = 0; i < achievementCards.Count && i < titles.Length; i++)
            {
                bool unlocked = i < 2;
                achievementCards[i].SetData(titles[i], descriptions[i], progress[i], unlocked);
            }
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
