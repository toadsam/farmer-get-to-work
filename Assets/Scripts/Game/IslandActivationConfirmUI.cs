using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IslandActivationConfirmUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text costText;
    public TMP_Text messageText;

    [Header("Buttons")]
    public Button confirmButton;
    public Button cancelButton;

    private IslandSetController currentIsland;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

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
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        currentIsland = null;

        if (messageText != null)
            messageText.text = "";

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (currentIsland == null)
            return;

        FarmManager farm = FarmManager.Instance;

        if (titleText != null)
            titleText.text = $"{currentIsland.displayName} 활성화";

        if (descriptionText != null)
            descriptionText.text = "골드를 사용해 이 섬을 되살릴까요? 활성화하면 밭과 건물, 장식이 나타납니다.";

        if (costText != null)
            costText.text = $"필요 골드: {currentIsland.activationGoldCost}";

        bool canActivate = currentIsland.CanActivate(farm, out string reason);

        if (messageText != null)
            messageText.text = canActivate ? "" : reason;

        if (confirmButton != null)
            confirmButton.interactable = canActivate;
    }

    private void OnClickConfirm()
    {
        if (currentIsland == null)
            return;

        IslandSetManager manager = IslandSetManager.Instance;

        if (manager == null)
            manager = FindAnyObjectByType<IslandSetManager>();

        if (manager == null)
        {
            SetMessage("IslandSetManager가 없습니다.");
            return;
        }

        bool success = manager.TryActivateIsland(currentIsland.islandId);

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

        Debug.Log($"[IslandActivationConfirmUI] {message}");
    }
}