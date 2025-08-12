using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Handles all player movement including walking, running, sprinting, jumping, crouching, and swimming
/// Optimized for Unity 6 with new Input System
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    #region Nested Types
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Speed Settings")]
        public float walkSpeed = 4f;
        public float runSpeed = 8f;
        public float sprintSpeed = 12f;
        public float crouchSpeed = 2f;
        public float swimSpeed = 5f;
        public float acceleration = 10f;
        public float deceleration = 10f;

        [Header("Jump Settings")]
        public float jumpHeight = 2f;
        public float gravity = -19.62f;
        public float groundCheckDistance = 0.4f;
        public LayerMask groundMask = -1;

        [Header("Crouch Settings")]
        public float crouchHeight = 1f;
        public float standHeight = 2f;
        public float crouchTransitionSpeed = 10f;

        [Header("Stamina Settings")]
        public float sprintStaminaCost = 10f;
        public float jumpStaminaCost = 5f;
        public float staminaRegenRate = 5f;
        public float minStaminaToSprint = 10f;
    }

    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Sprinting,
        Crouching,
        Jumping,
        Falling,
        Swimming,
        Climbing
    }
    #endregion

    #region Fields
    [Header("Components")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform cameraRoot;

    [Header("Settings")]
    [SerializeField] private MovementSettings settings = new MovementSettings();

    [Header("Speed Modifiers")]
    [SerializeField] private float baseSpeedMultiplier = 1f;
    private float currentSpeedModifier = 1f;
    private float terrainSpeedModifier = 1f;
    private float weatherSpeedModifier = 1f;
    private float equipmentSpeedModifier = 1f;

    // Input
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction crouchAction;

    // State
    private MovementState currentState = MovementState.Idle;
    private Vector3 velocity;
    private Vector2 currentInput;
    private Vector2 smoothedInput;
    private Vector2 inputVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private bool isSwimming;
    private float currentSpeed;
    private float targetSpeed;
    private float fallTimer;

    // Events
    public static event Action<MovementState> OnMovementStateChanged;
    public static event Action<float> OnSpeedChanged;
    public static event Action OnJump;
    public static event Action OnLand;
    #endregion

    #region Properties
    public MovementState CurrentState => currentState;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => currentInput.magnitude > 0.1f;
    public float CurrentSpeed => currentSpeed;
    public Vector3 Velocity => velocity;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }

        SetupInput();
    }

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void Update()
    {
        UpdateGroundCheck();
        HandleInput();
        UpdateMovement();
        UpdateState();
        ApplyGravity();
        ApplyMovement();
    }
    #endregion

    #region Input Setup
    private void SetupInput()
    {
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            sprintAction = playerInput.actions["Sprint"];
            jumpAction = playerInput.actions["Jump"];
            crouchAction = playerInput.actions["Crouch"];
        }
    }

    private void EnableInput()
    {
        if (jumpAction != null) jumpAction.performed += OnJumpInput;
        if (crouchAction != null) crouchAction.performed += OnCrouchInput;
    }

    private void DisableInput()
    {
        if (jumpAction != null) jumpAction.performed -= OnJumpInput;
        if (crouchAction != null) crouchAction.performed -= OnCrouchInput;
    }

    private void HandleInput()
    {
        if (moveAction != null)
        {
            currentInput = moveAction.ReadValue<Vector2>();
        }

        if (sprintAction != null)
        {
            bool sprintPressed = sprintAction.ReadValue<float>() > 0.5f;
            isSprinting = sprintPressed && CanSprint();
        }

        // Smooth input for better feel
        smoothedInput = Vector2.SmoothDamp(smoothedInput, currentInput, ref inputVelocity, 0.1f);
    }
    #endregion

    #region Movement Logic
    private void UpdateMovement()
    {
        // Calculate target speed based on state
        targetSpeed = CalculateTargetSpeed();

        // Smooth speed transition
        float speedChangeRate = currentSpeed < targetSpeed ? settings.acceleration : settings.deceleration;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

        // Calculate movement direction
        Vector3 moveDirection = CalculateMoveDirection();

        // Apply movement with all modifiers
        float finalSpeed = GetModifiedSpeed(currentSpeed);
        velocity.x = moveDirection.x * finalSpeed;
        velocity.z = moveDirection.z * finalSpeed;

        // Notify speed change
        OnSpeedChanged?.Invoke(finalSpeed);
    }

    private float CalculateTargetSpeed()
    {
        if (!IsMoving) return 0f;

        if (isSwimming) return settings.swimSpeed;
        if (isCrouching) return settings.crouchSpeed;
        if (isSprinting) return settings.sprintSpeed;
        if (currentInput.magnitude > 0.7f) return settings.runSpeed;

        return settings.walkSpeed;
    }

    private Vector3 CalculateMoveDirection()
    {
        if (cameraRoot == null) cameraRoot = Camera.main.transform;

        Vector3 forward = cameraRoot.forward;
        Vector3 right = cameraRoot.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return forward * smoothedInput.y + right * smoothedInput.x;
    }

    private void ApplyMovement()
    {
        characterController.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Ground Check & Gravity
    private void UpdateGroundCheck()
    {
        wasGrounded = isGrounded;

        // Sphere cast for ground detection
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            settings.groundCheckDistance,
            settings.groundMask
        );

        // Landing detection
        if (isGrounded && !wasGrounded && velocity.y < -2f)
        {
            OnLand?.Invoke();
            fallTimer = 0f;
        }

        // Update fall timer
        if (!isGrounded)
        {
            fallTimer += Time.deltaTime;
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        else
        {
            velocity.y += settings.gravity * Time.deltaTime;
        }
    }
    #endregion

    #region Actions
    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (CanJump())
        {
            Jump();
        }
    }

    private void Jump()
    {
        velocity.y = Mathf.Sqrt(settings.jumpHeight * -2f * settings.gravity);
        OnJump?.Invoke();

        //Consume stamina if stats system exists
        var stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ConsumeStamina(settings.jumpStaminaCost);
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching;
        StartCoroutine(AdjustHeight());
    }

    private System.Collections.IEnumerator AdjustHeight()
    {
        float targetHeight = isCrouching ? settings.crouchHeight : settings.standHeight;
        float startHeight = characterController.height;
        float elapsed = 0f;

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            characterController.height = Mathf.Lerp(startHeight, targetHeight, t);
            yield return null;
        }

        characterController.height = targetHeight;
    }
    #endregion

    #region State Management
    private void UpdateState()
    {
        MovementState newState = DetermineState();

        if (newState != currentState)
        {
            currentState = newState;
            OnMovementStateChanged?.Invoke(currentState);
        }
    }

    private MovementState DetermineState()
    {
        if (isSwimming) return MovementState.Swimming;
        if (!isGrounded && velocity.y > 0) return MovementState.Jumping;
        if (!isGrounded && velocity.y < 0) return MovementState.Falling;
        if (isCrouching) return MovementState.Crouching;
        if (isSprinting) return MovementState.Sprinting;
        if (currentSpeed > settings.walkSpeed + 1f) return MovementState.Running;
        if (IsMoving) return MovementState.Walking;

        return MovementState.Idle;
    }
    #endregion

    #region Conditions
    private bool CanJump()
    {
        return isGrounded && !isCrouching && !isSwimming;
    }

    private bool CanSprint()
    {
        if (!IsMoving || isCrouching || isSwimming) return false;

        var stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            return stats.CurrentStamina >= settings.minStaminaToSprint;
        }

        return true;
    }
    #endregion

    #region Speed Modifiers
    public void ApplySpeedModifier(float modifier)
    {
        currentSpeedModifier = modifier;
    }

    public void ResetSpeedModifier()
    {
        currentSpeedModifier = 1f;
    }

    public void SetTerrainModifier(float modifier)
    {
        terrainSpeedModifier = Mathf.Clamp(modifier, 0.1f, 2f);
    }

    public void SetWeatherModifier(float modifier)
    {
        weatherSpeedModifier = Mathf.Clamp(modifier, 0.1f, 2f);
    }

    public void SetEquipmentModifier(float modifier)
    {
        equipmentSpeedModifier = Mathf.Clamp(modifier, 0.1f, 2f);
    }

    private float GetModifiedSpeed(float baseSpeed)
    {
        return baseSpeed * baseSpeedMultiplier * currentSpeedModifier *
                terrainSpeedModifier * weatherSpeedModifier * equipmentSpeedModifier;
    }
    #endregion

    #region Swimming
    public void EnterWater()
    {
        isSwimming = true;
        velocity.y = 0f; // Reset vertical velocity when entering water
    }

    public void ExitWater()
    {
        isSwimming = false;
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, settings.groundCheckDistance);
        }
    }
    #endregion
}