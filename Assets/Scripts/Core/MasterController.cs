using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 다목적 자유 비행 카메라 및 디버그 이동을 위한 마스터 컨트롤러.
/// This component provides a flexible 'noclip' or 'free-flight' style of movement.
/// It is designed to be self-contained and easily added to any GameObject (typically the main camera)
/// for debugging, scene navigation, or simple flight mechanics.
/// 
/// Written from the perspective of a 20-year development veteran: prioritizing clarity,
/// inspector-friendliness, and robustness over premature optimization.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Custom/Master Controller")]
public class MasterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Forward/backward and strafing speed in units per second.")]
    [Min(0)]
    public float moveSpeed = 10.0f;

    [Tooltip("Up/down movement speed in units per second.")]
    [Min(0)]
    public float verticalSpeed = 7.0f;

    [Header("Rotation Settings")]
    [Tooltip("Yaw (left/right) rotation speed in degrees per second.")]
    [Min(0)]
    public float rotationSpeed = 80.0f;

    // --- Private State ---
    private Vector2 _moveInput;
    private float _verticalInput;
    private float _rotationInput;

    // --- Input Actions ---
    // For a production-ready system, these would typically be defined in a .inputactions asset.
    // Defining them here makes the script self-contained and easily portable.
    private InputAction _moveAction;
    private InputAction _verticalAction;
    private InputAction _rotationAction;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// We set up our input actions here.
    /// </summary>
    private void Awake()
    {
        // --- Input Action Setup ---
        // This modern approach to input is more robust and configurable than the legacy Input Manager.

        // WASD or Gamepad Left Stick for movement
        _moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            binding: "<Gamepad>/leftStick",
            interactions: "normalizeVector2"
        );
        _moveAction.AddCompositeBinding("Vector2")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Left Shift/Ctrl or Gamepad Triggers for vertical movement
        _verticalAction = new InputAction(
            name: "Vertical",
            type: InputActionType.Value
        );
        _verticalAction.AddCompositeBinding("1DAxis")
            .With("Positive", "<Keyboard>/leftShift")
            .With("Negative", "<Keyboard>/leftCtrl")
            .With("Positive", "<Gamepad>/rightTrigger")
            .With("Negative", "<Gamepad>/leftTrigger");

        // Q/R or Gamepad Right Stick X for rotation
        _rotationAction = new InputAction(
            name: "Rotation",
            type: InputActionType.Value
        );
        _rotationAction.AddCompositeBinding("1DAxis")
            .With("Positive", "<Keyboard>/r")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Gamepad>/rightStick/x");
    }

    /// <summary>
    // Called when the object becomes enabled and active.
    /// Input actions must be enabled to read values from them.
    /// </summary>
    private void OnEnable()
    {
        _moveAction.Enable();
        _verticalAction.Enable();
        _rotationAction.Enable();
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// It's crucial to disable actions to avoid unnecessary background processing.
    /// </summary>
    private void OnDisable()
    {
        _moveAction.Disable();
        _verticalAction.Disable();
        _rotationAction.Disable();
    }

    /// <summary>
    /// Called once per frame. The main logic loop.
    /// </summary>
    void Update()
    {
        // 1. Poll for input
        // It's good practice to poll input once per frame in Update() and store it.
        ReadInput();

        // 2. Apply transformations
        // We separate input reading from application of movement/rotation.
        ApplyMovement();
        ApplyRotation();
    }

    /// <summary>
    /// Reads the current values from our InputActions.
    /// </summary>
    private void ReadInput()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();
        _verticalInput = _verticalAction.ReadValue<float>();
        _rotationInput = _rotationAction.ReadValue<float>();
    }

    /// <summary>
    /// Calculates and applies movement based on the current input state.
    /// </summary>
    private void ApplyMovement()
    {
        // Use Time.deltaTime to ensure frame-rate independent movement.
        // This is critical for consistent behavior across different hardware.
        float dt = Time.deltaTime;

        // Calculate horizontal movement vector (relative to the object's local space)
        Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);
        transform.Translate(moveDirection * moveSpeed * dt, Space.Self);

        // Calculate vertical movement vector (in world space)
        Vector3 verticalDirection = new Vector3(0, _verticalInput, 0);
        transform.Translate(verticalDirection * verticalSpeed * dt, Space.World);

        /*
         * Veteran's Note:
         * Using transform.Translate is simple and effective for free-flight or no-clip scenarios.
         * For a player character that needs to interact with physics (gravity, collisions, slopes),
         * you would replace this with a CharacterController.Move() or Rigidbody.MovePosition() call.
         * 
         * Example with CharacterController (requires the component on this GameObject):
         *
         *   [RequireComponent(typeof(CharacterController))]
         *   ...
         *   private CharacterController _controller;
         *   void Awake() { _controller = GetComponent<CharacterController>(); }
         *   void ApplyMovement() 
         *   {
         *      Vector3 motion = transform.right * _moveInput.x + transform.forward * _moveInput.y;
         *      motion *= moveSpeed;
         *      motion.y += _verticalInput * verticalSpeed;
         *      _controller.Move(motion * Time.deltaTime);
         *   }
        */
    }

    /// <summary>
    /// Calculates and applies rotation based on the current input state.
    /// </summary>
    private void ApplyRotation()
    {
        float dt = Time.deltaTime;
        
        // Apply yaw rotation around the world's Y-axis.
        // Using Space.World prevents unwanted 'roll' if the camera pitches up or down.
        // For a full flight simulator, you might want local-space rotation instead.
        transform.Rotate(0, _rotationInput * rotationSpeed * dt, 0, Space.World);
    }
}
