using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerGetToWork
{
    /// <summary>
    /// FocusScene의 표시 UI를 KBW 메인 런타임의 FocusSessionService에 연결합니다.
    /// 타이머, 앱 이탈, 보상 처리, 기록 저장은 FocusSessionService와 RewardProcessor가 담당합니다.
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

        private FocusSessionService focusSessionService;
        private bool paused;
        private bool finished;

        private void Awake()
        {
            Bind();
            HookButtons();
        }

        private void OnDestroy()
        {
            UnbindFocusService();

            if (pauseDetector != null)
            {
                pauseDetector.AppLeft -= HandleAppLeave;
            }

            Time.timeScale = 1f;
        }

        private void Start()
        {
            BeginSessionFromRuntime();
        }

        public void BeginSessionFromRuntime()
        {
            paused = false;
            finished = false;
            Time.timeScale = 1f;

            focusSessionService = RuntimeGameDataAdapter.FocusSession;
            if (focusSessionService == null)
            {
                Debug.LogError("[FocusScene] FocusSessionService가 없어 집중 세션을 시작할 수 없습니다.", this);
                SceneLoader.LoadScene(SceneLoader.FailScene);
                return;
            }

            focusSessionService.autoMoveSceneOnFinish = false;
            BindFocusService();

            FocusSessionConfig selectedConfig = RuntimeGameDataAdapter.GetSelectedSessionOrFallback();
            RefreshSessionHeader(selectedConfig);

            if (!focusSessionService.IsRunning && !focusSessionService.StartSelectedSession())
            {
                Debug.LogWarning("[FocusScene] 선택 세션 시작 실패. 백업 세션으로 다시 시도합니다.", this);
                focusSessionService.StartSession(selectedConfig);
            }

            if (pauseDetector != null)
            {
                pauseDetector.SetFocusSessionActive(true);
            }

            RefreshTimerUI(focusSessionService.runtimeData);
        }

        public void TogglePause()
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            Debug.Log(paused ? "집중 타이머 일시정지" : "집중 타이머 재개");
        }

        public void GiveUp()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            Time.timeScale = 1f;

            if (focusSessionService != null && focusSessionService.IsRunning)
            {
                focusSessionService.CancelSession();
            }
            else
            {
                SceneLoader.LoadScene(SceneLoader.FailScene);
            }
        }

        public void CompleteSession()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            Time.timeScale = 1f;

            if (focusSessionService != null && focusSessionService.IsRunning)
            {
                focusSessionService.ForceCompleteSessionForTest();
            }
            else
            {
                SceneLoader.LoadScene(SceneLoader.SuccessScene);
            }
        }

        public void PlayNatureSound()
        {
            Debug.Log("자연의 소리 버튼 클릭");
        }

        private void HandleAppLeave()
        {
            GiveUp();
        }

        private void BindFocusService()
        {
            if (focusSessionService == null)
                return;

            focusSessionService.OnSessionStarted -= HandleSessionStarted;
            focusSessionService.OnSessionTick -= HandleSessionTick;
            focusSessionService.OnSessionFinished -= HandleSessionFinished;

            focusSessionService.OnSessionStarted += HandleSessionStarted;
            focusSessionService.OnSessionTick += HandleSessionTick;
            focusSessionService.OnSessionFinished += HandleSessionFinished;
        }

        private void UnbindFocusService()
        {
            if (focusSessionService == null)
                return;

            focusSessionService.OnSessionStarted -= HandleSessionStarted;
            focusSessionService.OnSessionTick -= HandleSessionTick;
            focusSessionService.OnSessionFinished -= HandleSessionFinished;
        }

        private void HandleSessionStarted(FocusSessionRuntimeData runtimeData)
        {
            RefreshSessionHeader(runtimeData == null ? null : runtimeData.config);
            RefreshTimerUI(runtimeData);
        }

        private void HandleSessionTick(FocusSessionRuntimeData runtimeData)
        {
            RefreshTimerUI(runtimeData);
        }

        private void HandleSessionFinished(FocusSessionResult result, RewardResultData rewardResult)
        {
            finished = true;
            paused = false;
            Time.timeScale = 1f;

            if (pauseDetector != null)
                pauseDetector.SetFocusSessionActive(false);

            bool success = rewardResult != null
                ? rewardResult.finalSuccess
                : result != null && result.success;
            SceneLoader.LoadScene(success ? SceneLoader.SuccessScene : SceneLoader.FailScene);
        }

        private void RefreshSessionHeader(FocusSessionConfig config)
        {
            if (config == null)
                config = RuntimeGameDataAdapter.GetSelectedSessionOrFallback();

            UIBinder.SetText(goalTitleText, $"{config.goalName} {config.plannedMinutes}분");
            UIBinder.SetText(endTimeText, $"{DateTime.Now.AddSeconds(config.GetSessionDurationSeconds()):HH:mm} 종료 예정");
        }

        private void RefreshTimerUI(FocusSessionRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                UIBinder.SetText(timerText, "00:00");
                if (progressRingFill != null)
                    progressRingFill.fillAmount = 0f;

                return;
            }

            UIBinder.SetText(timerText, GameDataUtility.ToMinuteSecondText(runtimeData.remainingSeconds));

            if (progressRingFill != null)
            {
                progressRingFill.fillAmount = runtimeData.durationSeconds <= 0f
                    ? 0f
                    : Mathf.Clamp01(runtimeData.remainingSeconds / runtimeData.durationSeconds);
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
