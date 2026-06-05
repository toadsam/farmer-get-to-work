using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusSessionTimer : MonoBehaviour
{
    [Header("References")]
    public FocusSessionService focusSessionService;
    public GameStateManager gameStateManager;
    public AppFocusTracker appFocusTracker;

    [Header("UI")]
    public TMP_Text goalText;
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text exitText;

    public Button[] startButtons;
    public Button cancelButton;

    [Header("Test Mode")]
    [Tooltip("KBW 씬의 테스트 버튼은 실제 분 단위 대신 짧은 테스트 시간으로 FocusSessionService를 실행합니다.")]
    public bool useShortTestDuration = true;

    [Tooltip("테스트 편의를 위해 빈 밭이 있으면 세션 시작 전에 기본 작물을 자동으로 심습니다.")]
    public bool autoPlantEmptyPlotsForTest = true;

    private void Awake()
    {
        RefreshReferences();
        BindServiceEvents();
        SetSessionRunningUI(false);
        UpdateIdleUI();
        UpdateExitUI();
    }

    private void OnDestroy()
    {
        UnbindServiceEvents();
    }

    private void RefreshReferences()
    {
        if (focusSessionService == null)
            focusSessionService = FocusSessionService.Instance;

        if (focusSessionService == null)
            focusSessionService = FindAnyObjectByType<FocusSessionService>();

        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (appFocusTracker == null)
            appFocusTracker = FindAnyObjectByType<AppFocusTracker>();
    }

    private void BindServiceEvents()
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

    private void UnbindServiceEvents()
    {
        if (focusSessionService == null)
            return;

        focusSessionService.OnSessionStarted -= HandleSessionStarted;
        focusSessionService.OnSessionTick -= HandleSessionTick;
        focusSessionService.OnSessionFinished -= HandleSessionFinished;
    }

    public void StartStudy10MinTest()
    {
        StartTestSession("Study", "공부하기", 10, 10, 10f);
    }

    public void StartStudy30MinTest()
    {
        StartTestSession("Study", "공부하기", 30, 30, 10f);
    }

    public void StartReading10MinTest()
    {
        StartTestSession("Reading", "독서하기", 10, 10, 10f);
    }

    public void StartExercise10MinTest()
    {
        StartTestSession("Exercise", "운동하기", 10, 10, 10f);
    }

    public void StartTestSession(
        string goalType,
        string goalName,
        int plannedMinutes,
        int rewardMinutes,
        float testSeconds
    )
    {
        RefreshReferences();

        if (focusSessionService == null)
        {
            Debug.LogError("[FocusSessionTimer] FocusSessionService를 찾지 못했습니다.", this);
            return;
        }

        if (focusSessionService.IsRunning)
        {
            Debug.LogWarning("[FocusSessionTimer] 이미 집중 세션이 진행 중입니다.", this);
            return;
        }

        if (autoPlantEmptyPlotsForTest)
            PlantDefaultCropToEmptyPlotsForTest();

        FocusSessionConfig config = FocusSessionConfig.Create(
            goalType,
            goalName,
            plannedMinutes,
            rewardMinutes,
            useShortTestDuration,
            useShortTestDuration ? testSeconds : 0f
        );

        gameStateManager?.SetSelectedSession(config);
        focusSessionService.autoMoveSceneOnFinish = false;
        focusSessionService.StartSession(config);
    }

    public void CancelSession()
    {
        RefreshReferences();

        if (focusSessionService == null || !focusSessionService.IsRunning)
            return;

        focusSessionService.CancelSession();
    }

    private void HandleSessionStarted(FocusSessionRuntimeData runtimeData)
    {
        SetSessionRunningUI(true);
        UpdateRuntimeUI(runtimeData);

        string goalName = runtimeData != null && runtimeData.config != null
            ? runtimeData.config.goalName
            : "선택한 목표";

        if (statusText != null)
            statusText.text = "집중 세션이 진행 중입니다. 스마트폰을 내려놓고 기다려 주세요.";

        Debug.Log($"[FocusSessionTimer] 집중 세션 시작: {goalName}");
    }

    private void HandleSessionTick(FocusSessionRuntimeData runtimeData)
    {
        UpdateRuntimeUI(runtimeData);
    }

    private void HandleSessionFinished(FocusSessionResult result, RewardResultData reward)
    {
        SetSessionRunningUI(false);
        UpdateExitUI();

        if (reward != null && reward.finalSuccess)
        {
            if (statusText != null)
            {
                statusText.text =
                    $"성공! 성장 +{reward.rewardGrowth}, 해금 +{reward.rewardUnlockProgress}, " +
                    $"스태미너 +{reward.rewardStamina}";
            }
        }
        else
        {
            string reason = reward == null || string.IsNullOrWhiteSpace(reward.failReasonMessage)
                ? "세션이 중단되었습니다. 보상은 지급되지 않습니다."
                : reward.failReasonMessage;

            if (statusText != null)
                statusText.text = reason;
        }

        Debug.Log(
            $"[FocusSessionTimer] 집중 세션 종료: {result?.goalName}, " +
            $"성공 {reward != null && reward.finalSuccess}, 집중 {result?.focusedMinutes ?? 0}분"
        );
    }

    private void UpdateRuntimeUI(FocusSessionRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return;

        if (goalText != null)
        {
            string goalName = runtimeData.config == null ? "선택한 목표" : runtimeData.config.goalName;
            goalText.text = $"목표: {goalName}";
        }

        if (timerText != null)
            timerText.text = GameDataUtility.ToMinuteSecondText(runtimeData.remainingSeconds);

        UpdateExitUI();
    }

    private void UpdateExitUI()
    {
        if (exitText == null)
            return;

        RefreshReferences();

        if (focusSessionService != null)
        {
            exitText.text = focusSessionService.GetExitInfoText();
            return;
        }

        if (appFocusTracker == null)
        {
            exitText.text = "이탈: 추적 없음";
            return;
        }

        exitText.text =
            $"이탈: {appFocusTracker.GetExitCount()}회 / {appFocusTracker.GetTotalExitSeconds():F1}초";
    }

    private void UpdateIdleUI()
    {
        if (goalText != null)
            goalText.text = "목표: 선택 없음";

        if (timerText != null)
            timerText.text = "00:00";

        if (statusText != null)
            statusText.text = "테스트할 집중 목표를 선택하세요.";
    }

    private void SetSessionRunningUI(bool running)
    {
        foreach (Button button in startButtons)
        {
            if (button != null)
                button.interactable = !running;
        }

        if (cancelButton != null)
            cancelButton.interactable = running;
    }

    private void PlantDefaultCropToEmptyPlotsForTest()
    {
        FarmManager farm = FarmManager.Instance;

        if (farm == null || farm.defaultCrop == null)
            return;

        foreach (CropPlot plot in farm.cropPlots)
        {
            if (plot != null && plot.IsEmpty)
                plot.Plant(farm.defaultCrop);
        }
    }
}
