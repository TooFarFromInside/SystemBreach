using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interactable Settings")]
    public float interactionRange = 5f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("Visual Feedback")]
    public GameObject highlightEffect;

    private bool isPlayerLooking = false;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        CheckPlayerLooking();
        HandleInteraction();
    }

    void CheckPlayerLooking()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        bool wasLooking = isPlayerLooking;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            isPlayerLooking = (hit.collider.gameObject == gameObject);
        }
        else
        {
            isPlayerLooking = false;
        }

        // Визуальная обратная связь
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(isPlayerLooking);
        }

        // Показываем подсказку
        if (isPlayerLooking && !wasLooking)
        {
            Debug.Log("Press E to interact");
        }
    }

    void HandleInteraction()
    {
        if (isPlayerLooking && Input.GetKeyDown(interactionKey))
        {
            OnInteract();
        }
    }

    void OnInteract()
    {
        Debug.Log($"Interacted with {gameObject.name}");

        // Вызываем метод из StartTrigger
        StartTrigger startTrigger = GetComponent<StartTrigger>();
        if (startTrigger != null)
        {
            startTrigger.Activate();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Показываем радиус взаимодействия в редакторе
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}