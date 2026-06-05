using System;
using System.Collections.Generic;
using UnityEngine;

public class SessionRecordManager : MonoBehaviour
{
    public static SessionRecordManager Instance { get; private set; }

    [Header("References")]
    public FocusSessionService focusSessionService;

    [Header("Records")]
    public List<FocusSessionRecordData> records = new List<FocusSessionRecordData>();

    [Header("Limit")]
    [Tooltip("기록이 너무 많아지는 것을 막기 위한 최대 보관 개수입니다.")]
    public int maxRecordCount = 500;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BindFocusSessionService();
    }

    private void OnDestroy()
    {
        UnbindFocusSessionService();
    }

    private void BindFocusSessionService()
    {
        if (focusSessionService == null)
            focusSessionService = FindAnyObjectByType<FocusSessionService>();

        if (focusSessionService == null)
        {
            Debug.LogWarning("[SessionRecord] FocusSessionService를 찾지 못했습니다.", this);
            return;
        }

        focusSessionService.OnSessionFinished -= HandleSessionFinished;
        focusSessionService.OnSessionFinished += HandleSessionFinished;

        Debug.Log("[SessionRecord] FocusSessionService 이벤트 연결 완료");
    }

    private void UnbindFocusSessionService()
    {
        if (focusSessionService == null)
            return;

        focusSessionService.OnSessionFinished -= HandleSessionFinished;
    }

    private void HandleSessionFinished(
        FocusSessionResult result,
        RewardResultData rewardResult
    )
    {
        AddRecordFromResult(result, rewardResult);
    }

    public FocusSessionRecordData AddRecordFromResult(
        FocusSessionResult result,
        RewardResultData rewardResult
    )
    {
        FocusSessionRecordData record =
            FocusSessionRecordData.FromResult(result, rewardResult);

        if (record == null)
        {
            Debug.LogWarning("[SessionRecord] 추가할 기록이 없습니다.", this);
            return null;
        }

        AddRecord(record);
        return record;
    }

    public void AddRecord(FocusSessionRecordData record)
    {
        if (record == null)
            return;

        if (string.IsNullOrEmpty(record.recordId))
            record.recordId = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(record.dateKey))
        {
            DateTime endedAt = GameDataUtility.FromTicks(record.endedAtTicks);
            if (endedAt == DateTime.MinValue)
                endedAt = DateTime.Now;

            record.dateKey = GameDataUtility.ToDateKey(endedAt);
        }

        records.Add(record);
        TrimRecordsIfNeeded();

        Debug.Log(
            $"[SessionRecord] 기록 추가 / {record.dateKey}, {record.goalName}, " +
            $"성공 {record.success}, 집중 {record.focusedMinutes}분"
        );
    }

    private void TrimRecordsIfNeeded()
    {
        if (maxRecordCount <= 0)
            return;

        while (records.Count > maxRecordCount)
        {
            records.RemoveAt(0);
        }
    }

    public List<FocusSessionRecordData> GetAllRecords()
    {
        return new List<FocusSessionRecordData>(records);
    }

    public List<FocusSessionRecordData> GetRecentRecords(int count)
    {
        List<FocusSessionRecordData> result = new List<FocusSessionRecordData>();

        if (count <= 0)
            return result;

        int startIndex = Mathf.Max(0, records.Count - count);

        for (int i = records.Count - 1; i >= startIndex; i--)
        {
            result.Add(records[i]);
        }

        return result;
    }

    public List<FocusSessionRecordData> GetRecordsByDateKey(string dateKey)
    {
        List<FocusSessionRecordData> result = new List<FocusSessionRecordData>();

        if (string.IsNullOrEmpty(dateKey))
            return result;

        foreach (FocusSessionRecordData record in records)
        {
            if (record == null)
                continue;

            if (record.dateKey == dateKey)
                result.Add(record);
        }

        return result;
    }

    public List<FocusSessionRecordData> GetTodayRecords()
    {
        return GetRecordsByDateKey(GameDataUtility.ToDateKey(DateTime.Now));
    }

    public SessionSummaryData GetTodaySummary()
    {
        return CreateSummary(
            GameDataUtility.ToDateKey(DateTime.Now),
            GetTodayRecords()
        );
    }

    public List<SessionSummaryData> GetRecentDaySummaries(int dayCount)
    {
        List<SessionSummaryData> summaries = new List<SessionSummaryData>();

        if (dayCount <= 0)
            return summaries;

        DateTime today = DateTime.Today;

        for (int i = dayCount - 1; i >= 0; i--)
        {
            DateTime date = today.AddDays(-i);
            string dateKey = GameDataUtility.ToDateKey(date);
            List<FocusSessionRecordData> dayRecords = GetRecordsByDateKey(dateKey);

            summaries.Add(CreateSummary(dateKey, dayRecords));
        }

        return summaries;
    }

    private SessionSummaryData CreateSummary(
        string dateKey,
        List<FocusSessionRecordData> targetRecords
    )
    {
        SessionSummaryData summary = new SessionSummaryData();
        summary.dateKey = dateKey;

        if (targetRecords == null)
            return summary;

        foreach (FocusSessionRecordData record in targetRecords)
        {
            if (record == null)
                continue;

            summary.totalSessionCount++;

            if (record.success)
                summary.successSessionCount++;
            else
                summary.failSessionCount++;

            summary.totalPlannedMinutes += record.plannedMinutes;
            summary.totalFocusedMinutes += record.focusedMinutes;

            summary.totalExitCount += record.exitCount;
            summary.totalExitSeconds += record.totalExitSeconds;

            summary.totalRewardGrowth += record.rewardGrowth;
            summary.totalRewardUnlockProgress += record.rewardUnlockProgress;
            summary.totalRewardGold += record.rewardGold;
        }

        if (summary.totalSessionCount > 0)
        {
            summary.successRate =
                (float)summary.successSessionCount / summary.totalSessionCount;
        }

        return summary;
    }

    public int GetTotalFocusedMinutes()
    {
        int total = 0;

        foreach (FocusSessionRecordData record in records)
        {
            if (record == null)
                continue;

            total += record.focusedMinutes;
        }

        return total;
    }

    public int GetTotalSuccessCount()
    {
        int count = 0;

        foreach (FocusSessionRecordData record in records)
        {
            if (record != null && record.success)
                count++;
        }

        return count;
    }

    public int GetTotalFailCount()
    {
        int count = 0;

        foreach (FocusSessionRecordData record in records)
        {
            if (record != null && !record.success)
                count++;
        }

        return count;
    }

    public void SetRecords(List<FocusSessionRecordData> loadedRecords)
    {
        records.Clear();

        if (loadedRecords != null)
            records.AddRange(loadedRecords);

        TrimRecordsIfNeeded();

        Debug.Log($"[SessionRecord] 기록 불러오기 완료 / {records.Count}개");
    }

    public void ClearRecords()
    {
        records.Clear();
        Debug.Log("[SessionRecord] 모든 기록 삭제");
    }

    [ContextMenu("Test Add Success Record")]
    public void TestAddSuccessRecord()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 30,
            focusedMinutes = 30,
            success = true,
            exitCount = 0,
            totalExitSeconds = 0f,
            startedAt = DateTime.Now.AddMinutes(-30),
            endedAt = DateTime.Now
        };

        RewardResultData reward = RewardResultData.CreateSuccess(
            focusedMinutes: 30,
            rewardGrowth: 120,
            rewardUnlockProgress: 60,
            rewardGold: 0,
            hasExitPenalty: false,
            rewardMultiplier: 1f
        );

        AddRecordFromResult(result, reward);
    }

    [ContextMenu("Test Add Fail Record")]
    public void TestAddFailRecord()
    {
        FocusSessionResult result = new FocusSessionResult
        {
            goalType = "Study",
            goalName = "공부하기",
            plannedMinutes = 30,
            focusedMinutes = 8,
            success = false,
            exitCount = 1,
            totalExitSeconds = 190f,
            startedAt = DateTime.Now.AddMinutes(-8),
            endedAt = DateTime.Now
        };

        RewardResultData reward = RewardResultData.CreateFailure(
            reasonCode: "LongExit",
            reasonMessage: "앱 이탈 시간이 길어 집중 흐름이 끊겼습니다.",
            focusedMinutes: 8
        );

        AddRecordFromResult(result, reward);
    }

    [ContextMenu("Test Print Today Summary")]
    public void TestPrintTodaySummary()
    {
        SessionSummaryData summary = GetTodaySummary();

        Debug.Log(
            $"[SessionRecord] 오늘 요약 / 세션 {summary.totalSessionCount}회, " +
            $"성공 {summary.successSessionCount}회, 실패 {summary.failSessionCount}회, " +
            $"집중 {summary.totalFocusedMinutes}분, 이탈 {summary.totalExitCount}회, " +
            $"이탈 시간 {summary.totalExitSeconds:F1}초, 성공률 {summary.successRate:P0}"
        );
    }

    [ContextMenu("Test Clear Records")]
    public void TestClearRecords()
    {
        ClearRecords();
    }
}

[Serializable]
public class SessionSummaryData
{
    public string dateKey;

    public int totalSessionCount;
    public int successSessionCount;
    public int failSessionCount;

    public int totalPlannedMinutes;
    public int totalFocusedMinutes;

    public int totalExitCount;
    public float totalExitSeconds;

    public int totalRewardGrowth;
    public int totalRewardUnlockProgress;
    public int totalRewardGold;

    public float successRate;
}