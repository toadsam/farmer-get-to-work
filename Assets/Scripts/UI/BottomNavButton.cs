using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 하단 네비게이션의 개별 버튼입니다.
    /// 선택/비선택 상태는 Img_ButtonSkin 색상과 Sprite 슬롯으로 표시합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BottomNavButton : MonoBehaviour
    {
        [SerializeField] private string targetSceneName;
        [SerializeField] private Image buttonSkinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Color selectedColor = new Color(0.35f, 0.72f, 0.38f, 1f);
        [SerializeField] private Color normalColor = new Color(0.92f, 0.94f, 0.88f, 1f);

        private Button button;

        private void Awake()
        {
            Bind();
            button.onClick.RemoveListener(LoadTargetScene);
            button.onClick.AddListener(LoadTargetScene);
        }

        private void OnValidate()
        {
            Bind();
        }

        public void Configure(string sceneName, string label)
        {
            targetSceneName = sceneName;
            Bind();
            UIBinder.SetText(labelText, label);
        }

        public void SetHighlighted(bool highlighted)
        {
            Bind();

            if (buttonSkinImage != null)
            {
                buttonSkinImage.sprite = highlighted && selectedSprite != null ? selectedSprite : normalSprite;
                buttonSkinImage.color = highlighted ? selectedColor : normalColor;
            }

            if (labelText != null)
            {
                labelText.color = highlighted ? Color.white : new Color(0.22f, 0.28f, 0.22f, 1f);
            }
        }

        private void LoadTargetScene()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"{name} has no target scene.");
                return;
            }

            if (SceneManager.GetActiveScene().name == targetSceneName)
            {
                return;
            }

            SceneLoader.LoadScene(targetSceneName);
        }

        private void Bind()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (buttonSkinImage == null)
            {
                buttonSkinImage = UIBinder.FindImage(transform, "Img_ButtonSkin");
            }

            if (iconImage == null)
            {
                iconImage = UIBinder.FindImage(transform, "Img_Icon");
            }

            if (labelText == null)
            {
                labelText = UIBinder.FindText(transform, "Txt_Label");
            }
        }
    }
}
