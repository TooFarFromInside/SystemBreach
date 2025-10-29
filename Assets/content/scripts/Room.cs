using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [System.Serializable]
    public class Door
    {
        public GameObject wall;          // Полная стена
        public GameObject doorPlug;      // Заглушка проема
        public GameObject doorTrigger;   // Триггер перехода
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
        roomData = data;
        gridPosition = gridPos;
        gameObject.name = $"Room_{gridPos.x}_{gridPos.y}_{data.roomName}";

        // Автоматически находим компоненты если не назначены
        AutoFindRoomComponents();

        // Закрываем все двери по умолчанию
        CloseAllDoors();

        // Создаем точку для награды если нет
        if (rewardSpawnPoint == null)
        {
            GameObject rewardPoint = new GameObject("RewardSpawnPoint");
            rewardPoint.transform.SetParent(transform);
            rewardPoint.transform.localPosition = Vector3.zero;
            rewardSpawnPoint = rewardPoint.transform;
        }
    }

    private void AutoFindRoomComponents()
    {
        if (floorRenderer == null)
            floorRenderer = transform.Find("Floor")?.GetComponent<Renderer>();

        if (ceilingRenderer == null)
            ceilingRenderer = transform.Find("Ceiling")?.GetComponent<Renderer>();

        // Автопоиск дверей по именам
        if (northDoor.wall == null) FindDoorComponents(ref northDoor, "North");
        if (eastDoor.wall == null) FindDoorComponents(ref eastDoor, "East");
        if (southDoor.wall == null) FindDoorComponents(ref southDoor, "South");
        if (westDoor.wall == null) FindDoorComponents(ref westDoor, "West");
    }

    private void FindDoorComponents(ref Door door, string direction)
    {
        door.wall = transform.Find($"Walls/Wall_{direction}")?.gameObject;
        door.doorPlug = transform.Find($"Door_Plugs/Plug_{direction}")?.gameObject;
        door.doorTrigger = transform.Find($"Door_Triggers/Trigger_{direction}")?.gameObject;
    }

    public void SetupDoors(bool north, bool east, bool south, bool west)
    {
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

        // Включаем/выключаем компоненты
        if (door.wall != null) door.wall.SetActive(!isOpen);
        if (door.doorPlug != null) door.doorPlug.SetActive(isOpen);
        if (door.doorTrigger != null) door.doorTrigger.SetActive(isOpen);
    }

    public void CloseAllDoors()
    {
        SetDoorState(Direction.North, false);
        SetDoorState(Direction.East, false);
        SetDoorState(Direction.South, false);
        SetDoorState(Direction.West, false);
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

    // Смена визуала для разных уровней
    public void SetFloorLevelVisuals(int level)
    {
        if (floorRenderer != null)
        {
            // Простая смена цвета в зависимости от уровня
            Color[] levelColors = { Color.white, Color.blue, Color.red, Color.green, Color.yellow };
            int colorIndex = (level - 1) % levelColors.Length;
            floorRenderer.material.color = levelColors[colorIndex];
        }
    }

    public void AddEnemy(GameObject enemy)
    {
        enemiesInRoom.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        enemiesInRoom.Remove(enemy);

        if (enemiesInRoom.Count == 0 && !isCleared)
        {
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        isCleared = true;
        GameManager.Instance.OnRoomCleared(this);
        Debug.Log($"Room {gridPosition} cleared!");
    }

    // Вызывается когда игрок входит в комнату через дверь
    public void OnPlayerEnteredFromDoor(Direction fromDirection)
    {
        // Активируем врагов в комнате
        ActivateRoomEnemies();
    }

    private void ActivateRoomEnemies()
    {
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