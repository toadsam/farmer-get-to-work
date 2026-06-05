using UnityEngine;
using UnityEngine.EventSystems;

public class FarmCameraController : MonoBehaviour
{
    public static bool IsCameraGestureActive { get; private set; }

    [Header("References")]
    public Camera targetCamera;
    public Transform cameraRoot;
    public Transform yawPivot;

    [Header("Ground Drag")]
    public bool enablePan = true;
    public float groundY = 0f;
    public float panStartThreshold = 12f;
    public bool invertPan = false;

    [Header("Pan Bounds")]
    public bool usePanBounds = true;

    [Tooltip("농장 섬/확장 섬들이 들어있는 부모 오브젝트를 넣으세요. 비워두면 Manual Bounds를 사용합니다.")]
    public Transform farmBoundsRoot;

    [Tooltip("Farm Bounds Root가 없거나 자동 계산이 부족할 때 사용하는 수동 이동 범위입니다.")]
    public Vector2 manualMinXZ = new Vector2(-12f, -12f);

    [Tooltip("위쪽으로 농장이 확장된다면 Z Max를 크게 잡으세요.")]
    public Vector2 manualMaxXZ = new Vector2(12f, 22f);

    [Tooltip("농장 경계보다 조금 더 바깥까지 볼 수 있게 하는 여유값입니다.")]
    public float boundsPadding = 5f;

    [Tooltip("켜두면 시작 시 Renderer 기준으로 농장 전체 범위를 자동 계산합니다.")]
    public bool calculateBoundsFromRenderersOnStart = true;

    [Header("Zoom")]
    public bool enableZoom = true;
    public float minOrthographicSize = 4.5f;
    public float maxOrthographicSize = 11f;
    public float mouseWheelZoomSpeed = 1.2f;
    public float pinchZoomSpeed = 0.01f;
    public float zoomSmoothSpeed = 12f;

    [Header("Snap Rotation")]
    public float rotationStep = 45f;
    public float rotationDuration = 0.25f;
    public bool blockInputWhileRotating = true;

    private float targetOrthographicSize;

    private bool mouseDown;
    private bool mouseStartedOverUI;
    private bool mousePanning;
    private Vector2 mouseDownPosition;
    private Vector2 lastMousePosition;

    private bool touchDown;
    private bool touchStartedOverUI;
    private bool touchPanning;
    private int activeFingerId = -1;
    private Vector2 touchDownPosition;
    private Vector2 lastTouchPosition;

    private bool isRotating;
    private float rotationTimer;
    private float startYaw;
    private float targetYaw;

