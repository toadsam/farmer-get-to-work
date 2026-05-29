using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 하단 네비게이션 프리팹 루트에 붙는 스크립트입니다.
    /// 현재 씬에 해당하는 버튼을 하이라이트하고, 각 버튼의 씬 이동을 담당합니다.
    /// </summary>
    public class BottomNavigationBar : MonoBehaviour
    {
        [SerializeField] private Image barSkinImage;
        [SerializeField] private BottomNavButton homeButton;
        [SerializeField] private BottomNavButton goalButton;
        [SerializeField] private BottomNavButton shopButton;
        [SerializeField] private BottomNavButton collectionButton;
        [SerializeField] private BottomNavButton recordButton;

        private void Awake()
        {
            Bind();
            ConfigureButtons();
        }

        private void Start()
        {
            HighlightCurrentScene();
        }

        private void OnValidate()
        {
            Bind();
        }

        public void NavigateHome()
        {
            SceneLoader.LoadScene(SceneLoader.HomeScene);
        }

        public void NavigateGoal()
        {
            SceneLoader.LoadScene(SceneLoader.GoalScene);
        }

        public void NavigateShop()
        {
            SceneLoader.LoadScene(SceneLoader.ShopScene);
        }

        public void NavigateCollection()
        {
            SceneLoader.LoadScene(SceneLoader.CollectionScene);
        }

        public void NavigateRecord()
        {
            SceneLoader.LoadScene(SceneLoader.RecordScene);
        }

        private void ConfigureButtons()
        {
            homeButton?.Configure(SceneLoader.HomeScene, "홈");
            goalButton?.Configure(SceneLoader.GoalScene, "목표");
            shopButton?.Configure(SceneLoader.ShopScene, "상점");
            collectionButton?.Configure(SceneLoader.CollectionScene, "도감");
            recordButton?.Configure(SceneLoader.RecordScene, "기록");
        }

        private void HighlightCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            homeButton?.SetHighlighted(sceneName == SceneLoader.HomeScene);
            goalButton?.SetHighlighted(sceneName == SceneLoader.GoalScene);
            shopButton?.SetHighlighted(sceneName == SceneLoader.ShopScene);
            collectionButton?.SetHighlighted(sceneName == SceneLoader.CollectionScene);
            recordButton?.SetHighlighted(sceneName == SceneLoader.RecordScene);
        }

        private void Bind()
        {
            if (barSkinImage == null)
            {
                barSkinImage = UIBinder.FindImage(transform, "Img_Background");
            }

            if (homeButton == null)
            {
                homeButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Home");
            }

            if (goalButton == null)
            {
                goalButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Goal");
            }

            if (shopButton == null)
            {
                shopButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Shop");
            }

            if (collectionButton == null)
            {
                collectionButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Collection");
            }

            if (recordButton == null)
            {
                recordButton = UIBinder.FindComponent<BottomNavButton>(transform, "Btn_Record");
            }
        }
    }
}
