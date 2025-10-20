using UnityEngine;

public class RewardCollector : MonoBehaviour
{
    public int scoreValue = 50;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AddScore(scoreValue);
            Debug.Log($"Collected reward: {scoreValue} points");
            Destroy(gameObject);
        }
    }
}