using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FarmerGetToWork;

public class TemporarySessionSetupBinder : MonoBehaviour
{
    [Header("Music UI")]
    public TMP_Dropdown musicDropdown;
    public TMP_Text selectedMusicText;

    [Header("Before Emotion UI")]
    public TMP_Dropdown beforeMoodDropdown;
    public Slider beforeMoodScoreSlider;
    public TMP_Text beforeMoodScoreText;
    public Slider beforePhoneUrgeSlider;
    public TMP_Text beforePhoneUrgeText;
    public TMP_InputField beforeReasonInput;

    [Header("Buttons")]
    public Button applyButton;
    public Button startSessionButton;

    [Header("Status")]
    public TMP_Text statusText;

    [Header("Fallback Test Session")]
    public bool createFallbackSessionIfMissing = true;
    public string fallbackGoalType = "Study";
    public string fallbackGoalName = "공부하기";
    public int fallbackPlannedMinutes = 10;
    public int fallbackRewardMinutes = 10;
    public bool useTestDuration = true;
    public float testDurationSeconds = 10f;

    [Header("Scene Flow")]
    public bool startSessionImmediately = false;
    public bool moveToFocusSceneAfterApply = true;

    [Header("Music Library")]
    public FocusMusicLibrary musicLibrary;

    [Header("Auto Music Selection")]
    public bool autoSelectMusicByGoal = true;
    public string silentTrackId = "Silent";
    public string defaultTrackId = "Default_01";
    public string studyTrackId = "Study_WhiteNoise_01";
    public string readingTrackId = "Reading_Classical_01";
    public string exerciseTrackId = "Exercise_Upbeat_01";
    public string sleepTrackId = "Sleep_ASMR_01";

    private readonly List<string> musicTrackIds = new List<string>();

    private readonly string[] moodIds =
    {
        "Calm",
        "Happy",
        "Tired",
        "Anxious",
        "Bored",
        "Irritated"
    };

    private readonly string[] moodNames =
    {
        "차분함",
        "기분 좋음",
        "피곤함",
        "불안함",
        "지루함",
        "짜증남"
    };

    private void Start()
    {
        RegisterMusicLibraryIfNeeded();

        PopulateMoodDropdown();
        EnsureSilentTrack();
        PopulateMusicDropdown();

        if (autoSelectMusicByGoal)
            SelectMusicByCurrentGoal();

        BindEvents();
        RefreshTexts();
        RefreshStatus("임시 세션 설정 UI 준비 완료");
    }

    private void RegisterMusicLibraryIfNeeded()
    {
        if (musicLibrary == null)
            musicLibrary = FindAnyObjectByType<FocusMusicLibrary>();

        if (musicLibrary != null)
            musicLibrary.Register();
    }

