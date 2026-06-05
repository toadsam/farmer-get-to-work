using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FarmHUD : MonoBehaviour
{
    [Header("References")]
    public FarmManager farmManager;
    public UnlockManager unlockManager;

    [Header("Resource UI")]
    public TMP_Text goldText;
    public TMP_Text staminaText;

    [Header("Crop UI")]
    public TMP_Text cropSummaryText;
    public TMP_Text cropDetailText;
    public Slider cropGrowthSlider;

    [Header("Unlock UI")]
    public TMP_Text unlockText;
    public Slider unlockProgressSlider;

    [Header("Refresh")]
    public float refreshInterval = 0.2f;

    private float refreshTimer;

    private void Awake()
    {
        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshHUD();
        }
    }

    public void RefreshHUD()
    {
        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (unlockManager == null)
            unlockManager = UnlockManager.Instance;

        if (farmManager != null)
        {
            RefreshResourceUI();
            RefreshCropUI();
        }

        if (unlockManager != null)
        {
            RefreshUnlockUI();
        }
    }

    private void RefreshResourceUI()
    {
        if (goldText != null)
            goldText.text = $"Gold  {farmManager.gold}";

        if (staminaText != null)
            staminaText.text = $"Stamina  {farmManager.stamina} / {farmManager.maxStamina}";
    }

    private void RefreshCropUI()
    {
        int emptyCount = 0;
        int seedCount = 0;
        int growingCount = 0;
        int readyCount = 0;

        int totalGrowth = 0;
        int totalRequired = 0;

        StringBuilder detailBuilder = new StringBuilder();

        foreach (CropPlot plot in farmManager.cropPlots)
        {
            if (plot == null)
                continue;

            switch (plot.state)
            {
                case CropPlotState.Empty:
                    emptyCount++;
                    detailBuilder.AppendLine($"{plot.plotId}: 빈 밭");
                    break;

                case CropPlotState.Seed:
                    seedCount++;
                    AppendPlotGrowthLine(plot, detailBuilder);
                    totalGrowth += plot.growthPoints;
                    totalRequired += GetRequiredGrowth(plot);
                    break;

                case CropPlotState.Growing:
                    growingCount++;
                    AppendPlotGrowthLine(plot, detailBuilder);
                    totalGrowth += plot.growthPoints;
                    totalRequired += GetRequiredGrowth(plot);
                    break;

                case CropPlotState.Ready:
                    readyCount++;
                    AppendPlotGrowthLine(plot, detailBuilder);
                    totalGrowth += plot.growthPoints;
                    totalRequired += GetRequiredGrowth(plot);
                    break;
            }
        }

        int activeCount = seedCount + growingCount + readyCount;

        if (cropSummaryText != null)
        {
            cropSummaryText.text =
                $"밭 상태  빈 밭 {emptyCount} / 성장 중 {seedCount + growingCount} / 수확 가능 {readyCount}";
        }

        if (cropDetailText != null)
        {
            if (activeCount == 0)
                cropDetailText.text = "작물이 심어진 밭이 없습니다.";
            else
                cropDetailText.text = detailBuilder.ToString();
        }

        if (cropGrowthSlider != null)
        {
            if (totalRequired <= 0)
                cropGrowthSlider.value = 0f;
            else
                cropGrowthSlider.value = Mathf.Clamp01((float)totalGrowth / totalRequired);
        }
    }

    private void RefreshUnlockUI()
    {
        UnlockEntry next = unlockManager.GetNextLockedEntry();

        if (next == null)
        {
            if (unlockText != null)
                unlockText.text = $"해금 진행도 {unlockManager.totalProgress} / 모든 기능 해금 완료";

            if (unlockProgressSlider != null)
                unlockProgressSlider.value = 1f;

            return;
        }

        int current = unlockManager.totalProgress;
        int required = Mathf.Max(1, next.requiredProgress);

        if (unlockText != null)
        {
            unlockText.text =
                $"해금 진행도 {current} / {required} - 다음: {next.displayName}";
        }

        if (unlockProgressSlider != null)
        {
            unlockProgressSlider.value = Mathf.Clamp01((float)current / required);
        }
    }

    private void AppendPlotGrowthLine(CropPlot plot, StringBuilder builder)
    {
        string cropName = "작물";

        if (plot.currentCrop != null && !string.IsNullOrEmpty(plot.currentCrop.displayName))
            cropName = plot.currentCrop.displayName;

        int required = GetRequiredGrowth(plot);

        builder.AppendLine(
            $"{plot.plotId}: {cropName} / {plot.state} / {plot.growthPoints} / {required}"
        );
    }

    private int GetRequiredGrowth(CropPlot plot)
    {
        if (plot == null || plot.currentCrop == null)
            return 0;

        return plot.currentCrop.requiredGrowthPoints;
    }
}