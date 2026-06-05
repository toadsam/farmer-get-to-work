using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusSessionTimer : MonoBehaviour
{
    [Header("References")]
    public FocusSessionResultProcessor resultProcessor;
    public AppFocusTracker appFocusTracker;

    [Header("UI")]
    public TMP_Text goalText;
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text exitText;

    public Button[] startButtons;
    public Button cancelButton;

    [Header("Test Mode")]
    [Tooltip("테스트 편의를 위해 빈 밭이 있으면 세션 성공 직전에 기본 작물을 자동으로 심습니다.")]
    public bool autoPlantEmptyPlotsForTest = true;

    private bool isRunning;

    private string currentGoalType;
    private string currentGoalName;

    private int currentPlannedMinutes;
    private int currentRewardMinutes;

    private float sessionDurationSeconds;
    private float remainingSeconds;

    private DateTime startedAt;

    private void Awake()
    {
        if (resultProcessor == null)
            resultProcessor = FindAnyObjectByType<FocusSessionResultProcessor>();

        if (appFocusTracker == null)
            appFocusTracker = FindAnyObjectByType<AppFocusTracker>();

        SetSessionRunningUI(false);
        UpdateIdleUI();
        UpdateExitUI();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        remainingSeconds -= Time.deltaTime;

        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            UpdateTimerUI();
            CompleteSession();
            return;
        }

        UpdateTimerUI();
        UpdateExitUI();
    }

    public void StartStudy10MinTest()
    {
        StartTestSession(
            goalType: "Study",
            goalName: "공부하기",
            plannedMinutes: 10,
            rewardMinutes: 10,
            testSeconds: 10f
        );
    }

    public void StartStudy30MinTest()
    {
        StartTestSession(
            goalType: "Study",
            goalName: "공부하기",
            plannedMinutes: 30,
            rewardMinutes: 30,
            testSeconds: 10f
        );
    }

    public void StartReading10MinTest()
    {
        StartTestSession(
            goalType: "Reading",
            goalName: "독서하기",
            plannedMinutes: 10,
            rewardMinutes: 10,
            testSeconds: 10f
        );
    }

    public void StartExercise10MinTest()
    {
        StartTestSession(
            goalType: "Exercise",
            goalName: "운동하기",
            plannedMinutes: 10,
            rewardMinutes: 10,
            testSeconds: 10f
        );
    }

    public void StartTestSession(
        string goalType,
        string goalName,
        int plannedMinutes,
        int rewardMinutes,
        float testSeconds
    )
    {
        if (isRunning)
        {
            Debug.LogWarning("[FocusSessionTimer] 이미 세션이 진행 중입니다.", this);
            return;
        }

        if (resultProcessor == null)
        {
            Debug.LogError("[FocusSessionTimer] FocusSessionResultProcessor가 연결되지 않았습니다.", this);
            return;
        }

        currentGoalType = goalType;
        currentGoalName = goalName;
        currentPlannedMinutes = plannedMinutes;
        currentRewardMinutes = rewardMinutes;

        sessionDurationSeconds = Mathf.Max(1f, testSeconds);
        remainingSeconds = sessionDurationSeconds;

        startedAt = DateTime.Now;
        isRunning = true;

        appFocusTracker?.ResetAndStartTracking();

        SetSessionRunningUI(true);
        UpdateTimerUI();
        UpdateExitUI();

        if (goalText != null)
            goalText.text = $"목표: {currentGoalName}";

        if (statusText != null)
            statusText.text = "집중 세션 진행 중입니다. 스마트폰을 사용하지 않고 기다려 주세요.";

        Debug.Log($"[FocusSessionTimer] 세션 시작: {currentGoalName}, 보상 기준 {currentRewardMinutes}분");
    }

    public void CancelSession()
    {
        if (!isRunning)
            return;

        float progressRatio = 1f - (remainingSeconds / sessionDurationSeconds);
        int focusedMinutes = Mathf.FloorToInt(currentRewardMinutes * progressRatio);

        int exitCount = 0;
        float totalExitSeconds = 0f;

        if (appFocusTracker != null)
        {
            appFocusTracker.StopTracking();
            exitCount = appFocusTracker.GetExitCount();
            totalExitSeconds = appFocusTracker.GetTotalExitSeconds();
        }

        FocusSessionResult result = new FocusSessionResult
        {
            goalType = currentGoalType,
            goalName = currentGoalName,
            plannedMinutes = currentPlannedMinutes,
            focusedMinutes = focusedMinutes,
            success = false,
            exitCount = exitCount,
            totalExitSeconds = totalExitSeconds,
            startedAt = startedAt,
            endedAt = DateTime.Now
        };

        isRunning = false;
        SetSessionRunningUI(false);
        UpdateExitUI();

        resultProcessor.ApplySessionResult(result);

        if (statusText != null)
            statusText.text = "세션을 중단했습니다. 보상은 지급되지 않습니다.";

        Debug.Log("[FocusSessionTimer] 세션 중단");
    }

    private void CompleteSession()
    {
        if (!isRunning)
            return;

        isRunning = false;

        int exitCount = 0;
        float totalExitSeconds = 0f;

        if (appFocusTracker != null)
        {
            appFocusTracker.StopTracking();
            exitCount = appFocusTracker.GetExitCount();
            totalExitSeconds = appFocusTracker.GetTotalExitSeconds();
        }

        if (autoPlantEmptyPlotsForTest)
            PlantDefaultCropToEmptyPlotsForTest();

        FocusSessionResult result = new FocusSessionResult
        {
            goalType = currentGoalType,
            goalName = currentGoalName,
            plannedMinutes = currentPlannedMinutes,
            focusedMinutes = currentRewardMinutes,
            success = true,
            exitCount = exitCount,
            totalExitSeconds = totalExitSeconds,
            startedAt = startedAt,
            endedAt = DateTime.Now
        };

        resultProcessor.ApplySessionResult(result);

        SetSessionRunningUI(false);
        UpdateExitUI();

        if (statusText != null)
        {
            if (totalExitSeconds >= 180f)
                statusText.text = "세션 실패: 앱을 너무 오래 이탈했습니다.";
            else if (totalExitSeconds > 30f)
                statusText.text = $"성공했지만 이탈 시간이 있어 보상이 감소했습니다. 이탈 시간: {totalExitSeconds:F1}초";
            else
                statusText.text = $"성공! {currentRewardMinutes}분 집중 보상이 농장에 적용되었습니다.";
        }

        Debug.Log(
            $"[FocusSessionTimer] 세션 완료: {currentGoalName}, 집중 {currentRewardMinutes}분, " +
            $"이탈 횟수 {exitCount}, 이탈 시간 {totalExitSeconds:F1}초"
        );
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

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingSeconds);
        int minutesPart = seconds / 60;
        int secondsPart = seconds % 60;

        timerText.text = $"{minutesPart:00}:{secondsPart:00}";
    }

    private void UpdateExitUI()
    {
        if (exitText == null)
            return;

        if (appFocusTracker == null)
        {
            exitText.text = "이탈: 추적기 없음";
            return;
        }

        exitText.text =
            $"이탈: {appFocusTracker.GetExitCount()}회 / {appFocusTracker.GetTotalExitSeconds():F1}초";
    }

    private void UpdateIdleUI()
    {
        if (goalText != null)
            goalText.text = "목표: 없음";

        if (timerText != null)
            timerText.text = "00:00";

        if (statusText != null)
            statusText.text = "테스트할 집중 세션을 선택하세요.";
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
}