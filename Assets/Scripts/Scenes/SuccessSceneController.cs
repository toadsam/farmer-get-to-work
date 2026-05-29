using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class SuccessSceneController : MonoBehaviour
    {
        [SerializeField] private Button backToFarmButton;
        [SerializeField] private TextMeshProUGUI goldRewardText;

        private void Awake()
        {
            backToFarmButton ??= UIBinder.FindButton(transform.root, "Btn_BackToFarm");
            if (backToFarmButton != null)
            {
                backToFarmButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.HomeScene));
            }

            Transform goldPanel = UIBinder.FindDeepChild(transform.root, "Panel_GoldReward");
            goldRewardText ??= goldPanel == null ? null : UIBinder.FindText(goldPanel, "Txt_Value");
        }

        private void Start()
        {
            GameData.ApplyPendingSuccessReward();
            UIBinder.SetText(goldRewardText, $"+{GameData.expectedRewardGold} 골드");
        }
    }
}
