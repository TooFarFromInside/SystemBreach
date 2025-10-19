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

    // СИСТЕМА ГЕНЕРАЦИИ
    private Dictionary<Vector2Int, Room> roomGrid = new Dictionary<Vector2Int, Room>();
    private Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };
    private Transform playerTransform; // ДОБАВИЛИ ЭТУ СТРОЧКУ

    public static GameManager Instance { get; private set; }

    // ... остальной код без изменений
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GenerateNewFloor();
    }

    void OnApplicationQuit()
    {
        // Принудительная очистка при выходе
        ClearCurrentFloorImmediate();
    }

    public void GenerateNewFloor()
    {
        // Очищаем предыдущий этаж
        ClearCurrentFloorImmediate();
        currentFloorScore = 0;

        Debug.Log("=== GENERATING NEW FLOOR ===");

        // Проверяем стартовую комнату
        if (startRoomData == null)
        {
            Debug.LogError("StartRoomData is not assigned in GameManager!");
            return;
        }

        if (startRoomData.roomPrefab == null)
        {
            Debug.LogError("StartRoomData.roomPrefab is null!");
            return;
        }

        // Спавним стартовую комнату
        SpawnRoom(startRoomData, Vector2Int.zero);

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

        Debug.Log($"New floor generated! Level {currentLevel}, Rooms: {roomGrid.Count}");
    }

    Vector2Int FindValidSpawnPosition()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Если это первый вызов и есть только стартовая комната
        if (roomGrid.Count == 1 && roomGrid.ContainsKey(Vector2Int.zero))
        {
            foreach (Vector2Int dir in directions)
            {
                possiblePositions.Add(dir);
            }
            return possiblePositions[Random.Range(0, possiblePositions.Count)];
        }

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

    void SpawnRoom(RoomData roomData, Vector2Int gridPos)
    {
        if (roomData == null || roomData.roomPrefab == null)
        {
            Debug.LogError("Room data or prefab is null!");
            return;
        }

        // НАХОДИМ ИГРОКА ЕСЛИ ЕЩЕ НЕ НАШЛИ
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // РАСЧЕТ ПОЗИЦИИ НА ОДНОМ УРОВНЕ С ИГРОКОМ
        float playerY = 0f;
        if (playerTransform != null)
        {
            playerY = playerTransform.position.y;
        }

        // ИСПРАВЛЕННАЯ СТРОКА: gridPos.y вместо gridPos.z
        Vector3 worldPos = new Vector3(
            gridPos.x * roomSize,
            playerY,  // ТА ЖЕ ВЫСОТА ЧТО И У ИГРОКА
            gridPos.y * roomSize  // ИСПРАВЛЕНО: y вместо z
        );

        GameObject roomObj = Instantiate(roomData.roomPrefab, worldPos, Quaternion.identity);

        Room room = roomObj.GetComponent<Room>();
        if (room == null)
        {
            room = roomObj.AddComponent<Room>();
        }
        room.Initialize(roomData, gridPos);

        roomGrid[gridPos] = room;

        // Заполняем врагами, если это не стартовая комната
        if (roomData != startRoomData)
        {
            PopulateRoomWithEnemies(room);
        }

        Debug.Log($"Spawned {roomData.roomName} at {gridPos} (world: {worldPos})");
    }
    void PopulateRoomWithEnemies(Room room)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned in GameManager!");
            return;
        }

        int enemyCount = Random.Range(room.roomData.minEnemies, room.roomData.maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector3 spawnPos = GetEnemySpawnPosition(room.transform);

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, room.transform);

            // Добавляем трекер
            EnemyRoomTracker tracker = enemy.GetComponent<EnemyRoomTracker>();
            if (tracker == null)
            {
                tracker = enemy.AddComponent<EnemyRoomTracker>();
            }
            tracker.room = room;

            Debug.Log($"Spawned enemy in room: {room.gameObject.name}");
        }
    }

    Vector3 GetEnemySpawnPosition(Transform roomTransform)
    {
        float halfSize = roomSize * 0.4f;
        Vector3 localPos = new Vector3(
            Random.Range(-halfSize, halfSize),
            1f,  // 1 единица над полом
            Random.Range(-halfSize, halfSize)
        );

        Vector3 worldPos = roomTransform.position + localPos;

        // Убедимся что враг на правильной высоте
        worldPos.y = roomTransform.position.y + 1f;

        return worldPos;
    }

    RoomData GetRandomRoomTemplate()
    {
        if (enemyRoomTemplates == null || enemyRoomTemplates.Length == 0)
        {
            Debug.LogError("No enemy room templates assigned in GameManager!");
            return null;
        }

        return enemyRoomTemplates[Random.Range(0, enemyRoomTemplates.Length)];
    }

    public void OnRoomCleared(Room room)
    {
        if (room == null) return;

        currentFloorScore += room.roomData.scoreReward;
        Debug.Log($"Room cleared! Floor score: {currentFloorScore}/{pointsPerLevel}");

        // Спавним новую комнату
        Vector2Int newPos = FindValidSpawnPosition();
        if (newPos != Vector2Int.zero)
        {
            RoomData newRoom = GetRandomRoomTemplate();
            if (newRoom != null)
            {
                SpawnRoom(newRoom, newPos);
            }
        }

        // Проверяем уровень
        CheckLevelProgress();
    }

    void CheckLevelProgress()
    {
        if (currentFloorScore >= pointsPerLevel)
        {
            currentLevel++;
            Debug.Log($"LEVEL UP! Now level {currentLevel}");
            GenerateNewFloor();
        }
    }

    public void AddScore(int points)
    {
        totalScore += points;
        Debug.Log($"Total score: {totalScore}");
    }

    void ClearCurrentFloorImmediate()
    {
        // Создаем временный список чтобы избежать модификации во время итерации
        List<GameObject> roomsToDestroy = new List<GameObject>();

        foreach (var room in roomGrid.Values)
        {
            if (room != null && room.gameObject != null)
            {
                roomsToDestroy.Add(room.gameObject);
            }
        }

        // Уничтожаем все комнаты
        foreach (var roomObj in roomsToDestroy)
        {
            if (roomObj != null)
            {
                DestroyImmediate(roomObj);
            }
        }

        roomGrid.Clear();
        Debug.Log("Floor cleared immediately");
    }

    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        foreach (var kvp in roomGrid)
        {
            Vector3 center = new Vector3(kvp.Key.x * roomSize, 0, kvp.Key.y * roomSize);
            Gizmos.DrawWireCube(center, new Vector3(roomSize, 0.1f, roomSize));

            // Цвет в зависимости от состояния комнаты
            if (kvp.Value.isCleared)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(center, new Vector3(2f, 0.1f, 2f));
                Gizmos.color = Color.cyan;
            }
        }
    }
}