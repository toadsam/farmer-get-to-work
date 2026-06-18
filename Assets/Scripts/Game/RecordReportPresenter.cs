using UnityEngine;
public class RecordReportPresenter : MonoBehaviour
{
    public TMPro.TMP_Text todayText;
    public TMPro.TMP_Text recentText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        SessionRecordManager manager = SessionRecordManager.Instance;

        if (manager == null)
            return;

        SessionSummaryData today = manager.GetTodaySummary();
        todayText.text = BuildSummaryText("오늘", today);

        var recentSummaries = manager.GetRecentDaySummaries(7);

        int totalFocused = 0;
        int totalSuccess = 0;
        int totalFail = 0;
        float totalExit = 0f;
        int moodCount = 0;
        float moodSum = 0f;
        int urgeCount = 0;
        float urgeSum = 0f;

        foreach (var summary in recentSummaries)
        {
            totalFocused += summary.totalFocusedMinutes;
            totalSuccess += summary.successSessionCount;
            totalFail += summary.failSessionCount;
            totalExit += summary.totalExitSeconds;

            if (summary.moodRecordCount > 0)
            {
                moodSum += summary.averageMoodDelta;
                moodCount++;
            }

            if (summary.phoneUrgeRecordCount > 0)
            {
                urgeSum += summary.averagePhoneUrgeDelta;
                urgeCount++;
            }
        }

        float avgMood = moodCount > 0 ? moodSum / moodCount : 0f;
        float avgUrge = urgeCount > 0 ? urgeSum / urgeCount : 0f;

        recentText.text =
            $"최근 7일\n" +
            $"총 집중 시간: {totalFocused}분\n" +
            $"성공: {totalSuccess}회 / 실패: {totalFail}회\n" +
            $"총 이탈 시간: {totalExit:F1}초\n" +
            $"평균 기분 변화: {avgMood:+0.0;-0.0;0.0}\n" +
            $"평균 스마트폰 욕구 감소: {avgUrge:+0.0;-0.0;0.0}";
    }

    private string BuildSummaryText(string title, SessionSummaryData summary)
    {
        if (summary == null)
            return $"{title}\n기록이 없습니다.";

        return
            $"{title}\n" +
            $"집중 시간: {summary.totalFocusedMinutes}분\n" +
            $"성공률: {summary.successRate:P0}\n" +
            $"이탈: {summary.totalExitCount}회 / {summary.totalExitSeconds:F1}초\n" +
            $"기분 변화: {summary.averageMoodDelta:+0.0;-0.0;0.0}\n" +
            $"스마트폰 욕구 감소: {summary.averagePhoneUrgeDelta:+0.0;-0.0;0.0}\n" +
            $"음악 도움 평균: {summary.averageMusicHelpedLevel:0.0}/5";
    }
}