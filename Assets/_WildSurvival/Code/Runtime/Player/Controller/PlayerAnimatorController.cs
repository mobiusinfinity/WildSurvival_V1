using UnityEngine;
using System;

/// <summary>
/// Manages all player animations and syncs with movement controller
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    #region Fields
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovementController movementController;

    [Header("Animation Settings")]
    [SerializeField] private float locomotionSmoothTime = 0.1f;
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private bool useRootMotion = false;

    [Header("Animation Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string movementXParam = "MovementX";
    [SerializeField] private string movementYParam = "MovementY";
    [SerializeField] private string isGroundedParam = "IsGrounded";
    [SerializeField] private string isSprintingParam = "IsSprinting";
    [SerializeField] private string isCrouchingParam = "IsCrouching";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string interactTrigger = "Interact";

    // Hash IDs for performance
    private int speedHash;
    private int movementXHash;
    private int movementYHash;
    private int isGroundedHash;
    private int isSprintingHash;
    private int isCrouchingHash;
    private int jumpHash;
    private int landHash;
    private int attackHash;
    private int interactHash;

    // State tracking
    private Vector2 currentBlendInput;
    private Vector2 velocityInput;
    private float currentSpeed;
    private bool wasGrounded;

    // Events
    public static event Action<string> OnAnimationEvent;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        CacheParameterHashes();
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            PlayerMovementController.OnMovementStateChanged += HandleMovementStateChanged;
            PlayerMovementController.OnJump += HandleJump;
            PlayerMovementController.OnLand += HandleLand;
        }
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            PlayerMovementController.OnMovementStateChanged -= HandleMovementStateChanged;
            PlayerMovementController.OnJump -= HandleJump;
            PlayerMovementController.OnLand -= HandleLand;
        }
    }

    private void Update()
    {
        if (movementController == null || animator == null) return;

        UpdateLocomotion();
        UpdateAnimationStates();
    }

    private void OnAnimatorMove()
    {
        if (useRootMotion && movementController != null)
        {
            // Apply root motion if enabled
            // This would need integration with movement controller
        }
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (movementController == null)
            movementController = GetComponentInParent<PlayerMovementController>();

        if (animator == null)
            Debug.LogError("[PlayerAnimatorController] No Animator component found!");

        if (movementController == null)
            Debug.LogWarning("[PlayerAnimatorController] No PlayerMovementController found in parent!");
    }

    private void CacheParameterHashes()
    {
        speedHash = Animator.StringToHash(speedParam);
        movementXHash = Animator.StringToHash(movementXParam);
        movementYHash = Animator.StringToHash(movementYParam);
        isGroundedHash = Animator.StringToHash(isGroundedParam);
        isSprintingHash = Animator.StringToHash(isSprintingParam);
        isCrouchingHash = Animator.StringToHash(isCrouchingParam);
        jumpHash = Animator.StringToHash(jumpTrigger);
        landHash = Animator.StringToHash(landTrigger);
        attackHash = Animator.StringToHash(attackTrigger);
        interactHash = Animator.StringToHash(interactTrigger);
    }
    #endregion

    #region Animation Updates
    private void UpdateLocomotion()
    {
        // Get movement input from controller
        Vector3 velocity = movementController.Velocity;
        float speed = movementController.CurrentSpeed;

        // Calculate normalized movement for blend tree
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        Vector2 targetBlend = new Vector2(localVelocity.x, localVelocity.z).normalized;

        // Smooth the blend tree input
        currentBlendInput = Vector2.SmoothDamp(
            currentBlendInput,
            targetBlend * speed,
            ref velocityInput,
            locomotionSmoothTime
        );

        // Update animator parameters
        animator.SetFloat(movementXHash, currentBlendInput.x);
        animator.SetFloat(movementYHash, currentBlendInput.y);

        // Smooth speed parameter
        currentSpeed = Mathf.Lerp(currentSpeed, speed, speedDampTime);
        animator.SetFloat(speedHash, currentSpeed);
    }

    private void UpdateAnimationStates()
    {
        // Update grounded state
        bool isGrounded = movementController.IsGrounded;
        animator.SetBool(isGroundedHash, isGrounded);

        // Update movement states
        animator.SetBool(isSprintingHash, movementController.IsSprinting);
        animator.SetBool(isCrouchingHash, movementController.IsCrouching);

        // Track grounded changes
        if (isGrounded && !wasGrounded)
        {
            HandleLand();
        }
        wasGrounded = isGrounded;
    }
    #endregion

    #region Event Handlers
    private void HandleMovementStateChanged(PlayerMovementController.MovementState newState)
    {
        // Update animator based on movement state
        switch (newState)
        {
            case PlayerMovementController.MovementState.Idle:
                SetAnimationState("Idle");
                break;
            case PlayerMovementController.MovementState.Walking:
                SetAnimationState("Walking");
                break;
            case PlayerMovementController.MovementState.Running:
                SetAnimationState("Running");
                break;
            case PlayerMovementController.MovementState.Sprinting:
                SetAnimationState("Sprinting");
                break;
            case PlayerMovementController.MovementState.Crouching:
                SetAnimationState("Crouching");
                break;
            case PlayerMovementController.MovementState.Swimming:
                SetAnimationState("Swimming");
                break;
        }
    }

    private void HandleJump()
    {
        animator.SetTrigger(jumpHash);
    }

    private void HandleLand()
    {
        animator.SetTrigger(landHash);
    }
    #endregion

    #region Public Methods
    public void TriggerAttack()
    {
        animator.SetTrigger(attackHash);
    }

    public void TriggerInteract()
    {
        animator.SetTrigger(interactHash);
    }

    public void TriggerCustomAnimation(string triggerName)
    {
        int hash = Animator.StringToHash(triggerName);
        animator.SetTrigger(hash);
    }

    public void SetAnimationState(string stateName)
    {
        // This would transition to specific animation states
        // Implementation depends on your animator setup
    }

    public void SetAnimationFloat(string paramName, float value)
    {
        int hash = Animator.StringToHash(paramName);
        animator.SetFloat(hash, value);
    }

    public void SetAnimationBool(string paramName, bool value)
    {
        int hash = Animator.StringToHash(paramName);
        animator.SetBool(hash, value);
    }

    public void SetAnimationSpeed(float speed)
    {
        animator.speed = speed;
    }

    public void EnableRootMotion(bool enable)
    {
        useRootMotion = enable;
        animator.applyRootMotion = enable;
    }
    #endregion

    #region Animation Events
    // These methods can be called from animation events
    public void OnFootstep()
    {
        OnAnimationEvent?.Invoke("Footstep");
    }

    public void OnWeaponSwing()
    {
        OnAnimationEvent?.Invoke("WeaponSwing");
    }

    public void OnAnimationHit()
    {
        OnAnimationEvent?.Invoke("Hit");
    }

    public void OnAnimationEnd()
    {
        OnAnimationEvent?.Invoke("AnimationEnd");
    }
    #endregion

    #region Debug
    private void OnValidate()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (movementController == null)
            movementController = GetComponentInParent<PlayerMovementController>();
    }
    #endregion
}