using UnityEngine;
using UnityEngine.InputSystem;
using System;
/// <summary>
/// Advanced third-person camera controller with collision detection, zoom, and smooth follow
/// Lines: 1-385
/// </summary>
public class ThirdPersonCameraController : MonoBehaviour
{
    #region Nested Types
    [System.Serializable]
    public class CameraSettings
    {
        [Header("Distance Settings")]
        public float defaultDistance = 5f;
        public float minDistance = 1f;
        public float maxDistance = 10f;
        public float zoomSpeed = 5f;
        public float zoomSmoothTime = 0.1f;

        [Header("Rotation Settings")]
        public float horizontalSensitivity = 2f;
        public float verticalSensitivity = 2f;
        public float minVerticalAngle = -30f;
        public float maxVerticalAngle = 60f;
        public bool invertY = false;

        [Header("Follow Settings")]
        public float followSmoothTime = 0.1f;
        public Vector3 targetOffset = new Vector3(0, 1.5f, 0);
        public float lookAheadDistance = 2f;
        public bool useLookAhead = true;

        [Header("Collision Settings")]
        public float collisionRadius = 0.3f;
        public LayerMask collisionMask = -1;
        public float collisionPadding = 0.1f;
        public bool useCollisionPrediction = true;

        [Header("FOV Settings")]
        public float defaultFOV = 60f;
        public float sprintFOV = 65f;
        public float aimFOV = 40f;
        public float fovTransitionSpeed = 5f;
    }

    public enum CameraMode
    {
        Normal,
        Aim,
        Locked,
        Cinematic,
        FirstPerson
    }
    #endregion

    #region Fields - Lines 57-90
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Camera cam;
    [SerializeField] private PlayerMovementController movementController;

    [Header("Settings")]
    [SerializeField] private CameraSettings settings = new CameraSettings();
    [SerializeField] private CameraMode currentMode = CameraMode.Normal;

    [Header("Input")]
    [SerializeField] private bool useNewInputSystem = true;
    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction zoomAction;

    // Runtime variables
    private float currentDistance;
    private float targetDistance;
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Vector3 currentFollowVelocity;
    private float currentZoomVelocity;
    private float currentFOV;
    private float targetFOV;

    // Collision
    private float actualDistance;
    private bool isColliding;
    private RaycastHit[] collisionHits = new RaycastHit[10];

    // Camera shake
    private float shakeIntensity;
    private float shakeDuration;
    private Vector3 shakeOffset;

    // Events
    public static event Action<CameraMode> OnCameraModeChanged;
    public static event Action<float> OnCameraZoom;
    #endregion

    #region Properties - Lines 91-96
    public CameraMode CurrentMode => currentMode;
    public bool IsColliding => isColliding;
    public float CurrentDistance => currentDistance;
    public Transform Target => target;
    #endregion

    #region Unity Lifecycle - Lines 97-150
    private void Awake()
    {
        InitializeComponents();
        InitializeValues();
    }

