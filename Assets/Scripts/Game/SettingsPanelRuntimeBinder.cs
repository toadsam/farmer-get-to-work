using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelRuntimeBinder : MonoBehaviour
{
    [Header("Panel Roots")]
    public GameObject settingsPanelRoot;
    public GameObject resetConfirmPanelRoot;

    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Texts")]
    public TMP_Text masterVolumeValueText;
    public TMP_Text musicVolumeValueText;
    public TMP_Text sfxVolumeValueText;

    [Header("Buttons")]
    public Button openSettingsButton;
    public Button closeSettingsButton;
    public Button resetDataButton;
    public Button confirmResetButton;
    public Button cancelResetButton;

    [Header("Message")]
    public TMP_Text messageText;

    private void Awake()
    {
        BindButtons();
        BindSliders();

        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(false);
    }

    private void Start()
    {
        RefreshSlidersFromSettings();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        UnbindSliders();
    }

    private void BindButtons()
    {
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveListener(OpenSettings);
            openSettingsButton.onClick.AddListener(OpenSettings);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettings);
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        if (resetDataButton != null)
        {
            resetDataButton.onClick.RemoveListener(OpenResetConfirm);
            resetDataButton.onClick.AddListener(OpenResetConfirm);
        }

        if (confirmResetButton != null)
        {
            confirmResetButton.onClick.RemoveListener(ConfirmResetData);
            confirmResetButton.onClick.AddListener(ConfirmResetData);
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.onClick.RemoveListener(CloseResetConfirm);
            cancelResetButton.onClick.AddListener(CloseResetConfirm);
        }
    }

    private void UnbindButtons()
    {
        if (openSettingsButton != null)
            openSettingsButton.onClick.RemoveListener(OpenSettings);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.RemoveListener(CloseSettings);

        if (resetDataButton != null)
            resetDataButton.onClick.RemoveListener(OpenResetConfirm);

        if (confirmResetButton != null)
            confirmResetButton.onClick.RemoveListener(ConfirmResetData);

        if (cancelResetButton != null)
            cancelResetButton.onClick.RemoveListener(CloseResetConfirm);
    }

    private void BindSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }

    private void UnbindSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    private void RefreshSlidersFromSettings()
    {
        GameAudioSettingsManager audioSettings = GameAudioSettingsManager.Instance;

        if (audioSettings == null)
            audioSettings = FindAnyObjectByType<GameAudioSettingsManager>();

        if (audioSettings == null)
            return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = audioSettings.MasterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = audioSettings.MusicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = audioSettings.SfxVolume;

        RefreshVolumeTexts();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (GameAudioSettingsManager.Instance != null)
            GameAudioSettingsManager.Instance.SetMasterVolume(value);

        RefreshVolumeTexts();
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (GameAudioSettingsManager.Instance != null)
            GameAudioSettingsManager.Instance.SetMusicVolume(value);

        RefreshVolumeTexts();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (GameAudioSettingsManager.Instance != null)
            GameAudioSettingsManager.Instance.SetSfxVolume(value);

        RefreshVolumeTexts();
    }

    private void RefreshVolumeTexts()
    {
        if (masterVolumeValueText != null && masterVolumeSlider != null)
            masterVolumeValueText.text = ToPercentText(masterVolumeSlider.value);

        if (musicVolumeValueText != null && musicVolumeSlider != null)
            musicVolumeValueText.text = ToPercentText(musicVolumeSlider.value);

        if (sfxVolumeValueText != null && sfxVolumeSlider != null)
            sfxVolumeValueText.text = ToPercentText(sfxVolumeSlider.value);
    }

    private string ToPercentText(float value)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        return $"{percent}%";
    }

    public void OpenSettings()
    {
        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(true);

        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(false);

        RefreshSlidersFromSettings();
        SetMessage("");
    }

    public void CloseSettings()
    {
        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(false);
    }

    public void OpenResetConfirm()
    {
        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(true);

        SetMessage("정말 모든 저장 데이터를 초기화할까요?");
    }

    public void CloseResetConfirm()
    {
        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(false);

        SetMessage("");
    }

    public void ConfirmResetData()
    {
        if (SaveSystem.Instance == null)
        {
            SetMessage("SaveSystem이 없습니다.");
            Debug.LogError("[SettingsPanel] SaveSystem.Instance가 없습니다.");
            return;
        }

        SaveSystem.Instance.DeleteSaveFile();

        if (resetConfirmPanelRoot != null)
            resetConfirmPanelRoot.SetActive(false);

        SetMessage("저장 데이터가 초기화되었습니다.");

        Debug.Log("[SettingsPanel] 저장 데이터 초기화 버튼 실행 완료");
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}