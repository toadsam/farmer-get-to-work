using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public enum StatPanelKind
    {
        Gold,
        TodayFocus,
        Streak,
        WeeklyFocus,
        TotalGold,
        FarmLevel,
        UnlockedItems,
        BestSession,
        Custom
    }

    /// <summary>
    /// TopStatusBar와 RecordScene의 작은 통계 패널에 붙이는 표시 컴포넌트입니다.
    /// 패널의 배경은 자식 Img_PanelSkin으로 분리되어 있으므로 나중에 Sprite만 교체하면 됩니다.
    /// </summary>
    public class StatPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelKind statKind = StatPanelKind.Custom;
        [SerializeField] private Image panelSkinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;

        private void Awake()
        {
            Bind();
        }

        private void OnValidate()
        {
            Bind();
        }

        private void Start()
        {
            RefreshFromGameData();
        }

        public void SetKind(StatPanelKind kind)
        {
            statKind = kind;
            RefreshFromGameData();
        }

        public void SetText(string label, string value)
        {
            Bind();
            UIBinder.SetText(labelText, label);
            UIBinder.SetText(valueText, value);
        }

        public void SetIcon(Sprite icon)
        {
            Bind();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
            }
        }

        public void RefreshFromGameData()
        {
            Bind();

            switch (statKind)
            {
                case StatPanelKind.Gold:
                    SetText("골드", GameData.gold.ToString("N0"));
                    break;
                case StatPanelKind.TodayFocus:
                    SetText("오늘 집중 시간", GameData.FormatMinutesKorean(GameData.totalFocusMinutesToday));
                    break;
                case StatPanelKind.Streak:
                    SetText("연속 성공", $"{GameData.streakDays}일");
                    break;
                case StatPanelKind.WeeklyFocus:
                    SetText("이번 주 누적 시간", GameData.FormatMinutesKorean(GameData.weeklyFocusMinutes));
                    break;
                case StatPanelKind.TotalGold:
                    SetText("총 획득 골드", GameData.gold.ToString("N0"));
                    break;
                case StatPanelKind.FarmLevel:
                    SetText("농장 레벨", $"Lv.{GameData.farmLevel}");
                    break;
                case StatPanelKind.UnlockedItems:
                    SetText("해제한 아이템", $"{GameData.unlockedItemCount}/{GameData.totalItemCount}");
                    break;
                case StatPanelKind.BestSession:
                    SetText("최고 집중 세션", "3시간 05분");
                    break;
            }
        }

        private void Bind()
        {
            if (panelSkinImage == null)
            {
                panelSkinImage = UIBinder.FindImage(transform, "Img_PanelSkin");
            }

            if (iconImage == null)
            {
                iconImage = UIBinder.FindImage(transform, "Img_Icon");
            }

            if (iconImage == null)
            {
                foreach (Image image in GetComponentsInChildren<Image>(true))
                {
                    bool isReplaceableIcon = image.name.StartsWith("Img_") && !image.name.Contains("PanelSkin") && !image.name.Contains("CardSkin") && !image.name.Contains("ButtonSkin");
                    if (isReplaceableIcon)
                    {
                        iconImage = image;
                        break;
                    }
                }
            }

            if (labelText == null)
            {
                labelText = UIBinder.FindText(transform, "Txt_Label");
            }

            if (valueText == null)
            {
                valueText = UIBinder.FindText(transform, "Txt_Value");
            }
        }
    }
}
