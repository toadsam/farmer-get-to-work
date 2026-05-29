using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// ShopScene의 카드 선택, 구매, 배치 버튼을 연결합니다.
    /// 지금은 UI 흐름만 만들고 실제 배치 데이터 저장은 Debug.Log로 남깁니다.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private List<ShopItemCardUI> itemCards = new List<ShopItemCardUI>();
        [SerializeField] private Image selectedObjectPreview;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button placeButton;
        [SerializeField] private Button cancelButton;

        private readonly string[] itemNames =
        {
            "헛간 확장", "울타리", "사과나무", "우물",
            "닭장", "램프", "꽃밭", "간이 창고"
        };

        private readonly int[] itemPrices =
        {
            2500, 300, 1200, 1000, 900, 250, 400, 750
        };

        private ShopItemCardUI selectedItem;

        private void Awake()
        {
            Bind();
            HookButtons();
            SetupItems();
        }

        public void SelectItem(ShopItemCardUI item)
        {
            selectedItem = item;

            foreach (ShopItemCardUI card in itemCards)
            {
                card.SetSelected(card == selectedItem);
            }

            if (selectedObjectPreview != null)
            {
                selectedObjectPreview.sprite = selectedItem == null ? selectedObjectPreview.sprite : selectedItem.ItemSprite;
                selectedObjectPreview.enabled = selectedItem != null;
            }
        }

        public void BuySelectedItem()
        {
            if (selectedItem == null)
            {
                Debug.Log("구매할 아이템이 선택되지 않았습니다.");
                return;
            }

            if (!GameData.TrySpendGold(selectedItem.Price))
            {
                Debug.Log($"골드 부족: {selectedItem.ItemName} 구매 실패");
                return;
            }

            Debug.Log($"{selectedItem.ItemName} 구매 완료. 남은 골드: {GameData.gold:N0}");
        }

        public void PlaceSelectedItem()
        {
            if (selectedItem == null)
            {
                Debug.Log("배치할 아이템이 선택되지 않았습니다.");
                return;
            }

            Debug.Log($"{selectedItem.ItemName} 배치 완료");
        }

        public void CancelSelection()
        {
            SelectItem(null);
            Debug.Log("상점 선택 취소");
        }

        private void SetupItems()
        {
            for (int i = 0; i < itemCards.Count; i++)
            {
                ShopItemCardUI card = itemCards[i];
                if (card == null)
                {
                    continue;
                }

                int dataIndex = i % itemNames.Length;
                card.SetData(itemNames[dataIndex], "농장을 꾸미는 요소", itemPrices[dataIndex]);
                card.Clicked -= SelectItem;
                card.Clicked += SelectItem;
                card.SetSelected(false);
            }

            SelectItem(null);
        }

        private void HookButtons()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuySelectedItem);
                buyButton.onClick.AddListener(BuySelectedItem);
            }

            if (placeButton != null)
            {
                placeButton.onClick.RemoveListener(PlaceSelectedItem);
                placeButton.onClick.AddListener(PlaceSelectedItem);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(CancelSelection);
                cancelButton.onClick.AddListener(CancelSelection);
            }
        }

        private void Bind()
        {
            if (itemCards.Count == 0)
            {
                itemCards.AddRange(FindObjectsByType<ShopItemCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }

            if (selectedObjectPreview == null)
            {
                selectedObjectPreview = UIBinder.FindImage(transform.root, "Img_SelectedObjectPreview");
            }

            if (buyButton == null)
            {
                buyButton = UIBinder.FindButton(transform.root, "Btn_Buy");
            }

            if (placeButton == null)
            {
                placeButton = UIBinder.FindButton(transform.root, "Btn_Place");
            }

            if (cancelButton == null)
            {
                cancelButton = UIBinder.FindButton(transform.root, "Btn_Cancel");
            }
        }
    }
}
