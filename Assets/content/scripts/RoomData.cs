using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "Game/Room Data")]
public class RoomData : ScriptableObject
{
    [Header("Room Settings")]
    public GameObject roomPrefab;
    public string roomName;
    public int minEnemies = 2;
    public int maxEnemies = 4;
    public int scoreReward = 100;
}