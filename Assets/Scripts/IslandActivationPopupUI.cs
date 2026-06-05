using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IslandActivationPopupUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject popupRoot;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text costText;
    public TMP_Text messageText;

    [Header("Buttons")]
    public Button confirmButton;
    public Button cancelButton;

    [Header("References")]
    public FarmManager farmManager;
    public IslandSetManager islandSetManager;

    private IslandSetController currentIsland;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (islandSetManager == null)
            islandSetManager = IslandSetManager.Instance;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Hide);
            cancelButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnClickConfirm);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Hide);
    }

    public void Show(IslandSetController island)
    {
        currentIsland = island;

        if (currentIsland == null)
        {
            Hide();
            return;
        }

        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (islandSetManager == null)
            islandSetManager = IslandSetManager.Instance;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        currentIsland = null;

        if (messageText != null)
            messageText.text = "";

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (currentIsland == null)
            return;

        if (titleText != null)
            titleText.text = $"{currentIsland.displayName} 활성화";

        if (descriptionText != null)
            descriptionText.text = "이 섬을 활성화하면 밭과 건물, 동물, 장식이 나타납니다.";

        if (costText != null)
            costText.text = $"필요 골드: {currentIsland.activationGoldCost}";

        bool canActivate = currentIsland.CanActivate(farmManager, out string reason);

        if (messageText != null)
            messageText.text = canActivate ? "" : reason;

        if (confirmButton != null)
            confirmButton.interactable = canActivate;
    }

    private void OnClickConfirm()
    {
        if (currentIsland == null)
            return;

        if (farmManager == null)
            farmManager = FarmManager.Instance;

        if (islandSetManager == null)
            islandSetManager = IslandSetManager.Instance;

        if (islandSetManager == null)
        {
            SetMessage("IslandSetManager가 없습니다.");
            return;
        }

        bool success = islandSetManager.TryActivateIsland(currentIsland.islandId);

        if (success)
        {
            Hide();
        }
        else
        {
            Refresh();
            SetMessage("섬을 활성화하지 못했습니다.");
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log($"[IslandActivationPopup] {message}");
    }
}