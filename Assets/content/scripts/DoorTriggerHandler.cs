using UnityEngine;

public class DoorTriggerHandler : MonoBehaviour
{
    public Direction direction; // North, East, South, West

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Room currentRoom = GetComponentInParent<Room>();
        if (currentRoom == null) return;

        // Вычисляем позицию соседней комнаты
        Vector2Int neighborPos = GetNeighborPosition(currentRoom.gridPosition, direction);

        // Находим соседнюю комнату в GameManager
        Room neighborRoom = GameManager.Instance.GetRoomAtPosition(neighborPos);

        if (neighborRoom != null)
        {
            // Игрок переходит в соседнюю комнату
            neighborRoom.OnPlayerEnteredFromDoor(GetOppositeDirection(direction));
            Debug.Log($"Player moved to room at {neighborPos}");
        }
    }

    private Vector2Int GetNeighborPosition(Vector2Int currentPos, Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return currentPos + Vector2Int.up;
            case Direction.East: return currentPos + Vector2Int.right;
            case Direction.South: return currentPos + Vector2Int.down;
            case Direction.West: return currentPos + Vector2Int.left;
            default: return currentPos;
        }
    }

    private Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return Direction.South;
            case Direction.East: return Direction.West;
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            default: return Direction.North;
        }
    }
}