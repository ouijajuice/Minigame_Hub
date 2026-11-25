using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 5.0f;
    public float turnSpeed = 200.0f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private InputAction moveAction;
    private Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        if(moveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            velocity.y = -2f; // Small downward force to keep the player grounded
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move((moveDirection * movementSpeed + velocity) * Time.deltaTime);
    }
}

