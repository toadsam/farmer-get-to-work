using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// 타이머 텍스트와 원형 진행 이미지를 갱신하는 작은 표시 전용 컴포넌트입니다.
    /// ProgressRingFill은 Image Type=Filled, Fill Method=Radial360으로 설정해 사용합니다.
    /// </summary>
    public class FocusTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image progressRingFill;

        private void Awake()
        {
            Bind();
        }

        private void OnValidate()
        {
            Bind();
        }

        public void SetTimer(float secondsRemaining)
        {
            Bind();
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            UIBinder.SetText(timerText, $"{minutes:00}:{seconds:00}");
        }

        public void SetProgress(float normalizedProgress)
        {
            Bind();
            if (progressRingFill != null)
            {
                progressRingFill.fillAmount = Mathf.Clamp01(normalizedProgress);
            }
        }

        private void Bind()
        {
            if (timerText == null)
            {
                timerText = UIBinder.FindText(transform, "Txt_Timer");
            }

            if (progressRingFill == null)
            {
                progressRingFill = UIBinder.FindImage(transform, "Img_ProgressRingFill");
            }
        }
    }
}
