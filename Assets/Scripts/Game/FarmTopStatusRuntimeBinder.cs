using TMPro;
using UnityEngine;

public class FarmTopStatusRuntimeBinder : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text goldText;
    public TMP_Text staminaText;

    [Header("Format")]
    public string goldPrefix = "";
    public string goldSuffix = "";
    public bool useCommaForGold = true;

    public string staminaPrefix = "";
    public string staminaSuffix = "";

    [Header("Refresh")]
    public bool refreshInLateUpdate = true;
    public float refreshInterval = 0.2f;

    private FarmManager farmManager;
    private float refreshTimer;

    private void OnEnable()
    {
        BindFarmManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindFarmManager();
    }

    private void LateUpdate()
    {
        if (!refreshInLateUpdate)
            return;

        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;

        if (farmManager == null || farmManager != FarmManager.Instance)
            BindFarmManager();

        Refresh();
    }

    private void BindFarmManager()
    {
        UnbindFarmManager();

        farmManager = FarmManager.Instance;

        if (farmManager == null)
            farmManager = FindAnyObjectByType<FarmManager>();

        if (farmManager == null)
            return;

        farmManager.OnResourcesChanged += Refresh;
    }

    private void UnbindFarmManager()
    {
        if (farmManager == null)
            return;

        farmManager.OnResourcesChanged -= Refresh;
        farmManager = null;
    }

    public void Refresh()
    {
        if (farmManager == null)
            return;

        if (goldText != null)
        {
            string goldValue = useCommaForGold
                ? farmManager.gold.ToString("N0")
                : farmManager.gold.ToString();

            goldText.text = $"{goldPrefix}{goldValue}{goldSuffix}";
        }

        if (staminaText != null)
        {
            staminaText.text =
                $"{staminaPrefix}{farmManager.stamina}/{farmManager.maxStamina}{staminaSuffix}";
        }
    }

    [ContextMenu("Debug Refresh")]
    public void DebugRefresh()
    {
        BindFarmManager();
        Refresh();
    }
}