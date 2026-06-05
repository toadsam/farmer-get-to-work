using System.Collections.Generic;
using UnityEngine;

public class IslandSetManager : MonoBehaviour
{
    public static IslandSetManager Instance { get; private set; }

    [Header("References")]
    public FarmManager farmManager;
    public UnlockManager unlockManager;
    public SaveSystem saveSystem;

    [Header("Island Sets")]
    public List<IslandSetController> islandSets = new List<IslandSetController>();

    [Header("Auto")]
    public bool autoCollectIslandSetsOnAwake = true;
    public bool refreshUnlocksOnStart = true;
    public bool autoSaveAfterActivation = true;

    private void Awake()
    {
        Instance = this;

        RefreshReferences();

        if (autoCollectIslandSetsOnAwake)
            CollectIslandSetsFromChildren();
    }

    private void Start()
    {
        RefreshReferences();

        if (refreshUnlocksOnStart)
            RefreshUnlocksFromCurrentProgress();

        ApplyAllVisualStates();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshReferences()
    {
        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (saveSystem == null)
            saveSystem = SaveSystem.Instance;
    }

    public void CollectIslandSetsFromChildren()
    {
        islandSets.Clear();

        IslandSetController[] foundSets =
            GetComponentsInChildren<IslandSetController>(true);

        foreach (IslandSetController islandSet in foundSets)
        {
            if (islandSet == null)
                continue;

            if (!islandSets.Contains(islandSet))
                islandSets.Add(islandSet);
        }

        Debug.Log($"[IslandSetManager] 섬 세트 수집 완료 / {islandSets.Count}개");
    }

    public int GetCurrentUnlockProgress()
    {
        RefreshReferences();

        if (unlockManager != null)
            return unlockManager.totalProgress;

        if (saveSystem != null &&
            saveSystem.currentSaveData != null)
            return saveSystem.currentSaveData.totalUnlockProgress;

        return 0;
    }

    public void RefreshUnlocksFromCurrentProgress()
    {
        RefreshUnlocksFromProgress(GetCurrentUnlockProgress());
    }

    public List<IslandSetController> RefreshUnlocksFromProgress(int totalUnlockProgress)
    {
        List<IslandSetController> newlyUnlockedIslands = new List<IslandSetController>();

        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            bool newlyUnlocked = islandSet.RefreshUnlockByProgress(totalUnlockProgress);

            if (newlyUnlocked)
                newlyUnlockedIslands.Add(islandSet);
        }

        return newlyUnlockedIslands;
    }

    public bool TryActivateIsland(string islandId)
    {
        RefreshReferences();

        IslandSetController islandSet = GetIslandSetById(islandId);

        if (islandSet == null)
        {
            Debug.LogWarning($"[IslandSetManager] islandId '{islandId}' 섬을 찾지 못했습니다.", this);
            return false;
        }

        bool success = islandSet.TryActivate(farmManager);

        if (success)
        {
            islandSet.ApplyVisualState();

            if (autoSaveAfterActivation && saveSystem != null)
                saveSystem.SaveGame();
        }

        return success;
    }

    public IslandSetController GetIslandSetById(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
            return null;

        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            if (islandSet.islandId == islandId)
                return islandSet;
        }

        return null;
    }

    public IslandSetController GetFirstUnlockedEmptyIsland()
    {
        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            if (islandSet.unlocked && !islandSet.activated)
                return islandSet;
        }

        return null;
    }

    public List<IslandStateData> CaptureIslandStates()
    {
        List<IslandStateData> result = new List<IslandStateData>();

        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            result.Add(islandSet.CaptureStateData());
        }

        return result;
    }

    public void ApplyIslandStates(List<IslandStateData> savedIslandStates)
    {
        RefreshReferences();

        if (savedIslandStates == null)
        {
            ApplyAllVisualStates();
            return;
        }

        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            IslandStateData data = savedIslandStates.Find(
                item => item != null && item.islandId == islandSet.islandId
            );

            if (data != null)
                islandSet.ApplyStateData(data, farmManager);
        }

        RefreshUnlocksFromCurrentProgress();
        ApplyAllVisualStates();

        Debug.Log("[IslandSetManager] 섬 상태 불러오기 완료");
    }

    public void AddGrowthToActivatedIslandCrops(int amount)
    {
        if (amount <= 0)
            return;

        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            islandSet.AddGrowthToIslandCrops(amount);
        }
    }

    public void ApplyAllVisualStates()
    {
        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            islandSet.ApplyVisualState();
        }
    }

    public void ResetAllIslandsForNewGame()
    {
        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            islandSet.ResetToInitialState();
        }

        Debug.Log("[IslandSetManager] 모든 추가 섬 초기화 완료");
    }

    [ContextMenu("Test Refresh Unlocks")]
    public void TestRefreshUnlocks()
    {
        RefreshUnlocksFromCurrentProgress();
    }

    [ContextMenu("Test Activate First Unlocked Empty Island")]
    public void TestActivateFirstUnlockedEmptyIsland()
    {
        IslandSetController islandSet = GetFirstUnlockedEmptyIsland();

        if (islandSet == null)
        {
            Debug.Log("[IslandSetManager] 활성화 가능한 빈 섬이 없습니다.");
            return;
        }

        TryActivateIsland(islandSet.islandId);
    }

    [ContextMenu("Test Add Growth To Activated Islands +50")]
    public void TestAddGrowthToActivatedIslands50()
    {
        AddGrowthToActivatedIslandCrops(50);
    }

    [ContextMenu("Test Print Island States")]
    public void TestPrintIslandStates()
    {
        foreach (IslandSetController islandSet in islandSets)
        {
            if (islandSet == null)
                continue;

            Debug.Log(
                $"[IslandSetManager] {islandSet.displayName} / " +
                $"ID: {islandSet.islandId}, " +
                $"State: {islandSet.CurrentState}, " +
                $"Required: {islandSet.requiredUnlockProgress}, " +
                $"Cost: {islandSet.activationGoldCost}"
            );
        }
    }
}