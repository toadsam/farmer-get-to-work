using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FarmerGetToWork;

public class TemporaryAfterSessionEmotionBinder : MonoBehaviour
{
    [Header("After Emotion UI")]
    public TMP_Dropdown afterMoodDropdown;
    public Slider afterMoodScoreSlider;
    public TMP_Text afterMoodScoreText;

    public Slider afterPhoneUrgeSlider;
    public TMP_Text afterPhoneUrgeText;

    public Slider musicHelpedSlider;
    public TMP_Text musicHelpedText;

    public TMP_InputField afterReflectionInput;
    public TMP_InputField musicReactionInput;

    [Header("Buttons")]
    public Button saveButton;
    public Button goHomeButton;
    public Button goRecordButton;

    [Header("Status")]
    public TMP_Text statusText;

    [Header("Options")]
    public bool goHomeAfterSave = false;

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
        PopulateMoodDropdown();
        BindEvents();
        RefreshTexts();
        RefreshStatus("세션 후 상태 기록을 입력하세요.");
    }

    private void PopulateMoodDropdown()
    {
        if (afterMoodDropdown == null)
            return;

        afterMoodDropdown.ClearOptions();
        afterMoodDropdown.AddOptions(new List<string>(moodNames));
        afterMoodDropdown.value = 0;
        afterMoodDropdown.RefreshShownValue();
    }

    private void BindEvents()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(SaveAfterEmotion);
            saveButton.onClick.AddListener(SaveAfterEmotion);
        }

        if (goHomeButton != null)
        {
            goHomeButton.onClick.RemoveListener(GoHome);
            goHomeButton.onClick.AddListener(GoHome);
        }

        if (goRecordButton != null)
        {
            goRecordButton.onClick.RemoveListener(GoRecord);
            goRecordButton.onClick.AddListener(GoRecord);
        }

        if (afterMoodScoreSlider != null)
        {
            afterMoodScoreSlider.onValueChanged.RemoveListener(HandleSliderChanged);
            afterMoodScoreSlider.onValueChanged.AddListener(HandleSliderChanged);
        }

        if (afterPhoneUrgeSlider != null)
        {
            afterPhoneUrgeSlider.onValueChanged.RemoveListener(HandleSliderChanged);
            afterPhoneUrgeSlider.onValueChanged.AddListener(HandleSliderChanged);
        }

        if (musicHelpedSlider != null)
        {
            musicHelpedSlider.onValueChanged.RemoveListener(HandleSliderChanged);
            musicHelpedSlider.onValueChanged.AddListener(HandleSliderChanged);
        }
    }

    private void HandleSliderChanged(float value)
    {
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (afterMoodScoreText != null && afterMoodScoreSlider != null)
            afterMoodScoreText.text = $"현재 기분 점수: {Mathf.RoundToInt(afterMoodScoreSlider.value)}/5";

        if (afterPhoneUrgeText != null && afterPhoneUrgeSlider != null)
            afterPhoneUrgeText.text = $"스마트폰 사용 욕구: {Mathf.RoundToInt(afterPhoneUrgeSlider.value)}/5";

        if (musicHelpedText != null && musicHelpedSlider != null)
            musicHelpedText.text = $"음악 도움 정도: {Mathf.RoundToInt(musicHelpedSlider.value)}/5";
    }

    public void SaveAfterEmotion()
    {
        SessionRecordManager manager = SessionRecordManager.Instance;

        if (manager == null)
        {
            RefreshStatus("SessionRecordManager를 찾지 못했습니다.");
            return;
        }

        SessionEmotionData afterEmotion = BuildAfterEmotionData();

        bool updated = manager.UpdateLatestRecordAfterEmotion(afterEmotion);

        if (!updated)
        {
            RefreshStatus("업데이트할 세션 기록이 없습니다.");
            return;
        }

        RefreshStatus(
            $"저장 완료 / 기분: {afterEmotion.afterMoodId} {afterEmotion.afterMoodScore}/5, " +
            $"욕구: {afterEmotion.afterPhoneUrgeLevel}/5, 음악 도움: {afterEmotion.musicHelpedLevel}/5"
        );

        if (goHomeAfterSave)
            GoHome();
    }

    private SessionEmotionData BuildAfterEmotionData()
    {
        int moodIndex = afterMoodDropdown != null
            ? Mathf.Clamp(afterMoodDropdown.value, 0, moodIds.Length - 1)
            : 0;

        int moodScore = afterMoodScoreSlider != null
            ? Mathf.RoundToInt(afterMoodScoreSlider.value)
            : 3;

        int phoneUrge = afterPhoneUrgeSlider != null
            ? Mathf.RoundToInt(afterPhoneUrgeSlider.value)
            : 3;

        int musicHelped = musicHelpedSlider != null
            ? Mathf.RoundToInt(musicHelpedSlider.value)
            : 0;

        return new SessionEmotionData
        {
            afterMoodId = moodIds[moodIndex],
            afterMoodScore = Mathf.Clamp(moodScore, 1, 5),
            afterPhoneUrgeLevel = Mathf.Clamp(phoneUrge, 1, 5),
            afterReflectionText = afterReflectionInput != null ? afterReflectionInput.text : "",
            musicHelpedLevel = Mathf.Clamp(musicHelped, 0, 5),
            musicReactionText = musicReactionInput != null ? musicReactionInput.text : ""
        };
    }

    public void GoHome()
    {
        SceneLoader.LoadScene(SceneLoader.HomeScene);
    }

    public void GoRecord()
    {
        SceneLoader.LoadScene(SceneLoader.RecordScene);
    }

    private void RefreshStatus(string message)
    {
        Debug.Log($"[TemporaryAfterSessionEmotion] {message}");

        if (statusText != null)
            statusText.text = message;
    }
}