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
        if (currentHealth <= 0) return; // Уже мертв

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Уведомляем AI о смерти
        if (enemyAI != null)
        {
            enemyAI.OnDeath();
        }

        // Начисляем очки
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        // Уничтожаем объект через 2 секунды (чтобы успели проиграться эффекты)
        Destroy(gameObject, 2f);

        // Временно отключаем видимость
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // Отключаем коллайдер сразу
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Отключаем AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
    }
}