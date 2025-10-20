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
        startPosition = transform.position;
        if (visualObject == null) visualObject = gameObject;
    }

    void Update()
    {
        if (isActive)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            visualObject.transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || !other.CompareTag("Player")) return;
        Debug.Log("Press E to start level");
    }

    void OnTriggerStay(Collider other)
    {
        if (!isActive || !other.CompareTag("Player")) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Activate();
        }
    }

    void Activate()
    {
        isActive = false;
        Debug.Log("Level start triggered!");

        GameManager.Instance.StartLevel();

        if (visualObject != null)
            visualObject.SetActive(false);

        Destroy(gameObject, 1f);
    }
}