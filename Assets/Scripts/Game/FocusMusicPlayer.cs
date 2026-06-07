using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FocusMusicTrack
{
    public string trackId = "Track_01";
    public string displayName = "기본 집중 음악";

    public AudioClip clip;

    public bool unlockedByDefault = true;

    [Tooltip("해금 진행도가 이 값 이상이면 해금됩니다.")]
    public int requiredUnlockProgress = 0;

    public bool loop = true;
}

[RequireComponent(typeof(AudioSource))]
public class FocusMusicPlayer : MonoBehaviour
{
    public static FocusMusicPlayer Instance { get; private set; }

    [Header("References")]
    public FocusSessionService focusSessionService;
    public UnlockManager unlockManager;

    [Header("Audio")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    public float volume = 0.6f;

    [Header("Tracks")]
    public List<FocusMusicTrack> tracks = new List<FocusMusicTrack>();

    [Header("Unlocked Tracks")]
    public List<string> unlockedTrackIds = new List<string>();

    [Header("Play Option")]
    public bool playRandomTrack = false;
    public bool stopMusicOnSessionEnd = true;

    public FocusMusicTrack CurrentTrack { get; private set; }
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    public event Action<FocusMusicTrack> OnMusicStarted;
    public event Action OnMusicStopped;
    public event Action<FocusMusicTrack> OnTrackUnlocked;

    private int lastTrackIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        EnsureDefaultUnlockedTracks();
    }

    private void Start()
    {
        BindFocusSessionService();
        RefreshUnlockedTracksFromCurrentProgress();

        if (GameAudioSettingsManager.Instance != null)
            SetVolume(GameAudioSettingsManager.Instance.MusicVolume);

        // GoalScene에서 세션이 먼저 시작되고 FocusScene에서 음악 라이브러리가 나중에 등록되는 경우 대비
        TryPlayIfSessionRunning();
    }

    private void OnDestroy()
    {
        UnbindFocusSessionService();

        if (Instance == this)
            Instance = null;
    }

    private void BindFocusSessionService()
    {
        if (focusSessionService == null)
            focusSessionService = FocusSessionService.Instance;

        if (focusSessionService == null)
            focusSessionService = FindAnyObjectByType<FocusSessionService>();

        if (focusSessionService == null)
            return;

        focusSessionService.OnSessionStarted -= HandleSessionStarted;
        focusSessionService.OnSessionFinished -= HandleSessionFinished;

        focusSessionService.OnSessionStarted += HandleSessionStarted;
        focusSessionService.OnSessionFinished += HandleSessionFinished;
    }

    private void UnbindFocusSessionService()
    {
        if (focusSessionService == null)
            return;

        focusSessionService.OnSessionStarted -= HandleSessionStarted;
        focusSessionService.OnSessionFinished -= HandleSessionFinished;
    }

    private void HandleSessionStarted(FocusSessionRuntimeData runtimeData)
    {
        if (runtimeData == null || runtimeData.config == null)
        {
            PlayFocusMusic();
            return;
        }

        if (!string.IsNullOrEmpty(runtimeData.config.musicTrackId))
            PlayFocusMusic(runtimeData.config.musicTrackId);
        else
            PlayFocusMusic();
    }

    private void HandleSessionFinished(
        FocusSessionResult result,
        RewardResultData rewardResult
    )
    {
        if (stopMusicOnSessionEnd)
            StopFocusMusic();
    }

    public void RegisterTracks(List<FocusMusicTrack> newTracks)
    {
        if (newTracks == null)
            return;

        foreach (FocusMusicTrack newTrack in newTracks)
        {
            if (newTrack == null)
                continue;

            if (string.IsNullOrEmpty(newTrack.trackId))
                continue;

            int index = tracks.FindIndex(track =>
                track != null && track.trackId == newTrack.trackId
            );

            if (index >= 0)
                tracks[index] = newTrack;
            else
                tracks.Add(newTrack);
        }

        EnsureDefaultUnlockedTracks();
        RefreshUnlockedTracksFromCurrentProgress();
        TryPlayIfSessionRunning();

        Debug.Log($"[FocusMusicPlayer] 음악 등록 완료 / 전체 {tracks.Count}개");
    }

    public void PlayFocusMusic()
    {
        FocusMusicTrack track = GetNextPlayableTrack();

        if (track == null)
        {
            Debug.LogWarning("[FocusMusicPlayer] 재생 가능한 음악이 없습니다.", this);
            return;
        }

        PlayFocusMusic(track.trackId);
    }

