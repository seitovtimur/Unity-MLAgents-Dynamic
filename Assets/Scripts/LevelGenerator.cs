using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<GameObject> obstaclePrefabs;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject fallZonePrefab;

    [Header("Settings")]
    [SerializeField] private int minObstacleCount = 3;
    [SerializeField] private int maxObstacleCount = 10;
    [SerializeField] private float minObstacleSpacing = 2.0f;
    [SerializeField] private float goalDistance = 20;

    [Header("Level Size")]
    [SerializeField] private Vector2 floorSizeRangeX = new Vector2(1f, 2f);
    [SerializeField] private Vector2 floorSizeRangeY = new Vector2(4f, 6f);

    [Header("Generated Objects")]
    private GameObject floor;
    private GameObject fallZone;
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private GameObject goal;

    public void GenerateLevel()
    {
        ClearLevel();

        // === 1. Генерация пола ===
        float floorSizeX = Random.Range(floorSizeRangeX.x, floorSizeRangeX.y);
        float floorSizeZ = Random.Range(floorSizeRangeY.x, floorSizeRangeY.y);
        Vector3 floorScale = new Vector3(floorSizeX, 1f, floorSizeZ);
        floor = Instantiate(floorPrefab, Vector3.zero, Quaternion.identity, transform);
        floor.transform.localScale = floorScale;

        // === 1.1 Генерация FallZone (на 5 ниже и 1.2x больше пола) ===
        if (fallZonePrefab != null)
        {
            Vector3 fallZoneScale = new Vector3(floorSizeX * 2f, 1f, floorSizeZ * 1.3f);
            Vector3 fallZonePos = new Vector3(0f, -5f, 0f); // 5 метров под полом
            fallZone = Instantiate(fallZonePrefab, fallZonePos, Quaternion.identity, transform);
            fallZone.transform.localScale = fallZoneScale;
        }

        // === 2. Генерация препятствий вдоль оси Z ===
        int obstacleCount = Random.Range(minObstacleCount, maxObstacleCount + 1);
        float startZ = -floorSizeZ / 2f + minObstacleSpacing;
        float endZ = floorSizeZ / 2f - minObstacleSpacing;
        float stepZ = (endZ - startZ) / Mathf.Max(obstacleCount - 1, 1);

        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            float posZ = startZ + i * stepZ;
            float posX = Random.Range(-floorSizeX / 2f + 1f, floorSizeX / 2f - 1f);

            Vector3 spawnPos = new Vector3(posX, 1f, posZ);
            GameObject obstacle = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            spawnedObstacles.Add(obstacle);
        }

        // === 3. Спавн цели в конце Z ===
        if (goalPrefab != null)
        {
            Vector3 goalPos = new Vector3(0f, 0.5f, endZ + goalDistance);
            goal = Instantiate(goalPrefab, goalPos, Quaternion.identity, transform);
        }
    }

    public void ClearLevel()
    {
        if (floor != null) Destroy(floor);
        if (fallZone != null) Destroy(fallZone);

        foreach (var obj in spawnedObstacles)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObstacles.Clear();

        if (goal != null) Destroy(goal);
    }

    public void RegenerateLevel()
    {
        GenerateLevel();
    }

    private void Start()
    {
        GenerateLevel();
    }
}
