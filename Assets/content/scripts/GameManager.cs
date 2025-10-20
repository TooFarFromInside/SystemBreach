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

    [Header("Trigger Prefab")]
    public GameObject startTriggerPrefab;

    // Система генерации
    private Dictionary<Vector2Int, Room> roomGrid = new Dictionary<Vector2Int, Room>();
    private Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };
    private Transform playerTransform;
    private Room currentPlayerRoom;
    private bool levelActive = false;
    private float levelTimer = 0f;
    private bool timerRunning = false;

    public static GameManager Instance { get; private set; }

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
        InitializeGame();
    }

    void Update()
    {
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
        }

        TrackPlayerRoom();
    }

    void InitializeGame()
    {
        // Спавним только стартовую комнату
        SpawnRoom(startRoomData, Vector2Int.zero);

        // Спавним триггер в стартовой комнате
        SpawnStartTrigger(Vector2Int.zero);

        Debug.Log("Game initialized. Wait for trigger activation.");
    }

    public void StartLevel()
    {
        if (levelActive) return;

        levelActive = true;
        StartTimer();

        // Спавним начальные комнаты
        for (int i = 0; i < startingRooms; i++)
        {
            Vector2Int spawnPos = FindValidSpawnPosition();
            if (spawnPos != Vector2Int.zero)
            {
                RoomData randomRoom = GetRandomRoomTemplate();
                if (randomRoom != null)
                {
                    SpawnRoom(randomRoom, spawnPos, false); // Мобы неактивны
                }
            }
        }

        Debug.Log($"Level {currentLevel} started! Timer running...");
    }

    void TrackPlayerRoom()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        foreach (var room in roomGrid.Values)
        {
            if (room != null && IsPositionInRoom(playerTransform.position, room.transform.position))
            {
                if (currentPlayerRoom != room)
                {
                    currentPlayerRoom = room;
                    ActivateRoomEnemies(room);
                    Debug.Log($"Player entered room: {room.gameObject.name}");
                }
                return;
            }
        }
    }

    bool IsPositionInRoom(Vector3 position, Vector3 roomCenter)
    {
        float halfSize = roomSize / 2f;
        return position.x >= roomCenter.x - halfSize && position.x <= roomCenter.x + halfSize &&
               position.z >= roomCenter.z - halfSize && position.z <= roomCenter.z + halfSize;
    }

    void ActivateRoomEnemies(Room room)
    {
        if (room == null || room.isCleared) return;

        foreach (Transform enemy in room.transform)
        {
            if (enemy.CompareTag("Enemy"))
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.SetActive(true);
                }
            }
        }
    }

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

    void SpawnRoom(RoomData roomData, Vector2Int gridPos, bool activateEnemies = false)
    {
        if (roomData == null || roomData.roomPrefab == null)
        {
            Debug.LogError("Room data or prefab is null!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 0, gridPos.y * roomSize);
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
            PopulateRoomWithEnemies(room, activateEnemies);
        }

        Debug.Log($"Spawned {roomData.roomName} at {gridPos}");
    }

    void PopulateRoomWithEnemies(Room room, bool activateImmediately = false)
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

            EnemyRoomTracker tracker = enemy.GetComponent<EnemyRoomTracker>();
            if (tracker == null) tracker = enemy.AddComponent<EnemyRoomTracker>();
            tracker.room = room;

            // Делаем врагов неактивными
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null && !activateImmediately)
            {
                enemyAI.SetActive(false);
            }
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

    void SpawnStartTrigger(Vector2Int gridPos)
    {
        if (startTriggerPrefab == null) return;

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 10f, gridPos.y * roomSize);
        GameObject trigger = Instantiate(startTriggerPrefab, worldPos, Quaternion.identity);
    }

    RoomData GetRandomRoomTemplate()
    {
        if (enemyRoomTemplates == null || enemyRoomTemplates.Length == 0)
        {
            Debug.LogError("No enemy room templates assigned!");
            return null;
        }
        return enemyRoomTemplates[Random.Range(0, enemyRoomTemplates.Length)];
    }

    public void OnRoomCleared(Room room)
    {
        if (room == null) return;

        currentFloorScore += room.roomData.scoreReward;
        Debug.Log($"Room cleared! Floor score: {currentFloorScore}/{pointsPerLevel}");

        // Спавним награду
        SpawnReward(room);

        // Спавним новую комнату
        Vector2Int newPos = FindValidSpawnPosition();
        if (newPos != Vector2Int.zero)
        {
            RoomData newRoom = GetRandomRoomTemplate();
            if (newRoom != null)
            {
                SpawnRoom(newRoom, newPos, false);
            }
        }

        CheckLevelProgress();
    }

    void SpawnReward(Room room)
    {
        if (room.rewardSpawnPoint == null) return;

        GameObject reward = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reward.transform.position = room.rewardSpawnPoint.position;
        reward.transform.localScale = Vector3.one * 0.5f;
        reward.GetComponent<Renderer>().material.color = Color.yellow;

        RewardCollector collector = reward.AddComponent<RewardCollector>();
        collector.scoreValue = room.roomData.scoreReward / 2;
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
        StopTimer();
        Debug.Log($"Level {currentLevel} completed! Time: {levelTimer:F2}s");

        // Сохраняем комнату игрока
        Vector2Int playerRoomPos = currentPlayerRoom != null ? currentPlayerRoom.gridPosition : Vector2Int.zero;

        // Уничтожаем все комнаты кроме текущей
        ClearFloorExceptPlayerRoom(playerRoomPos);

        // Спавним новый триггер
        SpawnStartTrigger(playerRoomPos);

        // Сбрасываем параметры
        currentFloorScore = 0;
        currentLevel++;
        levelActive = false;

        Debug.Log($"Ready for level {currentLevel}. Activate the trigger to start.");
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

    void StartTimer()
    {
        timerRunning = true;
    }

    void StopTimer()
    {
        timerRunning = false;
    }

    public void AddScore(int points)
    {
        totalScore += points;
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
}