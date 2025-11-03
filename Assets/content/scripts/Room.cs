using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [System.Serializable]
    public class Door
    {
        public GameObject wall;
        public GameObject doorPlug;
        public GameObject doorTrigger;
        public bool isOpen;
    }

    [Header("Room Info")]
    public RoomData roomData;
    public Vector2Int gridPosition;
    public bool isCleared = false;
    public List<GameObject> enemiesInRoom = new List<GameObject>();

    [Header("Door System")]
    public Door northDoor;
    public Door eastDoor;
    public Door southDoor;
    public Door westDoor;

    [Header("Visual Components")]
    public Renderer floorRenderer;
    public Renderer ceilingRenderer;
    public Renderer[] wallRenderers;

    [Header("References")]
    public Transform rewardSpawnPoint;

    public void Initialize(RoomData data, Vector2Int gridPos)
    {
        Debug.Log($"Room.Initialize: Initializing room at {gridPos} with {data.roomName}");

        roomData = data;
        gridPosition = gridPos;
        gameObject.name = $"Room_{gridPos.x}_{gridPos.y}_{data.roomName}";

        AutoFindRoomComponents();
        CloseAllDoors();

        if (rewardSpawnPoint == null)
        {
            GameObject rewardPoint = new GameObject("RewardSpawnPoint");
            rewardPoint.transform.SetParent(transform);
            rewardPoint.transform.localPosition = Vector3.zero;
            rewardSpawnPoint = rewardPoint.transform;
        }

        Debug.Log($"Room.Initialize: Room {gridPos} initialized successfully");
    }

    private void AutoFindRoomComponents()
    {
        Debug.Log("Room.AutoFindRoomComponents: Searching for room components...");

        if (floorRenderer == null)
            floorRenderer = transform.Find("Floor")?.GetComponent<Renderer>();

        if (ceilingRenderer == null)
            ceilingRenderer = transform.Find("Ceiling")?.GetComponent<Renderer>();

        if (northDoor.wall == null) FindDoorComponents(ref northDoor, "North");
        if (eastDoor.wall == null) FindDoorComponents(ref eastDoor, "East");
        if (southDoor.wall == null) FindDoorComponents(ref southDoor, "South");
        if (westDoor.wall == null) FindDoorComponents(ref westDoor, "West");

        Debug.Log($"Room.AutoFindRoomComponents: Components found - Floor: {floorRenderer != null}, Ceiling: {ceilingRenderer != null}");
    }

    private void FindDoorComponents(ref Door door, string direction)
    {
        string wallPath = $"Walls/Wall_{direction}";
        string plugPath = $"Door_Plugs/Plug_{direction}";
        string triggerPath = $"Door_Triggers/Trigger_{direction}";

        door.wall = transform.Find(wallPath)?.gameObject;
        door.doorPlug = transform.Find(plugPath)?.gameObject;
        door.doorTrigger = transform.Find(triggerPath)?.gameObject;

        Debug.Log($"Room.FindDoorComponents: {direction} - Wall: {door.wall != null}, Plug: {door.doorPlug != null}, Trigger: {door.doorTrigger != null}");
    }

    public void SetupDoors(bool north, bool east, bool south, bool west)
    {
        Debug.Log($"Room.SetupDoors: Setting doors - N:{north}, E:{east}, S:{south}, W:{west}");
        SetDoorState(Direction.North, north);
        SetDoorState(Direction.East, east);
        SetDoorState(Direction.South, south);
        SetDoorState(Direction.West, west);
    }

    public void SetDoorState(Direction direction, bool isOpen)
    {
        Door door = GetDoorByDirection(direction);
        if (door == null) return;

        door.isOpen = isOpen;

        // СТЕНА - ВСЕГДА ОСТАЕТСЯ ВКЛЮЧЕННОЙ! Никогда не меняется
        // if (door.wall != null) door.wall.SetActive(true); // Всегда true

        // ЗАГЛУШКА И ТРИГГЕР - управляют открытием/закрытием
        if (isOpen)
        {
            // Дверь открыта - показываем заглушку и включаем триггер
            if (door.doorPlug != null) door.doorPlug.SetActive(true);
            if (door.doorTrigger != null) door.doorTrigger.SetActive(true);
        }
        else
        {
            // Дверь закрыта - убираем заглушку и выключаем триггер
            if (door.doorPlug != null) door.doorPlug.SetActive(false);
            if (door.doorTrigger != null) door.doorTrigger.SetActive(false);
        }
    }

    // НОВЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ДВЕРЯМИ
    public void OpenDoor(Direction direction)
    {
        Debug.Log($"Room.OpenDoor: Opening {direction} door");
        Door door = GetDoorByDirection(direction);
        if (door == null) return;

        // ВКЛЮЧАЕМ заглушку и триггер
        if (door.doorPlug != null)
        {
            door.doorPlug.SetActive(true);
            Debug.Log($" - Plug activated: {door.doorPlug.activeSelf}");
        }
        if (door.doorTrigger != null)
        {
            door.doorTrigger.SetActive(true);
            Debug.Log($" - Trigger activated: {door.doorTrigger.activeSelf}");
        }
    }

    public void CloseDoor(Direction direction)
    {
        Debug.Log($"Room.CloseDoor: Closing {direction} door");
        Door door = GetDoorByDirection(direction);
        if (door == null) return;

        // ВЫКЛЮЧАЕМ заглушку и триггер
        if (door.doorPlug != null)
        {
            door.doorPlug.SetActive(false);
            Debug.Log($" - Plug deactivated: {door.doorPlug.activeSelf}");
        }
        if (door.doorTrigger != null)
        {
            door.doorTrigger.SetActive(false);
            Debug.Log($" - Trigger deactivated: {door.doorTrigger.activeSelf}");
        }
    }

    public void CloseAllDoors()
    {
        Debug.Log("Room.CloseAllDoors: Closing ALL doors");
        CloseDoor(Direction.North);
        CloseDoor(Direction.East);
        CloseDoor(Direction.South);
        CloseDoor(Direction.West);
    }

    public void OpenSpecificDoors(List<Direction> directionsToOpen)
    {
        CloseAllDoors(); // Сначала закрываем все

        foreach (Direction direction in directionsToOpen)
        {
            OpenDoor(direction);
        }

        Debug.Log($"Opened {directionsToOpen.Count} specific doors");
    }

    private Door GetDoorByDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return northDoor;
            case Direction.East: return eastDoor;
            case Direction.South: return southDoor;
            case Direction.West: return westDoor;
            default: return null;
        }
    }

    public void SetFloorLevelVisuals(int level)
    {
        if (floorRenderer != null)
        {
            Color[] levelColors = { Color.white, Color.blue, Color.red, Color.green, Color.yellow };
            int colorIndex = (level - 1) % levelColors.Length;
            floorRenderer.material.color = levelColors[colorIndex];
        }
    }

    public void AddEnemy(GameObject enemy)
    {
        enemiesInRoom.Add(enemy);
        Debug.Log($"Room.AddEnemy: Added enemy. Total enemies: {enemiesInRoom.Count}");
    }

    public void RemoveEnemy(GameObject enemy)
    {
        enemiesInRoom.Remove(enemy);
        Debug.Log($"Room.RemoveEnemy: Removed enemy. Total enemies: {enemiesInRoom.Count}");

        if (enemiesInRoom.Count == 0 && !isCleared)
        {
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        isCleared = true;
        Debug.Log($"Room.OnRoomCleared: Room {gridPosition} cleared!");
        GameManager.Instance.OnRoomCleared(this);
    }

    public void OnPlayerEnteredFromDoor(Direction fromDirection)
    {
        Debug.Log($"Room.OnPlayerEnteredFromDoor: Player entered from {fromDirection}");
        ActivateRoomEnemies();
    }

    private void ActivateRoomEnemies()
    {
        Debug.Log($"Room.ActivateRoomEnemies: Activating {enemiesInRoom.Count} enemies");
        foreach (GameObject enemy in enemiesInRoom)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetActive(true);
            }
        }
    }
}

public enum Direction { North, East, South, West }