    private Vector2 currentMinXZ;
    private Vector2 currentMaxXZ;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraRoot == null)
            cameraRoot = transform;

        if (yawPivot == null)
            yawPivot = transform;

        if (targetCamera != null)
            targetOrthographicSize = targetCamera.orthographicSize;

        targetYaw = yawPivot.eulerAngles.y;

        currentMinXZ = manualMinXZ;
        currentMaxXZ = manualMaxXZ;

        if (calculateBoundsFromRenderersOnStart)
            RecalculatePanBounds();
    }

    private void Update()
    {
        IsCameraGestureActive = false;

        UpdateSnapRotation();

        if (blockInputWhileRotating && isRotating)
        {
            SmoothZoom();
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif

        HandleTouchInput();
        SmoothZoom();
    }

    public void RotateLeft()
    {
        StartSnapRotation(-rotationStep);
    }

    public void RotateRight()
    {
        StartSnapRotation(rotationStep);
    }

    [ContextMenu("Recalculate Pan Bounds")]
    public void RecalculatePanBounds()
    {
        if (farmBoundsRoot == null)
        {
            currentMinXZ = manualMinXZ;
            currentMaxXZ = manualMaxXZ;

            Debug.Log(
                $"[FarmCamera] 수동 이동 범위 사용 / Min {currentMinXZ}, Max {currentMaxXZ}"
            );
            return;
        }

        Renderer[] renderers = farmBoundsRoot.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            currentMinXZ = manualMinXZ;
            currentMaxXZ = manualMaxXZ;

            Debug.LogWarning(
                "[FarmCamera] Farm Bounds Root 아래 Renderer가 없어 수동 이동 범위를 사용합니다.",
                this
            );
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            bounds.Encapsulate(renderers[i].bounds);
        }

        currentMinXZ = new Vector2(
            bounds.min.x - boundsPadding,
            bounds.min.z - boundsPadding
        );

        currentMaxXZ = new Vector2(
            bounds.max.x + boundsPadding,
            bounds.max.z + boundsPadding
        );

        Debug.Log(
            $"[FarmCamera] 자동 이동 범위 계산 완료 / Min {currentMinXZ}, Max {currentMaxXZ}"
        );

        ClampCameraRootToBounds();
    }

    private void HandleMouseInput()
    {
        if (targetCamera == null)
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (enableZoom && Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI())
        {
            targetOrthographicSize -= scroll * mouseWheelZoomSpeed;
            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize,
                minOrthographicSize,
                maxOrthographicSize
            );

            IsCameraGestureActive = true;
        }

        if (!enablePan)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            mouseStartedOverUI = IsPointerOverUI();
            mousePanning = false;
            mouseDownPosition = Input.mousePosition;
            lastMousePosition = Input.mousePosition;
        }

        if (mouseDown && Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;
            Vector2 totalDelta = currentPosition - mouseDownPosition;

            if (!mouseStartedOverUI && totalDelta.magnitude >= panStartThreshold)
                mousePanning = true;

            if (mousePanning)
            {
                PanByGroundDrag(lastMousePosition, currentPosition);
                IsCameraGestureActive = true;
            }

            lastMousePosition = currentPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
            mousePanning = false;
        }
    }

    private void HandleTouchInput()
    {
        if (targetCamera == null)
            return;

        if (Input.touchCount == 0)
        {
            touchDown = false;
            touchPanning = false;
            touchStartedOverUI = false;
            activeFingerId = -1;
            return;
        }

        if (Input.touchCount >= 2)
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);

            if (IsPointerOverUI(a.fingerId) || IsPointerOverUI(b.fingerId))
                return;

            IsCameraGestureActive = true;

            Vector2 previousCenter =
                ((a.position - a.deltaPosition) + (b.position - b.deltaPosition)) * 0.5f;

            Vector2 currentCenter =
                (a.position + b.position) * 0.5f;

            if (enablePan)
                PanByGroundDrag(previousCenter, currentCenter);

            if (enableZoom)
            {
                Vector2 aPrev = a.position - a.deltaPosition;
                Vector2 bPrev = b.position - b.deltaPosition;

                float previousDistance = Vector2.Distance(aPrev, bPrev);
                float currentDistance = Vector2.Distance(a.position, b.position);
                float distanceDelta = previousDistance - currentDistance;

                targetOrthographicSize += distanceDelta * pinchZoomSpeed;
                targetOrthographicSize = Mathf.Clamp(
                    targetOrthographicSize,
                    minOrthographicSize,
                    maxOrthographicSize
                );
            }

            touchDown = false;
            touchPanning = false;
            activeFingerId = -1;
            return;
        }

        if (!enablePan)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchDown = true;
            activeFingerId = touch.fingerId;
            touchStartedOverUI = IsPointerOverUI(touch.fingerId);
            touchPanning = false;
            touchDownPosition = touch.position;
            lastTouchPosition = touch.position;
        }

        if (!touchDown || touch.fingerId != activeFingerId)
            return;

        if (touch.phase == TouchPhase.Moved && !touchStartedOverUI)
        {
            Vector2 totalDelta = touch.position - touchDownPosition;

            if (totalDelta.magnitude >= panStartThreshold)
                touchPanning = true;

            if (touchPanning)
            {
                PanByGroundDrag(lastTouchPosition, touch.position);
                IsCameraGestureActive = true;
            }

            lastTouchPosition = touch.position;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            touchDown = false;
            touchPanning = false;
            touchStartedOverUI = false;
            activeFingerId = -1;
        }
    }

    private void PanByGroundDrag(Vector2 previousScreenPosition, Vector2 currentScreenPosition)
    {
        if (cameraRoot == null || targetCamera == null)
            return;

        bool previousHit = TryGetGroundPoint(previousScreenPosition, out Vector3 previousWorld);
        bool currentHit = TryGetGroundPoint(currentScreenPosition, out Vector3 currentWorld);

        if (!previousHit || !currentHit)
            return;

        Vector3 delta = previousWorld - currentWorld;
        delta.y = 0f;

        if (invertPan)
            delta = -delta;

        cameraRoot.position += delta;

        if (usePanBounds)
            ClampCameraRootToBounds();
    }

    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        if (!groundPlane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private void ClampCameraRootToBounds()
    {
        if (cameraRoot == null)
            return;

        Vector3 position = cameraRoot.position;

        position.x = Mathf.Clamp(position.x, currentMinXZ.x, currentMaxXZ.x);
        position.z = Mathf.Clamp(position.z, currentMinXZ.y, currentMaxXZ.y);

        cameraRoot.position = position;
    }

    private void StartSnapRotation(float deltaAngle)
    {
        if (yawPivot == null)
            return;

        if (isRotating)
            return;

        startYaw = yawPivot.eulerAngles.y;
        targetYaw = NormalizeAngle(startYaw + deltaAngle);

        rotationTimer = 0f;
        isRotating = true;
        IsCameraGestureActive = true;

        Debug.Log($"[FarmCamera] 회전 시작: {startYaw:F1} → {targetYaw:F1}");
    }

    private void UpdateSnapRotation()
    {
        if (!isRotating || yawPivot == null)
            return;

        rotationTimer += Time.deltaTime;

        float t = Mathf.Clamp01(rotationTimer / rotationDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        float yaw = Mathf.LerpAngle(startYaw, targetYaw, eased);

        Vector3 euler = yawPivot.eulerAngles;
        euler.y = yaw;
        yawPivot.eulerAngles = euler;

        IsCameraGestureActive = true;

        if (t >= 1f)
        {
            Vector3 finalEuler = yawPivot.eulerAngles;
            finalEuler.y = targetYaw;
            yawPivot.eulerAngles = finalEuler;

            isRotating = false;
            Debug.Log($"[FarmCamera] 회전 완료: {targetYaw:F1}");
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    private void SmoothZoom()
    {
        if (targetCamera == null)
            return;

        targetCamera.orthographicSize = Mathf.Lerp(
            targetCamera.orthographicSize,
            targetOrthographicSize,
            Time.deltaTime * zoomSmoothSpeed
        );
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 min = Application.isPlaying ? currentMinXZ : manualMinXZ;
        Vector2 max = Application.isPlaying ? currentMaxXZ : manualMaxXZ;

        Gizmos.color = Color.cyan;

        Vector3 a = new Vector3(min.x, groundY, min.y);
        Vector3 b = new Vector3(max.x, groundY, min.y);
        Vector3 c = new Vector3(max.x, groundY, max.y);
        Vector3 d = new Vector3(min.x, groundY, max.y);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
#endif
}