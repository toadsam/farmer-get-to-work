using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 기록 화면의 업적 카드입니다. 달성률 텍스트와 잠금 오버레이를 나중에 데이터에 연결할 수 있게 분리합니다.
    /// </summary>
    public class AchievementCardUI : MonoBehaviour
    {
        [SerializeField] private Image cardSkinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private GameObject lockOverlay;

        private void Awake()
        {
            Bind();
        }

        private void OnValidate()
        {
            Bind();
        }

        public void SetData(string title, string description, string progressText, bool unlocked, Sprite iconSprite = null)
        {
            Bind();
            UIBinder.SetText(titleText, title);
            UIBinder.SetText(descriptionText, description);
            UIBinder.SetText(valueText, progressText);

            if (iconImage != null && iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!unlocked);
            }
        }

        private void Bind()
        {
            if (cardSkinImage == null)
            {
                cardSkinImage = UIBinder.FindImage(transform, "Img_CardSkin");
            }

            if (iconImage == null)
            {
                iconImage = UIBinder.FindImage(transform, "Img_Icon");
            }

            if (titleText == null)
            {
                titleText = UIBinder.FindText(transform, "Txt_Title");
            }

            if (descriptionText == null)
            {
                descriptionText = UIBinder.FindText(transform, "Txt_Description");
            }

            if (valueText == null)
            {
                valueText = UIBinder.FindText(transform, "Txt_Value");
            }

            if (lockOverlay == null)
            {
                Transform lockTransform = UIBinder.FindDeepChild(transform, "Img_LockOverlay");
                lockOverlay = lockTransform == null ? null : lockTransform.gameObject;
            }
        }
    }
}
