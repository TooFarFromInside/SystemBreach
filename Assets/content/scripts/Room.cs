using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [Header("Room Info")]
    public RoomData roomData;
    public Vector2Int gridPosition;
    public bool isCleared = false;
    public List<GameObject> enemiesInRoom = new List<GameObject>();

    [Header("References")]
    public Transform rewardSpawnPoint;

    public void Initialize(RoomData data, Vector2Int gridPos)
    {
        roomData = data;
        gridPosition = gridPos;
        gameObject.name = $"Room_{gridPos.x}_{gridPos.y}_{data.roomName}";

        // ������������� ������� ����� ��� �������
        if (rewardSpawnPoint == null)
        {
            GameObject rewardPoint = new GameObject("RewardSpawnPoint");
            rewardPoint.transform.SetParent(transform);
            rewardPoint.transform.localPosition = Vector3.zero;
            rewardSpawnPoint = rewardPoint.transform;
        }
    }

    public void AddEnemy(GameObject enemy)
    {
        enemiesInRoom.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        enemiesInRoom.Remove(enemy);

        // ���������, �������� �� �������
        if (enemiesInRoom.Count == 0 && !isCleared)
        {
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        isCleared = true;
        SpawnReward();
        GameManager.Instance.OnRoomCleared(this);
    }

    private void SpawnReward()
    {
        // � ������� ����� ����� �������� �������, ������� � �.�.
        // ���� ������ ��������� ����
        GameManager.Instance.AddScore(roomData.scoreReward);
        Debug.Log($"Room cleared! Reward: {roomData.scoreReward} points");
    }
}