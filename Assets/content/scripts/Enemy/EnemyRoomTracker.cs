using UnityEngine;

public class EnemyRoomTracker : MonoBehaviour
{
    [Header("Room Reference")]
    public Room room;

    void Start()
    {
        // ������������ ����� � �������
        if (room != null)
        {
            room.AddEnemy(gameObject);
            Debug.Log($"Enemy registered in room: {room.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("Enemy spawned without room reference!");
        }
    }

    void OnDestroy()
    {
        // ������� ����� �� ������� ��� �����������
        if (room != null)
        {
            room.RemoveEnemy(gameObject);
        }
    }
}