    private void OnEnable()
    {
        if (useNewInputSystem && playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
            zoomAction = playerInput.actions["Zoom"];

            if (lookAction != null) lookAction.Enable();
            if (zoomAction != null) zoomAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (useNewInputSystem)
        {
            if (lookAction != null) lookAction.Disable();
            if (zoomAction != null) zoomAction.Disable();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateCameraRotation();
        UpdateCameraPosition();
        UpdateCameraFOV();
        //ApplyCameraShake();
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + settings.targetOffset, 0.2f);

            if (isColliding)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, settings.collisionRadius);
            }
        }
    }
    #endregion

    #region Initialization - Lines 151-180
    private void InitializeComponents()
    {
        // Get camera if not assigned
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = GetComponentInChildren<Camera>();
            if (cam == null)
                cam = Camera.main;
        }

        // Find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                lookAtTarget = target;
                movementController = player.GetComponent<PlayerMovementController>();

                if (useNewInputSystem)
                    playerInput = player.GetComponent<PlayerInput>();
            }
        }
    }

    private void InitializeValues()
    {
        currentDistance = settings.defaultDistance;
        targetDistance = settings.defaultDistance;
        currentFOV = settings.defaultFOV;
        targetFOV = settings.defaultFOV;
    }
    #endregion

    #region Input Handling - Lines 181-210
    private void HandleInput()
    {
        if (currentMode == CameraMode.Locked || currentMode == CameraMode.Cinematic)
            return;

        Vector2 lookInput = Vector2.zero;
        float zoomInput = 0f;

        if (useNewInputSystem && lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
            if (zoomAction != null)
                zoomInput = zoomAction.ReadValue<float>();
        }
        else
        {
            // Fallback to old input system
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            zoomInput = Input.GetAxis("Mouse ScrollWheel");
        }

        // Apply input
        currentHorizontalAngle += lookInput.x * settings.horizontalSensitivity;
        float verticalInput = settings.invertY ? -lookInput.y : lookInput.y;
        currentVerticalAngle -= verticalInput * settings.verticalSensitivity;
        currentVerticalAngle = UnityEngine.Mathf.Clamp(currentVerticalAngle, settings.minVerticalAngle, settings.maxVerticalAngle);

        // Handle zoom
        if (UnityEngine.Mathf.Abs(zoomInput) > 0.01f)
        {
            targetDistance -= zoomInput * settings.zoomSpeed;
            targetDistance = UnityEngine.Mathf.Clamp(targetDistance, settings.minDistance, settings.maxDistance);
            OnCameraZoom?.Invoke(targetDistance);
        }
    }
    #endregion

    #region Camera Updates - Lines 211-290
    private void UpdateCameraRotation()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);

        // Apply rotation smoothly
        transform.rotation = rotation;
    }

    private void UpdateCameraPosition()
    {
        if (target == null) return;

        // Calculate target position with offset
        Vector3 targetPosition = target.position + settings.targetOffset;

        // Add look-ahead if enabled
        if (settings.useLookAhead && movementController != null)
        {
            Vector3 velocity = movementController.Velocity;
            velocity.y = 0;
            if (velocity.magnitude > 0.1f)
            {
                Vector3 lookAhead = velocity.normalized * settings.lookAheadDistance;
                targetPosition += lookAhead * UnityEngine.Mathf.Min(velocity.magnitude / 10f, 1f);
            }
        }

        // Smooth follow
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position - (transform.forward * currentDistance),
            targetPosition,
            ref currentFollowVelocity,
            settings.followSmoothTime
        );

        // Update distance with smoothing
        currentDistance = UnityEngine.Mathf.SmoothDamp(
            currentDistance,
            targetDistance,
            ref currentZoomVelocity,
            settings.zoomSmoothTime
        );

        // Check for collisions
        actualDistance = CheckCameraCollision(smoothedPosition, -transform.forward, currentDistance);

        // Apply final position
        transform.position = smoothedPosition + (-transform.forward * actualDistance);
    }

    private void UpdateCameraFOV()
    {
        if (cam == null) return;

        // Determine target FOV based on mode and state
        switch (currentMode)
        {
            case CameraMode.Aim:
                targetFOV = settings.aimFOV;
                break;
            case CameraMode.Normal:
                if (movementController != null && movementController.IsSprinting)
                    targetFOV = settings.sprintFOV;
                else
                    targetFOV = settings.defaultFOV;
                break;
            default:
                targetFOV = settings.defaultFOV;
                break;
        }

        // Smooth FOV transition
        currentFOV = UnityEngine.Mathf.Lerp(currentFOV, targetFOV, UnityEngine.Time.deltaTime * settings.fovTransitionSpeed);
        cam.fieldOfView = currentFOV;
    }
    #endregion

    #region Collision Detection - Lines 291-320
    private float CheckCameraCollision(Vector3 targetPos, Vector3 direction, float distance)
    {
        isColliding = false;

        // Sphere cast from target to camera
        int hits = UnityEngine.Physics.SphereCastNonAlloc(
            targetPos,
            settings.collisionRadius,
            direction,
            collisionHits,
            distance,
            settings.collisionMask,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = distance;

        for (int i = 0; i < hits; i++)
        {
            // Ignore the target
            if (collisionHits[i].transform == target ||
                collisionHits[i].transform.IsChildOf(target))
                continue;

            float hitDistance = collisionHits[i].distance - settings.collisionPadding;
            if (hitDistance < closestDistance)
            {
                closestDistance = UnityEngine.Mathf.Max(hitDistance, settings.minDistance);
                isColliding = true;
            }
        }

        return closestDistance;
    }
    #endregion

    #region Camera Effects - Lines 321-350
    public void ShakeCamera(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }

    private void ApplyCameraShake()
    {
        if (shakeDuration > 0)
        {
            shakeOffset = UnityEngine.Random.insideUnitSphere * shakeIntensity;
            transform.position += shakeOffset;

            shakeDuration -= UnityEngine.Time.deltaTime;
            shakeIntensity = UnityEngine.Mathf.Lerp(shakeIntensity, 0, UnityEngine.Time.deltaTime * 5f);

            if (shakeDuration <= 0)
            {
                shakeIntensity = 0;
                shakeOffset = Vector3.zero;
            }
        }
    }
    #endregion

    #region Public Methods - Lines 351-385
    public void SetCameraMode(CameraMode mode)
    {
        if (currentMode != mode)
        {
            currentMode = mode;
            OnCameraModeChanged?.Invoke(mode);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lookAtTarget = newTarget;

        if (newTarget != null)
        {
            movementController = newTarget.GetComponent<PlayerMovementController>();
            if (useNewInputSystem)
                playerInput = newTarget.GetComponent<PlayerInput>();
        }
    }

    public void ResetCamera()
    {
        currentHorizontalAngle = 0;
        currentVerticalAngle = 0;
        currentDistance = settings.defaultDistance;
        targetDistance = settings.defaultDistance;
    }

    public void SetSensitivity(float horizontal, float vertical)
    {
        settings.horizontalSensitivity = horizontal;
        settings.verticalSensitivity = vertical;
    }
    #endregion
}