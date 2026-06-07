using System;
using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    [Header("Progress")]
    public int totalProgress;

    [Header("Unlock Entries")]
    public List<UnlockEntry> unlockEntries = new List<UnlockEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ApplyAllVisualStates();
    }

    public void AddProgress(int amount)
    {
        if (amount <= 0)
            return;

        totalProgress += amount;

        Debug.Log($"[UnlockManager] 해금 진행도 +{amount} / 현재 {totalProgress}");

        CheckUnlocks();
        ApplyAllVisualStates();
    }

    private void CheckUnlocks()
    {
        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null)
                continue;

            if (entry.unlocked)
                continue;

            if (totalProgress >= entry.requiredProgress)
            {
                entry.unlocked = true;
                Debug.Log($"[UnlockManager] 해금 완료: {entry.displayName}");
            }
        }
    }

    public UnlockEntry GetNextLockedEntry()
    {
        UnlockEntry next = null;

        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null || entry.unlocked)
                continue;

            if (next == null || entry.requiredProgress < next.requiredProgress)
                next = entry;
        }

        return next;
    }

    public void ApplyAllVisualStates()
    {
        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null)
                continue;

            entry.ApplyVisualState();
        }
    }

    public void SetProgress(int progress, bool recalculateUnlocks = true)
    {
        totalProgress = Mathf.Max(0, progress);

        if (recalculateUnlocks)
            RecalculateUnlocksFromProgress();

        ApplyAllVisualStates();

        Debug.Log($"[UnlockManager] 진행도 불러오기 완료 / {totalProgress}");
    }

    public void RecalculateUnlocksFromProgress()
    {
        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null)
                continue;

            entry.unlocked = totalProgress >= entry.requiredProgress;
        }
    }

    public void ResetProgressForNewGame()
    {
        totalProgress = 0;

        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null)
                continue;

            entry.unlocked = false;
        }

        ApplyAllVisualStates();

        Debug.Log("[UnlockManager] 해금 진행도 초기화 완료");
    }

    [ContextMenu("Test Add Progress +20")]
    public void TestAddProgress20()
    {
        AddProgress(20);
    }

    [ContextMenu("Test Add Progress +60")]
    public void TestAddProgress60()
    {
        AddProgress(60);
    }

    [ContextMenu("Test Reset Unlocks")]
    public void TestResetUnlocks()
    {
        totalProgress = 0;

        foreach (UnlockEntry entry in unlockEntries)
        {
            if (entry == null)
                continue;

            entry.unlocked = false;
        }

        ApplyAllVisualStates();

        Debug.Log("[UnlockManager] 테스트용 해금 상태 초기화");
    }
}

[Serializable]
public class UnlockEntry
{
    public string unlockId;
    public string displayName;

    [Tooltip("이 수치 이상이 되면 해금됩니다.")]
    public int requiredProgress = 100;

    public bool unlocked;

    [Header("Optional Visual Objects")]
    [Tooltip("해금되었을 때 활성화할 오브젝트입니다.")]
    public GameObject objectToActivateOnUnlock;

    [Tooltip("해금되기 전까지 보여줄 잠금 표시 오브젝트입니다.")]
    public GameObject objectToDeactivateOnUnlock;

    public void ApplyVisualState()
    {
        if (objectToActivateOnUnlock != null)
            objectToActivateOnUnlock.SetActive(unlocked);

        if (objectToDeactivateOnUnlock != null)
            objectToDeactivateOnUnlock.SetActive(!unlocked);
    }
}