using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject[] weapons; // ������ ������
    public int currentWeaponIndex = 0;
    public Transform weaponParent; // ������������ ������ ��� ������

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float swaySmoothness = 2f;
    public float swayMaxAmount = 0.1f;

    // ������� ���� ��������� ����� ��������
    public GameObject currentWeapon { get; private set; }
    private Vector3 initialWeaponPosition;
    private Quaternion initialWeaponRotation;

    // ��� Input System
    private PlayerInput playerInput;
    private Vector2 mouseDelta;

    void Start()
    {
        // ������� ������������ ������ ��� ������ ���� �� ��������
        if (weaponParent == null)
        {
            weaponParent = transform.Find("PlayerCamera/WeaponParent");
            if (weaponParent == null)
            {
                GameObject weaponParentObj = new GameObject("WeaponParent");
                weaponParent = weaponParentObj.transform;
                weaponParent.SetParent(transform.Find("PlayerCamera"));
                weaponParent.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            }
        }

        // �������� PlayerInput
        playerInput = GetComponent<PlayerInput>();

        EquipWeapon(currentWeaponIndex);
    }

    void Update()
    {
        HandleWeaponSway();
        HandleWeaponSwitch();
    }

    // ����� ��� ��������� �������� ����� �� PlayerMovement
    public void SetMouseDelta(Vector2 delta)
    {
        mouseDelta = delta;
    }

    void HandleWeaponSway()
    {
        if (currentWeapon == null) return;

        // ���������� Input System ������ ������� Input
        float mouseX = mouseDelta.x * swayAmount;
        float mouseY = mouseDelta.y * swayAmount;

        // ������������ ������������ ��������
        mouseX = Mathf.Clamp(mouseX, -swayMaxAmount, swayMaxAmount);
        mouseY = Mathf.Clamp(mouseY, -swayMaxAmount, swayMaxAmount);

        // ������� ������� � ������ sway
        Vector3 targetPosition = new Vector3(
            initialWeaponPosition.x + mouseX,
            initialWeaponPosition.y + mouseY,
            initialWeaponPosition.z
        );

        // ������� �����������
        currentWeapon.transform.localPosition = Vector3.Lerp(
            currentWeapon.transform.localPosition,
            targetPosition,
            Time.deltaTime * swaySmoothness
        );

        // ��������� ������ ������
        Quaternion targetRotation = initialWeaponRotation * Quaternion.Euler(
            -mouseY * 10f,
            mouseX * 10f,
            0
        );

        currentWeapon.transform.localRotation = Quaternion.Slerp(
            currentWeapon.transform.localRotation,
            targetRotation,
            Time.deltaTime * swaySmoothness
        );
    }

    void HandleWeaponSwitch()
    {
        // ������������ ������ ��������� ���� ����� Input System
        float scroll = Mouse.current.scroll.ReadValue().y * 0.01f;
        if (scroll > 0f)
        {
            EquipWeapon(currentWeaponIndex + 1);
        }
        else if (scroll < 0f)
        {
            EquipWeapon(currentWeaponIndex - 1);
        }

        // ������������ ������� ����� Input System
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            EquipWeapon(0);
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1)
        {
            EquipWeapon(1);
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2)
        {
            EquipWeapon(2);
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame && weapons.Length > 3)
        {
            EquipWeapon(3);
        }
    }

    void EquipWeapon(int index)
    {
        // �������� ������� ������
        if (currentWeapon != null)
            currentWeapon.SetActive(false);

        // ���������� ����� ������
        currentWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);
        if (weapons[currentWeaponIndex] != null)
        {
            currentWeapon = weapons[currentWeaponIndex];
            currentWeapon.SetActive(true);

            // ��������� ��������� ������� ��� sway �������
            initialWeaponPosition = currentWeapon.transform.localPosition;
            initialWeaponRotation = currentWeapon.transform.localRotation;

            Debug.Log($"Equipped: {currentWeapon.name}");
        }
    }

    // ����� ��� ������ ������ ��� ��������
    public void ApplyShake(float intensity, float duration)
    {
        StartCoroutine(WeaponShake(intensity, duration));
    }

    IEnumerator WeaponShake(float intensity, float duration)
    {
        if (currentWeapon == null) yield break;

        float elapsed = 0f;
        Vector3 originalPosition = currentWeapon.transform.localPosition;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            currentWeapon.transform.localPosition = new Vector3(
                originalPosition.x + x,
                originalPosition.y + y,
                originalPosition.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ���������� �� �����
        currentWeapon.transform.localPosition = originalPosition;
    }
}