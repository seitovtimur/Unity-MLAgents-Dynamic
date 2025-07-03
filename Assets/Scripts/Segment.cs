using System.Collections.Generic;
using UnityEngine;

public class Segment : MonoBehaviour
{
    public Transform StartPoint;
    public Transform EndPoint;

    [Header("Obstacle Settings")]
    public List<GameObject> obstaclePrefabs;
    public int minObstacleCount = 0;
    public int maxObstacleCount = 3;
    public float spawnAreaWidth = 2f;
    public float spawnAreaLength = 4f;

    [SerializeField] private Transform obstacleParent;

    private void Start()
    {
        SpawnObstacles();
    }

    public void SpawnObstacles()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0)
            return;

        int obstacleCount = Random.Range(minObstacleCount, maxObstacleCount + 1);
        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            Vector3 pos = GetRandomPosition();

            GameObject obstacle = Instantiate(prefab, pos, Quaternion.identity, obstacleParent != null ? obstacleParent : transform);
        }
    }

    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f);
        float z = Random.Range(1f, spawnAreaLength);
        return transform.position + transform.forward * z + transform.right * x + Vector3.up * 0.5f;
    }
}
