using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Selected Session")]
    public FocusSessionConfig selectedSessionConfig = new FocusSessionConfig();

    [Header("Last Session Result")]
    public FocusSessionResult lastSessionResult;
    public RewardResultData lastRewardResult;

    public bool HasSelectedSession
    {
        get
        {
            return selectedSessionConfig != null && selectedSessionConfig.IsValid();
        }
    }

    public bool HasLastSessionResult
    {
        get
        {
            return lastSessionResult != null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void SetSelectedSession(FocusSessionConfig config)
    {
        if (config == null || !config.IsValid())
        {
            Debug.LogWarning("[GameState] 유효하지 않은 세션 설정입니다.", this);
            return;
        }

        selectedSessionConfig = config;

        Debug.Log(
            $"[GameState] 세션 설정 저장: {config.goalName}, " +
            $"계획 {config.plannedMinutes}분, 보상 {config.rewardMinutes}분"
        );
    }

    public void SetSelectedGoal(
        string goalType,
        string goalName,
        int plannedMinutes,
        int rewardMinutes,
        bool useTestDuration = false,
        float testDurationSeconds = 0f
    )
    {
        FocusSessionConfig config = FocusSessionConfig.Create(
            goalType,
            goalName,
            plannedMinutes,
            rewardMinutes,
            useTestDuration,
            testDurationSeconds
        );

        SetSelectedSession(config);
    }

    public void ClearSelectedSession()
    {
        selectedSessionConfig = new FocusSessionConfig();
        Debug.Log("[GameState] 선택 세션 초기화");
    }

    public void SaveLastSessionResult(
        FocusSessionResult result,
        RewardResultData rewardResult
    )
    {
        lastSessionResult = result;
        lastRewardResult = rewardResult;

        if (result == null)
        {
            Debug.LogWarning("[GameState] 저장할 세션 결과가 없습니다.", this);
            return;
        }

        string resultText = rewardResult != null && rewardResult.finalSuccess
            ? "성공"
            : "실패";

        Debug.Log(
            $"[GameState] 마지막 세션 결과 저장: {result.goalName}, {resultText}, " +
            $"집중 {result.focusedMinutes}분"
        );
    }

    public void ClearLastSessionResult()
    {
        lastSessionResult = null;
        lastRewardResult = null;

        Debug.Log("[GameState] 마지막 세션 결과 초기화");
    }

    public void SetSelectedMusicTrack(string musicTrackId)
    {
        if (selectedSessionConfig == null)
            selectedSessionConfig = new FocusSessionConfig();

        selectedSessionConfig.musicTrackId = musicTrackId;

        Debug.Log($"[GameState] 선택 음악 저장: {musicTrackId}");
    }

    public void SetBeforeEmotionData(SessionEmotionData emotionData)
    {
        if (selectedSessionConfig == null)
            selectedSessionConfig = new FocusSessionConfig();

        if (emotionData == null)
            emotionData = new SessionEmotionData();

        selectedSessionConfig.emotionData = emotionData.Clone();

        Debug.Log(
            $"[GameState] 세션 전 감정 저장 / 기분: {emotionData.beforeMoodId}, " +
            $"점수: {emotionData.beforeMoodScore}, 욕구: {emotionData.beforePhoneUrgeLevel}"
        );
    }

    public void SetSelectedMusicAndBeforeEmotion(
        string musicTrackId,
        SessionEmotionData emotionData
    )
    {
        SetSelectedMusicTrack(musicTrackId);
        SetBeforeEmotionData(emotionData);
    }
}