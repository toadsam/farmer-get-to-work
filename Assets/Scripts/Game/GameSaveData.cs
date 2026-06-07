using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 1;

    public long savedAtTicks;

    public int gold;
    public int stamina;
    public int maxStamina;

    public int totalUnlockProgress;

    public List<IslandStateData> islands = new List<IslandStateData>();
    public List<FocusSessionRecordData> sessionRecords = new List<FocusSessionRecordData>();

    public List<string> unlockedMusicTrackIds = new List<string>();
    public List<string> unlockedCollectionIds = new List<string>();

    public static GameSaveData CreateNew()
    {
        return new GameSaveData
        {
            saveVersion = 1,
            savedAtTicks = DateTime.Now.Ticks,
            gold = 0,
            stamina = 5,
            maxStamina = 5,
            totalUnlockProgress = 0,
            islands = new List<IslandStateData>(),
            sessionRecords = new List<FocusSessionRecordData>(),
            unlockedMusicTrackIds = new List<string>(),
            unlockedCollectionIds = new List<string>()
        };
    }
}