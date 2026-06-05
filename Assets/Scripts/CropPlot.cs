using System;
using System.Collections.Generic;
using UnityEngine;

public enum CropPlotState
{
    Empty,
    Seed,
    Growing,
    Ready
}

public class CropPlot : MonoBehaviour
{
    [Header("Plot Info")]
    public string plotId;

    [Header("Crop State")]
    public CropDefinition currentCrop;
    public CropPlotState state = CropPlotState.Empty;
    public int growthPoints;

    [Header("Visual Roots")]
    public List<Transform> cropVisualRoots = new List<Transform>();

    [Header("Reward")]
    [Tooltip("체크하면 밭 안의 작물 슬롯 수만큼 보상을 지급합니다.")]
    public bool rewardBySlotCount = true;

    [Header("Planting")]
    public CropDefinition defaultCropOverride;

    public CropDefinition GetPlantCrop(CropDefinition fallbackCrop)
    {
        if (defaultCropOverride != null)
            return defaultCropOverride;

        return fallbackCrop;
    }

    private readonly List<GameObject> currentVisuals = new List<GameObject>();

    public bool IsEmpty => state == CropPlotState.Empty;
    public bool IsReady => state == CropPlotState.Ready;

    public int SlotCount
    {
        get
        {
            if (cropVisualRoots == null || cropVisualRoots.Count == 0)
                return 1;

            return cropVisualRoots.Count;
        }
    }

    public void Plant(CropDefinition crop)
    {
        if (!IsEmpty || crop == null)
            return;

        currentCrop = crop;
        growthPoints = 0;
        state = CropPlotState.Seed;

        RefreshVisuals();
    }

    public void AddGrowth(int amount)
    {
        if (currentCrop == null)
            return;

        if (state == CropPlotState.Empty || state == CropPlotState.Ready)
            return;

        growthPoints += amount;

        float ratio = (float)growthPoints / currentCrop.requiredGrowthPoints;

        if (ratio >= 1f)
        {
            growthPoints = currentCrop.requiredGrowthPoints;
            state = CropPlotState.Ready;
        }
        else if (ratio >= 0.5f)
        {
            state = CropPlotState.Growing;
        }
        else
        {
            state = CropPlotState.Seed;
        }

        RefreshVisuals();
    }

    public int Harvest()
    {
        if (!IsReady || currentCrop == null)
            return 0;

        int reward = currentCrop.sellGold;

        if (rewardBySlotCount)
            reward *= SlotCount;

        currentCrop = null;
        growthPoints = 0;
        state = CropPlotState.Empty;

        ClearVisuals();

        return reward;
    }

    public CropPlotStateData CaptureStateData()
    {
        return new CropPlotStateData
        {
            plotId = plotId,
            cropId = currentCrop != null ? currentCrop.cropId : "",
            state = state.ToString(),
            growthPoints = growthPoints
        };
    }

    public void ApplyStateData(CropPlotStateData data, CropDefinition cropDefinition)
    {
        if (data == null)
        {
            ClearPlot();
            return;
        }

        if (!Enum.TryParse(data.state, out CropPlotState loadedState))
            loadedState = CropPlotState.Empty;

        if (loadedState == CropPlotState.Empty || cropDefinition == null)
        {
            ClearPlot();
            return;
        }

        currentCrop = cropDefinition;
        state = loadedState;
        growthPoints = Mathf.Max(0, data.growthPoints);

        if (currentCrop != null)
        {
            int required = Mathf.Max(1, currentCrop.requiredGrowthPoints);
            growthPoints = Mathf.Clamp(growthPoints, 0, required);

            if (growthPoints >= required)
                state = CropPlotState.Ready;
        }

        RefreshVisuals();
    }

    public void ClearPlot()
    {
        currentCrop = null;
        growthPoints = 0;
        state = CropPlotState.Empty;

        ClearVisuals();
    }

    private void RefreshVisuals()
    {
        ClearVisuals();

        if (currentCrop == null || state == CropPlotState.Empty)
            return;

        GameObject prefab = GetPrefabByState();

        if (prefab == null)
        {
            Debug.LogWarning($"[CropPlot] {plotId} 단계 {state}에 해당하는 프리팹이 없습니다.", this);
            return;
        }

        if (cropVisualRoots == null || cropVisualRoots.Count == 0)
        {
            GameObject visual = Instantiate(prefab, transform.position + Vector3.up * 0.2f, transform.rotation, transform);
            currentVisuals.Add(visual);
            return;
        }

        foreach (Transform root in cropVisualRoots)
        {
            if (root == null)
                continue;

            GameObject visual = Instantiate(prefab, root.position, root.rotation, root);
            currentVisuals.Add(visual);
        }
    }

    private GameObject GetPrefabByState()
    {
        switch (state)
        {
            case CropPlotState.Seed:
                return currentCrop.seedPrefab;

            case CropPlotState.Growing:
                return currentCrop.growingPrefab;

            case CropPlotState.Ready:
                return currentCrop.readyPrefab;

            default:
                return null;
        }
    }

    private void ClearVisuals()
    {
        for (int i = currentVisuals.Count - 1; i >= 0; i--)
        {
            if (currentVisuals[i] != null)
                Destroy(currentVisuals[i]);
        }

        currentVisuals.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (cropVisualRoots == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (Transform root in cropVisualRoots)
        {
            if (root == null)
                continue;

            Gizmos.DrawSphere(root.position, 0.05f);
        }
    }
#endif
}