using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnlockPreviewEntry
{
    public string unlockId;
    public string displayName;
    public int requiredUnlockProgress;
}

[CreateAssetMenu(menuName = "Farm/Unlock Preview Database")]
public class UnlockPreviewDatabase : ScriptableObject
{
    public List<UnlockPreviewEntry> entries = new List<UnlockPreviewEntry>();

    public UnlockPreviewEntry FindNewlyUnlockedEntry(
        int beforeProgress,
        int afterProgress
    )
    {
        List<UnlockPreviewEntry> results =
            FindNewlyUnlockedEntries(beforeProgress, afterProgress);

        if (results.Count > 0)
            return results[0];

        return null;
    }

    public List<UnlockPreviewEntry> FindNewlyUnlockedEntries(
        int beforeProgress,
        int afterProgress
    )
    {
        List<UnlockPreviewEntry> results = new List<UnlockPreviewEntry>();

        foreach (UnlockPreviewEntry entry in entries)
        {
            if (entry == null)
                continue;

            bool wasLocked = beforeProgress < entry.requiredUnlockProgress;
            bool willUnlock = afterProgress >= entry.requiredUnlockProgress;

            if (wasLocked && willUnlock)
                results.Add(entry);
        }

        results.Sort((a, b) =>
            a.requiredUnlockProgress.CompareTo(b.requiredUnlockProgress)
        );

        return results;
    }
}