    public void PlayFocusMusic(string trackId)
    {
        FocusMusicTrack track = GetTrackById(trackId);

        if (track == null)
        {
            Debug.LogWarning($"[FocusMusicPlayer] trackId '{trackId}' 음악을 찾지 못했습니다.", this);
            PlayFocusMusic();
            return;
        }

        if (!IsTrackUnlocked(track.trackId))
        {
            Debug.LogWarning($"[FocusMusicPlayer] 아직 해금되지 않은 음악입니다: {track.displayName}", this);
            PlayFocusMusic();
            return;
        }

        if (track.clip == null)
        {
            Debug.LogWarning($"[FocusMusicPlayer] AudioClip이 없습니다: {track.displayName}", this);
            return;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.clip = track.clip;
        audioSource.loop = track.loop;
        audioSource.volume = volume;
        audioSource.Play();

        CurrentTrack = track;
        lastTrackIndex = tracks.IndexOf(track);

        Debug.Log($"[FocusMusicPlayer] 음악 재생: {track.displayName}");
        OnMusicStarted?.Invoke(track);
    }

    public void StopFocusMusic()
    {
        if (audioSource != null)
            audioSource.Stop();

        CurrentTrack = null;

        Debug.Log("[FocusMusicPlayer] 음악 정지");
        OnMusicStopped?.Invoke();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
            audioSource.volume = volume;
    }

    public void RefreshUnlockedTracksFromCurrentProgress()
    {
        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (unlockManager == null)
            unlockManager = FindAnyObjectByType<UnlockManager>();

        int progress = unlockManager != null ? unlockManager.totalProgress : 0;

        RefreshUnlockedTracksFromProgress(progress);
    }

    public List<FocusMusicTrack> RefreshUnlockedTracksFromProgress(int totalUnlockProgress)
    {
        List<FocusMusicTrack> newlyUnlockedTracks = new List<FocusMusicTrack>();

        foreach (FocusMusicTrack track in tracks)
        {
            if (track == null)
                continue;

            if (string.IsNullOrEmpty(track.trackId))
                continue;

            bool shouldUnlock =
                track.unlockedByDefault ||
                totalUnlockProgress >= track.requiredUnlockProgress;

            if (!shouldUnlock)
                continue;

            if (!unlockedTrackIds.Contains(track.trackId))
            {
                unlockedTrackIds.Add(track.trackId);
                newlyUnlockedTracks.Add(track);

                Debug.Log($"[FocusMusicPlayer] 음악 해금: {track.displayName}");
                OnTrackUnlocked?.Invoke(track);
            }
        }

        return newlyUnlockedTracks;
    }

    public bool UnlockTrack(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
            return false;

        FocusMusicTrack track = GetTrackById(trackId);

        if (track == null)
            return false;

        if (unlockedTrackIds.Contains(trackId))
            return false;

        unlockedTrackIds.Add(trackId);

        Debug.Log($"[FocusMusicPlayer] 음악 수동 해금: {track.displayName}");
        OnTrackUnlocked?.Invoke(track);

        return true;
    }

    public bool IsTrackUnlocked(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
            return false;

        return unlockedTrackIds.Contains(trackId);
    }

    public FocusMusicTrack GetTrackById(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
            return null;

        foreach (FocusMusicTrack track in tracks)
        {
            if (track == null)
                continue;

            if (track.trackId == trackId)
                return track;
        }

        return null;
    }

    public List<FocusMusicTrack> GetUnlockedTracks()
    {
        List<FocusMusicTrack> result = new List<FocusMusicTrack>();

        foreach (FocusMusicTrack track in tracks)
        {
            if (track == null)
                continue;

            if (IsTrackUnlocked(track.trackId))
                result.Add(track);
        }

        return result;
    }

    public List<string> CaptureUnlockedTrackIds()
    {
        return new List<string>(unlockedTrackIds);
    }

    public void ApplyUnlockedTrackIds(List<string> loadedTrackIds)
    {
        unlockedTrackIds.Clear();

        if (loadedTrackIds != null)
        {
            foreach (string trackId in loadedTrackIds)
            {
                if (string.IsNullOrEmpty(trackId))
                    continue;

                if (!unlockedTrackIds.Contains(trackId))
                    unlockedTrackIds.Add(trackId);
            }
        }

        EnsureDefaultUnlockedTracks();
        RefreshUnlockedTracksFromCurrentProgress();

        Debug.Log($"[FocusMusicPlayer] 해금 음악 불러오기 완료 / {unlockedTrackIds.Count}개");
    }

    private void EnsureDefaultUnlockedTracks()
    {
        foreach (FocusMusicTrack track in tracks)
        {
            if (track == null)
                continue;

            if (string.IsNullOrEmpty(track.trackId))
                continue;

            if (!track.unlockedByDefault)
                continue;

            if (!unlockedTrackIds.Contains(track.trackId))
                unlockedTrackIds.Add(track.trackId);
        }
    }

    private FocusMusicTrack GetNextPlayableTrack()
    {
        List<FocusMusicTrack> unlockedTracks = GetUnlockedTracks();

        unlockedTracks.RemoveAll(track => track == null || track.clip == null);

        if (unlockedTracks.Count == 0)
            return null;

        if (playRandomTrack)
        {
            int randomIndex = UnityEngine.Random.Range(0, unlockedTracks.Count);
            return unlockedTracks[randomIndex];
        }

        int startIndex = Mathf.Max(0, lastTrackIndex + 1);

        for (int i = 0; i < tracks.Count; i++)
        {
            int index = (startIndex + i) % tracks.Count;
            FocusMusicTrack track = tracks[index];

            if (track == null)
                continue;

            if (track.clip == null)
                continue;

            if (IsTrackUnlocked(track.trackId))
                return track;
        }

        return unlockedTracks[0];
    }

    public void TryPlayIfSessionRunning()
    {
        if (IsPlaying)
            return;

        if (focusSessionService == null)
            focusSessionService = FocusSessionService.Instance;

        if (focusSessionService == null)
            focusSessionService = FindAnyObjectByType<FocusSessionService>();

        if (focusSessionService != null && focusSessionService.IsRunning)
            PlayFocusMusic();
    }

    [ContextMenu("Test Play Music")]
    public void TestPlayMusic()
    {
        PlayFocusMusic();
    }

    [ContextMenu("Test Stop Music")]
    public void TestStopMusic()
    {
        StopFocusMusic();
    }

    [ContextMenu("Test Refresh Unlocks")]
    public void TestRefreshUnlocks()
    {
        RefreshUnlockedTracksFromCurrentProgress();
    }
}