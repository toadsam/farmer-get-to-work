using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// Img_ 오브젝트에 붙이는 공통 이미지 슬롯입니다.
    /// 최종 디자인 PNG/Sprite가 들어오면 targetImage의 Source Image만 교체하면 됩니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ImageSlot : MonoBehaviour
    {
        [Tooltip("디자이너와 공유할 슬롯 이름입니다. 기본값은 GameObject 이름입니다.")]
        public string slotName;

        [Tooltip("실제로 Sprite가 들어가는 Image 컴포넌트입니다.")]
        public Image targetImage;

        [Tooltip("ClearSprite 호출 뒤 다시 보여줄 임시 Sprite입니다.")]
        public Sprite placeholderSprite;

        private void Reset()
        {
            Bind();
        }

        private void Awake()
        {
            Bind();
        }

        private void OnValidate()
        {
            Bind();
        }

        private void Bind()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (string.IsNullOrEmpty(slotName))
            {
                slotName = gameObject.name;
            }
        }

        public void SetSprite(Sprite newSprite)
        {
            Bind();
            targetImage.sprite = newSprite != null ? newSprite : placeholderSprite;
            targetImage.enabled = targetImage.sprite != null;
        }

        public void ClearSprite()
        {
            Bind();
            targetImage.sprite = placeholderSprite;
            targetImage.enabled = placeholderSprite != null;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