    private void SelectMusicByCurrentGoal()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null || gameState.selectedSessionConfig == null)
        {
            SelectMusicDropdownByTrackId(defaultTrackId);
            return;
        }

        string goalType = gameState.selectedSessionConfig.goalType;
        string goalName = gameState.selectedSessionConfig.goalName;

        string targetTrackId = GetRecommendedTrackId(goalType, goalName);

        if (!SelectMusicDropdownByTrackId(targetTrackId))
            SelectMusicDropdownByTrackId(defaultTrackId);

        ApplySelection();
    }

    private string GetRecommendedTrackId(string goalType, string goalName)
    {
        string text = $"{goalType} {goalName}";

        if (text.Contains("Study") || text.Contains("공부"))
            return studyTrackId;

        if (text.Contains("Reading") || text.Contains("Read") || text.Contains("독서"))
            return readingTrackId;

        if (text.Contains("Exercise") || text.Contains("운동"))
            return exerciseTrackId;

        if (text.Contains("Sleep") || text.Contains("수면"))
            return sleepTrackId;

        return defaultTrackId;
    }

    private bool SelectMusicDropdownByTrackId(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
            return false;

        if (musicDropdown == null)
            return false;

        int index = musicTrackIds.IndexOf(trackId);

        if (index < 0)
            return false;

        musicDropdown.value = index;
        musicDropdown.RefreshShownValue();

        HandleMusicChanged(index);

        return true;
    }

    private void BindEvents()
    {
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplySelection);
            applyButton.onClick.AddListener(ApplySelection);
        }

        if (startSessionButton != null)
        {
            startSessionButton.onClick.RemoveListener(StartOrMoveNext);
            startSessionButton.onClick.AddListener(StartOrMoveNext);
        }

        if (musicDropdown != null)
        {
            musicDropdown.onValueChanged.RemoveListener(HandleMusicChanged);
            musicDropdown.onValueChanged.AddListener(HandleMusicChanged);
        }

        if (beforeMoodScoreSlider != null)
        {
            beforeMoodScoreSlider.onValueChanged.RemoveListener(HandleSliderChanged);
            beforeMoodScoreSlider.onValueChanged.AddListener(HandleSliderChanged);
        }

        if (beforePhoneUrgeSlider != null)
        {
            beforePhoneUrgeSlider.onValueChanged.RemoveListener(HandleSliderChanged);
            beforePhoneUrgeSlider.onValueChanged.AddListener(HandleSliderChanged);
        }
    }

    private void PopulateMoodDropdown()
    {
        if (beforeMoodDropdown == null)
            return;

        beforeMoodDropdown.ClearOptions();
        beforeMoodDropdown.AddOptions(new List<string>(moodNames));
        beforeMoodDropdown.value = 0;
        beforeMoodDropdown.RefreshShownValue();
    }

    private void EnsureSilentTrack()
    {
        FocusMusicPlayer player = FocusMusicPlayer.Instance;

        if (player == null)
            player = FindAnyObjectByType<FocusMusicPlayer>();

        if (player == null)
        {
            RefreshStatus("FocusMusicPlayer를 찾지 못했습니다.");
            return;
        }

        FocusMusicTrack silentTrack = player.GetTrackById("Silent");

        if (silentTrack != null)
            return;

        player.RegisterTracks(new List<FocusMusicTrack>
        {
            new FocusMusicTrack
            {
                trackId = "Silent",
                displayName = "무음",
                clip = null,
                isSilentTrack = true,
                unlockedByDefault = true,
                requiredUnlockProgress = 0,
                loop = false
            }
        });

        player.UnlockTrack("Silent");
    }

    private void PopulateMusicDropdown()
    {
        if (musicDropdown == null)
            return;

        FocusMusicPlayer player = FocusMusicPlayer.Instance;

        if (player == null)
            player = FindAnyObjectByType<FocusMusicPlayer>();

        musicTrackIds.Clear();
        musicDropdown.ClearOptions();

        List<string> optionNames = new List<string>();

        if (player != null)
        {
            List<FocusMusicTrack> tracks = player.GetUnlockedTracks();

            foreach (FocusMusicTrack track in tracks)
            {
                if (track == null)
                    continue;

                if (string.IsNullOrEmpty(track.trackId))
                    continue;

                musicTrackIds.Add(track.trackId);
                optionNames.Add(track.displayName);
            }
        }

        if (musicTrackIds.Count == 0)
        {
            musicTrackIds.Add("Silent");
            optionNames.Add("무음");
        }

        musicDropdown.AddOptions(optionNames);
        musicDropdown.value = 0;
        musicDropdown.RefreshShownValue();

        HandleMusicChanged(musicDropdown.value);
    }

    private void HandleMusicChanged(int index)
    {
        string trackId = GetSelectedMusicTrackId();

        FocusMusicPlayer player = FocusMusicPlayer.Instance;

        string displayName = trackId;

        if (player != null)
        {
            FocusMusicTrack track = player.GetTrackById(trackId);
            if (track != null)
                displayName = track.displayName;
        }

        if (selectedMusicText != null)
            selectedMusicText.text = $"선택 음악: {displayName}";
    }

    private void HandleSliderChanged(float value)
    {
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (beforeMoodScoreText != null && beforeMoodScoreSlider != null)
            beforeMoodScoreText.text = $"현재 기분 점수: {Mathf.RoundToInt(beforeMoodScoreSlider.value)}/5";

        if (beforePhoneUrgeText != null && beforePhoneUrgeSlider != null)
            beforePhoneUrgeText.text = $"스마트폰 사용 욕구: {Mathf.RoundToInt(beforePhoneUrgeSlider.value)}/5";
    }

    public void ApplySelection()
    {
        EnsureSelectedSessionExistsIfNeeded();

        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            RefreshStatus("GameStateManager를 찾지 못했습니다.");
            return;
        }

        string selectedTrackId = GetSelectedMusicTrackId();
        SessionEmotionData emotionData = BuildBeforeEmotionData();

        gameState.SetSelectedMusicAndBeforeEmotion(selectedTrackId, emotionData);

        RefreshStatus(
            $"저장 완료 / 음악: {selectedTrackId}, " +
            $"기분: {emotionData.beforeMoodId} {emotionData.beforeMoodScore}/5, " +
            $"욕구: {emotionData.beforePhoneUrgeLevel}/5"
        );
    }

    public void StartOrMoveNext()
    {
        ApplySelection();

        if (startSessionImmediately)
        {
            FocusSessionService focusService = FocusSessionService.Instance;

            if (focusService == null)
                focusService = FindAnyObjectByType<FocusSessionService>();

            if (focusService == null)
            {
                RefreshStatus("FocusSessionService를 찾지 못했습니다.");
                return;
            }

            bool started = focusService.StartSelectedSession();
            RefreshStatus(started ? "세션 시작 완료" : "세션 시작 실패");
            return;
        }

        if (moveToFocusSceneAfterApply)
        {
            SceneLoader.LoadScene(SceneLoader.FocusScene);
        }
    }

    private void EnsureSelectedSessionExistsIfNeeded()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
            return;

        if (gameState.HasSelectedSession)
            return;

        if (!createFallbackSessionIfMissing)
            return;

        gameState.SetSelectedGoal(
            fallbackGoalType,
            fallbackGoalName,
            fallbackPlannedMinutes,
            fallbackRewardMinutes,
            useTestDuration,
            testDurationSeconds
        );
    }

    private SessionEmotionData BuildBeforeEmotionData()
    {
        int moodIndex = beforeMoodDropdown != null
            ? Mathf.Clamp(beforeMoodDropdown.value, 0, moodIds.Length - 1)
            : 0;

        int moodScore = beforeMoodScoreSlider != null
            ? Mathf.RoundToInt(beforeMoodScoreSlider.value)
            : 3;

        int phoneUrge = beforePhoneUrgeSlider != null
            ? Mathf.RoundToInt(beforePhoneUrgeSlider.value)
            : 3;

        return new SessionEmotionData
        {
            beforeMoodId = moodIds[moodIndex],
            beforeMoodScore = Mathf.Clamp(moodScore, 1, 5),
            beforePhoneUrgeLevel = Mathf.Clamp(phoneUrge, 1, 5),
            beforeReasonText = beforeReasonInput != null
                ? beforeReasonInput.text
                : ""
        };
    }

    private string GetSelectedMusicTrackId()
    {
        if (musicDropdown == null)
            return "Silent";

        int index = Mathf.Clamp(musicDropdown.value, 0, musicTrackIds.Count - 1);

        if (musicTrackIds.Count == 0)
            return "Silent";

        return musicTrackIds[index];
    }

    private void RefreshStatus(string message)
    {
        Debug.Log($"[TemporarySessionSetup] {message}");

        if (statusText != null)
            statusText.text = message;
    }
}