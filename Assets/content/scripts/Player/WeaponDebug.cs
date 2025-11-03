using UnityEngine;

public class WeaponDebug : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugWeaponSystem();
        }
    }

    void DebugWeaponSystem()
    {
        // «¿Ã≈Õ»À Õ¿ ÕŒ¬€… Ã≈“Œƒ
        WeaponManager wm = FindAnyObjectByType<WeaponManager>();
        if (wm == null)
        {
            Debug.LogError("=== WEAPON DEBUG: No WeaponManager found in scene ===");
            return;
        }

        Debug.Log("=== WEAPON SYSTEM DEBUG ===");
        Debug.Log($"WeaponManager: {wm.gameObject.name}");
        Debug.Log($"Current Weapon: {wm.currentWeapon?.name ?? "NULL"}");
        Debug.Log($"Weapons array: {wm.weapons?.Length ?? 0} slots");

        if (wm.weapons != null)
        {
            for (int i = 0; i < wm.weapons.Length; i++)
            {
                Debug.Log($"  Slot {i}: {wm.weapons[i]?.name ?? "EMPTY"}");
            }
        }
    }
}