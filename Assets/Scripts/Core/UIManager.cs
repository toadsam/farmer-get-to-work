using UnityEngine;

namespace FarmerGetToWork
{
    /// <summary>
    /// 현재 씬 안의 공통 UI를 한 번에 갱신하는 얇은 관리자입니다.
    /// 프로토타입 단계에서는 TopStatusBar 같은 정적 표시값을 새로고침하는 용도로 사용합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public void RefreshStatusPanels()
        {
            StatPanelUI[] panels = FindObjectsByType<StatPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (StatPanelUI panel in panels)
            {
                panel.RefreshFromGameData();
            }
        }
    }
}
