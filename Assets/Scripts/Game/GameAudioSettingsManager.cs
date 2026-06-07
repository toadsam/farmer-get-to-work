using UnityEngine;

public class GameAudioSettingsManager : MonoBehaviour
{
    public static GameAudioSettingsManager Instance { get; private set; }

    private const string MasterVolumeKey = "Setting_MasterVolume";
    private const string MusicVolumeKey = "Setting_MusicVolume";
    private const string SfxVolumeKey = "Setting_SfxVolume";

    [Header("Runtime Values")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        LoadSettings();
        ApplyAll();
    }

    private void Start()
    {
        ApplyAll();
    }

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = masterVolume;

        SaveSettings();

        Debug.Log($"[AudioSettings] Master Volume: {masterVolume:F2}");
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (FocusMusicPlayer.Instance != null)
            FocusMusicPlayer.Instance.SetVolume(musicVolume);

        SaveSettings();

        Debug.Log($"[AudioSettings] Music Volume: {musicVolume:F2}");
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        SaveSettings();

        Debug.Log($"[AudioSettings] SFX Volume: {sfxVolume:F2}");
    }

    public void ApplyAll()
    {
        AudioListener.volume = masterVolume;

        if (FocusMusicPlayer.Instance != null)
            FocusMusicPlayer.Instance.SetVolume(musicVolume);
    }

    [ContextMenu("Reset Audio Settings")]
    public void ResetAudioSettings()
    {
        masterVolume = 1f;
        musicVolume = 0.6f;
        sfxVolume = 1f;

        ApplyAll();
        SaveSettings();

        Debug.Log("[AudioSettings] 설정 초기화 완료");
    }
}