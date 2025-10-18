using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lookSensitivity = 1.5f;

    [Header("References")]
    [SerializeField] private Transform playerCamera;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = transform.TransformDirection(move) * speed;
        controller.SimpleMove(move);
    }

    private void HandleLook()
    {
        // Поворот тела (по горизонтали)
        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        // Поворот камеры (по вертикали)
        cameraPitch -= lookInput.y * lookSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // ✅ Методы совместимы с Unity 6.2 — параметр теперь обязателен
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
            moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
            lookInput = context.ReadValue<Vector2>();
    }
}
