using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// FocusScene의 카운트다운과 성공/실패 이동을 담당합니다.
    /// 실제 DTx 검증 로직은 나중에 이 클래스 주변에 붙이고, 지금은 UI 프로토타입 흐름을 완성합니다.
    /// </summary>
    public class FocusSessionManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI goalTitleText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI endTimeText;
        [SerializeField] private Image progressRingFill;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private Button natureSoundButton;
        [SerializeField] private AppPauseDetector pauseDetector;

        private float totalSeconds;
        private float remainingSeconds;
        private bool paused;
        private bool finished;

        private void Awake()
        {
            Bind();
            HookButtons();
        }

        private void OnDestroy()
        {
            if (pauseDetector != null)
            {
                pauseDetector.AppLeft -= HandleAppLeave;
            }
        }

        private void Start()
        {
            BeginSessionFromGameData();
        }

        private void Update()
        {
            if (finished || paused)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                RefreshTimerUI();
                CompleteSession();
                return;
            }

            RefreshTimerUI();
        }

        public void BeginSessionFromGameData()
        {
            totalSeconds = Mathf.Max(1, GameData.selectedGoalMinutes * 60);
            remainingSeconds = totalSeconds;
            paused = false;
            finished = false;

            UIBinder.SetText(goalTitleText, $"{GameData.selectedGoalName} {GameData.selectedGoalMinutes}분");
            UIBinder.SetText(endTimeText, $"{DateTime.Now.AddMinutes(GameData.selectedGoalMinutes):HH:mm} 종료 예정");

            if (pauseDetector != null)
            {
                pauseDetector.SetFocusSessionActive(true);
            }

            RefreshTimerUI();
        }

        public void TogglePause()
        {
            paused = !paused;
            Debug.Log(paused ? "집중 타이머 일시정지" : "집중 타이머 재개");
        }

        public void GiveUp()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            GameData.MarkFocusSessionFailed();
            SceneLoader.LoadScene(SceneLoader.FailScene);
        }

        public void CompleteSession()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            if (pauseDetector != null)
            {
                pauseDetector.SetFocusSessionActive(false);
            }

            GameData.MarkFocusSessionSucceeded();
            SceneLoader.LoadScene(SceneLoader.SuccessScene);
        }

        public void PlayNatureSound()
        {
            Debug.Log("자연의 소리 버튼 클릭");
        }

        private void HandleAppLeave()
        {
            GiveUp();
        }

        private void RefreshTimerUI()
        {
            int total = Mathf.CeilToInt(remainingSeconds);
            int minutes = total / 60;
            int seconds = total % 60;

            UIBinder.SetText(timerText, $"{minutes:00}:{seconds:00}");

            if (progressRingFill != null)
            {
                progressRingFill.fillAmount = totalSeconds <= 0f ? 0f : Mathf.Clamp01(remainingSeconds / totalSeconds);
            }
        }

        private void HookButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (giveUpButton != null)
            {
                giveUpButton.onClick.RemoveListener(GiveUp);
                giveUpButton.onClick.AddListener(GiveUp);
            }

            if (natureSoundButton != null)
            {
                natureSoundButton.onClick.RemoveListener(PlayNatureSound);
                natureSoundButton.onClick.AddListener(PlayNatureSound);
            }

            if (pauseDetector != null)
            {
                pauseDetector.AppLeft -= HandleAppLeave;
                pauseDetector.AppLeft += HandleAppLeave;
            }
        }

        private void Bind()
        {
            if (goalTitleText == null)
            {
                goalTitleText = UIBinder.FindText(transform.root, "Txt_GoalTitle");
            }

            if (timerText == null)
            {
                timerText = UIBinder.FindText(transform.root, "Txt_Timer");
            }

            if (endTimeText == null)
            {
                endTimeText = UIBinder.FindText(transform.root, "Txt_EndTime");
            }

            if (progressRingFill == null)
            {
                progressRingFill = UIBinder.FindImage(transform.root, "Img_ProgressRingFill");
            }

            if (pauseButton == null)
            {
                pauseButton = UIBinder.FindButton(transform.root, "Btn_Pause");
            }

            if (giveUpButton == null)
            {
                giveUpButton = UIBinder.FindButton(transform.root, "Btn_GiveUp");
            }

            if (natureSoundButton == null)
            {
                natureSoundButton = UIBinder.FindButton(transform.root, "Btn_NatureSound");
            }

            if (pauseDetector == null)
            {
                pauseDetector = GetComponent<AppPauseDetector>();
            }
        }
    }
}
