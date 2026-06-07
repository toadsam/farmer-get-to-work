using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }

    [Header("Resources")]
    public int gold = 0;
    public int stamina = 5;
    public int maxStamina = 5;

    [Header("Crops")]
    public CropDefinition defaultCrop;
    public List<CropPlot> cropPlots = new List<CropPlot>();

    [Header("Save / Load")]
    public string farmIslandId = "MainFarm";
    public string farmDisplayName = "메인 농장";

    [Header("Stamina Costs")]
    public int plantStaminaCost = 0;
    public int harvestStaminaCost = 1;
    public int hiddenInteractionStaminaCost = 1;
    public int specialInteractionStaminaCost = 1;

    [Tooltip("저장된 cropId로 작물을 다시 찾기 위한 목록입니다. 기본 작물도 넣어두는 것을 권장합니다.")]
    public List<CropDefinition> availableCrops = new List<CropDefinition>();

    public event Action OnResourcesChanged;
    public event Action<int> OnGoldChanged;
    public event Action<int, int> OnStaminaChanged;

    [Header("Auto Save")]
    public bool autoSaveAfterFarmAction = true;

    private void NotifyResourcesChanged()
    {
        OnGoldChanged?.Invoke(gold);
        OnStaminaChanged?.Invoke(stamina, maxStamina);
        OnResourcesChanged?.Invoke();
    }

    private void SaveAfterFarmAction()
    {
        if (!autoSaveAfterFarmAction)
            return;

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveGame();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool HasEnoughStamina(int amount)
    {
        if (amount <= 0)
            return true;

        return stamina >= amount;
    }

    public bool SpendStamina(int amount)
    {
        if (amount <= 0)
            return true;

        if (stamina < amount)
        {
            Debug.LogWarning($"[FarmManager] 스태미너 부족 / 필요 {amount}, 현재 {stamina}", this);
            return false;
        }

        stamina -= amount;

        NotifyResourcesChanged();

        Debug.Log($"[FarmManager] Stamina -{amount} / 현재 {stamina}/{maxStamina}");
        return true;
    }

    public void AddStamina(int amount)
    {
        if (amount <= 0)
            return;

        stamina = Mathf.Clamp(stamina + amount, 0, maxStamina);

        NotifyResourcesChanged();

        Debug.Log($"[FarmManager] Stamina +{amount} / 현재 {stamina}/{maxStamina}");
    }

    public void RefillStamina()
    {
        stamina = maxStamina;

        NotifyResourcesChanged();

        Debug.Log($"[FarmManager] 스태미너 회복 완료 / {stamina}/{maxStamina}");
    }

    public void AddGold(int amount)
    {
        if (amount == 0)
            return;

        gold = Mathf.Max(0, gold + amount);

        NotifyResourcesChanged();

        Debug.Log($"[FarmManager] Gold: {gold}");
    }

    public bool CanPlantToPlot(CropPlot plot, CropDefinition crop, out string reason)
    {
        if (plot == null)
        {
            reason = "밭 정보가 없습니다.";
            return false;
        }

        if (!plot.IsEmpty)
        {
            reason = "이미 작물이 심어져 있습니다.";
            return false;
        }

        if (crop == null)
        {
            reason = "심을 작물이 설정되어 있지 않습니다.";
            return false;
        }

        if (!HasEnoughStamina(plantStaminaCost))
        {
            reason = "스태미너가 부족합니다.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool PlantToPlot(CropPlot plot, CropDefinition crop)
    {
        if (!CanPlantToPlot(plot, crop, out string reason))
        {
            Debug.LogWarning($"[FarmManager] 작물 심기 실패: {reason}", this);
            return false;
        }

        if (!SpendStamina(plantStaminaCost))
            return false;

        plot.Plant(crop);

        NotifyResourcesChanged();
        SaveAfterFarmAction();

        Debug.Log($"[FarmManager] 작물 심기 완료: {crop.displayName}");
        return true;
    }

    public bool CanHarvestPlot(CropPlot plot, out string reason)
    {
        if (plot == null)
        {
            reason = "밭 정보가 없습니다.";
            return false;
        }

        if (!plot.IsReady)
        {
            reason = "아직 수확할 수 없습니다.";
            return false;
        }

        if (!HasEnoughStamina(harvestStaminaCost))
        {
            reason = "스태미너가 부족해서 수확할 수 없습니다.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool HarvestPlot(CropPlot plot)
    {
        if (!CanHarvestPlot(plot, out string reason))
        {
            Debug.LogWarning($"[FarmManager] 수확 실패: {reason}", this);
            return false;
        }

        if (!SpendStamina(harvestStaminaCost))
            return false;

        int earnedGold = plot.Harvest();

        if (earnedGold > 0)
            AddGold(earnedGold);

        SaveAfterFarmAction();

        Debug.Log($"[FarmManager] 수확 완료 / Gold +{earnedGold}");
        return true;
    }

    public void AddGrowthToAllCrops(int amount)
    {
        foreach (CropPlot plot in cropPlots)
        {
            plot.AddGrowth(amount);
        }
    }

    public void SetResources(int loadedGold, int loadedStamina, int loadedMaxStamina)
    {
        gold = Mathf.Max(0, loadedGold);
        maxStamina = Mathf.Max(1, loadedMaxStamina);
        stamina = Mathf.Clamp(loadedStamina, 0, maxStamina);

        NotifyResourcesChanged();

        Debug.Log($"[FarmManager] 리소스 불러오기 / Gold {gold}, Stamina {stamina}/{maxStamina}");
    }

    public CropDefinition FindCropById(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return null;

        if (defaultCrop != null && defaultCrop.cropId == cropId)
            return defaultCrop;

        foreach (CropDefinition crop in availableCrops)
        {
            if (crop == null)
                continue;

            if (crop.cropId == cropId)
                return crop;
        }

        Debug.LogWarning($"[FarmManager] cropId '{cropId}'에 해당하는 CropDefinition을 찾지 못했습니다.", this);
        return null;
    }

    public IslandStateData CaptureIslandStateData()
    {
        IslandStateData islandData = new IslandStateData
        {
            islandId = farmIslandId,
            displayName = farmDisplayName,
            unlocked = true,
            activated = true,
            level = 1,
            growthPoints = 0
        };

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            islandData.cropPlots.Add(plot.CaptureStateData());
        }

        return islandData;
    }

    public void ApplyIslandStateData(IslandStateData islandData)
    {
        if (islandData == null)
        {
            Debug.LogWarning("[FarmManager] 불러올 섬 데이터가 없습니다.", this);
            return;
        }

        if (islandData.cropPlots == null)
            return;

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            CropPlotStateData plotData = islandData.cropPlots.Find(
                data => data != null && data.plotId == plot.plotId
            );

            if (plotData == null)
                continue;

            CropDefinition crop = FindCropById(plotData.cropId);
            plot.ApplyStateData(plotData, crop);
        }

        Debug.Log($"[FarmManager] 농장 상태 불러오기 완료 / {islandData.displayName}");
    }

    public bool CanSpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        return gold >= amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (gold < amount)
        {
            Debug.LogWarning($"[FarmManager] 골드 부족 / 필요 {amount}, 현재 {gold}", this);
            return false;
        }

        gold -= amount;

        NotifyResourcesChanged();
        SaveAfterFarmAction();

        Debug.Log($"[FarmManager] Gold -{amount} / 현재 Gold {gold}");
        return true;
    }

    public void ResetFarmForNewGame()
    {
        gold = 0;
        maxStamina = Mathf.Max(1, maxStamina);
        stamina = maxStamina;

        foreach (CropPlot plot in cropPlots)
        {
            if (plot == null)
                continue;

            plot.ClearPlot();
        }

        Debug.Log("[FarmManager] 새 게임 상태로 초기화 완료");
    }

    [ContextMenu("Test Plant All Direct")]
    public void TestPlantAllDirect()
    {
        foreach (var plot in cropPlots)
        {
            if (plot != null && plot.IsEmpty)
                plot.Plant(defaultCrop);
        }

        Debug.Log("[Test] 모든 빈 밭에 작물을 심었습니다.");
    }

    [ContextMenu("Test Grow All +50")]
    public void TestGrowAll50()
    {
        AddGrowthToAllCrops(50);
        Debug.Log("[Test] 모든 작물 성장 +50");
    }

    [ContextMenu("Test Refill Stamina")]
    public void TestRefillStamina()
    {
        stamina = maxStamina;
        Debug.Log("[Test] 스태미너 회복");
    }

    [ContextMenu("Test Harvest Ready All")]
    public void TestHarvestReadyAll()
    {
        foreach (var plot in cropPlots)
        {
            if (plot != null && plot.IsReady)
                HarvestPlot(plot);
        }

        Debug.Log("[Test] 수확 가능한 작물을 수확했습니다.");
    }


}