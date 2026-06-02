using UnityEngine;
using UnityEngine.EventSystems;

public class FarmCameraController : MonoBehaviour
{
    public static bool IsCameraGestureActive { get; private set; }

    [Header("References")]
    public Camera targetCamera;
    public Transform yawPivot;

    [Header("Rotation")]
    public bool enableRotation = true;
    public float touchRotationSpeed = 0.18f;
    public float mouseRotationSpeed = 0.25f;
    public float rotateStartThreshold = 18f;

    [Header("Zoom")]
    public bool enableZoom = true;
    public float minOrthographicSize = 4.2f;
    public float maxOrthographicSize = 9.5f;
    public float mouseWheelZoomSpeed = 1.2f;
    public float pinchZoomSpeed = 0.01f;
    public float zoomSmoothSpeed = 12f;

    [Header("Optional Two Finger Pan")]
    public bool enableTwoFingerPan = true;
    public float panSpeed = 0.004f;
    public Vector2 panLimitX = new Vector2(-2.5f, 2.5f);
    public Vector2 panLimitZ = new Vector2(-2.5f, 2.5f);

    private float targetOrthographicSize;

    private bool mouseDown;
    private bool mouseRotating;
    private Vector2 mouseDownPosition;
    private Vector2 lastMousePosition;

    private bool touchStartedOverUI;
    private bool touchRotating;
    private Vector2 touchDownPosition;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (yawPivot == null)
            yawPivot = transform;

        if (targetCamera != null)
            targetOrthographicSize = targetCamera.orthographicSize;
    }

    private void Update()
    {
        IsCameraGestureActive = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif

        HandleTouchInput();
        SmoothZoom();
    }

    private void HandleMouseInput()
    {
        if (targetCamera == null)
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (enableZoom && Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI())
        {
            targetOrthographicSize -= scroll * mouseWheelZoomSpeed;
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
        }

        if (!enableRotation)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            mouseRotating = false;
            mouseDownPosition = Input.mousePosition;
            lastMousePosition = Input.mousePosition;
        }

        if (mouseDown && Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;
            Vector2 totalDelta = currentPosition - mouseDownPosition;
            Vector2 frameDelta = currentPosition - lastMousePosition;

            if (!IsPointerOverUI() && totalDelta.magnitude >= rotateStartThreshold)
                mouseRotating = true;

            if (mouseRotating)
            {
                RotateByDelta(frameDelta.x, mouseRotationSpeed);
                IsCameraGestureActive = true;
            }

            lastMousePosition = currentPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
            mouseRotating = false;
        }
    }

    private void HandleTouchInput()
    {
        if (targetCamera == null)
            return;

        if (Input.touchCount == 0)
        {
            touchRotating = false;
            touchStartedOverUI = false;
            return;
        }

        if (Input.touchCount >= 2)
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);

            if (IsPointerOverUI(a.fingerId) || IsPointerOverUI(b.fingerId))
                return;

            IsCameraGestureActive = true;

            if (enableZoom)
            {
                Vector2 aPrev = a.position - a.deltaPosition;
                Vector2 bPrev = b.position - b.deltaPosition;

                float previousDistance = Vector2.Distance(aPrev, bPrev);
                float currentDistance = Vector2.Distance(a.position, b.position);
                float distanceDelta = previousDistance - currentDistance;

                targetOrthographicSize += distanceDelta * pinchZoomSpeed;
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }

            if (enableTwoFingerPan)
            {
                Vector2 averageDelta = (a.deltaPosition + b.deltaPosition) * 0.5f;
                PanByDelta(averageDelta);
            }

            return;
        }

        Touch touch = Input.GetTouch(0);

        if (!enableRotation)
            return;

        if (touch.phase == TouchPhase.Began)
        {
            touchDownPosition = touch.position;
            touchStartedOverUI = IsPointerOverUI(touch.fingerId);
            touchRotating = false;
        }

        if (touch.phase == TouchPhase.Moved && !touchStartedOverUI)
        {
            Vector2 totalDelta = touch.position - touchDownPosition;

            if (totalDelta.magnitude >= rotateStartThreshold)
                touchRotating = true;

            if (touchRotating)
            {
                RotateByDelta(touch.deltaPosition.x, touchRotationSpeed);
                IsCameraGestureActive = true;
            }
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            touchRotating = false;
            touchStartedOverUI = false;
        }
    }

    private void RotateByDelta(float deltaX, float speed)
    {
        if (yawPivot == null)
            return;

        yawPivot.Rotate(Vector3.up, deltaX * speed, Space.World);
    }

    private void PanByDelta(Vector2 screenDelta)
    {
        if (yawPivot == null || targetCamera == null)
            return;

        Vector3 right = targetCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 forward = targetCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 move =
            (-right * screenDelta.x - forward * screenDelta.y)
            * targetCamera.orthographicSize
            * panSpeed;

        yawPivot.position += move;

        Vector3 clamped = yawPivot.position;
        clamped.x = Mathf.Clamp(clamped.x, panLimitX.x, panLimitX.y);
        clamped.z = Mathf.Clamp(clamped.z, panLimitZ.x, panLimitZ.y);
        yawPivot.position = clamped;
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
}