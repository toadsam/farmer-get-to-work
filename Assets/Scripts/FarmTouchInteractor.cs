using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FarmTouchInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public TMP_Text messageText;

    [Header("Raycast")]
    public LayerMask interactableLayers = ~0;
    public float rayDistance = 100f;

    [Header("Tap")]
    public float maxTapMovement = 18f;

    [Header("Message")]
    public float messageDuration = 2f;

    private float messageTimer;

    private bool mousePointerDown;
    private bool mouseStartedOverUI;
    private bool mouseGestureCancelled;
    private Vector2 mouseDownPosition;

    private bool touchPointerDown;
    private bool touchStartedOverUI;
    private bool touchGestureCancelled;
    private int activeFingerId = -1;
    private Vector2 touchDownPosition;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        UpdateMessageTimer();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseTap();
#else
        HandleTouchTap();
#endif
    }

    private void HandleMouseTap()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mousePointerDown = true;
            mouseStartedOverUI = IsPointerOverUI();
            mouseGestureCancelled = false;
            mouseDownPosition = Input.mousePosition;
        }

        if (mousePointerDown && Input.GetMouseButton(0))
        {
            float moved = Vector2.Distance(mouseDownPosition, Input.mousePosition);

            if (moved > maxTapMovement)
                mouseGestureCancelled = true;
        }

        if (mousePointerDown && Input.GetMouseButtonUp(0))
        {
            bool canTap =
                !mouseStartedOverUI &&
                !mouseGestureCancelled &&
                !FarmCameraController.IsCameraGestureActive;

            if (canTap)
                HandleScreenInput(Input.mousePosition);

            mousePointerDown = false;
            mouseGestureCancelled = false;
        }
    }

    private void HandleTouchTap()
    {
        if (Input.touchCount == 0)
            return;

        if (Input.touchCount >= 2)
        {
            touchGestureCancelled = true;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchPointerDown = true;
            activeFingerId = touch.fingerId;
            touchStartedOverUI = IsPointerOverUI(touch.fingerId);
            touchGestureCancelled = false;
            touchDownPosition = touch.position;
        }

        if (!touchPointerDown || touch.fingerId != activeFingerId)
            return;

        if (touch.phase == TouchPhase.Moved)
        {
            float moved = Vector2.Distance(touchDownPosition, touch.position);

            if (moved > maxTapMovement)
                touchGestureCancelled = true;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            bool canTap =
                !touchStartedOverUI &&
                !touchGestureCancelled &&
                !FarmCameraController.IsCameraGestureActive;

            if (canTap)
                HandleScreenInput(touch.position);

            touchPointerDown = false;
            touchGestureCancelled = false;
            activeFingerId = -1;
        }
    }

    private void HandleScreenInput(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogError("[FarmTouchInteractor] Main Camera가 없습니다.", this);
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayers))
            return;

        CropPlot plot = hit.collider.GetComponentInParent<CropPlot>();

        if (plot != null)
        {
            HandleCropPlot(plot);
            return;
        }

        ShowMessage("상호작용할 수 없는 오브젝트입니다.");
    }

    private void HandleCropPlot(CropPlot plot)
    {
        if (FarmManager.Instance == null)
        {
            Debug.LogError("[FarmTouchInteractor] FarmManager.Instance가 없습니다.", this);
            return;
        }

        FarmManager farm = FarmManager.Instance;

        if (plot.IsEmpty)
        {
            CropDefinition cropToPlant = plot.GetPlantCrop(farm.defaultCrop);

            if (!farm.CanPlantToPlot(plot, cropToPlant, out string reason))
            {
                ShowMessage(reason);
                return;
            }

            bool planted = farm.PlantToPlot(plot, cropToPlant);

            if (planted)
                ShowMessage($"{cropToPlant.displayName}을(를) 심었습니다.");

            return;
        }

        if (plot.IsReady)
        {
            if (!farm.CanHarvestPlot(plot, out string reason))
            {
                ShowMessage(reason);
                return;
            }

            int beforeGold = farm.gold;
            bool harvested = farm.HarvestPlot(plot);
            int earnedGold = farm.gold - beforeGold;

            if (harvested)
                ShowMessage($"수확 완료! Gold +{earnedGold}");

            return;
        }

        if (plot.currentCrop != null)
            ShowMessage($"아직 성장 중입니다. 성장도: {plot.growthPoints}/{plot.currentCrop.requiredGrowthPoints}");
        else
            ShowMessage("아직 성장 중입니다.");
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[FarmTouch] {message}");

        if (messageText == null)
            return;

        messageText.text = message;
        messageTimer = messageDuration;
    }

    private void UpdateMessageTimer()
    {
        if (messageText == null)
            return;

        if (messageTimer <= 0f)
            return;

        messageTimer -= Time.deltaTime;

        if (messageTimer <= 0f)
            messageText.text = "";
    }
}