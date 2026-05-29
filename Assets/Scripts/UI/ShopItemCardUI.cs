using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 상점 아이템 카드입니다. 선택 테두리와 잠금 오버레이를 별도 Image 슬롯으로 분리합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShopItemCardUI : MonoBehaviour
    {
        [SerializeField] private Image cardSkinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject selectedOutline;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Button button;

        public string ItemName { get; private set; }
        public int Price { get; private set; }
        public Sprite ItemSprite => iconImage == null ? null : iconImage.sprite;
        public event Action<ShopItemCardUI> Clicked;

        private void Awake()
        {
            Bind();
            button.onClick.RemoveListener(NotifyClicked);
            button.onClick.AddListener(NotifyClicked);
        }

        private void OnValidate()
        {
            Bind();
        }

        public void SetData(string itemName, string description, int price, Sprite iconSprite = null, bool locked = false)
        {
            Bind();
            ItemName = itemName;
            Price = price;

            UIBinder.SetText(titleText, itemName);
            UIBinder.SetText(descriptionText, description);
            UIBinder.SetText(priceText, price.ToString("N0"));

            if (iconImage != null && iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(locked);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedOutline != null)
            {
                selectedOutline.SetActive(selected);
            }
        }

        private void NotifyClicked()
        {
            Clicked?.Invoke(this);
        }

        private void Bind()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

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

            if (priceText == null)
            {
                priceText = UIBinder.FindText(transform, "Txt_Value");
            }

            if (selectedOutline == null)
            {
                Transform selectedTransform = UIBinder.FindDeepChild(transform, "Img_SelectedOutline");
                selectedOutline = selectedTransform == null ? null : selectedTransform.gameObject;
            }

            if (lockOverlay == null)
            {
                Transform lockTransform = UIBinder.FindDeepChild(transform, "Img_LockOverlay");
                lockOverlay = lockTransform == null ? null : lockTransform.gameObject;
            }
        }
    }
}
