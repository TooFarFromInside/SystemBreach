using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float dashForce = 15f;
    public float dashDuration = 0.3f;
    public float jumpForce = 7f;
    public float gravity = 20f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;

    [Header("Weapon System")]
    public WeaponManager weaponManager;

    private CharacterController characterController;
    private Camera playerCamera;
    private PlayerControls controls;

    private Vector3 moveDirection = Vector3.zero;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float rotationX = 0f;

    // Прыжки
    private int jumpsRemaining;
    private bool isGrounded;

    // Рывок
    private bool isDashing;
    private float dashTimeRemaining;
    private Vector3 dashDirection;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        // Находим WeaponManager если не назначен
        if (weaponManager == null)
            weaponManager = GetComponent<WeaponManager>();

        controls = new PlayerControls();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        jumpsRemaining = enableDoubleJump ? 2 : 1;
    }

    void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Look.performed += OnLook;
        controls.Player.Look.canceled += OnLook;
        controls.Player.Jump.performed += OnJump;
        controls.Player.Fire.performed += OnFire;
        controls.Player.Dash.performed += OnDash;
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Look.performed -= OnLook;
        controls.Player.Look.canceled -= OnLook;
        controls.Player.Jump.performed -= OnJump;
        controls.Player.Fire.performed -= OnFire;
        controls.Player.Dash.performed -= OnDash;
        controls.Player.Disable();
    }

    void Update()
    {
        CheckGrounded();
        HandleMovement();
        HandleMouseLook();
        HandleDash();

        // Передаем данные мыши в WeaponManager для sway эффекта
        if (weaponManager != null)
        {
            weaponManager.SetMouseDelta(lookInput);
        }

        // УБРАЛИ отладочную клавишу P со старым Input
        // Вместо этого можно добавить через новую систему ввода если нужно
    }

    // ========== INPUT SYSTEM CALLBACKS ==========

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (jumpsRemaining > 0)
        {
            moveDirection.y = jumpForce;
            jumpsRemaining--;
            Debug.Log("Jump! Jumps remaining: " + jumpsRemaining);
        }
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        Debug.Log("Fire input detected!"); // ДОБАВЬ ЭТУ СТРОЧКУ

        // Используем систему оружия вместо прямой стрельбы
        if (weaponManager != null && weaponManager.currentWeapon != null)
        {
            Weapon currentWeapon = weaponManager.currentWeapon.GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.Shoot();
            }
            else
            {
                Debug.LogError("Weapon component not found on current weapon!");
            }
        }
        else
        {
            Debug.LogError("WeaponManager or currentWeapon is null!");
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!isDashing)
        {
            StartDash();
        }
    }

    // ========== MOVEMENT HANDLING ==========

    void CheckGrounded()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && moveDirection.y <= 0)
        {
            jumpsRemaining = enableDoubleJump ? 2 : 1;
        }
    }

    void HandleMouseLook()
    {
        if (lookInput.magnitude > 0)
        {
            float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

            transform.Rotate(0, mouseX, 0);

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
    }

    void HandleMovement()
    {
        if (isDashing) return; // Не управляем движением во время рывка

        float speed = walkSpeed;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = forward * moveInput.y + right * moveInput.x;

        if (isGrounded)
        {
            moveDirection.x = desiredMove.x * speed;
            moveDirection.z = desiredMove.z * speed;

            // Небольшая сила прижимающая к земле
            if (moveDirection.y < 0)
                moveDirection.y = -0.5f;
        }
        else
        {
            // В воздухе - плавное управление
            moveDirection.x = Mathf.Lerp(moveDirection.x, desiredMove.x * speed, 0.1f);
            moveDirection.z = Mathf.Lerp(moveDirection.z, desiredMove.z * speed, 0.1f);
        }

        // Гравитация
        if (!isDashing)
            moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void StartDash()
    {
        isDashing = true;
        dashTimeRemaining = dashDuration;

        // Направление рывка
        if (moveInput.magnitude > 0.1f)
        {
            // Рывок в сторону движения
            Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            dashDirection = moveDir;
        }
        else
        {
            // Если нет движения - рывок вперед
            dashDirection = transform.forward;
        }

        // Отключаем гравитацию на время рывка
        moveDirection.y = 0;

        Debug.Log("Dash started!");
    }

    void HandleDash()
    {
        if (!isDashing) return;

        dashTimeRemaining -= Time.deltaTime;

        // Двигаем во время рывка
        Vector3 dashMovement = dashDirection * dashForce * Time.deltaTime;
        characterController.Move(dashMovement);

        if (dashTimeRemaining <= 0)
        {
            isDashing = false;
            Debug.Log("Dash ended!");
        }
    }

    // ========== DEBUG INFO ==========

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;

        GUI.Label(new Rect(10, 10, 300, 30), $"Grounded: {isGrounded}", style);
        GUI.Label(new Rect(10, 40, 300, 30), $"Jumps: {jumpsRemaining}", style);
        GUI.Label(new Rect(10, 70, 300, 30), $"Dashing: {isDashing}", style);
        GUI.Label(new Rect(10, 100, 300, 30), $"Move Input: {moveInput}", style);
        GUI.Label(new Rect(10, 130, 300, 30), $"Look Input: {lookInput}", style);

        // Информация об оружии
        if (weaponManager != null && weaponManager.currentWeapon != null)
        {
            Weapon weapon = weaponManager.currentWeapon.GetComponent<Weapon>();
            if (weapon != null)
            {
                GUI.Label(new Rect(10, 160, 300, 30), $"Weapon: {weapon.weaponName}", style);
                GUI.Label(new Rect(10, 190, 300, 30), $"Damage: {weapon.damage}", style);
            }
        }
    }
}