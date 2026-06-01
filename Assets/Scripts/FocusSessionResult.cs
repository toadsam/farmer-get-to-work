using System;

[Serializable]
public class FocusSessionResult
{
    public string goalType;
    public string goalName;

    public int plannedMinutes;
    public int focusedMinutes;

    public bool success;

    public int exitCount;
    public float totalExitSeconds;

    public DateTime startedAt;
    public DateTime endedAt;
}