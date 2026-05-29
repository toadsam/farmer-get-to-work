using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 도감 항목 카드입니다. 해금/잠금 상태만 바꿔도 아이콘, 잠금 오버레이, 별 표시가 갱신됩니다.
    /// </summary>
    public class CollectionItemCardUI : MonoBehaviour
    {
        [SerializeField] private Image cardSkinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectedOutline;

        private void Awake()
        {
            Bind();
        }

        private void OnValidate()
        {
            Bind();
        }

        public void SetData(string itemName, bool unlocked, int starCount = 0, Sprite iconSprite = null)
        {
            Bind();

            UIBinder.SetText(titleText, unlocked ? itemName : "???");
            UIBinder.SetText(descriptionText, unlocked ? "해금 완료" : "잠금 항목");
            UIBinder.SetText(valueText, unlocked ? new string('★', Mathf.Clamp(starCount, 1, 3)) : "???");

            if (iconImage != null)
            {
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                }

                iconImage.color = unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 1f);
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!unlocked);
            }

            if (selectedOutline != null)
            {
                selectedOutline.SetActive(false);
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

            if (selectedOutline == null)
            {
                Transform selectedTransform = UIBinder.FindDeepChild(transform, "Img_SelectedOutline");
                selectedOutline = selectedTransform == null ? null : selectedTransform.gameObject;
            }
        }
    }
}
