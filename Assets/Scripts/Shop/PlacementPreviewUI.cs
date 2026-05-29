using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 상점에서 선택된 오브젝트를 임시 미리보기 슬롯에 표시합니다.
    /// 실제 3D/2D 배치 시스템은 나중에 이 클래스의 SetPreview 결과를 사용하면 됩니다.
    /// </summary>
    public class PlacementPreviewUI : MonoBehaviour
    {
        [SerializeField] private Image previewImage;
        [SerializeField] private TextMeshProUGUI guideText;

        private void Awake()
        {
            Bind();
        }

        public void SetPreview(Sprite sprite, string itemName)
        {
            Bind();
            if (previewImage != null)
            {
                previewImage.sprite = sprite;
                previewImage.enabled = true;
            }

            UIBinder.SetText(guideText, string.IsNullOrEmpty(itemName) ? "원하는 위치에 배치해 보세요!" : $"{itemName} 배치 미리보기");
        }

        public void ClearPreview()
        {
            Bind();
            if (previewImage != null)
            {
                previewImage.enabled = false;
            }

            UIBinder.SetText(guideText, "원하는 위치에 배치해 보세요!");
        }

        private void Bind()
        {
            if (previewImage == null)
            {
                previewImage = UIBinder.FindImage(transform.root, "Img_SelectedObjectPreview");
            }

            if (guideText == null)
            {
                guideText = UIBinder.FindText(transform.root, "Txt_Message");
            }
        }
    }
}
