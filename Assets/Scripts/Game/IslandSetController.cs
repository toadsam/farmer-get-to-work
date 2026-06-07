using System;
using System.Collections.Generic;
using UnityEngine;

public enum IslandSetState
{
    Locked,
    UnlockedEmpty,
    Activated
}

public class IslandSetController : MonoBehaviour
{
    [Header("Island Info")]
    public string islandId = "Island_01";
    public string displayName = "새로운 섬";

    [Header("Unlock / Activation")]
    public int requiredUnlockProgress = 100;
    public int activationGoldCost = 100;

    [Header("Runtime State")]
    public bool unlocked;
    public bool activated;
    public int level = 1;
    public int growthPoints;

    [Header("Visual Roots")]
    [Tooltip("아직 해금되지 않았을 때 보일 오브젝트입니다.")]
    public GameObject lockedRoot;

    [Tooltip("해금은 되었지만 아직 골드로 활성화하지 않은 빈 섬 오브젝트입니다.")]
    public GameObject emptyRoot;

    [Tooltip("골드로 활성화된 뒤 보일 섬 전체 루트입니다.")]
    public GameObject activatedRoot;

    [Header("Activated Contents")]
    [Tooltip("섬 활성화 시 같이 켜질 밭, 동물, 건물, 장식 오브젝트들입니다.")]
    public List<GameObject> activationTargets = new List<GameObject>();

    [Header("Crop Plots In This Island")]
    [Tooltip("이 섬에 포함된 CropPlot들입니다. 추가 섬의 밭은 FarmManager가 아니라 여기에 넣는 것을 권장합니다.")]
    public List<CropPlot> cropPlots = new List<CropPlot>();

    [Header("Auto Collect")]
    public bool autoCollectCropPlotsOnAwake = true;

    public IslandSetState CurrentState
    {
        get
        {
            if (!unlocked)
                return IslandSetState.Locked;

            if (!activated)
                return IslandSetState.UnlockedEmpty;

            return IslandSetState.Activated;
        }
    }

    private void Awake()
    {
        if (autoCollectCropPlotsOnAwake)
            CollectCropPlotsFromChildren();

        ApplyVisualState();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(islandId))
            islandId = gameObject.name;
    }

    public void CollectCropPlotsFromChildren()
    {
        cropPlots.Clear();

        CropPlot[] plots = GetComponentsInChildren<CropPlot>(true);

        foreach (CropPlot plot in plots)
        {
            if (plot == null)
                continue;

            if (!cropPlots.Contains(plot))
                cropPlots.Add(plot);
        }
    }

    public bool RefreshUnlockByProgress(int totalUnlockProgress)
    {
        if (activated)
        {
            unlocked = true;
            ApplyVisualState();
            return false;
        }

        if (unlocked)
        {
            ApplyVisualState();
            return false;
        }

        if (totalUnlockProgress >= requiredUnlockProgress)
        {
            unlocked = true;
            ApplyVisualState();

            Debug.Log($"[IslandSet] 섬 해금: {displayName}");
            return true;
        }

        ApplyVisualState();
        return false;
    }

    public bool CanActivate(FarmManager farmManager, out string reason)
    {
        if (!unlocked)
        {
            reason = "아직 해금되지 않은 섬입니다.";
            return false;
        }

        if (activated)
        {
            reason = "이미 활성화된 섬입니다.";
            return false;
        }

        if (farmManager == null)
        {
            reason = "FarmManager가 없습니다.";
            return false;
        }

        if (farmManager.gold < activationGoldCost)
        {
            reason = $"골드가 부족합니다. 필요 골드: {activationGoldCost}";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryActivate(FarmManager farmManager)
    {
        if (!CanActivate(farmManager, out string reason))
        {
            Debug.LogWarning($"[IslandSet] 활성화 실패 / {displayName}: {reason}", this);
            return false;
        }

        farmManager.SpendGold(activationGoldCost);

        ActivateWithoutCost();

        Debug.Log($"[IslandSet] 섬 활성화 완료: {displayName}, Gold -{activationGoldCost}");
        return true;
    }

    public void ActivateWithoutCost()
    {
        unlocked = true;
        activated = true;

        ApplyVisualState();
    }

    public void ApplyVisualState()
    {
        IslandSetState state = CurrentState;

        if (lockedRoot != null)
            lockedRoot.SetActive(state == IslandSetState.Locked);

        if (emptyRoot != null)
            emptyRoot.SetActive(state == IslandSetState.UnlockedEmpty);

        if (activatedRoot != null)
            activatedRoot.SetActive(state == IslandSetState.Activated);

        foreach (GameObject target in activationTargets)
        {
            if (target == null)
                continue;

            target.SetActive(state == IslandSetState.Activated);
        }
    }

    public void AddGrowthToIslandCrops(int amount)
    {
        if (!activated)
            return;

        if (amount <= 0)
            return;

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            plot.AddGrowth(amount);
        }
    }

    public IslandStateData CaptureStateData()
    {
        IslandStateData data = new IslandStateData
        {
            islandId = islandId,
            displayName = displayName,
            unlocked = unlocked,
            activated = activated,
            level = level,
            growthPoints = growthPoints
        };

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            data.cropPlots.Add(plot.CaptureStateData());
        }

        return data;
    }

    public void ApplyStateData(IslandStateData data, FarmManager farmManager)
    {
        if (data == null)
        {
            ApplyVisualState();
            return;
        }

        unlocked = data.unlocked;
        activated = data.activated;
        level = Mathf.Max(1, data.level);
        growthPoints = Mathf.Max(0, data.growthPoints);

        if (data.cropPlots != null)
        {
            foreach (CropPlot plot in cropPlots)
            {
                if (plot == null)
                    continue;

                CropPlotStateData plotData = data.cropPlots.Find(
                    item => item != null && item.plotId == plot.plotId
                );

                if (plotData == null)
                    continue;

                CropDefinition crop = farmManager != null
                    ? farmManager.FindCropById(plotData.cropId)
                    : null;

                plot.ApplyStateData(plotData, crop);
            }
        }

        ApplyVisualState();
    }

    public void ResetToInitialState()
    {
        unlocked = false;
        activated = false;
        level = 1;
        growthPoints = 0;

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            plot.ClearPlot();
        }

        ApplyVisualState();

        Debug.Log($"[IslandSet] 초기화 완료: {displayName}");
    }

    [ContextMenu("Test Unlock")]
    public void TestUnlock()
    {
        unlocked = true;
        ApplyVisualState();
    }

    [ContextMenu("Test Activate Without Cost")]
    public void TestActivateWithoutCost()
    {
        ActivateWithoutCost();
    }

    [ContextMenu("Test Lock")]
    public void TestLock()
    {
        unlocked = false;
        activated = false;
        ApplyVisualState();
    }
}