using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 2f;
    public float attackRange = 3f;
    public float detectionRange = 15f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;
    public float rotationSpeed = 5f; // Скорость поворота

    [Header("Visual Effects")]
    public ParticleSystem deathEffect;
    public GameObject attackProjectile;
    public Transform firePoint;

    private Transform player;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private bool canAttack = true;
    private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.5f;
            agent.updateRotation = false; // ВЫКЛЮЧАЕМ авто-поворот NavMeshAgent
        }

        if (firePoint == null)
        {
            CreateFirePoint();
        }
    }

    void CreateFirePoint()
    {
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(transform);
        firePointObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
        firePoint = firePointObj.transform;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // ПОВОРОТ К ИГРОКУ
            RotateTowardsPlayer();

            // Движение к игроку
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                // Fallback движение без NavMesh
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                transform.position += directionToPlayer * moveSpeed * Time.deltaTime;
            }

            // Атака
            if (distanceToPlayer <= attackRange && canAttack)
            {
                Attack();
            }
        }
    }

    void RotateTowardsPlayer()
    {
        // Направление к игроку (игнорируем разницу по высоте)
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            // Плавный поворот к игроку
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        if (!canAttack || isDead) return;

        Debug.Log($"{gameObject.name} attacks player!");

        // Визуальные эффекты атаки
        if (deathEffect != null)
        {
            ParticleSystem attackEffect = Instantiate(deathEffect, firePoint.position, Quaternion.identity);
            Destroy(attackEffect.gameObject, 2f);
        }

        // Наносим урон игроку
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)attackDamage);
        }

        // КД атаки
        canAttack = false;
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    void ResetAttack()
    {
        canAttack = true;
    }

    public void OnDeath()
    {
        isDead = true;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }

        if (deathEffect != null)
        {
            ParticleSystem effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Debug.Log($"{gameObject.name} died!");
    }
}