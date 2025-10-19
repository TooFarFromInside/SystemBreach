using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public string weaponName;
    public int damage = 25;
    public float fireRate = 0.2f;
    public float range = 100f;
    public WeaponType weaponType;

    [Header("Weapon References")]
    public Transform firePoint; // Точка откуда вылетают пули

    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public LineRenderer laserTrail;
    public Color laserColor = Color.blue;
    public float trailDuration = 0.1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Recoil")]
    public float recoilAmount = 0.05f;
    public float recoilRecoverySpeed = 4f;

    private AudioSource audioSource;
    private bool canShoot = true;
    private Vector3 initialPosition;
    private Camera playerCamera;

    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Rifle
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        playerCamera = Camera.main;
        initialPosition = transform.localPosition;

        // Автоматически создаем точку выстрела если ее нет
        if (firePoint == null)
        {
            CreateFirePoint();
        }

        // Автоматически создаем LineRenderer если его нет
        if (laserTrail == null)
        {
            CreateLaserTrail();
        }
        else
        {
            SetupLaserTrail();
        }

        Debug.Log($"Weapon {weaponName} initialized");
    }

    void CreateFirePoint()
    {
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(transform);

        // Позиционируем точку выстрела впереди оружия
        firePointObj.transform.localPosition = new Vector3(0.1f, 0.05f, 0.3f);
        firePointObj.transform.localRotation = Quaternion.identity;

        firePoint = firePointObj.transform;
        Debug.Log("Created FirePoint for " + weaponName);
    }

    void CreateLaserTrail()
    {
        GameObject trailObj = new GameObject("LaserTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        laserTrail = trailObj.AddComponent<LineRenderer>();
        SetupLaserTrail();

        Debug.Log("Created LaserTrail for " + weaponName);
    }

    void SetupLaserTrail()
    {
        if (laserTrail != null)
        {
            Shader laserShader = Shader.Find("Sprites/Default");
            if (laserShader != null)
            {
                laserTrail.material = new Material(laserShader);
            }

            laserTrail.startColor = laserColor;
            laserTrail.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.3f);
            laserTrail.startWidth = 0.015f;
            laserTrail.endWidth = 0.002f;
            laserTrail.positionCount = 2;
            laserTrail.enabled = false;

            laserTrail.numCapVertices = 5;
            laserTrail.numCornerVertices = 5;

            Debug.Log("LaserTrail setup completed for " + weaponName);
        }
        else
        {
            Debug.LogError("LaserTrail is still null after setup!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Рисуем точку выстрела в редакторе
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.01f);
            Gizmos.DrawWireSphere(firePoint.position, 0.02f);
        }

        // Рисуем луч прицеливания
        if (playerCamera != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * range);
        }
    }

    public void Shoot()
    {
        if (!canShoot)
        {
            Debug.Log("Cannot shoot - cooldown active");
            return;
        }

        StartCoroutine(ShootCooldown());

        // Визуальные эффекты
        PlayMuzzleFlash();
        ShowLaserTrail();
        PlayShootSound();

        // Отдача оружия (оставляем, но убираем тряску камеры)
        ApplyRecoil();

        // Hitscan
        PerformRaycast();

        Debug.Log("Fired! Weapon: " + weaponName);
    }

    void PerformRaycast()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not found!");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        Debug.DrawLine(ray.origin, ray.origin + ray.direction * range, Color.red, 1f);

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log($"Hit: {hit.transform.name} at position {hit.point}");

            // Спавним эффект попадания
            SpawnHitEffect(hit.point, hit.normal);

            // Наносим урон
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to {enemy.name}");
            }
            else
            {
                Debug.Log($"Hit object without EnemyHealth: {hit.transform.name}");
            }
        }
        else
        {
            Debug.Log("Shot missed - no hit detected");
        }
    }

    void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = position;
        hitEffect.transform.rotation = Quaternion.LookRotation(normal);

        Debug.Log($"Hit effect spawned at: {position}");

        Destroy(hitEffect, 2f);
    }

    void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
            Debug.Log("Muzzle flash played");
        }
        else
        {
            Debug.LogWarning("No muzzle flash assigned");
        }
    }

    void ShowLaserTrail()
    {
        if (laserTrail == null)
        {
            Debug.LogError("LaserTrail is null in ShowLaserTrail!");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("Player camera is null!");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            endPoint = hit.point;
            Debug.Log($"Laser trail to hit point: {endPoint}");
        }
        else
        {
            endPoint = ray.origin + ray.direction * range;
            Debug.Log($"Laser trail to max range: {endPoint}");
        }

        // ИСПОЛЬЗУЕМ FIREPOINT ВМЕСТО ПОЗИЦИИ ОРУЖИЯ
        Vector3 startPoint = firePoint.position;

        if (muzzleFlash != null && firePoint == null)
        {
            startPoint = muzzleFlash.transform.position;
        }

        laserTrail.SetPosition(0, startPoint);
        laserTrail.SetPosition(1, endPoint);
        laserTrail.enabled = true;

        Debug.Log($"Laser trail from {startPoint} to {endPoint}");

        StartCoroutine(HideLaserTrail());
    }

    IEnumerator HideLaserTrail()
    {
        yield return new WaitForSeconds(trailDuration);
        if (laserTrail != null)
        {
            laserTrail.enabled = false;
            Debug.Log("Laser trail hidden");
        }
    }

    void PlayShootSound()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
            Debug.Log("Shoot sound played");
        }
        else
        {
            Debug.LogWarning("No shoot sound or audio source");
        }
    }

    void ApplyRecoil()
    {
        // Отдача оружия (только визуальное движение оружия)
        Vector3 recoilVector = new Vector3(
            Random.Range(-0.01f, 0.01f),
            Random.Range(-0.01f, 0.01f),
            -recoilAmount
        );

        transform.localPosition = initialPosition + recoilVector;
        StartCoroutine(RecoverFromRecoil());
        Debug.Log("Recoil applied");
    }

    IEnumerator RecoverFromRecoil()
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.localPosition;

        while (elapsed < 1f)
        {
            transform.localPosition = Vector3.Lerp(startPosition, initialPosition, elapsed);
            elapsed += Time.deltaTime * recoilRecoverySpeed;
            yield return null;
        }

        transform.localPosition = initialPosition;
        Debug.Log("Recoil recovered");
    }

    IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(fireRate);
        canShoot = true;
        Debug.Log("Cooldown finished - can shoot again");
    }

    public void DebugWeapon()
    {
        Debug.Log($"=== Weapon Debug: {weaponName} ===");
        Debug.Log($"LaserTrail: {laserTrail != null}");
        Debug.Log($"MuzzleFlash: {muzzleFlash != null}");
        Debug.Log($"FirePoint: {firePoint != null}");
        Debug.Log($"PlayerCamera: {playerCamera != null}");
        Debug.Log($"CanShoot: {canShoot}");
        Debug.Log($"Damage: {damage}");
        Debug.Log($"FireRate: {fireRate}");
        Debug.Log($"Range: {range}");

        if (laserTrail != null)
        {
            Debug.Log($"LaserTrail enabled: {laserTrail.enabled}");
            Debug.Log($"LaserTrail positions: {laserTrail.positionCount}");
        }

        if (firePoint != null)
        {
            Debug.Log($"FirePoint position: {firePoint.position}");
        }
    }
}