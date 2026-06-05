using System;
using UnityEngine;

public class FocusSessionService : MonoBehaviour
{
    public static FocusSessionService Instance { get; private set; }

    [Header("References")]
    public GameStateManager gameStateManager;
    public AppFocusTracker appFocusTracker;
    public SceneFlowManager sceneFlowManager;

    [Header("Reward")]
    public RewardProcessor rewardProcessor;

    [Header("Scene Flow")]
    [Tooltip("세션 종료 후 자동으로 SuccessScene / FailScene으로 이동할지 여부입니다. UI 통합 전에는 꺼두는 것을 권장합니다.")]
    public bool autoMoveSceneOnFinish = false;

    [Header("Runtime")]
    public FocusSessionRuntimeData runtimeData = new FocusSessionRuntimeData();

    public FocusSessionResult LastResult { get; private set; }
    public RewardResultData LastRewardResult { get; private set; }

    public bool IsRunning
    {
        get { return runtimeData != null && runtimeData.isRunning; }
    }

    public float RemainingSeconds
    {
        get
        {
            if (runtimeData == null)
                return 0f;

            return runtimeData.remainingSeconds;
        }
    }

    public float ElapsedSeconds
    {
        get
        {
            if (runtimeData == null)
                return 0f;

            return runtimeData.elapsedSeconds;
        }
    }

    public int CurrentExitCount
    {
        get
        {
            if (appFocusTracker == null)
                return runtimeData != null ? runtimeData.exitCount : 0;

            return appFocusTracker.GetExitCount();
        }
    }

    public float CurrentTotalExitSeconds
    {
        get
        {
            if (appFocusTracker == null)
                return runtimeData != null ? runtimeData.totalExitSeconds : 0f;

            return appFocusTracker.GetTotalExitSeconds();
        }
    }

    public event Action<FocusSessionRuntimeData> OnSessionStarted;
    public event Action<FocusSessionRuntimeData> OnSessionTick;
    public event Action<FocusSessionResult, RewardResultData> OnSessionFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (appFocusTracker == null)
            appFocusTracker = FindAnyObjectByType<AppFocusTracker>();

        if (sceneFlowManager == null)
            sceneFlowManager = FindAnyObjectByType<SceneFlowManager>();

        if (rewardProcessor == null)
            rewardProcessor = FindAnyObjectByType<RewardProcessor>();

