using System.Collections.Generic;
using UnityEngine;

public class SegmentRouteBuilder : MonoBehaviour
{
    [SerializeField] private List<GameObject> segmentPrefabs;
    [SerializeField] private int segmentCount = 5;

    private Transform currentEndPoint;

    private List<GameObject> spawnedSegments = new List<GameObject>();

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        ClearLevel();

        // Начинаем со стартовой позиции
        currentEndPoint = this.transform;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject prefab = segmentPrefabs[Random.Range(0, segmentPrefabs.Count)];

            // Инстанс сегмента
            GameObject segment = Instantiate(prefab);

            // Поиск точек соединения
            Transform start = segment.transform.Find("StartPoint");
            Transform end = segment.transform.Find("EndPoint");

            if (start == null || end == null)
            {
                Debug.LogError($"Segment missing StartPoint or EndPoint: {segment.name}");
                return;
            }

            // --- 1. Совмещение StartPoint с текущим EndPoint ---
            // A) Позиция
            segment.transform.position = currentEndPoint.position;

            // B) Вращение
            Quaternion rotationDelta = Quaternion.FromToRotation(start.forward, currentEndPoint.forward);
            segment.transform.rotation = rotationDelta * segment.transform.rotation;

            // C) Коррекция смещения
            Vector3 offset = start.position - segment.transform.position;
            segment.transform.position -= offset;

            // --- 2. Устанавливаем новый текущий EndPoint ---
            currentEndPoint = segment.transform.Find("EndPoint");

            spawnedSegments.Add(segment);
        }
    }

    public void ClearLevel()
    {
        foreach (GameObject segment in spawnedSegments)
        {
            if (segment != null)
                Destroy(segment);
        }

        spawnedSegments.Clear();
    }

    public void RegenerateLevel()
    {
        GenerateLevel();
    }
}
