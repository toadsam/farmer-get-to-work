using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class CollectionSceneController : MonoBehaviour
    {
        [SerializeField] private List<CollectionItemCardUI> itemCards = new List<CollectionItemCardUI>();

        private void Awake()
        {
            BindCards();
            RefreshCollectionCards();
            HookCategoryButton("Btn_Tab_Building", "건물");
            HookCategoryButton("Btn_Tab_Animal", "동물");
            HookCategoryButton("Btn_Tab_Crop", "작물");
            HookCategoryButton("Btn_Tab_Decoration", "장식");
        }

        private void RefreshCollectionCards()
        {
            UnlockManager unlockManager = RuntimeGameDataAdapter.Unlocks;
            if (unlockManager != null && unlockManager.unlockEntries != null && unlockManager.unlockEntries.Count > 0)
            {
                for (int i = 0; i < itemCards.Count; i++)
                {
                    UnlockEntry entry = unlockManager.unlockEntries[i % unlockManager.unlockEntries.Count];
                    string displayName = entry == null || string.IsNullOrWhiteSpace(entry.displayName)
                        ? $"해금 항목 {i + 1}"
                        : entry.displayName;

                    bool unlocked = entry != null && entry.unlocked;
                    itemCards[i].SetData(displayName, unlocked, unlocked ? 3 : 0);
                }

                return;
            }

            string[] fallbackNames =
            {
                "농장 집", "헛간", "풍차", "우물",
                "젖소", "돼지", "닭", "양",
                "당근", "토마토", "밀", "양배추",
                "나무 울타리", "우편함", "꽃밭", "잠금 항목"
            };

            for (int i = 0; i < itemCards.Count; i++)
            {
                bool unlocked = i < 8;
                itemCards[i].SetData(fallbackNames[i % fallbackNames.Length], unlocked, unlocked ? 2 : 0);
            }
        }

        private void HookCategoryButton(string buttonName, string categoryName)
        {
            Button button = UIBinder.FindButton(transform.root, buttonName);
            if (button != null)
            {
                button.onClick.AddListener(() => Debug.Log($"도감 카테고리 선택: {categoryName}"));
            }
        }

        private void BindCards()
        {
            if (itemCards.Count > 0)
                return;

            itemCards.AddRange(
                FindObjectsByType<CollectionItemCardUI>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            );
        }
    }
}
