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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool SpendStamina(int amount)
    {
        if (stamina < amount)
        {
            Debug.Log("[FarmManager] 스태미너가 부족합니다.");
            return false;
        }

        stamina -= amount;
        Debug.Log($"[FarmManager] Stamina: {stamina}/{maxStamina}");
        return true;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[FarmManager] Gold: {gold}");
    }

    public void PlantToPlot(CropPlot plot, CropDefinition crop)
    {
        if (plot == null || crop == null)
            return;

        if (!plot.IsEmpty)
            return;

        if (!SpendStamina(1))
            return;

        plot.Plant(crop);
    }

    public void HarvestPlot(CropPlot plot)
    {
        if (plot == null || !plot.IsReady)
            return;

        if (!SpendStamina(1))
            return;

        int reward = plot.Harvest();
        AddGold(reward);
    }

    public void AddGrowthToAllCrops(int amount)
    {
        foreach (CropPlot plot in cropPlots)
        {
            plot.AddGrowth(amount);
        }
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