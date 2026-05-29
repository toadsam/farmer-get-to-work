using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// UI 프로토타입은 Hierarchy 이름이 중요하므로, 자식 이름으로 컴포넌트를 찾는 공통 헬퍼를 둡니다.
    /// 필드를 인스펙터에 직접 연결하지 않아도 같은 이름의 자식이 있으면 자동으로 연결됩니다.
    /// </summary>
    public static class UIBinder
    {
        public static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static T FindComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = FindDeepChild(root, childName);
            return child == null ? null : child.GetComponent<T>();
        }

        public static Button FindButton(Transform root, string childName)
        {
            return FindComponent<Button>(root, childName);
        }

        public static Image FindImage(Transform root, string childName)
        {
            return FindComponent<Image>(root, childName);
        }

        public static TextMeshProUGUI FindText(Transform root, string childName)
        {
            return FindComponent<TextMeshProUGUI>(root, childName);
        }

        public static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
