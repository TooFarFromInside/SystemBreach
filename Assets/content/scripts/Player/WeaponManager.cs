using UnityEngine;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject[] weapons; // Массив оружия
    public int currentWeaponIndex = 0;
    public Transform weaponParent; // Родительский объект для оружия
    public bool autoSwitchOnPickup = true;

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float swaySmoothness = 2f;
    public float swayMaxAmount = 0.1f;
    public bool enableSway = true;

    [Header("Weapon Switch")]
    public float switchSmoothTime = 0.2f;
    public bool enableWeaponSwitch = true;

    [Header("Debug")]
    public bool createDefaultWeaponIfEmpty = true;

    public GameObject currentWeapon { get; private set; }
    public bool IsSwitchingWeapon { get; private set; }

    private Vector3 initialWeaponPosition;
    private Quaternion initialWeaponRotation;
    private Coroutine switchWeaponCoroutine;
    private Vector2 mouseDelta;

    // События для внешних систем - ИНИЦИАЛИЗИРУЕМ КАК NULL
    public System.Action<GameObject> OnWeaponChanged = null;
    public System.Action<GameObject> OnWeaponEquipped = null;

    public void InitializeWeaponSlots()
    {
        // Создаем базовое оружие если слоты пустые
        if (GetWeaponCount() == 0)
        {
            CreateDefaultWeapon();
            Debug.Log("Auto-created default weapon in empty slot");
        }
    }
    void Start()
    {
        InitializeWeaponParent();
        InitializeWeapons();
        InitializeWeaponSlots(); // ДОБАВЬ ЭТУ СТРОЧКУ
        EquipWeapon(currentWeaponIndex, true);
    }

    void InitializeWeapons()
    {
        Debug.Log("=== WEAPON MANAGER INITIALIZATION ===");

        // Если массив пустой, создаем его
        if (weapons == null || weapons.Length == 0)
        {
            weapons = new GameObject[4];
            Debug.LogWarning("WeaponManager: Weapons array was empty, created new array with 4 slots");
        }

        Debug.Log($"Weapons array length: {weapons.Length}");

        // Если нет стартового оружия и разрешено создание по умолчанию
        if (createDefaultWeaponIfEmpty && (weapons[0] == null || weapons[0].GetComponent<Weapon>() == null))
        {
            Debug.Log("Creating default weapon because slot 0 is empty or invalid");
            CreateDefaultWeapon();
        }

        // Проверяем валидность индекса
        if (currentWeaponIndex >= weapons.Length)
        {
            currentWeaponIndex = 0;
            Debug.LogWarning("WeaponManager: currentWeaponIndex out of range, reset to 0");
        }

        Debug.Log($"Current weapon index: {currentWeaponIndex}");
        Debug.Log($"Weapon at index {currentWeaponIndex}: {weapons[currentWeaponIndex]?.name ?? "NULL"}");

        // Ищем первое валидное оружие если текущее null
        if (weapons[currentWeaponIndex] == null)
        {
            Debug.Log("Weapon at current index is null, searching for first valid weapon...");
            for (int i = 0; i < weapons.Length; i++)
            {
                Debug.Log($"Checking slot {i}: {weapons[i]?.name ?? "NULL"}");
                if (weapons[i] != null)
                {
                    currentWeaponIndex = i;
                    Debug.Log($"WeaponManager: Auto-switched to weapon at index {i}");
                    break;
                }
            }
        }

        // Если все еще нет оружия - ошибка
        if (weapons[currentWeaponIndex] == null)
        {
            Debug.LogError("WeaponManager: No weapons available! Assign weapons in inspector or enable createDefaultWeaponIfEmpty");
        }
        else
        {
            Debug.Log($"Final weapon selection: {weapons[currentWeaponIndex].name} at index {currentWeaponIndex}");
        }
    }

    void CreateDefaultWeapon()
    {
        Debug.Log("WeaponManager: Creating default weapon...");

        // Создаем простой объект оружия
        GameObject defaultWeapon = new GameObject("Default_Pistol");
        defaultWeapon.transform.SetParent(weaponParent);
        defaultWeapon.transform.localPosition = Vector3.zero;
        defaultWeapon.transform.localRotation = Quaternion.identity;

        // Добавляем компонент Weapon
        Weapon weaponComponent = defaultWeapon.AddComponent<Weapon>();
        weaponComponent.weaponName = "Default Pistol";
        weaponComponent.damage = 25;
        weaponComponent.fireRate = 0.5f;
        weaponComponent.range = 50f;

        // Создаем визуал (простой куб)
        GameObject weaponVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weaponVisual.transform.SetParent(defaultWeapon.transform);
        weaponVisual.transform.localPosition = new Vector3(0.1f, 0f, 0f);
        weaponVisual.transform.localScale = new Vector3(0.1f, 0.05f, 0.2f);

        // Убираем коллайдер чтобы не мешал
        Destroy(weaponVisual.GetComponent<Collider>());

        weapons[0] = defaultWeapon;
        currentWeaponIndex = 0;

        Debug.Log("WeaponManager: Default weapon created successfully");
    }

    void InitializeWeaponParent()
    {
        if (weaponParent == null)
        {
            weaponParent = transform.Find("PlayerCamera/WeaponParent");
            if (weaponParent == null)
            {
                GameObject weaponParentObj = new GameObject("WeaponParent");
                weaponParent = weaponParentObj.transform;

                Transform playerCamera = transform.Find("PlayerCamera");
                if (playerCamera != null)
                {
                    weaponParent.SetParent(playerCamera);
                }
                else
                {
                    weaponParent.SetParent(transform);
                    Debug.LogWarning("PlayerCamera not found! WeaponParent placed on player transform.");
                }

                weaponParent.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                weaponParent.localRotation = Quaternion.identity;
            }
        }
    }

    void Update()
    {
        if (enableSway)
            HandleWeaponSway();

        if (enableWeaponSwitch)
            HandleWeaponSwitch();
    }

    public void SetMouseDelta(Vector2 delta)
    {
        mouseDelta = delta;
    }

    void HandleWeaponSway()
    {
        if (currentWeapon == null || IsSwitchingWeapon) return;

        float mouseX = mouseDelta.x * swayAmount;
        float mouseY = mouseDelta.y * swayAmount;

        mouseX = Mathf.Clamp(mouseX, -swayMaxAmount, swayMaxAmount);
        mouseY = Mathf.Clamp(mouseY, -swayMaxAmount, swayMaxAmount);

        Vector3 targetPosition = new Vector3(
            initialWeaponPosition.x + mouseX,
            initialWeaponPosition.y + mouseY,
            initialWeaponPosition.z
        );

        currentWeapon.transform.localPosition = Vector3.Lerp(
            currentWeapon.transform.localPosition,
            targetPosition,
            Time.deltaTime * swaySmoothness
        );

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
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SwitchToNextWeapon();
        }
        else if (scroll < 0f)
        {
            SwitchToPreviousWeapon();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && weapons.Length > 0 && weapons[0] != null)
        {
            EquipWeapon(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Length > 1 && weapons[1] != null)
        {
            EquipWeapon(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.Length > 2 && weapons[2] != null)
        {
            EquipWeapon(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) && weapons.Length > 3 && weapons[3] != null)
        {
            EquipWeapon(3);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchToPreviousWeapon();
        }
    }

    public void SwitchToNextWeapon()
    {
        if (GetWeaponCount() <= 1) return; // Не переключать если только одно оружие

        int nextIndex = currentWeaponIndex;
        int attempts = 0;

        do
        {
            nextIndex = (nextIndex + 1) % weapons.Length;
            attempts++;
        } while (weapons[nextIndex] == null && attempts < weapons.Length);

        if (weapons[nextIndex] != null)
        {
            EquipWeapon(nextIndex);
        }
    }

    public void SwitchToPreviousWeapon()
    {
        if (GetWeaponCount() <= 1) return; // Не переключать если только одно оружие

        int previousIndex = currentWeaponIndex;
        int attempts = 0;

        do
        {
            previousIndex = (previousIndex - 1 + weapons.Length) % weapons.Length;
            attempts++;
        } while (weapons[previousIndex] == null && attempts < weapons.Length);

        if (weapons[previousIndex] != null)
        {
            EquipWeapon(previousIndex);
        }
    }

    // ИЗМЕНИ private НА public
    public void EquipWeapon(int index, bool immediate = false)
    {
        Debug.Log($"=== EQUIP WEAPON CALLED: index {index}, immediate {immediate} ===");

        if (index < 0 || index >= weapons.Length)
        {
            Debug.LogError($"Invalid index: {index}, array length: {weapons.Length}");
            return;
        }

        if (weapons[index] == null)
        {
            Debug.LogError($"Weapon at index {index} is NULL!");
            return;
        }

        Debug.Log($"Weapon at index {index}: {weapons[index].name}");

        if (IsSwitchingWeapon)
        {
            Debug.LogWarning("Cannot equip - currently switching weapons");
            return;
        }

        if (currentWeaponIndex == index)
        {
            Debug.LogWarning($"Weapon at index {index} is already equipped");
            return;
        }

        if (switchWeaponCoroutine != null)
        {
            Debug.Log("Stopping previous switch coroutine");
            StopCoroutine(switchWeaponCoroutine);
        }

        if (immediate)
        {
            Debug.Log("Performing immediate equip");
            PerformWeaponEquip(index);
        }
        else
        {
            Debug.Log("Starting smooth switch coroutine");
            switchWeaponCoroutine = StartCoroutine(SwitchWeaponSmooth(index));
        }

        Debug.Log($"=== EQUIP WEAPON COMPLETE: index {index} ===");
    }

    void PerformWeaponEquip(int index)
    {
        Debug.Log($"🔫 === PERFORM WEAPON EQUIP START: index {index} ===");

        // Шаг 1: Скрываем текущее оружие
        if (currentWeapon != null)
        {
            Debug.Log($"Hiding current weapon: {currentWeapon.name}");
            currentWeapon.SetActive(false);
            Debug.Log($"Current weapon hidden: {currentWeapon.name}, active: {currentWeapon.activeInHierarchy}");
        }
        else
        {
            Debug.Log("No current weapon to hide");
        }

        // Шаг 2: Устанавливаем новое оружие
        currentWeaponIndex = index;
        Debug.Log($"Setting currentWeaponIndex to: {currentWeaponIndex}");

        currentWeapon = weapons[currentWeaponIndex];
        Debug.Log($"Setting currentWeapon to: {currentWeapon?.name ?? "NULL"}");

        // КРИТИЧЕСКАЯ ПРОВЕРКА
        if (currentWeapon == null)
        {
            Debug.LogError($"🚨 CRITICAL ERROR: currentWeapon is NULL after assignment!");
            Debug.LogError($"Weapons array: {weapons.Length} slots");
            Debug.LogError($"Weapon at index {index}: {weapons[index]?.name ?? "NULL"}");
            return;
        }

        // Шаг 3: Устанавливаем родителя
        Debug.Log($"Weapon parent check: current parent = {currentWeapon.transform.parent?.name ?? "NULL"}, target parent = {weaponParent?.name ?? "NULL"}");

        if (currentWeapon.transform.parent != weaponParent)
        {
            Debug.Log($"Setting weapon parent for {currentWeapon.name}");
            currentWeapon.transform.SetParent(weaponParent);
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;
            Debug.Log($"Parent set: {currentWeapon.transform.parent?.name ?? "NULL"}");
        }
        else
        {
            Debug.Log("Weapon already has correct parent");
        }

        // Шаг 4: Активируем оружие
        Debug.Log($"Activating weapon: {currentWeapon.name}");
        currentWeapon.SetActive(true);
        Debug.Log($"Weapon activated: {currentWeapon.name}, activeInHierarchy: {currentWeapon.activeInHierarchy}");

        // Шаг 5: Сохраняем позиции для sway
        initialWeaponPosition = currentWeapon.transform.localPosition;
        initialWeaponRotation = currentWeapon.transform.localRotation;
        Debug.Log($"Initial position saved: {initialWeaponPosition}");

        Debug.Log($"✅ SUCCESS: Equipped {currentWeapon.name}");

        // Вызываем события
        if (OnWeaponChanged != null)
            OnWeaponChanged.Invoke(currentWeapon);

        if (OnWeaponEquipped != null)
            OnWeaponEquipped.Invoke(currentWeapon);

        Debug.Log($"🔫 === PERFORM WEAPON EQUIP COMPLETE ===");
    }

    // Добавь этот метод в WeaponManager.cs
    public void DebugWeaponState()
    {
        Debug.Log("=== WEAPON MANAGER DEBUG ===");
        Debug.Log($"Current Weapon: {currentWeapon?.name ?? "NULL"}");
        Debug.Log($"Current Index: {currentWeaponIndex}");
        Debug.Log($"Weapons array: {weapons?.Length ?? 0} slots");

        if (weapons != null)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                string status = weapons[i] == null ? "EMPTY" : weapons[i].name;
                string active = weapons[i] != null && weapons[i].activeInHierarchy ? "ACTIVE" : "INACTIVE";
                Debug.Log($"  Slot {i}: {status} ({active})");
            }
        }

        Debug.Log("=== DEBUG END ===");
    }

    IEnumerator SwitchWeaponSmooth(int index)
    {
        IsSwitchingWeapon = true;

        if (currentWeapon != null)
        {
            currentWeapon.SetActive(false);
        }

        yield return new WaitForSeconds(switchSmoothTime * 0.3f);

        PerformWeaponEquip(index);

        yield return new WaitForSeconds(switchSmoothTime * 0.7f);

        IsSwitchingWeapon = false;
    }

    public bool AddWeapon(GameObject newWeapon)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                weapons[i] = newWeapon;
                newWeapon.transform.SetParent(weaponParent);
                newWeapon.SetActive(false);

                if (autoSwitchOnPickup)
                    EquipWeapon(i);

                Debug.Log($"Added new weapon: {newWeapon.name} to slot {i}");
                return true;
            }
        }

        Debug.LogWarning("No empty weapon slots available!");
        return false;
    }

    public void RemoveWeapon(int index)
    {
        if (index >= 0 && index < weapons.Length && weapons[index] != null)
        {
            if (currentWeaponIndex == index)
            {
                weapons[index] = null;
                SwitchToNextWeapon();
            }
            else
            {
                weapons[index] = null;
            }
        }
    }

    public void ApplyShake(float intensity, float duration)
    {
        StartCoroutine(WeaponShake(intensity, duration));
    }

    IEnumerator WeaponShake(float intensity, float duration)
    {
        if (currentWeapon == null || IsSwitchingWeapon) yield break;

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

        currentWeapon.transform.localPosition = originalPosition;
    }

    public bool HasWeapon()
    {
        return currentWeapon != null;
    }

    public int GetWeaponCount()
    {
        int count = 0;
        foreach (var weapon in weapons)
        {
            if (weapon != null) count++;
        }
        return count;
    }

    void OnValidate()
    {
        if (weapons != null)
        {
            currentWeaponIndex = Mathf.Clamp(currentWeaponIndex, 0, weapons.Length - 1);
        }
    }
    // Добавь этот публичный метод для отладки
    public void ForceEquipWeapon(int index)
    {
        Debug.Log($"Force equipping weapon at index {index}");
        if (index >= 0 && index < weapons.Length && weapons[index] != null)
        {
            Debug.Log($"Weapon exists: {weapons[index].name}");
            EquipWeapon(index, true);

            // Проверяем результат
            if (currentWeapon == null)
            {
                Debug.LogError($"Force equip failed - currentWeapon still null!");
            }
            else
            {
                Debug.Log($"Force equip successful: {currentWeapon.name}");
            }
        }
        else
        {
            Debug.LogError($"Cannot force equip - invalid index {index} or weapon is null");
            Debug.Log($"Weapons array: {weapons?.Length}, Weapon at index: {weapons?[index]?.name ?? "NULL"}");
        }
    }
}