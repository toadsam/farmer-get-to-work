using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class TutorialSceneController : MonoBehaviour
    {
        [SerializeField] private Button nextButton;

        private void Awake()
        {
            nextButton ??= UIBinder.FindButton(transform.root, "Btn_Next");
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.HomeScene));
            }
        }
    }
}
