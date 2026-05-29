using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    public class CollectionSceneController : MonoBehaviour
    {
        private void Awake()
        {
            HookCategoryButton("Btn_Tab_Building", "건물");
            HookCategoryButton("Btn_Tab_Animal", "동물");
            HookCategoryButton("Btn_Tab_Crop", "작물");
            HookCategoryButton("Btn_Tab_Decoration", "장식");
        }

        private void HookCategoryButton(string buttonName, string categoryName)
        {
            Button button = UIBinder.FindButton(transform.root, buttonName);
            if (button != null)
            {
                button.onClick.AddListener(() => Debug.Log($"도감 카테고리 선택: {categoryName}"));
            }
        }
    }
}
