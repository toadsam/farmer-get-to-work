using System.Collections.Generic;
using UnityEngine;

public class FocusMusicLibrary : MonoBehaviour
{
    [Header("Tracks")]
    public List<FocusMusicTrack> tracks = new List<FocusMusicTrack>();

    [Header("Register")]
    public bool registerOnStart = true;

    private void Start()
    {
        if (registerOnStart)
            Register();
    }

    [ContextMenu("Register Tracks")]
    public void Register()
    {
        FocusMusicPlayer player = FocusMusicPlayer.Instance;

        if (player == null)
            player = FindAnyObjectByType<FocusMusicPlayer>();

        if (player == null)
        {
            Debug.LogWarning("[FocusMusicLibrary] FocusMusicPlayer가 없습니다.", this);
            return;
        }

        player.RegisterTracks(tracks);

        Debug.Log($"[FocusMusicLibrary] 음악 라이브러리 등록 완료 / {tracks.Count}개");
    }
}