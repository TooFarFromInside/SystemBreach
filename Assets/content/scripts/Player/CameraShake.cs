using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Transform cameraTransform;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            cameraTransform = transform; // ��������� ������ �� ��������� ������

            // ��������� ��������� ���������� ������������ ��������
            originalLocalPosition = cameraTransform.localPosition;
            originalLocalRotation = cameraTransform.localRotation;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude * 0.1f;

            // ���������� ��������� ����������
            cameraTransform.localPosition = new Vector3(
                originalLocalPosition.x + x,
                originalLocalPosition.y + y,
                originalLocalPosition.z + z
            );

            // ��������� �������� ��� ����� ����������� ������
            cameraTransform.localRotation = originalLocalRotation * Quaternion.Euler(
                x * 2f,
                y * 2f,
                z * 10f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ������� ������� � ������������ ��������� �����������
        float returnElapsed = 0f;
        Vector3 startPosition = cameraTransform.localPosition;
        Quaternion startRotation = cameraTransform.localRotation;

        while (returnElapsed < 0.2f)
        {
            cameraTransform.localPosition = Vector3.Lerp(startPosition, originalLocalPosition, returnElapsed / 0.2f);
            cameraTransform.localRotation = Quaternion.Slerp(startRotation, originalLocalRotation, returnElapsed / 0.2f);
            returnElapsed += Time.deltaTime;
            yield return null;
        }

        // ��������� ��������� ������ ��������
        cameraTransform.localPosition = originalLocalPosition;
        cameraTransform.localRotation = originalLocalRotation;
    }
}