using UnityEngine;

public class StartTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public float hoverHeight = 3f;
    public float hoverSpeed = 2f;
    public GameObject visualObject;

    private bool isActive = true;
    private Vector3 startPosition;

    void Start()
    {
        Debug.Log("StartTrigger: Start called - Trigger initialized");
        startPosition = transform.position;
        if (visualObject == null) visualObject = gameObject;
    }

    void Update()
    {
        // Парящая анимация
        if (isActive)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            visualObject.transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        if (GetComponent<Interactable>().highlightEffect == null)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.SetParent(transform);
            highlight.transform.localScale = Vector3.one * 1.2f;
            highlight.transform.localPosition = Vector3.zero;

            // Настраиваем материал
            Renderer renderer = highlight.GetComponent<Renderer>();
            renderer.material.color = new Color(1, 1, 0, 0.3f); // Полупрозрачный желтый

            // Убираем коллайдер
            Destroy(highlight.GetComponent<Collider>());

            highlight.SetActive(false);
            GetComponent<Interactable>().highlightEffect = highlight;
        }
    }

    // СДЕЛАЕМ ПУБЛИЧНЫМ для вызова из Interactable
    public void Activate()
    {
        if (!isActive) return;

        isActive = false;

        Debug.Log("=== START TRIGGER ACTIVATED ===");

        if (GameManager.Instance == null)
        {
            Debug.LogError("StartTrigger: GameManager.Instance is NULL!");
            return;
        }

        Debug.Log("StartTrigger: Calling GameManager.StartLevel()");
        GameManager.Instance.StartLevel();

        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        // Отключаем Interactable компонент
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.enabled = false;
        }

        Debug.Log("StartTrigger: Destroying trigger in 1 second");
        Destroy(gameObject, 1f);
    }
}