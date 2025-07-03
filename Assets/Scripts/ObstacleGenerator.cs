using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [SerializeField] private List<GameObject> obstaclePrefabs;

    [Header("Obstacle Settings")]
    [SerializeField] private int minObstacleCount = 3;
    [SerializeField] private int maxObstacleCount = 10;
    [SerializeField] private float minObstacleSpacing = 2.0f;
    [SerializeField] private float obstacleAreaWidth = 4f;   // ширина области (X)
    [SerializeField] private float obstacleAreaLength = 10f; // длина области (Z)
    [SerializeField] private float obstacleHeight = 1f;      // по Y

    [Header("Generated Obstacles")]
    private List<GameObject> spawnedObstacles = new List<GameObject>();

    public void GenerateObstacles()
    {
        ClearObstacles();

        int obstacleCount = Random.Range(minObstacleCount, maxObstacleCount + 1);
        float startZ = -obstacleAreaLength / 2f + minObstacleSpacing;
        float endZ = obstacleAreaLength / 2f - minObstacleSpacing;
        float stepZ = (endZ - startZ) / Mathf.Max(obstacleCount - 1, 1);

        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];

            //float posZ = startZ + i * stepZ;
            //float posX = Random.Range(-obstacleAreaWidth / 2f + 1f, obstacleAreaWidth / 2f - 1f);
            //Vector3 spawnPos = transform.position + new Vector3(posX, obstacleHeight, posZ);

            float offsetZ = startZ + i * stepZ;
            float offsetX = Random.Range(-obstacleAreaWidth / 2f + 1f, obstacleAreaWidth / 2f - 1f);

            // Используем transform.forward и transform.right для ориентации
            Vector3 spawnPos = transform.position
                             + transform.forward * offsetZ
                             + transform.right * offsetX
                             + Vector3.up * obstacleHeight;


            //GameObject obstacle = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            GameObject obstacle = Instantiate(prefab, spawnPos, transform.rotation, transform);
            spawnedObstacles.Add(obstacle);
        }
    }

    public void ClearObstacles()
    {
        foreach (var obj in spawnedObstacles)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObstacles.Clear();
    }

    public void RegenerateObstacles()
    {
        GenerateObstacles();
    }

    private void Start()
    {
        GenerateObstacles();
    }
}
