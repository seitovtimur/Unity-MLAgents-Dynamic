using System.Collections.Generic;
using UnityEngine;

public class SegmentRouteBuilder : MonoBehaviour
{
    [SerializeField] private List<GameObject> segmentPrefabs;
    [SerializeField] private int segmentCount = 5;
    [SerializeField] private GameObject goalPrefab;

    [Header("Branching Settings")]
    [SerializeField] private bool bothPathsToGoal = false; // ✅ чекбокс в инспекторе

    public Transform GoalTransform { get; private set; }

    private List<GameObject> spawnedSegments = new List<GameObject>();

    public void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        ClearLevel();

        // Начинаем со стартовой позиции
        List<Transform> openEndPoints = new List<Transform>();
        openEndPoints.Add(this.transform);

        for (int i = 0; i < segmentCount; i++)
        {
            List<Transform> newEndPoints = new List<Transform>();

            foreach (Transform currentEndPoint in openEndPoints)
            {
                GameObject prefab = segmentPrefabs[Random.Range(0, segmentPrefabs.Count)];
                GameObject segment = Instantiate(prefab);

                // Поиск Start и EndPoints
                Transform start = segment.transform.Find("StartPoint");
                Transform[] children = segment.GetComponentsInChildren<Transform>();

                if (start == null)
                {
                    Debug.LogError($"Segment {segment.name} missing StartPoint!");
                    continue;
                }

                // Совмещение StartPoint с текущим EndPoint
                segment.transform.position = Vector3.zero;
                segment.transform.rotation = Quaternion.identity;

                segment.transform.rotation = Quaternion.LookRotation(currentEndPoint.forward, currentEndPoint.up);
                segment.transform.position = currentEndPoint.position - (start.position - segment.transform.position);

                // Собираем все EndPoint
                foreach (Transform child in children)
                {
                    if (child.name.StartsWith("EndPoint"))
                    {
                        newEndPoints.Add(child);
                    }
                }

                spawnedSegments.Add(segment);
            }

            openEndPoints = newEndPoints;
        }

        // --- Спавн Goal ---
        if (goalPrefab != null && openEndPoints.Count > 0)
        {
            if (bothPathsToGoal)
            {
                // Все пути ведут к цели
                foreach (Transform point in openEndPoints)
                {
                    GameObject goal = Instantiate(goalPrefab, point.position, point.rotation);
                    spawnedSegments.Add(goal);

                    // Первый goal будет основным для агента
                    if (GoalTransform == null)
                        GoalTransform = goal.transform;
                }
            }
            else
            {
                // Только один путь правильный
                Transform goalPoint = openEndPoints[Random.Range(0, openEndPoints.Count)];
                GameObject goal = Instantiate(goalPrefab, goalPoint.position, goalPoint.rotation);
                spawnedSegments.Add(goal);

                GoalTransform = goal.transform;
            }
        }
    }

    public void ClearLevel()
    {
        foreach (GameObject obj in spawnedSegments)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedSegments.Clear();
        GoalTransform = null;
    }

    public void RegenerateLevel()
    {
        GenerateLevel();
    }
}
