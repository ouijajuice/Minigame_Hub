using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float turnSpeed = 900.0f;
    public float gravity = -20.0f;

    [Header("Jump Settings")]
    public float jumpHeight = 2.0f;
    [Tooltip("Multiplier for faster falling when jump is released")] public float fallMultiplier = 2.5f;
    public float coyoteTime = 0.2f;
    [Tooltip("Grace period after pressing jump in the air before landing")] public float jumpBufferTime = 0.2f;

    [Header("Bhop Settings")]
    [Tooltip("How long after landing you can jump to keep/build speed")] public float bhopWindow = 0.15f;
    [Tooltip("Speed added per successful bhop")] public float bhopSpeedGain = 1.5f;
    [Tooltip("Max extra speed you can snowball")] public float maxBhopBonus = 10.0f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private Vector3 velocity;

    // Jump & bhop privates
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float bhopWindowCounter;
    private float bhopBonusSpeed;
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;

        Vector3 moveDirection = (camForward.normalized * input.y + camRight.normalized * input.x).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
        else
        {
            // Lose momentum if you stop moving
            bhopBonusSpeed = 0f;
        }

        // Cache grounded state before Move() updates it at the end of the frame
        bool isGrounded = controller.isGrounded;

        // Grounded, Coyote Time, & Bhop Window
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Just landed this frame: open the bhop window
            if (!wasGrounded)
            {
                bhopWindowCounter = bhopWindow;
            }
            else
            {
                bhopWindowCounter -= Time.deltaTime;

                // Missed the timing window on the ground: reset snowball speed
                if (bhopWindowCounter <= 0f)
                {
                    bhopBonusSpeed = 0f;
                }
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Execute Jump
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            // If jumping within the bhop window while moving, stack speed
            if (bhopWindowCounter > 0f && moveDirection.magnitude >= 0.1f)
            {
                bhopBonusSpeed = Mathf.Min(bhopBonusSpeed + bhopSpeedGain, maxBhopBonus);
                Debug.Log("Bhop! Bonus Speed: " + bhopBonusSpeed);
            }

            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            bhopWindowCounter = 0f;
        }

        // Variable Jump Height
        if (jumpAction.WasReleasedThisFrame() && velocity.y > 0f)
        {
            velocity.y *= 0.5f;
        }

        // Gravity with Fall Multiplier
        float currentGravity;
        if (velocity.y < 0)
        {
            currentGravity = gravity * fallMultiplier;
        }
        else
        {
            currentGravity = gravity;
        }

        velocity.y += currentGravity * Time.deltaTime;

        // Calculate Final Speed
        float currentSpeed;
        if (sprintAction.ReadValue<float>() > 0.5f)
        {
            currentSpeed = sprintSpeed + bhopBonusSpeed;
        }
        else
        {
            currentSpeed = movementSpeed + bhopBonusSpeed;
        }

        // Save grounded state before Move() changes it
        wasGrounded = isGrounded;

        controller.Move((moveDirection * currentSpeed + velocity) * Time.deltaTime);
    }
}