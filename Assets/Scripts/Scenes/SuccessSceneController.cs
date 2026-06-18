using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class SuccessSceneController : MonoBehaviour
    {
        [SerializeField] private Button backToFarmButton;
        [SerializeField] private TextMeshProUGUI goldRewardText;
        [SerializeField] private TextMeshProUGUI goldRewardLabelText;
        [SerializeField] private TextMeshProUGUI unlockItemNameText;
        [SerializeField] private TextMeshProUGUI unlockDescriptionText;
        [SerializeField] private TextMeshProUGUI randomItemNameText;
        [SerializeField] private TextMeshProUGUI randomDescriptionText;

        private void Awake()
        {
            backToFarmButton ??= UIBinder.FindButton(transform.root, "Btn_BackToFarm");
            if (backToFarmButton != null)
            {
                backToFarmButton.onClick.AddListener(() => RuntimeGameDataAdapter.LoadScene("KBW_TempDTxAfter"));
            }

            Transform goldPanel = UIBinder.FindDeepChild(transform.root, "Panel_GoldReward");
            goldRewardText ??= goldPanel == null ? null : UIBinder.FindText(goldPanel, "Txt_Value");
            goldRewardLabelText ??= goldPanel == null ? null : UIBinder.FindText(goldPanel, "Txt_Label");

            Transform unlockPanel = UIBinder.FindDeepChild(transform.root, "Panel_UnlockReward");
            unlockItemNameText ??= unlockPanel == null ? null : UIBinder.FindText(unlockPanel, "Txt_ItemName");
            unlockDescriptionText ??= unlockPanel == null ? null : UIBinder.FindText(unlockPanel, "Txt_Description");

            Transform randomPanel = UIBinder.FindDeepChild(transform.root, "Panel_RandomReward");
            randomItemNameText ??= randomPanel == null ? null : UIBinder.FindText(randomPanel, "Txt_ItemName");
            randomDescriptionText ??= randomPanel == null ? null : UIBinder.FindText(randomPanel, "Txt_Description");
        }

        private void Start()
        {
            RefreshResultTexts();
        }

        private void RefreshResultTexts()
        {
            FocusSessionResult result = RuntimeGameDataAdapter.GetLastSessionResult();
            RewardResultData reward = RuntimeGameDataAdapter.GetLastRewardResult();

            if (result == null || reward == null)
            {
                GameData.ApplyPendingSuccessReward();
                UIBinder.SetText(goldRewardLabelText, "획득 골드");
                UIBinder.SetText(goldRewardText, $"+{GameData.expectedRewardGold} 골드");
                UIBinder.SetText(unlockItemNameText, "닭장");
                UIBinder.SetText(unlockDescriptionText, "새로운 농장 요소가 준비되었어요.");
                UIBinder.SetText(randomItemNameText, "물뿌리개(중)");
                UIBinder.SetText(randomDescriptionText, "다음 성장에 도움이 되는 보상입니다.");
                return;
            }

            UIBinder.SetText(goldRewardLabelText, "획득 보상");

            if (reward.rewardGold > 0)
                UIBinder.SetText(goldRewardText, $"+{reward.rewardGold} 골드");
            else
                UIBinder.SetText(goldRewardText, $"성장 +{reward.rewardGrowth}");

            if (reward.unlockedSomething)
            {
                UIBinder.SetText(unlockItemNameText, reward.unlockedDisplayName);
                UIBinder.SetText(unlockDescriptionText, "새로운 농장 요소가 해금되었어요.");
            }
            else
            {
                UIBinder.SetText(unlockItemNameText, $"해금 진행도 +{reward.rewardUnlockProgress}");
                UIBinder.SetText(unlockDescriptionText, "다음 농장 요소 해금에 가까워졌어요.");
            }

            if (reward.rewardStamina > 0)
            {
                UIBinder.SetText(randomItemNameText, $"스태미너 +{reward.rewardStamina}");
                UIBinder.SetText(randomDescriptionText, "농장에서 수확하거나 상호작용할 힘을 회복했어요.");
            }
            else if (reward.hasExitPenalty)
            {
                UIBinder.SetText(randomItemNameText, "부분 보상");
                UIBinder.SetText(randomDescriptionText, $"앱 이탈 시간이 있어 보상이 {reward.rewardMultiplier:P0}로 조정되었어요.");
            }
            else
            {
                UIBinder.SetText(randomItemNameText, $"{result.focusedMinutes}분 집중");
                UIBinder.SetText(randomDescriptionText, "이번 세션 기록이 저장되었어요.");
            }
        }
    }
}
