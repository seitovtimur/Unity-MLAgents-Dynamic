using System.Collections.Generic;
using UnityEngine;

public class SegmentRouteBuilder : MonoBehaviour
{
    [SerializeField] private List<GameObject> segmentPrefabs;
    [SerializeField] private int segmentCount = 5;
    [SerializeField] private GameObject goalPrefab;


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

            // 1. Обнуляем позицию и поворот сегмента
            segment.transform.position = Vector3.zero;
            segment.transform.rotation = Quaternion.identity;

            // 2. Считаем смещение от центра сегмента до StartPoint (в мировых координатах)
            Vector3 offset = start.position - segment.transform.position;

            // 3. Вращение сегмента, чтобы его StartPoint.forward совпал с текущим EndPoint.forward (и up)
            segment.transform.rotation = Quaternion.LookRotation(currentEndPoint.forward, currentEndPoint.up);

            // 4. Сдвигаем сегмент так, чтобы StartPoint совпал с текущей точкой
            segment.transform.position = currentEndPoint.position - (start.position - segment.transform.position);

            // 5. Обновляем текущий EndPoint
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
