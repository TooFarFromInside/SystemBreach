using UnityEngine;

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

    private Vector3 moveDirection = Vector3.zero;
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

        // Более надежный поиск WeaponManager
        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
            if (weaponManager == null)
            {
                weaponManager = GetComponentInChildren<WeaponManager>();
                if (weaponManager == null)
                {
                    // ИСПРАВЛЕННАЯ СТРОКА - используем новый метод
                    weaponManager = FindAnyObjectByType<WeaponManager>();
                    if (weaponManager != null)
                    {
                        Debug.Log("Found WeaponManager in scene");
                    }
                }
            }
        }

        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found anywhere!");
        }
        else
        {
            Debug.Log($"WeaponManager found: {weaponManager.gameObject.name}");
        }
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        jumpsRemaining = enableDoubleJump ? 2 : 1;
    }

    void Update()
    {
        CheckGrounded();
        HandleInput();
        HandleMovement();
        HandleMouseLook();
        HandleDash();

        // Передаем данные мыши в WeaponManager для sway эффекта
        if (weaponManager != null)
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            weaponManager.SetMouseDelta(mouseDelta);
        }
    }

    // ========== INPUT HANDLING ==========

    void HandleInput()
    {
        // Прыжок
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            moveDirection.y = jumpForce;
            jumpsRemaining--;
            Debug.Log("Jump! Jumps remaining: " + jumpsRemaining);
        }

        // Стрельба
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire input detected! (Old Input)");

            // Безопасная проверка оружия
            if (weaponManager == null)
            {
                Debug.LogError("WeaponManager is null! Attempting to find it...");
                weaponManager = GetComponent<WeaponManager>();
                if (weaponManager == null)
                {
                    Debug.LogError("WeaponManager not found on player!");
                    return;
                }
            }

            // ВЫЗОВ ОТЛАДКИ ПРИ СТРЕЛЬБЕ
            if (weaponManager != null)
            {
                weaponManager.DebugWeaponState();
            }

            if (weaponManager.currentWeapon == null)
            {
                Debug.LogError("No current weapon equipped! Attempting to equip weapon at index 0...");

                // Пытаемся принудительно экипировать оружие
                if (weaponManager.weapons != null && weaponManager.weapons.Length > 0 && weaponManager.weapons[0] != null)
                {
                    // ИСПОЛЬЗУЕМ ПУБЛИЧНЫЙ МЕТОД
                    weaponManager.ForceEquipWeapon(0);
                    Debug.Log("Attempted to equip weapon at index 0");

                    // Снова показываем состояние
                    if (weaponManager != null)
                    {
                        weaponManager.DebugWeaponState();
                    }
                }
                return;
            }

            Weapon currentWeapon = weaponManager.currentWeapon.GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.Shoot();
            }
            else
            {
                Debug.LogError($"Weapon component not found on {weaponManager.currentWeapon.name}!");
            }
        }

        // Рывок
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            StartDash();
        }

        // Отладка по клавише P
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (weaponManager != null)
            {
                weaponManager.DebugWeaponState();
            }
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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -verticalLookLimit, verticalLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    void HandleMovement()
    {
        if (isDashing) return; // Не управляем движением во время рывка

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Получаем ввод с клавиатуры
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = forward * vertical + right * horizontal;

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

        // Получаем текущее направление движения
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Направление рывка
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            // Рывок в сторону движения
            Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
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
        GUI.Label(new Rect(10, 100, 300, 30), $"Speed: {walkSpeed}", style);

        // Информация об оружии
        if (weaponManager != null && weaponManager.currentWeapon != null)
        {
            Weapon weapon = weaponManager.currentWeapon.GetComponent<Weapon>();
            if (weapon != null)
            {
                GUI.Label(new Rect(10, 130, 300, 30), $"Weapon: {weapon.weaponName}", style);
                GUI.Label(new Rect(10, 160, 300, 30), $"Damage: {weapon.damage}", style);
            }
        }
    }
}