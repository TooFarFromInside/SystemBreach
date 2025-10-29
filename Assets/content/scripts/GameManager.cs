using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int startingRooms = 3;
    public int pointsPerLevel = 1000;
    public int currentLevel = 1;
    public int totalScore = 0;
    public int currentFloorScore = 0;

    [Header("Room Generation")]
    public RoomData startRoomData;
    public RoomData[] enemyRoomTemplates;
    public float roomSize = 20f;

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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        // Спавним стартовую комнату
        SpawnRoom(startRoomData, Vector2Int.zero);
        SpawnStartTrigger(Vector2Int.zero);
        Debug.Log("Game initialized. Wait for trigger activation.");
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    void SpawnStartTrigger(Vector2Int gridPos)
    {
        if (startTriggerPrefab == null)
        {
            Debug.LogWarning("Start trigger prefab not assigned!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 1f, gridPos.y * roomSize);
        GameObject trigger = Instantiate(startTriggerPrefab, worldPos, Quaternion.identity);
    }

    public void StartLevel()
    {
        // Спавним начальные комнаты
        for (int i = 0; i < startingRooms; i++)
        {
            Vector2Int spawnPos = FindValidSpawnPosition();
            if (spawnPos != Vector2Int.zero)
            {
                RoomData randomRoom = GetRandomRoomTemplate();
                if (randomRoom != null)
                {
                    SpawnRoom(randomRoom, spawnPos);
                }
            }
        }

        // Обновляем двери между всеми комнатами
        UpdateAllRoomDoors();

        Debug.Log($"Level {currentLevel} started!");
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    Vector2Int FindValidSpawnPosition()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Ищем все занятые позиции и их соседей
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

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    RoomData GetRandomRoomTemplate()
    {
        if (enemyRoomTemplates == null || enemyRoomTemplates.Length == 0)
        {
            Debug.LogError("No enemy room templates assigned!");
            return null;
        }
        return enemyRoomTemplates[Random.Range(0, enemyRoomTemplates.Length)];
    }

    public void SpawnRoom(RoomData roomData, Vector2Int gridPos)
    {
        if (roomData == null || roomData.roomPrefab == null)
        {
            Debug.LogError("Room data or prefab is null!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 0, gridPos.y * roomSize);
        GameObject roomObj = Instantiate(roomData.roomPrefab, worldPos, Quaternion.identity);

        Room room = roomObj.GetComponent<Room>();
        room.Initialize(roomData, gridPos);
        room.SetFloorLevelVisuals(currentLevel);

        roomGrid[gridPos] = room;

        // Для вражеских комнат заполняем врагами
        if (roomData != startRoomData)
        {
            PopulateRoomWithEnemies(room);
        }
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    void PopulateRoomWithEnemies(Room room)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned!");
            return;
        }

        int enemyCount = Random.Range(room.roomData.minEnemies, room.roomData.maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector3 spawnPos = GetEnemySpawnPosition(room.transform);

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, room.transform);
            room.AddEnemy(enemy);

            // Настраиваем отслеживание врага
            EnemyRoomTracker tracker = enemy.GetComponent<EnemyRoomTracker>();
            if (tracker == null)
                tracker = enemy.AddComponent<EnemyRoomTracker>();
            tracker.room = room;
        }
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    Vector3 GetEnemySpawnPosition(Transform roomTransform)
    {
        float halfSize = roomSize * 0.4f;
        return roomTransform.position + new Vector3(
            Random.Range(-halfSize, halfSize),
            1f,
            Random.Range(-halfSize, halfSize)
        );
    }

    // ★ ВАЖНО: Обновляем двери между соседними комнатами
    private void UpdateAllRoomDoors()
    {
        foreach (var kvp in roomGrid)
        {
            UpdateRoomDoors(kvp.Key, kvp.Value);
        }
    }

    private void UpdateRoomDoors(Vector2Int roomPos, Room room)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int neighborPos = roomPos + directions[i];
            Direction direction = (Direction)i;

            // Если сосед существует - открываем дверь
            bool shouldOpen = roomGrid.ContainsKey(neighborPos);
            room.SetDoorState(direction, shouldOpen);
        }
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    public void OnRoomCleared(Room room)
    {
        if (room == null) return;

        currentFloorScore += room.roomData.scoreReward;
        AddScore(room.roomData.scoreReward);

        Debug.Log($"Room cleared! Floor score: {currentFloorScore}/{pointsPerLevel}");

        // Спавним новую комнату
        Vector2Int newPos = FindValidSpawnPosition();
        if (newPos != Vector2Int.zero)
        {
            RoomData newRoom = GetRandomRoomTemplate();
            if (newRoom != null)
            {
                SpawnRoom(newRoom, newPos);
                UpdateAllRoomDoors(); // Обновляем двери после добавления новой комнаты
            }
        }

        CheckLevelProgress();
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    void CheckLevelProgress()
    {
        if (currentFloorScore >= pointsPerLevel)
        {
            CompleteLevel();
        }
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    void CompleteLevel()
    {
        Debug.Log($"Level {currentLevel} completed!");

        // Сохраняем комнату игрока (нужно будет доработать)
        Vector2Int playerRoomPos = Vector2Int.zero; // Временное решение

        // Уничтожаем все комнаты кроме текущей
        ClearFloorExceptPlayerRoom(playerRoomPos);

        // Спавним новый триггер
        SpawnStartTrigger(playerRoomPos);

        // Сбрасываем параметры
        currentFloorScore = 0;
        currentLevel++;

        Debug.Log($"Ready for level {currentLevel}. Activate the trigger to start.");
    }

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
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

    // ★ ДОБАВЛЕННЫЙ МЕТОД ★
    public void AddScore(int points)
    {
        totalScore += points;
        Debug.Log($"Score: {totalScore} (+{points})");
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        foreach (var kvp in roomGrid)
        {
            Vector3 center = new Vector3(kvp.Key.x * roomSize, 0, kvp.Key.y * roomSize);
            Gizmos.DrawWireCube(center, new Vector3(roomSize, 0.1f, roomSize));
        }
    }
    public Room GetRoomAtPosition(Vector2Int gridPos)
    {
        roomGrid.TryGetValue(gridPos, out Room room);
        return room;
    }
}