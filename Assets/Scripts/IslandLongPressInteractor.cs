using UnityEngine;
using UnityEngine.EventSystems;

public class IslandLongPressInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public IslandActivationPopupUI popupUI;

    [Header("Raycast")]
    public LayerMask interactableLayers = ~0;
    public float rayDistance = 200f;

    [Header("Long Press")]
    public float longPressDuration = 0.65f;
    public float maxMoveDistance = 18f;

    [Header("Debug")]
    public bool logDebug;

    [Header("Fallback")]
    public bool activateImmediatelyWhenNoPopup = false;

    private bool pressing;
    private bool startedOverUI;
    private bool longPressTriggered;

    private int activeFingerId = -1;
    private Vector2 startScreenPosition;
    private Vector2 currentScreenPosition;
    private float pressStartTime;

    private IslandActivationInteractable currentTarget;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (popupUI == null)
            popupUI = FindAnyObjectByType<IslandActivationPopupUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif
        HandleTouchInput();

        UpdateLongPress();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginPress(Input.mousePosition, -1, IsPointerOverUI());
        }

        if (Input.GetMouseButton(0) && pressing && activeFingerId == -1)
        {
            currentScreenPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && pressing && activeFingerId == -1)
        {
            EndPress();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
            return;

        if (Input.touchCount > 1)
        {
            CancelPress();
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            BeginPress(touch.position, touch.fingerId, IsPointerOverUI(touch.fingerId));
        }
        else if (pressing && touch.fingerId == activeFingerId)
        {
            currentScreenPosition = touch.position;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndPress();
            }
        }
    }

    private void BeginPress(Vector2 screenPosition, int fingerId, bool overUI)
    {
        pressing = true;
        startedOverUI = overUI;
        longPressTriggered = false;

        activeFingerId = fingerId;
        startScreenPosition = screenPosition;
        currentScreenPosition = screenPosition;
        pressStartTime = Time.unscaledTime;

        currentTarget = null;

        if (startedOverUI)
            return;

        currentTarget = RaycastIslandActivationTarget(screenPosition);

        if (logDebug && currentTarget != null)
        {
            Debug.Log($"[IslandLongPress] Target: {currentTarget.name}", currentTarget);
        }
    }

    private void UpdateLongPress()
    {
        if (!pressing)
            return;

        if (longPressTriggered)
            return;

        if (startedOverUI)
            return;

        if (currentTarget == null)
            return;

        float moved = Vector2.Distance(startScreenPosition, currentScreenPosition);

        if (moved > maxMoveDistance)
        {
            CancelPress();

            if (logDebug)
                Debug.Log("[IslandLongPress] 이동량 초과로 취소");

            return;
        }

        float heldTime = Time.unscaledTime - pressStartTime;

        if (heldTime >= longPressDuration)
        {
            longPressTriggered = true;
            TriggerLongPress();
        }
    }

    private void TriggerLongPress()
    {
        if (currentTarget == null)
            return;

        if (!currentTarget.CanShowActivationPopup(out string reason))
        {
            Debug.Log($"[IslandLongPress] 활성화 불가: {reason}");
            return;
        }

        if (popupUI != null)
        {
            popupUI.Show(currentTarget.islandSet);
        }
        else
        {
            if (!activateImmediatelyWhenNoPopup)
            {
                Debug.Log("[IslandLongPress] 팝업 UI가 아직 연결되지 않아 활성화 요청만 감지했습니다.");
                return;
            }

            IslandSetManager manager = IslandSetManager.Instance;

            if (manager == null)
                manager = FindAnyObjectByType<IslandSetManager>();

            if (manager != null && currentTarget.islandSet != null)
                manager.TryActivateIsland(currentTarget.islandSet.islandId);
        }
    }

    private void EndPress()
    {
        pressing = false;
        activeFingerId = -1;
        currentTarget = null;
    }

    private void CancelPress()
    {
        pressing = false;
        activeFingerId = -1;
        currentTarget = null;
        longPressTriggered = false;
    }

    private IslandActivationInteractable RaycastIslandActivationTarget(Vector2 screenPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[IslandLongPress] Main Camera가 없습니다.", this);
            return null;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayers))
            return null;

        IslandActivationInteractable target =
            hit.collider.GetComponentInParent<IslandActivationInteractable>();

        return target;
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
}