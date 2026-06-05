using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmerGetToWork
{
    public class FailSceneController : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private TextMeshProUGUI reasonText;
        [SerializeField] private TextMeshProUGUI resultDescriptionText;

        private void Awake()
        {
            restartButton ??= UIBinder.FindButton(transform.root, "Btn_Restart");
            homeButton ??= UIBinder.FindButton(transform.root, "Btn_Home");

            Transform reasonPanel = UIBinder.FindDeepChild(transform.root, "Panel_Reason");
            reasonText ??= reasonPanel == null ? null : UIBinder.FindText(reasonPanel, "Txt_Reason");

            Transform resultPanel = UIBinder.FindDeepChild(transform.root, "Panel_Result");
            resultDescriptionText ??= resultPanel == null ? null : UIBinder.FindText(resultPanel, "Txt_Description");

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.GoalScene));
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(() => RuntimeGameDataAdapter.GoMainFarm());
            }
        }

        private void Start()
        {
            RefreshFailureReason();
        }

        private void RefreshFailureReason()
        {
            RewardResultData reward = RuntimeGameDataAdapter.GetLastRewardResult();
            FocusSessionResult result = RuntimeGameDataAdapter.GetLastSessionResult();

            if (reward == null)
            {
                GameData.MarkFocusSessionFailed();
                UIBinder.SetText(reasonText, "앱을 벗어났거나 다른 앱을 사용해서 세션이 중단되었어요.");
                UIBinder.SetText(resultDescriptionText, "집중을 끝까지 유지하면 보상을 받을 수 있어요!");
                return;
            }

            string reason = string.IsNullOrWhiteSpace(reward.failReasonMessage)
                ? "세션이 중단되어 농장 성장이 적용되지 않았어요."
                : reward.failReasonMessage;

            UIBinder.SetText(reasonText, reason);

            if (result != null && result.focusedMinutes > 0)
                UIBinder.SetText(resultDescriptionText, $"{result.focusedMinutes}분까지 집중했지만 최종 보상은 지급되지 않았어요.");
            else
                UIBinder.SetText(resultDescriptionText, "집중을 끝까지 유지하면 보상을 받을 수 있어요!");
        }
    }
}
