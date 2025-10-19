using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int maxHealth = 100;
    public int scoreValue = 100;

    private int currentHealth;
    private EnemyAI enemyAI;

    void Start()
    {
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // ��� �����

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // ���������� AI � ������
        if (enemyAI != null)
        {
            enemyAI.OnDeath();
        }

        // ��������� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        // ���������� ������ ����� 2 ������� (����� ������ ����������� �������)
        Destroy(gameObject, 2f);

        // �������� ��������� ���������
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // ��������� ��������� �����
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // ��������� AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
    }
}