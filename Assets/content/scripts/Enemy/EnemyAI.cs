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
    public float rotationSpeed = 5f;

    [Header("Visual Effects")]
    public ParticleSystem deathEffect;
    public Transform firePoint;

    private Transform player;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isActive = false; // Новое поле

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.5f;
            agent.updateRotation = false;
        }
    }

    void Update()
    {
        if (!isActive || isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            RotateTowardsPlayer();

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(player.position);
            }

            if (distanceToPlayer <= attackRange && canAttack)
            {
                Attack();
            }
        }
    }

    // НОВЫЙ МЕТОД для активации/деактивации
    public void SetActive(bool active)
    {
        isActive = active;
        if (agent != null)
        {
            agent.isStopped = !active;
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        if (!canAttack || isDead) return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)attackDamage);
        }

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
        isActive = false;

        if (agent != null) agent.isStopped = true;
        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }
}