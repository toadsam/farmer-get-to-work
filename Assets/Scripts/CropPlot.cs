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