        if (runtimeData == null)
            runtimeData = new FocusSessionRuntimeData();
    }

    private void Update()
    {
        if (runtimeData == null || !runtimeData.isRunning)
            return;

        runtimeData.remainingSeconds -= Time.deltaTime;

        if (runtimeData.remainingSeconds < 0f)
            runtimeData.remainingSeconds = 0f;

        runtimeData.elapsedSeconds = runtimeData.durationSeconds - runtimeData.remainingSeconds;

        runtimeData.exitCount = CurrentExitCount;
        runtimeData.totalExitSeconds = CurrentTotalExitSeconds;

        OnSessionTick?.Invoke(runtimeData);

        if (runtimeData.remainingSeconds <= 0f)
        {
            CompleteSession();
        }
    }

    public bool StartSelectedSession()
    {
        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (gameStateManager == null)
        {
            Debug.LogError("[FocusSessionService] GameStateManager가 없습니다.", this);
            return false;
        }

        if (!gameStateManager.HasSelectedSession)
        {
            Debug.LogWarning("[FocusSessionService] 선택된 세션 설정이 없습니다.", this);
            return false;
        }

        return StartSession(gameStateManager.selectedSessionConfig);
    }

    public bool StartSession(FocusSessionConfig config)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[FocusSessionService] 이미 세션이 진행 중입니다.", this);
            return false;
        }

        if (config == null || !config.IsValid())
        {
            Debug.LogWarning("[FocusSessionService] 유효하지 않은 세션 설정입니다.", this);
            return false;
        }

        FocusSessionConfig copiedConfig = CopyConfig(config);

        runtimeData.Reset();
        runtimeData.config = copiedConfig;
        runtimeData.isRunning = true;
        runtimeData.isCompleted = false;
        runtimeData.durationSeconds = copiedConfig.GetSessionDurationSeconds();
        runtimeData.remainingSeconds = runtimeData.durationSeconds;
        runtimeData.elapsedSeconds = 0f;
        runtimeData.startedAtTicks = DateTime.Now.Ticks;

        LastResult = null;
        LastRewardResult = null;

        if (appFocusTracker == null)
            appFocusTracker = FindAnyObjectByType<AppFocusTracker>();

        appFocusTracker?.ResetAndStartTracking();

        Debug.Log(
            $"[FocusSessionService] 세션 시작 / 목표: {copiedConfig.goalName}, " +
            $"계획: {copiedConfig.plannedMinutes}분, 실제 진행 시간: {runtimeData.durationSeconds:F1}초"
        );

        OnSessionStarted?.Invoke(runtimeData);

        return true;
    }

    public void CancelSession()
    {
        if (!IsRunning)
        {
            Debug.LogWarning("[FocusSessionService] 취소할 세션이 없습니다.", this);
            return;
        }

        FinishSession(rawSuccess: false, forcedFocusedMinutes: -1, reason: "Cancel");
    }

    public void CompleteSession()
    {
        if (!IsRunning)
            return;

        int focusedMinutes = runtimeData.config != null
            ? runtimeData.config.rewardMinutes
            : runtimeData.GetFocusedMinutes();

        FinishSession(rawSuccess: true, forcedFocusedMinutes: focusedMinutes, reason: "Complete");
    }

    public void ForceCompleteSessionForTest()
    {
        if (!IsRunning)
        {
            Debug.LogWarning("[FocusSessionService] 완료할 세션이 없습니다.", this);
            return;
        }

        CompleteSession();
    }

    private void FinishSession(bool rawSuccess, int forcedFocusedMinutes, string reason)
    {
        if (runtimeData == null || runtimeData.config == null)
        {
            Debug.LogWarning("[FocusSessionService] 종료할 세션 데이터가 없습니다.", this);
            return;
        }

        if (appFocusTracker != null)
        {
            appFocusTracker.StopTracking();
            runtimeData.exitCount = appFocusTracker.GetExitCount();
            runtimeData.totalExitSeconds = appFocusTracker.GetTotalExitSeconds();
        }

        runtimeData.isRunning = false;
        runtimeData.isCompleted = true;
        runtimeData.endedAtTicks = DateTime.Now.Ticks;

        int focusedMinutes = forcedFocusedMinutes >= 0
            ? forcedFocusedMinutes
            : runtimeData.GetFocusedMinutes();

        FocusSessionResult result = new FocusSessionResult
        {
            goalType = runtimeData.config.goalType,
            goalName = runtimeData.config.goalName,

            plannedMinutes = runtimeData.config.plannedMinutes,
            focusedMinutes = focusedMinutes,

            success = rawSuccess,

            exitCount = runtimeData.exitCount,
            totalExitSeconds = runtimeData.totalExitSeconds,

            startedAt = GameDataUtility.FromTicks(runtimeData.startedAtTicks),
            endedAt = GameDataUtility.FromTicks(runtimeData.endedAtTicks)
        };

        LastResult = result;

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (rewardProcessor == null)
            rewardProcessor = FindAnyObjectByType<RewardProcessor>();

        if (rewardProcessor != null)
        {
            LastRewardResult = rewardProcessor.ProcessSessionResult(result);
        }
        else
        {
            Debug.LogWarning("[FocusSessionService] RewardProcessor가 없어 보상 계산을 적용하지 못했습니다.", this);
            LastRewardResult = null;
            gameStateManager?.SaveLastSessionResult(result, null);
        }

        Debug.Log(
            $"[FocusSessionService] 세션 종료 / 사유: {reason}, " +
            $"목표: {result.goalName}, 성공값: {result.success}, " +
            $"집중 {result.focusedMinutes}분, 이탈 {result.exitCount}회, {result.totalExitSeconds:F1}초"
        );

        OnSessionFinished?.Invoke(result, LastRewardResult);

        if (autoMoveSceneOnFinish)
        {
            if (sceneFlowManager == null)
                sceneFlowManager = FindAnyObjectByType<SceneFlowManager>();

            if (sceneFlowManager != null)
            {
                if (LastRewardResult != null && LastRewardResult.finalSuccess)
                    sceneFlowManager.GoSuccess();
                else
                    sceneFlowManager.GoFail();
            }
        }
    }

    private FocusSessionConfig CopyConfig(FocusSessionConfig source)
    {
        return new FocusSessionConfig
        {
            goalType = source.goalType,
            goalName = source.goalName,

            plannedMinutes = source.plannedMinutes,
            rewardMinutes = source.rewardMinutes,

            useTestDuration = source.useTestDuration,
            testDurationSeconds = source.testDurationSeconds,

            musicTrackId = source.musicTrackId,
            selectedAtTicks = source.selectedAtTicks
        };
    }

    public string GetRemainingTimeText()
    {
        if (runtimeData == null)
            return "00:00";

        return GameDataUtility.ToMinuteSecondText(runtimeData.remainingSeconds);
    }

    public string GetElapsedTimeText()
    {
        if (runtimeData == null)
            return "00:00";

        return GameDataUtility.ToMinuteSecondText(runtimeData.elapsedSeconds);
    }

    public string GetExitInfoText()
    {
        return $"이탈 {CurrentExitCount}회 / {CurrentTotalExitSeconds:F1}초";
    }

    [ContextMenu("Test Start Study 10 Sec")]
    public void TestStartStudy10Sec()
    {
        FocusSessionConfig config = FocusSessionConfig.Create(
            goalType: "Study",
            goalName: "공부하기",
            plannedMinutes: 10,
            rewardMinutes: 10,
            useTestDuration: true,
            testDurationSeconds: 10f
        );

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        gameStateManager?.SetSelectedSession(config);

        StartSession(config);
    }

    [ContextMenu("Test Start Study 30 Sec")]
    public void TestStartStudy30Sec()
    {
        FocusSessionConfig config = FocusSessionConfig.Create(
            goalType: "Study",
            goalName: "공부하기",
            plannedMinutes: 30,
            rewardMinutes: 30,
            useTestDuration: true,
            testDurationSeconds: 30f
        );

        if (gameStateManager == null)
            gameStateManager = FindAnyObjectByType<GameStateManager>();

        gameStateManager?.SetSelectedSession(config);

        StartSession(config);
    }

    [ContextMenu("Test Cancel Running Session")]
    public void TestCancelRunningSession()
    {
        CancelSession();
    }

    [ContextMenu("Test Complete Running Session")]
    public void TestCompleteRunningSession()
    {
        ForceCompleteSessionForTest();
    }
}