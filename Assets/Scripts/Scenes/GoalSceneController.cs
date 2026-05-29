using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 목표와 집중 시간을 선택하고 GameData에 저장한 뒤 FocusScene으로 이동합니다.
    /// 카드와 버튼은 각각 Img_CardSkin, Img_ButtonSkin 슬롯을 갖고 있어 디자인 교체가 쉽습니다.
    /// </summary>
    public class GoalSceneController : MonoBehaviour
    {
        [SerializeField] private List<GoalCardUI> activityCards = new List<GoalCardUI>();
        [SerializeField] private List<Button> timeButtons = new List<Button>();
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button startSessionButton;
        [SerializeField] private TextMeshProUGUI customTimeText;
        [SerializeField] private TextMeshProUGUI expectedRewardText;
        [SerializeField] private TextMeshProUGUI expectedGrowthText;

        private readonly Dictionary<string, int> fixedTimeByButtonName = new Dictionary<string, int>
        {
            { "Btn_Time_10", 10 },
            { "Btn_Time_20", 20 },
            { "Btn_Time_30", 30 },
            { "Btn_Time_60", 60 }
        };

        private GoalCardUI selectedCard;
        private int selectedMinutes = 30;
        private int customMinutes = 30;

        private void Awake()
        {
            Bind();
            SetupCards();
            HookTimeButtons();
            HookControlButtons();
        }

        private void Start()
        {
            if (activityCards.Count > 0)
            {
                SelectCard(activityCards[0]);
            }

            SelectMinutes(30);
        }

        public void SelectCard(GoalCardUI card)
        {
            selectedCard = card;
            foreach (GoalCardUI activityCard in activityCards)
            {
                activityCard.SetSelected(activityCard == selectedCard);
            }

            RefreshExpectedReward();
        }

        public void SelectMinutes(int minutes)
        {
            selectedMinutes = Mathf.Clamp(minutes, 5, 180);
            customMinutes = selectedMinutes;
            RefreshTimeButtons();
            RefreshExpectedReward();
        }

        public void IncreaseCustomTime()
        {
            SelectMinutes(selectedMinutes + 5);
        }

        public void DecreaseCustomTime()
        {
            SelectMinutes(selectedMinutes - 5);
        }

        public void StartSession()
        {
            if (selectedCard == null && activityCards.Count > 0)
            {
                selectedCard = activityCards[0];
            }

            string goalName = selectedCard == null ? GameData.selectedGoalName : selectedCard.GoalName;
            int rewardGold = CalculateRewardGold();
            GameData.SetSelectedGoal(goalName, selectedMinutes, rewardGold);
            SceneLoader.LoadScene(SceneLoader.FocusScene);
        }

        private void SetupCards()
        {
            string[] names = { "공부하기", "독서하기", "운동하기", "수면 준비" };
            string[] descriptions = { "집중력 향상", "지식 성장", "체력 단련", "숙면 습관" };
            int[] rewards = { 120, 80, 60, 70 };

            for (int i = 0; i < activityCards.Count; i++)
            {
                GoalCardUI card = activityCards[i];
                int dataIndex = Mathf.Clamp(i, 0, names.Length - 1);
                card.SetData(names[dataIndex], descriptions[dataIndex], rewards[dataIndex]);
                card.Clicked -= SelectCard;
                card.Clicked += SelectCard;
                card.SetSelected(false);
            }
        }

        private void HookTimeButtons()
        {
            foreach (Button button in timeButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                string buttonName = button.name;
                button.onClick.AddListener(() =>
                {
                    if (fixedTimeByButtonName.TryGetValue(buttonName, out int fixedMinutes))
                    {
                        SelectMinutes(fixedMinutes);
                    }
                    else
                    {
                        SelectMinutes(customMinutes);
                    }
                });
            }
        }

        private void HookControlButtons()
        {
            if (plusButton != null)
            {
                plusButton.onClick.AddListener(IncreaseCustomTime);
            }

            if (minusButton != null)
            {
                minusButton.onClick.AddListener(DecreaseCustomTime);
            }

            if (startSessionButton != null)
            {
                startSessionButton.onClick.AddListener(StartSession);
            }
        }

        private void RefreshTimeButtons()
        {
            UIBinder.SetText(customTimeText, $"{selectedMinutes}분");

            foreach (Button button in timeButtons)
            {
                Image skin = UIBinder.FindImage(button.transform, "Img_ButtonSkin");
                bool selected = fixedTimeByButtonName.TryGetValue(button.name, out int fixedMinutes)
                    ? fixedMinutes == selectedMinutes
                    : !fixedTimeByButtonName.ContainsValue(selectedMinutes);

                if (skin != null)
                {
                    skin.color = selected ? new Color(0.35f, 0.72f, 0.38f, 1f) : new Color(0.94f, 0.96f, 0.90f, 1f);
                }
            }
        }

        private void RefreshExpectedReward()
        {
            int rewardGold = CalculateRewardGold();
            UIBinder.SetText(expectedRewardText, $"+{rewardGold} 골드");
            UIBinder.SetText(expectedGrowthText, $"{selectedMinutes}분 집중 성장");
        }

        private int CalculateRewardGold()
        {
            int baseReward = selectedCard == null ? 120 : selectedCard.BaseRewardGold;
            return Mathf.Max(1, Mathf.RoundToInt(baseReward * (selectedMinutes / 30f)));
        }

        private void Bind()
        {
            if (activityCards.Count == 0)
            {
                activityCards.AddRange(FindObjectsByType<GoalCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(card => card.name.StartsWith("ActivityCard_"))
                    .OrderBy(card => card.name));
            }

            if (timeButtons.Count == 0)
            {
                foreach (string buttonName in new[] { "Btn_Time_10", "Btn_Time_20", "Btn_Time_30", "Btn_Time_60", "Btn_Time_Custom" })
                {
                    Button button = UIBinder.FindButton(transform.root, buttonName);
                    if (button != null)
                    {
                        timeButtons.Add(button);
                    }
                }
            }

            plusButton ??= UIBinder.FindButton(transform.root, "Btn_Plus");
            minusButton ??= UIBinder.FindButton(transform.root, "Btn_Minus");
            startSessionButton ??= UIBinder.FindButton(transform.root, "Btn_StartSession");
            customTimeText ??= UIBinder.FindText(transform.root, "Txt_TimeValue");

            Transform expectedRewardPanel = UIBinder.FindDeepChild(transform.root, "Panel_ExpectedReward");
            Transform expectedGrowthPanel = UIBinder.FindDeepChild(transform.root, "Panel_ExpectedGrowth");
            expectedRewardText ??= expectedRewardPanel == null ? null : UIBinder.FindText(expectedRewardPanel, "Txt_Value");
            expectedGrowthText ??= expectedGrowthPanel == null ? null : UIBinder.FindText(expectedGrowthPanel, "Txt_Value");
        }
    }
}
