using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 목표 카드와 추천 목표 카드에 공통으로 사용하는 UI 컴포넌트입니다.
    /// 카드 배경, 아이콘, 선택 테두리는 모두 별도 Image 슬롯으로 유지합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GoalCardUI : MonoBehaviour
    {
        public Image cardSkinImage;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI rewardText;
        public GameObject selectedOutline;
        public GameObject checkIcon;
        public Button button;

        public string GoalName { get; private set; }
        public string GoalDescription { get; private set; }
        public int BaseRewardGold { get; private set; }
        public event Action<GoalCardUI> Clicked;

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

        public void SetData(string title, string description, int rewardGold, Sprite iconSprite = null)
        {
            Bind();
            GoalName = title;
            GoalDescription = description;
            BaseRewardGold = rewardGold;

            UIBinder.SetText(titleText, title);
            UIBinder.SetText(descriptionText, description);
            UIBinder.SetText(rewardText, $"+{rewardGold}");

            if (iconImage != null && iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedOutline != null)
            {
                selectedOutline.SetActive(isSelected);
            }

            if (checkIcon != null)
            {
                checkIcon.SetActive(isSelected);
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

            if (rewardText == null)
            {
                rewardText = UIBinder.FindText(transform, "Txt_Value");
            }

            if (selectedOutline == null)
            {
                Transform selectedTransform = UIBinder.FindDeepChild(transform, "Img_SelectedOutline");
                selectedOutline = selectedTransform == null ? null : selectedTransform.gameObject;
            }

            if (checkIcon == null)
            {
                Transform checkTransform = UIBinder.FindDeepChild(transform, "Img_CheckIcon");
                checkIcon = checkTransform == null ? null : checkTransform.gameObject;
            }
        }
    }
}
