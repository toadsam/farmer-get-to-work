using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class HomeSceneController : MonoBehaviour
    {
        [SerializeField] private Button focusStartButton;
        [SerializeField] private Button changeGoalButton;

        private void Awake()
        {
            focusStartButton ??= UIBinder.FindButton(transform.root, "Btn_FocusStart");
            changeGoalButton ??= UIBinder.FindButton(transform.root, "Btn_ChangeGoal");

            if (focusStartButton != null)
            {
                focusStartButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.GoalScene));
            }

            if (changeGoalButton != null)
            {
                changeGoalButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.GoalScene));
            }
        }
    }
}
