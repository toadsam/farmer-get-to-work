using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class FailSceneController : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            GameData.MarkFocusSessionFailed();
            restartButton ??= UIBinder.FindButton(transform.root, "Btn_Restart");
            homeButton ??= UIBinder.FindButton(transform.root, "Btn_Home");

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.GoalScene));
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.HomeScene));
            }
        }
    }
}
