using UnityEngine;

public class AppFocusTracker : MonoBehaviour
{
    [Header("Editor Test")]
    [Tooltip("에디터에서 창 포커스를 잃었을 때도 이탈로 기록할지 여부입니다. 실제 Android 테스트 전에는 꺼두는 것을 권장합니다.")]
    public bool trackInEditor = false;

    public bool IsTracking { get; private set; }
    public int ExitCount { get; private set; }
    public float TotalExitSeconds { get; private set; }

    private bool isOutOfApp;
    private float exitStartRealtime;

    public void ResetAndStartTracking()
    {
        ExitCount = 0;
        TotalExitSeconds = 0f;
        isOutOfApp = false;
        IsTracking = true;

        Debug.Log("[AppFocusTracker] 이탈 감지 시작");
    }

    public void StopTracking()
    {
        FinalizeCurrentExitIfNeeded();
        IsTracking = false;

        Debug.Log($"[AppFocusTracker] 이탈 감지 종료 / 횟수: {ExitCount}, 총 시간: {TotalExitSeconds:F1}초");
    }

    public float GetTotalExitSeconds()
    {
        if (isOutOfApp)
        {
            return TotalExitSeconds + (Time.realtimeSinceStartup - exitStartRealtime);
        }

        return TotalExitSeconds;
    }

    public int GetExitCount()
    {
        return ExitCount;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_EDITOR
        if (!trackInEditor)
            return;
#endif

        if (!IsTracking)
            return;

        if (!hasFocus)
            BeginExit();
        else
            EndExit();
    }

    private void OnApplicationPause(bool pause)
    {
#if UNITY_EDITOR
        if (!trackInEditor)
            return;
#endif

        if (!IsTracking)
            return;

        if (pause)
            BeginExit();
        else
            EndExit();
    }

    private void BeginExit()
    {
        if (isOutOfApp)
            return;

        isOutOfApp = true;
        ExitCount++;
        exitStartRealtime = Time.realtimeSinceStartup;

        Debug.Log("[AppFocusTracker] 앱 이탈 감지");
    }

    private void EndExit()
    {
        if (!isOutOfApp)
            return;

        float duration = Time.realtimeSinceStartup - exitStartRealtime;
        TotalExitSeconds += duration;
        isOutOfApp = false;

        Debug.Log($"[AppFocusTracker] 앱 복귀 / 이탈 시간: {duration:F1}초 / 누적: {TotalExitSeconds:F1}초");
    }

    private void FinalizeCurrentExitIfNeeded()
    {
        if (!isOutOfApp)
            return;

        EndExit();
    }

    public void DebugAddExitSeconds(float seconds)
    {
        if (!IsTracking)
        {
            Debug.LogWarning("[AppFocusTracker] 감지 중이 아닙니다. 세션 시작 후 테스트하세요.", this);
            return;
        }

        ExitCount++;
        TotalExitSeconds += seconds;

        Debug.Log($"[AppFocusTracker] 테스트 이탈 추가 / +{seconds:F1}초 / 횟수: {ExitCount}, 총 시간: {TotalExitSeconds:F1}초");
    }

    [ContextMenu("Debug Add Short Exit 45s")]
    public void DebugAddShortExit45s()
    {
        DebugAddExitSeconds(45f);
    }

    [ContextMenu("Debug Add Long Exit 200s")]
    public void DebugAddLongExit200s()
    {
        DebugAddExitSeconds(200f);
    }

    [ContextMenu("Debug Clear Exit Data")]
    public void DebugClearExitData()
    {
        ExitCount = 0;
        TotalExitSeconds = 0f;
        isOutOfApp = false;

        Debug.Log("[AppFocusTracker] 테스트 이탈 데이터 초기화");
    }
}
