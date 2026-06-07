using TMPro;
using UnityEngine;

public class TopStatusRuntimeBinder : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text goldText;
    public TMP_Text staminaText;
    public TMP_Text focusTimeText;

    [Header("Format")]
    public bool useCommaForGold = true;

    [Header("Refresh")]
    public bool refreshRepeatedly = true;
    public float refreshInterval = 0.25f;

    private float refreshTimer;
    private FarmManager boundFarmManager;

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
        if (!refreshRepeatedly)
            return;

        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;

        if (boundFarmManager != FarmManager.Instance)
            BindFarmManager();

        Refresh();
    }

    private void BindFarmManager()
    {
        UnbindFarmManager();

        boundFarmManager = FarmManager.Instance;

        if (boundFarmManager == null)
            boundFarmManager = FindAnyObjectByType<FarmManager>();

        if (boundFarmManager != null)
            boundFarmManager.OnResourcesChanged += Refresh;
    }

    private void UnbindFarmManager()
    {
        if (boundFarmManager == null)
            return;

        boundFarmManager.OnResourcesChanged -= Refresh;
        boundFarmManager = null;
    }

    public void Refresh()
    {
        int gold = 0;
        int stamina = 0;
        int maxStamina = 5;

        if (FarmManager.Instance != null)
        {
            gold = FarmManager.Instance.gold;
            stamina = FarmManager.Instance.stamina;
            maxStamina = FarmManager.Instance.maxStamina;
        }
        else if (SaveSystem.Instance != null && SaveSystem.Instance.currentSaveData != null)
        {
            gold = SaveSystem.Instance.currentSaveData.gold;
            stamina = SaveSystem.Instance.currentSaveData.stamina;
            maxStamina = SaveSystem.Instance.currentSaveData.maxStamina;
        }

        if (goldText != null)
        {
            goldText.text = useCommaForGold
                ? gold.ToString("N0")
                : gold.ToString();
        }

        if (staminaText != null)
            staminaText.text = $"{stamina}/{maxStamina}";

        if (focusTimeText != null)
            focusTimeText.text = GetTodayFocusTimeText();
    }

    private string GetTodayFocusTimeText()
    {
        if (SessionRecordManager.Instance == null)
            return "0분";

        SessionSummaryData today = SessionRecordManager.Instance.GetTodaySummary();

        int totalMinutes = today.totalFocusedMinutes;
        int hour = totalMinutes / 60;
        int minute = totalMinutes % 60;

        if (hour > 0)
            return $"{hour}시간 {minute}분";

        return $"{minute}분";
    }
}