using System;
using UnityEngine;
using UnityEngine.Events;

namespace FarmerGetToWork
{
    /// <summary>
    /// 앱 이탈 감지 구조입니다.
    /// 모바일에서 앱이 백그라운드로 가거나 포커스를 잃으면 FocusSessionManager가 실패 처리할 수 있게 이벤트를 발생시킵니다.
    /// </summary>
    public class AppPauseDetector : MonoBehaviour
    {
        [SerializeField] private bool failOnAppLeave = true;
        [SerializeField] private bool isFocusSessionActive = true;
        [SerializeField] private UnityEvent appLeftUnityEvent;

        public event Action AppLeft;

        private bool leaveAlreadyHandled;

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                HandleAppLeave("OnApplicationPause(true)");
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                HandleAppLeave("OnApplicationFocus(false)");
            }
        }

        public void SetFocusSessionActive(bool active)
        {
            isFocusSessionActive = active;
            if (active)
            {
                leaveAlreadyHandled = false;
            }
        }

        public void SimulateAppLeave()
        {
            HandleAppLeave("SimulateAppLeave()");
        }

        private void HandleAppLeave(string reason)
        {
            if (!failOnAppLeave || !isFocusSessionActive || leaveAlreadyHandled)
            {
                return;
            }

            leaveAlreadyHandled = true;
            Debug.Log($"앱 이탈 감지: {reason}");
            AppLeft?.Invoke();
            appLeftUnityEvent?.Invoke();
        }
    }
}
