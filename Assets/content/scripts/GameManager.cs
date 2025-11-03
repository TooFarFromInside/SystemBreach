using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int pointsPerLevel = 1000;
    public int currentLevel = 1;
    public int totalScore = 0;
    public int currentFloorScore = 0;

    [Header("Grid System")]
    public int gridSize = 7;
    private Vector2Int gridCenter = new Vector2Int(3, 3);

    [Header("Room Generation")]
    public RoomData startRoomData;
    public RoomData[] enemyRoomTemplates;
    public float roomSize = 42f;

    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;

    [Header("References")]
    public GameObject startTriggerPrefab;

    // Система генерации
    private Dictionary<Vector2Int, Room> roomGrid = new Dictionary<Vector2Int, Room>();
    private Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        Debug.Log("=== GAME STARTED ===");

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager: Instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("GameManager: Initializing game...");
        InitializeGame();
    }

    void Update()
    {
        // Временная отладка - генерация по кнопке G
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Manual generation triggered!");
            StartLevel();
        }

        // Просто проверяем что E работает
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("=== E KEY PRESSED (WORKS!) ===");
        }
    }

    void InitializeGame()
    {
        Debug.Log("GameManager: Spawning start room...");

        // Используем центр сетки вместо (0,0)
        SpawnRoom(startRoomData, gridCenter);
        SpawnStartTrigger(gridCenter);

        if (roomGrid.ContainsKey(gridCenter))
        {
            Room startRoom = roomGrid[gridCenter];
            Debug.Log("GameManager: Closing all doors in start room");
            startRoom.CloseAllDoors();
        }

        Debug.Log("=== GAME READY ===");
        Debug.Log("Find the trigger sphere and press E to start!");
        Debug.Log("Or press G for manual generation");
    }

    void SpawnStartTrigger(Vector2Int gridPos)
    {
        if (startTriggerPrefab == null)
        {
            Debug.LogError("GameManager: Start trigger prefab not assigned!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 1f, gridPos.y * roomSize);
        GameObject trigger = Instantiate(startTriggerPrefab, worldPos, Quaternion.identity);
        Debug.Log($"GameManager: Start trigger spawned at {worldPos}");
    }

    public void StartLevel()
    {
        Debug.Log("=== LEVEL START TRIGGERED ===");

        int roomCount = Random.Range(2, 4);
        Debug.Log($"Generating {roomCount} initial rooms");

        List<Vector2Int> directionList = new List<Vector2Int> {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

        // Перемешиваем направления
        for (int i = 0; i < directionList.Count; i++)
        {
            Vector2Int temp = directionList[i];
            int randomIndex = Random.Range(i, directionList.Count);
            directionList[i] = directionList[randomIndex];
            directionList[randomIndex] = temp;
        }

        // ШАГ 1: Спавним все комнаты
        int spawned = 0;
        foreach (Vector2Int dir in directionList)
        {
            if (spawned >= roomCount) break;

            Vector2Int newPos = gridCenter + dir;

            if (newPos.x >= 0 && newPos.x < gridSize && newPos.y >= 0 && newPos.y < gridSize)
            {
                if (!roomGrid.ContainsKey(newPos))
                {
                    RoomData randomRoom = GetRandomRoomTemplate();
                    if (randomRoom != null)
                    {
                        SpawnRoom(randomRoom, newPos);
                        spawned++;
                    }
                }
            }
        }

        // ШАГ 2: ОБНОВЛЯЕМ ВСЕ ДВЕРИ СИНХРОННО
        UpdateAllRoomDoors();

        Debug.Log($"GameManager: Level started with {spawned} rooms!");
    }

    RoomData GetRandomRoomTemplate()
    {
        if (enemyRoomTemplates == null || enemyRoomTemplates.Length == 0)
        {
            Debug.LogError("GameManager: No enemy room templates assigned!");
            return null;
        }
        return enemyRoomTemplates[Random.Range(0, enemyRoomTemplates.Length)];
    }

    public void SpawnRoom(RoomData roomData, Vector2Int gridPos)
    {
        if (roomData == null || roomData.roomPrefab == null)
        {
            Debug.LogError("GameManager: Room data or prefab is null!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 0, gridPos.y * roomSize);
        GameObject roomObj = Instantiate(roomData.roomPrefab, worldPos, Quaternion.identity);

        Room room = roomObj.GetComponent<Room>();
        room.Initialize(roomData, gridPos);
        room.SetFloorLevelVisuals(currentLevel);

        roomGrid[gridPos] = room;

        if (roomData != startRoomData)
        {
            PopulateRoomWithEnemies(room);
        }
    }

    void PopulateRoomWithEnemies(Room room)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("GameManager: No enemy prefabs assigned!");
            return;
        }

        int enemyCount = Random.Range(room.roomData.minEnemies, room.roomData.maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector3 spawnPos = GetEnemySpawnPosition(room.transform);

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, room.transform);
            room.AddEnemy(enemy);

            EnemyRoomTracker tracker = enemy.GetComponent<EnemyRoomTracker>();
            if (tracker == null)
                tracker = enemy.AddComponent<EnemyRoomTracker>();
            tracker.room = room;
        }
    }

    Vector3 GetEnemySpawnPosition(Transform roomTransform)
    {
        float halfSize = roomSize * 0.4f;
        return roomTransform.position + new Vector3(
            Random.Range(-halfSize, halfSize),
            1f,
            Random.Range(-halfSize, halfSize)
        );
    }

    private void UpdateAllRoomDoors()
    {
        Debug.Log("GameManager: Updating ALL room doors (NEW LOGIC)");

        // ШАГ 1: Закрываем ВСЕ двери во ВСЕХ комнатах
        foreach (var room in roomGrid.Values)
        {
            room.CloseAllDoors();
        }

        // ШАГ 2: Для каждой комнаты проверяем соседей и открываем двери
        foreach (var kvp in roomGrid)
        {
            Vector2Int roomPos = kvp.Key;
            Room room = kvp.Value;

            // Проверяем все 4 направления
            CheckAndOpenDoor(roomPos, room, Vector2Int.up, Direction.North);
            CheckAndOpenDoor(roomPos, room, Vector2Int.right, Direction.East);
            CheckAndOpenDoor(roomPos, room, Vector2Int.down, Direction.South);
            CheckAndOpenDoor(roomPos, room, Vector2Int.left, Direction.West);
        }

        Debug.Log($"Updated doors for {roomGrid.Count} rooms");
    }

    private void CheckAndOpenDoor(Vector2Int roomPos, Room room, Vector2Int direction, Direction doorDirection)
    {
        Vector2Int neighborPos = roomPos + direction;

        // Проверяем есть ли комната-сосед
        if (roomGrid.ContainsKey(neighborPos))
        {
            // ОТКРЫВАЕМ дверь в текущей комнате
            room.OpenDoor(doorDirection);
            Debug.Log($"Opened {doorDirection} door in room {roomPos} (neighbor at {neighborPos})");
        }
        else
        {
            // ЗАКРЫВАЕМ дверь (должно быть уже закрыто из шага 1)
            room.CloseDoor(doorDirection);
        }
    }

    private void UpdateRoomDoors(Vector2Int roomPos, Room room)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int neighborPos = roomPos + directions[i];
            Direction direction = (Direction)i;

            // Проверяем есть ли соседняя комната
            bool hasNeighbor = roomGrid.ContainsKey(neighborPos);

            // ОТКРЫВАЕМ дверь если:
            // 1. Есть соседняя комната
            // 2. И соседняя комната тоже "видит" эту комнату (двусторонняя проверка)
            bool shouldOpen = hasNeighbor;

            if (hasNeighbor)
            {
                Room neighborRoom = roomGrid[neighborPos];
                Direction oppositeDirection = GetOppositeDirection(direction);

                // Дополнительная проверка: соседняя комната тоже должна "видеть" эту комнату
                Vector2Int checkPos = neighborPos + GetDirectionVector(oppositeDirection);
                shouldOpen = shouldOpen && (checkPos == roomPos);
            }

            room.SetDoorState(direction, shouldOpen);

            // ОБНОВЛЯЕМ СОСЕДНЮЮ КОМНАТУ ТОЖЕ!
            if (hasNeighbor)
            {
                Room neighborRoom = roomGrid[neighborPos];
                Direction oppositeDirection = GetOppositeDirection(direction);
                neighborRoom.SetDoorState(oppositeDirection, shouldOpen);
            }
        }
    }


    // Вспомогательные методы для работы с направлениями
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

    private Vector2Int GetDirectionVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return Vector2Int.up;
            case Direction.East: return Vector2Int.right;
            case Direction.South: return Vector2Int.down;
            case Direction.West: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }

    public void OnRoomCleared(Room room)
    {
        if (room == null) return;

        currentFloorScore += room.roomData.scoreReward;
        AddScore(room.roomData.scoreReward);

        Debug.Log($"GameManager: Room cleared! Floor score: {currentFloorScore}/{pointsPerLevel}");

        Vector2Int newPos = FindValidSpawnPosition();
        if (newPos != Vector2Int.zero)
        {
            RoomData newRoom = GetRandomRoomTemplate();
            if (newRoom != null)
            {
                SpawnRoom(newRoom, newPos);
                UpdateAllRoomDoors();
            }
        }

        CheckLevelProgress();
    }

    Vector2Int FindValidSpawnPosition()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        foreach (var occupiedPos in roomGrid.Keys)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = occupiedPos + dir;
                if (!roomGrid.ContainsKey(neighborPos))
                {
                    possiblePositions.Add(neighborPos);
                }
            }
        }

        // Убираем дубликаты
        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>(possiblePositions);
        possiblePositions = new List<Vector2Int>(uniquePositions);

        return possiblePositions.Count > 0 ? possiblePositions[Random.Range(0, possiblePositions.Count)] : Vector2Int.zero;
    }

    void CheckLevelProgress()
    {
        if (currentFloorScore >= pointsPerLevel)
        {
            CompleteLevel();
        }
    }

    void CompleteLevel()
    {
        Debug.Log($"GameManager: Level {currentLevel} completed!");

        // Сохраняем позицию игрока как новый центр
        Vector2Int playerRoomPos = gridCenter; // Временно - потом заменим на реальную позицию игрока
        ClearFloorExceptPlayerRoom(playerRoomPos);
        SpawnStartTrigger(playerRoomPos);

        currentFloorScore = 0;
        currentLevel++;

        Debug.Log($"GameManager: Ready for level {currentLevel}");
    }

    void ClearFloorExceptPlayerRoom(Vector2Int playerRoomPos)
    {
        List<Vector2Int> roomsToDestroy = new List<Vector2Int>();

        foreach (var kvp in roomGrid)
        {
            if (kvp.Key != playerRoomPos)
            {
                roomsToDestroy.Add(kvp.Key);
            }
        }

        foreach (Vector2Int pos in roomsToDestroy)
        {
            if (roomGrid.ContainsKey(pos) && roomGrid[pos] != null)
            {
                Destroy(roomGrid[pos].gameObject);
                roomGrid.Remove(pos);
            }
        }
    }

    public void AddScore(int points)
    {
        totalScore += points;
        Debug.Log($"GameManager: Score: {totalScore} (+{points})");
    }

    public Room GetRoomAtPosition(Vector2Int gridPos)
    {
        roomGrid.TryGetValue(gridPos, out Room room);
        return room;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Рисуем сетку
        Gizmos.color = Color.green;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 worldPos = new Vector3(x * roomSize, 0, y * roomSize);
                Gizmos.DrawWireCube(worldPos, new Vector3(roomSize, 0.1f, roomSize));

                // Помечаем занятые клетки
                if (roomGrid.ContainsKey(new Vector2Int(x, y)))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(worldPos + Vector3.up * 2f, Vector3.one * 0.5f);
                    Gizmos.color = Color.green;
                }
            }
        }
    }
}