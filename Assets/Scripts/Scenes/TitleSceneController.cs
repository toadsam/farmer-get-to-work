using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// TitleScene의 시작/이어하기/유틸 버튼 연결입니다.
    /// </summary>
    public class TitleSceneController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button syncButton;

        private void Awake()
        {
            Bind();
            HookButtons();
        }

        private void Bind()
        {
            startButton ??= UIBinder.FindButton(transform.root, "Btn_Start");
            continueButton ??= UIBinder.FindButton(transform.root, "Btn_Continue");
            settingButton ??= UIBinder.FindButton(transform.root, "Btn_Setting");
            syncButton ??= UIBinder.FindButton(transform.root, "Btn_Sync");
        }

        private void HookButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.TutorialScene));
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.HomeScene));
            }

            if (settingButton != null)
            {
                settingButton.onClick.AddListener(() => Debug.Log("설정 버튼 클릭"));
            }

            if (syncButton != null)
            {
                syncButton.onClick.AddListener(() => Debug.Log("데이터 동기화 버튼 클릭"));
            }
        }
    }
}
