using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FarmerGetToWork
{
    /// <summary>
    /// Button OnClick에 바로 연결할 수 있는 씬 전환 전용 컴포넌트입니다.
    /// 각 메서드는 Build Settings에 등록된 씬 이름을 기준으로 이동합니다.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public const string TitleScene = "00_TitleScene";
        public const string TutorialScene = "01_TutorialScene";
        public const string HomeScene = "02_HomeScene";
        public const string GoalScene = "03_GoalScene";
        public const string FocusScene = "04_FocusScene";
        public const string SuccessScene = "05_SuccessScene";
        public const string FailScene = "06_FailScene";
        public const string ShopScene = "07_ShopScene";
        public const string CollectionScene = "08_CollectionScene";
        public const string RecordScene = "09_RecordScene";

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("Scene name is empty.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public void LoadTitleScene()
        {
            LoadScene(TitleScene);
        }

        public void LoadTutorialScene()
        {
            LoadScene(TutorialScene);
        }

        public void LoadHomeScene()
        {
            LoadScene(HomeScene);
        }

        public void LoadGoalScene()
        {
            LoadScene(GoalScene);
        }

        public void LoadFocusScene()
        {
            LoadScene(FocusScene);
        }

        public void LoadSuccessScene()
        {
            LoadScene(SuccessScene);
        }

        public void LoadFailScene()
        {
            LoadScene(FailScene);
        }

        public void LoadShopScene()
        {
            LoadScene(ShopScene);
        }

        public void LoadCollectionScene()
        {
            LoadScene(CollectionScene);
        }

        public void LoadRecordScene()
        {
            LoadScene(RecordScene);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
